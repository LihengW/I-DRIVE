#region HeadComments
// ********************************************************************
//  Copyright (C) #YEAR# #COMPANYNAME# #PROJECTNAME#
//  作    者：#AUTHOR#
//  文件路径：#FILEPATH#
//  创建日期：#CREATIONDATE#
//  功能描述：
// *********************************************************************
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.Searcher.SearcherWindow.Alignment;
using static UnityEngine.GraphicsBuffer;

public struct BezierCurve3D
{
    [SerializeField]
    public Vector3 controlPoint1;

    [SerializeField]
    public Vector3 controlPoint2;

    [SerializeField]
    public Vector3 controlPoint3;

    [SerializeField]
    public Vector3 controlPoint4;

    [HideInInspector] public bool hasSample;

    [HideInInspector] public List<Vector3> samplePoints;

    public Vector3 GetControlPoint(int i)
    {
        if (i == 0) return controlPoint1;
        if (i == 1) return controlPoint2;
        if (i == 2) return controlPoint3;
        if (i == 3) return controlPoint4;

        return Vector3.zero;
    }

    public void SetControlPoint(int i, Vector3 p)
    {
        if (i == 0) controlPoint1 = p;
        if (i == 1) controlPoint2 = p;
        if (i == 2) controlPoint3 = p;
        if (i == 3) controlPoint4 = p;
    }

    public BezierCurve3D BendZ(Vector3 updirection, RoadZType roadZType, float height)
    {
        BezierCurve3D curve = new BezierCurve3D();

        if (roadZType == RoadZType.Flat)
        {
            return this;
        }
        else if (roadZType == RoadZType.Rising)
        {
            curve.controlPoint2 = controlPoint2 + updirection.normalized * UnityEngine.Random.Range(0.25f, 0.5f) * height;
            curve.controlPoint3 = controlPoint3 + updirection.normalized * UnityEngine.Random.Range(0.5f, 0.75f) * height;
            curve.controlPoint4 = controlPoint4 + updirection.normalized * UnityEngine.Random.Range(0.75f, 1.0f) * height;
        }
        else if (roadZType == RoadZType.Descending)
        {
            curve.controlPoint2 = controlPoint2 - updirection.normalized * UnityEngine.Random.Range(0.25f, 0.5f) * height;
            curve.controlPoint3 = controlPoint3 - updirection.normalized * UnityEngine.Random.Range(0.5f, 0.75f) * height;
            curve.controlPoint4 = controlPoint4 - updirection.normalized * UnityEngine.Random.Range(0.75f, 1.0f) * height;
        }
        else if (roadZType == RoadZType.Waving)
        {
            curve.controlPoint2 = controlPoint2 + updirection.normalized * UnityEngine.Random.Range(-0.5f, 0.5f) * height;
            curve.controlPoint3 = controlPoint3 + updirection.normalized * UnityEngine.Random.Range(-0.5f, 0.5f) * height;
            curve.controlPoint4 = controlPoint4 + updirection.normalized * UnityEngine.Random.Range(-0.5f, 0.5f) * height;
        }

        curve.controlPoint1 = this.controlPoint1;

        return curve;
    }


    public Vector3 GetPos(float t)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        Vector3 p = uu * u * controlPoint1; 
        p += 3 * uu * t * controlPoint2;
        p += 3 * u * tt * controlPoint3;
        p += tt * t * controlPoint4;
        return p;
    }

    public void Offset(Vector3 offset)
    {
        controlPoint1 += offset;
        controlPoint2 += offset;
        controlPoint3 += offset;
        controlPoint4 += offset;
    }

    public Vector3 GetTangent(float t)
    {
        return (3 * (1-t) * (1-t) * (controlPoint2 - controlPoint1) + 6 * (1-t)* t * (controlPoint3 - controlPoint2) + 3 * t * t * (controlPoint4 - controlPoint3)).normalized;
    }

    public Vector3 GetNormal(float t)
    {
        return Vector3.Cross(GetTangent(t), Vector3.up);
    }

    public float GetCurvature(float t)
    {
        Vector3 controlPoint2Prime = 3 * Mathf.Pow(1 - t, 2) * (controlPoint2 - controlPoint1) + 6 * (1 - t) * t * (controlPoint3 - controlPoint2) + 3 * Mathf.Pow(t, 2) * (controlPoint4 - controlPoint3);

        Vector3 controlPoint3Prime = 6 * (1 - t) * (controlPoint3 - 2 * controlPoint2 + controlPoint1) + 6 * t * (controlPoint4 - 2 * controlPoint3 + controlPoint2);

        Vector3 crossProduct = Vector3.Cross(controlPoint2Prime, controlPoint3Prime);

        float numerator = crossProduct.magnitude;
        float denominator = Mathf.Pow(controlPoint2Prime.magnitude, 3);

        return numerator / denominator;
    }

    public static BezierCurve3D Lerp(BezierCurve3D Curve1, BezierCurve3D Curve2, float t)
    {
        BezierCurve3D res_curve = new BezierCurve3D();
        res_curve.controlPoint1 = Vector3.Lerp(Curve1.controlPoint1, Curve2.controlPoint1, t);
        res_curve.controlPoint2 = Vector3.Lerp(Curve1.controlPoint2, Curve2.controlPoint2, t);
        res_curve.controlPoint3 = Vector3.Lerp(Curve1.controlPoint3, Curve2.controlPoint3, t);
        res_curve.controlPoint4 = Vector3.Lerp(Curve1.controlPoint4, Curve2.controlPoint4, t);

        return res_curve;
    }



    public BezierCurve2D ProjectToXY()
    {
        var projection = new BezierCurve2D();
        projection.controlPoint1 = new Vector2(this.controlPoint1.x, this.controlPoint1.y);
        projection.controlPoint2 = new Vector2(this.controlPoint2.x, this.controlPoint2.y);
        projection.controlPoint3 = new Vector2(this.controlPoint3.x, this.controlPoint3.y);
        projection.controlPoint4 = new Vector2(this.controlPoint4.x, this.controlPoint4.y);
        return projection;
    }

    public void StartFromOrign()
    {
        controlPoint2 = controlPoint2 - controlPoint1;
        controlPoint3 = controlPoint3 - controlPoint1;
        controlPoint4 = controlPoint4 - controlPoint1;
        controlPoint1 = Vector3.zero;
    }

    public void DrawBezier(int resolution, Transform parent_transform, Color color = default)
    {
        if (color == default) color = Color.green;  // Default to green if no color is provided

        Vector3 previousPoint = parent_transform.TransformPoint(GetPos(0));

        // Use Debug.DrawLine for Scene view (non-runtime drawing)
        for (int i = 1; i <= resolution; i++)
        {
            float t = i / (float)resolution;
            Vector3 currentPoint = parent_transform.TransformPoint(GetPos(t));

            Debug.DrawLine(previousPoint, currentPoint, color);  // Draw line between consecutive points

            previousPoint = currentPoint;
        }
    }

    public void DrawWithLineRenderer(GameObject targetObject, int resolution, Color color = default)
    {
        LineRenderer lineRenderer = targetObject.GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = targetObject.AddComponent<LineRenderer>();
        }

        lineRenderer.positionCount = resolution + 1;

        for (int i = 0; i <= resolution; i++)
        {
            float t = i / (float)resolution;
            lineRenderer.SetPosition(i, GetPos(t));  
        }

        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = color == default ? Color.green : color;
        lineRenderer.endColor = color == default ? Color.green : color;
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;
    }

    public static BezierCurve3D GenerateRandomCurve(Vector3 frompos, float length, Vector3 maindirection, Vector3 updirection, float width_limit = 10.0f)
    {
        var curve = new BezierCurve3D();
        Vector3 right = Vector3.Cross(updirection, maindirection).normalized;
        curve.controlPoint1 = frompos;
        curve.controlPoint2 = frompos + maindirection.normalized * length / 3.0f * 1 + right * UnityEngine.Random.Range(- width_limit / 2.0f, width_limit / 2.0f);
        curve.controlPoint3 = frompos + maindirection.normalized * length / 3.0f * 2 + right * UnityEngine.Random.Range(-width_limit / 2.0f, width_limit / 2.0f);
        curve.controlPoint4 = frompos + maindirection.normalized * length / 3.0f * 3 + right * UnityEngine.Random.Range(-width_limit / 2.0f, width_limit / 2.0f);

        return curve;
    }

    public static BezierCurve3D GenerateLeftTurn(Vector3 frompos, float length, Vector3 maindirection, Vector3 updirection, float width_limit = 10.0f)
    {
        var curve = new BezierCurve3D();
        Vector3 right = Vector3.Cross(updirection, maindirection).normalized;

        float sidelen = length / Mathf.Sqrt(2);

        curve.controlPoint1 = frompos;
        curve.controlPoint2 = frompos + maindirection.normalized * sidelen / 2.0f * 1 + right * UnityEngine.Random.Range(-width_limit / 2.0f, width_limit / 2.0f);
        curve.controlPoint3 = frompos + maindirection.normalized * sidelen - right * sidelen / 2.0f * 1 + (maindirection + right).normalized * UnityEngine.Random.Range(-width_limit / 2.0f, width_limit / 2.0f);
        curve.controlPoint4 = frompos + maindirection.normalized * sidelen - right * sidelen + maindirection * UnityEngine.Random.Range(-width_limit / 2.0f, width_limit / 2.0f);

        return curve;
    }

    public static BezierCurve3D GenerateSharpLeftTurn(Vector3 frompos, float length, Vector3 maindirection, Vector3 updirection, float width_limit = 10.0f)
    {
        var curve = new BezierCurve3D();
        Vector3 right = Vector3.Cross(updirection, maindirection).normalized;

        float sidelen = length / 3;

        curve.controlPoint1 = frompos;
        curve.controlPoint2 = frompos + maindirection.normalized * sidelen + right * UnityEngine.Random.Range(-width_limit / 2.0f, width_limit / 2.0f);
        curve.controlPoint3 = frompos + maindirection.normalized * sidelen / 2 - right * sidelen / 2.0f * Mathf.Sqrt(3) + (maindirection + right).normalized * UnityEngine.Random.Range(-width_limit / 2.0f, width_limit / 2.0f);
        curve.controlPoint4 = frompos - right * sidelen * Mathf.Sqrt(3) + maindirection * UnityEngine.Random.Range(-width_limit / 2.0f, width_limit / 2.0f);

        return curve;
    }

    public static BezierCurve3D GenerateRightTurn(Vector3 frompos, float length, Vector3 maindirection, Vector3 updirection, float width_limit = 10.0f)
    {
        var curve = new BezierCurve3D();
        Vector3 right = Vector3.Cross(updirection, maindirection).normalized;

        float sidelen = length / Mathf.Sqrt(2);

        curve.controlPoint1 = frompos;
        curve.controlPoint2 = frompos + maindirection.normalized * sidelen / 2.0f * 1 - right * UnityEngine.Random.Range(-width_limit / 2.0f, width_limit / 2.0f);
        curve.controlPoint3 = frompos + maindirection.normalized * sidelen + right * sidelen / 2.0f * 1 + (maindirection - right).normalized * UnityEngine.Random.Range(-width_limit / 2.0f, width_limit / 2.0f);
        curve.controlPoint4 = frompos + maindirection.normalized * sidelen + right * sidelen + maindirection * UnityEngine.Random.Range(-width_limit / 2.0f, width_limit / 2.0f);

        return curve;
    }

    public static BezierCurve3D GenerateSharpRightTurn(Vector3 frompos, float length, Vector3 maindirection, Vector3 updirection, float width_limit = 10.0f)
    {
        var curve = new BezierCurve3D();
        Vector3 right = Vector3.Cross(updirection, maindirection).normalized;

        float sidelen = length / 3;

        curve.controlPoint1 = frompos;
        curve.controlPoint2 = frompos + maindirection.normalized * sidelen - right * UnityEngine.Random.Range(-width_limit / 2.0f, width_limit / 2.0f);
        curve.controlPoint3 = frompos + maindirection.normalized * sidelen / 2 + right * sidelen / 2.0f * Mathf.Sqrt(3) + (maindirection - right).normalized * UnityEngine.Random.Range(-width_limit / 2.0f, width_limit / 2.0f);
        curve.controlPoint4 = frompos + right * sidelen * Mathf.Sqrt(3) + maindirection * UnityEngine.Random.Range(-width_limit / 2.0f, width_limit / 2.0f);

        return curve;
    }

    public static BezierCurve3D CreateTransition(float length, BezierCurve3D cur, BezierCurve3D next)
    {
        BezierCurve3D trans_spine = new BezierCurve3D();
        trans_spine.controlPoint1 = cur.controlPoint4;
        trans_spine.controlPoint2 = trans_spine.controlPoint1 + length / 3.0f * cur.GetTangent(1.0f);
        trans_spine.controlPoint3 = trans_spine.controlPoint2 + length / 3.0f * Vector3.Lerp(cur.GetTangent(1.0f), next.GetTangent(0.0f), 0.5f);
        trans_spine.controlPoint4 = trans_spine.controlPoint3 + length / 3.0f * next.GetTangent(0.0f);

        return trans_spine;
    }

    public (Vector3, float) GetNearestPoint(Vector3 point)
    {
        if (!hasSample)
        {
            CreateSample();
        }

        int min_idx = 0;
        Vector3 min_pos = Vector3.down;
        float min_dist = float.MaxValue;


        for (int i = 0; i < samplePoints.Count; i++)
        {
            var sample = samplePoints[i];
            if ((point - sample).magnitude < min_dist)
            {
                min_dist = (point - sample).magnitude;
                min_pos = sample;
                min_idx = i;
            }
        }

        return (min_pos, (float)min_idx / (float)(samplePoints.Count - 1));
    }

    public void CreateSample(int num = 20)
    {
        samplePoints = new List<Vector3>();
        for (int i = 0; i < num; i++)
        {
            samplePoints.Add(GetPos((float)i / (num - 1)));
        }
        hasSample = true;
    }

}

public struct BezierCurve2D
{
    [SerializeField] public Vector2 controlPoint1;
    [SerializeField] public Vector2 controlPoint2;
    [SerializeField] public Vector2 controlPoint3;
    [SerializeField] public Vector2 controlPoint4;

    public Vector2 GetPos(float t)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        Vector2 p = uu * u * controlPoint1;
        p += 3 * uu * t * controlPoint2;
        p += 3 * u * tt * controlPoint3;
        p += tt * t * controlPoint4;
        return p;
    }



    public void PrintInfo()
    {
        UnityEngine.Debug.Log($"ControlPoint1 : ({controlPoint1.x} , {controlPoint1.y})");
        UnityEngine.Debug.Log($"ControlPoint2 : ({controlPoint2.x} , {controlPoint2.y})");
        UnityEngine.Debug.Log($"ControlPoint3 : ({controlPoint3.x} , {controlPoint3.y})");
        UnityEngine.Debug.Log($"ControlPoint4 : ({controlPoint4.x} , {controlPoint4.y})");
    }

    public static BezierCurve2D Lerp(BezierCurve2D Curve1, BezierCurve2D Curve2, float t)
    {
        BezierCurve2D res_curve = new BezierCurve2D();
        res_curve.controlPoint1 = Vector2.Lerp(Curve1.controlPoint1, Curve2.controlPoint1, t);
        res_curve.controlPoint2 = Vector2.Lerp(Curve1.controlPoint2, Curve2.controlPoint2, t);
        res_curve.controlPoint3 = Vector2.Lerp(Curve1.controlPoint3, Curve2.controlPoint3, t);
        res_curve.controlPoint4 = Vector2.Lerp(Curve1.controlPoint4, Curve2.controlPoint4, t);

        return res_curve;
    }

    public static BezierCurve2D CreateRoadShapeCurve(Vector2 widthRange, Vector2 HeightRange)
    {
        float[] xsamples = new float[4];
        
        for (int i = 0; i < 4; i++)
        {
            xsamples[i] = UnityEngine.Random.Range(widthRange.x, widthRange.y);
        }


        Array.Sort(xsamples);
        
        xsamples[0] = widthRange.x + UnityEngine.Random.Range(-0.5f, 0f);
        xsamples[3] = widthRange.y + UnityEngine.Random.Range(0f, 0.5f);

        BezierCurve2D curve = new BezierCurve2D();

        curve.controlPoint1 = new Vector2(xsamples[0], UnityEngine.Random.Range(HeightRange.x, HeightRange.y));
        curve.controlPoint2 = new Vector2(xsamples[1], UnityEngine.Random.Range(HeightRange.x, HeightRange.y));
        curve.controlPoint3 = new Vector2(xsamples[2], UnityEngine.Random.Range(HeightRange.x, HeightRange.y));
        curve.controlPoint4 = new Vector2(xsamples[3], UnityEngine.Random.Range(HeightRange.x, HeightRange.y));

        return curve;
    }

    public static BezierCurve2D CreateLinearShapeCurve(Vector2 widthRange, Vector2 HeightRange, bool ascending)
    {
        float[] xsamples = new float[4];

        for (int i = 0; i < 4; i++)
        {
            xsamples[i] = UnityEngine.Random.Range(widthRange.x, widthRange.y);
        }


        Array.Sort(xsamples);

        xsamples[0] = widthRange.x + UnityEngine.Random.Range(-0.5f, 0f);
        xsamples[3] = widthRange.y + UnityEngine.Random.Range(0f, 0.5f);

        BezierCurve2D curve = new BezierCurve2D();

        if (ascending)
        {
            curve.controlPoint1 = new Vector2(xsamples[0], HeightRange.x + UnityEngine.Random.Range(-0.5f, 0.5f));
            curve.controlPoint2 = new Vector2(xsamples[1], HeightRange.x + (HeightRange.y - HeightRange.x) * Mathf.Sqrt((xsamples[1] - xsamples[0]) / (xsamples[3] - xsamples[0])) + UnityEngine.Random.Range(-0.5f, 0.5f));
            curve.controlPoint3 = new Vector2(xsamples[2], HeightRange.x + (HeightRange.y - HeightRange.x) * Mathf.Sqrt((xsamples[2] - xsamples[0]) / (xsamples[3] - xsamples[0])) + UnityEngine.Random.Range(-0.5f, 0.5f));
            curve.controlPoint4 = new Vector2(xsamples[3], HeightRange.x + (HeightRange.y - HeightRange.x) * Mathf.Sqrt((xsamples[3] - xsamples[0]) / (xsamples[3] - xsamples[0])) + UnityEngine.Random.Range(-0.5f, 0.5f));
        }
        else
        {
            curve.controlPoint1 = new Vector2(xsamples[0], HeightRange.y + UnityEngine.Random.Range(-0.5f, 0.5f));
            curve.controlPoint2 = new Vector2(xsamples[1], HeightRange.y + (HeightRange.x - HeightRange.y) * Mathf.Sqrt((xsamples[1] - xsamples[0]) / (xsamples[3] - xsamples[0])) + UnityEngine.Random.Range(-0.5f, 0.5f));
            curve.controlPoint3 = new Vector2(xsamples[2], HeightRange.y + (HeightRange.x - HeightRange.y) * Mathf.Sqrt((xsamples[2] - xsamples[0]) / (xsamples[3] - xsamples[0])) + UnityEngine.Random.Range(-0.5f, 0.5f));
            curve.controlPoint4 = new Vector2(xsamples[3], HeightRange.y + (HeightRange.x - HeightRange.y) * Mathf.Sqrt((xsamples[3] - xsamples[0]) / (xsamples[3] - xsamples[0])) + UnityEngine.Random.Range(-0.5f, 0.5f));

        }

        return curve;
    }

    public Vector2 GetTangent(float t)
    {
        return 3 * (1 - t) * (1 - t) * (controlPoint2 - controlPoint1) + 6 * (1 - t) * t * (controlPoint3 - controlPoint2) + 3 * t * t * (controlPoint4 - controlPoint3);
    }

    public void StartFromOrign()
    {
        controlPoint2 = controlPoint2 - controlPoint1;
        controlPoint3 = controlPoint3 - controlPoint1;
        controlPoint4 = controlPoint4 - controlPoint1;
        controlPoint1 = Vector2.zero;
    }

    public void CenterAtOrign()
    {
        Vector2 pivot = GetPos(0.5f);
        controlPoint1 = controlPoint1 - pivot;
        controlPoint2 = controlPoint2 - pivot;
        controlPoint3 = controlPoint3 - pivot;
        controlPoint4 = controlPoint4 - pivot;
    }



}

public enum RoadBlockType
{
    Forward, LeftTurn, SharpLeftTurn, RightTurn, SharpRightTurn
}

// how the Z axis varies
public enum RoadZType
{
    Flat, Rising, Descending, Waving
}


public class BezierBlock : MonoBehaviour
{
    [SerializeField] public BezierCurve3D CentralLine;

    [SerializeField] public BezierCurve2D[] VerticleCurves;

    [SerializeField] public float thickness = 4.0f;

    [SerializeField] Color boneColor;

    [SerializeField] public RoadBlockType roadBlockType = RoadBlockType.Forward;

    public void GenerateBlockMesh(int numSamplesX, int numSamplesY)
    {
        // numSamples will be decided by the the actual needs of the geometry ? 
        // Y marches forward and X goes sides

        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> bottom_vertices = new List<Vector3>();
        List<Vector2> uv = new List<Vector2>();

        List<int> triangles = new List<int>();

        CentralLine.StartFromOrign();

        for (int y = 0; y < numSamplesY; y++)
        {
            float t_y = y / (float)(numSamplesY - 1);
            
            Vector3 center = CentralLine.GetPos(t_y);
            Vector3 localLeft = Vector3.Cross(CentralLine.GetTangent(t_y), Vector3.up).normalized;
            var localRight = -localLeft;
            Vector3 localUp = Vector3.Cross(localLeft, CentralLine.GetTangent(t_y)).normalized;

            BezierCurve2D verticle_curve;
            if (t_y <= 0.5f)
            {
                verticle_curve = BezierCurve2D.Lerp(VerticleCurves[0], VerticleCurves[1], t_y * 2.0f);
            }
            else
            {
                verticle_curve = BezierCurve2D.Lerp(VerticleCurves[1], VerticleCurves[2], (t_y - 0.5f) * 2.0f);
            }


            BezierCurve3D verticle_curve_3d = new BezierCurve3D();
            verticle_curve_3d.controlPoint1 = center + localRight * verticle_curve.controlPoint1.x + localUp * verticle_curve.controlPoint1.y;
            verticle_curve_3d.controlPoint2 = center + localRight * verticle_curve.controlPoint2.x + localUp * verticle_curve.controlPoint2.y;
            verticle_curve_3d.controlPoint3 = center + localRight * verticle_curve.controlPoint3.x + localUp * verticle_curve.controlPoint3.y;
            verticle_curve_3d.controlPoint4 = center + localRight * verticle_curve.controlPoint4.x + localUp * verticle_curve.controlPoint4.y;


            for (int x = 0; x < numSamplesX; x++)
            {
                float t_x = x / (float)(numSamplesX - 1);
                vertices.Add(verticle_curve_3d.GetPos(t_x) + thickness * localUp);
                uv.Add(new Vector2(t_x, t_y));
            }

            // bottom
            bottom_vertices.Add(vertices[vertices.Count - numSamplesX] - thickness * localUp);
            bottom_vertices.Add(vertices[vertices.Count - 1] - thickness * localUp);
        }

        foreach (var vertice in bottom_vertices)
        {
            vertices.Add(vertice);
            uv.Add(new Vector2(1.0f, 1.0f));
        }

        // build surface
        for (int y = 0; y < numSamplesY - 1; y++)
        {
            for (int x = 0; x < numSamplesX - 1; x++)
            {
                int i0 = y * numSamplesX + x;              
                int i1 = y * numSamplesX + (x + 1);        
                int i2 = (y + 1) * numSamplesX + x;        
                int i3 = (y + 1) * numSamplesX + (x + 1);  

                // First triangle (bottom-left, bottom-right, top-left)
                triangles.Add(i0);
                triangles.Add(i2);
                triangles.Add(i1);

                // Second triangle (bottom-right, top-right, top-left)
                triangles.Add(i1);
                triangles.Add(i2);
                triangles.Add(i3);
            }
        }

        int bottomIndex_Start = vertices.Count - bottom_vertices.Count;
        for (int y = 1; y < numSamplesY; y++)
        {
            triangles.Add(bottomIndex_Start + (y - 1) * 2);
            triangles.Add(bottomIndex_Start + y * 2);
            triangles.Add(bottomIndex_Start + (y - 1) * 2 + 1);
        
            triangles.Add(bottomIndex_Start + (y - 1) * 2 + 1);
            triangles.Add(bottomIndex_Start + y * 2);
            triangles.Add(bottomIndex_Start + y * 2 + 1);
        }

        for (int y = 0; y < numSamplesY - 1; y++)
        {
            int i0 = y * numSamplesX; // Top-left vertex
            int i1 = bottomIndex_Start + (y + 1) * 2; // Bottom-left vertex
            int i2 = bottomIndex_Start + y * 2; // Next bottom-left vertex

            int i3 = y * numSamplesX; // Top-left vertex
            int i4 = (y + 1) * numSamplesX; // Next top-left vertex
            int i5 = bottomIndex_Start + (y + 1) * 2; // Next bottom-left vertex

            triangles.Add(i0);
            triangles.Add(i1);
            triangles.Add(i2);

            triangles.Add(i0);
            triangles.Add(i4);
            triangles.Add(i1);

            // Right side triangles
            int i6 = bottomIndex_Start + y * 2 + 1; // Bottom-right vertex
            int i7 = bottomIndex_Start + (y + 1) * 2 + 1; // Next bottom-right vertex
            int i8 = (y + 1) * numSamplesX - 1; // Next top-right vertex

            int i9 = bottomIndex_Start + (y + 1) * 2 + 1; // Next bottom-right vertex
            int i10 = (y + 2) * numSamplesX - 1; // Next top-right vertex
            int i11 = (y + 1) * numSamplesX - 1; // Current top-right vertex

            // Right side triangles
            triangles.Add(i6);
            triangles.Add(i7);
            triangles.Add(i8);

            triangles.Add(i7);
            triangles.Add(i10);
            triangles.Add(i8);
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uv.ToArray();
        mesh.RecalculateNormals(); 

        GetComponent<MeshFilter>().mesh = mesh;

        GetComponent<MeshCollider>().sharedMesh = mesh; 
        GetComponent<MeshCollider>().convex = false;
    }

    public virtual void GenerateFrom(Vector3 startpoint, Vector3 tangent, BezierCurve2D startVerticleCurve)
    {
        // UnityEngine.Debug.Log(tangent);
        if (roadBlockType == RoadBlockType.Forward)
        {
            this.CentralLine = new BezierCurve3D();
            this.CentralLine.controlPoint1 = Vector3.zero;
            this.CentralLine.controlPoint2 = tangent / 3 + Vector3.zero;
            this.CentralLine.controlPoint3 = new Vector3(UnityEngine.Random.Range(-5f, 5f), UnityEngine.Random.Range(0f, 5f), UnityEngine.Random.Range(50f, 70f)) + Vector3.zero;
            this.CentralLine.controlPoint4 = new Vector3(UnityEngine.Random.Range(-5f, 5f), UnityEngine.Random.Range(0f, 5f), UnityEngine.Random.Range(80f, 120f)) + Vector3.zero;
        }
        else if (roadBlockType == RoadBlockType.LeftTurn || roadBlockType == RoadBlockType.SharpLeftTurn)
        {
            this.CentralLine = new BezierCurve3D();
            this.CentralLine.controlPoint1 = Vector3.zero;
            this.CentralLine.controlPoint2 = tangent / 3 * 2.0f + Vector3.zero;
            Vector3 local_left = Vector3.Cross(tangent.normalized, Vector3.up).normalized;
            this.CentralLine.controlPoint3 = this.CentralLine.controlPoint2 + local_left * UnityEngine.Random.Range(40, 60f) + tangent.normalized * UnityEngine.Random.Range(0.0f, 20.0f);
            this.CentralLine.controlPoint4 = this.CentralLine.controlPoint2 + local_left * UnityEngine.Random.Range(70, 90f) + tangent.normalized * UnityEngine.Random.Range(0.0f, 20.0f);
        }
        else if (roadBlockType == RoadBlockType.RightTurn || roadBlockType == RoadBlockType.SharpRightTurn)
        {
            this.CentralLine = new BezierCurve3D();
            this.CentralLine.controlPoint1 = Vector3.zero;
            this.CentralLine.controlPoint2 = tangent / 3 * 2.0f + Vector3.zero;
            Vector3 local_left = Vector3.Cross(tangent.normalized, Vector3.up).normalized;
            this.CentralLine.controlPoint3 = this.CentralLine.controlPoint2 - local_left * UnityEngine.Random.Range(40, 60f) + tangent.normalized * UnityEngine.Random.Range(0.0f, 20.0f);
            this.CentralLine.controlPoint4 = this.CentralLine.controlPoint2 - local_left * UnityEngine.Random.Range(70, 90f) + tangent.normalized * UnityEngine.Random.Range(0.0f, 20.0f);
        }

        this.VerticleCurves = new BezierCurve2D[3];
        this.VerticleCurves[0] = startVerticleCurve;

        if (roadBlockType == RoadBlockType.Forward)
        {
            for (int i = 1; i < 3; i++)
            {
                Vector2 widthRange = new Vector2(-20.0f, 20.0f);
                Vector2 heightRange = new Vector2(0.0f, 10.0f);
                this.VerticleCurves[i] = BezierCurve2D.CreateRoadShapeCurve(widthRange, heightRange);
            }
        }
        else if (roadBlockType == RoadBlockType.LeftTurn || roadBlockType == RoadBlockType.SharpLeftTurn)
        {
            Vector2 heightRange = new Vector2(0.0f, 10.0f);
            this.VerticleCurves[1] = BezierCurve2D.CreateLinearShapeCurve(new Vector2(-10.0f, 40.0f), heightRange, false);
            this.VerticleCurves[2] = BezierCurve2D.CreateRoadShapeCurve(new Vector2(-20.0f, 20.0f), heightRange);
        }
        else if (roadBlockType == RoadBlockType.RightTurn || roadBlockType == RoadBlockType.SharpRightTurn)
        {
            Vector2 heightRange = new Vector2(0.0f, 10.0f);
            this.VerticleCurves[1] = BezierCurve2D.CreateLinearShapeCurve(new Vector2(-40.0f, 10.0f), heightRange, true);
            this.VerticleCurves[2] = BezierCurve2D.CreateRoadShapeCurve(new Vector2(-20.0f, 20.0f), heightRange);
        }

        this.thickness = 3.0f;
    }

    private void Start()
    {
        boneColor = UnityEngine.Random.ColorHSV();
    }

    private void Update()
    {
        DrawBones(boneColor);
    }

    public void DrawBones(Color boneColor)
    {

        CentralLine.DrawBezier(100, this.transform, boneColor);

        for (int y = 0; y < 10; y++)
        {
            float t_y = y / (float)(10 - 1);

            Vector3 center = CentralLine.GetPos(t_y);
            Vector3 localLeft = Vector3.Cross(CentralLine.GetTangent(t_y), Vector3.up).normalized;
            var localRight = -localLeft;
            Vector3 localUp = Vector3.Cross(localLeft, CentralLine.GetTangent(t_y)).normalized;

            BezierCurve2D verticle_curve;
            if (t_y <= 0.5f)
            {
                verticle_curve = BezierCurve2D.Lerp(VerticleCurves[0], VerticleCurves[1], t_y * 2.0f);
            }
            else
            {
                verticle_curve = BezierCurve2D.Lerp(VerticleCurves[1], VerticleCurves[2], (t_y - 0.5f) * 2.0f);
            }

            BezierCurve3D verticle_curve_3d = new BezierCurve3D();
            verticle_curve_3d.controlPoint1 = center + localRight * verticle_curve.controlPoint1.x + localUp * verticle_curve.controlPoint1.y;
            verticle_curve_3d.controlPoint2 = center + localRight * verticle_curve.controlPoint2.x + localUp * verticle_curve.controlPoint2.y;
            verticle_curve_3d.controlPoint3 = center + localRight * verticle_curve.controlPoint3.x + localUp * verticle_curve.controlPoint3.y;
            verticle_curve_3d.controlPoint4 = center + localRight * verticle_curve.controlPoint4.x + localUp * verticle_curve.controlPoint4.y;

            verticle_curve_3d.DrawBezier(100, transform, boneColor);


            for (int x = 0; x < 2; x++)
            {
                float t_x = x / (float)(2 - 1);

                // verticle_curve.CenterAtOrign();

            }
        }
    }

    public (Vector3, Vector3) GetEndInfo()
    {
        return (transform.TransformPoint(CentralLine.controlPoint4), transform.TransformVector(CentralLine.GetTangent(1)));
    }

    public void RandomBlockGeneration()
    {
        this.CentralLine = new BezierCurve3D();
        this.CentralLine.controlPoint1 = new Vector3();
        this.CentralLine.controlPoint2 = new Vector3(UnityEngine.Random.Range(-5f, 5f), UnityEngine.Random.Range(0f, 10f), UnityEngine.Random.Range(20f, 40f));
        this.CentralLine.controlPoint3 = new Vector3(UnityEngine.Random.Range(-5f, 5f), UnityEngine.Random.Range(0f, 10f), UnityEngine.Random.Range(50f, 70f));
        this.CentralLine.controlPoint4 = new Vector3(UnityEngine.Random.Range(-5f, 5f), UnityEngine.Random.Range(0f, 10f), UnityEngine.Random.Range(80f, 120f));

        this.VerticleCurves = new BezierCurve2D[3];

        for (int i = 0; i < this.VerticleCurves.Length; i++)
        {
            Vector2 widthRange = new Vector2(-20.0f, 20.0f);
            Vector2 heightRange = new Vector2(0.0f, 10.0f);
            this.VerticleCurves[i] = BezierCurve2D.CreateRoadShapeCurve(widthRange, heightRange);
        }

        this.thickness = 3.0f;
    }

}


