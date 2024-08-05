// Crest Ocean System for HDRP

// Copyright 2020 Wave Harmonic Ltd

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Base class for scripts that provide the time to the ocean system. See derived classes for examples.
    /// </summary>
    public interface ITimeProvider
    {
        float CurrentTime { get; }
        float DeltaTime { get; }

        // Delta time used for dynamics such as the ripple sim
        float DeltaTimeDynamics { get; }
    }

    public abstract class TimeProviderBase : MonoBehaviour, ITimeProvider
    {
        public abstract float CurrentTime { get; }
        public abstract float DeltaTime { get; }
        public abstract float DeltaTimeDynamics { get; }
    }
}
