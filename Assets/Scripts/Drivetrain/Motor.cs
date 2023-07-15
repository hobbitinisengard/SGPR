using UnityEngine;
using System.Collections;

namespace RVP
{
    // Class for engines
    public abstract class Motor : MonoBehaviour
    {
        protected VehicleParent vp;
        public bool ignition;

        [Tooltip("Throttle curve, x-axis = input, y-axis = output")]
        public AnimationCurve inputCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        protected float actualInput; // Input after applying the input curve

        protected AudioSource snd;

        [Header("Engine Audio")]

        public float minPitch;
        public float maxPitch;
        [System.NonSerialized]
        public float targetPitch;
        protected float pitchFactor;
        protected float airPitch;

        [Header("Nitrous Boost")]

        public bool canBoost = true;
        public GameObject[] jets;
        public bool boosting;
        public float battery = 1;
        bool boostReleased;
        bool boostPrev;
        public float maxBoost = 0.5f;
        protected AnimationCurve boostPowerCurve = AnimationCurve.EaseInOut(0, 0, 0.5f, 1);
        public float maxBattery = 1;
        public float boostBurnRate = 0.01f;
        public AudioSource boostLoopSnd;
        AudioSource boostSnd; // AudioSource for boostStart and boostEnd
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
        float SinArg = 0;
        float SinJetCoeff = 11f;
        public float boostActivatedTime;

        public virtual void Start()
        {
            vp = transform.GetTopmostParentComponent<VehicleParent>();
            // Get engine sound
            snd = GetComponent<AudioSource>();
            if (snd)
            {
                snd.pitch = minPitch;
            }

            // Get boost sound
            if (boostLoopSnd)
            {
                GameObject newBoost = Instantiate(boostLoopSnd.gameObject, boostLoopSnd.transform.position, boostLoopSnd.transform.rotation) as GameObject;
                boostSnd = newBoost.GetComponent<AudioSource>();
                boostSnd.transform.parent = boostLoopSnd.transform;
                boostSnd.transform.localPosition = Vector3.zero;
                boostSnd.transform.localRotation = Quaternion.identity;
                boostSnd.loop = false;
            }

            if (smoke)
            {
                initialSmokeEmission = smoke.emission.rateOverTime.constantMax;
            }
            foreach (GameObject jet in jets)
            {
                jet.transform.localScale = Vector3.zero;
            }
        }
        public virtual void FixedUpdate() {
            health = Mathf.Clamp01(health);
            // Boost logic
            battery = Mathf.Clamp(boosting ? battery - boostBurnRate * Time.timeScale * 0.05f * TimeMaster.inverseFixedTimeFactor : battery, 0, maxBattery);
            boostPrev = boosting;

            if (canBoost && ignition && health > 0 && battery > 0 && 
                (vp.hover ? vp.accelInput != 0 || Mathf.Abs(vp.localVelocity.z) > 1 : vp.accelInput > 0)) 
            {
                if (((boostReleased && !boosting) || boosting) && vp.boostButton) {
                    if(boostReleased)
                    {
                        boostActivatedTime = Time.time;
                    }
                    boosting = true;
                    boostReleased = false;
                }
                else {
                    boosting = false;
                }
            }
            else {
                boosting = false;
            }

            if (!vp.boostButton) {
                boostReleased = true;
            }
            
            if (boostLoopSnd && boostSnd) {
                if (boosting && !boostLoopSnd.isPlaying) {
                    boostLoopSnd.Play();
                }
                else if (!boosting && boostLoopSnd.isPlaying) {
                    boostLoopSnd.Stop();
                }
                if (boosting && !boostPrev) {
                    boostSnd.clip = boostStart;
                    boostSnd.Play();
                }
                else if (!boosting && boostPrev) {
                    boostSnd.clip = boostEnd;
                    boostSnd.Play();
                }
            }

            if (jets != null)
            {
                // boosting visuals
                foreach (GameObject jet in jets)
                {
                    baseJetScale = Mathf.SmoothDamp(baseJetScale, boosting ? 1f : 0f, ref boostVel,
                        0.1f, 0.3f);
                    float sine = Mathf.Sin(SinArg);
                    jet.transform.localScale = (1 + 0.1f * sine) * baseJetScale * Vector3.one;

                    if (jet.transform.localScale.x > 0.01f) // ~0
                    {
                        jet.SetActive(true);
                        SinArg += Time.fixedDeltaTime * SinJetCoeff;
                        jet.GetComponent<MeshRenderer>().material.SetVector("_Offset", new Vector4(0, Mathf.Sin(SinArg/30f)));
                    }
                    else
                    {
                        SinArg = 0;
                        jet.SetActive(false);
                    }
                }
            }
        }

        public virtual void Update() {
            // Set engine sound properties
            if (!ignition) {
                targetPitch = 0;
            }

            if (snd) {
                if (ignition && health > 0) {
                    snd.enabled = true;
                    snd.pitch = Mathf.Lerp(snd.pitch, Mathf.Lerp(minPitch, maxPitch, targetPitch), 20 * Time.deltaTime) + Mathf.Sin(Time.time * 200 * (1 - health)) * (1 - health) * 0.1f * damagePitchWiggle;
                    snd.volume = Mathf.Lerp(snd.volume, 0.3f + targetPitch * 0.7f, 20 * Time.deltaTime);
                }
                else {
                    snd.enabled = false;
                }
            }

            // Play boost particles
            if (boostParticles.Length > 0) {
                foreach (ParticleSystem curBoost in boostParticles) {
                    if (boosting && curBoost.isStopped) {
                        curBoost.Play();
                    }
                    else if (!boosting && curBoost.isPlaying) {
                        curBoost.Stop();
                    }
                }
            }

            // Adjusting smoke particles based on damage
            if (smoke) {
                ParticleSystem.EmissionModule em = smoke.emission;
                em.rateOverTime = new ParticleSystem.MinMaxCurve(health < 0.7f ? initialSmokeEmission * (1 - health) : 0);
            }
        }
    }
}
