using RVP;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class ResultInfo
{
	public VehicleParent vp;
	public bool finished;
	public ulong id;
	public string name;
	public TimeSpan lap;
	public TimeSpan raceTime;
	public float progress;
	public float drift;
	public float aeromiles;
	public int maxAeroStars;
	public int score { get; private set; }
	public Livery sponsor;

	public void SetPostRaceScore(int finalScore)
	{
		Debug.Log(name + " " + score.ToString() + " " + finalScore);
		score = finalScore;
	}
	public void Update(VehicleParent vp)
	{
		this.vp = vp;
		drift = vp.raceBox.Drift;
		lap = vp.raceBox.bestLapTime;
		progress = vp.raceBox.RaceProgressLaps;
		aeromiles = vp.raceBox.Aero;
		raceTime = vp.raceBox.raceTime;
		name = vp.transform.name;
		score = vp.lastRoundScore;
		sponsor = vp.sponsor;
		finished = !vp.raceBox.enabled;
		//Debug.Log(string.Format("{0}, progress:{1}, score:{2}, ", name, progress, aeromiles));
	}
	public ResultInfo(VehicleParent vp)
	{
		id = vp.OwnerClientId;
		Update(vp);
	}
	public ResultInfo()
	{
	}
	public string ToString(RecordType recordType)
	{
		switch (recordType)
		{
			case RecordType.BestLap:
				return lap.ToLaptimeStr();
			case RecordType.RaceTime:
				return raceTime.ToLaptimeStr();
			case RecordType.StuntScore:
				return ((int)(aeromiles)).ToString();
			case RecordType.DriftScore:
				return drift.ToString("F0");
			default:
				return "-";
		}
	}
}
/// <summary>
/// Scoring table with medals after race end
/// </summary>
public class ResultsView : MainMenuView
{
	const int maxRowHeight = 50;
	const int cols = 5;
	const int addingSpeedPerSec = 10000;
	int finalPosition;
	AudioSource tickSnd;
	public WinnersView winnersView;
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

	/// <summary>
	/// Returns 1-10
	/// </summary>
	static int Pos(ulong id, Comparison<ResultInfo> comp)
	{
		resultData.Sort(comp);
		int index = resultData.FindIndex(pr => pr.id == id);
		if (index != -1)
		{
			return index + 1;
		}
		Debug.LogError("PlayerName not found in resultData");
		return -1;
	}

	public static int CalculatePostraceReward(ResultInfo ri)
	{
		int finalScore = 0;
		int finalPos = Pos(ri.id, ComparisonBasedOnRaceType()) - 1;
		int lapPos = Pos(ri.id, lapComp);
		int stuntPos = Pos(ri.id, stuntComp);
		int driftPos = Pos(ri.id, driftComp);
		Debug.Log(string.Format("lap,stunt,drift = {0}, {1}, {2}", lapPos, stuntPos, driftPos));
		float positionPerc = (resultData.Count - finalPos) / (float)resultData.Count;
		int lapBonus = (lapPos <= 2) ? (int)(5000f / lapPos) : 0;
		int stuntBonus = (int)((stuntPos <= 2) ? 5000f / stuntPos : 0);
		int driftBonus = (int)((driftPos <= 2) ? 2500f / driftPos : 0);
		int aeroMeter = (int)ri.aeromiles;
		int positionBonus = 0;
		switch (F.I.scoringType)
		{
			case ScoringType.Championship:
				positionBonus = (int)(10000 * positionPerc);
				finalScore = positionBonus + lapBonus + stuntBonus + driftBonus + aeroMeter;
				break;
			case ScoringType.Points:
				positionBonus = (int)(10 * positionPerc);
				finalScore = positionBonus;
				break;
			case ScoringType.Victory:
				positionBonus = (int)positionPerc;
				finalScore = (int)positionPerc;
				break;
			default:
				break;
		}
		Debug.Log(ri.name + string.Format("Reward: lap,stunt,drift = {0}, {1}, {2}, {3}, {4}", positionBonus, lapBonus, stuntBonus, driftBonus, aeroMeter));
		return finalScore;
	}
	public static List<ResultInfo> SortedResultsByScore
	{
		get
		{
			if (F.I.teams)
			{
				List<SponsorScore> teamScores = new();

				foreach (var ri in resultData)
				{
					var teamScore = teamScores.Find(s => ri.sponsor == s.sponsor);

					if (teamScore == null)
					{
						teamScores.Add(new SponsorScore { sponsor = ri.sponsor, score = ri.score });
					}
					else
					{
						teamScore.score += ri.score;
					}
				}
				teamScores.Sort((x, y) => y.score.CompareTo(x.score));

				resultData.Sort((ResultInfo A, ResultInfo B) =>
				{
					var teamScoreA = teamScores.Find(s => s.sponsor == A.sponsor).score;
					var teamScoreB = teamScores.Find(s => s.sponsor == B.sponsor).score;
					return teamScoreB.CompareTo(teamScoreA);
				});
			}
			else
			{
				resultData.Sort(ScoreComp);
			}
			return resultData;
		}
	}
	public static List<ResultInfo> SortedResultsByFinishPos
	{
		get
		{
			var comparison = ComparisonBasedOnRaceType();
			resultData.Sort(comparison);
			return resultData;
		}
	}
	public static ResultInfo Get(VehicleParent vp)
	{
		return resultData.FirstOrDefault(r => r.vp == vp);
	}

	public static void Clear()
	{
		resultData.Clear();
	}
	public static int FinishedPlayers
	{
		get { return resultData.Count(r => r.finished); }
	}
	public static int Count
	{
		get { return resultData.Count; }
	}
	public static void Remove(ulong id)
	{
		var entry = resultData.FirstOrDefault(RD => RD.id == id);
		if (entry != default)
			resultData.Remove(entry);
	}
	public static void Add(VehicleParent car)
	{
		var entry = resultData.FirstOrDefault(RD => RD.vp == car);
		if (entry == default)
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
	private int lapPos;
	private int stuntPos;
	private int driftPos;
	private float positionPerc;
	private int positionBonus;
	private int lapBonus;
	private int stuntBonus;
	private int driftBonus;
	private int aeroMeter;
	public static readonly Comparison<ResultInfo> raceComp = new((ResultInfo x, ResultInfo y) => x.raceTime.TotalSeconds.CompareTo(y.raceTime.TotalSeconds));
	public static readonly Comparison<ResultInfo> knockoutComp = new((ResultInfo x, ResultInfo y) => { return y.progress.CompareTo(x.progress); });
	public static readonly Comparison<ResultInfo> stuntComp = new((ResultInfo x, ResultInfo y) => y.aeromiles.CompareTo(x.aeromiles));
	public static readonly Comparison<ResultInfo> driftComp = new((ResultInfo x, ResultInfo y) => y.drift.CompareTo(x.drift));
	public static readonly Comparison<ResultInfo> lapComp = new((ResultInfo x, ResultInfo y) => x.lap.TotalSeconds.CompareTo(y.lap.TotalSeconds));
	public static readonly Comparison<ResultInfo> ScoreComp = new((ResultInfo x, ResultInfo y) => y.score.CompareTo(x.score));

	public void OKButton()
	{
		if (F.I.gameMode == GameMode.Multiplayer)
		{
			if (F.I.Rounds > 0 && F.I.CurRound > F.I.Rounds)
			{
				for (int i = 0; i < resultData.Count; ++i)
				{
					resultData[i].SetPostRaceScore(resultData[i].score + CalculatePostraceReward(resultData[i]));
				}
				winnersView.PrepareViewUsingMultiplayer();
				GoToView(winnersView);
			}
			else
			{
				Clear();
				GoToView(MultiPlayerSelector.I.thisView);
			}
		}
		else if (F.I.gameMode == GameMode.Arcade)
		{
			for (int i = 0; i < resultData.Count; ++i)
			{
				resultData[i].SetPostRaceScore(resultData[i].score + CalculatePostraceReward(resultData[i]));
			}

			bool continuationCheck = CheckArcadeCondition(F.I.curNode.continuationReq);
			bool prizeCheck = CheckArcadeCondition(F.I.curNode.prizeReq);
			List<string> prizes = prizeCheck ? UnlockPrizes() : null;
			int targetNodeID = ArcadeSelector.I.TargetNodeID();
			if (continuationCheck)
			{ // update unlocked paths
				if(!F.I.curVariant.progress.unlockedPaths[F.I.curArcadeNodeID].Any(nodeID => nodeID == targetNodeID))
				{
					F.I.curVariant.progress.unlockedPaths[F.I.curArcadeNodeID].Add(targetNodeID);
					F.I.SaveArcadeProgress();
				}
			}
			if (prizes != null && prizes.Count > 0)
			{
				PrizeView.I.Prepare(prizes, continuationCheck);
				GoToView(PrizeView.I.thisView);
			}
			else
			{
				if (continuationCheck) // go back to arcade selector
				{
					Clear();
					GoToView(ArcadeSelector.I.thisView);
				}
				else
				{ // go to winners view
					winnersView.PrepareUsingArcade(continuationCheck);
					GoToView(winnersView);
				}
			}
		}
	}
	enum ArcadeResult
	{
		Failed,
		Succeeded,
		Continue
	}
	/// <returns>list of unlocked prizes</returns>
	List<string> UnlockPrizes()
	{
		string[] prizes = F.I.curNode.prizeReq.name.Split(',');
		List<string> prizeList = new(prizes);
		for (int i = 0; i < prizes.Length; i++)
		{
			if (prizes[i].Contains("car") && !F.I.Car(prizes[i]).unlocked) // unlock car
			{
				prizeList.Add(prizes[i]);
				F.I.Car(prizes[i]).unlocked = true;
			}
			else if (prizes[i].Contains("lvr") && !F.I.unlockedLiveries.Contains((Livery)int.Parse(prizes[i]))) // unlock livery
			{
				prizeList.Add(prizes[i]);
				int liveryNr = int.Parse(prizes[i]);
				F.I.unlockedLiveries[liveryNr] = (Livery)liveryNr;
			}
			else
			{
				prizeList.Add(prizes[i]); // unlock track
				F.I.tracks[prizes[i]].unlocked = true;
			}
		}
		return prizeList;
	}
	bool CheckArcadeCondition(ArcadeVariant.Prize req)
	{
		ResultInfo playerResult;
		if (F.I.alwaysFirst)
		{
			F.I.alwaysFirst = SortedResultsByFinishPos[0].name == F.I.playerData.playerName;
		}

		switch (req.condition)
		{
			case ArcadeVariant.Prize.Condition.PositionAtLeast:
				var sortedPlayers = SortedResultsByFinishPos;
				int minimumPosition = int.Parse(req.conditionArgument);
				for (int i = 0; i < minimumPosition; ++i)
				{
					if (sortedPlayers[i].name == F.I.playerData.playerName)
						return true;
				}
				return false;
			case ArcadeVariant.Prize.Condition.LapAtMost:
				playerResult = resultData.First(p => p.name == F.I.playerData.playerName);
				TimeSpan requiredLap = TimeSpan.Parse(req.conditionArgument);
				return playerResult.lap <= requiredLap;
			case ArcadeVariant.Prize.Condition.AeroStarsAtLeast:
				playerResult = resultData.First(p => p.name == F.I.playerData.playerName);
				int starsReq = int.Parse(req.conditionArgument);
				return playerResult.maxAeroStars >= starsReq;
			case ArcadeVariant.Prize.Condition.StuntAtLeast:
				playerResult = resultData.First(p => p.name == F.I.playerData.playerName);
				float aeromilesReq = float.Parse(req.conditionArgument);
				return playerResult.aeromiles >= aeromilesReq;
			case ArcadeVariant.Prize.Condition.DriftsAtLeast:
				playerResult = resultData.First(p => p.name == F.I.playerData.playerName);
				float driftsReq = float.Parse(req.conditionArgument);
				return playerResult.drift >= driftsReq;
			case ArcadeVariant.Prize.Condition.TimeAtMost:
				playerResult = resultData.First(p => p.name == F.I.playerData.playerName);
				TimeSpan timeAtMostReq = TimeSpan.Parse(req.conditionArgument);
				return playerResult.raceTime <= timeAtMostReq;
			case ArcadeVariant.Prize.Condition.FastestLaptime:
				resultData.Sort((ResultInfo A, ResultInfo B) =>
				{
					return A.lap.CompareTo(B.lap);
				});
				return resultData[0].name == F.I.playerData.playerName;
			case ArcadeVariant.Prize.Condition.AlwaysFirst:
				return F.I.alwaysFirst;
			case ArcadeVariant.Prize.Condition.AllPathsFound:
				// sum of all unlocked paths in all nodes
				int allPaths = F.I.curVariant.nodes.Sum(n => n.connections.Length);
				int unlockedPaths = 0;
				for (int i = 0; i < F.I.curVariant.progress.unlockedPaths.Count; i++)
				{
					unlockedPaths += F.I.curVariant.progress.unlockedPaths[i].Count;
				}
				return unlockedPaths == allPaths;
			default:
				Debug.Log("null");
				return false;
		}
	}
	new void Awake()
	{
		base.Awake();
		gridTableTr = gridTable.GetComponent<RectTransform>();
		tickSnd = GetComponent<AudioSource>();
	}
	protected override void OnDisable()
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

		base.OnDisable();
	}
	static Comparison<ResultInfo> ComparisonBasedOnRaceType()
	{
		return F.I.s_raceType switch
		{
			RaceType.Race => raceComp,
			RaceType.Knockout => knockoutComp,
			RaceType.Stunt => stuntComp,
			RaceType.Drift => driftComp,
			RaceType.TimeTrial => lapComp,
			_ => raceComp,
		};
	}

	protected override void OnEnable()
	{
		F.I.CurRound++;
		//ResultRandomizer(); // for testing 
		grandScoreMoving = 0;
		grandScore0Text.text = "      0";
		var cellSize = gridTable.cellSize;
		cellSize.y = Mathf.Clamp(gridTableTr.rect.height / (1 + resultData.Count), 0, maxRowHeight);
		gridTable.cellSize = cellSize;
		resultData.Sort(ComparisonBasedOnRaceType());
		// grid has 5 rows and max 11 cols
		for (int i = 0; i < 10; i++)
		{
			bool visible = i < resultData.Count;
			bool highlight = visible && ServerC.I.networkManager.LocalClientId == resultData[i].id;
			if (highlight)
				finalPosition = i;
			SetText(gridTableTr.GetChild(cols + cols * i + 0), visible ? Pos(i) : null, highlight);
			SetText(gridTableTr.GetChild(cols + cols * i + 1), visible ? resultData[i].name : null, highlight);
			SetText(gridTableTr.GetChild(cols + cols * i + 2), visible ? resultData[i].lap.ToLaptimeStr() : null, highlight);
			SetText(gridTableTr.GetChild(cols + cols * i + 3), visible ? resultData[i].aeromiles.ToString("N0") : null, highlight);
			SetText(gridTableTr.GetChild(cols + cols * i + 4), visible ? resultData[i].drift.ToString("N0") : null, highlight);
		}

		lapPos = Pos(ServerC.I.networkManager.LocalClientId, lapComp);
		stuntPos = Pos(ServerC.I.networkManager.LocalClientId, stuntComp);
		driftPos = Pos(ServerC.I.networkManager.LocalClientId, driftComp);

		positionPerc = (resultData.Count - finalPosition) / (float)resultData.Count;
		positionBonus = 0;
		lapBonus = (lapPos <= 2) ? (int)(5000f / lapPos) : 0;
		stuntBonus = (int)((stuntPos <= 2) ? 5000f / stuntPos : 0);
		driftBonus = (int)((driftPos <= 2) ? 2500f / driftPos : 0);
		aeroMeter = (int)resultData[finalPosition].aeromiles;

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
		Debug.Log(resultData[finalPosition].name + string.Format("OnEnable. lap,stunt,drift = {0}, {1}, {2}, {3}, {4}",
			positionBonus, lapBonus, stuntBonus, driftBonus, aeroMeter));
		ServerC.I.ScoreSet(ServerC.I.PlayerMe.ScoreGet() + grandScoreFinal);
		ServerC.I.UpdatePlayerData();

		if (payoutCo != null)
			StopCoroutine(payoutCo);
		payoutCo = StartCoroutine(PayoutSeq());

		base.OnEnable();
	}
	string Pos(int i)
	{
		string s;
		switch (i)
		{
			case 0:
				s = "1-st";
				break;
			case 1:
				s = "2-nd";
				break;
			case 2:
				s = "3-rd";
				break;
			default:
				s = (i + 1).ToString() + "-th";
				break;
		}
		return F.I.LocStr(s);
	}
	void SetText(Transform tr, string content, bool highlight)
	{
		if (content != null)
		{
			var ugui = tr.GetComponent<TextMeshProUGUI>();
			ugui.text = content;
			ugui.color = highlight ? Color.white : Color.gray;
		}
		tr.gameObject.SetActive(content != null);
	}


	IEnumerator PayoutSeq()
	{
		//Debug.Log($"Set points {p.ScoreGet()} + {grandScoreFinal}");
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
		addingScoreCo = StartCoroutine(AddingScoreSeq(F.I.LocStr("POSITION") + ":", positionBonus, medal));

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
				addingScoreCo = StartCoroutine(AddingScoreSeq(F.I.LocStr("LAPTIME") + ":", lapBonus, medal));

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
				addingScoreCo = StartCoroutine(AddingScoreSeq(F.I.LocStr("STUNTS") + ":", stuntBonus, medal));

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
				addingScoreCo = StartCoroutine(AddingScoreSeq(F.I.LocStr("DRIFT") + ":", driftBonus, medal));

				while (isAddingScore)
					yield return null;
			}

			isAddingScore = true;
			addingScoreCo = StartCoroutine(AddingScoreSeq(F.I.LocStr("AEROMILES") + ":", aeroMeter, null));

			while (isAddingScore)
				yield return null;
		}

		addingScore.SetActive(false);
		grandScore0.SetActive(false);
		grandScore1.SetActive(true);
		grandScore1Text.text = "      " + grandScoreFinal;
		tickSnd.pitch = 1;
		tickSnd.Play();
		grandScoreFinal = 0;
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
			timer += 2 * Time.deltaTime;
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
		while (timer < timeRequired)
		{
			var c = newMedal.color;
			c.a = timer;
			newMedal.color = c;
			timer += Time.deltaTime;
			yield return null;
		}
	}
	void ResultRandomizer()
	{
		resultData.AddRange(new ResultInfo[F.R(2, 11)]);
		for (int i = 0; i < resultData.Count; ++i)
		{
			resultData[i] = new ResultInfo()
			{
				drift = F.R(0, 100000),
				lap = TimeSpan.FromMilliseconds(F.R(30 * 1000, 2 * 3600 * 1000)),
				name = F.RandomString(F.R(3, 12)),
				aeromiles = F.R(0, 100000),
			};
		}
		int x = F.R(0, resultData.Count);
		resultData[x].name = F.I.playerData.playerName;
	}
}
