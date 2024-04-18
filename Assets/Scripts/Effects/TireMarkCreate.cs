using UnityEngine;
using System;

namespace RVP
{
	[RequireComponent(typeof(Wheel))]
	[DisallowMultipleComponent]
	[AddComponentMenu("RVP/Effects/Tire Mark Creator", 0)]

	// Class for creating tire marks
	public class TireMarkCreate : MonoBehaviour
	{
		Transform tr;
		Wheel w;
		Mesh mesh;
		int[] tris;
		Vector3[] verts;
		Vector2[] uvs;
		Color[] colors;

		Vector3 leftPoint;
		Vector3 rightPoint;
		Vector3 leftPointPrev;
		Vector3 rightPointPrev;

		bool creatingMark;
		bool continueMark; // Continue making mark after current one ends
		GameObject curMark; // Current mark
		Transform curMarkTr;
		int curEdge;
		float gapDelay; // Gap between segments

		int curSurface = -1; // Current surface type
		int prevSurface = -1; // Previous surface type

		bool popped = false;
		bool poppedPrev = false;

		public float alwaysScrape;

		public bool calculateTangents = true;

		[Tooltip("Materials in array correspond to indices in surface types in GroundSurfaceMaster")]
		public Material[] tireMarkMaterials;

		[Tooltip("Materials in array correspond to indices in surface types in GroundSurfaceMaster")]
		public Material[] rimMarkMaterials;

		[Tooltip("Particles in array correspond to indices in surface types in GroundSurfaceMaster")]
		[NonSerialized]
		public ParticleSystem[] debrisParticles;
		public ParticleSystem sparks;
		float[] initialEmissionRates;
		ParticleSystem.MinMaxCurve zeroEmission = new(0);
		private void Awake()
		{
			debrisParticles = new ParticleSystem[]
			{
				transform.GetChild(1).GetComponent<ParticleSystem>(),//smoke
				transform.GetChild(2).GetComponent<ParticleSystem>(),//mud
				transform.GetChild(3).GetComponent<ParticleSystem>(),//dirt
				transform.GetChild(4).GetComponent<ParticleSystem>(),//ice
				transform.GetChild(2).GetComponent<ParticleSystem>(),//extmud
				transform.GetChild(3).GetComponent<ParticleSystem>(),//exdirt
				transform.GetChild(4).GetComponent<ParticleSystem>(),//exice
			};
		}

		void Start()
		{
			tr = transform;
			w = GetComponent<Wheel>();

			initialEmissionRates = new float[debrisParticles.Length + 1];
			for (int i = 0; i < debrisParticles.Length; i++)
			{
				initialEmissionRates[i] = debrisParticles[i].emission.rateOverTime.constantMax;
				var em = debrisParticles[i].emission;
				em.rateOverTime = zeroEmission;
			}

			if (sparks)
			{
				initialEmissionRates[debrisParticles.Length] = sparks.emission.rateOverTime.constantMax;
			}
		}
		void Update()
		{

			// Check for continuous marking
			if (w.grounded)
			{
				alwaysScrape = GroundSurfaceMaster.surfaceTypesStatic[w.contactPoint.surfaceType].alwaysScrape ?
					(w.slipThreshold + w.vp.engine.targetPitch) : 0;
			}
			else
			{
				alwaysScrape = 0;
			}

			// Create mark
			if (w.grounded && (Mathf.Abs(F.MaxAbs(w.sidewaysSlip, w.forwardSlip)) > w.slipThreshold || alwaysScrape > 0) && w.connected)
			{
				w.sliding = true;
				prevSurface = curSurface;
				curSurface = w.grounded ? w.contactPoint.surfaceType : -1;

				poppedPrev = popped;
				popped = w.popped;

				if (!creatingMark)
				{
					prevSurface = curSurface;
					StartMark();
				}
				else if (curSurface != prevSurface || popped != poppedPrev)
				{
					EndMark();
				}

				// Calculate segment points
				if (curMark)
				{
					Vector3 pointDir = Quaternion.AngleAxis(90, w.contactPoint.normal) * tr.right * (w.popped ? w.rimWidth : w.tireWidth);
					leftPoint = curMarkTr.InverseTransformPoint(w.contactPoint.point + pointDir * w.suspensionParent.flippedSideFactor * Mathf.Sign(w.rawRPM) + w.contactPoint.normal * RaceManager.tireMarkHeightStatic);
					rightPoint = curMarkTr.InverseTransformPoint(w.contactPoint.point - pointDir * w.suspensionParent.flippedSideFactor * Mathf.Sign(w.rawRPM) + w.contactPoint.normal * RaceManager.tireMarkHeightStatic);
				}
			}
			else if (creatingMark)
			{
				w.sliding = false;
				EndMark();
			}

			// Update mark if it's short enough, otherwise end it
			if (curEdge < RaceManager.tireMarkLengthStatic && creatingMark)
			{
				UpdateMark();
			}
			else if (creatingMark)
			{
				EndMark();
			}

			// Set particle emission rates
			ParticleSystem.EmissionModule em;
			for (int ps = 0; ps < debrisParticles.Length; ps++)
			{
				if (w.connected)
				{
					if (ps == w.contactPoint.surfaceType)
					{
						if (GroundSurfaceMaster.surfaceTypesStatic[w.contactPoint.surfaceType].leaveSparks && w.popped)
						{
							em = debrisParticles[ps].emission;
							em.rateOverTime = zeroEmission;

							if (sparks)
							{
								em = sparks.emission;
								em.rateOverTime = new ParticleSystem.MinMaxCurve(initialEmissionRates[debrisParticles.Length] *
									Mathf.Clamp01(Mathf.Abs(F.MaxAbs(w.sidewaysSlip, w.forwardSlip, alwaysScrape)) - w.slipThreshold));
							}
						}
						else
						{
							em = debrisParticles[ps].emission;
							em.rateOverTime = new ParticleSystem.MinMaxCurve(initialEmissionRates[ps] *
								Mathf.Clamp01(Mathf.Abs(F.MaxAbs(w.sidewaysSlip, w.forwardSlip, alwaysScrape)) - w.slipThreshold));

							if (sparks)
							{
								em = sparks.emission;
								em.rateOverTime = zeroEmission;
							}
						}
					}
					else
					{
						em = debrisParticles[ps].emission;
						em.rateOverTime = zeroEmission;
					}
				}
				else
				{
					em = debrisParticles[ps].emission;
					em.rateOverTime = zeroEmission;

					if (sparks)
					{
						em = sparks.emission;
						em.rateOverTime = zeroEmission;
					}
				}
			}
		}

		// Start creating a mark
		void StartMark()
		{
			if (!w.contactPoint.col)
				return;
			creatingMark = true;
			curMark = new GameObject("Tire Mark");
			curMarkTr = curMark.transform;
			curMarkTr.parent = w.contactPoint.col.transform;
			curMark.AddComponent<TireMark>();
			MeshRenderer tempRend = curMark.AddComponent<MeshRenderer>();

			// Set material based on whether the tire is popped
			if (w.popped)
			{
				tempRend.sharedMaterial = rimMarkMaterials[Mathf.Min(w.contactPoint.surfaceType, rimMarkMaterials.Length - 1)];
			}
			else
			{
				tempRend.sharedMaterial = tireMarkMaterials[Mathf.Min(w.contactPoint.surfaceType, tireMarkMaterials.Length - 1)];
			}

			tempRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			mesh = curMark.AddComponent<MeshFilter>().mesh;
			verts = new Vector3[RaceManager.tireMarkLengthStatic * 2];
			tris = new int[RaceManager.tireMarkLengthStatic * 3];

			if (continueMark)
			{
				verts[0] = leftPointPrev;
				verts[1] = rightPointPrev;

				tris[0] = 0;
				tris[1] = 3;
				tris[2] = 1;
				tris[3] = 0;
				tris[4] = 2;
				tris[5] = 3;
			}

			uvs = new Vector2[verts.Length];
			uvs[0] = new Vector2(0, 0);
			uvs[1] = new Vector2(1, 0);
			uvs[2] = new Vector2(0, 1);
			uvs[3] = new Vector2(1, 1);

			colors = new Color[verts.Length];
			colors[0].a = 0;
			colors[1].a = 0;

			curEdge = 2;
			gapDelay = RaceManager.tireMarkGapStatic;
		}

		// Update mark currently being generated
		void UpdateMark()
		{
			if (gapDelay == 0)
			{
				float alpha = (curEdge < RaceManager.tireMarkLengthStatic - 2 && curEdge > 5 ? 1 : 0) *
					 UnityEngine.Random.Range(
						  Mathf.Clamp01(F.MaxAbs(w.sidewaysCurveStretch * w.sidewaysSlip, w.forwardCurveStretch * w.forwardSlip, alwaysScrape) - w.slipThreshold),
						  Mathf.Clamp01(F.MaxAbs(w.sidewaysCurveStretch * w.sidewaysSlip, w.forwardCurveStretch * w.forwardSlip, alwaysScrape) - w.slipThreshold));
				gapDelay = RaceManager.tireMarkGapStatic;
				curEdge += 2;

				verts[curEdge] = leftPoint;
				verts[curEdge + 1] = rightPoint;

				for (int i = curEdge + 2; i < verts.Length; i++)
				{
					verts[i] = Mathf.Approximately(i * 0.5f, Mathf.Round(i * 0.5f)) ? leftPoint : rightPoint;
					colors[i].a = 0;
				}

				tris[curEdge * 3 - 3] = curEdge;
				tris[curEdge * 3 - 2] = curEdge + 3;
				tris[curEdge * 3 - 1] = curEdge + 1;
				tris[Mathf.Min(curEdge * 3, tris.Length - 1)] = curEdge;
				tris[Mathf.Min(curEdge * 3 + 1, tris.Length - 1)] = curEdge + 2;
				tris[Mathf.Min(curEdge * 3 + 2, tris.Length - 1)] = curEdge + 3;

				uvs[curEdge] = new Vector2(0, curEdge * 0.5f);
				uvs[curEdge + 1] = new Vector2(1, curEdge * 0.5f);

				colors[curEdge] = new Color(1, 1, 1, alpha);
				colors[curEdge + 1] = colors[curEdge];

				mesh.vertices = verts;
				mesh.triangles = tris;
				mesh.uv = uvs;
				mesh.colors = colors;
				mesh.RecalculateBounds();
				mesh.RecalculateNormals();
			}
			else
			{
				gapDelay = Mathf.Max(0, gapDelay - Time.deltaTime);
				verts[curEdge] = leftPoint;
				verts[curEdge + 1] = rightPoint;

				for (int i = curEdge + 2; i < verts.Length; i++)
				{
					verts[i] = Mathf.Approximately(i * 0.5f, Mathf.Round(i * 0.5f)) ? leftPoint : rightPoint;
					colors[i].a = 0;
				}

				mesh.vertices = verts;
				mesh.RecalculateBounds();
			}

			if (calculateTangents)
			{
				mesh.RecalculateTangents();
			}
		}

		// Stop making mark
		void EndMark()
		{
			creatingMark = false;
			leftPointPrev = verts[Mathf.RoundToInt(verts.Length * 0.5f)];
			rightPointPrev = verts[Mathf.RoundToInt(verts.Length * 0.5f + 1)];
			continueMark = w.grounded;

			curMark.GetComponent<TireMark>().fadeTime = RaceManager.tireFadeTimeStatic;
			curMark.GetComponent<TireMark>().mesh = mesh;
			curMark.GetComponent<TireMark>().colors = colors;
			curMark = null;
			curMarkTr = null;
			mesh = null;
		}

		// Clean up mark if destroyed while creating
		void OnDestroy()
		{
			if (creatingMark && curMark)
			{
				EndMark();
			}
			else if (mesh != null)
			{
				Destroy(mesh);
			}
		}
	}
}