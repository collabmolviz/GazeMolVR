using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GetLookingAtProteinValue : MonoBehaviour
{
    public SRanipalEyeRaycaster sRanipalEyeRaycaster;
    public bool islookingAtProtein;

    void Update()
    {
        if (sRanipalEyeRaycaster != null)
        {
            islookingAtProtein = sRanipalEyeRaycaster.lookingAtProtein;
        }
    }
}
