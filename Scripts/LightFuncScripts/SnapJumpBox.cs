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
using UnityEngine;

public class SnapJumpBox : MonoBehaviour
{
    public float snapduration = 0.3f;

    SphereCollider _collider = null;
    [SerializeField] Player playerscript = null;
    Transform karttrans = null;
    private bool onLeftWheel = false;

    public Quaternion rightsnapQ = Quaternion.Euler(0, 0, -30f);
    public Quaternion leftsnapQ = Quaternion.Euler(0, 0, 30f);

    [SerializeField] private Transform snap_point = null;
    [SerializeField] private float snapColdDown = 0.0f;

    private void Start()
    {
        _collider = GetComponent<SphereCollider>();
        playerscript = transform.parent.parent.GetComponent<Player>();
        karttrans = playerscript.transform.GetChild(0);
    }

    private void Update()
    {
        if (snapColdDown > 0.0f)
        {
            snapColdDown -= Time.deltaTime;
            if (snapColdDown <= 0.0f)
            {
                snapColdDown = 0.0f;
            }
        }
    }


    private void OnTriggerStay(Collider other)
    {
        if (snapColdDown == 0.0f && other.tag == "Track")
        {
            if (!playerscript.snapping && !playerscript.snapped && !playerscript.grounded)
            {
                Vector3 nearest_p = other.GetComponent<BezierTrack>().GetNearestPointonTrack(_collider.transform.position).Item1;
                Vector3 offset = nearest_p - _collider.transform.position;
                if (Vector3.Dot(offset, playerscript.transform.right) >= 0)
                {
                    // right wheel snap
                    if (karttrans.localEulerAngles.z > -20.0f && karttrans.localEulerAngles.z < 20.0f)
                    {
                        playerscript.snapping = true;
                        StartCoroutine(SnapToQuaternion(karttrans.localRotation, rightsnapQ));
                        onLeftWheel = false;
                    }
                }
                else
                {
                    if (karttrans.localEulerAngles.z > -20.0f && karttrans.localEulerAngles.z < 20.0f)
                    {
                        playerscript.snapping = true;
                        StartCoroutine(SnapToQuaternion(karttrans.localRotation, leftsnapQ));
                        onLeftWheel = true;
                    }
                }
            }
            else if (playerscript.snapping)
            {
                playerscript.Snapping();

                Vector3 snapVec;
                Vector3 trackpoint;
                float t;

                var res = other.GetComponent<BezierTrack>().GetNearestPointonTrack(_collider.transform.position);
                trackpoint = res.Item1;
                t = res.Item2;

                Transform snap_point;

                if (onLeftWheel)
                {
                    snap_point = transform.GetChild(0);
                    // snapVec = trackpoint - transform.GetChild(0).position;
                }
                else
                {
                    snap_point = transform.GetChild(1);
                    // snapVec = trackpoint - transform.GetChild(1).position;
                }

                snapVec = trackpoint - snap_point.position;
                playerscript.SnappedToTrack(other.GetComponent<BezierTrack>(), t, snap_point);
                playerscript.transform.position = playerscript.transform.position + snapVec;

                // if (snapVec.magnitude < 1.0f)
                // {
                //     playerscript.SnappedToTrack(other.GetComponent<BezierTrack>(), t);
                // }
                // else snapVec *= 2 * Time.deltaTime;
            }
            else if (playerscript.snapped)
            {

            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (playerscript.snapping || playerscript.snapped)
        {
            if (other.tag == "Track")
            {
                playerscript.LeaveTrack(other.GetComponent<BezierTrack>());
            }
        }
    }

    private IEnumerator SnapToQuaternion(Quaternion original, Quaternion target)
    {
        float elapsed = 0.0f;
        while (elapsed <= snapduration)
        {
            elapsed += Time.deltaTime;
            karttrans.localRotation = Quaternion.Slerp(original, target, elapsed / snapduration);
            yield return null;
        }
    }

    public void SnapColdDown()
    {
        snapColdDown = 3.0f;
    }
}
