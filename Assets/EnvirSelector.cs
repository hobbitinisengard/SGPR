using RVP;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using UnityEngine.InputSystem;

public class EnvirSelector : Selector
{
	private enum SortingCond { Difficulty, Name };
	public TextMeshProUGUI envirDescText;
	public RectTransform envirContent;
	public Scrollbar scrollx;
	public Scrollbar scrolly;
	public MainMenuButton startButton;
	public Image tile;
	Transform selectedEnvir;
	int persistentSelectedEnvir = 0;
	Coroutine containerCo;

	private void OnDisable()
	{
		move2Ref.action.started -= CalculateTargetToSelect;
	}
	private void OnEnable()
	{
		move2Ref.action.started += CalculateTargetToSelect;
		startButton.Select();
		selectedEnvir = envirContent.GetChild(0).GetChild(persistentSelectedEnvir);
		Info.s_trackName = selectedEnvir.name;
		if (containerCo != null)
			StopCoroutine(containerCo);
		envirDescText.text = Info.EnvirDescs[selectedEnvir.GetSiblingIndex()];
		containerCo = StartCoroutine(MoveToEnvir());
		Info.s_inEditor = true;
	}

	void SetTile()
	{
		tile.sprite = Info.icons.First(i => i.name == selectedEnvir.name);
	}
	void CalculateTargetToSelect(InputAction.CallbackContext ctx)
	{
		if (!selectedEnvir)
			return;
		Vector2 move2 = move2Ref.action.ReadValue<Vector2>();
		int x = Mathf.RoundToInt(move2.x);
		if (x != 0)
		{
			int posx = x + selectedEnvir.GetSiblingIndex();
			if (posx < 0)
				posx = 0;
			if (posx >= 0)
			{
				Transform tempSelectedEnvir;
				Transform selectedClass = envirContent.GetChild(0);

				if (posx >= selectedClass.childCount)
					posx = selectedClass.childCount - 1;
				tempSelectedEnvir = selectedClass.GetChild(posx);
				
				if (tempSelectedEnvir != null && tempSelectedEnvir != selectedEnvir)
				{
					selectedEnvir = tempSelectedEnvir;
					Info.s_trackName = selectedEnvir.name;
					Debug.Log(selectedEnvir);
					PlaySFX("fe-bitmapscroll");
				}
				// new track has been selected
				// set description
				envirDescText.text = Info.EnvirDescs[selectedEnvir.GetSiblingIndex()];
				SetTile();
				// focus on track
				if (containerCo != null)
					StopCoroutine(containerCo);
				containerCo = StartCoroutine(MoveToEnvir());
			}
		}
	}
	IEnumerator MoveToEnvir()
	{
		yield return null;
		if (!selectedEnvir)
			yield break;
		float timer = 0;
		Vector2 initPos = envirContent.anchoredPosition;
		Vector2 targetPos = new Vector2(-((RectTransform)selectedEnvir).anchoredPosition.x,
			-selectedEnvir.parent.GetComponent<RectTransform>().anchoredPosition.y);
		Vector2 scrollInitPos = new Vector2(scrollx.value, scrolly.value);
		Vector2 scrollInitSize = new Vector2(scrollx.size, scrolly.size);
		float envirInGroupPos = Info.InGroupPos(selectedEnvir);
		float groupPos = envirContent.PosAmongstActive(selectedEnvir.parent, false);
		Vector2 scrollTargetPos = new Vector2(envirInGroupPos, groupPos);
		Vector2 scrollTargetSize = new Vector2(1f / selectedEnvir.parent.ActiveChildren(), 1f / envirContent.ActiveChildren());

		while (timer < 1)
		{
			float step = F.EasingOutQuint(timer);
			envirContent.anchoredPosition = Vector2.Lerp(initPos, targetPos, step);
			scrollx.value = Mathf.Lerp(scrollInitPos.x, scrollTargetPos.x, step);
			scrolly.value = Mathf.Lerp(scrollInitPos.y, scrollTargetPos.y, step);
			scrollx.size = Mathf.Lerp(scrollInitSize.x, scrollTargetSize.x, step);
			scrolly.size = Mathf.Lerp(scrollInitSize.y, scrollTargetSize.y, step);
			timer += Time.deltaTime;

			yield return null;
		}
	}
}
