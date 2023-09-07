using RVP;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Experimental.Animations;
using UnityEngine.UIElements.Experimental;

public class CarSelector : MonoBehaviour
{
	public RectTransform[] bars;
	public Text carDescText;
	public Transform buttonsContainer;
	public RectTransform carContent;
	public Scrollbar scrollx;
	public Scrollbar scrolly;
	RectTransform selectedCar;
	float initBarSizeDelta;
	Coroutine barsCo;
	Coroutine containerCo;
	public bool d_co;
	void Awake()
	{
		initBarSizeDelta = bars[0].sizeDelta.x;
		Debug.Log(initBarSizeDelta);
		//carImgDims = carContent.GetChild(0).GetChild(0).GetComponent<RectTransform>().rect.size;
	}
	private void OnEnable()
	{
		bool[] menuButtons = new bool[4];
		for (int i = 0; i < carContent.childCount; ++i)
		{
			Transform carClass = carContent.GetChild(i);
			for (int j = 0; j < carClass.childCount; ++j)
			{
				Transform carxx = carClass.GetChild(j);
				// show only unlocked cars. I can add more conditions to show cars
				bool conditionOK = Info.cars[carxx.name].unlocked;
				carxx.gameObject.SetActive(conditionOK);
				if (selectedCar == null && conditionOK)
					selectedCar = (RectTransform)carxx;
				// if we don't have any car of this class registered
				if (!menuButtons[(int)Info.cars[carxx.name].carClass])
					menuButtons[(int)Info.cars[carxx.name].carClass] = conditionOK;
			}
		}
		// set description
		carDescText.text = Info.cars[selectedCar.name].desc;
		// set bars
		barsCo = StartCoroutine(SetPerformanceBars());

		// set buttons
		for (int i = 0; i < buttonsContainer.childCount; ++i)
		{
			buttonsContainer.GetChild(i).gameObject.SetActive(menuButtons[i]);

			if ((int)Info.cars[selectedCar.name].carClass == i)
				StartCoroutine(SelectBtn(buttonsContainer.GetChild(i)));
		}
		containerCo = StartCoroutine(MoveToCar());

		// focus on car
		carContent.anchoredPosition = new Vector2(selectedCar.anchoredPosition.x,
			selectedCar.parent.GetComponent<RectTransform>().anchoredPosition.y);
	}
	IEnumerator SelectBtn(Transform btn)
	{
		float timer = 1;
		while(timer>0)
		{
			if(btn.gameObject.activeSelf)
			{
				btn.GetComponent<Button>().Select();
				yield break;
			}
			timer -= Time.deltaTime;
			yield return null;
		}
	}
	void Update()
	{
		d_co = containerCo == null;
		int x = Input.GetKeyDown(KeyCode.RightArrow) ? 1 : Input.GetKeyDown(KeyCode.LeftArrow) ? -1 : 0;
		int y = Input.GetKeyDown(KeyCode.UpArrow) ? -1 : Input.GetKeyDown(KeyCode.DownArrow) ? 1 : 0;

		if (x != 0 || y != 0)
		{
			int posx = x + selectedCar.GetSiblingIndex();
			int posy = y + selectedCar.parent.GetSiblingIndex();
			if (posy >= 0 && posy <= 3)
			{
				for (int i = selectedCar.parent.GetSiblingIndex(); i < carContent.childCount && i>=0;)
				{
					Transform selectedClass = (RectTransform)carContent.GetChild(posy);
					if (selectedClass.ActiveChildren() > 0)
					{
						selectedCar = (RectTransform)selectedClass.GetChild(
							Mathf.Clamp(posx, 0, selectedClass.childCount - 1));
						while (!selectedCar.gameObject.activeSelf)
						{
							int idx = selectedCar.GetSiblingIndex() - 1;
							if (idx < 0)
							{
								Debug.LogError("idx < 0");
								break;
							}
							selectedCar = (RectTransform)selectedClass.GetChild(idx);
						}
					}
					i = (y > 0) ? (i+1) : (i-1);
				}

				// new car has been selected
				// set description
				carDescText.text = Info.cars[selectedCar.name].desc;
				// set bars
				if (barsCo != null)
					StopCoroutine(barsCo);
				barsCo = StartCoroutine(SetPerformanceBars());
				// focus on car
				if (containerCo != null)
					StopCoroutine(containerCo);
				containerCo = StartCoroutine(MoveToCar());
			}
		}
	}
	IEnumerator MoveToCar()
	{
		float timer = 0;
		Vector2 initPos = carContent.anchoredPosition;
		Vector2 targetPos = new Vector2(-selectedCar.anchoredPosition.x,
			-selectedCar.parent.GetComponent<RectTransform>().anchoredPosition.y);

		Vector2 scrollInitPos = new Vector2(scrollx.value, scrolly.value);
		Vector2 scrollInitSize = new Vector2(scrollx.size, scrolly.size);
		float carInGroupPos = PosAmongstActive(selectedCar.parent, selectedCar);
		float groupPos = PosAmongstActive(carContent, selectedCar.parent);
		Vector2 scrollTargetPos = new Vector2(carInGroupPos, groupPos);
		Vector2 scrollTargetSize = new Vector2(1f / selectedCar.parent.ActiveChildren(), 1f / carContent.ActiveChildren());

		while (timer<1)
		{
			float step = Easing.OutCubic(timer);
			carContent.anchoredPosition = Vector2.Lerp(initPos, targetPos, step);
			scrollx.value = Mathf.Lerp(scrollInitPos.x, scrollTargetPos.x, step);
			scrolly.value = Mathf.Lerp(scrollInitPos.y, scrollTargetPos.y, step);
			scrollx.size = Mathf.Lerp(scrollInitSize.x, scrollTargetSize.x, step);
			scrolly.size = Mathf.Lerp(scrollInitSize.y, scrollTargetSize.y, step);
			timer += Time.deltaTime;

			yield return null;
		}
	}
	float PosAmongstActive(Transform group, Transform child)
	{
		float posAmongstActive = -1;
		int activeChildren = 0;
		for (int i = 0; i < group.childCount; ++i)
		{
			if (posAmongstActive == -1 && group.GetChild(i) == child)
			{
				posAmongstActive = activeChildren;
				continue;
			}

			if (group.GetChild(i).gameObject.activeSelf)
				activeChildren++;
		}
		if(activeChildren==0)
		{
			Debug.LogError("active children = 0");
			return 0;
		}
		//Debug.Log(posAmongstActive + " " + activeChildren);
		return posAmongstActive / activeChildren;
	}
	IEnumerator SetPerformanceBars()
	{
		float[] targetSgpBars = Info.cars[selectedCar.name].sgpBars;
		float[] initSgpBars = new float[3];

		for(int i=0; i<3; i++)
			initSgpBars[i] = bars[i].sizeDelta.x / initBarSizeDelta;

		float timer = 0;
		while (timer>1)
		{
			for (int i = 0; i < 3; ++i)
			{
				float animValue = Mathf.Lerp(initSgpBars[i], targetSgpBars[i], F.curve.Evaluate(timer));
				bars[i].sizeDelta = new Vector2(animValue * initBarSizeDelta, bars[i].sizeDelta.y);
			}

			timer += 2 * Time.deltaTime;

			yield return null;
		}
	}
}
