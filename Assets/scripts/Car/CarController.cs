using UnityEngine;
using System;

public class CarController : MonoBehaviour
{
    [SerializeField] private EngineSimulator engine;
    [SerializeField] private WheelData[] wheels;

    [Header("settings")]
    [SerializeField] private float maxSteerAngle = 30f;
    [SerializeField] private float brakeForce = 3000f;

    [Header("traction control")]
    [SerializeField] bool tcsEnabled = true;
    [Range(0, 1)][SerializeField] float slipLimit = 0.35f;
    [HideInInspector] public float tcsTorqueFactor = 1f;
    private float maxSlipDetected = 0f;
    [SerializeField] private float wheelInertiaFactor = 0.6f;

    [Header("surface settings")]
    [SerializeField] private float rockStiffness = 1.0f;
    [SerializeField] private float dirtStiffness = 0.7f;
    [SerializeField] private float mudStiffness = 0.3f;

    [Header("drag")]
    [SerializeField] private float aeroDragCoeff = 0.45f;
    [SerializeField] private float rollingResistanceCoeff = 0.015f;

    private Rigidbody rb;
    [HideInInspector] public float gasInput;
    [HideInInspector] public float brakeInput;
    [HideInInspector] public float steerInput;

    private int activeWheels = 0;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        //input event susbscriptions - lamdas
        InputManager.Instance.OnAccelerateEvent += (val) => gasInput = val;
        InputManager.Instance.OnBrakeEvent += (val) => brakeInput = val;
        InputManager.Instance.OnSteeringEvent += (val) => steerInput = val;
        InputManager.Instance.OnGearUpEvent += engine.ShiftUp;
        InputManager.Instance.OnGearDownEvent += engine.ShiftDown;

        //fixed - removed local 'int' declaration that was shadowing the class field, keeping activeWheels at 0 forever
        activeWheels = 0;
        foreach (var w in wheels)
            if (IsWheelInDrivetrain(w)) activeWheels++;
    }

    private void FixedUpdate()
    {
        //engine torque based on input and current speed
        float speedMS = Vector3.Dot(rb.linearVelocity, transform.forward);

        //sample slope from wheel ground normals and pass to engine so it can account for grade load
        engine.groundSlopeAngle = 0f;
        foreach (var w in wheels)
        {
            WheelHit hit;
            if (w.collider.GetGroundHit(out hit))
            {
                engine.groundSlopeAngle = Mathf.Max(engine.groundSlopeAngle, Vector3.Angle(hit.normal, Vector3.up));
                break;
            }
        }

        float totalOutputTorque = engine.UpdateEngine(gasInput, speedMS);

        //traction control
        if (tcsEnabled) ApplyTractionControl();

        //distribute torque to wheels
        float torquePerWheel = activeWheels > 0 ? (totalOutputTorque / activeWheels) * tcsTorqueFactor : 0f;

        //apply steering torque and braking to each wheel
        foreach (var wheel in wheels)
        {
            if (wheel.isSteerable)
                wheel.collider.steerAngle = steerInput * maxSteerAngle;

            //negative needed since wheel colliders are backwards in game spcae
            wheel.collider.motorTorque = IsWheelInDrivetrain(wheel) ? -torquePerWheel : 0f;
            wheel.collider.brakeTorque = brakeInput * brakeForce;

            //update friction curve based on surface
            UpdateWheelFriction(wheel.collider);
        }

        //magnitude for drag since it always opposes velocity regardless of direction
        ApplyDrag(rb.linearVelocity.magnitude);
    }


    private void ApplyDrag(float speedMS)
    {
        float drag = aeroDragCoeff * speedMS * speedMS;

        //rolling resistance when engine not giveing torque to wheels
        bool engineConnected = !engine.isStalled && engine.currentGear != -1;
        if (!engineConnected)
            drag += rollingResistanceCoeff * rb.mass * 9.81f;

        //apply force - so much simplilar then fully simulating drag - might re visit and add accutrate modeling===========
        if (speedMS > 0.1f)
            rb.AddForce(-rb.linearVelocity.normalized * drag, ForceMode.Force);
    }

    private void Update()
    {
        //updating the wheel visuals
        foreach (var wheel in wheels)
        {
            wheel.collider.GetWorldPose(out Vector3 pos, out Quaternion rot);
            wheel.visual.position = pos;
            wheel.visual.rotation = rot;
        }
    }

    private void UpdateWheelFriction(WheelCollider wheel)
    {
        WheelHit hit;
        if (!wheel.GetGroundHit(out hit)) return;

        //get current material name
        string mat = hit.collider.sharedMaterial != null ? hit.collider.sharedMaterial.name : "";

        //adjust settings based on physics material
        float stiffness = 1.0f;
        if (mat.Contains("Rock")) stiffness = rockStiffness;
        else if (mat.Contains("Dirt")) stiffness = dirtStiffness;
        else if (mat.Contains("Mud")) stiffness = mudStiffness;

        WheelFrictionCurve fwd = wheel.forwardFriction;
        fwd.stiffness = stiffness;
        wheel.forwardFriction = fwd;

        WheelFrictionCurve side = wheel.sidewaysFriction;
        side.stiffness = stiffness;
        wheel.sidewaysFriction = side;
    }

    private void ApplyTractionControl()
    {
        //find the wheel with the most slip
        maxSlipDetected = 0f;
        foreach (var wheel in wheels)
        {
            if (!wheel.isMotorized) continue;
            WheelHit hit;
            if (!wheel.collider.GetGroundHit(out hit)) continue;

            //normalize slip 0-1 ratio
            float wheelSpeedMS = wheel.collider.radius * wheel.collider.rpm * Mathf.Deg2Rad * 6f;
            float normalizedSlip = wheelSpeedMS > 0.5f ? Mathf.Abs(hit.forwardSlip) / wheelSpeedMS : 0f;

            if (normalizedSlip > maxSlipDetected) maxSlipDetected = normalizedSlip;
        }

        //limit torque if is over slip limit
        tcsTorqueFactor = maxSlipDetected > slipLimit
            ? Mathf.Lerp(tcsTorqueFactor, 1f - Mathf.Clamp01((maxSlipDetected - slipLimit) / 0.5f), Time.fixedDeltaTime * 10f)
            : Mathf.Lerp(tcsTorqueFactor, 1f, Time.fixedDeltaTime * 2f);
    }

    private bool IsWheelInDrivetrain(WheelData wheel)
    {
        if (!wheel.isMotorized) return false;
        return (DrivetrainType)engine.drivetrainMode switch
        {
            DrivetrainType.AllWheelDrive => true,
            DrivetrainType.RearWheelDrive => !wheel.isFrontWheel,
            DrivetrainType.FrontWheelDrive => wheel.isFrontWheel,
            _ => false
        };
    }

    [Serializable]
    public struct WheelData
    {
        public WheelCollider collider;
        public Transform visual;
        public bool isSteerable;
        public bool isMotorized;
        public bool isFrontWheel;
    }
}