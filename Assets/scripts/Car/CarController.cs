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
    [SerializeField] float slipLimit = 3.0f;
    [SerializeField] float tcsMinSpeed = 1.5f;
    [HideInInspector] public float tcsTorqueFactor = 1f;
    private float maxSlipDetected = 0f;

    [Header("surface settings")]
    [SerializeField] private float rockStiffness = 1.0f;
    [SerializeField] private float dirtStiffness = 0.7f;
    [SerializeField] private float mudStiffness = 0.3f;

    [Header("launch friction")]
    [SerializeField] private float launchExtremumSlip = 0.4f;//slip point where peak grip is - lower = earlier bite
    [SerializeField] private float launchExtremumValue = 2.5f;//peak grip multiplier at launch - higher = more bite
    [SerializeField] private float launchAsymptoteSlip = 0.8f;//where grip settles post peak
    [SerializeField] private float launchAsymptoteValue = 1.8f;//settled grip level post peak
    [SerializeField] private float launchBlendSpeed = 5f;//were normal friction is used

    [Header("drag")]
    [SerializeField] private float aeroDragCoeff = 0.45f;
    [SerializeField] private float rollingResistanceCoeff = 0.015f;
    [SerializeField] private float engineBrakingForce = 800f;

    private Rigidbody rb;
    [HideInInspector] public float gasInput;
    [HideInInspector] public float brakeInput;
    [HideInInspector] public float steerInput;

    private int activeWheels = 0;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        InputManager.Instance.OnAccelerateEvent += (val) => gasInput = val;
        InputManager.Instance.OnBrakeEvent += (val) => brakeInput = val;
        InputManager.Instance.OnSteeringEvent += (val) => steerInput = val;
        InputManager.Instance.OnGearUpEvent += engine.ShiftUp;
        InputManager.Instance.OnGearDownEvent += engine.ShiftDown;

        activeWheels = 0;
        foreach (var w in wheels)
            if (IsWheelInDrivetrain(w)) activeWheels++;
    }

    private void FixedUpdate()
    {
        float speedMS = Vector3.Dot(rb.linearVelocity, transform.forward);

        //sample slope from wheel ground normals and pass to engine so it can account for grade load
        engine.groundSlopeAngle = 0f;
        foreach (var w in wheels)
        {
            WheelHit hit;
            if (w.collider.GetGroundHit(out hit))
            {
                engine.groundSlopeAngle = Mathf.Max(engine.groundSlopeAngle,
                    Vector3.Angle(hit.normal, Vector3.up));
                break;
            }
        }

        //engine updating
        float avgDrivenWheelRPM = GetAverageDrivenWheelRPM();
        float totalOutputTorque = engine.UpdateEngine(gasInput, avgDrivenWheelRPM);

        //traction control
        if (tcsEnabled) ApplyTractionControl(speedMS);

        //distribute torque to drive wheels
        float torquePerWheel = activeWheels > 0 ? (totalOutputTorque / activeWheels) * tcsTorqueFactor : 0f;

        //apply steering torque and braking to each wheel
        foreach (var wheel in wheels)
        {
            if (wheel.isSteerable)
                wheel.collider.steerAngle = steerInput * maxSteerAngle;

            //negative bc wheel colliders are backwards in game space
            wheel.collider.motorTorque = IsWheelInDrivetrain(wheel) ? -torquePerWheel : 0f;
            wheel.collider.brakeTorque = brakeInput * brakeForce;

            //update friction based on surface and current speed
            UpdateWheelFriction(wheel.collider, speedMS);
        }

        //drag
        //ApplyDrag(speedMS);
    }

    private float GetAverageDrivenWheelRPM()
    {
        //average RPM across all driven wheels - gives engine a realistic load signal
        //using avg rather than max prevents a single spinning wheel from inflating the reading
        float total = 0f;
        int count = 0;
        foreach (var w in wheels)
        {
            if (!IsWheelInDrivetrain(w)) continue;
            total += Mathf.Abs(w.collider.rpm);
            count++;
        }
        return count > 0 ? total / count : 0f;
    }

    private void ApplyTractionControl(float carSpeedMS)
    {
        //car build momentum before tsc can cut in
        if (carSpeedMS < tcsMinSpeed)
        {
            tcsTorqueFactor = 1f;
            return;
        }

        //find the wheel with the most slip
        maxSlipDetected = 0f;
        foreach (var wheel in wheels)
        {
            if (!wheel.isMotorized) continue;
            WheelHit hit;
            if (!wheel.collider.GetGroundHit(out hit)) continue;

            float slip = Mathf.Abs(hit.forwardSlip);
            if (slip > maxSlipDetected) maxSlipDetected = slip;
        }

        //limit torque if over slip limit
        if (maxSlipDetected > slipLimit)
        {
            //how far over the limit - scale output
            float overSlip = Mathf.Clamp01((maxSlipDetected - slipLimit) / slipLimit);
            float target = 1f - overSlip;
            tcsTorqueFactor = Mathf.Lerp(tcsTorqueFactor, target, Time.fixedDeltaTime * 8f);
        }
        else
            tcsTorqueFactor = Mathf.Lerp(tcsTorqueFactor, 1f, Time.fixedDeltaTime * 8f);

        tcsTorqueFactor = Mathf.Clamp01(tcsTorqueFactor);
    }

    private void UpdateWheelFriction(WheelCollider wheel, float carSpeedMS)
    {
        WheelHit hit;
        if (!wheel.GetGroundHit(out hit)) return;

        //get current material name
        string mat = hit.collider.sharedMaterial != null
            ? hit.collider.sharedMaterial.name : "";

        //adjust stiffness based on physics material
        float stiffness = 1.0f;
        if (mat.Contains("Rock")) stiffness = rockStiffness;
        else if (mat.Contains("Dirt")) stiffness = dirtStiffness;
        else if (mat.Contains("Mud")) stiffness = mudStiffness;

        //blend launch curve at low speed - 1.0 at standstill, 0.0 at launchBlendSpeed
        float launchBlend = 1f - Mathf.Clamp01(carSpeedMS / launchBlendSpeed);

        WheelFrictionCurve fwd = wheel.forwardFriction;
        fwd.stiffness = stiffness;

        //blend extremum and asymptote toward launch values when near standstill for better initial grip
        fwd.extremumSlip = Mathf.Lerp(fwd.extremumSlip, launchExtremumSlip, launchBlend);
        fwd.extremumValue = Mathf.Lerp(fwd.extremumValue, launchExtremumValue, launchBlend);
        fwd.asymptoteSlip = Mathf.Lerp(fwd.asymptoteSlip, launchAsymptoteSlip, launchBlend);
        fwd.asymptoteValue = Mathf.Lerp(fwd.asymptoteValue, launchAsymptoteValue, launchBlend);
        wheel.forwardFriction = fwd;

        //sideways stiffness set by surface
        WheelFrictionCurve side = wheel.sidewaysFriction;
        side.stiffness = stiffness;
        wheel.sidewaysFriction = side;
    }

    private void ApplyDrag(float speedMS)
    {
        //aero drag - exponential
        float drag = aeroDragCoeff * speedMS * speedMS;

        //only when moving
        if (Mathf.Abs(speedMS) > 1.0f)
            drag += rollingResistanceCoeff * rb.mass * 9.81f;

        //engine braking - in gear clutch engaged off throttle
        bool inGear = !engine.isStalled && engine.currentGear >= 0;
        if (inGear && gasInput < 0.05f && speedMS > 0.5f)
        {
            //scale with gears - lower gear = more engine braking
            float gearFactor = Mathf.Clamp01(engine.drivetrainRPM /
                Mathf.Max(engine.currentRPM, 1f));
            float engineBrake = engineBrakingForce * gearFactor * (1f - gasInput);

            //low speed idle creep - fade engine braking out so it doesnt hold car at rest
            float speedFade = Mathf.InverseLerp(2f, 4f, speedMS);
            drag += engineBrake * speedFade;
        }

        if (Mathf.Abs(speedMS) > 0.1f)
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

    public void ResetWheels()
    {
        for (int i = 0; i < wheels.Length; i++)
            wheels[i].collider.rotationSpeed = 0;
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