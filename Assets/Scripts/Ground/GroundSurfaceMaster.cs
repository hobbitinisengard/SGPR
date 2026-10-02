using UnityEngine;
using System;

namespace RVP
{
	[DisallowMultipleComponent]
	[AddComponentMenu("RVP/Ground Surface/Ground Surface Master", 0)]

	// Class managing surface types
	public class GroundSurfaceMaster : MonoBehaviour
	{

		public GroundSurface[] surfaceTypes;
		public static GroundSurface[] surfaceTypesStatic;

		void Awake()
		{
			surfaceTypesStatic = surfaceTypes;
		}

		public static int FindSurfaceTypeIndex(string surfaceName, int fallbackIndex = 0)
		{
			GroundSurface[] availableTypes = surfaceTypesStatic;
			if (availableTypes == null || availableTypes.Length == 0)
				return 0;

			for (int i = 0; i < availableTypes.Length; i++)
				if (availableTypes[i] != null && string.Equals(availableTypes[i].name,
					surfaceName, StringComparison.OrdinalIgnoreCase))
					return i;

			return Mathf.Clamp(fallbackIndex, 0, availableTypes.Length - 1);
		}
	}

	// Class for individual surface types
	[System.Serializable]
	public class GroundSurface
	{
		public string name = "Surface";
		public bool useColliderFriction;
		public float friction;
		[Header("Original Stunt GP contact")]
		[Tooltip("Source tire grip scalar. Leave below zero to use the verified source asphalt profile.")]
		public float sourceGrip = -1;
		[Tooltip("Raw source PMD material flags. The low nibble identifies the original contact type; -1 uses the source asphalt type.")]
		[Range(-1, 255)]
		public int sourceFlags = -1;
		[Tooltip("Raw source rolling-resistance byte. Leave below zero to use the verified source asphalt profile.")]
		public float sourceRolling = -1;
		[Tooltip("Source track-material roughness byte (0-255), used by the original suspension noise.")]
		public int sourceRoughness = -1;
		[Tooltip("Always leave tire marks")]
		public bool alwaysScrape;
		[Tooltip("Rims leave sparks on this surface")]
		public bool leaveSparks;
		public AudioClip tireSnd;
		public AudioClip roadNoise;
	}
}
