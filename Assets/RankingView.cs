using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class RankingView : MainMenuView
{
   public TextMeshProUGUI upBarText;
   public GameObject rankingRowPrefab;
	public Transform rankingContent;
	public Scrollbar scrollbar;
	Coroutine sinCo;
	Coroutine moveTableCo;
	AudioSource audio;
	Coroutine selectBlinkCo;
	/// <summary>
	/// 1 = Top, 0 = Bottom
	/// </summary>
	private float scrollTarget;
	private Transform selectedRow;
	public void OKButton()
	{
		GoToView(MultiPlayerSelector.I.thisView);
	}
	protected override void Awake()
	{
		audio = GetComponent<AudioSource>();
		base.Awake();
	}
	IEnumerator SelectAndBlink(Transform row)
	{
		if (moveTableCo != null)
			StopCoroutine(moveTableCo);
		scrollTarget = 1 - ((float)row.GetSiblingIndex() / (row.parent.childCount));
		moveTableCo = StartCoroutine(MoveTableCo());

		while (gameObject.activeSelf)
		{
			SetColorOfRow(row, F.I.red);
			yield return new WaitForSecondsRealtime(.5f);
			SetColorOfRow(row, Color.white);
			yield return new WaitForSecondsRealtime(.5f);
		}
	}
	protected override void OnDisable()
	{
		base.OnDisable();
		F.I.move2Ref.action.performed -= Move;
		SetColorOfRow(selectedRow, Color.white);
		F.I.SaveRanking();
	}
	protected override void OnEnable()
	{
		F.I.move2Ref.action.performed += Move;
		Player[] players = ServerC.I.scoreSortedPlayers;
		RankingRowData newEntry = new RankingRowData(players);

		string gameName;
		LinkedList<RankingRowData> data;
		switch (F.I.scoringType)
		{
			case ScoringType.Championship:
				gameName = "CHAMPIONSHIPS";
				data = F.I.teams ? F.I.rankingData.TeamChamp : F.I.rankingData.Champ;
				break;
			case ScoringType.Points:
				gameName = "POINTS";
				data = F.I.teams ? F.I.rankingData.TeamPts : F.I.rankingData.Pts;
				break;
			case ScoringType.Victory:
				gameName = "VICTORY";
				data = F.I.teams ? F.I.rankingData.TeamVic : F.I.rankingData.Vic;
				break;
			default:
				gameName = null;
				data = null;
				break;
		}

		upBarText.text = "MULTIPLAYER RANKING - " + (F.I.teams ? "TEAM " : "") + gameName + " - Top 100"; 

		var newScore = newEntry.WinValue;		

		while (rankingContent.childCount != 100)
		{
			Instantiate(rankingRowPrefab, rankingContent);
		}

		LinkedListNode<RankingRowData> curNode = data.First;
		for (int i=0; i<rankingContent.childCount; ++i)
		{
			var row = rankingContent.GetChild(i);
			row.GetChild(0).GetComponent<TextMeshProUGUI>().text = (i + 1).ToString("D3");

			if (newEntry != null && (curNode == null || newScore >= curNode.Value.WinValue))
			{ // add newEntry to data
				if (curNode == null)
					curNode = data.AddFirst(newEntry);
				else
					curNode = data.AddBefore(curNode, newEntry);
				newEntry = null;
				selectedRow = row;
				if (selectBlinkCo != null)
					StopCoroutine(selectBlinkCo);
				selectBlinkCo = StartCoroutine(SelectAndBlink(selectedRow));
			}

			if (i < data.Count)
			{
				row.GetChild(1).GetComponent<TextMeshProUGUI>().text = curNode.Value.name;
				row.GetChild(2).GetComponent<TextMeshProUGUI>().text = curNode.Value.dateStr;
				row.GetChild(3).GetComponent<TextMeshProUGUI>().text = "Round " + curNode.Value.rounds.ToString();
				row.GetChild(4).GetComponent<TextMeshProUGUI>().text = F.I.scoringType switch
				{
					ScoringType.Championship => $"${curNode.Value.moneyOrPerc}",
					ScoringType.Points => $"{curNode.Value.moneyOrPerc} %",
					ScoringType.Victory => $"{curNode.Value.moneyOrPerc} %",
					_ => null,
				};
			}
			else
			{
				row.GetChild(1).GetComponent<TextMeshProUGUI>().text = "------------";
				row.GetChild(2).GetComponent<TextMeshProUGUI>().text = "------------------";
				row.GetChild(3).GetComponent<TextMeshProUGUI>().text = "-------";
				row.GetChild(4).GetComponent<TextMeshProUGUI>().text = "--------";
			}

			if (curNode != null)
				curNode = curNode.Next;
		}

		if (sinCo != null)
			StopCoroutine(sinCo);
		sinCo = StartCoroutine(SinAnim());

		while(data.Count > 100)
			data.RemoveLast();

		base.OnEnable();
	}

	private void Move(UnityEngine.InputSystem.InputAction.CallbackContext input)
	{
		int dir = -(int)input.ReadValue<Vector2>().y;
		if(dir != 0)
		{
			scrollTarget = Mathf.Clamp01(scrollTarget - dir * 0.1f);
			if (moveTableCo != null)
				StopCoroutine(moveTableCo);
			moveTableCo = StartCoroutine(MoveTableCo());
		}
	}
	protected IEnumerator MoveTableCo()
	{
		float timer = 0;
		float scrollInit = scrollbar.value;
		audio.Play();
		while (timer < 1)
		{
			float step = F.EasingOutQuint(timer);
			scrollbar.value = Mathf.Lerp(scrollInit, scrollTarget, step);
			timer += Time.deltaTime;

			yield return null;
		}
	}

	IEnumerator SinAnim()
	{
		float animDurationSecs = 2;
		float power = 1;
		float sinArg = 0;
		float sinSpeed = 10;
		yield return null;

		while(power > 0)
		{
			for (int i = 0; i < rankingContent.childCount; ++i)
			{
				var childRt = rankingContent.GetChild(i).GetComponent<RectTransform>();
				var pos = childRt.anchoredPosition;
				pos.x = 350 * power * Mathf.Sin((((i % 2) == 0) ? Mathf.PI/2f : 0) + sinArg);
				childRt.anchoredPosition = pos;
			}
			power = Mathf.Clamp01(power - Time.deltaTime / animDurationSecs);
			sinArg += Time.deltaTime * sinSpeed;
			//sinSpeed -= Time.deltaTime / animDurationSecs;
			yield return null;
		}
	}
	
	void SetColorOfRow(Transform row, Color c)
	{
		Debug.Log(c != Color.white);
		for (int i = 0; i < row.childCount; ++i)
			row.GetChild(i).GetComponent<TextMeshProUGUI>().color = c;
	}
}
