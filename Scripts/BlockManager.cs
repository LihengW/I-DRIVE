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

public class BlockManager : MonoBehaviour
{

    public GameObject blockprefab;

    private void Awake()
    {
        
    }

    private void Start()
    {
        var startblock = GameObject.Instantiate(blockprefab);
        startblock.GetComponent<BezierBlock>().RandomBlockGeneration();
        startblock.GetComponent<BezierBlock>().GenerateBlockMesh(40, 40);

        GameObject lastBlockObj = startblock;
        GameObject nextBlockObj;
        for (int i = 0; i < 10; i++)
        {
            nextBlockObj = GameObject.Instantiate(blockprefab);
            BezierBlock lastBlock = lastBlockObj.GetComponent<BezierBlock>();
            
            var end_info = lastBlock.GetEndInfo();
            nextBlockObj.GetComponent<BezierBlock>().roadBlockType = (RoadBlockType)Random.Range(1, 5);
            nextBlockObj.GetComponent<BezierBlock>().GenerateFrom(end_info.Item1, end_info.Item2, lastBlock.VerticleCurves[2]);
           
            nextBlockObj.transform.position = end_info.Item1;
            Debug.Log(lastBlockObj.GetComponent<BezierBlock>().CentralLine.GetTangent(1));
            Debug.Log(nextBlockObj.GetComponent<BezierBlock>().CentralLine.GetTangent(0));

            nextBlockObj.GetComponent<BezierBlock>().GenerateBlockMesh(40, 40);

            lastBlockObj = nextBlockObj;
            nextBlockObj = null;
        }
    }



}
