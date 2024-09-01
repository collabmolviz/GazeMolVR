using Photon.Pun;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(TMP_Dropdown))]
public class SyncDropdownState : MonoBehaviourPunCallbacks
{
    public GameObject dropdownList; // Assign the custom list here

    private TMP_Dropdown dropdown;
    private bool wasExpanded = false;

    private void Awake()
    {
        dropdown = GetComponent<TMP_Dropdown>();
        dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
    }

    void Update()
    {
        // Check if dropdown was previously expanded and is now collapsed
        if (wasExpanded && !dropdown.IsExpanded)
        {
            photonView.RPC("RPC_ToggleDropdownList", RpcTarget.Others, false);
        }

        wasExpanded = dropdown.IsExpanded;
    }

    public void OnDropdownClick()
    {
        photonView.RPC("RPC_ToggleDropdownList", RpcTarget.Others, true);
    }

    void OnDropdownValueChanged(int selectedValue)
    {
        photonView.RPC("RPC_SyncSelectedValue", RpcTarget.Others, selectedValue);
    }

    [PunRPC]
    void RPC_ToggleDropdownList(bool show)
    {
        dropdownList.SetActive(show);
    }

    [PunRPC]
    void RPC_SyncSelectedValue(int value)
    {
        dropdown.value = value;
    }
}
