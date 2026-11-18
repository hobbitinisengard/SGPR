using UnityEngine;
using System;

namespace RVP
{
	// Class for engines
	public abstract class Motor : MonoBehaviour
	{
		protected VehicleParent vp;
		[NonSerialized]
		public bool ignition = true;

		[Tooltip("Throttle curve, x-axis = input, y-axis = output")]
		public AnimationCurve inputCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
		protected float actualInput; // Input after applying the input curve

		protected AudioSource engineAudio;
		protected AudioSource idlingEngineAudio;
		int selectedAudioPartIndex = -1;
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
		const float sinArgSpd = 23f;
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
			if (jets != null)
				foreach (GameObject jet in jets)
					if (jet)
						jet.transform.localScale = Vector3.zero;
		}
		public void SetPartAudio(int partIndex)
		{
			if (partIndex <= 0 || selectedAudioPartIndex == partIndex) return;
			selectedAudioPartIndex = partIndex;
			if (!engineAudio) engineAudio = GetComponent<AudioSource>();
			if (!idlingEngineAudio && transform.childCount > 0)
				idlingEngineAudio = transform.GetChild(0).GetComponent<AudioSource>();
			AudioClip rev = Resources.Load<AudioClip>("sfx/engine/engine" + partIndex);
			AudioClip idle = Resources.Load<AudioClip>("sfx/engine/engine" + partIndex + "i");
			if (!rev || !idle)
			{
				// The available collection ends at engine4; keep a deterministic
				// audible fallback until an engine5/engine5i pair is provided.
				Debug.LogWarning($"[Motor] Missing engine{partIndex}/engine{partIndex}i audio; using engine4/engine4i.", this);
				rev = Resources.Load<AudioClip>("sfx/engine/engine4");
				idle = Resources.Load<AudioClip>("sfx/engine/engine4i");
			}
			SetLoopClip(engineAudio, rev);
			SetLoopClip(idlingEngineAudio, idle);
		}
		// Source sound IDs are independent of tuning-part numbers. Share existing
		// recordings where identical; keep distinct retail recordings by source name.
		static readonly string[] stockRevClips =
			{ "petes-revving", "karls-revving", "engine1", "stefans-revving", "engine3-revving" };
		static readonly string[] stockIdleClips =
			{ "engine2i", "engine3i", "engine1i", "engine4i", "engine0i" };

		public void SetStockAudio(int revIndex, int idleIndex)
		{
			if (revIndex < 0 || revIndex > 4 || idleIndex < 0 || idleIndex > 4) return;
			int selection = -2 - revIndex * 5 - idleIndex;
			if (selectedAudioPartIndex == selection) return;
			if (!engineAudio) engineAudio = GetComponent<AudioSource>();
			if (!idlingEngineAudio && transform.childCount > 0)
				idlingEngineAudio = transform.GetChild(0).GetComponent<AudioSource>();
			// Stock selection zero keeps the car's own sound IDs. They are
			// independent indices into the retail rev and idle sample tables.
			AudioClip rev = Resources.Load<AudioClip>("sfx/engine/" + stockRevClips[revIndex]);
			AudioClip idle = Resources.Load<AudioClip>("sfx/engine/" + stockIdleClips[idleIndex]);
			if (!rev || !idle) return;
			selectedAudioPartIndex = selection;
			SetLoopClip(engineAudio, rev);
			SetLoopClip(idlingEngineAudio, idle);
		}

		static void SetLoopClip(AudioSource source, AudioClip clip)
		{
			if (!source || !clip || source.clip == clip) return;
			bool resume = source.isPlaying || source.playOnAwake;
			source.Stop();
			source.clip = clip;
			source.loop = true;
			if (resume && source.isActiveAndEnabled) source.Play();
		}
		protected virtual void FixedUpdate()
		{
			UpdateMotorAudio();
			health = Mathf.Clamp01(health);

			if (canBoost && ignition && vp.ebrakeInput == 0)
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
				boostReleased = true;

			UpdateBoostPresentation();
		}

		static readonly System.Collections.Generic.Dictionary<AudioClip, AudioClip> sourcePitchClips = new();
		static void SetSourcePlaybackFrequency(AudioSource source, float frequency)
		{
			if (!source || !source.clip) return;
			AudioClip clip = source.clip;
			// Unity limits pitch to 3. The original reaches 81900 Hz even for
			// 22050-Hz samples. Copy their unchanged PCM to a higher-rate clip so
			// that the entire original range fits, without resampling the sound.
			if (clip.frequency * 3 < 81900)
			{
				if (!sourcePitchClips.TryGetValue(clip, out AudioClip playbackClip))
				{
					float[] samples = new float[clip.samples * clip.channels];
					if (clip.GetData(samples, 0))
					{
						playbackClip = AudioClip.Create(clip.name + " (source pitch)", clip.samples,
							clip.channels, 44100, false);
						playbackClip.SetData(samples, 0);
					}
					sourcePitchClips[clip] = playbackClip;
				}
				if (playbackClip)
				{
					bool playing = source.isPlaying;
					source.clip = playbackClip;
					if (playing) source.Play();
					clip = playbackClip;
				}
			}
			source.pitch = Mathf.Clamp(frequency / clip.frequency, 0, 3);
		}

		protected void UpdateMotorAudio(float? sourceRpm = null)
		{
			if (engineAudio)
			{
				if (health > 0)
				{
					if (sourceRpm.HasValue)
					{
						float rpm = sourceRpm.Value;
						if (float.IsNaN(rpm) || float.IsInfinity(rpm)) rpm = 0;
						// vehicle_audio_engine: absolute RPM maps directly to playback Hz,
						// independently of the engine's limiter and HUD needle range.
						float frequency = Mathf.Min(81900f, Mathf.Abs(rpm) * 4f + 11025f);
						if (vp && vp.IsLocalHuman && rpm < 4096f)
							frequency = Mathf.Min(81900f, frequency + 14000f);
						SetSourcePlaybackFrequency(engineAudio, frequency);
						SetSourcePlaybackFrequency(idlingEngineAudio, frequency);
					}
					else
					{
						engineAudio.pitch = Mathf.LerpUnclamped(engineAudio.pitch, Mathf.LerpUnclamped(minPitch, maxPitch, targetPitch),
							20 * Time.fixedDeltaTime) + Mathf.Sin(Time.time * 200 * (1 - health)) * (1 - health) * 0.1f * damagePitchWiggle;
						if (idlingEngineAudio) idlingEngineAudio.pitch = engineAudio.pitch;
					}
					// blend idling engine audio with revving audio
					float blendPoint = 0.4f;
					if (idlingEngineAudio)
						idlingEngineAudio.volume = idlingEngineAudioCurve.Evaluate(1 / (2 * blendPoint) * targetPitch);
					engineAudio.volume = idlingEngineAudio ? 1 - idlingEngineAudio.volume : 1;
				}
			}
		}

		protected void UpdateBoostPresentation()
		{
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
						jet.GetComponent<MeshRenderer>().material.SetVector("_GlobalXYTilingXYZWOffsetXY", new Vector4(1,1,0, Mathf.Sin(sinArg / 30f)));
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
			

			// Play boost particles
			if (boostParticles != null && boostParticles.Length > 0)
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
