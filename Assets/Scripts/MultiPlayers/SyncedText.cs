using TMPro;
using Photon.Pun;
using UnityEngine;

public class SyncedText : MonoBehaviourPun
{
    [SerializeField] private TextMeshProUGUI textMesh;
    private string lastText;

    private void Start()
    {
        lastText = textMesh.text;
    }

    private void Update()
    {
        if(textMesh.text != lastText)
        {
            lastText = textMesh.text;
            ChangeText(lastText);
        }
    }

    public void ChangeText(string newText)
    {
        photonView.RPC("UpdateText", RpcTarget.All, newText);
    }

    [PunRPC]
    void UpdateText(string newText)
    {
        textMesh.text = newText;
    }
}
