using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Photon.Pun;
using TMPro;
using System;

namespace UnityEngine.UI.Extensions.ColorPicker
{
    public class NetworkPlayer_US01 : MonoBehaviour
    {
        public Transform head;
        public Transform leftHand;
        public Transform rightHand;

        public LineRenderer leftHandLineRenderer;
        public LineRenderer rightHandLineRenderer;

        public GameObject leftSphereReticle;
        public GameObject rightSphereReticle;

        private Transform camTransform;
        private string gazeVizType;
        private TextMeshProUGUI textMeshProteinViz;

        public GameObject eyeGazeCuePointer;
        public GameObject eyeGazeCueArrow;

        // Trail: 01 - Sphere Head
        public GameObject eyeGazeCueTrailWithSphereHead;
        public ParticleSystem particleSystemTrailWithSphereHead;

        public GameObject eyeGazeCueSpotlight;
        public Light spotLight;

        public GameObject eyeGazeCuePointlight;
        public Light pointLight;

        private PhotonView photonView;
        private Vector3 rayHitPositionLH;
        private Vector3 rayHitPositionRH;

        public GameObject textHoverAtomGO;
        public TextMesh textm;
        private TextMeshProUGUI dText;

        //public LineRenderer debugLine1;
        //public LineRenderer debugLine2;

        void Awake()
        {
            photonView = GetComponent<PhotonView>();
        }

        void Start()
        {
            eyeGazeCuePointer.SetActive(false);
            eyeGazeCueArrow.SetActive(false);
            eyeGazeCueTrailWithSphereHead.SetActive(false);
            eyeGazeCueSpotlight.SetActive(false);
            eyeGazeCuePointlight.SetActive(false);

            GameObject textMeshPro = GameObject.Find("Protein Viz");
            textMeshProteinViz = textMeshPro.GetComponent<TextMeshProUGUI>();

            dText = GameObject.Find("DebugText02").GetComponent<TextMeshProUGUI>();

            if (photonView.IsMine)
            {
                textHoverAtomGO.SetActive(false);
            }
        }

        void Update()
        {
            if (camTransform == null)
            {
                camTransform = Camera.main.transform;
            }

            if (!photonView.IsMine)
                return;

            GameObject textMeshPro = GameObject.Find("Protein Viz");
            textMeshProteinViz = textMeshPro.GetComponent<TextMeshProUGUI>();

            // This hovering part needs to be redesigned from scratch for multiplayer. Still not working as expected.
            if (GameObject.Find("HoverAtomName") != null && GameObject.Find("AtomHovering") != null)
            {
                TextMeshProUGUI hoverAtomName = GameObject.Find("HoverAtomName").GetComponent<TextMeshProUGUI>();
                GameObject atomHoveringGO = GameObject.Find("AtomHovering");

                if (hoverAtomName.text != "")
                {
                    photonView.RPC("ShowHoverAtomText", RpcTarget.All, hoverAtomName.text, atomHoveringGO.transform.position);
                }
                else
                {
                    photonView.RPC("DisablingHoverAtomText", RpcTarget.All);
                }
            }

            // This part is for eye-gaze visualization            
            if (GameObject.Find("eye-pointer") != null && GameObject.Find("SelectingEyeGazeViz") != null)
            {
                GameObject eyePointer = GameObject.Find("eye-pointer");

                GameObject selectingEyeGazeViz = GameObject.Find("SelectingEyeGazeViz");
                foreach (Transform child in selectingEyeGazeViz.transform)
                {
                    if (child.gameObject.activeSelf)
                    {
                        gazeVizType = child.gameObject.name;
                    }
                }
                if (eyePointer != null)
                {
                    photonView.RPC("EnableGazeCue", RpcTarget.All, eyePointer.transform.position, gazeVizType, textMeshProteinViz.text);
                }
            }


            if (photonView.IsMine)
            {
                head.gameObject.SetActive(false);
                leftHand.gameObject.SetActive(false);
                rightHand.gameObject.SetActive(false);

                MappingPosRot(head, XRNode.Head);
                MappingPosRot(leftHand, XRNode.LeftHand);
                MappingPosRot(rightHand, XRNode.RightHand);

                leftHandLineRenderer.gameObject.SetActive(false);
                rightHandLineRenderer.gameObject.SetActive(false);

                leftSphereReticle.SetActive(false);
                rightSphereReticle.SetActive(false);
            }

            leftHandLineRenderer.SetPosition(0, leftHand.position);
            rightHandLineRenderer.SetPosition(0, rightHand.position);

            if (GameObject.Find("SphereLH") == null)
            {
                rayHitPositionLH = leftHand.forward * 20 + leftHand.position;
                photonView.RPC(
                    "ShowLineRenderer",
                    RpcTarget.Others,
                    leftHand.position,
                    rayHitPositionLH,
                    "left-hand"
                );
                photonView.RPC(
                    "ShowEndPointer",
                    RpcTarget.Others,
                    rayHitPositionLH,
                    false,
                    "left-hand"
                );
            }
            else
            {
                GameObject sphereLH = GameObject.Find("SphereLH");
                rayHitPositionLH = sphereLH.transform.position;
                photonView.RPC(
                    "ShowLineRenderer",
                    RpcTarget.Others,
                    leftHand.position,
                    rayHitPositionLH,
                    "left-hand"
                );
                photonView.RPC("ShowEndPointer", RpcTarget.Others, rayHitPositionLH, true, "left-hand");
            }

            if (GameObject.Find("SphereRH") == null)
            {
                rayHitPositionRH = rightHand.forward * 20 + rightHand.position;
                photonView.RPC(
                    "ShowLineRenderer",
                    RpcTarget.Others,
                    rightHand.position,
                    rayHitPositionRH,
                    "right-hand"
                );
                photonView.RPC(
                    "ShowEndPointer",
                    RpcTarget.Others,
                    rayHitPositionRH,
                    false,
                    "right-hand"
                );
            }
            else
            {
                GameObject sphereRH = GameObject.Find("SphereRH");
                rayHitPositionRH = sphereRH.transform.position;
                photonView.RPC(
                    "ShowLineRenderer",
                    RpcTarget.Others,
                    rightHand.position,
                    rayHitPositionRH,
                    "right-hand"
                );
                photonView.RPC(
                    "ShowEndPointer",
                    RpcTarget.Others,
                    rayHitPositionRH,
                    true,
                    "right-hand"
                );
            }
        }



        [PunRPC]
        void ShowHoverAtomText(string atomText, Vector3 atomPos)
        {
            textHoverAtomGO.SetActive(true);

            textHoverAtomGO.transform.position = atomPos;
            textHoverAtomGO.transform.rotation = Quaternion.LookRotation(
                textHoverAtomGO.transform.position - camTransform.position
            );

            textm.text = atomText;
        }

        [PunRPC]
        void DisablingHoverAtomText()
        {
            if (textHoverAtomGO.activeSelf == true)
            {
                textHoverAtomGO.transform.position = new Vector3(0, -1000, 0);
                textHoverAtomGO.SetActive(false);
            }
        }


        [PunRPC]
        void EnableGazeCue(Vector3 pos, string gazeVizName, string proteinVizName)
        {
            Color receivedColor = new Color(1.0f, 0, 0, 1.0f); //float r, float g, float b, float a
            Vector3 remotePos = new Vector3(0, 0, 0);
            float radius = 0.12f;


            if (gazeVizName == "GazePointer")
            {
                eyeGazeCuePointer.SetActive(true);
                eyeGazeCuePointer.transform.position = pos;

                eyeGazeCueArrow.SetActive(false);
                eyeGazeCueTrailWithSphereHead.SetActive(false);
                eyeGazeCueSpotlight.SetActive(false);
                eyeGazeCuePointlight.SetActive(false);
                resetLight();
            }

            if (gazeVizName == "GazeArrow")
            {
                eyeGazeCueArrow.SetActive(true);
                eyeGazeCueArrow.transform.position = pos + new Vector3(0, 0.055f, 0);

                Vector3 cameraRotation = camTransform.rotation.eulerAngles;
                eyeGazeCueArrow.transform.rotation = Quaternion.Euler(new Vector3(0, cameraRotation.y, 0));

                eyeGazeCuePointer.SetActive(false);
                eyeGazeCueTrailWithSphereHead.SetActive(false);
                eyeGazeCueSpotlight.SetActive(false);
                eyeGazeCuePointlight.SetActive(false);
                resetLight();
            }

            if (gazeVizName == "GazeTrail(S)")
            {
                eyeGazeCueTrailWithSphereHead.SetActive(true);
                eyeGazeCueTrailWithSphereHead.transform.position = pos;

                eyeGazeCuePointer.SetActive(false);
                eyeGazeCueArrow.SetActive(false);
                eyeGazeCueSpotlight.SetActive(false);
                eyeGazeCuePointlight.SetActive(false);
                resetLight();
            }

            if (gazeVizName == "GazeTrail(A)")
            {
                eyeGazeCuePointer.SetActive(false);
                eyeGazeCueArrow.SetActive(false);
                eyeGazeCueSpotlight.SetActive(false);
                eyeGazeCuePointlight.SetActive(false);
                eyeGazeCueTrailWithSphereHead.SetActive(false);
                resetLight();
            }

            if (gazeVizName == "GazeSpotlight(S)" && proteinVizName == "Cartoon")
            {
                enablePointLightForCartoonAndHyperBall(pos, remotePos, radius, receivedColor);
            }

            if (gazeVizName == "GazeSpotlight(S)" && proteinVizName == "HyperBall")
            {
                enablePointLightForCartoonAndHyperBall(pos, remotePos, radius, receivedColor);
            }

            if (gazeVizName == "GazeSpotlight(S)" && proteinVizName == "Surface")
            {
                enableSpotLightForSurfaces(pos, remotePos, radius, receivedColor);

                /*
                eyeGazeCueArrow.SetActive(false);
                eyeGazeCueTrailWithSphereHead.SetActive(false);
                eyeGazeCueTrailWithArrowHead.SetActive(false);
                eyeGazeCuePointlight.SetActive(false);

                eyeGazeCuePointer.SetActive(true);
                eyeGazeCuePointer.transform.position = pos;

                eyeGazeCueSpotlight.SetActive(true);
                eyeGazeCueSpotlight.transform.position = pos - new Vector3(0.0f, 0.0f, 0.45f);
                */
            }


            if (gazeVizName == "GazeTurnOff")
            {
                eyeGazeCuePointer.SetActive(false);
                eyeGazeCueArrow.SetActive(false);
                eyeGazeCueTrailWithSphereHead.SetActive(false);
                eyeGazeCueSpotlight.SetActive(false);
                eyeGazeCuePointlight.SetActive(false);
                resetLight();

                /*
                if (proteinVizName == "Cartoon" || proteinVizName == "HyperBall" || proteinVizName == "Surface")
                {
                    resetLight();                    
                }
                */
            }
        }

        void enableSpotLightForSurfaces(Vector3 pos, Vector3 remotePos, float radius, Color receivedColor)
        {
            eyeGazeCuePointer.SetActive(true);
            eyeGazeCuePointer.transform.position = pos;

            eyeGazeCueArrow.SetActive(false);
            eyeGazeCueTrailWithSphereHead.SetActive(false);
            eyeGazeCuePointlight.SetActive(false);
            eyeGazeCueSpotlight.SetActive(false);

            GameObject atomSurfaceRep = GameObject.Find("AtomSurfaceRepresentation");
            if (atomSurfaceRep != null)
            {
                int childCount = atomSurfaceRep.transform.childCount;

                for (int i = 0; i < childCount; i++)
                {
                    Transform childTransform = atomSurfaceRep.transform.GetChild(i);
                    GameObject childGameObject = childTransform.gameObject;

                    Renderer renderer = childGameObject.GetComponent<Renderer>();
                    Material material = renderer.material;

                    material.SetVector("_SpotLight2Pos", new Vector4(pos.x, pos.y, pos.z, 1));
                    material.SetColor("_SpotLight2Color", spotLight.color);
                    material.SetFloat("_SpotLight2Range", spotLight.range);
                    material.SetFloat("_SpotLight2Intensity", spotLight.intensity);
                }
            }
        }

        void enablePointLightForCartoonAndHyperBall(Vector3 pos, Vector3 remotePos, float radius, Color receivedColor)
        {
            eyeGazeCuePointlight.SetActive(true);
            eyeGazeCuePointlight.transform.position = pos;

            eyeGazeCuePointer.SetActive(false);
            eyeGazeCueArrow.SetActive(false);
            eyeGazeCueTrailWithSphereHead.SetActive(false);
            eyeGazeCueSpotlight.SetActive(false);

            GameObject atomOptiHBRep = GameObject.Find("AtomOptiHBRepresentation");
            if (atomOptiHBRep != null)
            {
                int childCount = atomOptiHBRep.transform.childCount;

                for (int i = 0; i < childCount; i++)
                {
                    Transform childTransform = atomOptiHBRep.transform.GetChild(i);
                    GameObject childGameObject = childTransform.gameObject;

                    Renderer renderer = childGameObject.GetComponent<Renderer>();
                    Material material = renderer.material;

                    material.SetVector("_PointLightPosition1", new Vector4(pos.x, pos.y, pos.z, 1));
                    material.SetColor("_PointLightColor1", pointLight.color);
                    material.SetFloat("_PointLightRadius1", pointLight.range);

                    /*
                    material.SetVector("_PointLightPosition0", new Vector4(pos.x, pos.y, pos.z, 1));
                    material.SetColor("_PointLightColor0", pointLight.color);
                    material.SetFloat("_PointLightRadius0", pointLight.range);

                    material.SetVector("_PointLightPosition1", new Vector4(remotePos.x, remotePos.y, remotePos.z, 1));
                    material.SetColor("_PointLightColor1", receivedColor);
                    material.SetFloat("_PointLightRadius1", radius);
                    */
                }
            }

            GameObject bondOptiHSRep = GameObject.Find("BondOptiHSRepresentation");
            if (bondOptiHSRep != null)
            {
                int childCount = bondOptiHSRep.transform.childCount;

                for (int i = 0; i < childCount; i++)
                {
                    Transform childTransform = bondOptiHSRep.transform.GetChild(i);
                    GameObject childGameObject = childTransform.gameObject;

                    Renderer renderer = childGameObject.GetComponent<Renderer>();
                    Material material = renderer.material;

                    material.SetVector("_PointLightPosition1", new Vector4(pos.x, pos.y, pos.z, 1));
                    material.SetColor("_PointLightColor1", pointLight.color);
                    material.SetFloat("_PointLightRadius1", pointLight.range);

                    /*
                    material.SetVector("_PointLightPosition0", new Vector4(pos.x, pos.y, pos.z, 1));
                    material.SetColor("_PointLightColor0", pointLight.color);
                    material.SetFloat("_PointLightRadius0", pointLight.range);

                    material.SetVector("_PointLightPosition1", new Vector4(remotePos.x, remotePos.y, remotePos.z, 1));
                    material.SetColor("_PointLightColor1", receivedColor);
                    material.SetFloat("_PointLightRadius1", radius);
                    */
                }
            }
        }


        void resetLight()
        {
            if (GameObject.Find("AtomOptiHBRepresentation") != null)
            {
                GameObject atomOptiHBRep = GameObject.Find("AtomOptiHBRepresentation");
                int childCount = atomOptiHBRep.transform.childCount;

                for (int i = 0; i < childCount; i++)
                {
                    Transform childTransform = atomOptiHBRep.transform.GetChild(i);
                    GameObject childGameObject = childTransform.gameObject;

                    Renderer renderer = childGameObject.GetComponent<Renderer>();
                    Material material = renderer.material;

                    material.SetVector("_PointLightPosition0", new Vector4(0, -1000, 0, 1));
                    material.SetVector("_PointLightPosition1", new Vector4(0, -1000, 0, 1));
                }
            }

            if (GameObject.Find("BondOptiHSRepresentation") != null)
            {
                GameObject bondOptiHSRep = GameObject.Find("BondOptiHSRepresentation");
                int childCount = bondOptiHSRep.transform.childCount;

                for (int i = 0; i < childCount; i++)
                {
                    Transform childTransform = bondOptiHSRep.transform.GetChild(i);
                    GameObject childGameObject = childTransform.gameObject;

                    Renderer renderer = childGameObject.GetComponent<Renderer>();
                    Material material = renderer.material;

                    material.SetVector("_PointLightPosition0", new Vector4(0, -1000, 0, 1));
                    material.SetVector("_PointLightPosition1", new Vector4(0, -1000, 0, 1));
                }
            }

            GameObject atomSurfaceRep = GameObject.Find("AtomSurfaceRepresentation");
            if (atomSurfaceRep != null)
            {
                int childCount = atomSurfaceRep.transform.childCount;
                for (int i = 0; i < childCount; i++)
                {
                    Transform childTransform = atomSurfaceRep.transform.GetChild(i);
                    GameObject childGameObject = childTransform.gameObject;

                    Renderer renderer = childGameObject.GetComponent<Renderer>();
                    Material material = renderer.material;

                    material.SetVector("_SpotLight2Pos", new Vector4(0, -1000, 0, 1));
                    material.SetColor("_SpotLight2Color", spotLight.color);
                    material.SetFloat("_SpotLight2Range", spotLight.range);
                    material.SetFloat("_SpotLight2Intensity", spotLight.intensity);
                }
            }
        }

        [PunRPC]
        void ShowLineRenderer(Vector3 rayStartPos, Vector3 rayEndPos, string whichHand)
        {
            if (whichHand == "left-hand")
            {
                leftHandLineRenderer.SetPosition(0, rayStartPos);
                leftHandLineRenderer.SetPosition(1, rayEndPos);
            }

            if (whichHand == "right-hand")
            {
                rightHandLineRenderer.SetPosition(0, rayStartPos);
                rightHandLineRenderer.SetPosition(1, rayEndPos);
            }
        }

        [PunRPC]
        void ShowEndPointer(Vector3 pos, bool flag, string whichHand)
        {
            if (flag == true)
            {
                if (whichHand == "left-hand")
                {
                    leftSphereReticle.gameObject.SetActive(true);
                    leftSphereReticle.transform.position = pos;
                }

                if (whichHand == "right-hand")
                {
                    rightSphereReticle.gameObject.SetActive(true);
                    rightSphereReticle.transform.position = pos;
                }
            }

            if (flag == false)
            {
                if (whichHand == "left-hand")
                {
                    leftSphereReticle.gameObject.SetActive(false);
                }

                if (whichHand == "right-hand")
                {
                    rightSphereReticle.gameObject.SetActive(false);
                }
            }
        }

        void MappingPosRot(Transform target, XRNode node)
        {
            InputDevices
                .GetDeviceAtXRNode(node)
                .TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position);
            InputDevices
                .GetDeviceAtXRNode(node)
                .TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation);

            target.position = position;
            target.rotation = rotation;
        }
    }
}