using RVP;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public enum BottomInfoType { NEW_LEADER, NO_BATT, PIT_OUT, PIT_IN, STUNT, CAR_WINS, ELIMINATED };

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
	public GameObject AEROText;
	public GameObject DRIFTText;
	public GameObject AERODisplay;
	public GameObject progressDisplay;
	public GameObject positionDisplay;
	public GameObject TIMEDisplay;
	public GameObject RECDisplay;
	public GameObject LAPDisplay;
	public InputActionReference cancelInput;
	public VehicleParent vp { get; private set; }
	GearboxTransmission trans;
	//StuntDetect stunter;
	GasMotor engine;
	RaceBox racebox;
	readonly int minRpmRotation = 122;
	readonly int maxRpmRotation = -63;
	public ComponentPanel componentPanel;
	public LapRecordSeq lapRecordSeq;
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
	StuntsData stuntData;
	public float dimStuntTableTimer = 0;
	public Dictionary<VehicleParent, Transform> carProgressIcons = new Dictionary<VehicleParent, Transform>();
	public TimeSpan bestLapTime;

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
	public void UpdateStuntSeqTable(in StuntsData sData)
	{
		if (sData == null)
			return;

		// if previous stunt table hasn't ended dimming yet
		if (dimStuntTableTimer > 0)
		{
			Debug.Log("prev hasn't stopped dimming");
			dimStuntTableTimer = 0;
			ClearStuntInfo();
		}
		foreach (Stunt stunt in sData)
		{
			if (stunt.updateOverlay)
			{
				int stuntEntriesCount = StuntInfo.transform.childCount;
				if (stuntEntriesCount == 1) // if no elements; first element is just a template
				{
					AddStunt(stunt);
				}
				else // at least one element
				{
					StuntInfoOverlay lastElement = StuntInfo.transform
						.GetChild(stuntEntriesCount - 1).GetComponent<StuntInfoOverlay>();
					if (lastElement.name == stunt.overlayName)
					{
						lastElement.UpdatePostfix(stunt);
					}
					else
					{
						if (stuntEntriesCount == 7)
							Destroy(StuntInfo.transform.GetChild(1).gameObject);
						AddStunt(stunt);
					}
				}
			}
			stunt.updateOverlay = false;
		}
		if (StuntInfo.transform.childCount > ((Info.s_raceType == Info.RaceType.Drift) ? 1 : 2))
			StuntInfo.SetActive(true);
	}
	public void AddStunt(in Stunt stunt)
	{
		//if(!vp.followAI.isCPU)
		//	Debug.Log("addElement");
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
	public void Reset()
	{
		for (int i = 0; i < progressBar.childCount; ++i)
		{
			progressBar.GetChild(i).gameObject.SetActive(false);
		}
		ClearStuntInfo();
		liveMessages.Clear();
		curMsgInQueue = null;
		carProgressIcons.Clear();
		infoText.gameObject.SetActive(false);
	}
	public void OnDisable()
	{
		Reset();
	}
	private void OnEnable()
	{
		AEROText.SetActive(Info.s_raceType != Info.RaceType.Drift);
		DRIFTText.SetActive(Info.s_raceType == Info.RaceType.Drift);

		starTargets = new int[10];
		starCoroutines = new bool[10];
		for (int i = 0; i < 10; i++)
			StartCoroutine(SetStarVisible(i));

		if (Info.tracks[Info.s_trackName].records[0].secondsOrPts > 0)
			bestLapTime = new TimeSpan(0, 0, 0, (int)Info.tracks[Info.s_trackName].records[0].secondsOrPts, (int)(100 * (Info.tracks[Info.s_trackName].records[0].secondsOrPts % 1f)));
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
		Reset();

		infoText.gameObject.SetActive(true);

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
		bool inRace = Info.InRace;

		AERODisplay.SetActive(inRace);
		progressDisplay.SetActive(inRace);
		positionDisplay.SetActive(inRace);
		TIMEDisplay.SetActive(inRace);
		RECDisplay.SetActive(inRace);
		LAPDisplay.SetActive(inRace);
	}
	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.F3) && Info.s_rivals == 0 && !pauseMenu.gameObject.activeSelf)
		{
			componentPanel.gameObject.SetActive(!componentPanel.gameObject.activeSelf);
		}
		if (cancelInput.action.ReadValue<float>()==1 && (DateTime.Now - Info.raceStartDate).TotalSeconds > 5
			&& !componentPanel.gameObject.activeSelf && !raceManager.resultsSeq.gameObject.activeSelf)
		{
			pauseMenu.gameObject.SetActive(true);
		}
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
		if (!racebox.enabled && Info.InRace)
		{
			Debug.Log("finished");
			raceManager.PlayFinishSeq();
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
			if (result == StuntSeqStatus.None && dimStuntTableTimer <= 0)
			{ // hide stunt panel abruptly if car suddenly isn't stunting (e.g. when resetting on track)
				ClearStuntInfo();
				StuntInfo.SetActive(false);
			}
			else if (stuntPai != null)
			{ // show animation and stunt info
				if (Info.s_raceType != Info.RaceType.Drift && StuntInfo.transform.childCount < 3)
				{
					AddMessage(new Message(StuntInfo.transform.GetChild(1).GetComponent<StuntInfoOverlay>().ToString(), BottomInfoType.STUNT));
				}
				else
				{
					dimStuntTableTimer = dimmingStuntTableTime;
				}
				ptsAnim.Play(stuntPai);
			}
		}
		if (racebox.GetStuntSeq(ref stuntData))
			UpdateStuntSeqTable(stuntData);

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
		if (vp.wheels != null)
			compression = vp.wheels[0].suspensionParent.compression;
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
		if (vp.BatteryPercent < vp.lowBatteryLevel)
		{  // low battery level blink
			if (batteryCutOffTimer == 0 || Time.time - batteryCutOffTimer > 1)
				batteryCutOffTimer = Time.time;

			if (Time.time - batteryCutOffTimer < 0.5f)
				batteryLackPosition.x = maxBatteryLack;
			else
				batteryLackPosition.x = Mathf.Lerp(maxBatteryLack, minBatteryLack, vp.BatteryPercent);
		}
		else
			batteryLackPosition.x = Mathf.Lerp(maxBatteryLack, minBatteryLack, vp.BatteryPercent);
		batteryLack.GetComponent<RectTransform>().anchoredPosition = batteryLackPosition;


		if (positionDisplay.activeSelf)
		{  // Update position (1st to 10th)
			int racePosition = raceManager.Position(vp);
			positionImage.sprite = positionsSprites[racePosition];
			positionImage.SetNativeSize();
			positionSuffixImage.SetActive(racePosition > 3);
		}
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
		if (LAPDisplay.activeSelf)
		{
			TimeSpan? curLapTime = racebox.CurLaptime;
			if (curLapTime.HasValue)
			{
				SetRollers(curLapTime.Value, ref lapRollers, true);
			}
			else
			{
				foreach (var roller in lapRollers)
					roller.SetActive(false);
			}
		}

		if (RECDisplay.activeSelf)
		{
			if (bestLapTime == TimeSpan.MaxValue)
			{
				foreach (var roller in recRollers)
					roller.SetActive(false);
			}
			else
			{
				SetRollers(bestLapTime, ref recRollers);
			}
		}

		if (LAPDisplay.activeSelf)
		{
			if (racebox.curLap > 0)
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
		}

		if (AERODisplay.activeSelf)
		{
			// UPPER PANEL
			if (racebox.starLevel != carStarLevel)
			{
				for (int i = 0; i < 10; ++i)
				{
					starTargets[i] = (i < racebox.starLevel) ? 1 : 0;
					StartCoroutine(SetStarVisible(i));
				}
			}
			carStarLevel = racebox.starLevel;

			// Original AERO movement
			float score = (Info.s_raceType == Info.RaceType.Drift) ? racebox.drift : racebox.Aero;
			for (int i = 6; i >= 0; --i)
			{
				mainRollers[i].SetFrac(score % 10f / 10f);
				score /= 10;
			}
			// Alternative AERO movement
			//int score = (int)racebox.aero;
			//mainRollers[6].SetFrac(score % 10f/10f);
			//for (int i = 5; i >= 0; --i)
			//{
			//	mainRollers[i].SetValue(score % 10);
			//	score /= 10;
			//}

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

		}

		if (progressDisplay.activeSelf)
		{
			// Progress bar
			if (Info.s_cars.Count > 1 && Time.time - progressBarUpdateTime > .5f)
			{
				progressBarUpdateTime = Time.time;
				float playerDistance = vp.raceBox.curLap + vp.followAI.ProgressPercent;
				foreach (var car in Info.s_cars)
				{
					float distance = car.raceBox.curLap + car.followAI.ProgressPercent;
					float diff = Mathf.Clamp(distance - playerDistance, -1, 1);
					if (Info.s_catchup)
					{
						if (vp.catchupStatus != CatchupStatus.NoCatchup && distance - playerDistance < 50)
						{ // normal cpus when speeding to player
							car.SetCatchup(CatchupStatus.NoCatchup);
						}
						else if (distance - playerDistance > 500 && vp.catchupStatus != CatchupStatus.Slowing)
						{
							car.SetCatchup(CatchupStatus.Slowing);
						}
						else if (playerDistance - distance > 500 && vp.catchupStatus != CatchupStatus.Speeding)
						{
							car.SetCatchup(CatchupStatus.Speeding);
						}
					}
					Vector3 pos = carProgressIcons[car].GetComponent<RectTransform>().anchoredPosition;
					pos.x = 62 * diff; // from -62 to -62
					carProgressIcons[car].GetComponent<RectTransform>().anchoredPosition = pos;
				}
			}
		}
	}
	int[] starTargets;
	bool[] starCoroutines;
	

	IEnumerator SetStarVisible(int starNumber)
	{
		if (starCoroutines[starNumber])
			yield break;

		starCoroutines[starNumber] = true;
		Image starImg = starsParent.GetChild(starNumber).GetComponent<Image>();
		starImg.gameObject.SetActive(true);
		float beginA = starImg.color.a;
		var c = starImg.color;
		float timer = 0;

		while (c.a != starTargets[starNumber])
		{
			if (starTargets[starNumber] == 1)
			{
				starImg.transform.localScale = Mathf.Lerp(2, 1, 2*timer) * Vector3.one;
			}
			c.a = Mathf.Lerp(beginA, starTargets[starNumber], 2*timer);
			starImg.color = c;
			timer += Time.deltaTime;
			yield return null;
		}
		starCoroutines[starNumber] = false;
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
