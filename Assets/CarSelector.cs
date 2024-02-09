using RVP;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements.Experimental;

public class CarSelector : Sfxable
{
	public RectTransform[] bars;
	public Text carDescText;
	public Transform buttonsContainer;
	public RectTransform carContent;
	public Scrollbar scrollx;
	public Scrollbar scrolly;
	public RadialOneVisible radial;
	public GameObject carImageTemplate;
	Transform selectedCar;
	string persistentSelectedCar;
	float initBarSizeDelta;
	Coroutine barsAndRadialCo;
	Coroutine containerCo;
	bool loadCo;
	public bool d_co;
	float horizontal;
	float vertical;

	void Awake()
	{
		initBarSizeDelta = bars[0].sizeDelta.x;
	}
	private void OnDisable()
	{ // in unity, 
		persistentSelectedCar = selectedCar.name;
		Info.s_playerCarName = selectedCar.name;
		Debug.Log("Disable "+persistentSelectedCar);
	}
	private void OnEnable()
	{
		if (loadCo)
		{
			StopCoroutine(Load());
		}
		StartCoroutine(Load());
	}
	IEnumerator Load()
	{
		loadCo = true;
		bool[] menuButtons = new bool[4];
		for (int i = 0; i < carContent.childCount; ++i)
		{ // remove cars from previous entry
			Transform carClass = carContent.GetChild(i);
			for (int j = 0; j < carClass.childCount; ++j)
			{
				//Debug.Log(carClass.GetChild(j).name);
				Destroy(carClass.GetChild(j).gameObject);
			}
		}
		for(int i=0; i<Info.cars.Length; ++i)
		{ // populate car grid
			var car = Info.cars[i];
			if (car.unlocked)
			{
				var newcar = Instantiate(carImageTemplate, carContent.GetChild((int)car.category));
				newcar.name = "car"+(i+1).ToString("D2");
				newcar.GetComponent<Image>().sprite = Resources.Load<Sprite>(Info.carImagesPath + newcar.name);
				newcar.SetActive(true);
				menuButtons[(int)car.category] = true;
				if (persistentSelectedCar != null && persistentSelectedCar == newcar.name)
					selectedCar = newcar.transform;
			}
		}
		Debug.Log(menuButtons[0] + " " + menuButtons[1] + " " + menuButtons[2] + " " + menuButtons[3]);

		yield return null; // wait for one frame for active objects to refresh

		// set buttons
		for (int i = 0; i < buttonsContainer.childCount; ++i)
		{
			if (selectedCar == null && menuButtons[i])
			{
				selectedCar = carContent.GetChild(i).GetChild(0);
			}
			// show only carclass buttons when carclass exists
			buttonsContainer.GetChild(i).gameObject.SetActive(menuButtons[i]);
			// disable car classes without children (required for sliders to work)
			carContent.GetChild(i).gameObject.SetActive(menuButtons[i]);
		}
		if (selectedCar == null)
		{
			carDescText.text = "No cars available lol";
		}
		else
		{
			buttonsContainer.GetChild(selectedCar.parent.GetSiblingIndex()).GetComponent<MainMenuButton>().Select();
			carDescText.text = Info.Car(selectedCar.name).desc;
		}
		radial.gameObject.SetActive(selectedCar);

		containerCo = StartCoroutine(MoveToCar());
		radial.SetChildrenActive(menuButtons);
		barsAndRadialCo = StartCoroutine(SetPerformanceBarsAndRadial());
		Debug.Log(selectedCar);
		loadCo = false;
	}
	void Update()
	{
		if (!selectedCar || loadCo)
			return;
		d_co = containerCo == null;

		if (Mathf.Abs(horizontal) < 0.1f && Mathf.Abs(vertical) < 0.1f)
		{
			horizontal = Input.GetAxis("Horizontal");
			vertical = Input.GetAxis("Vertical");
			int x = horizontal > 0.1f ? 1 : horizontal < 0.1f ? -1 : 0;
			int y = vertical > 0.1f ? -1 : horizontal < 0.1f ? 1 : 0;

			if (x != 0 || y != 0)
			{
				int posx = x + selectedCar.GetSiblingIndex();
				int posy = y + selectedCar.parent.GetSiblingIndex();
				if (posy >= 0 && posy <= 3 && posx >= 0)
				{
					Transform tempSelectedCar = null;
					for (int i = posy; i < carContent.childCount && i >= 0;)
					{
						Transform selectedClass = carContent.GetChild(i);

						if (selectedClass.childCount > 0)
						{
							if (posx >= selectedClass.childCount)
								posx = selectedClass.childCount - 1;
							tempSelectedCar = selectedClass.GetChild(posx);
							Debug.Log(tempSelectedCar);
							break;
						}
						i = (y > 0) ? (i + 1) : (i - 1);
					}
					if (tempSelectedCar != null && tempSelectedCar != selectedCar)
					{
						selectedCar = tempSelectedCar;
						PlaySFX("fe-bitmapscroll");
					}
					// new car has been selected
					// set description
					carDescText.text = Info.Car(selectedCar.name).desc;
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

		horizontal = Input.GetAxis("Horizontal");
		vertical = Input.GetAxis("Vertical");
	}

	IEnumerator MoveToCar()
	{
		yield return null;
		if (!selectedCar)
			yield break;
		float timer = 0;
		Vector2 initPos = carContent.anchoredPosition;
		Vector2 targetPos = new Vector2(-((RectTransform)selectedCar).anchoredPosition.x,
			-selectedCar.parent.GetComponent<RectTransform>().anchoredPosition.y);
		Vector2 scrollInitPos = new Vector2(scrollx.value, scrolly.value);
		Vector2 scrollInitSize = new Vector2(scrollx.size, scrolly.size);
		float carInGroupPos = Info.InGroupPos(selectedCar);//.parent.PosAmongstActive(selectedCar, false);
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
		if(selectedCar)
			radial.SetAnimTo(selectedCar.parent.GetSiblingIndex());
		float[] targetSgpBars = selectedCar ? Info.Car(selectedCar.name).config.SGP : new float[] { .03f, .03f, .03f };
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
