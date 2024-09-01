using UnityEngine;
using System.Collections;
using System.Text;
using Photon.Pun;

using HTC.UnityPlugin.Vive;
using HTC.UnityPlugin.Utility;

namespace UMol
{
    [RequireComponent(typeof(ViveRoleSetter))]
    [RequireComponent(typeof(PointerAtomSelection))]
    public class PointerHoverAtom : MonoBehaviourPunCallbacks
    {
        public GameObject textHoverAtomGO;

        PhotonView photonView;
        GameObject trajExtraGo;
        GameObject haloGo;
        TextMesh textm;
        Transform camTransform;
        string atomText;
        Vector3 atomPos;

        bool pressed = false;
        CustomRaycastBurst raycaster;

        float hoverScaleMultiplier = 1.0f;

        public bool pauseHovering = false;
        UnityMolAtom lastPointedAtom = null;

        ViveRoleProperty curRole;

        PointerAtomSelection pas;
        GameObject goAtom;

        void OnEnable()
        {
            curRole = GetComponent<ViveRoleSetter>().viveRole;

            pas = GetComponent<PointerAtomSelection>();
            if (curRole != null)
            {
                ViveInput.AddPressDown(
                    (HandRole)curRole.roleValue,
                    ControllerButton.PadTouch,
                    buttonPressed
                );
                ViveInput.AddPressUp(
                    (HandRole)curRole.roleValue,
                    ControllerButton.PadTouch,
                    buttonReleased
                );
            }
        }

        void OnDisable()
        {
            if (curRole != null)
            {
                ViveInput.RemovePressDown(
                    (HandRole)curRole.roleValue,
                    ControllerButton.PadTouch,
                    buttonPressed
                );
                ViveInput.RemovePressUp(
                    (HandRole)curRole.roleValue,
                    ControllerButton.PadTouch,
                    buttonReleased
                );
            }
        }

        void Start()
        {
            photonView = GetComponent<PhotonView>();

            raycaster = UnityMolMain.getCustomRaycast();

            haloGo = GameObject.Instantiate((GameObject)Resources.Load("Prefabs/SphereOverAtom"));
            textm = haloGo.GetComponentsInChildren<TextMesh>()[0];

            haloGo.SetActive(false);
            textm.gameObject.GetComponent<MeshRenderer>().sortingOrder = 50;

            trajExtraGo = new GameObject("DummyTrajExtractedGo");
            goAtom = new GameObject("HoverAtomGo");

            DontDestroyOnLoad(haloGo);
            DontDestroyOnLoad(trajExtraGo);
            DontDestroyOnLoad(goAtom);
        }

        void Update()
        {
            if (pauseHovering)
            {
                disableHovering();
                return;
            }
            if (pressed && !pas.isOverUI)
            {
                if (haloGo != null)
                {
                    showHover();
                }
            }
        }

        public static string formatAtomText(UnityMolAtom a)
        {
            string nameS = a.residue.chain.model.structure.formatName(25);

            string textAtom = "<size=30>" + nameS + " </size>\n";

            textAtom += "<color=white>" + a.residue.chain.name + "</color> | ";
            textAtom += "<color=white>" + a.residue.name + a.residue.id + "</color> | ";
            textAtom += "<b>" + a.name + "</b>";

            return textAtom;
        }

        void showHover()
        {
            if (haloGo == null)
            {
                Debug.LogWarning("haloGo is null. Cannot show hover.");
                return;
            }

            RigidPose cpose = VivePose.GetPose(curRole);

            Vector3 p = Vector3.zero;
            bool isExtrAtom = false;
            UnityMolAtom a = raycaster.customRaycastAtomBurst(
                cpose.pos,
                cpose.forward,
                ref p,
                ref isExtrAtom,
                true
            );

            if (a != null)
            {
                if (haloGo == null)
                {
                    haloGo = GameObject.Instantiate(
                        (GameObject)Resources.Load("Prefabs/SphereOverAtom")
                    );
                    textm = haloGo.GetComponentsInChildren<TextMesh>()[0];
                    haloGo.layer = LayerMask.NameToLayer("Ignore Raycast");
                    haloGo.SetActive(false);
                    DontDestroyOnLoad(haloGo);
                }

                UnityMolStructure s = a.residue.chain.model.structure;

                atomText = formatAtomText(a);
                textm.text = atomText;
                haloGo.SetActive(true); 

                atomPos = p;
                haloGo.transform.position = p;
                trajExtraGo.transform.position = p;

                if (camTransform == null)
                {
                    camTransform = Camera.main.transform;
                }

                haloGo.transform.rotation = Quaternion.LookRotation(
                    haloGo.transform.position - camTransform.position
                );

                //show hover-atom-text on the remote clinets
                photonView.RPC(
                    "EnableHoverAtomText",
                    RpcTarget.Others,
                    atomText,
                    atomPos,
                    camTransform.position
                );

                UnityMolMain.getAnnotationManager().setGOPos(a, goAtom);

                if (goAtom != null && haloGo != null)
                {
                    if (!isExtrAtom)
                    {
                        haloGo.transform.SetParent(goAtom.transform);
                    }
                    else
                    {
                        if (trajExtraGo != null)
                        {
                            trajExtraGo.transform.SetParent(goAtom.transform.parent);
                            trajExtraGo.transform.localScale = goAtom.transform.localScale;
                            haloGo.transform.SetParent(trajExtraGo.transform);
                        }
                    }
                }

                haloGo.transform.localScale = hoverScaleMultiplier * a.radius * Vector3.one * 1.1f;

                if (lastPointedAtom == null || lastPointedAtom != a)
                {
                    ViveInput.TriggerHapticPulseEx(curRole.roleType, curRole.roleValue, 500);
                }
                lastPointedAtom = a;
            }
        }

        void disableHovering()
        {
            if (haloGo != null)
            {
                haloGo.SetActive(false);
                haloGo.transform.parent = null;
            }

            //don't show hover-atom-text on the remote clinets
            photonView.RPC("DisableHoverAtomText", RpcTarget.Others);
        }

        void buttonReleased()
        {
            disableHovering();
            pressed = false;
            lastPointedAtom = null;
        }

        void buttonPressed()
        {
            pressed = true;
        }

        public static string ReplaceFirstOccurrance(
            string original,
            string oldValue,
            string newValue
        )
        {
            if (string.IsNullOrEmpty(original))
                return "";
            if (string.IsNullOrEmpty(oldValue))
                return original;
            int loc = original.IndexOf(oldValue);
            return original.Remove(loc, oldValue.Length).Insert(loc, newValue);
        }

        [PunRPC]
        void EnableHoverAtomText(string atomText, Vector3 atomPos, Vector3 camPos)
        {
            textHoverAtomGO.SetActive(true);
            textHoverAtomGO.transform.position = atomPos;
            textHoverAtomGO.transform.rotation = Quaternion.LookRotation(
                textHoverAtomGO.transform.position - camPos
            );

            TextMesh textm = textHoverAtomGO.GetComponentsInChildren<TextMesh>()[0];
            textm.gameObject.GetComponent<MeshRenderer>().sortingOrder = 50;
            textm.text = atomText;
        }

        [PunRPC]
        void DisableHoverAtomText()
        {
            if (textHoverAtomGO.activeSelf == true)
            {
                textHoverAtomGO.SetActive(false);
                return;
            }
        }
    }
}
