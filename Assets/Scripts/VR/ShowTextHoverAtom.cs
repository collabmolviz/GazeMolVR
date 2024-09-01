/*

The issue I am facing might be related to latency or a race condition. The photon networking library attempts to synchronize game state across all players, 
but there may be a slight delay in updating the position of the molecule across different players' instances of the game. When the raycast is performed and the hover text is displayed, 
it's possible that the position of the molecule hasn't been updated yet on the other clients, causing the text to appear in mid-air where the molecule used to be.

*/

using UnityEngine;
using System.Collections;
using System.Text;
using Photon.Pun;
using TMPro;

using HTC.UnityPlugin.Vive;
using HTC.UnityPlugin.Utility;

namespace UMol
{
    [RequireComponent(typeof(ViveRoleSetter))]
    [RequireComponent(typeof(PointerAtomSelection))]
    public class ShowTextHoverAtom : MonoBehaviourPunCallbacks
    {
        public GameObject hoverAtomGO;
        public TextMeshProUGUI hoverAtomName;

        PhotonView photonView;

        //TextMesh textm;

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

            //textm = textHoverAtomGO.GetComponentsInChildren<TextMesh>()[0];
            //textm.gameObject.GetComponent<MeshRenderer>().sortingOrder = 50;

            goAtom = new GameObject("HoverAtomGo");
            DontDestroyOnLoad(goAtom);
        }

        void Update()
        {
            if (pauseHovering)
            {
                disableHovering();
            }
            if (pressed && !pas.isOverUI)
            {
                showHover();
            }
        }

        public static string formatAtomText(UnityMolAtom a)
        {
            string nameS = a.residue.chain.model.structure.formatName(25);

            string textAtom = "\n<size=23>" + nameS + " </size>\n";

            textAtom += "<color=white>" + a.residue.chain.name + "</color> | ";
            textAtom += "<color=white>" + a.residue.name + a.residue.id + "</color> | ";
            textAtom += "<b>" + a.name + "</b>";

            return textAtom;
        }

        void showHover()
        {
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
                UnityMolStructure s = a.residue.chain.model.structure;

                atomText = formatAtomText(a);

                //textm.text = atomText;

                atomPos = p;

                if (camTransform == null)
                {
                    camTransform = Camera.main.transform;
                }

                hoverAtomGO.transform.position = atomPos;
                hoverAtomName.text = atomText;

                /*
                photonView.RPC(
                    "ShowHoverAtomText",
                    RpcTarget.All,
                    atomText,
                    atomPos,
                    camTransform.position
                );
                */

                if (goAtom != null)
                {
                    UnityMolMain.getAnnotationManager().setGOPos(a, goAtom);
                }

                if (lastPointedAtom == null || lastPointedAtom != a)
                {
                    ViveInput.TriggerHapticPulseEx(curRole.roleType, curRole.roleValue, 500);
                }

                lastPointedAtom = a;
            }
        }

        void disableHovering()
        {
            //photonView.RPC("DisablingHoverAtomText", RpcTarget.All);
            hoverAtomName.text = "";
        }

        void buttonPressed()
        {
            pressed = true;
        }

        void buttonReleased()
        {
            disableHovering();
            pressed = false;
            lastPointedAtom = null;
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

        /*
        [PunRPC]
        void ShowHoverAtomText(string atomText, Vector3 atomPos, Vector3 camPos)
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
        void DisablingHoverAtomText()
        {
            if (textHoverAtomGO.activeSelf == true)
            {
                textHoverAtomGO.SetActive(true);
                textHoverAtomGO.transform.position = new Vector3(0, -1000, 0);
            }
        }
        */
    }
}
