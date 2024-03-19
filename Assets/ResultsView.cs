using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResultsView : MonoBehaviour
{
	public class PersistentResult
	{
		public string name;
		public TimeSpan lap;
		public float drift;
		public float stunt;
	}
	const int maxRowHeight = 50;
	const int cols = 5;
	const int addingSpeedPerSec = 10000;
	int finalPosition;
	AudioSource tickSnd;
	public GridLayoutGroup gridTable;
	public GameObject grandScore0;
	public TextMeshProUGUI grandScore0Text;
	public GameObject grandScore1;
	public TextMeshProUGUI grandScore1Text;
	public GameObject addingScore;
	public TextMeshProUGUI addingScoreText;
	public TextMeshProUGUI addingScoreScore;
	public Transform medalsTable;
	public GameObject medalPrefab;
	public Sprite[] silverMedals; // race,lap,stunt,drift
	public Sprite[] goldMedals;
	public static PersistentResult[] resultData;
	RectTransform gridTableTr;
	Coroutine payoutCo;
	Coroutine addingScoreCo;
	Coroutine addingMedalCo;
	bool isAddingScore;
	float grandScoreMoving = 0;
	public static int grandScoreFinal = 0;
	private void Awake()
	{
		gridTableTr = gridTable.GetComponent<RectTransform>();
		tickSnd = GetComponent<AudioSource>();
	}
	private void OnDisable()
	{
		if (addingScoreCo != null)
			StopCoroutine(addingScoreCo);
		if (payoutCo != null)
			StopCoroutine(payoutCo);
		if (addingMedalCo != null)
			StopCoroutine(addingMedalCo);

		grandScore1.SetActive(false);
		addingScore.SetActive(false);
		medalsTable.gameObject.SetActive(false);

		resultData = null;
	}
	private void OnEnable()
	{
		// for testing 
		ResultRandomizer();

		grandScoreFinal = 0;
		grandScoreMoving = 0;
		grandScore0Text.text = "      000000";
		if(resultData == null || resultData.Length < 2)
		{
			Debug.LogError("No resultData");
			return;
		}
		int carsInSession = resultData.Length;
		var cellSize = gridTable.cellSize;
		cellSize.y = Mathf.Clamp(gridTableTr.rect.height / (1 + carsInSession), 0, maxRowHeight);
		gridTable.cellSize = cellSize;

		// grid has 5 rows and max 11 cols
		for(int i=0; i<10;i++)
		{
			bool visible = i < resultData.Length;
			bool highlight = visible && Info.playerData.playerName == resultData[i].name;
			if (highlight)
				finalPosition = i;
			SetText(gridTableTr.GetChild(cols + cols * i + 0), visible ? Pos(i) : null, highlight);
			SetText(gridTableTr.GetChild(cols + cols * i + 1), visible ? resultData[i].name : null, highlight);
			SetText(gridTableTr.GetChild(cols + cols * i + 2), visible ? resultData[i].lap.ToLaptimeStr() : null, highlight);
			SetText(gridTableTr.GetChild(cols + cols * i + 3), visible ? resultData[i].stunt.ToString("N0") : null, highlight);
			SetText(gridTableTr.GetChild(cols + cols * i + 4), visible ? resultData[i].drift.ToString("N0") : null, highlight);
		}
		if (payoutCo != null)
			StopCoroutine(payoutCo);
		payoutCo = StartCoroutine(PayoutSeq());
	}
	string Pos(int i)
	{
		return i switch
		{
			0 => "1-st",
			1 => "2-nd",
			2 => "3-rd",
			_ => (i + 1).ToString() + "-th"
		};
	}
	void SetText(Transform tr, string content, bool highlight)
	{
		if(content!=null)
		{
			var ugui = tr.GetComponent<TextMeshProUGUI>();
			ugui.text = content;
			ugui.color = highlight ? Color.white : Color.gray;
		}
		tr.gameObject.SetActive(content!=null);
	}
	int GetResultPos(Comparison<PersistentResult> comp)
	{
		Array.Sort(resultData, comp);
		for (int i = 0; i < resultData.Length; ++i)
		{
			if (resultData[i].name == Info.playerData.playerName)
				return i+1;
		}
		Debug.LogError("PlayerName not found in resultData");
		return -1;
	}
	IEnumerator PayoutSeq()
	{
		int lapPos = GetResultPos((PersistentResult y, PersistentResult x) => { return y.lap.TotalMilliseconds.CompareTo(x.lap.TotalMilliseconds); });
		int stuntPos = GetResultPos((PersistentResult x, PersistentResult y) => { return y.stunt.CompareTo(x.stunt); });
		int driftPos = GetResultPos((PersistentResult x, PersistentResult y) => { return y.drift.CompareTo(x.drift); });
		Debug.Log(string.Format("lap,stunt,drift = {0}, {1}, {2}", lapPos, stuntPos, driftPos));
		int positionBonus = 10000 * (resultData.Length - finalPosition) / resultData.Length;
		int lapBonus = (lapPos <= 2) ? 5000 / lapPos : 0;
		int stuntBonus = (int)((stuntPos <= 2) ? 5000f / stuntPos : 0);
		int driftBonus = (int)((driftPos <= 2) ? 2500f / stuntPos : 0);
		int aeroMeter = (int)resultData.First(r => r.name == Info.playerData.playerName).stunt;
		grandScoreFinal = positionBonus + lapBonus + stuntBonus + driftBonus + aeroMeter;
		
		grandScore0.SetActive(true);

		medalsTable.DestroyAllChildren();
		medalsTable.gameObject.SetActive(true);

		yield return new WaitForSeconds(1);

		Sprite medal;
		
		medal = finalPosition switch
		{
			0 => goldMedals[0],
			1 => silverMedals[0],
			_ => null,
		};

		isAddingScore = true;
		addingScoreCo = StartCoroutine(AddingScoreSeq("POSITION:", positionBonus, medal));

		while (isAddingScore)
			yield return null;
		
		if(lapBonus > 0)
		{
			medal = lapPos switch
			{
				1 => goldMedals[1],
				2 => silverMedals[1],
				_ => null,
			};

			isAddingScore = true;
			addingScoreCo = StartCoroutine(AddingScoreSeq("LAP-TIME:", lapBonus, medal));

			while (isAddingScore)
				yield return null;
		}

		if (stuntBonus > 0)
		{
			medal = stuntPos switch
			{
				1 => goldMedals[2],
				2 => silverMedals[2],
				_ => null,
			};
			isAddingScore = true;
			addingScoreCo = StartCoroutine(AddingScoreSeq("STUNTS:", stuntBonus, medal));

			while (isAddingScore)
				yield return null;
		}

		if (driftBonus > 0)
		{
			medal = stuntPos switch
			{
				1 => goldMedals[3],
				2 => silverMedals[3],
				_ => null,
			};
			isAddingScore = true;
			addingScoreCo = StartCoroutine(AddingScoreSeq("DRIFT:", driftBonus, medal));

			while (isAddingScore)
				yield return null;
		}

		isAddingScore = true;
		addingScoreCo = StartCoroutine(AddingScoreSeq("AEROMETER:", aeroMeter, null));

		while (isAddingScore)
			yield return null;

		addingScore.SetActive(false);
		grandScore0.SetActive(false);
		grandScore1.SetActive(true);
		grandScore1Text.text = "      " + grandScoreFinal;
		tickSnd.pitch = 1;
		tickSnd.Play();
	}
	IEnumerator AddingScoreSeq(string recordType, float bonus, Sprite medal)
	{ 
		isAddingScore = true;
		addingScore.SetActive(true);
		addingScoreText.text = recordType;
		addingScoreScore.text = "      +" + bonus.ToString();
		float grandScoreInit = grandScoreMoving;
		float timeRequired = bonus / addingSpeedPerSec;
		float timer = 0;
		if (medal != null)
			addingMedalCo = StartCoroutine(AddMedal(medal));
		while (grandScoreMoving < grandScoreInit + bonus)
		{
			grandScoreMoving = Mathf.Round(Mathf.Lerp(grandScoreInit, grandScoreInit + bonus, timer / timeRequired));
			grandScore0Text.text = "      " + grandScoreMoving.ToString();
			tickSnd.pitch = Mathf.LerpUnclamped(1, 1.5f, timer);
			tickSnd.Play();
			timer += Time.deltaTime;
			yield return null;
		}
		yield return new WaitForSeconds(1);
		isAddingScore = false;
	}
	IEnumerator AddMedal(Sprite medal)
	{
		var newMedal = Instantiate(medalPrefab, medalsTable).GetComponent<Image>();
		newMedal.sprite = medal;
		float timer = 0;
		float timeRequired = 1;
		while(timer < timeRequired)
		{
			var c = newMedal.color;
			c.a = timer;
			newMedal.color = c;
			timer += Time.deltaTime;
			yield return null;
		}
	}
	int R(int min, int max)
	{
		return UnityEngine.Random.Range(min, max);
	}
	void ResultRandomizer()
	{
		resultData = new PersistentResult[R(2, 11)];
		for(int i=0; i<resultData.Length; ++i)
		{
			resultData[i] = new PersistentResult()
			{
				drift = R(0, 100000),
				lap = TimeSpan.FromMilliseconds(R(30 * 1000, 2 * 3600 * 1000)),
				name = RandomString(R(3, 12)),
				stunt = R(0, 100000),
			};	
		}
		int x = R(0, resultData.Length);
		resultData[x].name = Info.playerData.playerName;
	}
	string RandomString(int length)
	{
		var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
		var stringChars = new char[length];

		for (int i = 0; i < stringChars.Length; i++)
			stringChars[i] = chars[R(0,chars.Length)];

		return new string(stringChars);
	}
}
