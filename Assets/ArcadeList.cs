using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ArcadeList : MainMenuView
{
	public Transform content;
	public GameObject rowPrefab;
	public MainMenuView enterNameView;
	private new void OnEnable()
	{
		base.OnEnable();
		content.DestroyAllChildren();
		Refresh();
	}
	public void Refresh()
	{
		content.DestroyAllChildren();

		foreach (var variant in F.I.arcadeVariants)
		{
			var newRow = Instantiate(rowPrefab, content).transform;
			newRow.name = variant.name;
			if(newRow.name == F.I.curVariant.name)
			{ // highlight current variant
				//newRow.GetComponent<Image>().color = new Color(1, 1, 0, 0.5f);
				newRow.GetComponent<Button>().Select();
				firstButtonToBeSelected = newRow.GetComponent<Button>();
			}
			newRow.GetComponent<Button>().onClick.AddListener(() => {
				if (EventSystem.current.currentSelectedGameObject.name != F.I.curVariant.name)
				{
					F.I.SwitchArcadeVariant(EventSystem.current.currentSelectedGameObject.name);
				}
				F.I.arcadeSelector.Reset();
				GoToView(enterNameView);
			});
			newRow.GetChild(0).GetComponent<TextMeshProUGUI>().text = variant.name;
			newRow.GetChild(1).GetComponent<TextMeshProUGUI>().text = variant.nodes.Length + " " + F.I.LocStr("TRACKS");
			newRow.GetChild(2).GetComponent<TextMeshProUGUI>().text = F.I.LocStr("PROGRESS") + ": " + variant.progress.OverallProgress().ToString("F0") + "%";
		}
	}
}
