using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UMol {
public class SetActiveWhenDockingMode : MonoBehaviour
{
	DockingManager dm;
	bool isActive = false;
	// Start is called before the first frame update
	void Start()
	{
		dm = UnityMolMain.getDockingManager();
	}

	// Update is called once per frame
	void Update()
	{
		if(dm.isRunning != isActive){
			foreach(Transform t in transform){
				t.gameObject.SetActive(dm.isRunning);
			}
			isActive = dm.isRunning;	
		}
	}
}
}