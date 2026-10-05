using UnityEngine;
using UnityEngine.UI;

public class BackgroundTiles : MonoBehaviour
{
	Vector2 tileDimensions;
	static Vector2 movementDir = Vector2.zero;
	float baseSpeed;
	public float speed = 1;
	RectTransform rt;
	Image tuningBase;
	Image tuningGreen;
	Image tuningRed;
	public Sprite tileBlack;
	public Sprite tileDarkGreen;
	public Sprite tileNavy;
	public Sprite tileBrown;
	public Sprite tileGray;
	public Sprite tileGreen;
	public Sprite tileLightBlue;
	public Sprite tileOrange;
	public Sprite tileDarkRed;
	public Sprite tileDarkBlue;
	public Sprite tileDarkPurple;
	public Sprite tileWhite;
	public Sprite tileRed;
	public Sprite tileSky;
	public Sprite tilePurple;
	void Awake()
	{
		var img = GetComponent<Image>();
		rt = GetComponent<RectTransform>();
		float dim = img.sprite.texture.width / img.sprite.pixelsPerUnit;
		tileDimensions = new Vector2(dim, dim);

		if(movementDir == Vector2.zero)
			RandomizeMovement();
		
		baseSpeed = Mathf.Sqrt(2) * dim / 3f;
	}
	private void OnEnable()
	{
		rt.anchoredPosition = Vector2.zero;
	}
	// -1: tile01, 0: brown, +1: tile21. Layers share this transform so
	// their tiled patterns stay aligned while the background moves.
	public void SetTuningBlend(float position)
	{
		if (!tuningBase)
		{
			tuningBase = GetComponent<Image>();
			tuningBase.sprite = tileBrown;
			tuningGreen = CreateTuningLayer("Tuning green", tileDarkGreen);
			tuningRed = CreateTuningLayer("Tuning red", tileDarkRed);
		}
		position = Mathf.Clamp(position, -1, 1);
		SetLayerOpacity(tuningGreen, Mathf.Max(0, -position));
		SetLayerOpacity(tuningRed, Mathf.Max(0, position));
	}
	Image CreateTuningLayer(string layerName, Sprite sprite)
	{
		GameObject layer = new GameObject(layerName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
		layer.layer = gameObject.layer;
		RectTransform rect = (RectTransform)layer.transform;
		rect.SetParent(transform, false);
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.offsetMin = rect.offsetMax = Vector2.zero;
		Image image = layer.GetComponent<Image>();
		image.sprite = sprite;
		image.type = tuningBase.type;
		image.material = tuningBase.material;
		image.pixelsPerUnitMultiplier = tuningBase.pixelsPerUnitMultiplier;
		image.preserveAspect = tuningBase.preserveAspect;
		image.maskable = tuningBase.maskable;
		image.raycastTarget = false;
		image.enabled = false;
		return image;
	}
	void SetLayerOpacity(Image image, float opacity)
	{
		Color color = tuningBase.color;
		color.a *= opacity;
		image.color = color;
		image.enabled = opacity > 0;
	}
	public void SwitchBackgroundTo(in Sprite sprite)
	{
		if (tuningGreen) tuningGreen.enabled = false;
		if (tuningRed) tuningRed.enabled = false;
		GetComponent<Image>().sprite = sprite;
		RandomizeMovement();
	}
	void Update()
	{
		Vector2 pos = rt.anchoredPosition;

		if (Mathf.Abs(pos.x) >= tileDimensions.x)
			pos = Vector2.zero;

		pos += movementDir * speed * baseSpeed * Time.deltaTime;
		transform.localPosition = pos;
	}
	public void RandomizeMovement()
	{
		movementDir.x = Random.value > 0.5f ? 1 : -1;
		movementDir.y = Random.value > 0.5f ? 1 : -1;
		if(rt)
			rt.anchoredPosition = Vector2.zero;
	}
}
