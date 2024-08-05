// Crest Ocean System

// Copyright 2020 Wave Harmonic Ltd

using UnityEngine;

public class SubmarineParticles : MonoBehaviour
{
    private Submarine _submarine;
    private ParticleSystem _particleSystem;

    private void Awake()
    {
        _submarine = GetComponentInParent<Submarine>();
        _particleSystem = GetComponent<ParticleSystem>();
    }

    private void Update()
    {
        if ((_particleSystem != null) || (_submarine != null))
        {
            float subSpeed = Mathf.Clamp(_submarine._submarineSpeed, 0f, 1f);
            var emission = _particleSystem.emission;
            emission.rateOverTime = Mathf.Lerp(0, 200, subSpeed);
        }
    }
}
