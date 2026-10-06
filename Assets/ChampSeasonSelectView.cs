using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ChampSeasonSelectView : MainMenuView
{
	public Text sponsorDescrText;
	public Text startingMoneyText;
	public RectTransform content;
	public Scrollbar scrollx;
	public GameObject smallCupIcon;
	public bool d_co;
	public Button startButton;
	int selected;
	Coroutine motion;
	Button confirm;
	bool starting;
	protected override void OnEnable()
	{
		base.OnEnable(); starting = false;
		F.I.move2Ref.action.performed += Move;
		F.I.enterRef.action.performed += Confirm;
		confirm = startButton;
		if (!confirm) { Debug.LogError("ChampSeasonSelectView requires a START button reference.", this); return; }
		var scroll = content.GetComponentInParent<ScrollRect>(); if (scroll) { scroll.StopMovement(); scroll.enabled = false; }
		for (int i = 0; i < content.childCount; i++)
		{
			int index = i;
			var icon = content.GetChild(i); var button = icon.GetComponent<Button>() ?? icon.gameObject.AddComponent<Button>();
			button.onClick = new Button.ButtonClickedEvent(); button.onClick.AddListener(() => Select(index));
		}
		Select(Mathf.Clamp(selected, 0, content.childCount - 1));
	}
	protected override void OnDisable()
	{
		F.I.move2Ref.action.performed -= Move; F.I.enterRef.action.performed -= Confirm;
		if (motion != null) StopCoroutine(motion); motion = null;
		base.OnDisable();
	}
	void Move(InputAction.CallbackContext ctx)
	{
		int step = Mathf.RoundToInt(ctx.ReadValue<Vector2>().x);
		if (step != 0) Select(Mathf.Clamp(selected + step, 0, content.childCount - 1));
	}
	void Confirm(InputAction.CallbackContext ctx) => StartSelectedSeason();
	string Season => Championships.NormalizeSeason(content.GetChild(selected).name);
	void Select(int index)
	{
		selected = index;
		string season = Season; bool unlocked = Championships.Unlocked(season);
		if (smallCupIcon) smallCupIcon.SetActive(F.I.HasWonChampionship(season));
		if (sponsorDescrText)
			ChampionshipUI.SetText(sponsorDescrText.gameObject, F.I.LocStr("Champ.Season." + season + ".Description") +
				(unlocked ? "" : "\n\n" + F.I.LocStr("Win all seven national championships to unlock.")) +
				$"\n\n{F.I.LocStr("Funds")}: ${Championships.Current.cash:N0}");
		if (startingMoneyText) ChampionshipUI.SetText(startingMoneyText.gameObject, $"${Championships.Current.cash:N0}");
		confirm.interactable = unlocked;
		confirm.Select();
		for (int i = 0; i < content.childCount; i++)
		{
			var image = content.GetChild(i).GetComponent<Image>();
			if (image) image.color = Championships.Unlocked(Championships.NormalizeSeason(image.name)) ? Color.white : new Color(.4f,.4f,.4f,1);
		}
		if (motion != null) StopCoroutine(motion);
		PlaySFX("fe-bitmapscroll");
		motion = StartCoroutine(Center());
	}
	IEnumerator Center()
	{
		yield return null; Canvas.ForceUpdateCanvases();
		
		var icon = (RectTransform)content.GetChild(selected); var viewport = (RectTransform)content.parent;
		Vector2 start = content.anchoredPosition;
		float iconCenter = icon.anchoredPosition.x + (.5f - icon.pivot.x) * icon.rect.width;
		Vector2 end = new Vector2(viewport.rect.width * .5f - iconCenter, start.y);
		float scrollbarStart = scrollx ? scrollx.value : 0;
		float scrollbarEnd = content.childCount < 2 ? 0 : (float)selected / (content.childCount - 1);
		if (scrollx) scrollx.size = 1f / content.childCount;
		for (float t = 0; t < .5f; t += Time.unscaledDeltaTime)
		{
			float progress = F.EasingOutQuint(t * 2);
			content.anchoredPosition = Vector2.Lerp(start, end, progress);
			if (scrollx) scrollx.SetValueWithoutNotify(Mathf.Lerp(scrollbarStart, scrollbarEnd, progress));
			yield return null;
		}
		content.anchoredPosition = end;
		if (scrollx) scrollx.SetValueWithoutNotify(scrollbarEnd);
		motion = null;
	}
	public void StartSelectedSeason()
	{
		if (starting) return;
		if (Championships.StartSeason(Season)) { starting = true; GoToView(ChampionshipUI.View("ChampionshipsView")); }
		else PlaySFX("fe-warning");
	}
}
