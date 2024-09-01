using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace UMol {

public class ControllerShakeDetector : MonoBehaviour {

	// VRTK_VelocityEstimator veloEstim;
	// public float thresholdMagnitude = 50000.0f;
	// public float catchTime = 0.25f;
	// private float pauseTime = 0.7f;
	// private float lastEventTime = 0.0f;
	// private List<float> shakeTimes = new List<float>();
	// private List<Vector3> shakeVec = new List<Vector3>();

	// public delegate void ShakeDetected();
	// public event ShakeDetected OnShakeDetected;

	// void OnEnable() {
	// 	veloEstim = GetComponent<VRTK_VelocityEstimator>();
	// }

	// void Update() {
	// 	Vector3 accel = veloEstim.GetAccelerationEstimate();

	// 	if (accel.sqrMagnitude > thresholdMagnitude) {
	// 		float curTime = Time.time;
	// 		//Not in recovery time
	// 		if (curTime - lastEventTime > pauseTime) {

	// 			if (shakeTimes.Count > 2) {
	// 				int N = shakeTimes.Count;

	// 				//Acceleration changed several times
	// 				if (curTime - shakeTimes[N - 1] < catchTime &&
	// 				        shakeTimes[N - 1] - shakeTimes[N - 2] < catchTime &&
	// 				        shakeTimes[N - 2] - shakeTimes[N - 3] < catchTime) {

	// 					//At least one of the acceleration is opposite to the previous one
	// 					if (Vector3.Dot(accel, shakeVec[N - 1]) < 0.0f ||
	// 					        Vector3.Dot(shakeVec[N - 1], shakeVec[N - 2]) < 0.0f ||
	// 					        Vector3.Dot(shakeVec[N - 2], shakeVec[N - 3]) < 0.0f) {

	// 						shakeTimes.Clear();
	// 						shakeVec.Clear();
	// 						lastEventTime = curTime;

	// 						if (OnShakeDetected != null) {
	// 							OnShakeDetected();
	// 						}
	// 					}
	// 				}
	// 			}
	// 			shakeTimes.Add(curTime);
	// 			shakeVec.Add(accel);
	// 		}
	// 	}
	// }
}
}