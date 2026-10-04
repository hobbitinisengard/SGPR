using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;

namespace RVP
{
	/// <summary>
	/// Stunt GP's track collision is a swept sphere against polygon faces, edges,
	/// and vertices. This applies that query to the readable meshes used by the
	/// remake's modular track colliders, leaving the response to OriginalVehiclePhysics.
	/// </summary>
	internal static class OriginalTrackSphereSweep
	{
		const float PlaneTolerance = 0.0025f; // 0.25 source units converted to Unity metres.
		const float SourceDistanceTolerance = 0.0001f; // 0.01 source units converted to metres.
		const float SourceSpeedTolerance = 0.00001f; // 0.001 source units converted to metres.
		const int LeafTriangleCount = 8;

		static readonly Dictionary<Mesh, MeshTree> Trees = new();
		static readonly HashSet<Mesh> UnreadableMeshes = new();
		static readonly HashSet<int> ProcessedColliderIds = new();
		static readonly List<int> CandidateTriangles = new();
		static readonly List<SweepCollider> SweepColliders = new();
		static readonly Dictionary<int, ColliderWorldMesh> ColliderWorldMeshes = new();
		static readonly ProfilerMarker CandidateCollectionMarker =
			new ProfilerMarker("RVP.OriginalPhysics.TrackMeshSweep.CandidateCollection");
		static readonly ProfilerMarker ColliderFilteringMarker =
			new ProfilerMarker("RVP.OriginalPhysics.TrackMeshSweep.ColliderFiltering");
		static readonly ProfilerMarker TriangleTestMarker =
			new ProfilerMarker("RVP.OriginalPhysics.TrackMeshSweep.TriangleTests");

		struct Triangle
		{
			public int a, b, c;
			public int sourceTriangleIndex;
			public Bounds bounds;
			public Vector3 center;
		}

		sealed class Node
		{
			public Bounds bounds;
			public Node left, right;
			public int first, count;
			public bool IsLeaf => left == null;
		}

		sealed class MeshTree
		{
			public readonly Vector3[] vertices;
			public readonly Triangle[] triangles;
			public readonly Node root;

			public MeshTree(Vector3[] vertices, Triangle[] triangles)
			{
				this.vertices = vertices;
				this.triangles = triangles;
				root = triangles.Length == 0 ? null : BuildNode(triangles, 0, triangles.Length);
			}
		}

		sealed class ColliderWorldMesh
		{
			public Mesh mesh;
			public Matrix4x4 localToWorld;
			public Vector3[] vertices;
		}

		struct SweepCollider
		{
			public MeshCollider collider;
			public MeshTree tree;
			public Matrix4x4 worldToLocal;
			public Vector3[] worldVertices;
			public Vector3 localRadiusExpansion;
		}

		sealed class CenterComparer : IComparer<Triangle>
		{
			readonly int axis;
			public CenterComparer(int axis) => this.axis = axis;
			public int Compare(Triangle a, Triangle b) => a.center[axis].CompareTo(b.center[axis]);
		}

		struct Contact
		{
			public Vector3 center, surface, normal;
			public int triangleIndex;
		}

		struct SegmentQuery
		{
			readonly Vector3 start, end, step, direction;
			readonly float radius, radiusSquared, stepLength;
			float polygonClosest;
			Contact polygonContact;
			bool polygonFound;

			public SegmentQuery(Vector3 start, Vector3 end, Vector3 direction, float radius)
			{
				this.start = start;
				this.end = end;
				step = end - start;
				this.direction = direction;
				this.radius = radius;
				radiusSquared = radius * radius;
				stepLength = step.magnitude;
				polygonClosest = 0;
				polygonContact = default;
				polygonFound = false;
			}

			float ProjectedDistance(Vector3 point) => Vector3.Dot(direction, point - start);

			bool Accept(Vector3 center)
			{
				float distance = ProjectedDistance(center);
				if (!(distance >= -SourceDistanceTolerance && distance < polygonClosest))
					return false;
				polygonClosest = distance;
				return true;
			}

			void SetContact(Vector3 center, Vector3 surface, Vector3 normal, int triangleIndex)
			{
				polygonContact = new Contact
				{
					center = center,
					surface = surface,
					normal = normal,
					triangleIndex = triangleIndex
				};
				polygonFound = true;
			}

			bool TestVertex(Vector3 point, int triangleIndex)
			{
				Vector3 delta = point - start;
				float distanceSquared = delta.sqrMagnitude;
				float reach = stepLength + radius;
				if (distanceSquared > reach * reach)
					return false;

				float along = Vector3.Dot(delta, direction);
				float perpendicular = distanceSquared - along * along;
				if (perpendicular > radiusSquared)
					return false;

				float t = along - Mathf.Sqrt(radiusSquared - perpendicular);
				Vector3 center = start + direction * t;
				if (!Accept(center))
					return false;

				Vector3 normal = (center - point).normalized;
				SetContact(center, point + normal * radius, normal, triangleIndex);
				return true;
			}

			bool TestEdge(Vector3 a, Vector3 b, int triangleIndex)
			{
				Vector3 edge = b - a;
				float edgeLength = edge.magnitude;
				if (edgeLength <= 0.000001f)
					return false;
				Vector3 axis = edge / edgeLength;
				Vector3 delta = start - a;
				float along = Vector3.Dot(delta, axis);
				float movement = Vector3.Dot(step, axis);
				if (-movement > along || along > movement + edgeLength)
					return false;

				Vector3 axisProjection = axis * along;
				Vector3 offAxis = delta - axisProjection;
				if (offAxis.magnitude > radius + stepLength)
					return false;

				Vector3 castDirection = stepLength < 0.0000001f ? delta : step;
				Vector3 perpendicular = Vector3.Cross(castDirection, axis);
				float perpendicularLength = perpendicular.magnitude;
				if (perpendicularLength <= 0.000001f)
					return false;
				perpendicular /= perpendicularLength;
				float distance = Mathf.Abs(Vector3.Dot(perpendicular, delta));
				if (!(distance < radius))
					return false;

				Vector3 crossDelta = Vector3.Cross(delta, axis);
				float shift = -Vector3.Dot(crossDelta, perpendicular) / perpendicularLength;
				Vector3 perpendicular2 = Vector3.Cross(perpendicular, axis).normalized;
				float denominator = Vector3.Dot(perpendicular2, castDirection);
				if (Mathf.Abs(denominator) <= 0.000001f)
					return false;

				float t = Mathf.Abs(Mathf.Sqrt(radiusSquared - distance * distance) / denominator);
				if (t < 0.0001f)
					t = 0;
				t = shift - t;
				Vector3 center = start + castDirection * t;
				if (!Accept(center))
					return false;

				Vector3 centerDelta = center - a;
				Vector3 alongAxis = axis * Vector3.Dot(centerDelta, axis);
				Vector3 surface = a + alongAxis;
				Vector3 normal = (center - surface).normalized;
				SetContact(center, surface, normal, triangleIndex);
				return true;
			}

			bool InsideTriangle(Vector3 a, Vector3 b, Vector3 c, Vector3 normal, Vector3 point)
			{
				Vector3 previous = a;
				for (int i = 0; i < 3; i++)
				{
					Vector3 next = i == 0 ? b : i == 1 ? c : a;
					Vector3 edge = next - previous;
					Vector3 side = Vector3.Cross(edge, normal).normalized;
					if (Vector3.Dot(point - previous, side) > 0)
						return false;
					previous = next;
				}
				return true;
			}

			public bool TestTriangle(Vector3 a, Vector3 b, Vector3 c, int triangleIndex, out Contact contact)
			{
				contact = default;
				Vector3 normal = Vector3.Cross(b - a, c - a);
				if (normal.sqrMagnitude <= 0.00000001f)
					return false;
				normal.Normalize();

				float endpointDistance = Vector3.Dot(end - a, normal);
				float startDistance = Vector3.Dot(start - a, normal);
				if (Mathf.Abs(endpointDistance) > radius + PlaneTolerance &&
					Mathf.Abs(startDistance) > radius + PlaneTolerance)
					return false;

				polygonClosest = (radius * 4) * (radius * 4);
				polygonContact = default;
				polygonFound = false;
				TestVertex(a, triangleIndex);
				TestVertex(b, triangleIndex);
				TestVertex(c, triangleIndex);
				TestEdge(a, b, triangleIndex);
				TestEdge(b, c, triangleIndex);
				TestEdge(c, a, triangleIndex);

				float speed = Vector3.Dot(step, normal);
				if (!(Mathf.Abs(startDistance + speed) < radius))
				{
					contact = polygonContact;
					return polygonFound;
				}

				Vector3 center = start;
				if (Mathf.Abs(speed) >= SourceSpeedTolerance)
				{
					float distance = Vector3.Dot(end - a, normal);
					float t = startDistance < 0
						? (-distance - radius) / speed
						: -((distance - radius) / speed);
					t = Mathf.Clamp(t - 0.0001f, -1, 0);
					center = end + step * t;
				}

				if (!InsideTriangle(a, b, c, normal, center) || !Accept(center))
				{
					contact = polygonContact;
					return polygonFound;
				}

				if (startDistance < 0)
					normal = -normal;
				SetContact(center, center - normal * radius, normal, triangleIndex);
				contact = polygonContact;
				return true;
			}
		}

		public static bool WasProcessed(MeshCollider collider) =>
			collider && ProcessedColliderIds.Contains(collider.GetInstanceID());

		static MeshTree GetTree(Mesh mesh)
		{
			if (!mesh || UnreadableMeshes.Contains(mesh))
				return null;
			if (Trees.TryGetValue(mesh, out MeshTree cached))
				return cached;
			if (!mesh.isReadable)
			{
				UnreadableMeshes.Add(mesh);
				return null;
			}

			try
			{
				Vector3[] vertices = mesh.vertices;
				List<Triangle> triangles = new();
				int sourceTriangleIndex = 0;
				for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
				{
					if (mesh.GetTopology(submesh) != MeshTopology.Triangles)
						continue;
					int[] indices = mesh.GetTriangles(submesh);
					for (int i = 0; i + 2 < indices.Length; i += 3, sourceTriangleIndex++)
					{
						int a = indices[i], b = indices[i + 1], c = indices[i + 2];
						if (a < 0 || b < 0 || c < 0 || a >= vertices.Length || b >= vertices.Length || c >= vertices.Length)
							continue;
						Bounds bounds = new(vertices[a], Vector3.zero);
						bounds.Encapsulate(vertices[b]);
						bounds.Encapsulate(vertices[c]);
						triangles.Add(new Triangle
						{
							a = a,
							b = b,
							c = c,
							sourceTriangleIndex = sourceTriangleIndex,
							bounds = bounds,
							center = (vertices[a] + vertices[b] + vertices[c]) / 3
						});
					}
				}

				MeshTree tree = new(vertices, triangles.ToArray());
				Trees.Add(mesh, tree);
				return tree;
			}
			catch (Exception exception) when (exception is UnityException || exception is ArgumentException ||
				exception is InvalidOperationException || exception is IndexOutOfRangeException)
			{
				UnreadableMeshes.Add(mesh);
				return null;
			}
		}

		static bool MatricesExactlyMatch(Matrix4x4 a, Matrix4x4 b)
		{
			for (int i = 0; i < 16; i++)
				if (a[i] != b[i])
					return false;
			return true;
		}

		static Vector3[] GetWorldVertices(MeshCollider collider, MeshTree tree, Matrix4x4 localToWorld)
		{
			int colliderId = collider.GetInstanceID();
			Mesh mesh = collider.sharedMesh;
			if (!ColliderWorldMeshes.TryGetValue(colliderId, out ColliderWorldMesh cache) ||
				cache.mesh != mesh || !MatricesExactlyMatch(cache.localToWorld, localToWorld))
			{
				Vector3[] worldVertices = new Vector3[tree.vertices.Length];
				for (int i = 0; i < worldVertices.Length; i++)
					worldVertices[i] = localToWorld.MultiplyPoint3x4(tree.vertices[i]);

				if (cache == null)
					cache = new ColliderWorldMesh();
				cache.mesh = mesh;
				cache.localToWorld = localToWorld;
				cache.vertices = worldVertices;
				ColliderWorldMeshes[colliderId] = cache;
			}

			return cache.vertices;
		}

		static Node BuildNode(Triangle[] triangles, int first, int count)
		{
			Bounds bounds = triangles[first].bounds;
			for (int i = first + 1; i < first + count; i++)
				bounds.Encapsulate(triangles[i].bounds);
			Node node = new() { bounds = bounds, first = first, count = count };
			if (count <= LeafTriangleCount)
				return node;

			Vector3 extents = bounds.extents;
			int axis = extents.x >= extents.y && extents.x >= extents.z ? 0 : extents.y >= extents.z ? 1 : 2;
			Array.Sort(triangles, first, count, new CenterComparer(axis));
			int leftCount = count / 2;
			node.left = BuildNode(triangles, first, leftCount);
			node.right = BuildNode(triangles, first + leftCount, count - leftCount);
			return node;
		}

		static void Collect(Node node, Bounds query, List<int> output)
		{
			if (node == null || !node.bounds.Intersects(query))
				return;
			if (node.IsLeaf)
			{
				for (int i = node.first; i < node.first + node.count; i++)
					output.Add(i);
				return;
			}
			Collect(node.left, query, output);
			Collect(node.right, query, output);
		}

		static float MinAbsoluteScale(Vector3 scale)
		{
			float x = Mathf.Abs(scale.x), y = Mathf.Abs(scale.y), z = Mathf.Abs(scale.z);
			return Mathf.Min(x, Mathf.Min(y, z));
		}

		static Vector3 WorldRadiusInLocalBounds(Matrix4x4 worldToLocal, float worldRadius)
		{
			return new Vector3(
				new Vector3(worldToLocal.m00, worldToLocal.m01, worldToLocal.m02).magnitude,
				new Vector3(worldToLocal.m10, worldToLocal.m11, worldToLocal.m12).magnitude,
				new Vector3(worldToLocal.m20, worldToLocal.m21, worldToLocal.m22).magnitude) * worldRadius;
		}

		public static bool TrySweep(Collider[] colliders, int layerMask, Vector3 start, Vector3 movement, float radius,
			out Collider collider, out Vector3 point, out Vector3 normal, out Vector3 center,
			out float travelDistance, out int triangleIndex)
		{
			collider = null;
			point = normal = center = Vector3.zero;
			travelDistance = 0;
			triangleIndex = -1;
			ProcessedColliderIds.Clear();
			float distance = movement.magnitude;
			if (colliders == null || distance <= 0.000001f || radius <= 0)
				return false;

			Vector3 direction = movement / distance;
			int steps = Mathf.CeilToInt(distance / (radius * 2));
			// The original query bounds its subdivided sweep. Leave exceptionally
			// long or malformed probes to PhysX rather than stalling the fixed tick.
			if (steps <= 0 || steps > 65536)
				return false;
			Bounds sweptBounds = new((start + (start + movement)) * 0.5f,
				new Vector3(Mathf.Abs(movement.x), Mathf.Abs(movement.y), Mathf.Abs(movement.z)) +
				Vector3.one * (2 * (radius + PlaneTolerance)));

			// Filter track colliders once per complete sweep. The previous loop
			// repeated collider state, layer, and bounds checks for every substep.
			SweepColliders.Clear();
			using (ColliderFilteringMarker.Auto())
			{
				foreach (Collider candidate in colliders)
				{
					if (!candidate || candidate.isTrigger || !candidate.enabled ||
						!candidate.gameObject.activeInHierarchy || candidate.attachedRigidbody ||
						(layerMask & (1 << candidate.gameObject.layer)) == 0 ||
						!sweptBounds.Intersects(candidate.bounds) || candidate is not MeshCollider meshCollider ||
						meshCollider.convex)
						continue;

					MeshTree tree = GetTree(meshCollider.sharedMesh);
					if (tree == null || tree.root == null)
						continue;

					Transform colliderTransform = candidate.transform;
					if (MinAbsoluteScale(colliderTransform.lossyScale) <= 0.000001f)
						continue;

					ProcessedColliderIds.Add(candidate.GetInstanceID());
					Matrix4x4 worldToLocal = colliderTransform.worldToLocalMatrix;
					Matrix4x4 localToWorld = colliderTransform.localToWorldMatrix;
					SweepColliders.Add(new SweepCollider
					{
						collider = meshCollider,
						tree = tree,
						worldToLocal = worldToLocal,
					worldVertices = GetWorldVertices(meshCollider, tree, localToWorld),
					localRadiusExpansion = WorldRadiusInLocalBounds(worldToLocal, radius + PlaneTolerance) * 2
					});
				}
			}

			for (int stepIndex = 1; stepIndex <= steps; stepIndex++)
			{
				Vector3 segmentStart = start + movement * ((stepIndex - 1f) / steps);
				Vector3 segmentEnd = start + movement * (stepIndex / (float)steps);
				float segmentLength = Vector3.Distance(segmentStart, segmentEnd);
				float closestDistance = segmentLength;
				Contact closestContact = default;
				Collider closestCollider = null;

				foreach (SweepCollider sweepCollider in SweepColliders)
				{
					MeshCollider candidate = sweepCollider.collider;
					MeshTree tree = sweepCollider.tree;
					Vector3[] worldVertices = sweepCollider.worldVertices;
					Vector3 localStart = sweepCollider.worldToLocal.MultiplyPoint3x4(segmentStart);
					Vector3 localEnd = sweepCollider.worldToLocal.MultiplyPoint3x4(segmentEnd);
					Bounds localQuery = new(localStart, Vector3.zero);
					localQuery.Encapsulate(localEnd);
					localQuery.Expand(sweepCollider.localRadiusExpansion);
					CandidateTriangles.Clear();
					using (CandidateCollectionMarker.Auto())
						Collect(tree.root, localQuery, CandidateTriangles);

					SegmentQuery query = new(segmentStart, segmentEnd, direction, radius);
					using (TriangleTestMarker.Auto())
					{
						foreach (int triangleSlot in CandidateTriangles)
						{
							Triangle triangle = tree.triangles[triangleSlot];
							Vector3 a = worldVertices[triangle.a];
							Vector3 b = worldVertices[triangle.b];
							Vector3 c = worldVertices[triangle.c];
							if (!query.TestTriangle(a, b, c, triangle.sourceTriangleIndex, out Contact contact))
								continue;

							float projected = Vector3.Dot(direction, contact.center - segmentStart);
							if (projected < -SourceDistanceTolerance || !(projected < closestDistance))
								continue;
							closestDistance = projected;
							closestContact = contact;
							closestCollider = candidate;
						}
					}
				}

				if (!closestCollider)
					continue;
				collider = closestCollider;
				point = closestContact.surface;
				normal = closestContact.normal;
				center = closestContact.center;
				travelDistance = Vector3.Distance(start, center);
				triangleIndex = closestContact.triangleIndex;
				return true;
			}
			return false;
		}
	}
}
