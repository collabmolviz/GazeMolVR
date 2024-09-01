
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlaySoundAndDestroy : MonoBehaviour
{
    AudioSource audioSource;
    bool started = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        AudioSource.PlayClipAtPoint(audioSource.clip, transform.position);
        started = true;
    }

    void Update(){
        if(started){
            if(!audioSource.isPlaying){
                GameObject.Destroy(gameObject);
            }
        }
    }
}

