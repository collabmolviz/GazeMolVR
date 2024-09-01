using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace UMol
{

    public class LookAtCamera : MonoBehaviour
    {
        private Transform mainCam;
        public bool onlyAlignVectors = false;

        void Start()
        {
            if (!UnityMolMain.inVR())
            {
                if (Camera.main != null)
                    mainCam = Camera.main.transform;
            }
            else
            {
                // mainCam = VRTK.VRTK_DeviceFinder.HeadsetCamera();
                if (Camera.main != null)
                    mainCam = Camera.main.transform;
            }
        }
        void Update()
        {
            if (mainCam == null)
            {
                if (!UnityMolMain.inVR())
                {
                    if (Camera.main != null)
                        mainCam = Camera.main.transform;
                }
                else
                {
                    // mainCam = VRTK.VRTK_DeviceFinder.HeadsetCamera();
                    mainCam = Camera.main.transform;
                }
            }
            if (mainCam != null)
            {
                if (onlyAlignVectors)
                    transform.rotation = Quaternion.LookRotation(mainCam.forward);
                else
                    transform.rotation = Quaternion.LookRotation(transform.position - mainCam.position);

            }
        }
    }
}