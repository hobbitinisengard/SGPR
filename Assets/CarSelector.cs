using RVP;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements.Experimental;

public class CarSelector : MonoBehaviour
{
	public RectTransform[] bars;
	public Text carDescText;
	public Transform buttonsContainer;
	public RectTransform carContent;
	public Scrollbar scrollx;
	public Scrollbar scrolly;
	public RadialOneVisible radial;
	RectTransform selectedCar;
	float initBarSizeDelta;
	Coroutine barsAndRadialCo;
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
		// set buttons
		for (int i = 0; i < buttonsContainer.childCount; ++i)
		{
			// show only carclass buttons when carclass exists
			buttonsContainer.GetChild(i).gameObject.SetActive(menuButtons[i]);
			// set carclass transforms with/without children to active/notactive
			carContent.GetChild(i).gameObject.SetActive(menuButtons[i]);
			if ((int)Info.cars[selectedCar.name].carClass == i)
				buttonsContainer.GetChild(i).GetComponent<MainMenuButton>().Select();
		}
		// set description
		carDescText.text = Info.cars[selectedCar.name].desc;
		// set bars and radial
		barsAndRadialCo = StartCoroutine(SetPerformanceBarsAndRadial());
		containerCo = StartCoroutine(MoveToCar());
		radial.SetChildrenActive(menuButtons);
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
			if (posy >= 0 && posy <= 3 && posx>=0)
			{
				for (int i = posy; i < carContent.childCount && i >= 0;)
				{
					Transform selectedClass = (RectTransform)carContent.GetChild(i);
					if (selectedClass.ActiveChildren() > 0)
					{
						if(posx < selectedClass.childCount && selectedClass.GetChild(posx).gameObject.activeSelf)
						{// /\ if car exists set it
							selectedCar = (RectTransform)selectedClass.GetChild(posx);
							break;
						}
						// such car isn't available. set the closest one to posx
						int prev = posx-1;
						int next = prev+1;
						while (prev >= 0 || next < selectedClass.childCount)
						{
							if (prev >= 0 && selectedClass.GetChild(prev).gameObject.activeSelf)
							{
								if(selectedCar != (RectTransform)selectedClass.GetChild(prev))
								{ // don't choose the same car as currently set
									selectedCar = (RectTransform)selectedClass.GetChild(prev);
									break;
								}
							}
							if (next < selectedClass.childCount && selectedClass.GetChild(next).gameObject.activeSelf)
							{
								if (selectedCar != (RectTransform)selectedClass.GetChild(next))
								{ // don't choose the same car as currently set
									selectedCar = (RectTransform)selectedClass.GetChild(next);
									break;
								}
							}
							--prev;
							++next;
						}
						break;
					}
					i = (y > 0) ? (i + 1) : (i - 1);
				}

				// new car has been selected
				// set description
				carDescText.text = Info.cars[selectedCar.name].desc;
				// set bars
				if (barsAndRadialCo != null)
					StopCoroutine(barsAndRadialCo);
				barsAndRadialCo = StartCoroutine(SetPerformanceBarsAndRadial());
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
		float carInGroupPos = selectedCar.parent.PosAmongstActive(selectedCar, false);
		float groupPos = carContent.PosAmongstActive(selectedCar.parent, false);
		Vector2 scrollTargetPos = new Vector2(carInGroupPos, groupPos);
		Vector2 scrollTargetSize = new Vector2(1f / selectedCar.parent.ActiveChildren(), 1f / carContent.ActiveChildren());

		while (timer < 1)
		{
			float step = F.EasingOutQuint(timer);
			carContent.anchoredPosition = Vector2.Lerp(initPos, targetPos, step);
			scrollx.value = Mathf.Lerp(scrollInitPos.x, scrollTargetPos.x, step);
			scrolly.value = Mathf.Lerp(scrollInitPos.y, scrollTargetPos.y, step);
			scrollx.size = Mathf.Lerp(scrollInitSize.x, scrollTargetSize.x, step);
			scrolly.size = Mathf.Lerp(scrollInitSize.y, scrollTargetSize.y, step);
			timer += Time.deltaTime;

			yield return null;
		}
	}
	
	IEnumerator SetPerformanceBarsAndRadial()
	{
		radial.SetAnimTo(selectedCar.parent.GetSiblingIndex());
		float[] targetSgpBars = Info.cars[selectedCar.name].sgpBars;
		float[] initSgpBars = new float[3];

		for (int i = 0; i < 3; i++)
			initSgpBars[i] = bars[i].sizeDelta.x / initBarSizeDelta;
		float timer = 0;
		while (timer < 1)
		{
			float step = Easing.OutCubic(timer);
			// set bars
			for (int i = 0; i < 3; ++i)
			{
				float animValue = Mathf.Lerp(initSgpBars[i], targetSgpBars[i], step);
				bars[i].sizeDelta = new Vector2(animValue * initBarSizeDelta, bars[i].sizeDelta.y);
			}

			timer += Time.deltaTime;

			yield return null;
		}
	}

}
