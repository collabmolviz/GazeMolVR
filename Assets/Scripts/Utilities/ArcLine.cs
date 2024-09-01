using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent( typeof(LineRenderer) )]
public class ArcLine : MonoBehaviour {

    [Range(1, 50)]
    public int arcSize = 20;
    public float distanceFromB = 2.0f;

    public Vector3 A;
    public Vector3 B;
    public Vector3 C;



    public void UpdatePointLine()
    {

        LineRenderer line = GetComponent<LineRenderer>();

        //get smoothed values
        Vector3[] smoothedPoints = new Vector3[arcSize];

        Vector3 BA = Vector3.Normalize(A - B) * transform.lossyScale.x;
        Vector3 BC = Vector3.Normalize(C - B) * transform.lossyScale.x;

        Vector3 pt = B + BA * distanceFromB;

        Vector3 pivotVec = Vector3.Normalize(Vector3.Cross(BA, BC)) * transform.lossyScale.x;

        float totalAngle = Vector3.SignedAngle(BA, BC, pivotVec);
        float step = totalAngle / (float)arcSize;

        for (int i = 0; i < arcSize; i++) {
            pt = RotatePointAroundPivot(B, B + pivotVec, pt, step);
            smoothedPoints[i] = transform.InverseTransformPoint(pt);
        }

        //set line settings
        line.positionCount = smoothedPoints.Length;
        line.SetPositions( smoothedPoints );
    }


    public static Vector3 RotatePointAroundPivot(Vector3 vecOri, Vector3 vecEnd, Vector3 point, float degrees)
    {
        Vector3 rotationCenter = vecOri + Vector3.Project(point - vecOri, vecEnd - vecOri);
        Vector3 rotationAxis = (vecEnd - vecOri).normalized;
        Vector3 relativePosition = point - rotationCenter;

        Quaternion rotatedAngle = Quaternion.AngleAxis(degrees, rotationAxis);
        Vector3 rotatedPosition = rotatedAngle * relativePosition;

        // New object position
        return rotationCenter + rotatedPosition;
    }


}
