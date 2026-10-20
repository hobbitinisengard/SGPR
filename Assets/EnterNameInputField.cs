using UnityEngine;
using UnityEngine.UI;

public class EnterNameInputField : Sfxable
{
	KeyCode[] allowedKeys = { 
		KeyCode.Space, KeyCode.A, KeyCode.B, KeyCode.C, KeyCode.D, KeyCode.E, KeyCode.F, KeyCode.G, KeyCode.H, 
		KeyCode.I, KeyCode.J, KeyCode.K, KeyCode.L, KeyCode.P, KeyCode.O, KeyCode.N, KeyCode.M, KeyCode.Q, KeyCode.R, 
		KeyCode.S, KeyCode.T, KeyCode.U, KeyCode.V, KeyCode.W, KeyCode.X, KeyCode.Y, KeyCode.Z, 
		KeyCode.Equals, KeyCode.Colon, KeyCode.LeftBracket, KeyCode.RightBracket 
	};
	public Sprite[] allowedKeysSprites;
	public GameObject letterTemplate;
	public Transform selector;
	public Sprite delSprite;
	public Sprite endSprite;
	public SlideInOut OKButton;
	GridLayoutGroup glg;
	int len = 0;
	TouchScreenKeyboard screenKeyboard;
	string lastKeyboardText;
	void OnEnable()
	{
        if (!MobileControls.Enabled) return;
        // The whole name field is tappable, including empty space between letters.
        var graphic = GetComponent<Graphic>();
        if (!graphic)
        {
            var hitArea = gameObject.AddComponent<Image>();
            hitArea.color = Color.clear;
            graphic = hitArea;
        }
        graphic.raycastTarget = true;
        MobileTapTarget.Bind(gameObject, OpenScreenKeyboard);
	}
	void OpenScreenKeyboard()
	{
		if (!MobileControls.Enabled || !isActiveAndEnabled || (screenKeyboard != null && screenKeyboard.active)) return;
		lastKeyboardText = GetInputField();
		screenKeyboard = TouchScreenKeyboard.Open(lastKeyboardText, TouchScreenKeyboardType.Default, false, false, false, false, "Name");
	}
	void OnDisable()
	{
		if (screenKeyboard != null) screenKeyboard.active = false;
		screenKeyboard = null;
	}
	private void Start()
	{
		glg = GetComponent<GridLayoutGroup>();
		if (SplitScreenSelector.I && SplitScreenSelector.I.RegistrationActive) return;
		// write F.I.s_playerName for the first time
		foreach(char c in F.I.playerData.playerName)
		{
			for(int i = 0; i<allowedKeys.Length; i++)
			{
				if ((char)allowedKeys[i] == char.ToLowerInvariant(c))
				{
					PlaceLetter(i);
					break;
				}
			}
		}
	}
	public string GetInputField()
	{
		string result = "";
		foreach (Transform child in transform)
			if (child.gameObject != letterTemplate && child != selector && child.gameObject.activeSelf) result += child.name;
		return result;
	}
	public void SetInputField(string value)
	{
		if (!glg) glg = GetComponent<GridLayoutGroup>();
		foreach (Transform child in transform)
			if (child.gameObject != letterTemplate && child != selector)
			{ child.gameObject.SetActive(false); Destroy(child.gameObject); }
		len = 0;
		if (OKButton) OKButton.gameObject.SetActive(false);
		selector.GetComponent<Image>().sprite = allowedKeysSprites[0];
		foreach (char character in value ?? "")
		{
			if (len >= glg.constraintCount - 1) break;
			for (int i = 0; i < allowedKeys.Length; i++)
				if ((char)allowedKeys[i] == char.ToLowerInvariant(character)) { PlaceLetter(i); break; }
		}
	}
	public void SetName()
	{
		if (SplitScreenSelector.I && SplitScreenSelector.I.RegistrationActive) return;
		string driverName = GetInputField().Trim();
		if (string.IsNullOrWhiteSpace(driverName)) return;
		if (F.I.gameMode == GameMode.Championships)
		{
			if (Championships.Current == null) Championships.Create(driverName);
			else Championships.Current.driverName = driverName;
		}
		else F.I.playerData.playerName = driverName;
	}
	void Update()
	{
		if (MobileControls.Enabled && screenKeyboard != null)
		{
			if (screenKeyboard.text != lastKeyboardText)
			{
				SetInputField(screenKeyboard.text);
				lastKeyboardText = screenKeyboard.text;
			}
			return;
		}
		if (SplitScreenSelector.I && SplitScreenSelector.I.IgnoreNameTypingThisFrame) return;
		if(Input.GetKeyDown(KeyCode.Backspace) && len > 0)
		{
			PlaySFX("fe-keydeleted");
			if (len == 3)
			{
				OKButton.gameObject.SetActive(true);
				OKButton.PlaySlideOut(true);
			}
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
					PlaySFX("fe-keypressed");
					PlaceLetter(i);
				}
			}
		}
	}
	void PlaceLetter(int i)
	{
		var newletter = Instantiate(letterTemplate, transform);
		newletter.GetComponent<Image>().sprite = allowedKeysSprites[i];
		newletter.SetActive(true);
		newletter.name = KeyCodeToString(allowedKeys[i]);
		selector.SetAsLastSibling();
		len++;
		selector.GetComponent<Image>().sprite = (len >= 3) ? endSprite : allowedKeysSprites[0];
		if (len >= 3)
		{
			OKButton.transform.gameObject.SetActive(true);
			OKButton.transform.GetComponent<Button>().Select();
		}

		static string KeyCodeToString(KeyCode key)
		{
			return key switch
			{
				KeyCode.Space => " ",
				KeyCode.Equals => "=",
				KeyCode.Colon => ":",
				KeyCode.LeftBracket => "[",
				KeyCode.RightBracket => "]",

				_ => key.ToString()
			};
		}
	}
	//private void OnEnable()
	//{
	//	len = 0;
	//	OKButton.transform.gameObject.SetActive(false);
	//	selector.gameObject.SetActive(false);
	//	for(int i=0; i<transform.childCount; ++i)
	//	{
	//		if (transform.GetChild(i).gameObject.activeSelf)
	//			Destroy(transform.GetChild(i).gameObject);
	//	}
	//	selector.gameObject.SetActive(true);
	//}
	
}
