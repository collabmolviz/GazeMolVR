using UnityEngine;


namespace UMol {
public class RaytracingMetallicPaintMaterial : RaytracingMaterial{

    private Vector3 _baseColor = new Vector3(0.8f, 0.8f, 0.8f);// white 0.8	color of base coat
    public Vector3 baseColor {
        get { return _baseColor; }
        set {
            propertyChanged = true;
            _baseColor = value;
        }
    }
    private float _flakeAmount = 0.3f;// 0.3	amount of flakes, in [0–1]
    public float flakeAmount {
        get { return _flakeAmount; }
        set {
            propertyChanged = true;
            _flakeAmount = value;
        }
    }
    private Vector3 _flakeColor;// Aluminium	color of metallic flakes
    public Vector3 flakeColor {
        get { return _flakeColor; }
        set {
            propertyChanged = true;
            _flakeColor = value;
        }
    }
    private float _flakeSpread = 0.5f;// 0.5	spread of flakes, in [0–1]
    public float flakeSpread {
        get { return _flakeSpread; }
        set {
            propertyChanged = true;
            _flakeSpread = value;
        }
    }
    private float _eta = 1.5f;// 1.5	index of refraction of clear coat
    public float eta {
        get { return _eta;}
        set {
            propertyChanged = true;
            _eta = value;
        }
    }
}
}