using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
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
	public Text arcadeReqText;
	string curVariantName;
	Coroutine blinkingCo;
	RectTransform curNodeRT;
	RectTransform targetNodeRT;
	Image selectedPath;
	int curPathConnectionIdx;
	Dictionary<string, RectTransform> pathsRTs = new();
	public int TargetNodeID => int.Parse(targetNodeRT.name);
	private new void Awake()
	{
		base.Awake();
		I = this;
	}
	private new void OnDisable()
	{
		F.I.move2Ref.action.performed -= ChangePath;
		base.OnDisable();
		if (blinkingCo != null)
			StopCoroutine(blinkingCo);
		var nodeTR = nodeParent.GetChild(F.I.curArcadeNodeID);
		nodeTR.GetComponent<Image>().color = F.I.curVariant.nodes[F.I.curArcadeNodeID].color;

		foreach (var targetID in F.I.curVariant.nodes[F.I.curArcadeNodeID].connections)
		{
			pathsRTs[F.I.curArcadeNodeID + "-" + targetID].GetComponent<Image>().color = Color.gray;
		}
	}
	protected override void OnEnable()
	{
		F.I.curArcadeNodeID = TargetNodeID;
		F.I.move2Ref.action.performed += ChangePath;

		base.OnEnable();
		if (curVariantName != F.I.playerData.currentArcadeVariant)
		{
			F.I.curVariant = F.I.arcadeVariants.Where(v => v.name == F.I.playerData.currentArcadeVariant).FirstOrDefault();
			CreateNodeMap();
		}
		WriteContinuationText();

		curNodeRT = nodeParent.GetChild(F.I.curArcadeNodeID).GetComponent<RectTransform>();
		ChangePath(0);
		if (blinkingCo != null)
			StopCoroutine(blinkingCo);
		StartCoroutine(Blinking());
	}
	void AlignMapToCurrentNodes()
	{
		// make sure current node and target node are visible in viewport
		var contentRT = nodeParent.parent.GetComponent<RectTransform>();
		var viewportDims = nodeParent.parent.GetComponent<RectTransform>().sizeDelta;
		float newContentX = 0, newContentY = 0;
		if (curNodeRT.anchoredPosition.x < 0 || curNodeRT.anchoredPosition.x > viewportDims.x
			|| targetNodeRT.anchoredPosition.x < 0 || targetNodeRT.anchoredPosition.x > viewportDims.x)
		{ // if either node is out of viewport horizontally
			newContentX = -Mathf.Min(curNodeRT.anchoredPosition.x, targetNodeRT.anchoredPosition.x)
				+ viewportDims.x / 2;
			newContentX = Mathf.Clamp(newContentX, -contentRT.rect.width + viewportDims.x, 0);
		}
		if (curNodeRT.anchoredPosition.y < 0 || curNodeRT.anchoredPosition.y > viewportDims.y
			|| targetNodeRT.anchoredPosition.y < 0 || targetNodeRT.anchoredPosition.y > viewportDims.y)
		{ // if either node is out of viewport vertically
			newContentY = -Mathf.Min(curNodeRT.anchoredPosition.y, targetNodeRT.anchoredPosition.y)
				+ viewportDims.y / 2;
			newContentY = Mathf.Clamp(newContentY, -contentRT.rect.height + viewportDims.y, 0);
		}
		contentRT.anchoredPosition = new Vector2(newContentX, newContentY);
	}
	private void ChangePath(InputAction.CallbackContext context)
	{
		Vector2 move2 = F.I.move2Ref.action.ReadValue<Vector2>();
		int x = Mathf.RoundToInt(move2.x);
		int dir = x > 0 ? 1 : -1;
		ChangePath(dir);
	}
	void ChangePath(int dir)
	{
		selectedPath.color = Color.gray;
		curPathConnectionIdx = (curPathConnectionIdx + dir) % F.I.curVariant.nodes[F.I.curArcadeNodeID].connections.Length;
		var targetNodeID = F.I.curVariant.nodes[F.I.curArcadeNodeID].connections[curPathConnectionIdx];
		selectedPath = pathsRTs[F.I.curArcadeNodeID + "-" + targetNodeID].GetComponent<Image>();
		targetNodeRT = nodeParent.GetChild(targetNodeID).GetComponent<RectTransform>();
		AlignMapToCurrentNodes();
	}
	IEnumerator Blinking()
	{
		float timer = 0;
		while (true)
		{
			selectedPath.color = new Color(1, 0, 0, Mathf.Abs(Mathf.Sin(timer * 2 * Mathf.PI)));
			timer += Time.unscaledDeltaTime;
			yield return null;
		}
	}
	void CreateNodeMap()
	{
		F.DestroyAllChildren(nodeParent);
		F.DestroyAllChildren(pathParent);
		pathsRTs.Clear();
		curVariantName = F.I.playerData.currentArcadeVariant;
		foreach (var nodeData in F.I.curVariant.nodes)
		{ // create nodes
			var position = new Vector3(80 + 110 * nodeData.coords.x, 50 + 133.34f * nodeData.coords.y, 0);
			var newNode = Instantiate(nodePrefab, position, Quaternion.identity, nodeParent);
			newNode.name = nodeData.id.ToString();
			newNode.GetComponent<Image>().color = nodeData.color;
			newNode.GetComponent<RectTransform>().sizeDelta = Vector2.one * nodeData.size;
		}
		foreach (var nodeData in F.I.curVariant.nodes)
		{
			if (nodeData.connections == null || nodeData.connections.Length == 0)
				continue;
			foreach (var nodeB_ID in nodeData.connections)
			{
				var nodeAIMG = nodeParent.GetChild(nodeData.id).GetComponent<Image>();
				var nodeA_RT = nodeAIMG.GetComponent<RectTransform>();
				var nodeBIMG = nodeParent.GetChild(nodeB_ID).GetComponent<Image>();
				var nodeB_RT = nodeBIMG.GetComponent<RectTransform>();
				var posA = nodeA_RT.anchoredPosition;
				var posB = nodeB_RT.anchoredPosition;
				var dir = (posB - posA).normalized;
				var dist = Vector3.Distance(posA, posB);
				var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
				var newPath = Instantiate(pathPrefab, (posA + posB) / 2, Quaternion.Euler(0, 0, angle), pathParent);
				newPath.name = nodeA_RT.name + "-" + nodeB_RT.name;
				var rt = newPath.GetComponent<RectTransform>();
				pathsRTs.Add(newPath.name, rt);
				var nodeARadius = nodeAIMG.sprite.rect.width * nodeA_RT.sizeDelta.x / 2 / nodeAIMG.pixelsPerUnit;
				var nodeBRadius = nodeBIMG.sprite.rect.width * nodeB_RT.sizeDelta.x / 2 / nodeBIMG.pixelsPerUnit;
				rt.sizeDelta = new Vector2(dist - nodeARadius - nodeBRadius, rt.sizeDelta.y);
				rt.GetComponent<Image>().color = Color.gray;
				newPath.SetActive(false);
			}
		}
		for (int i = 1; i < F.I.curVariant.progress.unlockedPaths.Count; i++)
		{ // make visible if unlocked
			// [] - we haven't driven on this node yet
			// [0] - we have driven on this node, but haven't unlocked any paths
			// [0,1,4...] - we have driven on this node and unlocked paths to nodes 1,4...
			// no node can have id = 0
			if (F.I.curVariant.progress.unlockedPaths[i].Count == 0)
			{
				nodeParent.GetChild(i).GetComponent<Image>().sprite = squareRounded;
				continue;
			}
			
			for (int j = 1; j < F.I.curVariant.progress.unlockedPaths[i].Count; j++)
			{// have driven on this node
				pathsRTs[i + "-" + F.I.curVariant.progress.unlockedPaths[i][j]].gameObject.SetActive(true);
			}
		}
	}
	public void SetArcadeConditions()
	{
		F.I.s_roadType = F.I.curVariant.nodes[F.I.curArcadeNodeID].pavementType;
		F.I.s_cpuLevel = F.I.curVariant.nodes[F.I.curArcadeNodeID].cpuLevel;
		F.I.s_timeOfDay = F.I.curVariant.nodes[F.I.curArcadeNodeID].timeOfDay;
		F.I.s_laps = F.I.curVariant.nodes[F.I.curArcadeNodeID].laps;
		F.I.s_raceType = F.I.curVariant.nodes[F.I.curArcadeNodeID].raceType;
		if (F.I.curVariant.nodes[F.I.curArcadeNodeID].cars == null)
			F.I.s_cpuRivals = 5;
		else
			F.I.s_cpuRivals = F.I.curVariant.nodes[F.I.curArcadeNodeID].cars.Length;
		F.I.s_trackName = F.I.curVariant.nodes[F.I.curArcadeNodeID].trackName;
	}
	void WriteContinuationText()
	{
		var req = F.I.curVariant.nodes[F.I.curArcadeNodeID].continuationReq;
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
