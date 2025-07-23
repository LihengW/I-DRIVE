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
using Unity.VisualScripting;
using UnityEditor.SearchService;
using UnityEngine;

public class RoadManager : MonoBehaviour
{
    public GameObject roadprefab;

    public GameObject bannerPrefab;

    public OverlapHelper overlapHelper;

    public List<BezierRoad> Roads = new List<BezierRoad>();

    public RoadObjectManager RoadObjectManager;

    private void Awake()
    {
        overlapHelper = new OverlapHelper();
    }

    private void Start()
    {
        var roadobj = GameObject.Instantiate(roadprefab);
        RoadEndInfo roadinfo = roadobj.GetComponent<BezierRoad>().GenerateRoad(RoadBlockType.Forward, null);
        roadobj.GetComponent<BezierRoad>().GenerationRoadMesh();

        overlapHelper.AddNewCurve(roadobj.GetComponent<BezierRoad>().centralLine);

        for (int i = 0; i < 50; i ++)
        {
            roadobj = GenerateSmoothRoad((RoadBlockType)Random.Range(0, 4), roadobj.GetComponent<BezierRoad>(), roadinfo);
            roadinfo = roadobj.GetComponent<BezierRoad>().OutputEndInfo();

        }

        foreach (var road in Roads)
        {
            GenerateBannerAlong(road);
        }

        RoadObjectManager.BuildRoadObjects();
    }

    public GameObject GenerateSmoothRoad(RoadBlockType roadtype, BezierRoad lastRoad, RoadEndInfo info = null, RoadZType roadZType = RoadZType.Flat)
    {
        BezierCurve3D next_centerspine;

        // check overlap
        // make Z axis adjustments

        next_centerspine = GenerateCentralLine(roadtype, info);

        roadZType = (RoadZType)Random.Range(0, 3);

        float height = 5.0f;
        BezierCurve3D trybend = next_centerspine.BendZ(Vector3.up, roadZType, height);
        int time = 0;

        while (overlapHelper.CheckOverlap(trybend))
        {
            time++;

            if (time > 60)
            {
                Debug.Log("Fail to Find Route!");
                break;
            }

            height += 7.5f;

            if (time % 3 == 0)
            {
                roadZType = (RoadZType)Random.Range(1, 3);
                height = 5.0f;
            }

            trybend = next_centerspine.BendZ(Vector3.up, roadZType, height);

            if (time % 20 == 0)
            {
                roadtype = (RoadBlockType)Random.Range(0, 5);
                next_centerspine = GenerateCentralLine(roadtype, info);
                Debug.Log("Forced Repick");
                height = 5.0f;
            }

            Debug.Log("Reset!");
        }

        next_centerspine = trybend;

        info.SetCentralSpine(next_centerspine);
        // instantiate

        var nextRoad = GameObject.Instantiate(roadprefab);

        RoadEndInfo nextroadinfo = nextRoad.GetComponent<BezierRoad>().GenerateRoad(roadtype, info);


        var intervalRoad = GameObject.Instantiate(roadprefab);

        intervalRoad.GetComponent<BezierRoad>().GenerateInterval(lastRoad, nextRoad.GetComponent<BezierRoad>());

        intervalRoad.GetComponent<BezierRoad>().GenerationRoadMesh();

        nextRoad.GetComponent<BezierRoad>().GenerationRoadMesh();

        overlapHelper.AddNewRoad(nextRoad.GetComponent<BezierRoad>());
        overlapHelper.AddNewRoad(intervalRoad.GetComponent<BezierRoad>());

        // overlapHelper.AddNewCurve(nextRoad.GetComponent<BezierRoad>().centralLine);

        // overlapHelper.AddNewCurve(intervalRoad.GetComponent<BezierRoad>().centralLine);

        Roads.Add(nextRoad.GetComponent<BezierRoad>());

        Roads.Add(intervalRoad.GetComponent<BezierRoad>());

        return nextRoad;
    }

    public void GenerateBannerAlong(BezierRoad road)
    {
        int banner_num = 20;
        BezierCurve3D inner_curve;
        BezierCurve3D outer_curve;
        bool isLeftTurn = false;
        if (road.roadBlockType == RoadBlockType.LeftTurn || road.roadBlockType == RoadBlockType.SharpLeftTurn)
        {
            inner_curve = road.leftSideCurve;
            outer_curve = road.rightSideCurve;
            isLeftTurn = true;
        }
        else
        {
            inner_curve = road.rightSideCurve;
            outer_curve = road.leftSideCurve;
        }

        for (int i = 0; i < banner_num; i++)
        {
            float t = 0.2f + 0.8f * (float)i / (float)(banner_num - 1);
            
            var banner = GameObject.Instantiate(bannerPrefab);
            Vector3 up = road.GetUpVector(t);
            Vector3 right = road.GetRightVector(t);

            var in_normal = isLeftTurn ? -Vector3.Cross(up, outer_curve.GetTangent(t)) : Vector3.Cross(up, outer_curve.GetTangent(t));

            banner.transform.position = outer_curve.GetPos(t) + 0.1f * in_normal + up * road.thickness / 2.0f;

            banner.transform.LookAt(banner.transform.position + in_normal, up);
        }
    }

    public BezierCurve3D GenerateCentralLine(RoadBlockType roadType, RoadEndInfo info)
    {
        BezierCurve3D _centerspine;

        if (roadType == RoadBlockType.Forward)
        {
            _centerspine = BezierCurve3D.GenerateRandomCurve(info.endposition, 100.0f, info.forward_direction, info.up_direction, 30.0f);
        }
        else if (roadType == RoadBlockType.LeftTurn)
        {
            _centerspine = BezierCurve3D.GenerateLeftTurn(info.endposition, 100.0f, info.forward_direction, info.up_direction, 30.0f);
        }
        else if (roadType == RoadBlockType.SharpLeftTurn)
        {
            _centerspine = BezierCurve3D.GenerateSharpLeftTurn(info.endposition, 100.0f, info.forward_direction, info.up_direction, 30.0f);
        }
        else if (roadType == RoadBlockType.RightTurn)
        {
            _centerspine = BezierCurve3D.GenerateRightTurn(info.endposition, 100.0f, info.forward_direction, info.up_direction, 30.0f);
        }
        else if (roadType == RoadBlockType.SharpRightTurn)
        {
            _centerspine = BezierCurve3D.GenerateSharpRightTurn(info.endposition, 100.0f, info.forward_direction, info.up_direction, 30.0f);
        }
        else
        {
            _centerspine = BezierCurve3D.GenerateRandomCurve(info.endposition, 100.0f, info.forward_direction, info.up_direction, 30.0f);
        }

        return _centerspine;
    }
}


public class OverlapHelper
{
    public List<BezierCurve3D> roadLines = new();

    public List<BezierAABB> roadAABBs = new();

    public Dictionary<BezierAABB, BezierCurve3D> AABB2Bezier = new();

    public static int sampleNum = 20;

    public static float threshold = 8.0f;


    public void AddNewCurve(BezierCurve3D roadcurve)
    {
        // what we can also do is split one aabb into 4 or more to accommodate the shape of curves
        roadLines.Add(roadcurve);
        var projected = roadcurve.ProjectToXY();
        var aabb = BezierAABB.GetAABB(projected);
        roadAABBs.Add(aabb);
        AABB2Bezier.Add(aabb, roadcurve);
    }

    public void AddNewRoad(BezierRoad road)
    {
        var roadcurve = road.centralLine;

        roadLines.Add(roadcurve);
        var projected = roadcurve.ProjectToXY();
        var aabb = BezierAABB.GetAABB(projected);
        roadAABBs.Add(aabb);
        AABB2Bezier.Add(aabb, roadcurve);


        roadcurve = road.leftSideCurve;

        roadLines.Add(roadcurve);
        projected = roadcurve.ProjectToXY();
        aabb = BezierAABB.GetAABB(projected);
        roadAABBs.Add(aabb);
        AABB2Bezier.Add(aabb, roadcurve);

        roadcurve = road.rightSideCurve;

        roadLines.Add(roadcurve);
        projected = roadcurve.ProjectToXY();
        aabb = BezierAABB.GetAABB(projected);
        roadAABBs.Add(aabb);
        AABB2Bezier.Add(aabb, roadcurve);
    }

    public bool CheckOverlap(BezierCurve3D roadcurve)
    {
        // brute force for now, will change into algo later
        var aabb = BezierAABB.GetAABB(roadcurve.ProjectToXY());

        foreach (var existed in roadAABBs)
        {
            if (BezierAABB.Compare(existed, aabb))
            {
                var existed_curve = AABB2Bezier[existed];
                bool connected = (existed_curve.controlPoint4 - roadcurve.controlPoint1).magnitude < 10.5f;
                List<Vector3> Nodes1 = new List<Vector3>();
                List<Vector3> Nodes2 = new List<Vector3>();

                for (int i = 0; i < sampleNum; i++)
                {
                    float t;
                    if (connected)
                    {
                        t = 0.2f + 0.8f * (i / (float)(sampleNum - 1));
                    }
                    else
                    {
                        t = (i / (float)(sampleNum - 1));
                    }

                    Nodes1.Add(existed_curve.GetPos(t));
                    Nodes2.Add(roadcurve.GetPos(t));
                }


                for (int i = 0; i < sampleNum; i++)
                {

                    float t1;
                    if (connected)
                    {
                        t1 = 0.2f + 0.8f * (i / (float)(sampleNum - 1));
                    }
                    else
                    {
                        t1 = (i / (float)(sampleNum - 1));
                    }

                    for (int j = 0; j < sampleNum; j++)
                    {

                        float t2;
                        if (connected)
                        {
                            t2 = 0.1f + 0.9f * (i / (float)(sampleNum - 1));
                        }
                        else
                        {
                            t2 = (i / (float)(sampleNum - 1));
                        }

                        Vector3 Offset = Nodes1[i] - Nodes2[j];

                        float heightOffset = Mathf.Abs(Offset.y);
                        float yOffset1 = Mathf.Abs(Vector3.Dot(Offset, existed_curve.GetTangent(t1)));
                        float yOffset2 = Mathf.Abs(Vector3.Dot(Offset, roadcurve.GetTangent(t2)));

                        if (heightOffset < 5.0f && (yOffset1 < 5.0f || yOffset2 < 5.0f))
                        {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }
}
