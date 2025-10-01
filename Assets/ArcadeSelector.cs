using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ArcadeSelector : TrackSelectorTemplate
{
	public static ArcadeSelector I;
	public MainMenuView thisView;
	public Transform nodeParent;
	public Transform pathParent;
	public GameObject nodePrefab;
	public GameObject pathPrefab;
	public Sprite squareRounded;
	public Sprite circle;
	public Text arcadeReqText;
	Coroutine blinkingCo;
	RectTransform curNodeRT;
	RectTransform targetNodeRT;
	Image selectedPath;
	int curPathConnectionIdx;
	Dictionary<string, RectTransform> pathsRTs = new();
	private new void Awake()
	{
		base.Awake();
		I = this;
	}
	public void Reset()
	{
		F.I.curArcadeNodeID = 0;
	}
	private new void OnDisable()
	{
		F.I.move2Ref.action.performed -= ChangePath;

		if (blinkingCo != null)
			StopCoroutine(blinkingCo);
		targetNodeRT.GetComponent<Image>().color = F.I.curVariant.nodes[curPathConnectionIdx].color;

		if(F.I.curArcadeNodeID != -1)
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
		if(F.I.curArcadeNodeID >= 0)
			pathsRTs[F.I.curArcadeNodeID + "-" + F.I.targetArcadeNodeID].tag = Info.alreadyWalkedTag;

		targetNodeRT.GetComponent<Image>().sprite = circle;
		F.I.curArcadeNodeID = targetNodeRT.GetSiblingIndex();
		F.I.targetArcadeNodeID = F.I.curNode.connections[0];
	}
	protected override void OnEnable()
	{
		F.I.move2Ref.action.performed += ChangePath;

		if (F.I.curArcadeNodeID == -1) // when starting new arcade variant
		{
			F.I.targetArcadeNodeID = F.I.curVariant.starts.FirstOrDefault(s => s.allowedCarsIdxs.Any(carIdx => carIdx == F.I.s_playerCarIdx)).node;
			CreateNodeMap();
		}
		targetNodeRT = nodeParent.GetChild(F.I.targetArcadeNodeID).GetComponent<RectTransform>();
		
		if(F.I.curArcadeNodeID >= 0)
			curNodeRT = nodeParent.GetChild(F.I.curArcadeNodeID).GetComponent<RectTransform>();
		else
			curNodeRT = null;

		ChangePath(0);
		if (blinkingCo != null)
			StopCoroutine(blinkingCo);
		blinkingCo = StartCoroutine(Blinking());

		// modified base OnEnable
		F.I.move2Ref.action.performed += CalculateTargetToSelect;
		if (loadCo)
			StopCoroutine(Load());
		StartCoroutine(Load(forceReload:true));

		WriteContinuationText();
	}
	void AlignMapToCurrentNodes()
	{
		// make sure current node and target node are visible in viewport
		var contentRT = nodeParent.GetComponent<RectTransform>();
		var viewportDims = nodeParent.GetComponent<RectTransform>().sizeDelta;
		var focusObj = curNodeRT == null ? targetNodeRT : pathsRTs[F.I.curArcadeNodeID + "-" + F.I.targetArcadeNodeID];
		// first move contentRT so focusObj is visible in viewport
		float newContentX = contentRT.anchoredPosition.x;
		float newContentY = contentRT.anchoredPosition.y;
		var focusPos = focusObj.anchoredPosition + contentRT.anchoredPosition;
		if (focusPos.x < 0)
			newContentX += -focusPos.x + 20;
		else if (focusPos.x > viewportDims.x)
			newContentX -= focusPos.x - viewportDims.x + 20;
		if (focusPos.y < 0)
			newContentY += -focusPos.y + 20;
		else if (focusPos.y > viewportDims.y)
			newContentY -= focusPos.y - viewportDims.y + 20;
		// then clamp contentRT to not go out of bounds
		//newContentX = Mathf.Clamp(newContentX, -contentRT.sizeDelta.x + viewportDims.x, 0);
		//newContentY = Mathf.Clamp(newContentY, -contentRT.sizeDelta.y + viewportDims.y, 0);

		contentRT.anchoredPosition = new Vector2(newContentX, newContentY);
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
			if(blinkingCo != null)
				StopCoroutine(blinkingCo);
			if (selectedPath != null)
			{
				selectedPath.color = Color.gray;
				selectedPath.gameObject.SetActive(selectedPath.gameObject.CompareTag(Info.alreadyWalkedTag));
			}
			targetNodeRT.GetComponent<Image>().color = F.I.curVariant.nodes[curPathConnectionIdx].color;
			curPathConnectionIdx = Mathf.Clamp(curPathConnectionIdx + dir, 0, F.I.curNode.connections.Length - 1);
			var targetNodeID = F.I.curNode.connections[curPathConnectionIdx];
			selectedPath = pathsRTs[F.I.curArcadeNodeID + "-" + targetNodeID].GetComponent<Image>();
			targetNodeRT = nodeParent.GetChild(targetNodeID).GetComponent<RectTransform>();
			F.I.targetArcadeNodeID = targetNodeID;
			blinkingCo = StartCoroutine(Blinking());
		}
		AlignMapToCurrentNodes();
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
		F.DestroyAllChildren(nodeParent);
		F.DestroyAllChildren(pathParent);
		pathsRTs.Clear();
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
				newPath.SetActive(false);
			}
		}
		for (int i = 0; i < F.I.curVariant.progress.unlockedNodes.Length; i++)
		{ // make node square if locked
			if (!F.I.curVariant.progress.unlockedNodes[i])
			{
				nodeParent.GetChild(i).GetComponent<Image>().sprite = squareRounded;
			}
		}
		for (int i = 0; i < F.I.curVariant.progress.unlockedPaths.Count; i++)
		{ // make visible if unlocked
			for (int j = 0; j < F.I.curVariant.progress.unlockedPaths[i].Count; j++)
			{// have driven on this node
				pathsRTs[i + "-" + F.I.curVariant.progress.unlockedPaths[i][j]].gameObject.SetActive(true);
			}
		}
	}
	public void SetArcadeConditions()
	{
		F.I.s_roadType = F.I.curNode.pavementType;
		F.I.s_cpuLevel = F.I.curNode.cpuLevel;
		F.I.s_timeOfDay = F.I.curNode.timeOfDay;
		F.I.s_laps = F.I.curNode.laps;
		F.I.s_raceType = F.I.curNode.raceType;
		if (F.I.curNode.cars == null)
			F.I.s_cpuRivals = 5;
		else
			F.I.s_cpuRivals = F.I.curNode.cars.Length;
		F.I.s_trackName = F.I.curNode.trackName;
		F.I.catchup = true;
	}
	void WriteContinuationText()
	{
		var req = F.I.curNode.continuationReq;
		switch (req.condition)
		{
			case ArcadeVariant.Prize.Condition.PositionAtLeast:
				arcadeReqText.text = string.Format(F.I.LocStr("Finish at least in {0}. place!"), req.conditionArgument);
				break;
			case ArcadeVariant.Prize.Condition.LapAtMost:
				arcadeReqText.text = string.Format(F.I.LocStr("Do a lap faster than {0}!"), req.conditionArgument);
				break;
			case ArcadeVariant.Prize.Condition.AeroStarsAtLeast:
				arcadeReqText.text = string.Format(F.I.LocStr("Get at least {0} aero stars!"), req.conditionArgument);
				break;
			case ArcadeVariant.Prize.Condition.StuntAtLeast:
				arcadeReqText.text = string.Format(F.I.LocStr("Get at least {0} aeromiles!"), req.conditionArgument);
				break;
			case ArcadeVariant.Prize.Condition.DriftsAtLeast:
				arcadeReqText.text = string.Format(F.I.LocStr("Get at least {0} drift points!"), req.conditionArgument);
				break;
			case ArcadeVariant.Prize.Condition.TimeAtMost:
				arcadeReqText.text = string.Format(F.I.LocStr("Finish the race in less than {0}!"), req.conditionArgument);
				break;
			case ArcadeVariant.Prize.Condition.FastestLaptime:
				arcadeReqText.text = F.I.LocStr("Set the fastest lap time!");
				break;
			case ArcadeVariant.Prize.Condition.AlwaysFirst:
				arcadeReqText.text = F.I.LocStr("Always finish in first place!");
				break;
			case ArcadeVariant.Prize.Condition.AllPathsFound:
				arcadeReqText.text = F.I.LocStr("Find all hidden paths!");
				break;
			default:
				break;
		}
	}

}
