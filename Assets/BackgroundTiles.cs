using UnityEngine;
using UnityEngine.UI;

public class BackgroundTiles : MonoBehaviour
{
	Vector2 tileDimensions;
	static Vector2 movementDir = Vector2.zero;
	public float speed;
	RectTransform rt;
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
		
		speed = Mathf.Sqrt(2) * dim / 3f;
	}
	public void SwitchBackgroundTo(in Sprite sprite)
	{
		GetComponent<Image>().sprite = sprite;
		RandomizeMovement();
	}
	void Update()
	{
		Vector2 pos = rt.anchoredPosition;

		if (Mathf.Abs(pos.x) >= tileDimensions.x)
			pos = Vector2.zero;

		pos += movementDir * speed * Time.deltaTime;
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
