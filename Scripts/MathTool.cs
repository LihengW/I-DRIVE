#region HeadComments
// ********************************************************************
//  Copyright (C) #YEAR# #COMPANYNAME# #PROJECTNAME#
//  作    者：#AUTHOR#
//  文件路径：#FILEPATH#
//  创建日期：#CREATIONDATE#
//  功能描述：
// *********************************************************************
#endregion

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MathTool : MonoBehaviour
{
    public static (Vector3 intersectionPoint1, Vector3 intersectionPoint2, float distance) FindNearestDistanceAndPoints(
        Vector3 P1, Vector3 d1, 
        Vector3 P2, Vector3 d2  
    )
    {
        Vector3 N = Vector3.Cross(d1, d2);
        if (N.sqrMagnitude == 0)
        {
            Debug.Log("The lines are parallel or coincident");
            return (Vector3.zero, Vector3.zero, -1f);  // No intersection
        }
        Vector3 V = P2 - P1;

        float distance = Mathf.Abs(Vector3.Dot(V, N)) / N.magnitude;

        float t = Vector3.Dot(V, Vector3.Cross(d2, N)) / Vector3.Dot(d1, Vector3.Cross(d2, N));
        float s = Vector3.Dot(V, Vector3.Cross(d1, N)) / Vector3.Dot(d2, Vector3.Cross(d1, N));

        Vector3 intersectionPoint1 = P1 + t * d1;
        Vector3 intersectionPoint2 = P2 + s * d2;

        return (intersectionPoint1, intersectionPoint2, distance);
    }
}

public class BezierAABB
{
    public Vector2 min;
    public Vector2 max;

    public BezierAABB(Vector2 min, Vector2 max)
    {
        this.min = min;
        this.max = max;
    }
    static public BezierAABB GetAABB(BezierCurve2D bezierCurve)
    {
        float[] tSamples = new float[] { 0f, 0.25f, 0.5f, 0.75f, 1f };

        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;

        foreach (float t in tSamples)
        {
            Vector2 p = bezierCurve.GetPos(t);
            minX = Mathf.Min(minX, p.x);
            maxX = Mathf.Max(maxX, p.x);
            minY = Mathf.Min(minY, p.y);
            maxY = Mathf.Max(maxY, p.y);
        }

        return new BezierAABB(new Vector2(minX, minY), new Vector2(maxX, maxY));
    }

    public static bool Compare(BezierAABB a, BezierAABB b)
    {
        if (a.min.x > b.max.x || a.min.y > b.max.y)
        {
            return false;
        }
        else if (b.min.x > a.max.x || b.min.y > a.max.y)
        {
            return false;
        }
        else
        {
            return true;
        }
    }
}

