using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.UIElements.Experimental;

public enum GarageType { Unlocked, Earned, Wild, Aero, Speed, Specials }
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
	public GarageType type = GarageType.Unlocked;
	float playerMoney = 0;
	public void SetType(GarageType type, float money = 1e6f)
	{
		playerMoney = money;

		if (this.type == type)
			return;

		ClearAllCars();
		this.type = type;
	}
	new void Awake()
	{
		base.Awake();
		initBarSizeDelta = bars[0].sizeDelta.x;
	}
	private void OnDisable()
	{ // in unity, 
		F.I.move2Ref.action.performed -= CalculateTargetToSelect;
		persistentSelectedCar = selectedCar.name;
		F.I.s_playerCarName = selectedCar.name;
	}
	private void OnEnable()
	{
		F.I.move2Ref.action.performed += CalculateTargetToSelect;
		if (loadCo)
		{
			StopCoroutine(Load());
		}
		StartCoroutine(Load());
	}
	bool ShowCar(Car c)
	{
		switch (type)
		{
			case GarageType.Unlocked:
				return c.price != -1;
			case GarageType.Earned:
				return c.price != -1 && c.price <= playerMoney;
			case GarageType.Aero:
				return c.price != -1 && c.category == CarGroup.Aero;
			case GarageType.Wild:
				return c.price != -1 && c.category == CarGroup.Wild;
			case GarageType.Speed:
				return c.price != -1 && c.category == CarGroup.Speed;
			case GarageType.Specials:
				return c.price != -1 && c.category == CarGroup.Team;
			default:
				return false;
		}
	}
	void ClearAllCars()
	{
		for (int i = 0; i < carContent.childCount; ++i)
		{ // remove cars from previous entry
			carContent.GetChild(i).DestroyAllChildren();
		}
	}
	IEnumerator Load()
	{
		int carsCurrentlyVisible = 0;
		for (int i = 0; i < carContent.childCount; ++i)
			carsCurrentlyVisible += carContent.GetChild(i).childCount;

		int numberOfCarsThatShouldBeVisible = F.I.cars.Count(c => ShowCar(c));
		loadCo = true;
		if (carsCurrentlyVisible != numberOfCarsThatShouldBeVisible)
		{
			bool[] menuButtons = new bool[4];
			ClearAllCars();
			for (int i = 0; i < F.I.cars.Length; ++i)
			{ // populate car grid
				var car = F.I.cars[i];
				if (ShowCar(car))
				{
					var newcar = Instantiate(carImageTemplate, carContent.GetChild((int)car.category));
					newcar.name = "car" + (i + 1).ToString("D2");
					newcar.GetComponent<Image>().sprite = Resources.Load<Sprite>(F.I.carImagesPath + newcar.name);
					newcar.SetActive(true);
					menuButtons[(int)car.category] = true;
					if (persistentSelectedCar != null && persistentSelectedCar == newcar.name)
						selectedCar = newcar.transform;
				}
			}
			//Debug.Log(menuButtons[0] + " " + menuButtons[1] + " " + menuButtons[2] + " " + menuButtons[3]);

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
			
		}
		if (selectedCar == null)
		{
			carDescText.text = "No cars available lulz";
		}
		else
		{
			buttonsContainer.GetChild(selectedCar.parent.GetSiblingIndex()).GetComponent<MainMenuButton>().Select();
			carDescText.text = F.I.Car(selectedCar.name).name + "\n\n" + F.I.Car(selectedCar.name).desc;
		}
		radial.gameObject.SetActive(selectedCar);
		containerCo = StartCoroutine(MoveToCar());
		radial.SetChildrenActive(carContent);
		
		if (barsAndRadialCo != null)
			StopCoroutine(barsAndRadialCo);
		barsAndRadialCo = StartCoroutine(SetPerformanceBarsAndRadial());
		//Debug.Log(selectedCar);
		loadCo = false;
	}

	void CalculateTargetToSelect(InputAction.CallbackContext ctx)
	{
		if (!selectedCar || loadCo)
			return;
		d_co = containerCo == null;

		Vector2 move2 = F.I.move2Ref.action.ReadValue<Vector2>();
		int x = Mathf.RoundToInt(move2.x);
		int y = Mathf.RoundToInt(-move2.y);

		if (x != 0 || y != 0)
		{
			int posx = x + selectedCar.GetSiblingIndex();
			int posy = y + selectedCar.parent.GetSiblingIndex();
			if (posy >= 0 && posy <= 3 && posx >= 0)
			{
				Transform tempSelectedCar = null;
				for (int i = posy; i < carContent.childCount && i >= 0;	i = (y > 0) ? (i + 1) : (i - 1))
				{
					Transform selectedClass = carContent.GetChild(i);

					if (selectedClass.childCount > 0)
					{
						if (posx >= selectedClass.childCount)
							posx = selectedClass.childCount - 1;
						tempSelectedCar = selectedClass.GetChild(posx);
						//Debug.Log(tempSelectedCar);
						break;
					}
				}
				if (tempSelectedCar != null && tempSelectedCar != selectedCar)
				{
					selectedCar = tempSelectedCar;
					F.I.s_playerCarName = selectedCar.name;
					buttonsContainer.GetChild(selectedCar.parent.GetSiblingIndex()).GetComponent<MainMenuButton>().Select();
					PlaySFX("fe-bitmapscroll");
				}
				// new car has been selected
				// set description
				var car = F.I.Car(selectedCar.name);
				carDescText.text = car.name + "\n\n" + car.desc;
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
		yield return null;
		if (!selectedCar)
			yield break;
		float timer = 0;
		Vector2 initPos = carContent.anchoredPosition;
		Vector2 targetPos = new Vector2(-((RectTransform)selectedCar).anchoredPosition.x,
			-selectedCar.parent.GetComponent<RectTransform>().anchoredPosition.y);
		Vector2 scrollInitPos = new Vector2(scrollx.value, scrolly.value);
		Vector2 scrollInitSize = new Vector2(scrollx.size, scrolly.size);
		float carInGroupPos = F.I.InGroupPos(selectedCar);//.parent.PosAmongstActive(selectedCar, false);
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
		if (selectedCar)
			radial.SetAnimTo(selectedCar.parent.GetSiblingIndex());
		float[] targetSgpBars = selectedCar ? F.I.Car(selectedCar.name).config.SGP : new float[] { .03f, .03f, .03f };
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
