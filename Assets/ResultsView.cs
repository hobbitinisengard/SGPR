using RVP;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// Scoring table with medals after race end
/// </summary>
public class ResultsView : MonoBehaviour
{
	public class ResultInfo
	{
		public VehicleParent vp;
		public int pos;
		public string name;
		public TimeSpan lap;
		public TimeSpan raceTime;
		public float drift;
		public float stunt;
		public void Update(VehicleParent vp)
		{
			this.vp = vp;
			drift = vp.raceBox.drift;
			lap = vp.raceBox.bestLapTime;
			stunt = vp.raceBox.Aero;
			raceTime = vp.raceBox.raceTime;
			name = vp.transform.name;
			pos = RaceManager.I.Position(vp) - 1;
			//Debug.Log($"ResultInfo pos={pos}");
		}
		public ResultInfo(VehicleParent vp)
		{
			Update(vp);
		}
		public ResultInfo()
		{
		}
	}
	const int maxRowHeight = 50;
	const int cols = 5;
	const int addingSpeedPerSec = 10000;
	int finalPosition;
	AudioSource tickSnd;
	public Button OKbutton;
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
	readonly static List<ResultInfo> resultData = new();
	public static ResultInfo Get(VehicleParent vp)
	{
		return resultData.FirstOrDefault(r => r.vp == vp);
	}
	public static void Clear()
	{
		resultData.Clear();
	}
	public static int Count
	{
		get { return resultData.Count; }
	}
	public static void Remove(VehicleParent car)
	{
		var entry = resultData.FirstOrDefault(RD => RD.vp == car);
		if (entry != default)
			resultData.Remove(entry);
	}
	public static void Add(VehicleParent car)
	{
		var entry = resultData.FirstOrDefault(RD => RD.vp == car);
		if(entry == default)
		{
			resultData.Add(new ResultInfo(car));
		}
		else
		{
			entry.Update(car);
		}
	}
	RectTransform gridTableTr;
	Coroutine payoutCo;
	Coroutine addingScoreCo;
	Coroutine addingMedalCo;
	bool isAddingScore;
	float grandScoreMoving = 0;
	public int grandScoreFinal = 0;

	readonly Comparison<ResultInfo> raceTimeComp = new((ResultInfo x, ResultInfo y) => x.raceTime.TotalMilliseconds.CompareTo(y.raceTime.TotalMilliseconds));
	readonly Comparison<ResultInfo> knockoutComp = new((ResultInfo x, ResultInfo y) => y.raceTime.TotalMilliseconds.CompareTo(x.raceTime.TotalMilliseconds));
	readonly Comparison<ResultInfo> stuntComp = new((ResultInfo x, ResultInfo y) => y.stunt.CompareTo(x.stunt));
	readonly Comparison<ResultInfo> driftComp = new((ResultInfo x, ResultInfo y) => y.drift.CompareTo(x.drift));
	readonly Comparison<ResultInfo> lapComp = new((ResultInfo x, ResultInfo y) => x.lap.TotalMilliseconds.CompareTo(y.lap.TotalMilliseconds));

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
		Clear();
	}
	Comparison<ResultInfo> ComparisonBasedOnRaceType()
	{
		return F.I.s_raceType switch
		{
			RaceType.Race => raceTimeComp,
			RaceType.Knockout => knockoutComp,
			RaceType.Stunt => stuntComp,
			RaceType.Drift => driftComp,
			RaceType.TimeTrial => lapComp,
			_ => raceTimeComp,
		};
	}
	private void OnEnable()
	{
		// for testing 
		//ResultRandomizer();

		var comparison = ComparisonBasedOnRaceType();
		resultData.Sort(comparison);
		grandScoreFinal = 0;
		grandScoreMoving = 0;
		grandScore0Text.text = "      000000";
		int carsInSession = resultData.Count;
		var cellSize = gridTable.cellSize;
		cellSize.y = Mathf.Clamp(gridTableTr.rect.height / (1 + carsInSession), 0, maxRowHeight);
		gridTable.cellSize = cellSize;

		resultData.Sort((a, b) => a.pos.CompareTo(b.pos));
		// grid has 5 rows and max 11 cols
		for(int i=0; i<10;i++)
		{
			bool visible = i < resultData.Count;
			bool highlight = visible && F.I.playerData.playerName == resultData[i].name;
			if (highlight)
				finalPosition = resultData[i].pos;
			SetText(gridTableTr.GetChild(cols + cols * i + 0), visible ? Pos(resultData[i].pos) : null, highlight);
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
	int GetResultPos(Comparison<ResultInfo> comp)
	{
		resultData.Sort(comp);
		int index = resultData.FindIndex(pr => pr.name == F.I.playerData.playerName);
		if(index != -1)
		{
			return index + 1;
		}
		Debug.LogError("PlayerName not found in resultData");
		return -1;
	}
	IEnumerator PayoutSeq()
	{
		int lapPos = GetResultPos(lapComp);
		int stuntPos = GetResultPos(stuntComp);
		int driftPos = GetResultPos(driftComp);
		Debug.Log(string.Format("lap,stunt,drift = {0}, {1}, {2}", lapPos, stuntPos, driftPos));
		float positionPerc = (resultData.Count - finalPosition) / (float)resultData.Count;
		int positionBonus = 0;
		int lapBonus = (lapPos <= 2) ? 5000 / lapPos : 0;
		int stuntBonus = (int)((stuntPos <= 2) ? 5000f / stuntPos : 0);
		int driftBonus = (int)((driftPos <= 2) ? 2500f / stuntPos : 0);
		int aeroMeter = (int)resultData.First(r => r.name == F.I.playerData.playerName).stunt;

		
		switch (F.I.scoringType)
		{
			case ScoringType.Championship:
				positionBonus = (int)(10000 * positionPerc);
				grandScoreFinal = positionBonus + lapBonus + stuntBonus + driftBonus + aeroMeter;
				break;
			case ScoringType.Points:
				positionBonus = (int)(10 * positionPerc);
				grandScoreFinal = positionBonus;
				break;
			case ScoringType.Victory:
				positionBonus = (int)positionPerc;
				grandScoreFinal = (int)positionPerc;
				break;
			default:
				break;
		}

		var p = ServerC.I.PlayerMe;
		Debug.Log($"Set points {p.ScoreGet()} + {grandScoreFinal}");
		ServerC.I.ScoreSet(p.ScoreGet() + grandScoreFinal);
		ServerC.I.UpdatePlayerData();

		grandScore0.SetActive(true);

		medalsTable.DestroyAllChildren();
		medalsTable.gameObject.SetActive(true);

		yield return new WaitForSeconds(1);

		Sprite medal;
		
		medal = finalPosition switch
		{
			0 => goldMedals[0],
			1 => (F.I.scoringType == ScoringType.Victory) ? null : silverMedals[0],
			_ => null,
		};

		isAddingScore = true;
		addingScoreCo = StartCoroutine(AddingScoreSeq("POSITION:", positionBonus, medal));

		while (isAddingScore)
			yield return null;

		if (F.I.scoringType == ScoringType.Championship)
		{
			if (lapBonus > 0)
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
		}

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
			tickSnd.pitch = Mathf.LerpUnclamped(1, 1.3f, timer);
			tickSnd.Play();
			timer += 2*Time.deltaTime;
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
		resultData.AddRange(new ResultInfo[R(2, 11)]);
		for(int i=0; i<resultData.Count; ++i)
		{
			resultData[i] = new ResultInfo()
			{
				drift = R(0, 100000),
				lap = TimeSpan.FromMilliseconds(R(30 * 1000, 2 * 3600 * 1000)),
				name = RandomString(R(3, 12)),
				stunt = R(0, 100000),
			};	
		}
		int x = R(0, resultData.Count);
		resultData[x].name = F.I.playerData.playerName;
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
