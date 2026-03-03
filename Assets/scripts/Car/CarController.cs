using UnityEngine;
using System;

public class CarController : MonoBehaviour
{
    [SerializeField] private EngineSimulator engine;
    [SerializeField] private WheelData[] wheels;
    private int numberOfWheels;

    [Header("settings")]
    [SerializeField] private float maxSteerAngle = 30f;
    [SerializeField] private float brakeForce = 3000f;

    [Header("traction control")]
    [SerializeField] bool tcsEnabled = true;
    [Range(0, 1)][SerializeField] float slipLimit = 0.35f;
    private float tcsTorqueFactor = 1f;
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

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        InputManager.Instance.OnAccelerateEvent += (val) => gasInput = val;
        InputManager.Instance.OnBrakeEvent += (val) => brakeInput = val;
        InputManager.Instance.OnSteeringEvent += (val) => steerInput = val;
        InputManager.Instance.OnGearUpEvent += engine.ShiftUp;
        InputManager.Instance.OnGearDownEvent += engine.ShiftDown;
        numberOfWheels = GetMotorizedWheelCount();
    }

    private void FixedUpdate()
    {
        float driveWheelRPM = GetAverageWheelRPM();
        float speedMS = rb.linearVelocity.magnitude;
        float totalOutputTorque = engine.UpdateEngine(gasInput, brakeInput, driveWheelRPM, speedMS);

        if (tcsEnabled) ApplyTractionControl();

        int activeWheels = 0;
        foreach (var w in wheels)
            if (IsWheelInDrivetrain(w)) activeWheels++;

        float torquePerWheel = activeWheels > 0
            ? (totalOutputTorque / activeWheels) * tcsTorqueFactor
            : 0f;

        foreach (var wheel in wheels)
        {
            if (wheel.isSteerable)
                wheel.collider.steerAngle = steerInput * maxSteerAngle;

            //negative needed since wheel colliders are backwards in game spcae
            wheel.collider.motorTorque = IsWheelInDrivetrain(wheel) ? -torquePerWheel : 0f;

            wheel.collider.brakeTorque = brakeInput * brakeForce;

            UpdateWheelFriction(wheel.collider);
        }

        ApplyDrag(speedMS);
    }

    private void ApplyDrag(float speedMS)
    {
        float drag = aeroDragCoeff * speedMS * speedMS;

        bool engineConnected = !engine.isStalled && engine.currentGear != -1;
        if (!engineConnected)
            drag += rollingResistanceCoeff * rb.mass * 9.81f;

        if (speedMS > 0.1f)
            rb.AddForce(-rb.linearVelocity.normalized * drag, ForceMode.Force);
    }

    private void Update()
    {
        foreach (var wheel in wheels)
        {
            wheel.collider.GetWorldPose(out Vector3 pos, out Quaternion rot);
            wheel.visual.position = pos;
            wheel.visual.rotation = rot;
        }
    }

    public float GetAverageWheelRPM()
    {
        float total = 0; int count = 0;
        foreach (var w in wheels)
        {
            if (!IsWheelInDrivetrain(w)) continue;
            total += w.collider.rpm;
            count++;
        }
        return count > 0 ? total / count : 0f;
    }

    private void UpdateWheelFriction(WheelCollider wheel)
    {
        WheelHit hit;
        if (!wheel.GetGroundHit(out hit)) return;

        string mat = hit.collider.sharedMaterial != null ? hit.collider.sharedMaterial.name : "";

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
        maxSlipDetected = 0f;
        foreach (var wheel in wheels)
        {
            if (!wheel.isMotorized) continue;
            WheelHit hit;
            if (!wheel.collider.GetGroundHit(out hit)) continue;
            float slip = Mathf.Abs(hit.forwardSlip);
            if (slip > maxSlipDetected) maxSlipDetected = slip;
        }

        tcsTorqueFactor = maxSlipDetected > slipLimit
            ? Mathf.Lerp(tcsTorqueFactor, 1f - Mathf.Clamp01((maxSlipDetected - slipLimit) / 0.5f), Time.fixedDeltaTime * 10f)
            : Mathf.Lerp(tcsTorqueFactor, 1f, Time.fixedDeltaTime * 2f);
    }

    private int GetMotorizedWheelCount()
    {
        int count = 0;
        foreach (var wheel in wheels)
            if (wheel.isMotorized) count++;
        return count;
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

    public enum DrivetrainType
    {
        RearWheelDrive,
        FrontWheelDrive,
        AllWheelDrive
    }
}