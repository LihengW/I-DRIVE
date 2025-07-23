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
using TMPro;
using UnityEngine;

public class KartCamera : MonoBehaviour
{
    [Header("Kart Reference")]
    public Transform kart; 
    private Player playerscript;
    private Camera mainCamera;

    [Header("Camera Settings")]
    public float followSpeed = 10f; 
    public float rotateSpeed = 5f; 
    public float heightOffset = 2f; 
    public float distanceOffset = 5f; 
    public float antigravityOffset = 3.0f;

    public float boostedFov = 90.0f;
    public float normalFov = 60.0f;

    Vector3 currentVelocity = Vector3.zero;
    private Vector3 targetPosition; 
    private Quaternion targetRotation;


    private void Start()
    {
        if (kart == null)
        {
            Debug.LogError("Kart Transform is not assigned!");
        }

        playerscript = kart.GetComponent<Player>();

        transform.position = kart.position - kart.forward * distanceOffset + Vector3.up * heightOffset;

        mainCamera = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (kart != null)
        {
            if (playerscript.antiGravity)
            {
                Ray upRay = new Ray(kart.position, kart.up);

                Vector3 upDist;
                upDist = upRay.GetPoint(antigravityOffset);
                transform.position = upDist - kart.forward * distanceOffset;

                targetRotation = Quaternion.LookRotation(kart.position - transform.position, kart.transform.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
            }
            else
            {

                if (playerscript.Boost)
                {
                    // Increase the damping effect when boosting
                    mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, boostedFov, Time.deltaTime);
                }
                else
                {
                    mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, normalFov, Time.deltaTime);
                }

                targetPosition = kart.position - kart.forward * distanceOffset + Vector3.up * heightOffset;
                targetRotation = Quaternion.LookRotation(kart.position - transform.position, Vector3.up);
                // targetPosition.y = Mathf.Lerp (transform.position.y, targetPosition.y, 0.7f);
                // transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, followSpeed * Time.deltaTime);
                transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
                // Smoothly rotate the camera to match the kart's rotation
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
                // transform.rotation = targetRotation;

            }

        }
    }
}
