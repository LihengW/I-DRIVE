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
using System.Runtime.ConstrainedExecution;
using UnityEditor;
using UnityEngine;
public enum RoadTurning
{
    None, Left, Right
}

public enum RoadSurface
{
    Flat, Rough, AntiGravity
}

public class RoadEndInfo
{
    public RoadSurface Surface;

    public Vector3 endposition;

    public Vector3 forward_direction;
    public Vector3 up_direction;

    public Vector2 spine_widths;

    public Vector3 left_spine_direction;
    public Vector3 right_spine_direction;

    public bool contain_centralspine = false;
    public BezierCurve3D centralspine;

    public void SetCentralSpine(BezierCurve3D curve)
    {
        contain_centralspine = true;
        centralspine = curve;
    }
}

public class BezierRoad : MonoBehaviour
{
    static public int Meshsample_Dir = 100;
    static public int Meshsample_Tan = 9;

    [SerializeField] public BezierCurve3D centralLine;

    [SerializeField] public BezierCurve3D leftSideCurve;

    [SerializeField] public BezierCurve3D rightSideCurve;

    [SerializeField] public float idealWidth = 20.0f;

    [SerializeField] public float thickness = 4.0f;

    [SerializeField] Color[] debugColor;

    [SerializeField] public RoadBlockType roadBlockType = RoadBlockType.Forward;

    [SerializeField] public RoadSurface surfaceType = RoadSurface.Flat;


    [SerializeField] public bool isSharp = false;

    // the max curvature point is also called anchor point
    private float _max_curvature_t = -1.0f;

    [SerializeField] public bool isWorldUp = true;
    [SerializeField] public bool usingAnchor = false;

    // the start point / mid point(or anchor) / end point normal 
    [SerializeField] public Vector3[] RoadUps;

    // the start point / mid point(or anchor) / end point width
    // (left, right)
    [SerializeField] public Vector2[] RoadWidths;

    [SerializeField]
    public float length = 100.0f;



    private void Awake()
    {
        RoadUps = new Vector3[3];

        RoadUps[0] = Vector3.up;
        RoadUps[1] = Vector3.up;
        RoadUps[2] = Vector3.up;

        RoadWidths = new Vector2[3];

        RoadWidths[0] = new Vector2(idealWidth / 2.0f, idealWidth / 2.0f);
        RoadWidths[1] = new Vector2(idealWidth / 2.0f, idealWidth / 2.0f);
        RoadWidths[2] = new Vector2(idealWidth / 2.0f, idealWidth / 2.0f);

    }

    private void Start()
    {
        // GenerateRoad(RoadBlockType.SharpLeftTurn);
    }

    private void Update()
    {
        DebugShow();
    }

    public RoadEndInfo GenerateRoad(RoadBlockType roadtype, RoadEndInfo lastRoadInfo = null)
    {
        roadBlockType = roadtype;

        Vector3 start_direction = Vector3.forward;

        if (lastRoadInfo == null)
        {
            SetCentralLine(BezierCurve3D.GenerateRandomCurve(Vector3.zero, length, Vector3.forward, Vector3.up, 30.0f));
            // start_direction = centralLine.GetTangent(0.0f);
        }
        else
        {
            BezierCurve3D _centerspine;

            if (lastRoadInfo.contain_centralspine)
            {
                _centerspine = lastRoadInfo.centralspine;
            }
            else
            {
                if (roadtype == RoadBlockType.Forward)
                {
                    _centerspine = BezierCurve3D.GenerateRandomCurve(lastRoadInfo.endposition, length, lastRoadInfo.forward_direction, lastRoadInfo.up_direction, 30.0f);
                }
                else if (roadtype == RoadBlockType.LeftTurn)
                {
                    _centerspine = BezierCurve3D.GenerateLeftTurn(lastRoadInfo.endposition, length, lastRoadInfo.forward_direction, lastRoadInfo.up_direction, 30.0f);
                }
                else if (roadtype == RoadBlockType.SharpLeftTurn)
                {
                    _centerspine = BezierCurve3D.GenerateSharpLeftTurn(lastRoadInfo.endposition, length, lastRoadInfo.forward_direction, lastRoadInfo.up_direction, 30.0f);
                }
                else if (roadtype == RoadBlockType.RightTurn)
                {
                    _centerspine = BezierCurve3D.GenerateRightTurn(lastRoadInfo.endposition, length, lastRoadInfo.forward_direction, lastRoadInfo.up_direction, 30.0f);
                }
                else if (roadtype == RoadBlockType.SharpRightTurn)
                {
                    _centerspine = BezierCurve3D.GenerateSharpRightTurn(lastRoadInfo.endposition, length, lastRoadInfo.forward_direction, lastRoadInfo.up_direction, 30.0f);
                }
                else
                {
                    _centerspine = BezierCurve3D.GenerateRandomCurve(lastRoadInfo.endposition, length, lastRoadInfo.forward_direction, lastRoadInfo.up_direction, 30.0f);
                }
            }

            SetCentralLine(_centerspine);

            if (roadtype == RoadBlockType.SharpLeftTurn)
            {
                isSharp = true;
                usingAnchor = true;
                Vector3 mid_up = Vector3.Lerp(-GetRightVector(_max_curvature_t), lastRoadInfo.up_direction, 0.75f);
                List<Vector3> ups = new(3);
                ups.Add(lastRoadInfo.up_direction);
                ups.Add(mid_up);
                ups.Add(lastRoadInfo.up_direction);
                SetUpVectors(ups);
            }
            else if (roadtype == RoadBlockType.SharpRightTurn)
            {
                isSharp = true;
                usingAnchor = true;
                Vector3 mid_up = Vector3.Lerp(GetRightVector(_max_curvature_t), lastRoadInfo.up_direction, 0.75f);
                List<Vector3> ups = new(3);
                ups.Add(lastRoadInfo.up_direction);
                ups.Add(mid_up);
                ups.Add(lastRoadInfo.up_direction);
                SetUpVectors(ups);
            }

            RoadWidths[0] = lastRoadInfo.spine_widths;

            start_direction = lastRoadInfo.forward_direction;
        }

        Vector3[] pivots = new Vector3[4];

        if (!isSharp)
        {
            for (int i = 0; i <= 3; i++)
            {
                pivots[i] = centralLine.GetPos(i / 3.0f);
            }
        }
        else
        {
            // sharp turn
            pivots[0] = centralLine.GetPos(0);
            pivots[3] = centralLine.GetPos(1);

            if (_max_curvature_t > 0.5f)
            {
                pivots[1] = centralLine.GetPos(_max_curvature_t / 2.0f);
                pivots[2] = centralLine.GetPos(_max_curvature_t / 2.0f);
            }
            else
            {
                pivots[1] = centralLine.GetPos(_max_curvature_t);
                pivots[2] = centralLine.GetPos((_max_curvature_t + 1.0f) / 2.0f);
            }
        }

        RoadTurning roadTurning = RoadTurning.None;

        if (roadtype == RoadBlockType.SharpLeftTurn || roadtype == RoadBlockType.LeftTurn)
        {
            roadTurning = RoadTurning.Left;
            RoadWidths[1].y *= 6.0f; 
        }
        else if (roadtype == RoadBlockType.RightTurn || roadtype == RoadBlockType.SharpRightTurn)
        {
            roadTurning = RoadTurning.Right;
            RoadWidths[1].x *= 6.0f;
        }


        leftSideCurve = new BezierCurve3D();
        rightSideCurve = new BezierCurve3D();

        Vector3 start_right = Vector3.Cross(GetUpVector(0.0f), start_direction).normalized;

        for (int i = 0; i <= 3; i++)
        {
            Vector3 right = GetRightVector(i / 3.0f);
            leftSideCurve.SetControlPoint(i, pivots[i] - right * GetWidth(i / 3.0f).x );
            rightSideCurve.SetControlPoint(i, pivots[i] + right * GetWidth(i / 3.0f).y);
        }

        return OutputEndInfo();
    }

    public RoadEndInfo GenerateInterval(BezierRoad currentRoad, BezierRoad nextRoad, float length = 20.0f)
    {
        BezierCurve3D leftspine_cur = currentRoad.leftSideCurve;
        BezierCurve3D rightspine_cur = currentRoad.rightSideCurve;

        BezierCurve3D leftspine_next = nextRoad.leftSideCurve;
        BezierCurve3D rightspine_next = nextRoad.rightSideCurve;

        BezierCurve3D central_spine = new BezierCurve3D();
        central_spine.controlPoint1 = currentRoad.centralLine.controlPoint4;
        central_spine.controlPoint2 = central_spine.controlPoint1 + length / 3.0f * currentRoad.centralLine.GetTangent(1.0f);
        central_spine.controlPoint3 = central_spine.controlPoint2 + length / 3.0f * Vector3.Lerp(currentRoad.centralLine.GetTangent(1.0f), nextRoad.centralLine.GetTangent(0.0f), 0.5f);
        central_spine.controlPoint4 = central_spine.controlPoint3 + length / 3.0f * nextRoad.centralLine.GetTangent(0.0f);

        SetCentralLine(central_spine);

        Vector3 right = nextRoad.GetRightVector(0.0f);

        Vector3 left_end = central_spine.controlPoint4 - right * currentRoad.GetWidth(1.0f).x;
        Vector3 right_end = central_spine.controlPoint4 + right * currentRoad.GetWidth(1.0f).y;


        var intersect = MathTool.FindNearestDistanceAndPoints(currentRoad.leftSideCurve.controlPoint4, currentRoad.leftSideCurve.GetTangent(1.0f), left_end, nextRoad.leftSideCurve.GetTangent(0.0f));

        BezierCurve3D left_trans_spine = new BezierCurve3D();
        // float dist = (left_end - currentRoad.leftSideCurve.controlPoint4).magnitude / (currentRoad.leftSideCurve.GetTangent(1.0f) + nextRoad.leftSideCurve.GetTangent(0.0f)).magnitude;
        float dist = 1 / 3.0f * length;
        left_trans_spine.controlPoint1 = currentRoad.leftSideCurve.controlPoint4;
        left_trans_spine.controlPoint4 = left_end;
        if (Vector3.Dot(currentRoad.leftSideCurve.GetTangent(1.0f), nextRoad.leftSideCurve.GetTangent(0.0f)) > Mathf.Cos(180 * Mathf.Deg2Rad))
        {
            dist = 1 / 3.0f * length;
            left_trans_spine.controlPoint2 = left_trans_spine.controlPoint1 + dist * currentRoad.leftSideCurve.GetTangent(1.0f);
            left_trans_spine.controlPoint3 = left_end - dist * nextRoad.leftSideCurve.GetTangent(0.0f);
        }
        else
        {
            dist = (left_end - currentRoad.leftSideCurve.controlPoint4).magnitude / (currentRoad.leftSideCurve.GetTangent(1.0f) + nextRoad.leftSideCurve.GetTangent(0.0f)).magnitude;
            left_trans_spine.controlPoint2 = intersect.distance < 5.0f ? intersect.intersectionPoint1: left_trans_spine.controlPoint1 + dist * currentRoad.leftSideCurve.GetTangent(1.0f);
            left_trans_spine.controlPoint3 = intersect.distance < 5.0f ? intersect.intersectionPoint2 : left_end - dist * nextRoad.leftSideCurve.GetTangent(0.0f);
        }

        leftSideCurve = left_trans_spine;

        intersect = MathTool.FindNearestDistanceAndPoints(currentRoad.rightSideCurve.controlPoint4, currentRoad.rightSideCurve.GetTangent(1.0f), right_end, nextRoad.rightSideCurve.GetTangent(0.0f));

        BezierCurve3D right_trans_spine = new BezierCurve3D();
        // float dist2 = (right_end - currentRoad.rightSideCurve.controlPoint4).magnitude / (currentRoad.rightSideCurve.GetTangent(1.0f) + nextRoad.rightSideCurve.GetTangent(0.0f)).magnitude;
        right_trans_spine.controlPoint1 = currentRoad.rightSideCurve.controlPoint4;
        right_trans_spine.controlPoint4 = right_end;

        if (Vector3.Dot(currentRoad.leftSideCurve.GetTangent(1.0f), nextRoad.leftSideCurve.GetTangent(0.0f)) > Mathf.Cos(180 * Mathf.Deg2Rad))
        {
            dist = 1 / 3.0f * length;
            right_trans_spine.controlPoint2 = right_trans_spine.controlPoint1 + dist * currentRoad.rightSideCurve.GetTangent(1.0f);
            right_trans_spine.controlPoint3 = right_end - dist * nextRoad.rightSideCurve.GetTangent(0.0f);
        }
        else
        {
            dist = (left_end - currentRoad.leftSideCurve.controlPoint4).magnitude / (currentRoad.leftSideCurve.GetTangent(1.0f) + nextRoad.leftSideCurve.GetTangent(0.0f)).magnitude;
            right_trans_spine.controlPoint2 = intersect.distance < 5.0f ? intersect.intersectionPoint1 : right_trans_spine.controlPoint1 + dist * currentRoad.rightSideCurve.GetTangent(1.0f);
            right_trans_spine.controlPoint3 = intersect.distance < 5.0f ? intersect.intersectionPoint2 : right_end - dist * nextRoad.rightSideCurve.GetTangent(0.0f);
        }

        rightSideCurve = right_trans_spine;

        nextRoad.Offset(central_spine.controlPoint4 - currentRoad.centralLine.controlPoint4);

        return OutputEndInfo();

    }

    public void GenerationRoadMesh()
    {
        Mesh mesh = new Mesh();

        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> bottom_vertices = new List<Vector3>();
        List<Vector2> uv = new List<Vector2>();
        List<int> triangles = new List<int>();

        if (surfaceType == RoadSurface.Flat)
        {
            List<BezierCurve3D> Base_Curves = new();

            if (Meshsample_Tan % 2 == 0)
            {
                Meshsample_Tan += 1;
            }

            int lerped_num = Meshsample_Tan - 3;

            if (lerped_num <= 0)
            {
                lerped_num = 0;
            }

            // lerp all the curves to smooth the surface

            Base_Curves.Add(leftSideCurve);

            for (int k = 1; k <= lerped_num / 2; k++)
            {
                float lerpv = (float)k / (lerped_num / 2.0f + 1);
                Base_Curves.Add(BezierCurve3D.Lerp(leftSideCurve, centralLine, lerpv));
            }

            Base_Curves.Add(centralLine);

            for (int k = 1; k <= lerped_num / 2; k++)
            {
                float lerpv = (float)k / (lerped_num / 2.0f + 1);
                Base_Curves.Add(BezierCurve3D.Lerp(centralLine, rightSideCurve, lerpv));
            }

            Base_Curves.Add(rightSideCurve);


            for (int i = 0; i < Meshsample_Dir; i++)
            {
                float t = i / (float)(Meshsample_Dir - 1);

                // use all the lerped curves saved in the List

                Vector3 thickness_offset = GetUpVector(t) * thickness / 2.0f;

                for (int j = 0; j < Base_Curves.Count; j++)
                {
                    vertices.Add(Base_Curves[j].GetPos(t) + thickness_offset);
                }

                for (int j = 0; j < Base_Curves.Count; j++)
                {
                    vertices.Add(Base_Curves[j].GetPos(t) - thickness_offset);
                }

                // vertices.Add(leftSideCurve.GetPos(t) + thickness_offset);
                // vertices.Add(centralLine.GetPos(t) + thickness_offset);
                // vertices.Add(rightSideCurve.GetPos(t) + thickness_offset);
                // 
                // vertices.Add(leftSideCurve.GetPos(t) - thickness_offset);
                // vertices.Add(centralLine.GetPos(t) - thickness_offset);
                // vertices.Add(rightSideCurve.GetPos(t) - thickness_offset);
            }

            // layout:
            //  surface:     6 | 7 | 8     downside: 9 | 10| 11
            //  surface:     0 | 1 | 2     downside: 3 | 4 | 5

            //   left side:  6 | 0         rightside:   2 | 8
            //   left side:  9 | 3         rightside:   5 | 11

            int row_length = Base_Curves.Count;

            for (int i = 0; i < Meshsample_Dir - 1; i++)
            {


                int idx_start = i * 2 * row_length;

                // Up surface
                for (int k = 0; k < row_length - 1; k++)
                {
                    triangles.Add(idx_start + k);
                    triangles.Add(idx_start + 2 * row_length + k + 1);
                    triangles.Add(idx_start + k + 1);

                    triangles.Add(idx_start + 2 * row_length + k);
                    triangles.Add(idx_start + 2 * row_length + k + 1);
                    triangles.Add(idx_start + k);
                }

                // triangles.Add(idx_start);
                // triangles.Add(idx_start + 7);
                // triangles.Add(idx_start + 1);
                // 
                // triangles.Add(idx_start + 6);
                // triangles.Add(idx_start + 7);
                // triangles.Add(idx_start + 0);
                // 
                // triangles.Add(idx_start + 1);
                // triangles.Add(idx_start + 8);
                // triangles.Add(idx_start + 2);
                // 
                // triangles.Add(idx_start + 1);
                // triangles.Add(idx_start + 7);
                // triangles.Add(idx_start + 8);

                // Down surface

                for (int k = 0; k < row_length - 1; k++)
                {
                    triangles.Add(idx_start + row_length + k);
                    triangles.Add(idx_start + row_length + k + 1);
                    triangles.Add(idx_start + 3 * row_length + k + 1);

                    triangles.Add(idx_start + 3 * row_length + k);
                    triangles.Add(idx_start + row_length + k);
                    triangles.Add(idx_start + 3 * row_length + k + 1);
                }

                // triangles.Add(idx_start + 3);
                // triangles.Add(idx_start + 4);
                // triangles.Add(idx_start + 10);
                // 
                // triangles.Add(idx_start + 9);
                // triangles.Add(idx_start + 3);
                // triangles.Add(idx_start + 10);
                // 
                // triangles.Add(idx_start + 4);
                // triangles.Add(idx_start + 5);
                // triangles.Add(idx_start + 11);
                // 
                // triangles.Add(idx_start + 4);
                // triangles.Add(idx_start + 10);
                // triangles.Add(idx_start + 11);

                // left surface

                triangles.Add(idx_start + row_length);
                triangles.Add(idx_start + 3 * row_length);
                triangles.Add(idx_start + 2 * row_length);

                triangles.Add(idx_start + row_length);
                triangles.Add(idx_start + 2 * row_length);
                triangles.Add(idx_start);

                // triangles.Add(idx_start + 3);
                // triangles.Add(idx_start + 9);
                // triangles.Add(idx_start + 6);
                // 
                // triangles.Add(idx_start + 3);
                // triangles.Add(idx_start + 6);
                // triangles.Add(idx_start + 0);

                // right surface

                triangles.Add(idx_start + 2 * row_length - 1);
                triangles.Add(idx_start + 1 * row_length - 1);
                triangles.Add(idx_start + 4 * row_length - 1);

                triangles.Add(idx_start + 4 * row_length - 1);
                triangles.Add(idx_start + 1 * row_length - 1);
                triangles.Add(idx_start + 3 * row_length - 1);

                // triangles.Add(idx_start + 5);
                // triangles.Add(idx_start + 2);
                // triangles.Add(idx_start + 11);
                // 
                // triangles.Add(idx_start + 11);
                // triangles.Add(idx_start + 2);
                // triangles.Add(idx_start + 8);
            }

            // front side
            // front: 0 | 1 | 2
            // front: 3 | 4 | 5

            // triangles.Add(3);
            // triangles.Add(0);
            // triangles.Add(4);
            // 
            // triangles.Add(4);
            // triangles.Add(0);
            // triangles.Add(1);
            // 
            // triangles.Add(4);
            // triangles.Add(1);
            // triangles.Add(5);
            // 
            // triangles.Add(5);
            // triangles.Add(1);
            // triangles.Add(2);

            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uv.ToArray();
            mesh.RecalculateNormals();

            GetComponent<MeshFilter>().mesh = mesh;
            GetComponent<MeshCollider>().sharedMesh = mesh;
            GetComponent<MeshCollider>().convex = false;
        }
        else
        {
                // Vector3 center = centralLine.GetPos(t_bone);
                // Vector3 localLeft = Vector3.Cross(centralLine.GetTangent(t_bone), RoadUp).normalized;
                // var localRight = -localLeft;
                // Vector3 localUp = Vector3.Cross(localLeft, centralLine.GetTangent(t_bone)).normalized;
            UnityEngine.Debug.Log("Not Implemented");
        }
    }

    // functional
    public void DebugShow()
    {
        centralLine.DrawBezier(100, transform, color: Color.red);
        leftSideCurve.DrawBezier(100, transform, color: Color.cyan);
        rightSideCurve.DrawBezier(100, transform, color: Color.green);

        for (int i = 0; i < 4; i++)
        {
            Debug.DrawLine(leftSideCurve.GetControlPoint(i), centralLine.GetPos(i / 3.0f), Color.yellow);
            Debug.DrawLine(centralLine.GetPos(i / 3.0f), rightSideCurve.GetControlPoint(i), Color.yellow);
        }
    }

    public void Offset(Vector3 offset)
    {
        centralLine.Offset(offset);
        leftSideCurve.Offset(offset);
        rightSideCurve.Offset(offset);
    }

    public void SetUpVectors(List<Vector3> ups)
    {
        if (ups.Count != 3)
        {
            Debug.Log("input length is not correct!");
        }
        else
        {
            RoadUps = ups.ToArray();
            isWorldUp = false;
        }
    }

    public RoadEndInfo OutputEndInfo()
    {
        RoadEndInfo roadend = new();
        roadend.endposition = centralLine.controlPoint4;
        roadend.up_direction = RoadUps[2];
        roadend.forward_direction = centralLine.GetTangent(1.0f);
        roadend.spine_widths = RoadWidths[2];

        return roadend;
    }

    public Vector3 GetUpVector(float t)
    {
        if (isWorldUp)
        {
            return Vector3.up;
        }
        else
        {
            if (!usingAnchor)
            {
                if(t < 0.5)
                {
                    return Vector3.Lerp(RoadUps[0], RoadUps[1], t * 2.0f).normalized;
                }
                else
                {
                    return Vector3.Lerp(RoadUps[1], RoadUps[2], (t - 0.5f) * 2.0f).normalized;
                }
            }
            else
            {
                if (t < _max_curvature_t)
                {
                    return Vector3.Lerp(RoadUps[0], RoadUps[1], t / _max_curvature_t).normalized;
                }
                else
                {
                    return Vector3.Lerp(RoadUps[1], RoadUps[2], (t - _max_curvature_t) / (1.0f - _max_curvature_t)).normalized;
                }
            }
        }
    }

    public Vector2 GetWidth(float t)
    // return (LeftWidth, RightWidth)
    {
        if (!usingAnchor)
        {
            if (t < 0.5)
            {
                return Vector3.Lerp(RoadWidths[0], RoadWidths[1], t * 2.0f);
            }
            else
            {
                return Vector3.Lerp(RoadWidths[1], RoadWidths[2], (t - 0.5f) * 2.0f);
            }
        }
        else
        {
            if (t < _max_curvature_t)
            {
                return Vector3.Lerp(RoadWidths[0], RoadWidths[1], t / _max_curvature_t);
            }
            else
            {
                return Vector3.Lerp(RoadWidths[1], RoadWidths[2], (t - _max_curvature_t) / (1.0f - _max_curvature_t));
            }
        }
    }


    public Vector3 GetRightVector(float t)
    {
        return Vector3.Cross(GetUpVector(t), centralLine.GetTangent(t)).normalized;
    }

    public void SetCentralLine(BezierCurve3D centerline)
    {
        this.centralLine = centerline;
        SearchMaxCurvature();

    }

    public void SearchMaxCurvature()
    {
        float maxcurve_t = 0.5f;
        float maxcurve_value = 0f;
        for (int seg = 1; seg <= 19; seg++)
        {
            float t = seg / 20.0f;
            float curvature = centralLine.GetCurvature(t);
            if (curvature > maxcurve_value)
            {
                maxcurve_value = curvature;
                maxcurve_t = t;
            }
        }
        _max_curvature_t = maxcurve_t;
    }
}
