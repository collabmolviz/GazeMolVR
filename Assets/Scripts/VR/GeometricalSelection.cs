using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using HTC.UnityPlugin.Vive;
using HTC.UnityPlugin.Utility;

namespace UMol {
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(ViveRoleSetter))]

public class GeometricalSelection : MonoBehaviour {

	public enum GeometrySelectionMode
	{
		Sphere,
		Cube
	}

	public GeometrySelectionMode mode = GeometrySelectionMode.Sphere;

	public Transform secondController;

	// private float startDistance = -1.0f;

	// void Update(){

	// 	if(secondController == null){
	// 		return;
	// 	}
	// 	if((ViveInput.GetPress(HandRole.RightHand, activationButton)
	// 		&& ViveInput.GetPressDown(HandRole.LeftHand, activationButton)) ||
	// 		(ViveInput.GetPressDown(HandRole.RightHand, activationButton)
	// 		&& ViveInput.GetPress(HandRole.LeftHand, activationButton))
	// 		){

	// 		startDistance = Vector3.Distance(transform.position, secondController.position);
	// 		return;
	// 	}
	// 	if(ViveInput.GetPressUp(HandRole.RightHand, activationButton) ||
	// 		ViveInput.GetPressUp(HandRole.LeftHand, activationButton)) {

	// 			startDistance = -1.0f;
	// 			API.APIPython.selectInSphere(transform.position, transform.lossyScale.x);
	// 		}

	// 	if(ViveInput.GetPress(HandRole.RightHand, activationButton) &&
	// 		ViveInput.GetPress(HandRole.LeftHand, activationButton)){

	// 		if(startDistance == -1.0f){
	// 			return;
	// 		}

	// 		float dist = Vector3.Distance(transform.position, secondController.position);
	// 		float diff = (dist - startDistance)/10.0f;
	// 		transform.localScale += Vector3.one * diff;
	// 		transform.localScale = Mathf.Max(0.01f, transform.localScale.x) * Vector3.one;
	// 		transform.localScale = Mathf.Min(1.1f, transform.localScale.x) * Vector3.one;
	// 	}
	// }

}
}