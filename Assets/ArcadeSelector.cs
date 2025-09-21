using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ArcadeSelector : TrackSelectorTemplate
{
	public Transform nodesParent;
	public Transform map;
	public GameObject nodePrefab;
	public GameObject pathPrefab;
	public Sprite squareRounded;
	string curVariantName;
	Coroutine blinkingCo;
	Image selectedPath;
	int selectedPathChild;
	private new void OnDisable()
	{
		F.I.move2Ref.action.performed -= ChangePath;
		base.OnDisable();
		if (blinkingCo != null)
			StopCoroutine(blinkingCo);
		var nodeTR = nodesParent.GetChild(F.I.curArcadeNodeID);
		nodeTR.GetComponent<Image>().color = F.I.curVariant.nodes[F.I.curArcadeNodeID].color;
		for (int i = 0; i < nodeTR.childCount; i++)
			nodeTR.GetChild(i).GetComponent<Image>().color = Color.gray;
	}

	private void ChangePath(InputAction.CallbackContext context)
	{
		Vector2 move2 = F.I.move2Ref.action.ReadValue<Vector2>();
		int x = Mathf.RoundToInt(move2.x);
		int dir = x > 0 ? 1 : -1;
		selectedPath.color = Color.gray;
		var curNode = nodesParent.Find(F.I.curArcadeNodeID.ToString());
		var selectedPathTR = curNode.GetChild((selectedPathChild+dir) % curNode.childCount);
	}

	protected override void OnEnable()
	{
		F.I.move2Ref.action.performed += ChangePath;

		base.OnEnable();
		if (curVariantName != F.I.playerData.currentArcadeVariant)
		{
			F.I.curVariant = F.I.arcadeVariants.Where(v => v.name == F.I.playerData.currentArcadeVariant).FirstOrDefault();
			CreateNodeMap();
		}
		var nodeTR = nodesParent.GetChild(F.I.curArcadeNodeID).GetComponent<RectTransform>();
		if (blinkingCo != null)
			StopCoroutine(blinkingCo);
		StartCoroutine(Blinking());
	}
	IEnumerator Blinking()
	{
		var nodeTR = nodesParent.GetChild(F.I.curArcadeNodeID).GetComponent<RectTransform>();
		var img = nodeTR.GetComponent<Image>();
		float timer = 0;
		while (true)
		{
			img.color = new Color(img.color.r, img.color.g, img.color.b, Mathf.Abs(Mathf.Sin(timer * 2 * Mathf.PI)));
			selectedPath.color = new Color(1, 0, 0, Mathf.Abs(Mathf.Sin(timer * 2 * Mathf.PI)));
			timer += Time.unscaledDeltaTime;
			yield return null;
		}
	}
	void CreateNodeMap()
	{
		F.DestroyAllChildren(nodesParent);

		foreach (var nodeData in F.I.curVariant.nodes)
		{
			var position = new Vector3(80 + 110 * nodeData.coords.x, 50 + 133.34f * nodeData.coords.y, 0);
			var newNode = Instantiate(nodePrefab, position, Quaternion.identity, nodesParent);
			newNode.name = nodeData.id.ToString();
			var img = newNode.GetComponent<Image>();
			img.color = nodeData.color;
			newNode.GetComponent<RectTransform>().sizeDelta = Vector2.one * nodeData.size;
		}
		for (int i = 0; i < F.I.curVariant.progress.unlockedPaths.Length; i++)
		{
			if (F.I.curVariant.progress.unlockedPaths[i].Length == 0)
			{// haven't driven on this node yet
				nodesParent.GetChild(i).GetComponent<Image>().sprite = squareRounded;
				continue;
			}
			for (int j = 1; j < F.I.curVariant.progress.unlockedPaths[i].Length; j++)
			{// have driven on this node
				var nodeAID = F.I.curVariant.nodes[i].id;
				var nodeBID = F.I.curVariant.nodes[F.I.curVariant.progress.unlockedPaths[i][j]].id;
				var nodeAIMG = nodesParent.GetChild(nodeAID).GetComponent<Image>();
				var nodeBIMG = nodesParent.GetChild(nodeBID).GetComponent<Image>();
				var posA = nodeAIMG.transform.position;
				var posB = nodeBIMG.transform.position;
				var dir = (posB - posA).normalized;
				var dist = Vector3.Distance(posA, posB);
				var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
				var nodeTR = nodesParent.GetChild(nodeAID).transform;
				var newPath = Instantiate(pathPrefab, (posA + posB) / 2, Quaternion.Euler(0, 0, angle), nodeTR);
				newPath.name = nodeBIMG.transform.name;
				var rt = newPath.GetComponent<RectTransform>();
				rt.sizeDelta = new Vector2(dist, 20);
				if (Mathf.Abs(angle) > 45)
					rt.sizeDelta = new Vector2(20, dist);
			}
		}
	}
	

}
