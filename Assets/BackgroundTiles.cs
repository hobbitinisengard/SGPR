using UnityEngine;

public class BackgroundTiles : MonoBehaviour
{
    Vector2 tileDimensions;
    Vector2 movementDir = Vector2.zero;
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
    void Start()
    {
        var sr = GetComponent<SpriteRenderer>();
        rt = GetComponent<RectTransform>();
        sr.size = new Vector2(Screen.width * 2, Screen.height * 2);
        float dim = sr.sprite.texture.width / sr.sprite.pixelsPerUnit;
        tileDimensions = new Vector2(dim, dim);
        RandomizeMovement();
        speed = Mathf.Sqrt(2) * dim / 3f;
    }
    public void SwitchBackgroundTo(in Sprite sprite)
    {
        GetComponent<SpriteRenderer>().sprite = sprite;
    }
    void Update()
    {
        Vector2 pos = rt.anchoredPosition;

        if (Mathf.Abs(pos.x) >= tileDimensions.x)
            pos = Vector3.zero;
        
        pos += movementDir * speed * Time.deltaTime;
        transform.localPosition = pos;
    }

    void RandomizeMovement()
    {
        movementDir.x = Random.value > 0.5f ? 1 : -1;
        movementDir.y = Random.value > 0.5f ? 1 : -1;
    }
}
