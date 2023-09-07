using UnityEngine;
using UnityEngine.UI;

public class EnterNameInputField : MonoBehaviour
{
	KeyCode[] allowedKeys = { KeyCode.Space, KeyCode.A, KeyCode.B, KeyCode.C, KeyCode.D, KeyCode.E, KeyCode.F, KeyCode.G, KeyCode.H, KeyCode.I, KeyCode.J, KeyCode.K, KeyCode.L, KeyCode.P, KeyCode.O, KeyCode.N, KeyCode.M, KeyCode.Q, KeyCode.R, KeyCode.S, KeyCode.T, KeyCode.U, KeyCode.V, KeyCode.W, KeyCode.X, KeyCode.Y, KeyCode.Z, KeyCode.Equals, KeyCode.Colon, KeyCode.LeftBracket, KeyCode.RightBracket };
	public Sprite[] allowedKeysSprites;
	public GameObject letterTemplate;
	public Transform selector;
	public Sprite delSprite;
	public Sprite endSprite;
	public SlideInOut OKButton;
	GridLayoutGroup glg;
	int len = 0;
	private void Start()
	{
		glg = GetComponent<GridLayoutGroup>();
	}
	void Update()
	{
		if(Input.GetKeyDown(KeyCode.Backspace) && len > 0)
		{
			if (len == 3)
				OKButton.PlaySlideOut(true);
			Destroy(transform.GetChild(transform.childCount - 2).gameObject);
			selector.GetComponent<Image>().sprite = delSprite;
			len--;
			return;
		}
		if(len < glg.constraintCount-1)
		{
			for (int i = 0; i < allowedKeys.Length; ++i)
			{
				if (Input.GetKeyDown(allowedKeys[i]))
				{
					var newletter = Instantiate(letterTemplate, transform);
					newletter.GetComponent<Image>().sprite = allowedKeysSprites[i];
					newletter.SetActive(true);
					selector.SetAsLastSibling();
					len++;
					selector.GetComponent<Image>().sprite = (len >= 3) ? endSprite : allowedKeysSprites[0];
					if (len == 3)
					{
						OKButton.transform.gameObject.SetActive(true);
						OKButton.transform.GetComponent<Button>().Select();
					}
				}
			}
		}
	}
	private void OnEnable()
	{
		len = 0;
		OKButton.transform.gameObject.SetActive(false);
		selector.gameObject.SetActive(false);
		for(int i=0; i<transform.childCount; ++i)
		{
			if (transform.GetChild(i).gameObject.activeSelf)
				Destroy(transform.GetChild(i).gameObject);
		}
		selector.gameObject.SetActive(true);
	}
	
}
