using RVP;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum BottomInfoType { NEW_LEADER, NO_BATT, PIT_OUT, PIT_IN, STUNT, CAR_WINS };



public class Message
{
	public string text = "";
	public BottomInfoType type;
	public Message(string content, BottomInfoType type)
	{
		text = content;
		this.type = type;
	}
	public Message()
	{
	}
}
public class SGP_HUD : MonoBehaviour
{
	public VehicleParent vp { get; private set; }
	GearboxTransmission trans;
	//StuntDetect stunter;
	GasMotor engine;
	RaceBox racebox;
	readonly int minRpmRotation = 122;
	readonly int maxRpmRotation = -63;
	public PtsAnim ptsAnim;
	public RaceManager raceManager;
	public Sprite[] gearsSprites;
	public Image currentGear;
	public Transform rpmIndicator;
	public Transform batteryLack;
	readonly int minBatteryLack = 198;
	readonly int maxBatteryLack = 46;
	// 0,1,2,3,4,5,6,7,8,9
	public Sprite[] speedoSprites;
	// Hundreds, Tens, Ones
	public Image[] SpeedoRows;
	float batteryCutOffTimer;

	public Sprite[] positionsSprites;
	public Image positionImage;
	public GameObject positionSuffixImage;

	/// 8:88:88
	public Roller[] lapRollers;
	/// 8:88:88
	public Roller[] recRollers;
	// 88:88
	public Roller[] lapNoRollers;
	private float smolScaleGear;
	private float fullScaleGear;

	public Transform starsParent;
	// 8888888
	public Roller[] mainRollers;

	public Image blinker;
	float blinkerStart = 0;
	float progressBarUpdateTime;

	public Transform progressBar;

	public float hudPos0;
	public float hudHeight;
	RectTransform rt;

	// HUD spring
	public float frequency = 2;
	public float halflife = .5f;
	float compression = 0;
	float spring_v = 0;
	float spring_pos = 0;
	public float spring_maxV = 4;

	// Progress bar
	public Sprite[] SponserSprites; // set collection as in Info.Livery
	// Bottom info text
	public Text infoText;
	// 0 = hidden text, 1 = visible text, x - time of animation
	public AnimationCurve bottomTextAnim;
	RectTransform infoText_rt;
	Color32 bottomTextColor1 = new Color32(255, 223, 0, 255);
	Color32 bottomTextColor2 = new Color32(255, 64, 64, 255);
	Queue<Message> liveMessages = new Queue<Message>();
	Message curMsgInQueue;
	public float msgArriveTime = 0;
	float msgHiddenPos = 80;
	float msgVisiblePos = 0;
	public float newPosY = 0;
	public PauseMenu pauseMenu;
	public GameObject StuntInfo;
	GameObject stuntTemplate;
	int carStarLevel = 0;
	Stunt[] stuntData;
	public float dimStuntTableTimer = 0;
	public Dictionary<VehicleParent, Transform> carProgressIcons = new Dictionary<VehicleParent, Transform>();

	/// <summary>
	/// Seconds required to dim stuntTableTimer
	/// </summary>
	const float dimmingStuntTableTime = 1;

	
	public void SetBottomTextPos(float posy)
	{
		Vector2 position = infoText_rt.anchoredPosition;
		//Vector2 size = infoText_rt.sizeDelta;

		// Set the top distance
		position.y = -posy; // Invert the value because Unity's RectTransform uses negative y-axis for top

		// Apply the new position
		infoText_rt.anchoredPosition = position;
	}
	public void AddMessage(Message message)
	{
		if (curMsgInQueue != null && message.type == curMsgInQueue.type)
		{ // if already displaying message of the same type -> immediately switch to this message
			curMsgInQueue = message;
			infoText.text = curMsgInQueue.text;
			msgArriveTime = Time.time;
		}
		else
		{
			bool found = false;
			foreach (var livemsg in liveMessages)
			{
				if (message.type == livemsg.type)
				{ // found message of the same type in queue -> just update text
					livemsg.text = message.text;
					found = true;
					break;
				}
			}
			if (!found) // new message -> add it to queue
				liveMessages.Enqueue(message);
		}
	}
	public void UpdateStuntSequenceTable(in Stunt[] stunts)
	{
		if (stunts == null)
			return;

		// if previous stunt table hasn't ended dimming yet
		if (dimStuntTableTimer > 0)
		{
			Debug.Log("prev hasn't dimming. Hiding now.");
			dimStuntTableTimer = 0;
			ClearStuntInfo();
			StuntInfo.SetActive(false);
		}
		for (int j = 0; j < stunts.Length; j++)
		{
			if (stunts[j].updateOverlay)
			{
				int stuntEntriesCount = StuntInfo.transform.childCount;
				if (stuntEntriesCount == 1) // if no elements; first element is just a template
				{
					AddStunt(stunts[j]);
				}
				else // at least one element
				{
					StuntInfoOverlay lastElement = StuntInfo.transform.GetChild(stuntEntriesCount - 1).GetComponent<StuntInfoOverlay>();
					if (lastElement.name == stunts[j].overlayName)
					{
						lastElement.UpdatePostfix(stunts[j]);
					}
					else
					{
						if (stuntEntriesCount == 7)
							Destroy(StuntInfo.transform.GetChild(1).gameObject);
						AddStunt(stunts[j]);
						stunts[j].doneTimes = 0;
					}
				}
			}
			stunts[j].updateOverlay = false;
		}
		if (StuntInfo.transform.childCount > 2)
			StuntInfo.SetActive(true);
	}
	public void AddStunt(in Stunt stunt)
	{
		if(!vp.followAI.isCPU)
			Debug.Log("addElement");
		GameObject stuntEntry = Instantiate(stuntTemplate, StuntInfo.transform);
		stuntEntry.GetComponent<StuntInfoOverlay>().WriteStuntName(stunt);
		stuntEntry.SetActive(true);
	}
	void ClearStuntInfo()
	{
		for (int i = 1; i < StuntInfo.transform.childCount; ++i)
		{
			Destroy(StuntInfo.transform.GetChild(i).gameObject);
		}
	}
	public void Disconnect()
	{
		vp = null;
		trans = null;
		engine = null;
		racebox = null;
	}
	public void OnDisable()
	{
		for(int i=0; i<progressBar.childCount; ++i)
		{
			progressBar.GetChild(i).gameObject.SetActive(false);
		}
		Disconnect();
		ClearStuntInfo();
		liveMessages.Clear();
		carProgressIcons.Clear();
	}
	private void Awake()
	{
		stuntTemplate = StuntInfo.transform.GetChild(0).gameObject;
		fullScaleGear = currentGear.transform.localScale.x;
		smolScaleGear = fullScaleGear * 0.75f;
		progressBarUpdateTime = Time.time;
		rt = GetComponent<RectTransform>();
		hudPos0 = rt.anchoredPosition.y;
		hudHeight = -55;//-transform.parent.GetComponent<RectTransform>().sizeDelta.y / 4f;
		infoText_rt = infoText.transform.GetComponent<RectTransform>();
		curMsgInQueue = new Message();
		SetBottomTextPos(msgHiddenPos);
	}
	public void Connect(VehicleParent newVehicle)
	{
		if (!newVehicle)
		{
			Debug.LogError("newVehicle is null");
			return;
		}	
		vp = newVehicle;

		trans = newVehicle.GetComponentInChildren<Transmission>() as GearboxTransmission;

		engine = newVehicle.GetComponentInChildren<GasMotor>();

		racebox = newVehicle.GetComponent<RaceBox>();

		transform.gameObject.SetActive(true);

		gameObject.SetActive(true);

		for (int i = 0; i < Info.s_cars.Count; ++i)
		{
			progressBar.GetChild(i).gameObject.SetActive(true);
			progressBar.GetChild(i).localScale = 0.75f * Vector3.one;
			carProgressIcons.Add(Info.s_cars[i], progressBar.GetChild(i));
			progressBar.GetChild(i).name = Info.s_cars[i].tr.name;
			progressBar.GetChild(i).GetComponent<Image>().sprite = SponserSprites[Info.s_cars[i].sponsor];
		}
		carProgressIcons[vp].SetSiblingIndex(9);
		carProgressIcons[vp].localScale = Vector3.one;
	}
	
	void FixedUpdate()
	{
		if (!vp)
			return;

		// debug stunt UI
		//if (Input.GetKeyDown(KeyCode.T)) // update overlay
		//{
		//    racebox.stunts[d_select].updateOverlay = true;
		//    UpdateStuntSequenceTable();
		//}
		//else if (Input.GetKeyDown(KeyCode.Y)) // select next
		//{
		//    d_select++;
		//}
		//else if (Input.GetKeyDown(KeyCode.U)) // select prev
		//{
		//    d_select--;
		//}
		//else if (Input.GetKeyDown(KeyCode.I)) // succeeded
		//{
		//    EndStuntSeq(true);
		//}
		//else if (Input.GetKeyDown(KeyCode.O)) // failed
		//{
		//    EndStuntSeq(false);
		//}
		if(racebox.Finished())
		{
			StartCoroutine(raceManager.PlayFinishSeq());
			gameObject.SetActive(false);
			return;
		}

		ptsAnim.Play(racebox.JumpPai);

		// dim stunt table after end of evos
		if (dimStuntTableTimer > 0)
		{
			float progress = (dimStuntTableTimer) / dimmingStuntTableTime;
			for (int i = 1; i < StuntInfo.transform.childCount; ++i)
			{
				StuntInfo.transform.GetChild(i).GetComponent<StuntInfoOverlay>().DimTexts(progress);
			}
			dimStuntTableTimer -= Time.fixedDeltaTime;
		}
		if (StuntInfo.transform.childCount > 1)
		{
			var result = racebox.StuntSeqEnded(out var stuntPai);
			if (result == StuntSeqStatus.None && dimStuntTableTimer<=0)
			{ // hide stunt panel abruptly if car suddenly isn't stunting (e.g. when resetting on track)
				ClearStuntInfo();
				StuntInfo.SetActive(false);
			}
			else if (stuntPai != null)
			{ // show animation and stunt info
				if (StuntInfo.transform.childCount < 3)
				{
					AddMessage(new Message(StuntInfo.transform.GetChild(1).GetComponent<StuntInfoOverlay>().ToString(), BottomInfoType.STUNT));
				}
				else
					dimStuntTableTimer = dimmingStuntTableTime;
				ptsAnim.Play(stuntPai);
			}
		}
		if (racebox.GetStuntsSeq(ref stuntData))
			UpdateStuntSequenceTable(stuntData);
		if (Input.GetButtonDown("Cancel"))
		{
			pauseMenu.gameObject.SetActive(true);
		}

		// Bottom Info
		bool msgAwaits = liveMessages.TryPeek(out curMsgInQueue);
		if (msgArriveTime > 0 || msgAwaits)
		{
			if (msgArriveTime == 0) // nothing currently displayed 
			{// .. so dequeue container and set result to be displayed
				infoText.text = curMsgInQueue.text;
				msgArriveTime = Time.time;
			}
			float msgSecsOnScreen = Time.time - msgArriveTime;
			if (msgSecsOnScreen > bottomTextAnim.Duration())
			{
				msgArriveTime = 0;
				liveMessages.TryDequeue(out _);
			}
			newPosY = Mathf.Lerp(msgHiddenPos, msgVisiblePos, bottomTextAnim.Evaluate(msgSecsOnScreen));
			SetBottomTextPos(newPosY);
			infoText.color = msgSecsOnScreen % 1f > 0.5f ? bottomTextColor1 : bottomTextColor2;
		}

		// HUD vibrates along with dampers
		Vector3 hudPos = rt.anchoredPosition;
		try
		{
			compression = vp.wheels[0].suspensionParent.compression;
		}
		catch
		{

		}
		float target = Mathf.Lerp(hudPos0, hudHeight - hudPos0, compression);
		damper_spring(ref spring_pos, ref spring_v, target, spring_maxV);
		hudPos.y = spring_pos;
		rt.anchoredPosition = hudPos;

		// Gears
		currentGear.sprite = gearsSprites[trans.currentGear];
		float scale = currentGear.transform.localScale.x;
		if (trans.selectedGear != trans.currentGear)
			scale = Mathf.Lerp(scale, smolScaleGear, 20 * Time.fixedDeltaTime);
		else
			scale = Mathf.Lerp(scale, fullScaleGear, 20 * Time.fixedDeltaTime);
		currentGear.transform.localScale = Vector3.one * scale;

		// RPM indicator
		Vector3 rpmRotation = rpmIndicator.rotation.eulerAngles;
		rpmRotation.z = Mathf.LerpUnclamped(minRpmRotation, maxRpmRotation, engine.targetPitch);
		rpmIndicator.rotation = Quaternion.Euler(rpmRotation);

		// Speedometer 888
		int speed = Mathf.Clamp((int)(vp.velMag * 3.6f), 0, 999);
		for (int i = 2; i >= 0; --i)
		{
			int letter = speed % 10;
			SpeedoRows[i].sprite = speedoSprites[letter];
			if (letter == 0 && speed < 10)
				SpeedoRows[i].color = new Color32(128, 128, 128, 128);
			else
				SpeedoRows[i].color = new Color32(255, 255, 255, 255);
			speed /= 10;
		}
		// Update battery level
		Vector3 batteryLackPosition = batteryLack.GetComponent<RectTransform>().anchoredPosition;
		if (vp.battery < engine.batteryCutOffLevel)
		{  // low battery level blink
			if (batteryCutOffTimer == 0 || Time.time - batteryCutOffTimer > 1)
				batteryCutOffTimer = Time.time;

			if (Time.time - batteryCutOffTimer < 0.5f)
				batteryLackPosition.x = maxBatteryLack;
			else
				batteryLackPosition.x = Mathf.Lerp(maxBatteryLack, minBatteryLack, vp.battery);
		}
		else
			batteryLackPosition.x = Mathf.Lerp(maxBatteryLack, minBatteryLack, vp.battery);
		batteryLack.GetComponent<RectTransform>().anchoredPosition = batteryLackPosition;

		// Update position (1st to 10th)
		int racePosition = raceManager.Position(vp);
		positionImage.sprite = positionsSprites[racePosition];
		positionImage.SetNativeSize();
		positionSuffixImage.SetActive(racePosition > 3);

		// debug LAP rollers
		//if (Input.GetKeyDown(KeyCode.Alpha0))
		//    racebox.lapStartTime = DateTime.Now.AddSeconds(-55);
		//if (Input.GetKeyDown(KeyCode.Alpha1))
		//    racebox.lapStartTime = DateTime.Now.AddSeconds(-595); // < 10 minutes
		//if (Input.GetKeyDown(KeyCode.Alpha2))
		//    racebox.lapStartTime = DateTime.Now.AddSeconds(-3595); // < 60 minutes
		//if (Input.GetKeyDown(KeyCode.Alpha3))
		//    racebox.lapStartTime = DateTime.Now.AddSeconds(-7195); // < 2 hours
		//if (Input.GetKeyDown(KeyCode.Alpha4))
		//{
		//    racebox.NextLap();
		//}
		TimeSpan? curLapTime = racebox.CurLapTime();
		if (curLapTime.HasValue)
		{
			SetRollers(curLapTime.Value, ref lapRollers, true);
		}
		else
		{
			foreach (var roller in lapRollers)
				roller.SetActive(false);
		}

		// REC rollers
		if (racebox.bestLapTime == TimeSpan.MaxValue)
		{
			foreach (var roller in recRollers)
				roller.SetActive(false);
		}
		else
		{
			SetRollers(racebox.bestLapTime, ref recRollers);
		}

		// LAP rollers
		if(racebox.curLap > 0)
		{
			lapNoRollers[1].SetValue(racebox.curLap % 10); // ones
			lapNoRollers[0].SetValue(racebox.curLap / 10); // tens

			lapNoRollers[3].SetValue(Info.s_laps % 10); // ones
			lapNoRollers[2].SetValue(Info.s_laps / 10); // tens
		}
		else
		{
			foreach (var roller in lapNoRollers)
				roller.SetActive(false);
		}

		// Stars
		if (racebox.starLevel > carStarLevel)
		{
			StartCoroutine(SetStarVisible(starsParent.GetChild(racebox.starLevel-1).GetComponent<Image>(), true));
		}
		else if (racebox.starLevel < carStarLevel)
		{
			for(int i=0; i<carStarLevel; ++i)
			{
				StartCoroutine(SetStarVisible(starsParent.GetChild(i).GetComponent<Image>(), false));
			}
		}
		carStarLevel = racebox.starLevel;

		// Aero/Drift score
		mainRollers[6].SetFrac(racebox.aero % 1f);
		int score = (int)racebox.aero;
		for (int i = 5; i >= 0; --i)
		{
			mainRollers[i].SetValue(score % 10);
			score /= 10;
		}

		// Combo Blinker
		if (racebox.grantedComboTime > 0)
		{
			if (blinkerStart == 0 || Time.time - blinkerStart >= 1)
				blinkerStart = Time.time;

			Color clr = Color.Lerp(Color.red, Color.green, racebox.grantedComboTime / 3f);
			clr.a = Mathf.Lerp(.5f, 1, Mathf.Abs(Mathf.Sin(2 * Mathf.PI * (Time.time - blinkerStart))));
			blinker.color = clr;
		}
		else
		{
			blinker.color = Color.red;
		}

		// Progress bar
		if (Info.s_cars.Count > 1 && Time.time - progressBarUpdateTime > .5f)
		{
			progressBarUpdateTime = Time.time;
			float playerDistance = vp.raceBox.curLap + vp.followAI.ProgressPercent();
			foreach(var car in Info.s_cars)
			{
				float distance = car.raceBox.curLap + car.followAI.ProgressPercent();
				float diff = Mathf.Clamp(distance - playerDistance, -1, 1);
				Vector3 pos = carProgressIcons[car].GetComponent<RectTransform>().anchoredPosition;
				pos.x = 62 * diff; // from -62 to -62
				carProgressIcons[car].GetComponent<RectTransform>().anchoredPosition = pos;
			}
		}
	}

	IEnumerator SetStarVisible(Image starObj, bool targetVisiblity)
	{
		float timer = 0;
		while(timer<.5f)
		{
			if(targetVisiblity==true)
			{
				starObj.gameObject.SetActive(true);
				starObj.transform.localScale = Mathf.Lerp(2, 1, timer * 2) * Vector3.one;
			}
			var c= starObj.color;
			c.a = targetVisiblity ? 2*timer : (1 - 2*timer);
			starObj.color = c;
			timer += Time.deltaTime;
			yield return null;
		}
		if(targetVisiblity == false)
			starObj.gameObject.SetActive(false);
	}

	void SetRollers(in TimeSpan timespan, ref Roller[] rollers, bool millisecondsAsFrac = false)
	{
		if (timespan < TimeSpan.FromMinutes(10)) // laptime < 10 mins
		{
			rollers[0].SetValue(timespan.Minutes);

			int seconds = timespan.Seconds;
			rollers[2].SetValue(seconds % 10); // ones
			rollers[1].SetValue(seconds / 10); // tens


			if (millisecondsAsFrac)
			{
				float mills = timespan.Milliseconds / 1000f;
				rollers[4].SetFrac(mills % 0.1f * 10); // frac ones
				rollers[3].SetFrac(mills); // frac tens
			}
			else
			{
				int mills = timespan.Milliseconds / 10;
				rollers[4].SetValue(mills % 10); // ones
				rollers[3].SetValue(mills / 10); // tens
			}
		}
		else
		{ // laptime > 10mins
			rollers[0].SetValue(timespan.Hours);

			int minutes = timespan.Minutes;
			rollers[2].SetValue(minutes % 10); // ones
			rollers[1].SetValue(minutes / 10); // tens

			int seconds = timespan.Seconds;
			rollers[4].SetValue(seconds % 10); // ones
			rollers[3].SetValue(seconds / 10); // tens
		}
	}

	void damper_spring(ref float x, ref float v, in float x_goal, in float v_goal)
	{
		float dt = Time.fixedDeltaTime;
		float g = x_goal;
		float q = v_goal;
		float s = frequency_to_stiffness(frequency);
		float d = halflife_to_damping(halflife);
		float c = g + (d * q) / (s + Mathf.Epsilon);
		float y = d / 2.0f;

		if (Mathf.Abs(s - (d * d) / 4.0f) < Mathf.Epsilon) // Critically Damped
		{
			float j0 = x - c;
			float j1 = v + j0 * y;

			float eydt = fast_negexp(y * dt);

			x = j0 * eydt + dt * j1 * eydt + c;
			v = -y * j0 * eydt - y * dt * j1 * eydt + j1 * eydt;
		}
		else if (s - (d * d) / 4.0f > 0.0) // Under Damped
		{
			float w = Mathf.Sqrt(s - (d * d) / 4.0f);
			float j = Mathf.Sqrt(squaref(v + y * (x - c)) / (w * w + Mathf.Epsilon) + squaref(x - c));
			float p = Mathf.Atan((v + (x - c) * y) / (-(x - c) * w + Mathf.Epsilon));

			j = (x - c) > 0.0f ? j : -j;

			float eydt = fast_negexp(y * dt);

			x = j * eydt * Mathf.Cos(w * dt + p) + c;
			v = -y * j * eydt * Mathf.Cos(w * dt + p) - w * j * eydt * Mathf.Sin(w * dt + p);
		}
		else if (s - (d * d) / 4.0f < 0.0) // Over Damped
		{
			float y0 = (d + Mathf.Sqrt(d * d - 4 * s)) / 2.0f;
			float y1 = (d - Mathf.Sqrt(d * d - 4 * s)) / 2.0f;
			float j1 = (c * y0 - x * y0 - v) / (y1 - y0);
			float j0 = x - j1 - c;

			float ey0dt = fast_negexp(y0 * dt);
			float ey1dt = fast_negexp(y1 * dt);

			x = j0 * ey0dt + j1 * ey1dt + c;
			v = -y0 * j0 * ey0dt - y1 * j1 * ey1dt;
		}
		float frequency_to_stiffness(float frequency)
		{
			return squaref(2.0f * Mathf.PI * frequency);
		}

		float halflife_to_damping(float halflife)
		{
			return (4.0f * 0.69314718056f) / (halflife + Mathf.Epsilon);
		}
		float fast_negexp(float x)
		{
			return 1.0f / (1.0f + x + 0.48f * x * x + 0.235f * x * x * x);
		}
		float squaref(float x) { return x * x; }
	}
}
