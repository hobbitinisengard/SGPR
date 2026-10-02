using UnityEngine;
using System.Collections;

namespace RVP
{
	[RequireComponent(typeof(Collider))]
	[DisallowMultipleComponent]
	[AddComponentMenu("RVP/Ground Surface/Ground Surface Instance", 1)]

	// Class for instances of surface types
	public class GroundSurfaceInstance : MonoBehaviour
	{
		[Tooltip("Which surface type to use from the GroundSurfaceMaster list of surface types")]
		public int surfaceType;
		[System.NonSerialized]
		public float friction;
		[Header("Original Stunt GP contact overrides")]
		[Tooltip("Optional source grip override for this collider; below zero uses its surface type.")]
		public float sourceGrip = -1;
		[Tooltip("Optional raw PMD material flags override for this collider; below zero uses its surface type.")]
		[Range(-1, 255)]
		public int sourceFlags = -1;
		[Tooltip("Optional source rolling-resistance override; below zero uses its surface type.")]
		public float sourceRolling = -1;
		[Tooltip("Optional source roughness byte override (0-255); below zero uses its surface type.")]
		public int sourceRoughness = -1;
		[Tooltip("Optional original contact profile overrides keyed by this collider mesh's submesh index.")]
		public SourceTrackSubmeshProfile[] sourceSubmeshProfiles = System.Array.Empty<SourceTrackSubmeshProfile>();

		public bool TryGetSourceSubmeshProfile(int triangleIndex, out float grip, out float rolling,
			out int roughness, out int gripByte, out int materialFlags)
		{
			grip = rolling = -1;
			roughness = gripByte = materialFlags = -1;
			if (triangleIndex < 0 || sourceSubmeshProfiles == null || sourceSubmeshProfiles.Length == 0)
				return false;

			MeshCollider meshCollider = GetComponent<MeshCollider>();
			Mesh mesh = meshCollider ? meshCollider.sharedMesh : null;
			if (!mesh)
				return false;

			int remainingTriangle = triangleIndex;
			int hitSubmesh = -1;
			for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
			{
				if (mesh.GetTopology(submesh) != MeshTopology.Triangles)
					continue;

				uint triangleCount = mesh.GetIndexCount(submesh) / 3;
				if ((uint)remainingTriangle < triangleCount)
				{
					hitSubmesh = submesh;
					break;
				}
				remainingTriangle -= (int)triangleCount;
			}
			if (hitSubmesh < 0)
				return false;

			foreach (SourceTrackSubmeshProfile profile in sourceSubmeshProfiles)
			{
				if (profile == null || profile.submeshIndex != hitSubmesh)
					continue;
				grip = profile.sourceGrip;
				rolling = profile.sourceRolling;
				roughness = profile.sourceRoughness;
				gripByte = profile.sourceGripByte;
				materialFlags = profile.sourceFlags;
				return true;
			}
			return false;
		}

		public void UseGroundSurfaceMasterProfile()
		{
			sourceGrip = -1;
			sourceFlags = -1;
			sourceRolling = -1;
			sourceRoughness = -1;
			sourceSubmeshProfiles = System.Array.Empty<SourceTrackSubmeshProfile>();
		}

		void Start()
		{
			// Set friction
			if (GroundSurfaceMaster.surfaceTypesStatic[surfaceType].useColliderFriction)
			{
				PhysicsMaterial sharedMat = GetComponent<Collider>().sharedMaterial;
				friction = sharedMat != null ? sharedMat.dynamicFriction * 2 : 1.0f;
			}
			else
			{
				friction = GroundSurfaceMaster.surfaceTypesStatic[surfaceType].friction;
			}
		}
	}

	[System.Serializable]
	public sealed class SourceTrackSubmeshProfile
	{
		[Min(0)]
		public int submeshIndex;
		[Tooltip("Optional effective source grip override. Below zero uses the raw PMD grip byte and flags below.")]
		public float sourceGrip = -1;
		[Tooltip("Raw PMD grip byte (0-255); source code scales it by 1/64.")]
		[Range(-1, 255)]
		public int sourceGripByte = -1;
		[Tooltip("Raw original PMD rolling-resistance byte. Below zero leaves the surface profile unchanged.")]
		public float sourceRolling = -1;
		[Tooltip("Raw original PMD roughness byte. Below zero leaves the surface profile unchanged.")]
		public int sourceRoughness = -1;
		[Tooltip("Raw PMD material flags byte. A low nibble of 10 overrides effective grip to 4.")]
		[Range(-1, 255)]
		public int sourceFlags = -1;
	}
}
