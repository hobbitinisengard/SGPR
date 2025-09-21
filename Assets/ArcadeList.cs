using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ArcadeList : MonoBehaviour
{
	public Transform content;
	public GameObject rowPrefab;
	public MainMenuView thisView;
	public MainMenuView enterNameView;

	private void OnEnable()
	{
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
			newRow.GetComponent<Button>().onClick.AddListener(() => {
				F.I.gameMode = GameMode.Arcade;
				thisView.GoToView(enterNameView);
			});
			newRow.GetChild(0).GetComponent<TextMeshProUGUI>().text = variant.name;
			newRow.GetChild(1).GetComponent<TextMeshProUGUI>().text = variant.nodes.Length + " " + F.I.LocStr("TRACKS");
			newRow.GetChild(2).GetComponent<TextMeshProUGUI>().text = F.I.LocStr("PROGRESS") + ": " + variant.progress.progress.ToString("F0") + "%";
		}
	}
}
