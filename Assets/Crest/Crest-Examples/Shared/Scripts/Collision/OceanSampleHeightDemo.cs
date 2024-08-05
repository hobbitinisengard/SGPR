// Crest Ocean System

// Copyright 2020 Wave Harmonic Ltd

using Crest;
using UnityEngine;

/// <summary>
/// Places the game object on the water surface by moving it vertically.
/// </summary>
public class OceanSampleHeightDemo : MonoBehaviour
{
    SampleHeightHelper _sampleHeightHelper = new SampleHeightHelper();

    void Update()
    {
        // Assume a primitive like a sphere or box.
        var r = transform.lossyScale.magnitude;
        _sampleHeightHelper.Init(transform.position, 2f * r);

        if (_sampleHeightHelper.Sample(out var height))
        {
            var pos = transform.position;
            pos.y = height;
            transform.position = pos;
        }
    }
}
