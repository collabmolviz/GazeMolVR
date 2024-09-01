using UnityEngine;


namespace UMol {
public class RaytracingMetalMaterial : RaytracingMaterial {
//Metal                 eta                   k
//Ag, Silver	(0.051, 0.043, 0.041)	(5.3, 3.6, 2.3)
// Al, Aluminium	(1.5, 0.98, 0.6)	(7.6, 6.6, 5.4)
// Au, Gold	(0.07, 0.37, 1.5)	(3.7, 2.3, 1.7)
// Cr, Chromium	(3.2, 3.1, 2.3)	(3.3, 3.3, 3.1)
// Cu, Copper	(0.1, 0.8, 1.1)	(3.5, 2.5, 2.4)
//https://refractiveindex.info/

    // private Vector3[] _ior;// Aluminium data array of spectral samples of complex refractive index, each entry in the form (wavelength, eta, k), ordered by wavelength (which is in nm)
    // public Vector3[] ior {
    //     get { return _ior;}
    //     set {
    //         propertyChanged = true;
    //         ior = value;
    //     }
    // }
    private Vector3 _eta = new Vector3(0.051f, 0.043f, 0.041f);//  RGB complex refractive index, real part
    public Vector3 eta {
        get { return _eta;}
        set {
            propertyChanged = true;
            _eta = value;
        }
    }
    private Vector3 _k = new Vector3(5.3f, 3.6f, 2.3f);//  RGB complex refractive index, imaginary part
    public Vector3 k {
        get { return _k;}
        set {
            propertyChanged = true;
            _k = value;
        }
    }
    private float _roughness = 0.1f;// 0.1 roughness in [0–1], 0 is perfect mirror
    public float roughness {
        get { return _roughness;}
        set {
            propertyChanged = true;
            _roughness = value;
        }
    }
}
}