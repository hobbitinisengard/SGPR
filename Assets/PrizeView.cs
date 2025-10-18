using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PrizeView : MainMenuView
{
	public GameObject UnlockPrefab;
	public WinnersView winnersView;
	public Transform unlocksParent;
	public GameObject OkButton;
	int currentUnlockIndex = 0;
	bool continuationCheck = false;
	Coroutine moveCo;
	Vector2 unlockparentBeginPos;
	protected override void Awake()
	{
		unlockparentBeginPos = unlocksParent.GetComponent<RectTransform>().anchoredPosition;
		base.Awake();
	}
	protected override void OnDisable()
	{
		F.DestroyAllChildren(unlocksParent);
		currentUnlockIndex = 0;
		unlocksParent.GetComponent<RectTransform>().anchoredPosition = unlockparentBeginPos;
		OkButton.SetActive(false);
		base.OnDisable();
	}
	protected override void OnEnable()
	{
		OkButton.SetActive(true);
		OkButton.GetComponent<Button>().Select();
		//Prepare(new List<string>() { "car01" }, false);
		//Prepare(new List<string>() { "car01", "THE SANDWINDER" }, false);
		//Prepare(new List<string>() { "spn3", "spn1" }, false);
		base.OnEnable();
	}
	public void Prepare(List<string> prizes, bool continuationCheck)
	{
		this.continuationCheck = continuationCheck;
		for (int i=0; i<prizes.Count; i++)
		{
			string prize = prizes[i];
			var unlockPrefab = Instantiate(UnlockPrefab, unlocksParent).transform;
			unlockPrefab.GetComponent<RectTransform>().anchoredPosition = new Vector2(i * Screen.width,0);
			switch (prize[0..3])
			{
				case "car":
					//inside box sprite
					unlockPrefab.GetChild(0).GetComponent<Image>().enabled = false;
					// outside box sprite
					unlockPrefab.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Resources.Load<Sprite>("catalog/" + prize);
					unlockPrefab.GetChild(0).GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text = "";//F.I.Car(prize).name;
					unlockPrefab.GetChild(1).GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text = 
						string.Format(F.I.LocStr("Congratulations! You've unlocked car {0} in Exhibition Mode!"), 
						F.I.LocStr(F.I.Car(prize).name));
					break;
				case "spn":
					//inside box sprite
					unlockPrefab.GetChild(0).GetComponent<Image>().sprite = Resources.Load<Sprite>("catalog/spn" + prize[3..]);
					// outside box sprite
					unlockPrefab.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Resources.Load<Sprite>("catalog/sponsor");
					unlockPrefab.GetChild(0).GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text = 
						Enum.GetName(typeof(Livery),int.Parse(prize[3..]));
					unlockPrefab.GetChild(1).GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text =
						string.Format(F.I.LocStr("Congratulations! You've unlocked livery {0} in Exhibition Mode!"), 
						Enum.GetName(typeof(Livery), int.Parse(prize[3..])));
					break;
				default://track
					//inside box sprite
					unlockPrefab.GetChild(0).GetComponent<Image>().sprite = IMG2Sprite.LoadNewSprite(F.I.tracksPath + prize + ".jpg");
					// outside box sprite
					unlockPrefab.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Resources.Load<Sprite>("catalog/track");
					unlockPrefab.GetChild(0).GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text = prize;
					unlockPrefab.GetChild(1).GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text =
						string.Format(F.I.LocStr("Congratulations! You've unlocked track {0} in Exhibition Mode!"), 
						F.I.tracks[prize].LocalizedName);
					break;
			}
		}
	}
	public void OKButton()
	{
		if(++currentUnlockIndex >= unlocksParent.childCount)
		{
			if (continuationCheck)
			{
				ResultsView.Clear();
				F.I.arcadeSelector.MoveNodeForward();
				GoToView(F.I.arcadeSelector.thisView);
			}
			else
			{
				winnersView.PrepareUsingArcade(continuationCheck);
				GoToView(winnersView);
			}
		}
		else
		{
			if(moveCo != null)
				StopCoroutine(moveCo);
			moveCo = StartCoroutine(Move());
		}
	}
	IEnumerator Move()
	{
		float duration = 1f;
		float elapsed = 0;
		PlaySFX("fe-bitmapscroll");
		Vector2 startPos = unlocksParent.GetComponent<RectTransform>().anchoredPosition;
		Vector2 endPos = new Vector2(-currentUnlockIndex * Screen.width, startPos.y);
		while (elapsed < duration)
		{
			elapsed += Time.deltaTime;
			unlocksParent.GetComponent<RectTransform>().anchoredPosition = Vector2.Lerp(startPos, endPos, F.EasingOutQuint(elapsed));
			yield return null;
		}
		unlocksParent.GetComponent<RectTransform>().anchoredPosition = endPos;
		moveCo = null;
	}
}
