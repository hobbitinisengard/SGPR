using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ArcadeSelector : TrackSelectorTemplate
{
	public MainMenuView thisView;
	public Transform nodeParent;
	public Transform pathParent;
	public GameObject nodePrefab;
	public GameObject pathPrefab;
	public Sprite squareRounded;
	public Sprite circle;
	public Text arcadeReqText;
	public Transform objectivesContainer;
	public Text objectivesRecordsTitle;
	Coroutine blinkingCo;
	Coroutine alignCo;
	RectTransform curNodeRT;
	RectTransform targetNodeRT;
	Image selectedPath;
	int curPathConnectionIdx;
	Dictionary<string, RectTransform> pathsRTs = new();
	public void Reset()
	{
		F.DestroyAllChildren(nodeParent);
		F.DestroyAllChildren(pathParent);
		pathsRTs.Clear();
		F.I.curArcadeNodeID = -1;
		F.I.CurRound = 0;
		F.I.curArcadeScore = 0;
		F.I.scoringType = ScoringType.Championship;
		selectedPath = null;
	}
	private new void OnDisable()
	{
		F.I.move2Ref.action.performed -= ChangePath;
		if (alignCo != null)
			StopCoroutine(alignCo);
		if (blinkingCo != null)
			StopCoroutine(blinkingCo);
		targetNodeRT.GetComponent<Image>().color = F.I.curVariant.nodes[F.I.targetArcadeNodeID].color;

		if (F.I.curArcadeNodeID != -1)
		{
			foreach (var targetID in F.I.curNode.connections)
			{
				pathsRTs[F.I.curArcadeNodeID + "-" + targetID].GetComponent<Image>().color = Color.gray;
				var pathGO = pathsRTs[F.I.curArcadeNodeID + "-" + targetID].gameObject;
				pathGO.SetActive(pathGO.CompareTag(Info.alreadyWalkedTag));
			}
		}
		
		base.OnDisable();
	}
	public void MoveNodeForward()
	{
		if(F.I.targetNode.connections.Length > 0)
		{
			if (F.I.curArcadeNodeID >= 0)
				pathsRTs[F.I.curArcadeNodeID + "-" + F.I.targetArcadeNodeID].tag = Info.alreadyWalkedTag;

			targetNodeRT.GetComponent<Image>().sprite = circle;
			F.I.curArcadeNodeID = targetNodeRT.GetSiblingIndex();
			F.I.targetArcadeNodeID = F.I.curNode.connections[0];
			F.I.CurRound++;
		}
	}
	protected override void OnEnable()
	{
		if (F.I.curArcadeNodeID == -1) // when starting new arcade variant
		{
			F.I.targetArcadeNodeID = F.I.curVariant.starts.FirstOrDefault(s => s.allowedCarsIdxs.Any(carIdx => carIdx == F.I.s_playerCarIdx)).node;
			CreateNodeMap();
		}
		targetNodeRT = nodeParent.GetChild(F.I.targetArcadeNodeID).GetComponent<RectTransform>();

		if (F.I.curArcadeNodeID >= 0)
			curNodeRT = nodeParent.GetChild(F.I.curArcadeNodeID).GetComponent<RectTransform>();
		else
			curNodeRT = null;

		curPathConnectionIdx = 0;
		ChangePath(0);
		if (blinkingCo != null)
			StopCoroutine(blinkingCo);
		blinkingCo = StartCoroutine(Blinking());

		F.I.move2Ref.action.performed += ChangePath;
		// modified base OnEnable
		F.I.move2Ref.action.performed += CalculateTargetToSelect;
		if (loadCo)
			StopCoroutine(Load());
		StartCoroutine(Load(F.I.curVariant.nodes[F.I.curNode.connections[0]].trackName, forceReload: true));

		WriteContinuationText();
		
	}
	void UpdateObjectivesTable()
	{
		if(F.I.targetNode.prizeReqs.Length == 0)
		{
			objectivesRecordsTitle.text = F.I.LocStr("RECORDS");
			objectivesContainer.gameObject.SetActive(false);
			recordsContainer.gameObject.SetActive(true);
		}
		else
		{
			objectivesRecordsTitle.text = F.I.LocStr("OBJECTIVES");
			objectivesContainer.gameObject.SetActive(true);
			recordsContainer.gameObject.SetActive(false);

			for (int i = 0; i < objectivesContainer.childCount; ++i)
			{
				var text = objectivesContainer.GetChild(i).GetComponent<TextMeshProUGUI>();
				if (i < F.I.targetNode.prizeReqs.Length)
				{
					bool achievedThisPrize = F.I.curVariant.progress.prizesCompleted[F.I.targetArcadeNodeID].GetBits(i) == 1;
					text.text = GetObjectiveText(F.I.targetNode.prizeReqs[i]);
					if (achievedThisPrize)
					{
						text.text = "<s>" + text.text + "</s>";
						text.color = Color.gray;
					}
					else
					{
						text.color = Color.white;
					}
					text.gameObject.SetActive(true);
				}
				else
				{
					text.gameObject.SetActive(false);
				}
			}
		}
	}
	IEnumerator AlignMapToCurrentNodes()
	{
		// make sure current node and target node are visible in viewport
		var contentRT = nodeParent.parent.GetComponent<RectTransform>();
		var viewportDims = contentRT.sizeDelta;
		var focusObj = targetNodeRT;
		// first move contentRT so focusObj is visible in viewport
		float newContentX = contentRT.anchoredPosition.x;
		float newContentY = contentRT.anchoredPosition.y;
		var focusPos = focusObj.anchoredPosition + contentRT.anchoredPosition;
		if (focusPos.x < 0)
			newContentX += -focusPos.x + 100;
		else if (focusPos.x > viewportDims.x)
			newContentX -= focusPos.x - viewportDims.x + 100;
		if (focusPos.y < 0)
			newContentY += -focusPos.y + 100;
		else if (focusPos.y > viewportDims.y)
			newContentY -= focusPos.y - viewportDims.y + 100;


		Vector2 target = new Vector2(newContentX, newContentY);
		Vector2 beginPos = contentRT.anchoredPosition;
		float timer = 0;
		while(timer < 1)
		{
			contentRT.anchoredPosition = Vector2.Lerp(beginPos,target,F.EasingOutQuint(timer));
			timer += Time.deltaTime;
			yield return null;
		}
		contentRT.anchoredPosition = target;
	}
	void ChangePath(InputAction.CallbackContext context)
	{
		if (curNodeRT == null)
			return;
		Vector2 move2 = F.I.move2Ref.action.ReadValue<Vector2>();
		int x = Mathf.RoundToInt(move2.x);
		int dir = x > 0 ? 1 : -1;
		ChangePath(dir);
	}
	void ChangePath(int dir)
	{
		if(F.I.curArcadeNodeID == -1)
		{
			curPathConnectionIdx = 0;
		}
		else
		{
			
			if (selectedPath != null)
			{
				selectedPath.color = Color.gray;
				selectedPath.gameObject.SetActive(selectedPath.gameObject.CompareTag(Info.alreadyWalkedTag));
			}
			targetNodeRT.GetComponent<Image>().color = F.I.curVariant.nodes[F.I.targetArcadeNodeID].color;
			curPathConnectionIdx = Mathf.Clamp(curPathConnectionIdx + dir, 0, F.I.curNode.connections.Length - 1);
			var targetNodeID = F.I.curNode.connections[curPathConnectionIdx];
			selectedPath = pathsRTs[F.I.curArcadeNodeID + "-" + targetNodeID].GetComponent<Image>();
			targetNodeRT = nodeParent.GetChild(targetNodeID).GetComponent<RectTransform>();
			F.I.targetArcadeNodeID = targetNodeID;

			if (blinkingCo != null)
				StopCoroutine(blinkingCo);
			blinkingCo = StartCoroutine(Blinking());
		}
		if (alignCo != null)
			StopCoroutine(alignCo);

		alignCo = StartCoroutine(AlignMapToCurrentNodes());
		UpdateObjectivesTable();
	}
	IEnumerator Blinking()
	{
		float timer = 0;
		selectedPath?.gameObject.SetActive(true);
		var Color = targetNodeRT.GetComponent<Image>().color;
		var img = targetNodeRT.GetComponent<Image>();
		while (true)
		{
			if (selectedPath)
				selectedPath.color = new Color(1, 0, 0, Mathf.Abs(Mathf.Sin(timer * 2 * Mathf.PI)));
			img.color = new Color(Color.r, Color.g, Color.b, Mathf.Abs(Mathf.Sin(timer * 2 * Mathf.PI)));
			timer += Time.unscaledDeltaTime;
			yield return null;
		}
	}
	void CreateNodeMap()
	{
		foreach (var nodeData in F.I.curVariant.nodes)
		{ // create nodes
			var position = new Vector3(80 + 110 * nodeData.coords.x, 50 + 133.34f * nodeData.coords.y, 0);
			var newNode = Instantiate(nodePrefab, position, Quaternion.identity, nodeParent);
			newNode.name = nodeData.id.ToString();
			newNode.GetComponent<Image>().color = nodeData.color;
			newNode.GetComponent<RectTransform>().sizeDelta = 50 * nodeData.size * Vector2.one;
			newNode.GetComponent<RectTransform>().anchoredPosition = position;
		}
		foreach (var nodeData in F.I.curVariant.nodes)
		{
			if (nodeData.connections == null)
				continue;
			foreach (var nodeB_ID in nodeData.connections)
			{
				var nodeA_IMG = nodeParent.GetChild(nodeData.id).GetComponent<Image>();
				var nodeA_RT = nodeA_IMG.GetComponent<RectTransform>();
				var nodeB_IMG = nodeParent.GetChild(nodeB_ID).GetComponent<Image>();
				var nodeB_RT = nodeB_IMG.GetComponent<RectTransform>();
				var posA = nodeA_RT.anchoredPosition;
				var posB = nodeB_RT.anchoredPosition;
				var dir = (posB - posA).normalized;
				var dist = Vector2.Distance(posA, posB);
				var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
				var newPath = Instantiate(pathPrefab, Vector3.zero, Quaternion.Euler(0, 0, angle), pathParent);
				newPath.name = nodeA_RT.name + "-" + nodeB_RT.name;
				var rt = newPath.GetComponent<RectTransform>();
				pathsRTs.Add(newPath.name, rt);
				var nodeARadius = nodeA_IMG.sprite.rect.width / 2;
				var nodeBRadius = nodeB_IMG.sprite.rect.width / 2;
				rt.sizeDelta = new Vector2(dist - nodeARadius - nodeBRadius, rt.sizeDelta.y);
				rt.anchoredPosition = (posA + posB) / 2f;
				rt.GetComponent<Image>().color = Color.gray;
				bool pathAlreadyWalked = F.I.curVariant.progress.pathsDone[nodeData.id].Contains(nodeB_ID);
				newPath.SetActive(pathAlreadyWalked);
				if (pathAlreadyWalked)
					newPath.tag = Info.alreadyWalkedTag;
			}
		}
		for (int i = 0; i < F.I.curVariant.progress.prizesCompleted.Length; i++)
		{ // make node square if locked
			if(F.I.curVariant.nodes[i].prizeReqs != null)
			{
				if (F.I.curVariant.progress.prizesCompleted[i].CountBits() < F.I.curVariant.nodes[i].prizeReqs.Length)
				{
					nodeParent.GetChild(i).GetComponent<Image>().sprite = squareRounded;
				}
			}
		}
		for (int i = 0; i < F.I.curVariant.progress.pathsDone.Count; i++)
		{
			for (int j = 0; j < F.I.curVariant.progress.pathsDone[i].Count; j++)
			{// have driven on this node
				pathsRTs[i + "-" + F.I.curVariant.progress.pathsDone[i][j]].gameObject.SetActive(true);
			}
		}
	}
	public void SetArcadeConditions()
	{
		var node = F.I.targetNode;
		F.I.s_roadType = node.pavementType;
		F.I.randomPavement = false;
		F.I.s_cpuLevel = CpuLevel.Hard;
		F.I.s_timeOfDay = (TimeOfDay)Random.Range(0,Info.TimeOfDays);
		F.I.s_laps = node.laps;
		F.I.s_raceType = node.raceType;
		F.I.s_cpuRivals = node.cars.Length;
		F.I.s_trackName = node.trackName;
		F.I.catchup = false;
		F.I.s_PlayerCarSponsor = F.I.cars[F.I.s_playerCarIdx].defaultLivery;

		foreach(var conds in node.prizeReqs)
		{
			if(conds.condition == ArcadeVariant.Prize.Condition.ExactStunts)
			{
				var split = conds.conditionArgument.Split(',');
				F.I.arcadeObjectiveStunts = new (string, int)[split.Length / 2];
				for (int i = 0; i < split.Length / 2; i++)
				{
					F.I.arcadeObjectiveStunts[i] = (split[2*i], int.Parse(split[2*i + 1]));
				}
			}
		}
	}
	void WriteContinuationText()
	{
		var req = F.I.curNode.continuationReq;
		arcadeReqText.text = GetObjectiveText(req);
	}
	string GetObjectiveText(ArcadeVariant.Prize req)
	{
		string text;
		switch (req.condition)
		{
			case ArcadeVariant.Prize.Condition.PositionAtLeast:
				text = F.I.LocStr("Finish at least ") + F.I.LocStr(F.PosSuffix(int.Parse(req.conditionArgument)-1));
				break;
			case ArcadeVariant.Prize.Condition.LapAtMost:
				text = string.Format(F.I.LocStr("Do a lap faster than {0}"), req.conditionArgument);
				break;
			case ArcadeVariant.Prize.Condition.StarsAtLeast:
				text = string.Format(F.I.LocStr("Get at least {0} aero stars"), req.conditionArgument);
				break;
			case ArcadeVariant.Prize.Condition.StuntAtLeast:
				text = string.Format(F.I.LocStr("Get at least {0} aeromiles"), req.conditionArgument);
				break;
			case ArcadeVariant.Prize.Condition.DriftsAtLeast:
				text = string.Format(F.I.LocStr("Get at least {0} drift points"), req.conditionArgument);
				break;
			case ArcadeVariant.Prize.Condition.TimeAtMost:
				text = string.Format(F.I.LocStr("Finish the race in less than {0}"), req.conditionArgument);
				break;
			case ArcadeVariant.Prize.Condition.FastestLaptime:
				text = F.I.LocStr("Set the fastest lap time");
				break;
			case ArcadeVariant.Prize.Condition.AlwaysFirst:
				text = F.I.LocStr("Always finish in first place");
				break;
			case ArcadeVariant.Prize.Condition.AllPathsFound:
				text = F.I.LocStr("Find all hidden paths");
				break;
			case ArcadeVariant.Prize.Condition.ExactStunts:
				{
					var split = req.conditionArgument.Split(',');
					text = string.Format(F.I.LocStr("Perform {0} at least {1} times"), F.I.LocStr(split[0]), split[1]);
					for (int i = 2; i < split.Length; i += 2)
					{
						text += string.Format(", {0} - {1}x", F.I.LocStr(split[i]), split[i+1]);
					}
				}
				break;
			default:
				text = "";
				break;
		}
		return text;
	}
}
