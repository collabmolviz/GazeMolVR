using UnityEngine;


namespace UMol {
public class RaytracingAlloyMaterial : RaytracingMaterial{

    private Vector3 _color = new Vector3(0.9f, 0.9f, 0.9f);// white 0.9 reflectivity at normal incidence (0 degree)
    public Vector3 color {
        get { return _color;}
        set {
            propertyChanged = true;
            _color = value;
        }
    }

    private Vector3 _edgeColor = Vector3.one;// white reflectivity at grazing angle (90 degree)
    public Vector3 edgeColor {
        get { return _edgeColor;}
        set {
            propertyChanged = true;
            _edgeColor = value;
        }
    }
    
    private float _roughness = 0.1f;// 0.1	roughness, in [0–1], 0 is perfect mirror
    public float roughness {
        get { return _roughness;}
        set {
            propertyChanged = true;
            _roughness = value;
        }
    }

}
}