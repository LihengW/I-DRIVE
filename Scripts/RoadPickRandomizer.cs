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

public class RoadPickRandomizer : MonoBehaviour
{
    List<List<float>> TransferMat = new List<List<float>>();

    public void BuildTransferMat()
    {

    }


    public RoadGParameter GetRandomRoadPara(BezierRoad lastRoad)
    {
        RoadGParameter paras = new RoadGParameter();

        int idx = RoadType2Int(lastRoad.roadBlockType);

        RoadBlockType roadType = RoadBlockType.Forward;

        float startp = 0.0f;
        float hitp = Random.Range(0.0f, 1.0f);
        for (int j = 0; j < TransferMat[idx].Count; j++)
        {
            float endp = startp + TransferMat[idx][j];
            if (hitp < endp)
            {
                roadType = Int2RoadType(j);
                break;
            }
            else
            {
                startp = endp;
            }
        }

        return paras;
    }


    private RoadBlockType Int2RoadType(int i)
    {
        return (RoadBlockType)i;
    }

    private int RoadType2Int(RoadBlockType type)
    {
        return (int)type;
    }
}

public class RoadGParameter
{
    RoadBlockType roadType;
}
