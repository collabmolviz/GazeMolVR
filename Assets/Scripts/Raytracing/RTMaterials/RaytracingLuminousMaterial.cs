using UnityEngine;


namespace UMol {
public class RaytracingLuminousMaterial : RaytracingMaterial{

    private Vector3 _color = Vector3.one;//	white color of the emitted light
    public Vector3 color {
        get { return _color; }
        set {
            propertyChanged = true;
            _color = value;
        }
    }
    private float _intensity = 1.0f;//	1	intensity of the light (a factor)
    public float intensity {
        get { return _intensity; }
        set {
            propertyChanged = true;
            _intensity = value;
        }
    }
    private float _transparency = 1.0f;// 1 material transparency
    public float transparency {
        get { return _transparency; }
        set {
            propertyChanged = true;
            _transparency = value;
        }
    }
}
}