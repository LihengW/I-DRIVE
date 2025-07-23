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

public class AntiGravityTriggerBox : MonoBehaviour
{
    BoxCollider _collider;
    public bool Activate = true;

    private void Start()
    {
        _collider = GetComponent<BoxCollider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            Player playercomponent = other.gameObject.GetComponent<Player>();
            if (Activate)
            {
                StartCoroutine(playercomponent.EnterAntiGravity());
            }
            else
            {
                StartCoroutine(playercomponent.LeaveAntiGravity());
            }
        }
    }

}
