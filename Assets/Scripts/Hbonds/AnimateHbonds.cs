
using UnityEngine;


public class AnimateHbonds : MonoBehaviour {

    public Material hbondMat;
    public float speedAnim = 0.05f;
    private float curTexOffset = 0.0f;
    private Vector2 newOffset = Vector2.zero;

    void Update(){
        if(hbondMat != null){
            curTexOffset -= speedAnim;
            if(curTexOffset == 0.0f){
                curTexOffset = 5.0f;
            }

            newOffset.x = curTexOffset;
            hbondMat.SetTextureOffset("_MainTex",newOffset);

        }
    }
}