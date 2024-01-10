using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace RVP
{
	[RequireComponent(typeof(VehicleParent))]
	[DisallowMultipleComponent]
	[AddComponentMenu("RVP/Stunt/Stunt Detector", 1)]

	// Class for detecting stunts
	public class StuntDetect : MonoBehaviour
	{
		Transform tr;
		Rigidbody rb;
		VehicleParent vp;

		[System.NonSerialized]
		public float score;
		bool drifting;
		float driftDist;
		float driftScore;
		float endDriftTime; // Time during which drifting counts even if the vehicle is not actually drifting

		public bool detectDrift = true;
		public bool detectJump = true;
		public bool detectFlips = true;

		string driftString; // String indicating drift distance
		string jumpString; // String indicating jump distance
		string flipString; // String indicating flips
		[System.NonSerialized]
		public string stuntString; // String containing all stunts

		public Motor engine;

		public float lastGrindTime = 0;
		public float grindTimer = 0;
		void Start()
		{
			tr = transform;
			rb = GetComponent<Rigidbody>();
			vp = GetComponent<VehicleParent>();
		}

		private void OnTriggerEnter(Collider other)
		{
			if (vp.groundedWheels == 0)
			{
				lastGrindTime = Time.time;
				grindTimer += Time.fixedDeltaTime;
			}
		}
		void FixedUpdate()
		{
			// Detect grinds

			// Detect drifts
			

			// Detect jumps
			//if (detectJump && !vp.crashing) {
			//    DetectJump();
			//}
			//else {
			//    jumpTime = 0;
			//    jumpDist = 0;
			//    jumpString = "";
			//}



			// Combine strings into final stunt string
			stuntString = vp.crashing ? "Crashed" : driftString + jumpString + (string.IsNullOrEmpty(flipString) || string.IsNullOrEmpty(jumpString) ? "" : " + ") + flipString;
		}

		// Logic for detecting and tracking drift


		// Logic for detecting and tracking jumps
		//void DetectJump() {
		//    if (vp.groundedWheels == 0) {
		//        jumpDist = Vector3.Distance(jumpStart, tr.position);
		//        jumpTime += Time.fixedDeltaTime;
		//        jumpString = "Jump: " + jumpDist.ToString("n0") + " m";

		//        if (engine) {
		//            vp.battery += StuntManager.jumpBoostAddStatic * Time.timeScale * 0.01f * TimeMaster.inverseFixedTimeFactor;
		//        }
		//    }
		//    else {
		//        score += (jumpDist + jumpTime) * StuntManager.jumpScoreRateStatic;

		//        if (engine) {
		//            vp.battery += (jumpDist + jumpTime) * StuntManager.jumpBoostAddStatic * Time.timeScale * 0.01f * TimeMaster.inverseFixedTimeFactor;
		//        }

		//        jumpStart = tr.position;
		//        jumpDist = 0;
		//        jumpTime = 0;
		//        jumpString = "";
		//    }
		//}

		// Logic for detecting and tracking flips

	}
}