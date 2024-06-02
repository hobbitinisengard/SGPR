using UnityEngine;
using System;

namespace RVP
{
	// Class for engines
	public abstract class Motor : MonoBehaviour
	{
		protected VehicleParent vp;
		[NonSerialized]
		public bool ignition = false;

		[Tooltip("Throttle curve, x-axis = input, y-axis = output")]
		public AnimationCurve inputCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
		protected float actualInput; // Input after applying the input curve

		protected AudioSource engineAudio;
		protected AudioSource idlingEngineAudio;
		[Header("Engine Audio")]

		public float minPitch;
		public float maxPitch;
		public float targetPitch;
		protected float pitchFactor;
		protected float airPitch;
		[Header("Nitrous Boost")]
		public bool canBoost = true;
		public GameObject[] jets;
		public bool boosting;
		
		bool boostReleased;
		public float maxBoost = 0.5f;
		protected AnimationCurve boostPowerCurve = AnimationCurve.EaseInOut(0, 0, 0.5f, 1);
		
		public AudioSource boostLoopSnd;
		public AudioClip boostStart;
		public AudioClip boostEnd;
		public ParticleSystem[] boostParticles;

		[Header("Damage")]

		[Range(0, 1)]
		public float strength = 1;
		[System.NonSerialized]
		public float health = 1;
		public float damagePitchWiggle;
		public ParticleSystem smoke;
		float initialSmokeEmission;
		float baseJetScale = 0;
		float boostVel;
		float sinArg = 0;
		float sinArgSpd = 22f;
		public float jetConsumption;
		public float boostActivatedTime;
		static AnimationCurve idlingEngineAudioCurve = AnimationCurve.Linear(0, .5f, 1, 0);

		public virtual void Start()
		{
			vp = transform.GetTopmostParentComponent<VehicleParent>();
			engineAudio = GetComponent<AudioSource>();
			idlingEngineAudio = transform.GetChild(0).GetComponent<AudioSource>();
			if (engineAudio)
				engineAudio.pitch = minPitch;

			if (smoke)
			{
				initialSmokeEmission = smoke.emission.rateOverTime.constantMax;
			}
			foreach (GameObject jet in jets)
			{
				jet.transform.localScale = Vector3.zero;
			}
		}
		protected virtual void FixedUpdate()
		{
			health = Mathf.Clamp01(health);
			if (canBoost && ignition && vp.accelInput > 0)
			{
				if (((boostReleased && !boosting) || boosting) && vp.boostButton == 1)
				{
					if (boostReleased)
					{
						boostActivatedTime = Time.time;
					}
					boosting = true;
					boostReleased = false;
				}
				else
				{
					boosting = false;
				}
			}
			else
			{
				boosting = false;
			}

			if (vp.boostButton == 0 || vp.accelInput == 0)
			{
				boostReleased = true;
			}

			if (jets != null)
			{
				// boosting visuals
				baseJetScale = Mathf.SmoothDamp(baseJetScale, boosting ? 1f : 0f, ref boostVel,
						 Time.fixedDeltaTime, 1);
				float sine = Mathf.Sin(sinArg);
				if (baseJetScale > 0.01f) // ~0
				{
					boostLoopSnd.volume = Mathf.InverseLerp(0.1f, 0.3f, baseJetScale);
					if (!boostLoopSnd.isPlaying)
						boostLoopSnd.Play();
					sinArg += Time.fixedDeltaTime * sinArgSpd;
					foreach (var jet in jets)
					{
						jet.transform.localScale = (1 + 0.1f * sine) * baseJetScale * Vector3.one;
						jet.SetActive(true);
						jet.GetComponent<MeshRenderer>().material.SetVector("_Offset", new Vector4(0, Mathf.Sin(sinArg / 30f)));
					}
				}
				else
				{
					boostLoopSnd.Stop();
					sinArg = 0;
					foreach (var jet in jets)
					{
						jet.SetActive(false);
					}
				}
			}
		}
		protected virtual void Update()
		{
			// Set engine sound properties
			if (!ignition)
			{
				targetPitch = 0;
			}

			if (engineAudio)
			{
				if (ignition && health > 0)
				{
					engineAudio.enabled = true;					
					engineAudio.pitch = Mathf.LerpUnclamped(engineAudio.pitch, Mathf.LerpUnclamped(minPitch, maxPitch, targetPitch),
						20 * Time.deltaTime) + Mathf.Sin(Time.time * 200 * (1 - health)) * (1 - health) * 0.1f * damagePitchWiggle;
					idlingEngineAudio.pitch = engineAudio.pitch;
					// blend idling engine audio with revving audio
					float blendPoint = 0.4f;
					idlingEngineAudio.volume = idlingEngineAudioCurve.Evaluate(1 / (2*blendPoint) * targetPitch);
					engineAudio.volume = 1 - idlingEngineAudio.volume;
				}
				else
				{
					engineAudio.enabled = false;
				}
			}

			// Play boost particles
			if (boostParticles.Length > 0)
			{
				foreach (ParticleSystem curBoost in boostParticles)
				{
					if (boosting && curBoost.isStopped)
					{
						curBoost.Play();
					}
					else if (!boosting && curBoost.isPlaying)
					{
						curBoost.Stop();
					}
				}
			}

			// Adjusting smoke particles based on damage
			if (smoke)
			{
				ParticleSystem.EmissionModule em = smoke.emission;
				em.rateOverTime = new ParticleSystem.MinMaxCurve(health < 0.7f ? initialSmokeEmission * (1 - health) : 0);
			}
		}
	}
}
