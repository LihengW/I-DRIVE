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

public class Player : MonoBehaviour
{
    [HideInInspector]
    public bool Boost = false;

    //movements
    public float boost_speed = 90;
    public float drift_speed;
    public float desiredMaxSpeed = 75;
    public float max_speed;
    [SerializeField] public float currentspeed = 0;
    [SerializeField]
    public float REALCURRENTSPEED;

    private Rigidbody rb;

    public float desired_rotate_strength = 25;
    private float rotateStrengthWithStar;

    [SerializeField] float rotate_strength;
    public Transform raycastPos;

    public bool grounded = false;

    //steer and direction of drift
    float direction;
    int drift_direction; //-1 is left and 1 is right

    bool drift_right = false;
    bool drift_left = false;

    //collision cooldown
    float collideCooldown = 0;

    //kart gameobjects
    public GameObject FrontLeftTire;
    public GameObject FrontRightTire;
    float max_tire_rotation = 20;
    public GameObject[] tires;
    public GameObject steeringwheel;

    // what boost will I get
    [SerializeField]
    float Drift_time = 0;
    [HideInInspector]
    public float Boost_time = 0;

    // Audio System
    private KartSoundManager soundManager;

    //before start boost
    private float beforeStartAccelTime;

    public GameObject Boost_PS;
    public GameObject BoostBurstPS;
    public GameObject DriftPS;

    public GameObject Right_Wheel_Drift_PS;
    public GameObject Left_Wheel_Drift_PS;

    public LayerMask mask;

    // jump tricks
    public bool trickAvailable = false;
    [HideInInspector]
    public bool trickBoostPending = false;
    public float trickJumpTime = 0.75f;
    private bool _inTrickJump = false;
    private float groundRayDist;
    public Transform trickParticles;

    public bool antiGravity = false;

    //jump panel
    [HideInInspector]
    public bool JUMP_PANEL = false;
    private float jumpPanelUpForce = -250000;
    private float jumpPanelDownForce = 0;

    // Tires
    public GameObject[] TireParents;
    private Vector3[] tireLocalPositions = new Vector3[4];

    // drift fx
    public bool drifting = false;
    //particle colors
    public Color drift1;
    public Color drift2;
    public Color drift3;
    public GameObject trails;

    // out of bounds
    public bool outOfBounds = false;
    public bool beingMoved = false;

    // Animation
    Animator PlayerAnimator;

    // jumpcharging
    [HideInInspector] private float jumpchargingTime;
    [HideInInspector] private bool charging;
    [HideInInspector] private float charging_jump_timer;
    
    public float TriggerChargingTime = 0.5f;
    public float jumpchargingMaxTime = 3.0f;
    public float chargeJumpFloatingTime = 0.75f;

    // snapping fx
    public bool snapping = false;
    public bool snapped = false;
    [SerializeField] private BezierTrack _snappedTrack;
    public SnapJumpBox snapJumpBoxScript;


    void Start()
    {
        rb = GetComponent<Rigidbody>();

        for (int i = 0; i < TireParents.Length; i++)
        {
            tireLocalPositions[i] = TireParents[i].transform.localPosition;
        }

        PlayerAnimator = GetComponent<Animator>();
    }

    private void Update()
    {

        if (!snapped && !snapping)
        {
            if (Input.GetKey(KeyCode.Space))
            {
                drifting = true;
            }
            else
            {
                drifting = false;
            }

            if (trickAvailable && Input.GetKeyDown(KeyCode.R)) //REALCURRENTSPEED > 40)
            {
                trickAvailable = false;
                trickBoostPending = true;
                StartCoroutine(trickJump());
            }

            ChargingJump(Time.deltaTime);
            Debug.Log(charging_jump_timer);

            Drift();
        }

        if (snapped)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                snapped = false;
                LeaveTrack(_snappedTrack);
                StartCoroutine(ChargeJump());
                StartCoroutine(ChargingJumpTransformation(false));
            }
            if (_snappedTrack != null)
            {
                _snappedTrack.FollowTrack();
            }
        }

        // Debug.Log("Velocity: " + rb.velocity);
        // Debug.Log("Angular Velocity: " + rb.angularVelocity);

    }

    private void FixedUpdate()
    {
        CheckGroud();

        if (!outOfBounds & !snapping & !snapped)
        {
            Move();
            Steer();
            movingCarParts();
        }

    }


    IEnumerator trickJump()
    {
        for (int i = 0; i < trickParticles.childCount; i++)
        {
            trickParticles.GetChild(i).GetComponent<ParticleSystem>().Play();
        }


        if (!JUMP_PANEL)
        {
            // float force = 6500;

            StartCoroutine(trickJumpRotate());
            float force = 12500;
            for (int i = 0; i < 30 && !JUMP_PANEL; i++)
            {
                rb.AddRelativeForce(Vector3.up * force * Time.deltaTime, ForceMode.Acceleration);
                if (force >= 600)
                    force -= 600;
                yield return new WaitForSeconds(0.01f);
            }

            // float timepassed = 0.0f;
            // for (int i = 0; i < (int)(trickJumpTime * 100); i++)
            // {
            //     timepassed += Time.deltaTime;
            //     transform.GetChild(0).localRotation = Quaternion.Lerp(transform.GetChild(0).localRotation, Quaternion.Euler(0, 360.0f, 0), timepassed / trickJumpTime);
            //     yield return new WaitForSeconds(0.01f);
            // }
        }

    }

    IEnumerator trickJumpRotate()
    {
        _inTrickJump = true;

        float jumprotationSpeed = 390.0f / trickJumpTime;  // Degrees per second
        float totalRotation = 0f;

        while (totalRotation < 390.0f)
        {
            float deltaRotation = jumprotationSpeed * Time.deltaTime;
            totalRotation += deltaRotation;

            // Rotate the kart child object
            transform.GetChild(0).localRotation *= Quaternion.Euler(0, deltaRotation, 0);
            yield return null;  // Continue on next frame
        }

        _inTrickJump = false;
    }

    IEnumerator ChargeJump()
    {
        for (int i = 0; i < trickParticles.childCount; i++)
        {
            trickParticles.GetChild(i).GetComponent<ParticleSystem>().Play();
        }
        // 1 : left ; 2 : right ; 0 : forward

        int direction = 0;

        if (Input.GetAxis("Horizontal") > 0.5f)
        {
            // right charge jump
            direction = 2;
        }
        else if (Input.GetAxis("Horizontal") < -0.5f)
        {
            direction = 1;
        }



        if (!JUMP_PANEL)
        {
            StartCoroutine(ChargeJumpRotate(direction));

            float force = 30000;
            for (int i = 0; i < 30 && !JUMP_PANEL; i++)
            {
                rb.AddRelativeForce(transform.up * force * Time.deltaTime, ForceMode.Acceleration);
                if (direction == 1)
                {
                    rb.AddRelativeForce(transform.right * -force / 3.0f * Time.deltaTime, ForceMode.Acceleration);
                }
                else if (direction == 2)
                {
                    rb.AddRelativeForce(transform.right * force / 3.0f * Time.deltaTime, ForceMode.Acceleration);
                }

                if (force >= 1000)
                    force -= 1000;

                if (snapping || snapped)
                {
                    break;
                }

                yield return new WaitForSeconds(0.01f);
            }

        }

    }

    IEnumerator ChargeJumpRotate(int direction)
    {
        // borrow the variable tmp
        _inTrickJump = true;
        float jumpspeed = 360.0f / chargeJumpFloatingTime;
        float realjumpspeed = 0.0f;
        if (direction == 1)
        {
            realjumpspeed = 360.0f / chargeJumpFloatingTime;
        }
        else if (direction == 2)
        {
            realjumpspeed = -360.0f / chargeJumpFloatingTime;
        }
        else if (direction == 0)
        {
            realjumpspeed = 360.0f / chargeJumpFloatingTime;
        }
        
        float totalRotation = 0f;

        while (totalRotation < 360.0f)
        {
            float deltaRotation = jumpspeed * Time.deltaTime;
            float deltaRotationReal = realjumpspeed * Time.deltaTime;
            totalRotation += deltaRotation;

            // Rotate the kart child object
            if (direction == 1 || direction == 2)
            {
                transform.GetChild(0).localRotation *= Quaternion.Euler(0, 0, deltaRotationReal);
            }
            else if (direction == 0)
            {
                transform.GetChild(0).localRotation *= Quaternion.Euler(deltaRotationReal, 0, 0);
            }
            else
            {
                transform.GetChild(0).localRotation *= Quaternion.Euler(0, deltaRotationReal, 0);
            }

            if (snapping || snapped)
            {
                break;
            }

            yield return null;  // Continue on next frame
        }

        _inTrickJump = false;

    }

    void Drift()
    {
        Ray ground = new Ray(raycastPos.position, -transform.up);
        RaycastHit hit;

        bool onGround = Physics.Raycast(ground, out hit, 1, mask) && (hit.normal.y > 0.5f || antiGravity);

        if (Input.GetKeyDown(KeyCode.Space) && !JUMP_PANEL && onGround)
        {
            // transform.GetChild(0).gameObject.GetComponent<Animator>().SetTrigger("Drift");
            // 
            // if (!transform.GetChild(0).GetChild(0).GetComponent<Animator>().GetCurrentAnimatorStateInfo(1).IsName("Shake"))
            // {
            //     transform.GetChild(0).GetChild(0).GetComponent<Animator>().SetTrigger("Shake");
            // }

            if (direction > 0)
            {
                drift_direction = 1;
            }
            if (direction < 0)
            {
                drift_direction = -1;
            }

            rotate_strength = 5;
        }

        if (Input.GetKey(KeyCode.Space) && grounded && currentspeed > 40 && Input.GetAxis("Horizontal") != 0 && !JUMP_PANEL)
        {
            rotate_strength = Mathf.Lerp(rotate_strength, desired_rotate_strength, 3 * Time.deltaTime);
            Drift_time += Time.deltaTime;

            if (drift_direction == -1)
            {
                drift_right = false;
                drift_left = true;
                // if (!soundManager.effectSounds[0].isPlaying)
                // {
                //     soundManager.effectSounds[0].PlayDelayed(0.25f); //drift sound steering
                //     if (isRainbowRoad)
                //     {
                //         if (!soundManager.effectSounds[26].isPlaying)
                //         {
                //             soundManager.effectSounds[26].PlayDelayed(0.25f); //drift on rainbow road
                //         }
                //     }
                // }
            }
            else if (drift_direction == 1)
            {
                drift_right = true;
                drift_left = false;
            }

            if (Drift_time >= 1.5 && Drift_time < 4)
            {
                // stage 1
                for (int i = 0; i < 5; i++)
                {
                    ParticleSystem DriftPS = Right_Wheel_Drift_PS.transform.GetChild(i).gameObject.GetComponent<ParticleSystem>(); //right wheel particles
                    ParticleSystem.MainModule PSMAIN = DriftPS.main;

                    ParticleSystem DriftPS2 = Left_Wheel_Drift_PS.transform.GetChild(i).gameObject.GetComponent<ParticleSystem>(); //left wheel particles
                    ParticleSystem.MainModule PSMAIN2 = DriftPS2.main;
                    PSMAIN.startColor = drift1;
                    PSMAIN2.startColor = drift1;

                    if (!DriftPS.isPlaying && !DriftPS2.isPlaying)
                    {
                        DriftPS.Play();
                        DriftPS2.Play();
                    }

                }
                // if (!playersounds.effectSounds[1].isPlaying)
                //     playersounds.effectSounds[1].Play();
            }

            if (Drift_time >= 4 && Drift_time < 7)
            {
                //drift color particles
                for (int i = 0; i < 5; i++)
                {
                    ParticleSystem DriftPS = Right_Wheel_Drift_PS.transform.GetChild(i).gameObject.GetComponent<ParticleSystem>();
                    ParticleSystem.MainModule PSMAIN = DriftPS.main;
                    ParticleSystem DriftPS2 = Left_Wheel_Drift_PS.transform.GetChild(i).gameObject.GetComponent<ParticleSystem>();
                    ParticleSystem.MainModule PSMAIN2 = DriftPS2.main;
                    PSMAIN.startColor = drift2;
                    PSMAIN2.startColor = drift2;


                }
            }

            if (Drift_time >= 7)
            {
                for (int i = 0; i < 5; i++)
                {

                    ParticleSystem DriftPS = Right_Wheel_Drift_PS.transform.GetChild(i).gameObject.GetComponent<ParticleSystem>();
                    ParticleSystem.MainModule PSMAIN = DriftPS.main;
                    ParticleSystem DriftPS2 = Left_Wheel_Drift_PS.transform.GetChild(i).gameObject.GetComponent<ParticleSystem>();
                    ParticleSystem.MainModule PSMAIN2 = DriftPS2.main;
                    PSMAIN.startColor = drift3;
                    PSMAIN2.startColor = drift3;

                }
            }
        }

        else if (currentspeed < 40)
        {
            drift_left = false;
            drift_right = false;
            Drift_time = 0;
        }
        if (!Input.GetKey(KeyCode.Space)) //if not drifting, or drifting without direction
        {
            drifting = false;
            drift_direction = 0;
            drift_left = false;
            drift_right = false;

            if (Drift_time > 1.5 && Drift_time < 4)
            {
                Boost = true;
                Boost_time = 0.75f;
            }
            if (Drift_time >= 4 && Drift_time < 7)
            {
                Boost = true;
                Boost_time = 1.5f;
            }
            if (Drift_time >= 7)
            {
                Boost = true;
                Boost_time = 2.5f;
            }

            //reset everything
            Drift_time = 0;
            //stop particles
            for (int i = 0; i < 5; i++)
            {
                ParticleSystem DriftPS = Right_Wheel_Drift_PS.transform.GetChild(i).gameObject.GetComponent<ParticleSystem>(); //right wheel particles
                ParticleSystem.MainModule PSMAIN = DriftPS.main;

                ParticleSystem DriftPS2 = Left_Wheel_Drift_PS.transform.GetChild(i).gameObject.GetComponent<ParticleSystem>(); //left wheel particles
                ParticleSystem.MainModule PSMAIN2 = DriftPS2.main;

                DriftPS.Stop();
                DriftPS2.Stop();

            }
        }
    }

    void player_animations()
    {
        if (!Input.GetKey(KeyCode.LeftArrow) && !Input.GetKey(KeyCode.RightArrow))
        {
            PlayerAnimator.SetBool("TurnLeft", false);
            PlayerAnimator.SetBool("TurnRight", false);
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            PlayerAnimator.SetBool("TurnLeft", false);
            PlayerAnimator.SetBool("TurnRight", true);
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            PlayerAnimator.SetBool("TurnLeft", true);
            PlayerAnimator.SetBool("TurnRight", false);
        }
    }

    void Move()
    {
        REALCURRENTSPEED = transform.InverseTransformDirection(rb.velocity).z;
        collideCooldown -= Time.deltaTime;

        if (!JUMP_PANEL)
        {
            if (antiGravity)
            {
                rb.AddRelativeForce(Vector3.down * 5000 * Time.deltaTime, ForceMode.Acceleration);
            }
            else
            {
                rb.AddForce(Vector3.down * 5000 * Time.deltaTime, ForceMode.Acceleration);
            }
        }

        //input speed into velocity
        Vector3 velocity = transform.forward * currentspeed;
        if (velocity.y > rb.velocity.y)
        {
            if (!antiGravity)
                velocity.y = rb.velocity.y;

        }

        rb.velocity = velocity;

        if (antiGravity)
        {
            rb.AddRelativeForce(Physics.gravity * 5, ForceMode.Acceleration);
        }

        if (Input.GetKey(KeyCode.W))
        {
            currentspeed = Mathf.Lerp(currentspeed, max_speed, 0.5f * Time.deltaTime);
            if (!drift_right && !drift_left)
                rotate_strength = desired_rotate_strength;
        }

        if (Input.GetKey(KeyCode.S))
        {
            currentspeed = Mathf.Lerp(currentspeed, -max_speed / 1.6f, 0.03f);
            if (REALCURRENTSPEED <= 0)
            {
                rotate_strength = 120;
            }
        }

        // slow down gradually
        if (!Input.GetKey(KeyCode.W))
        {
            currentspeed = Mathf.Lerp(currentspeed, 0, 0.01f);
            drift_right = false;
            drift_left = false;
            drift_direction = 0;
            if (!_inTrickJump)
            {
                transform.GetChild(0).localRotation = Quaternion.Lerp(transform.GetChild(0).localRotation, Quaternion.Euler(0, 0, 0), 0.4f);
            }
        }

        // Speed Limits on different states
        if (!grounded && !Boost)
        {
            max_speed = 30;
            if (Input.GetKey(KeyCode.W))
            {
                currentspeed = Mathf.Lerp(currentspeed, max_speed, 3 * Time.deltaTime);
            }
        }
        if (!grounded && Boost)
        {
            max_speed = boost_speed;
            currentspeed = boost_speed;
        }

        if (grounded && !Boost)
        {
            max_speed = desiredMaxSpeed;
        }

        if (grounded && Boost)
        {
            max_speed = boost_speed;
        }

        if (JUMP_PANEL)
        {
            rb.velocity = transform.forward * currentspeed;
            jumpPanelUpForce = Mathf.Lerp(jumpPanelUpForce, jumpPanelDownForce, 2.5f * Time.deltaTime);
            rb.AddRelativeForce(Vector3.down * jumpPanelUpForce * Time.deltaTime, ForceMode.Acceleration);
            rb.AddForce(transform.forward * 60000 * Time.deltaTime, ForceMode.Acceleration);
        }
    }

    void Steer()
    {
        float force = 30000;
        //steer
        if (Input.GetAxis("Horizontal") != 0)
        {
            if (drift_right && !drift_left)
            {
                direction = Input.GetAxis("Horizontal") > 0 ? 2.1f : 0.5f;
                transform.GetChild(0).localRotation = Quaternion.Lerp(transform.GetChild(0).localRotation, Quaternion.Euler(0, 20f, 0), 8f * Time.deltaTime);
                max_speed = desiredMaxSpeed - 10;

                //force
                if (drifting)
                {
                    rb.AddForce(-transform.right * force * Time.deltaTime, ForceMode.Acceleration);
                }
            }
            if (drift_left && !drift_right)
            {
                direction = Input.GetAxis("Horizontal") < 0 ? -2.1f : -0.5f;
                transform.GetChild(0).localRotation = Quaternion.Lerp(transform.GetChild(0).localRotation, Quaternion.Euler(0, -20f, 0), 8f * Time.deltaTime);
                max_speed = desiredMaxSpeed - 10;

                if (drifting)
                {
                    rb.AddForce(transform.right * force * Time.deltaTime, ForceMode.Acceleration);
                    Debug.Log($"drift force : {transform.right * force * Time.deltaTime}");
                }
            }

            // different rate of rotation in each speed level
            float speed_rotate_rate = 0;
            if (drift_left || drift_right)
                speed_rotate_rate = 1.2f;

            if (REALCURRENTSPEED > 10 && REALCURRENTSPEED < 40 && !drift_right && !drift_left)
                speed_rotate_rate = 1.3f;

            if (REALCURRENTSPEED < 10 && REALCURRENTSPEED > 3 && !drift_right && !drift_left)
            {
                speed_rotate_rate = 0.5f;
            }

            if (REALCURRENTSPEED >= 40 && !drift_right && !drift_left)
                speed_rotate_rate = 1.5f;

            if (REALCURRENTSPEED < -5 && !Input.GetKey(KeyCode.W)) //reverse
                speed_rotate_rate = -0.5f;

            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D))
                transform.Rotate(new Vector3(0, rotate_strength * direction * speed_rotate_rate * 0.025f, 0), Space.Self);

            // Debug.Log($"angle : {rotate_strength * direction * speed_rotate_rate * 0.025f}");

            if (!drift_right && !drift_left) //no drift
            {
                direction = Input.GetAxis("Horizontal") > 0 ? 1 : -1; //-1 = left, 1 = right
                if (!_inTrickJump)
                {
                    transform.GetChild(0).localRotation = Quaternion.Lerp(transform.GetChild(0).localRotation, Quaternion.Euler(0, 0, 0), 8f * Time.deltaTime);
                }
                max_speed = desiredMaxSpeed;
            }
        }

        if (!drift_right && !drift_left) //no drift
        {
            direction = Input.GetAxis("Horizontal") > 0 ? 1 : -1; //-1 = left, 1 = right
            if (!_inTrickJump)
            {
                transform.GetChild(0).localRotation = Quaternion.Lerp(transform.GetChild(0).localRotation, Quaternion.Euler(0, 0, 0), 8f * Time.deltaTime);
            }
            max_speed = desiredMaxSpeed;
        }
    }


    void CheckGroud()
    {
        grounded = false;

        Ray groudray = new Ray(raycastPos.position, -transform.up);
        RaycastHit hit;

        // bool ground = Physics.Raycast(groudray, out hit, 5, mask) && (hit.normal.y > 0.5f);
        bool ground = Physics.Raycast(groudray, out hit, 0.5f, mask);
        Debug.Log($"grounded : {ground}");

        if (ground)
        {
            grounded = true;

            if (trickAvailable)
            {
                trickAvailable = false;
            }

            if (antiGravity)
            {
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.FromToRotation(transform.up * 2, hit.normal) * transform.rotation, 1f * Time.deltaTime);
            }
            else
            {
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.FromToRotation(transform.up * 2, hit.normal) * transform.rotation, 7.5f * Time.deltaTime);
            }

            if (hit.collider.tag == "Dirt")
            {
                max_speed = 30;
            }

            if (hit.collider.tag == "Boost")
            {
                Boost_time = 2;
                Boost = true;
                max_speed = boost_speed;
            }
        }
    }

    void movingCarParts()
    {
        if (!antiGravity && !charging)
        {
            //tire gameObject steer and rotate and steeringwheel rotate
            float x = Input.GetAxis("Horizontal"); //direction 
            float rotate_speed = 4f;
            float rotation_limit = 20.0f;

            if (x >= 0.1f)
            {
                // Right turn
                Quaternion targetRotationRight = Quaternion.Euler(0, rotation_limit, 0);
                FrontRightTire.transform.localRotation = Quaternion.Lerp(FrontRightTire.transform.localRotation, targetRotationRight, rotate_speed * Time.deltaTime);
                FrontLeftTire.transform.localRotation = Quaternion.Lerp(FrontLeftTire.transform.localRotation, targetRotationRight, rotate_speed * Time.deltaTime);
                steeringwheel.transform.localRotation = Quaternion.Lerp(steeringwheel.transform.localRotation, targetRotationRight, rotate_speed * Time.deltaTime);
            }
            else if (x <= -0.1f)
            {
                // Left turn
                Quaternion targetRotationLeft = Quaternion.Euler(0, -rotation_limit, 0);
                FrontRightTire.transform.localRotation = Quaternion.Lerp(FrontRightTire.transform.localRotation, targetRotationLeft, rotate_speed * Time.deltaTime);
                FrontLeftTire.transform.localRotation = Quaternion.Lerp(FrontLeftTire.transform.localRotation, targetRotationLeft, rotate_speed * Time.deltaTime);
                steeringwheel.transform.localRotation = Quaternion.Lerp(steeringwheel.transform.localRotation, targetRotationLeft, rotate_speed * Time.deltaTime);
            }
            if (x == 0)
            {
                Quaternion targetRotationLeft = Quaternion.Euler(0, 0, 0);
                FrontRightTire.transform.localRotation = Quaternion.Lerp(FrontRightTire.transform.localRotation, targetRotationLeft, rotate_speed * Time.deltaTime);
                FrontLeftTire.transform.localRotation = Quaternion.Lerp(FrontLeftTire.transform.localRotation, targetRotationLeft, rotate_speed * Time.deltaTime);
                steeringwheel.transform.localRotation = Quaternion.Lerp(steeringwheel.transform.localRotation, targetRotationLeft, rotate_speed * Time.deltaTime);

            } //0
        }
        else
        {
            FrontRightTire.transform.localEulerAngles = Vector3.Lerp(FrontRightTire.transform.localEulerAngles, new Vector3(0, 0, 0), 10 * Time.deltaTime);
            FrontLeftTire.transform.localEulerAngles = Vector3.Lerp(FrontLeftTire.transform.localEulerAngles, new Vector3(0, 0, 0), 10 * Time.deltaTime);
        }

        //tire spinning movement
        for (int i = 0; i < 4; i++)
        {
            if (Input.GetKey(KeyCode.Space) && REALCURRENTSPEED < 0)
            {
        
                tires[0].transform.Rotate(-90 * Time.deltaTime * REALCURRENTSPEED * 0.5f, 0, 0);
                tires[1].transform.Rotate(-90 * Time.deltaTime * REALCURRENTSPEED * 0.5f, 0, 0);
                tires[2].transform.Rotate(-90 * Time.deltaTime * 5, 0, 0);
                tires[3].transform.Rotate(-90 * Time.deltaTime * 5, 0, 0);
            }
            else
            {
                if (currentspeed < 6.5 && currentspeed > -6.5)
                {
                    tires[i].transform.Rotate(-90 * Time.deltaTime * REALCURRENTSPEED * 0.5f, 0, 0);
                }
                else
                {
                    tires[i].transform.Rotate(-90 * Time.deltaTime * currentspeed / 4f, 0, 0);
                }
            }
        
        
        }
    }


    public IEnumerator EnterAntiGravity()
    {
        tires[0].GetComponent<MeshCollider>().enabled = false;
        tires[1].GetComponent<MeshCollider>().enabled = false;
        tires[2].GetComponent<MeshCollider>().enabled = false;
        tires[3].GetComponent<MeshCollider>().enabled = false;


        Quaternion r1 = tires[0].transform.localRotation;
        Quaternion t1 = r1 * Quaternion.Euler(0, 0, 90);
        Quaternion r2 = tires[1].transform.localRotation;
        Quaternion t2 = r2 * Quaternion.Euler(0, 0, -90);
        Quaternion r3 = tires[2].transform.localRotation;
        Quaternion t3 = r3 * Quaternion.Euler(0, 0, 90);
        Quaternion r4 = tires[3].transform.localRotation;
        Quaternion t4 = r4 * Quaternion.Euler(0, 0, -90);

        Quaternion z90 = Quaternion.Euler(0, 0, 90);
        Quaternion zminus90 = Quaternion.Euler(0, 0, -90);

        Vector3 centerfloating = new Vector3(0.0f, 0.5f, 0.0f);
        Vector3 originalpos = transform.GetChild(0).localPosition;

        float transitionTime = 1.0f;  // In seconds
        float elapsedTime = 0.0f;

        while (elapsedTime < transitionTime)
        {
            float lerpRatio = elapsedTime / transitionTime;
            tires[0].transform.localRotation = Quaternion.Slerp(r1, z90, lerpRatio);
            tires[1].transform.localRotation = Quaternion.Slerp(r2, zminus90, lerpRatio);
            tires[2].transform.localRotation = Quaternion.Slerp(r3, z90, lerpRatio);
            tires[3].transform.localRotation = Quaternion.Slerp(r4, zminus90, lerpRatio);

            transform.GetChild(0).localPosition = Vector3.Lerp(originalpos, centerfloating, lerpRatio);

            elapsedTime += Time.deltaTime;  // Accumulate time
            yield return null;  // Wait for the next frame
        }


        antiGravity = true;
    }

    public IEnumerator LeaveAntiGravity()
    {
        Quaternion r1 = tires[0].transform.rotation;
        Quaternion t1 = r1 * Quaternion.Euler(0, 0, -90);
        Quaternion r2 = tires[1].transform.rotation;
        Quaternion t2 = r2 * Quaternion.Euler(0, 0, -90);
        Quaternion r3 = tires[2].transform.rotation;
        Quaternion t3 = r3 * Quaternion.Euler(0, 0, -90);
        Quaternion r4 = tires[3].transform.rotation;
        Quaternion t4 = r4 * Quaternion.Euler(0, 0, -90);

        Vector3 centerstep = new Vector3(0.0f, +0.05f, 0.0f);

        for (int i = 0; i < 50; i++)
        {
            tires[0].transform.rotation = Quaternion.Slerp(r1, t1, i / 50.0f);
            tires[1].transform.rotation = Quaternion.Slerp(r2, t2, i / 50.0f);
            tires[2].transform.rotation = Quaternion.Slerp(r3, t3, i / 50.0f);
            tires[3].transform.rotation = Quaternion.Slerp(r4, t4, i / 50.0f);

            GetComponent<SphereCollider>().center += centerstep;

            yield return null;
        }

        antiGravity = false;
    }


    private void ChargingJump(float delta_time)
    {
        if (Input.GetKey(KeyCode.R))
        {
            if (!charging)
            {
                if (Input.GetAxis("Horizontal") <= 0.05f && grounded)
                {
                    charging_jump_timer += delta_time;
                    if (charging_jump_timer >= TriggerChargingTime)
                    {
                        StartCoroutine(ChargingJumpTransformation(true));
                        charging = true;
                    }
                }
            }
            else
            {
                // charging
                charging_jump_timer += delta_time;
                if (charging_jump_timer >= jumpchargingMaxTime)
                {
                    // play effects
                }
            }
        }
        else if (Input.GetKeyUp(KeyCode.R))
        {
            if (!charging)
            {
                charging_jump_timer = 0;
            }
            else
            {
                charging_jump_timer = 0;
                trickBoostPending = true;
                StartCoroutine(ChargeJump());
                StartCoroutine(ChargingJumpTransformation(false));
                charging = false;
            }
        }
    }


    private IEnumerator ChargingJumpTransformation(bool start)
    {
        if (start)
        {
            tires[0].GetComponent<MeshCollider>().enabled = false;
            tires[1].GetComponent<MeshCollider>().enabled = false;
            tires[2].GetComponent<MeshCollider>().enabled = false;
            tires[3].GetComponent<MeshCollider>().enabled = false;


            Quaternion r1 = tires[0].transform.localRotation;
            Quaternion t1 = r1 * Quaternion.Euler(0, 0, -30);
            Quaternion r2 = tires[1].transform.localRotation;
            Quaternion t2 = r2 * Quaternion.Euler(0, 0, 30);
            Quaternion r3 = tires[2].transform.localRotation;
            Quaternion t3 = r3 * Quaternion.Euler(0, 0, -30);
            Quaternion r4 = tires[3].transform.localRotation;
            Quaternion t4 = r4 * Quaternion.Euler(0, 0, 30);

            Quaternion z30 = Quaternion.Euler(0, 0, 30);
            Quaternion zminus30 = Quaternion.Euler(0, 0, -30);

            Vector3 centerfloating = new Vector3(0.0f, 0.5f, 0.0f);
            Vector3 originalpos = transform.GetChild(0).localPosition;

            float transitionTime = 1.0f;  // In seconds
            float elapsedTime = 0.0f;

            while (elapsedTime < transitionTime)
            {
                float lerpRatio = elapsedTime / transitionTime;
                tires[0].transform.localRotation = Quaternion.Slerp(r1, zminus30, lerpRatio);
                tires[1].transform.localRotation = Quaternion.Slerp(r2, z30, lerpRatio);
                tires[2].transform.localRotation = Quaternion.Slerp(r3, zminus30, lerpRatio);
                tires[3].transform.localRotation = Quaternion.Slerp(r4, z30, lerpRatio);

                transform.GetChild(0).localPosition = Vector3.Lerp(originalpos, centerfloating, lerpRatio);

                elapsedTime += Time.deltaTime;  // Accumulate time
                yield return null;  // Wait for the next frame
            }
        }
        else
        {
            Quaternion r1 = tires[0].transform.localRotation;
            Quaternion r2 = tires[1].transform.localRotation;
            Quaternion r3 = tires[2].transform.localRotation;
            Quaternion r4 = tires[3].transform.localRotation;

            Quaternion zero = Quaternion.Euler(0, 0, 0);

            Vector3 originalpos = transform.GetChild(0).localPosition;
            Vector3 centerground = new Vector3(0.0f, 0.08f, 0.0f);

            float transitionTime = 1.0f;  // In seconds
            float elapsedTime = 0.0f;

            while (elapsedTime < transitionTime)
            {
                float lerpRatio = elapsedTime / transitionTime;
                tires[0].transform.localRotation = Quaternion.Slerp(r1, zero, lerpRatio);
                tires[1].transform.localRotation = Quaternion.Slerp(r2, zero, lerpRatio);
                tires[2].transform.localRotation = Quaternion.Slerp(r3, zero, lerpRatio);
                tires[3].transform.localRotation = Quaternion.Slerp(r4, zero, lerpRatio);

                transform.GetChild(0).localPosition = Vector3.Lerp(originalpos, centerground, lerpRatio);

                elapsedTime += Time.deltaTime;  // Accumulate time
                yield return null;  // Wait for the next frame
            }

            tires[0].GetComponent<MeshCollider>().enabled = true;
            tires[1].GetComponent<MeshCollider>().enabled = true;
            tires[2].GetComponent<MeshCollider>().enabled = true;
            tires[3].GetComponent<MeshCollider>().enabled = true;
        }
            // tires transformation
    }

    // Track System
    public void Snapping()
    {
        var colliders = GetComponents<BoxCollider>();
        foreach (var collider in colliders)
        {
            collider.enabled = false;
        }

        GetComponent<Rigidbody>().useGravity = false;

        tires[0].GetComponent<MeshCollider>().enabled = false;
        tires[1].GetComponent<MeshCollider>().enabled = false;
        tires[2].GetComponent<MeshCollider>().enabled = false;
        tires[3].GetComponent<MeshCollider>().enabled = false;
    }

    public void SnappedToTrack(BezierTrack track, float t, Transform snapPoint)
    {
        snapped = true;
        snapping = false;
        _snappedTrack = track;
        _snappedTrack.StartTrackRun(this, t, snapPoint);
    }

    public void LeaveTrack(BezierTrack track)
    {
        snapped = false;
        snapping = false;
        _snappedTrack.EndTrackRun();
        _snappedTrack = null;

        var colliders = GetComponents<BoxCollider>();
        foreach (var collider in colliders)
        {
            collider.enabled = true;
        }

        tires[0].GetComponent<MeshCollider>().enabled = true;
        tires[1].GetComponent<MeshCollider>().enabled = true;
        tires[2].GetComponent<MeshCollider>().enabled = true;
        tires[3].GetComponent<MeshCollider>().enabled = true;

        snapJumpBoxScript.SnapColdDown();
    }

    public void OnCollisionEnter(Collision collision)
    {
        if (!snapping && !snapped)
        {
            foreach (var contact in collision.contacts)
            {
                if (contact.otherCollider.gameObject.layer == LayerMask.NameToLayer("Ground"))
                {
                    ;
                }
                else if (contact.otherCollider.CompareTag("Track"))
                {
                    ;
                }
                else
                {
                    if (currentspeed > 30.0f)
                    {
                        currentspeed -= 30.0f;
                    }
                    else
                    {
                        currentspeed = 5.0f;
                    }
                        // rb.AddForce(contact.normal.normalized * 50.0f);
                    rb.AddRelativeForce(-50.0f * transform.forward.normalized, ForceMode.Impulse);
                }
                
            }
            // play effects
        }
    }
}
