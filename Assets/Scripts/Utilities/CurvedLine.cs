using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent( typeof(LineRenderer) )]
public class CurvedLine : MonoBehaviour
{
	public float lineSegmentSize = 0.005f;

	public Vector3[] linePositions = new Vector3[0];




	public void UpdatePointLine()
	{

		LineRenderer line = GetComponent<LineRenderer>();

		//get smoothed values
		Vector3[] smoothedPoints = LineSmoother.SmoothLine( linePositions, lineSegmentSize );

		//set line settings
		line.positionCount = smoothedPoints.Length;
		line.SetPositions( smoothedPoints );
	}


}
