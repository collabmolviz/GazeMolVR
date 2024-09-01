using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HTC.UnityPlugin.Vive;
using HTC.UnityPlugin.Utility;

[RequireComponent (typeof (Rigidbody))]
public class CollisionsVRDocking : MonoBehaviour {

    // public MoveChainsVRDocking refMoveCh;
    public ExplosionSpawner spawner;

    private int uilayer;
    private AudioClip audioClip;

    void OnEnable(){
        uilayer = LayerMask.NameToLayer("UI");
        audioClip = (AudioClip) Resources.Load("Sounds/bang1.wav");
    }

    void OnTriggerEnter(Collider col){
        if(col.gameObject.layer == uilayer)
            return;
        if(col.gameObject.name[0] != '<')
            return;
        // refMoveCh.isGameObjectColliding[gameObject] = true;


        //TODO: get the real position of the collision
        spawner.SpawnExplosion(col.transform, Vector3.zero);

        ViveInput.TriggerHapticPulse(HandRole.RightHand, 500);
        ViveInput.TriggerHapticPulse(HandRole.LeftHand, 500);

        if(audioClip == null){
            audioClip = (AudioClip) Resources.Load("Sounds/bang1.wav");
        }
        AudioSource.PlayClipAtPoint(audioClip, transform.position);
    }
    void OnCollisionEnter(Collision collision){
        // if(collision.collider.gameObject.layer == uilayer)
            // return;
        // if(collision.collider.gameObject.name[0] != '<')
            // return;

        spawner.SpawnExplosion(collision.collider.transform, collision.contacts[0].point);

        ViveInput.TriggerHapticPulse(HandRole.RightHand, 500);
        ViveInput.TriggerHapticPulse(HandRole.LeftHand, 500);

        if(audioClip == null){
            audioClip = Resources.Load("Sounds/bang1") as AudioClip;
        }

        AudioSource.PlayClipAtPoint(audioClip, transform.position, 0.1f);

    }
    // void OnTriggerStay(Collider col){
    //  refMoveCh.isGameObjectColliding[gameObject] = true;
    // }

    void OnDestroy(){
    }
}