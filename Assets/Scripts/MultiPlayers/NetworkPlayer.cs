using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Photon.Pun;
using TMPro;
using System;

namespace UnityEngine.UI.Extensions.ColorPicker
{
    public class NetworkPlayer : MonoBehaviour
    {
        public Transform head;
        public Transform leftHand;
        public Transform rightHand;

        public LineRenderer leftHandLineRenderer;
        public LineRenderer rightHandLineRenderer;

        public GameObject leftSphereReticle;
        public GameObject rightSphereReticle;

        private bool controllerRayHittingProteinFlagRH;
        private bool controllerRayHittingProteinFlagLH;

        private Transform camTransform;
        private string gazeVizType;
        private TextMeshProUGUI textMeshProteinViz;

        public GameObject eyeGazeCuePointer;
        private Renderer eyeGazeCuePointerRenderer;
        private Vector3 initialScaleGazePointer;

        public GameObject eyeGazeCueArrow;
        private Renderer eyeGazeCueArrowRenderer;
        private Vector3 initialScaleGazeArrow;

        // Trail: 01 - Sphere Head
        public GameObject eyeGazeCueTrailWithSphereHead;
        public ParticleSystem particleSystemTrailWithSphereHead;
        private Renderer eyeGazeCueTrailSphereHeadRenderer;
        private Toggle sphereHeadToggle;
        private Toggle trailToggle;
        private bool sphereHeadFlag;
        private bool trailFlag;

        // Trail: 02 - Arrow Head
        public GameObject eyeGazeCueTrailWithArrowHead;
        public ParticleSystem particleSystemTrailWithArrowHead;
        private Renderer eyeGazeCueTrailArrowHeadRenderer;
        private Toggle arrowHeadToggle;
        private bool arrowHeadFlag;


        public GameObject eyeGazeCueSpotlight;
        public Light spotLight;

        public GameObject eyeGazeCuePointlight;
        private Renderer eyeGazeCuePointlightHeadRenderer;
        public Light pointLight;
        private float initialPointLightRange;
        private Toggle lightToggle;
        private bool lightFlag;

        private PhotonView photonView;
        private Vector3 rayHitPositionLH;
        private Vector3 rayHitPositionRH;

        public GameObject textHoverAtomGO;
        public TextMesh textm;

        private ColorPickerControl colorPicker;
        private Slider slider;

        private TextMeshProUGUI dText;

        private DataSynchronization remotePlayerData;
        private float pointLightColorR, pointLightColorG, pointLightColorB, pointLightColorA;
        private float spotLightColorR, spotLightColorG, spotLightColorB, spotLightColorA;

        void Awake()
        {
            photonView = GetComponent<PhotonView>();

            if (photonView.IsMine)
            {
                if (GameObject.Find("ColorPickerUI") != null && GameObject.Find("SliderController") != null)
                {
                    colorPicker = GameObject.Find("ColorPickerUI").GetComponent<ColorPickerControl>();
                    slider = GameObject.Find("SliderController").GetComponent<Slider>();
                }
            }
        }

        void Start()
        {
            // get all eye-viz renderer

            eyeGazeCuePointerRenderer = eyeGazeCuePointer.GetComponent<Renderer>();
            eyeGazeCueArrowRenderer = eyeGazeCueArrow.transform.GetChild(0).GetComponent<Renderer>();
            eyeGazeCueTrailSphereHeadRenderer = eyeGazeCueTrailWithSphereHead.GetComponent<Renderer>();
            eyeGazeCueTrailArrowHeadRenderer = eyeGazeCueTrailWithArrowHead.transform.GetChild(0).GetComponent<Renderer>();
            eyeGazeCuePointlightHeadRenderer = eyeGazeCuePointlight.GetComponent<Renderer>();

            initialScaleGazePointer = eyeGazeCuePointer.transform.localScale;
            initialScaleGazeArrow = eyeGazeCueArrow.transform.localScale;
            initialPointLightRange = pointLight.range;

            // assign toggle check box
            sphereHeadToggle = GameObject.Find("Toggle-Sphere").GetComponent<Toggle>();
            sphereHeadFlag = sphereHeadToggle.isOn;

            arrowHeadToggle = GameObject.Find("Toggle-Arrow").GetComponent<Toggle>();
            arrowHeadFlag = arrowHeadToggle.isOn;

            trailToggle = GameObject.Find("Toggle-Trail").GetComponent<Toggle>();
            trailFlag = trailToggle.isOn;

            lightToggle = GameObject.Find("Toggle-Light").GetComponent<Toggle>();
            lightFlag = lightToggle.isOn;

            eyeGazeCuePointer.SetActive(false);
            eyeGazeCueArrow.SetActive(false);
            eyeGazeCueTrailWithSphereHead.SetActive(false);
            eyeGazeCueTrailWithArrowHead.SetActive(false);
            eyeGazeCueSpotlight.SetActive(false);
            eyeGazeCuePointlight.SetActive(false);

            GameObject textMeshPro = GameObject.Find("Protein Viz");
            textMeshProteinViz = textMeshPro.GetComponent<TextMeshProUGUI>();

            dText = GameObject.Find("DebugText02").GetComponent<TextMeshProUGUI>();

            if (photonView.IsMine)
            {
                colorPicker.onValueChanged.AddListener(color =>
                {
                    if (gazeVizType == "GazePointer")
                    {
                        photonView.RPC("GazePointerChangeColorRPC", RpcTarget.All, new object[] { color.r, color.g, color.b, color.a });
                    }
                    if (gazeVizType == "GazeArrow")
                    {
                        photonView.RPC("GazeArrowChangeColorRPC", RpcTarget.All, new object[] { color.r, color.g, color.b, color.a });
                    }
                    if (gazeVizType == "GazeTrail(S)")
                    {
                        photonView.RPC("GazeTrailChangeColorRPC", RpcTarget.All, new object[] { color.r, color.g, color.b, color.a });
                    }
                    if (gazeVizType == "GazeTrail(A)")
                    {
                        photonView.RPC("GazeTrailChangeColorRPC02", RpcTarget.All, new object[] { color.r, color.g, color.b, color.a });
                    }
                    if (gazeVizType == "GazeSpotlight(S)") // Point Light
                    {
                        if (textMeshProteinViz.text == "Surface")
                        {
                            photonView.RPC("GazeSpotlightChangeColorRPC", RpcTarget.All, new object[] { color.r, color.g, color.b, color.a });
                        }
                        if (textMeshProteinViz.text == "HyperBall" || textMeshProteinViz.text == "Cartoon")
                        {
                            photonView.RPC("GazePointlightChangeColorRPC", RpcTarget.All, new object[] { color.r, color.g, color.b, color.a });
                        }
                    }
                });

                slider.onValueChanged.AddListener(delegate
                {
                    float scaleFactor = slider.value;

                    if (gazeVizType == "GazePointer")
                    {
                        photonView.RPC("GazePointerChangeScaleRPC", RpcTarget.All, scaleFactor);
                    }
                    if (gazeVizType == "GazeArrow")
                    {
                        photonView.RPC("GazeArrowChangeScaleRPC", RpcTarget.All, scaleFactor);
                    }
                    if (gazeVizType == "GazeSpotlight(S)")
                    {
                        photonView.RPC("PointLightChangeRadiusRPC", RpcTarget.All, scaleFactor);
                    }
                });

                textHoverAtomGO.SetActive(false);
            }
        }

        void Update()
        {
            GameObject remotePlayer = GameObject.FindGameObjectWithTag("RemotePlayer");
            if (remotePlayer)
            {
                remotePlayerData = remotePlayer.GetComponent<DataSynchronization>();

                pointLightColorR = remotePlayerData.pointLightColor.r;
                pointLightColorG = remotePlayerData.pointLightColor.g;
                pointLightColorB = remotePlayerData.pointLightColor.b;
                pointLightColorA = remotePlayerData.pointLightColor.a;

                spotLightColorR = remotePlayerData.spotLightColor.r;
                spotLightColorG = remotePlayerData.spotLightColor.g;
                spotLightColorB = remotePlayerData.spotLightColor.b;
                spotLightColorA = remotePlayerData.spotLightColor.a;


                //dText.text = "POS: " + remotePlayerPointLightData.latestEyePosition + " Color: " 
                //+ remotePlayerPointLightData.pointLightColor + " Radius: " + remotePlayerPointLightData.pointLightRadius;
            }

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
                if (eyePointer != null && remotePlayerData != null)
                {
                    photonView.RPC("EnableGazeCue", RpcTarget.All, eyePointer.transform.position,
                            gazeVizType, textMeshProteinViz.text,
                            remotePlayerData.latestEyePosition, remotePlayerData.pointLightRadius,
                            pointLightColorR, pointLightColorG, pointLightColorB, pointLightColorA,
                            remotePlayerData.spotLightRange, remotePlayerData.spotLightIntensity,
                            spotLightColorR, spotLightColorG, spotLightColorB, spotLightColorA);
                }
            }

            // toggle check box
            sphereHeadFlag = sphereHeadToggle.isOn;
            arrowHeadFlag = arrowHeadToggle.isOn;
            trailFlag = trailToggle.isOn;
            lightFlag = lightToggle.isOn;

            if (photonView.IsMine)
            {
                head.gameObject.SetActive(true);
                AssignLayerToPlayer(head.gameObject, LayerMask.NameToLayer("Lay15"));

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

            // Right Hand
            if (GameObject.Find("ControllerRayHitsProteinRH") == null)
            {
                controllerRayHittingProteinFlagRH = true;
            }
            else
            {
                controllerRayHittingProteinFlagRH = false;
            }

            // Left Hand
            if (GameObject.Find("ControllerRayHitsProteinLH") == null)
            {
                controllerRayHittingProteinFlagLH = true;
            }
            else
            {
                controllerRayHittingProteinFlagLH = false;
            }


            if (GameObject.Find("SphereLH") == null)
            {
                rayHitPositionLH = leftHand.forward * 20 + leftHand.position;
                photonView.RPC("ShowLineRenderer", RpcTarget.Others, leftHand.position, rayHitPositionLH, "left-hand");
                photonView.RPC("ShowEndPointer", RpcTarget.Others, rayHitPositionLH, false, "left-hand", controllerRayHittingProteinFlagLH);
            }
            else
            {
                GameObject sphereLH = GameObject.Find("SphereLH");
                rayHitPositionLH = sphereLH.transform.position;
                photonView.RPC("ShowLineRenderer", RpcTarget.Others, leftHand.position, rayHitPositionLH, "left-hand");
                photonView.RPC("ShowEndPointer", RpcTarget.Others, rayHitPositionLH, true, "left-hand", controllerRayHittingProteinFlagLH);
            }

            if (GameObject.Find("SphereRH") == null)
            {
                rayHitPositionRH = rightHand.forward * 20 + rightHand.position;
                photonView.RPC("ShowLineRenderer", RpcTarget.Others, rightHand.position, rayHitPositionRH, "right-hand");
                photonView.RPC("ShowEndPointer", RpcTarget.Others, rayHitPositionRH, false, "right-hand", controllerRayHittingProteinFlagRH);
            }
            else
            {
                GameObject sphereRH = GameObject.Find("SphereRH");
                rayHitPositionRH = sphereRH.transform.position;
                photonView.RPC("ShowLineRenderer", RpcTarget.Others, rightHand.position, rayHitPositionRH, "right-hand");
                photonView.RPC("ShowEndPointer", RpcTarget.Others, rayHitPositionRH, true, "right-hand", controllerRayHittingProteinFlagRH);
            }
        }

        [PunRPC]
        void GazePointerChangeColorRPC(float r, float g, float b, float a)
        {
            eyeGazeCuePointerRenderer.material.color = new Color(r, g, b, a);
        }

        [PunRPC]
        void GazeArrowChangeColorRPC(float r, float g, float b, float a)
        {
            eyeGazeCueArrowRenderer.material.color = new Color(r, g, b, a);
        }

        [PunRPC]
        void GazeTrailChangeColorRPC(float r, float g, float b, float a)
        {
            if (sphereHeadFlag && trailFlag)
            {
                eyeGazeCueTrailSphereHeadRenderer.material.color = new Color(r, g, b, a);
                particleColorChange(r, g, b, a);
            }
            if (sphereHeadFlag)
            {
                eyeGazeCueTrailSphereHeadRenderer.material.color = new Color(r, g, b, a);
            }
            if (trailFlag)
            {
                particleColorChange(r, g, b, a);
            }
        }

        [PunRPC]
        void GazeTrailChangeColorRPC02(float r, float g, float b, float a)
        {
            if (arrowHeadFlag && trailFlag)
            {
                eyeGazeCueTrailArrowHeadRenderer.material.color = new Color(r, g, b, a);
                particleColorChange02(r, g, b, a);
            }
            if (arrowHeadFlag)
            {
                eyeGazeCueTrailArrowHeadRenderer.material.color = new Color(r, g, b, a);
            }
            if (trailFlag)
            {
                particleColorChange02(r, g, b, a);
            }
        }

        private void particleColorChange02(float r, float g, float b, float a)
        {
            Color particleColor = new Color(r, g, b, a);
            if (particleSystemTrailWithArrowHead != null)
            {
                // Change the start color
                var main = particleSystemTrailWithArrowHead.main;
                main.startColor = particleColor;

                // Set the color over lifetime to the same color
                var colorOverLifetime = particleSystemTrailWithArrowHead.colorOverLifetime;
                colorOverLifetime.enabled = true;

                UnityEngine.Gradient gradient = new UnityEngine.Gradient();

                GradientColorKey[] colorKey = new GradientColorKey[2];
                colorKey[0].color = particleColor;
                colorKey[0].time = 0.0f;
                colorKey[1].color = particleColor;
                colorKey[1].time = 1.0f;

                GradientAlphaKey[] alphaKey = new GradientAlphaKey[2];
                alphaKey[0].alpha = particleColor.a;
                alphaKey[0].time = 0.0f;
                alphaKey[1].alpha = particleColor.a;
                alphaKey[1].time = 1.0f;

                gradient.colorKeys = colorKey;
                gradient.alphaKeys = alphaKey;

                colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
            }
        }

        private void particleColorChange(float r, float g, float b, float a)
        {
            Color particleColor = new Color(r, g, b, a);
            if (particleSystemTrailWithSphereHead != null)
            {
                // Change the start color
                var main = particleSystemTrailWithSphereHead.main;
                main.startColor = particleColor;

                // Set the color over lifetime to the same color
                var colorOverLifetime = particleSystemTrailWithSphereHead.colorOverLifetime;
                colorOverLifetime.enabled = true;

                UnityEngine.Gradient gradient = new UnityEngine.Gradient();

                GradientColorKey[] colorKey = new GradientColorKey[2];
                colorKey[0].color = particleColor;
                colorKey[0].time = 0.0f;
                colorKey[1].color = particleColor;
                colorKey[1].time = 1.0f;

                GradientAlphaKey[] alphaKey = new GradientAlphaKey[2];
                alphaKey[0].alpha = particleColor.a;
                alphaKey[0].time = 0.0f;
                alphaKey[1].alpha = particleColor.a;
                alphaKey[1].time = 1.0f;

                gradient.colorKeys = colorKey;
                gradient.alphaKeys = alphaKey;

                colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
            }
        }

        [PunRPC]
        void GazeSpotlightChangeColorRPC(float r, float g, float b, float a)
        {
            eyeGazeCuePointerRenderer.material.color = new Color(r, g, b, a);
            spotLight.color = new Color(r, g, b, a);
        }

        [PunRPC]
        void GazePointlightChangeColorRPC(float r, float g, float b, float a)
        {
            if (sphereHeadFlag && lightFlag)
            {
                eyeGazeCuePointlightHeadRenderer.material.color = new Color(r, g, b, a);
                pointLight.color = new Color(r, g, b, a);
            }
            if (sphereHeadFlag)
            {
                eyeGazeCuePointlightHeadRenderer.material.color = new Color(r, g, b, a);
            }
            if (lightFlag)
            {
                pointLight.color = new Color(r, g, b, a);
            }
        }

        [PunRPC]
        void GazePointerChangeScaleRPC(float scaleFactor)
        {
            eyeGazeCuePointer.transform.localScale = initialScaleGazePointer * scaleFactor;
        }

        [PunRPC]
        void GazeArrowChangeScaleRPC(float scaleFactor)
        {
            eyeGazeCueArrow.transform.localScale = initialScaleGazeArrow * scaleFactor;
        }

        [PunRPC]
        void PointLightChangeRadiusRPC(float scaleFactor)
        {
            pointLight.range = initialPointLightRange * scaleFactor;
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
        void EnableGazeCue(Vector3 pos, string gazeVizName, string proteinVizName, Vector3 remotePos,
            float radiusPointLight, float rPointLight, float gPointLight, float bPointLight, float aPointLight,
                float rangeSpotLight, float intensitySpotLight, float rSpotLight, float gSpotLight, float bSpotLight, float aSpotLight)
        {
            Color receivedPointLightColor = new Color(rPointLight, gPointLight, bPointLight, aPointLight);
            Color receivedSpotLightColor = new Color(rSpotLight, gSpotLight, bSpotLight, aSpotLight);

            if (gazeVizName == "GazePointer")
            {
                eyeGazeCuePointer.SetActive(true);
                eyeGazeCuePointer.transform.position = pos;

                eyeGazeCueArrow.SetActive(false);
                eyeGazeCueTrailWithSphereHead.SetActive(false);
                eyeGazeCueTrailWithArrowHead.SetActive(false);
                eyeGazeCueSpotlight.SetActive(false);
                eyeGazeCuePointlight.SetActive(false);

                if (proteinVizName == "Cartoon" || proteinVizName == "HyperBall" || proteinVizName == "Surface")
                {
                    resetLight();
                }
            }

            if (gazeVizName == "GazeArrow")
            {
                eyeGazeCueArrow.SetActive(true);
                eyeGazeCueArrow.transform.position = pos + new Vector3(0, 0.055f, 0);

                Vector3 cameraRotation = camTransform.rotation.eulerAngles;
                eyeGazeCueArrow.transform.rotation = Quaternion.Euler(new Vector3(0, cameraRotation.y, 0));

                eyeGazeCuePointer.SetActive(false);
                eyeGazeCueTrailWithSphereHead.SetActive(false);
                eyeGazeCueTrailWithArrowHead.SetActive(false);
                eyeGazeCueSpotlight.SetActive(false);
                eyeGazeCuePointlight.SetActive(false);

                if (proteinVizName == "Cartoon" || proteinVizName == "HyperBall" || proteinVizName == "Surface")
                {
                    resetLight();
                }
            }

            if (gazeVizName == "GazeTrail(S)")
            {
                eyeGazeCueTrailWithSphereHead.SetActive(true);
                eyeGazeCueTrailWithSphereHead.transform.position = pos;

                eyeGazeCuePointer.SetActive(false);
                eyeGazeCueArrow.SetActive(false);
                eyeGazeCueSpotlight.SetActive(false);
                eyeGazeCuePointlight.SetActive(false);
                eyeGazeCueTrailWithArrowHead.SetActive(false);

                if (proteinVizName == "Cartoon" || proteinVizName == "HyperBall" || proteinVizName == "Surface")
                {
                    resetLight();
                }
            }

            if (gazeVizName == "GazeTrail(A)")
            {
                eyeGazeCueTrailWithArrowHead.SetActive(true);
                eyeGazeCueTrailWithArrowHead.transform.position = pos + new Vector3(0, 0.035f, 0); ;

                Vector3 cameraRotation = camTransform.rotation.eulerAngles;
                eyeGazeCueTrailWithArrowHead.transform.rotation = Quaternion.Euler(new Vector3(0, cameraRotation.y, 0));

                eyeGazeCuePointer.SetActive(false);
                eyeGazeCueArrow.SetActive(false);
                eyeGazeCueSpotlight.SetActive(false);
                eyeGazeCuePointlight.SetActive(false);
                eyeGazeCueTrailWithSphereHead.SetActive(false);

                if (proteinVizName == "Cartoon" || proteinVizName == "HyperBall" || proteinVizName == "Surface")
                {
                    resetLight();
                }
            }

            if (gazeVizName == "GazeSpotlight(S)" && proteinVizName == "Cartoon")
            {
                enablePointLightForCartoonAndHyperBall(pos, remotePos, radiusPointLight, receivedPointLightColor);
            }

            if (gazeVizName == "GazeSpotlight(S)" && proteinVizName == "HyperBall")
            {
                enablePointLightForCartoonAndHyperBall(pos, remotePos, radiusPointLight, receivedPointLightColor);
            }

            if (gazeVizName == "GazeSpotlight(S)" && proteinVizName == "Surface")
            {
                enableSpotLightForSurfaces(pos, remotePos, rangeSpotLight, receivedSpotLightColor, intensitySpotLight);
            }

            if (gazeVizName == "GazeTurnOff")
            {
                eyeGazeCuePointer.SetActive(false);
                eyeGazeCueArrow.SetActive(false);
                eyeGazeCueTrailWithSphereHead.SetActive(false);
                eyeGazeCueTrailWithArrowHead.SetActive(false);
                eyeGazeCueSpotlight.SetActive(false);
                eyeGazeCuePointlight.SetActive(false);

                if (proteinVizName == "Cartoon" || proteinVizName == "HyperBall" || proteinVizName == "Surface")
                {
                    resetLight();
                }
            }
        }

        private void enableSpotLightForSurfaces(Vector3 pos, Vector3 remotePos, float rangeSpotLight, Color receivedSpotLightColor, float intensitySpotLight)
        {
            eyeGazeCuePointer.SetActive(true);
            eyeGazeCuePointer.transform.position = pos;

            eyeGazeCueArrow.SetActive(false);
            eyeGazeCueTrailWithSphereHead.SetActive(false);
            eyeGazeCueTrailWithArrowHead.SetActive(false);
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

                    material.SetVector("_SpotLight1Pos", new Vector4(pos.x, pos.y, pos.z, 1));
                    material.SetColor("_SpotLight1Color", spotLight.color);
                    material.SetFloat("_SpotLight1Range", spotLight.range);
                    material.SetFloat("_SpotLight1Intensity", spotLight.intensity);

                    material.SetVector("_SpotLight2Pos", new Vector4(remotePos.x, remotePos.y, remotePos.z, 1));
                    material.SetColor("_SpotLight2Color", receivedSpotLightColor);
                    material.SetFloat("_SpotLight2Range", rangeSpotLight);
                    material.SetFloat("_SpotLight2Intensity", intensitySpotLight);
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
            eyeGazeCueTrailWithArrowHead.SetActive(false);
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

                    material.SetVector("_PointLightPosition0", new Vector4(pos.x, pos.y, pos.z, 1));
                    material.SetColor("_PointLightColor0", pointLight.color);
                    material.SetFloat("_PointLightRadius0", pointLight.range);

                    material.SetVector("_PointLightPosition1", new Vector4(remotePos.x, remotePos.y, remotePos.z, 1));
                    material.SetColor("_PointLightColor1", receivedColor);
                    material.SetFloat("_PointLightRadius1", radius);
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

                    material.SetVector("_PointLightPosition0", new Vector4(pos.x, pos.y, pos.z, 1));
                    material.SetColor("_PointLightColor0", pointLight.color);
                    material.SetFloat("_PointLightRadius0", pointLight.range);

                    material.SetVector("_PointLightPosition1", new Vector4(remotePos.x, remotePos.y, remotePos.z, 1));
                    material.SetColor("_PointLightColor1", receivedColor);
                    material.SetFloat("_PointLightRadius1", radius);
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

                    material.SetVector("_SpotLight1Pos", new Vector4(0, -1000, 0, 1));
                    material.SetVector("_SpotLight2Pos", new Vector4(0, -1000, 0, 1));
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

                // Set the line width
                leftHandLineRenderer.startWidth = 0.0065f; // Example start width
                leftHandLineRenderer.endWidth = 0.0065f;   // Example end width
            }

            if (whichHand == "right-hand")
            {
                rightHandLineRenderer.SetPosition(0, rayStartPos);
                rightHandLineRenderer.SetPosition(1, rayEndPos);
                // Set the line width
                rightHandLineRenderer.startWidth = 0.0065f; // Example start width
                rightHandLineRenderer.endWidth = 0.0065f;   // Example end width
            }
        }


        //photonView.RPC("ShowEndPointer", RpcTarget.Others, rayHitPositionRH, true, "right-hand", controllerRayHittingProteinFlagRH);
        [PunRPC]
        void ShowEndPointer(Vector3 pos, bool flag, string whichHand, bool controllerRayHittingProteinFlag)
        {
            if (flag == true && controllerRayHittingProteinFlag == false)
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
        void AssignLayerToPlayer(GameObject playerObject, int layerIndex)
        {
            playerObject.layer = layerIndex;

            // Optionally, assign the layer to all child objects as well, since layer settings do not automatically propagate to children
            foreach (Transform child in playerObject.transform)
            {
                AssignLayerToPlayer(child.gameObject, layerIndex); // Recursive call to ensure all children are assigned the layer
            }
        }
    }
}