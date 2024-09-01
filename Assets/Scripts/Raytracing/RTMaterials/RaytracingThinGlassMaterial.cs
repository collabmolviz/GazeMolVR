using UnityEngine;


namespace UMol {
public class RaytracingThinGlassMaterial : RaytracingMaterial{
    private float _eta = 1.5f;//	1.5	index of refraction
    public float eta {
        get { return _eta; }
        set {
            propertyChanged = true;
            _eta = value;
        }
    }
    private Vector3 _attenuationColor = Vector3.one;//	white	resulting color due to attenuation
    public Vector3 attenuationColor {
        get { return _attenuationColor; }
        set {
            propertyChanged = true;
            _attenuationColor = value;
        }
    }
    private float _attenuationDistance = 1.0f;//	1	distance affecting attenuation
    public float attenuationDistance {
        get { return _attenuationDistance; }
        set {
            propertyChanged = true;
            _attenuationDistance = value;
        }
    }
    private float _thickness = 1.0f;//	1	virtual thickness
    public float thickness {
        get { return _thickness; }
        set {
            propertyChanged = true;
            _thickness = value;
        }
    }
}
}