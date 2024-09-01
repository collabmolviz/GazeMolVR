using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class MultiplayerButtonPressColor : MonoBehaviourPunCallbacks
{
    public Button Btn;
    private Color pressColor = Color.grey; // This color simulates the press effect
    private Color originalColor;
    private bool isPressed = false;

    private void Start()
    {
        originalColor = Btn.image.color; // Store the original color of the button

        Btn.onClick.AddListener(() =>
        {
            // Simulate button press locally
            SimulateButtonPress();

            // Notify others about the button press
            photonView.RPC("RPC_SimulateButtonPress", RpcTarget.All);
        });
    }

    [PunRPC]
    void RPC_SimulateButtonPress()
    {
        SimulateButtonPress();
    }

    void SimulateButtonPress()
    {
        if (!isPressed)
        {
            isPressed = true;
            Btn.image.color = pressColor;
            Invoke("RevertButtonColor", 0.1f); // Revert color after 0.1 seconds
        }
    }

    void RevertButtonColor()
    {
        Btn.image.color = originalColor;
        isPressed = false;
    }
}
