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
using UnityEngine.UIElements;

public class RoadObjectManager : MonoBehaviour
{
    public GameObject RampPrefab;

    public GameObject TrackPrefab;

    public RoadManager roadManager;

    public void BuildRoadObjects()
    {
        foreach (var road in roadManager.Roads)
        {
            if (Random.Range(0.0f, 1.0f) >  0.0f)
            {
                CreateRampOntheRoad(road, Random.Range(0.0f, 1.0f));
            }

            if (Random.Range(0.0f, 1.0f) > 0.0f)
            {
                CreateTrackOntheRoad(road, Random.Range(0.0f, 0.5f), Random.Range(0.5f, 1.0f));
            }
        }
    }

    public void CreateRampOntheRoad(BezierRoad road, float t)
    {
        var rampobj = GameObject.Instantiate(RampPrefab);
        Mathf.Clamp(t, 0.2f, 0.8f);
        rampobj.transform.position = road.centralLine.GetPos(t) + road.GetUpVector(t) * road.thickness / 2.0f;
        rampobj.transform.position += road.GetRightVector(t) * Random.Range(-5.0f, 5.0f);
        rampobj.transform.LookAt(rampobj.transform.position + road.centralLine.GetTangent(t), road.GetUpVector(t));
    }

    public void CreateTrackOntheRoad(BezierRoad road, float start, float end)
    {
        var track = GameObject.Instantiate(TrackPrefab).GetComponent<BezierTrack>();
        track.GenerateTrackOnRoad(road, new Vector2(start, end), Random.Range(-1.0f, 1.0f));
        // centralSpine.CreateSample();
        track.GenerateTrackMesh();
        track.GenerateSupport();
    }
}
