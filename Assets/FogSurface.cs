using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public class FogSurface : MonoBehaviour
{
	public Color fogColor = new Color(0.58f, 0.60f, 0.62f, 1f);
	[Range(0f, 1f)] public float opacity = 0.4f;
	[Range(0f, 1f)] public float horizonOpacity = 0.95f;
	[Min(0.01f)] public float intersectionFade = 1.5f;
	[Min(100f)] public float radius = 10000f;
	[Min(1f)] public float patternSize = 80f;
	public Vector2 driftSpeed = new Vector2(0.4f, 0.15f);
	public bool followCamera = true;

	MeshRenderer surfaceRenderer;
	MaterialPropertyBlock properties;
	static readonly int ColorId = Shader.PropertyToID("_Color");
	static readonly int OpacityId = Shader.PropertyToID("_Opacity");
	static readonly int HorizonId = Shader.PropertyToID("_HorizonOpacity");
	static readonly int FadeId = Shader.PropertyToID("_IntersectionFade");
	static readonly int PatternId = Shader.PropertyToID("_PatternSize");
	static readonly int DriftId = Shader.PropertyToID("_DriftSpeed");

	void OnEnable()
	{
		ApplyAppearance();
		Camera.onPreCull += PrepareCamera;
	}

	void OnDisable() => Camera.onPreCull -= PrepareCamera;
	void OnValidate() => ApplyAppearance();

	void ApplyAppearance()
	{
		if (!surfaceRenderer) surfaceRenderer = GetComponent<MeshRenderer>();
		if (!surfaceRenderer) return;
		if (properties == null) properties = new MaterialPropertyBlock();
		properties.SetColor(ColorId, fogColor);
		properties.SetFloat(OpacityId, Mathf.Clamp01(opacity));
		properties.SetFloat(HorizonId, Mathf.Clamp01(horizonOpacity));
		properties.SetFloat(FadeId, Mathf.Max(0.01f, intersectionFade));
		properties.SetFloat(PatternId, Mathf.Max(1f, patternSize));
		properties.SetVector(DriftId, new Vector4(driftSpeed.x, driftSpeed.y, 0f, 0f));
		surfaceRenderer.SetPropertyBlock(properties);
		surfaceRenderer.shadowCastingMode = ShadowCastingMode.Off;
		surfaceRenderer.receiveShadows = false;
		surfaceRenderer.lightProbeUsage = LightProbeUsage.Off;
		surfaceRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
		// Unity's built-in plane has a width of ten metres.
		transform.localScale = new Vector3(Mathf.Max(100f, radius) / 5f, 1f, Mathf.Max(100f, radius) / 5f);
	}

	void PrepareCamera(Camera camera)
	{
		if (!camera || camera.cameraType == CameraType.Preview || camera.cameraType == CameraType.Reflection) return;
		camera.depthTextureMode |= DepthTextureMode.Depth;
		if (!Application.isPlaying || !followCamera || camera.cameraType != CameraType.Game) return;
		Vector3 position = transform.position;
		position.x = camera.transform.position.x;
		position.z = camera.transform.position.z;
		transform.position = position;
		float visibleRadius = Mathf.Max(radius, camera.farClipPlane * 2f);
		transform.localScale = new Vector3(visibleRadius / 5f, 1f, visibleRadius / 5f);
	}
}
