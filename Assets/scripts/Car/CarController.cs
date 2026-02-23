using UnityEngine;
using System;
using Unity.Cinemachine;

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    [SerializeField] private EngineSimulator engine;
    [SerializeField] private WheelData[] wheels;
    private int numberOfWheels;

    [Header("settings")]
    [SerializeField] private float maxSteerAngle = 30f;
    [SerializeField] private float brakeForce = 3000f;

    [Header("Traction Control")]
    [SerializeField] bool tcsEnabled = true;
    [Range(0, 1)][SerializeField] float slipLimit = 0.35f;
    private float tcsTorqueFactor = 1f;//1 = full power 0 = no power
    [SerializeField] private float maxSlipLimit = 0f;

    [Header("surface settings")]
    [SerializeField] float rockStiffness = 1.0f;
    [SerializeField] float dirtStiffness = 0.7f;
    [SerializeField] float mudStiffness = 0.3f;

    private Rigidbody rb;
    [HideInInspector] public float gasInput;
    [HideInInspector] public float brakeInput;
    [HideInInspector] public float steerInput;

    //hp 200 - 400

    [Serializable]
    public struct WheelData
    {
        public WheelCollider collider;
        public Transform visual;
        public bool isSteerable;
        public bool isMotorized;
        public bool isFrontWheel;
    }

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
        float wheelRPM = GetAverageMotorRPM();
        float torque = -engine.CalculateTorque(gasInput, wheelRPM);

        if (gasInput == 0 && rb.linearVelocity.z < 0) torque = engine.ApplyEngineBreaking();
        if (tcsEnabled) ApplyTractionControl();

        foreach (var wheel in wheels)
        {
            UpdateWheelFriction(wheel.collider);

            if (wheel.isSteerable)
                wheel.collider.steerAngle = steerInput * maxSteerAngle;

            if (wheel.isMotorized)
            {
                wheel.collider.motorTorque = torque / numberOfWheels;
                Debug.Log(torque /numberOfWheels);
            }

            wheel.collider.brakeTorque = brakeInput * brakeForce;
            wheel.collider.forceAppPointDistance = 0.1f;
        }
    }

    private void Update()
    {
        foreach (var wheel in wheels)
            UpdateWheelVisual(wheel);
    }

    private void UpdateWheelVisual(WheelData wheel)
    {
        Vector3 pos;
        Quaternion rot;
        wheel.collider.GetWorldPose(out pos, out rot);
        wheel.visual.position = pos;
        wheel.visual.rotation = rot;
    }

    private float GetAverageMotorRPM()
    {
        float totalRPM = 0;
        int count = 0;
        foreach (var wheel in wheels)
        {
            if (wheel.isMotorized)
            {
                totalRPM += wheel.collider.rpm;
                count++;
            }
        }
        return count > 0 ? totalRPM / count : 0;
    }

    private void UpdateWheelFriction(WheelCollider wheel)
    {
        WheelHit hit;
        if (wheel.GetGroundHit(out hit))
        {
            string materialName = hit.collider.sharedMaterial != null ? hit.collider.sharedMaterial.name : "";

            float targetStiffness = 1.0f;

            if (materialName.Contains("Rock")) targetStiffness = rockStiffness;
            else if (materialName.Contains("Dirt")) targetStiffness = dirtStiffness;
            else if (materialName.Contains("Mud")) targetStiffness = mudStiffness;

            WheelFrictionCurve fFriction = wheel.forwardFriction;
            fFriction.stiffness = targetStiffness;
            wheel.forwardFriction = fFriction;

            WheelFrictionCurve sFriction = wheel.sidewaysFriction;
            sFriction.stiffness = targetStiffness;
            wheel.sidewaysFriction = sFriction;
        }
    }

    private void ApplyTractionControl()
    {
        foreach (var wheel in wheels)
        {
            if (wheel.isMotorized)
            {
                WheelHit hit;
                if (wheel.collider.GetGroundHit(out hit))
                {
                    float currentSlip = Mathf.Abs(hit.forwardSlip);
                    if (currentSlip > maxSlipLimit) maxSlipLimit = currentSlip;
                }
            }
        }

        if (maxSlipLimit > slipLimit)
            tcsTorqueFactor = Mathf.Lerp(tcsTorqueFactor, 1f - Mathf.Clamp01((maxSlipLimit - slipLimit) / 0.5f), Time.fixedDeltaTime * 10f);
        else
            tcsTorqueFactor = Mathf.Lerp(tcsTorqueFactor, 1f, Time.fixedDeltaTime * 2f);
    }

    private int GetMotorizedWheelCount()
    {
        int count = 0;
        foreach (var wheel in wheels)
        {
            if (wheel.isMotorized)
                count++;
        }
        return count;
    }
}