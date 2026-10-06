using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChampSlotSelect : MainMenuView
{
	public Transform content;
	public GameObject rowPrefab;
	public MainMenuView enterNameView;
	protected override void OnEnable()
	{
		base.OnEnable();
		StartCoroutine(LoadSlots());
	}
	System.Collections.IEnumerator LoadSlots()
	{
		while (F.I.cars == null || System.Array.Exists(F.I.cars, c => c.config == null)) yield return null;
		Refresh();
	}

	public void Refresh()
	{
		foreach (Transform child in content) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
		for (int i = 0; i < 8; i++)
		{
			int slot = i;
			var save = Championships.ReadSlot(slot);
			var row = Instantiate(rowPrefab, content); row.name = "Slot" + (slot + 1);
			var button = row.GetComponent<Button>(); button.onClick = new Button.ButtonClickedEvent(); button.onClick.AddListener(() => SelectSlot(slot));
			var texts = row.GetComponentsInChildren<TMP_Text>(true);
			int points = save?.standings.Find(entry => entry.sponsor == save.sponsor)?.points ?? 0;
			if (texts.Length > 0) ChampionshipUI.SetText(texts[0].gameObject, save == null ? $"{slot + 1}. {F.I.LocStr("Empty slot")}" :
				$"{slot + 1}. {save.driverName} ({save.sponsor})   {save.season}   ${save.cash:N0}   {F.I.LocStr("Points")}: {points}   {Mathf.Min(save.nextRace+1,save.tracks.Length)}/{save.tracks.Length}");
			if (texts.Length > 1) ChampionshipUI.SetText(texts[1].gameObject, save?.season ?? "");
			if (texts.Length > 2) ChampionshipUI.SetText(texts[2].gameObject, save == null ? "" : $"{save.nextRace}/{save.tracks.Length}   ${save.cash:N0}");
			if (slot == 0) { firstButtonToBeSelected = button; button.Select(); }
		}
	}
	public void SelectSlot(int slot)
	{
		Championships.OpenSlot(slot);
		if (Championships.Current == null) GoToView(enterNameView);
		else GoToView(ChampionshipUI.View(Championships.Current.season == null || Championships.Current.Completed ? "ChampSeasonSelection" : "ChampionshipsView"));
	}
}
