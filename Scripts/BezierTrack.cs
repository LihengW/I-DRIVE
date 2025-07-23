#region HeadComments
// ********************************************************************
//  Copyright (C) #YEAR# #COMPANYNAME# #PROJECTNAME#
//  作    者：#AUTHOR#
//  文件路径：#FILEPATH#
//  创建日期：#CREATIONDATE#
//  功能描述：
// *********************************************************************
#endregion

using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BezierTrack : MonoBehaviour
{
    static public int Meshsample_Dir = 50;

    public BezierCurve3D centralSpine;
    public float width = 0.5f;
    public float length = 30.0f;
    private Mesh trackMesh;

    public bool AttachToRoad = false;
    public BezierRoad parentRoad = null;

    [SerializeField] private Transform snappedPoint = null;
    [SerializeField] private Player snapped_player = null;
    [SerializeField] private Vector3 snapped_offset = Vector3.zero;
    [SerializeField] private float snapped_t = 0.0f;

    public GameObject supportPrefab;

    public int supportNum = 5;

    // the start point / mid point(or anchor) / end point normal 
    [SerializeField] public Vector3[] TrackUps;


    private void Start()
    {
        ;
    }

    private void Update()
    {
        centralSpine.DrawBezier(100, transform, color:Color.blue);
    }

    public void GenerateTrackMesh()
    {
        Mesh mesh = new Mesh();

        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uv = new List<Vector2>();
        List<int> triangles = new List<int>();

        for (int i = 0; i < Meshsample_Dir; i++)
        {
            float t = i / (float)(Meshsample_Dir - 1);

            Vector3 height_offset = GetUpVector(t) * width / 2.0f;

            Vector3 width_offset = GetRightVector(t) * width / 2.0f;

            vertices.Add(centralSpine.GetPos(t) - width_offset + height_offset);
            vertices.Add(centralSpine.GetPos(t) + width_offset + height_offset);
            vertices.Add(centralSpine.GetPos(t) - width_offset - height_offset);
            vertices.Add(centralSpine.GetPos(t) + width_offset - height_offset);
        }                                       

        // vertice idx :   0 | 1
        // vertice idx :   2 | 3

        for (int i = 0; i < Meshsample_Dir - 1; i++)
        {
            int idx_start = i * 4;

            int next_start = i * 4 + 4;

            // up
            triangles.Add(idx_start + 0);
            triangles.Add(next_start + 0);
            triangles.Add(idx_start + 1);

            triangles.Add(idx_start + 1);
            triangles.Add(next_start + 0);
            triangles.Add(next_start + 1);

            // down

            triangles.Add(idx_start + 3);
            triangles.Add(next_start + 2);
            triangles.Add(idx_start + 2);

            triangles.Add(next_start + 3);
            triangles.Add(next_start + 2);
            triangles.Add(idx_start + 3);

            // left
            triangles.Add(idx_start + 2);
            triangles.Add(next_start + 0);
            triangles.Add(idx_start + 0);

            triangles.Add(idx_start + 2);
            triangles.Add(next_start + 2);
            triangles.Add(next_start + 0);

            // right
            triangles.Add(idx_start + 1);
            triangles.Add(next_start + 1);
            triangles.Add(idx_start + 3);

            triangles.Add(next_start + 1);
            triangles.Add(next_start + 3);
            triangles.Add(idx_start + 3);
        }

        // front 
        triangles.Add(2);
        triangles.Add(0);
        triangles.Add(3);

        triangles.Add(3);
        triangles.Add(0);
        triangles.Add(1);

        int back_idx = (Meshsample_Dir - 1) * 4;

        triangles.Add(back_idx + 3);
        triangles.Add(back_idx + 0);
        triangles.Add(back_idx + 2);

        triangles.Add(back_idx + 1);
        triangles.Add(back_idx + 0);
        triangles.Add(back_idx + 3);

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uv.ToArray();
        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshCollider>().sharedMesh = mesh;
        GetComponent<MeshCollider>().convex = false;
    }

    public void GenerateSupport()
    {
        for (int i = 0; i < supportNum; i++)
        {
            float t = i / (float)(supportNum - 1);
            var support = GameObject.Instantiate(supportPrefab);
            support.transform.position = transform.TransformPoint(centralSpine.GetPos(t) - GetUpVector(t) * 0.6f);
            support.transform.LookAt(support.transform.position + transform.TransformDirection(centralSpine.GetTangent(t)), transform.TransformDirection(GetUpVector(t)));
            support.transform.parent = transform;
        }
    }

    public Vector3 GetUpVector(float t)
    {
        if (!AttachToRoad)
        {
            if (t < 0.5)
            {
                return Vector3.Lerp(TrackUps[0], TrackUps[1], t * 2.0f).normalized;
            }
            else
            {
                return Vector3.Lerp(TrackUps[1], TrackUps[2], (t - 0.5f) * 2.0f).normalized;
            }
        }
        else
        {
            return parentRoad.GetUpVector(t);
        }
    }

    public Vector3 GetRightVector(float t)
    {
        return Vector3.Cross(GetUpVector(t), centralSpine.GetTangent(t)).normalized;
    }

    public Vector3 GetWorldPosition(float t)
    {
        return transform.position + centralSpine.GetPos(t);
    }

    public void FollowTrack()
    {
        float new_t = snapped_t + snapped_player.currentspeed / length * Time.deltaTime;

        if (snapped_player)
        {
            snapped_player.transform.position = GetWorldPosition(new_t) - snappedPoint.position + snapped_player.transform.position;
            snapped_player.transform.LookAt(snapped_player.transform.position + centralSpine.GetTangent(new_t), GetUpVector(new_t));
            snapped_t = new_t;
        }

        if (new_t >= 1.0f)
        {
            snapped_player.LeaveTrack(this);
        }


    }
    public void StartTrackRun(Player playerscript, float t, Transform snapPoint)
    {
        snapped_t = t;
        snapped_player = playerscript;
        snappedPoint = snapPoint;
        snapped_offset = playerscript.transform.position - centralSpine.GetPos(t);
    }

    public void EndTrackRun()
    {
        snapped_t = 0.0f;
        snapped_player = null;
        snappedPoint = null;
        snapped_offset = Vector3.zero;
    }

    public (Vector3, float) GetNearestPointonTrack(Vector3 point)
    {
        if (!centralSpine.hasSample)
        {
            centralSpine.CreateSample();
        }

        int min_idx = 0;
        Vector3 min_pos = Vector3.down;
        float min_dist = float.MaxValue;


        for (int i = 0; i < centralSpine.samplePoints.Count; i++)
        {
            var sample = centralSpine.samplePoints[i] + transform.position;
            if ((point - sample).magnitude < min_dist)
            {
                min_dist = (point - sample).magnitude;
                min_pos = sample;
                min_idx = i;
            }
        }

        return (min_pos, (float)min_idx / (float)(centralSpine.samplePoints.Count - 1));
    }

    public void GenerateTrackOnRoad(BezierRoad road, Vector2 t_range, float y)
    {
        // y belongs to (-1, 1), refering from left to right
        if (road != null)
        {
            BezierCurve3D baseSpine;
            if (y > 0.0f)
            {
                baseSpine = BezierCurve3D.Lerp(road.centralLine, road.rightSideCurve, y);
            }
            else
            {
                baseSpine = BezierCurve3D.Lerp(road.leftSideCurve, road.centralLine, 1 + y);
            }

            BezierCurve3D spine = new BezierCurve3D();

            spine.controlPoint1 = baseSpine.GetPos(Mathf.Lerp(t_range.x, t_range.y, 1 / 4.0f)) + road.GetUpVector(Mathf.Lerp(t_range.x, t_range.y, 1 / 4.0f)) * (road.thickness / 2.0f + 0.5f);
            spine.controlPoint2 = baseSpine.GetPos(Mathf.Lerp(t_range.x, t_range.y, 2 / 4.0f)) + road.GetUpVector(Mathf.Lerp(t_range.x, t_range.y, 2 / 4.0f)) * (road.thickness / 2.0f + 0.5f);
            spine.controlPoint3 = baseSpine.GetPos(Mathf.Lerp(t_range.x, t_range.y, 3 / 4.0f)) + road.GetUpVector(Mathf.Lerp(t_range.x, t_range.y, 3 / 4.0f)) * (road.thickness / 2.0f + 0.5f);
            spine.controlPoint4 = baseSpine.GetPos(Mathf.Lerp(t_range.x, t_range.y, 4 / 4.0f)) + road.GetUpVector(Mathf.Lerp(t_range.x, t_range.y, 4 / 4.0f)) * (road.thickness / 2.0f + 0.5f);

            centralSpine = spine;
            centralSpine.CreateSample();

            length = road.length * (t_range.y - t_range.x);
        }
    }



}
