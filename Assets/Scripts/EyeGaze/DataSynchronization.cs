using Photon.Pun;
using UnityEngine;

public class DataSynchronization : MonoBehaviourPunCallbacks, IPunObservable
{
    public Vector3 latestEyePosition;
    public bool lookingAtProteinFlag;

    public Light pointLight;
    public Color pointLightColor;
    public float pointLightRadius;

    public Light spotLight;
    public Color spotLightColor;
    public float spotLightRange;
    public float spotLightIntensity;

    private GameObject eyePoint;

    void Start()
    {
        if (photonView.IsMine)
        {
            gameObject.tag = "LocalPlayer";

            eyePoint = GameObject.Find("eye-pointer");
            if (eyePoint == null)
            {
                Debug.LogError("eye-pointer not found!");
            }

            if (pointLight == null || spotLight == null)
            {
                Debug.LogError("No Light component found on the player!");
            }
            else
            {
                pointLightColor = pointLight.color;
                pointLightRadius = pointLight.range;

                spotLightColor = spotLight.color;
                spotLightRange = spotLight.range;
                spotLightIntensity = spotLight.intensity;
            }
        }
        else
        {
            gameObject.tag = "RemotePlayer";
        }
    }

    void Update()
    {
        if (photonView.IsMine && eyePoint != null)
        {
            latestEyePosition = eyePoint.transform.position;

            var getLookingScript = eyePoint.GetComponent<GetLookingAtProteinValue>();
            if (getLookingScript != null)
            {
                lookingAtProteinFlag = getLookingScript.islookingAtProtein;
            }
            if (pointLight != null && spotLight != null)
            {
                pointLightColor = pointLight.color;
                pointLightRadius = pointLight.range;

                spotLightColor = spotLight.color;
                spotLightRange = spotLight.range;
                spotLightIntensity = spotLight.intensity;
            }
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(latestEyePosition);
            stream.SendNext(lookingAtProteinFlag);

            stream.SendNext(pointLightColor.r);
            stream.SendNext(pointLightColor.g);
            stream.SendNext(pointLightColor.b);
            stream.SendNext(pointLightColor.a);
            stream.SendNext(pointLightRadius);

            stream.SendNext(spotLightColor.r);
            stream.SendNext(spotLightColor.g);
            stream.SendNext(spotLightColor.b);
            stream.SendNext(spotLightColor.a);
            stream.SendNext(spotLightRange);
            stream.SendNext(spotLightIntensity);
        }
        else
        {
            latestEyePosition = (Vector3)stream.ReceiveNext();
            lookingAtProteinFlag = (bool)stream.ReceiveNext();

            float pointLightR = (float)stream.ReceiveNext();
            float pointLightG = (float)stream.ReceiveNext();
            float pointLightB = (float)stream.ReceiveNext();
            float pointLightA = (float)stream.ReceiveNext();
            pointLightColor = new Color(pointLightR, pointLightG, pointLightB, pointLightA);
            pointLightRadius = (float)stream.ReceiveNext();

            float spotLightR = (float)stream.ReceiveNext();
            float spotLightG = (float)stream.ReceiveNext();
            float spotLightB = (float)stream.ReceiveNext();
            float spotLightA = (float)stream.ReceiveNext();
            spotLightColor = new Color(spotLightR, spotLightG, spotLightB, spotLightA);
            spotLightRange = (float)stream.ReceiveNext();
            spotLightIntensity = (float)stream.ReceiveNext();
        }
    }
}
