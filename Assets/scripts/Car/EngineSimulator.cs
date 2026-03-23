using UnityEngine;

public enum DrivetrainType { RearWheelDrive, FrontWheelDrive, AllWheelDrive }

public class EngineSimulator : MonoBehaviour
{
    [Header("engine")]
    [SerializeField] private float maxTorque = 450f;
    [SerializeField] private float redlineRPM = 7000f;
    [SerializeField] private float idleRPM = 800f;
    [SerializeField] private float stallRPM = 400f;
    [SerializeField] private float idleReturnSpeed = 5f;
    [SerializeField] private float engineInertia = 3f;
    [SerializeField] private float frictionCoeff = 0.02f;
    [SerializeField] private float engineBrakingCoeff = 0.1f;

    [Header("gearbox")]
    public DrivetrainType drivetrainMode = DrivetrainType.AllWheelDrive;
    [SerializeField] private float[] gearRatios = { 4.696f, 2.985f, 2.146f, 1.769f, 1.520f, 1.275f, 1.000f, 0.854f, 0.689f, 0.636f };
    [SerializeField] private float reverseRatio = 4.866f;
    [SerializeField] private float finalDriveRatio = 4.10f;
    [SerializeField] private float transmissionEfficiency = 0.97f;
    [SerializeField] private float rpmSyncThreshold = 300f;

    [Header("drivetrain load")]
    [SerializeField] private float vehicleMass = 2000f;
    [SerializeField] private float wheelRadius = 0.38f;
    [SerializeField] private float dragCoeff = 0.45f;
    [SerializeField] private float rollingResistCoeff = 0.015f;

    [Header("clutch")]
    [SerializeField] private float clutchTorqueCapacity = 700f;
    [SerializeField] private float clutchEngageSpeed = 4f;

    [HideInInspector] public int currentGear = 0;
    [HideInInspector] public float currentRPM;
    [HideInInspector] public float drivetrainRPM;
    [HideInInspector] public bool isStalled = false;

    private const int NeutralGear = -1;
    private const int ReverseGear = -2;

    [Range(0f, 1f)] private float clutchEngagement = 0f;
    private float clutchInput = 1f;
    private float shiftTimer = 0f;
    private AnimationCurve torqueCurve;

    [HideInInspector] public float outputWheelTorque = 0f;

    [HideInInspector] public float groundSlopeAngle = 0f;

    void Awake()
    {
        torqueCurve = new AnimationCurve(
            new Keyframe(0.00f, 0.30f, 0f, 2.0f),
            new Keyframe(0.15f, 0.70f, 2.0f, 1.2f),
            new Keyframe(0.40f, 0.95f, 0.8f, 0.4f),
            new Keyframe(0.65f, 1.00f, 0.1f, -0.3f),
            new Keyframe(0.80f, 0.92f, -0.6f, -0.8f),
            new Keyframe(1.00f, 0.65f, -1.2f, 0f)
        );
    }

    void Start() => currentRPM = idleRPM;

    public float UpdateEngine(float gasInput, float wheelRPM)
    {
        float dt = Time.fixedDeltaTime;

        //stall recovery - must be in neutral and give gas to restart
        if (isStalled)
        {
            if (currentGear == NeutralGear && gasInput > 0.1f)
            {
                isStalled = false;
                currentRPM = idleRPM;
            }
            else return 0f;
        }

        //powerloss during shift
        shiftTimer -= dt;
        clutchInput = shiftTimer > 0f ? 0f : 1f;

        //gearing
        float gearRatio = GetCurrentGearRatio();
        float totalRatio = gearRatio * finalDriveRatio;

        //clutch engagement
        float targetClutch = currentGear == NeutralGear ? 0f : clutchInput;
        clutchEngagement = Mathf.Lerp(clutchEngagement, targetClutch, dt * clutchEngageSpeed);

        //engine torque from curve and throttle
        float rpmPercent = Mathf.InverseLerp(idleRPM, redlineRPM, currentRPM);
        float curveSample = torqueCurve.Evaluate(rpmPercent);
        bool revLimiter = currentRPM >= redlineRPM * 0.98f;
        float engineTorque = revLimiter ? 0f : curveSample * maxTorque * gasInput;

        //idle creep + hill hold - keeps engine from immediately dying against gravity before momentum builds
        if (currentGear != NeutralGear && currentGear != ReverseGear && !isStalled)
        {
            float idleCreep = Mathf.Lerp(maxTorque * 0.08f, 0f, rpmPercent * 3f);

            //back-calculate torque needed at engine to offset grade load at wheels
            float hillHoldTorque = vehicleMass * 9.81f *
                Mathf.Sin(groundSlopeAngle * Mathf.Deg2Rad) *
                wheelRadius / Mathf.Max(totalRatio, 0.01f);
            //clamp so it cant fully self drive up a hill - tune 0.6f up toward 1.0 for easier hills
            hillHoldTorque = Mathf.Clamp(hillHoldTorque * 0.6f, 0f, maxTorque * 0.4f);

            engineTorque += idleCreep + hillHoldTorque;
        }

        //torque losses
        float frictionTorque = currentRPM * frictionCoeff;

        //apply engine braking through drivetrain
        float engineBrakingTorque = clutchEngagement > 0.1f ? currentRPM * engineBrakingCoeff * (1f - gasInput) : 0f;

        drivetrainRPM = wheelRPM * totalRatio;

        //clutch lock - true when engagement is high and engine/drivetrain RPMs are close
        float rpmDelta = currentRPM - drivetrainRPM;
        bool clutchLocked = clutchEngagement >= 0.95f
            && Mathf.Abs(rpmDelta) < rpmSyncThreshold
            && currentGear != NeutralGear;

        float outputTorque;

        if (clutchLocked)//clutch engaged - engine and wheels move together
        {
            //lock engine RPM to drivetrain, clamped to idle so torque doesnt collapse at standstill
            currentRPM = Mathf.Clamp(drivetrainRPM, idleRPM, redlineRPM);

            //road load - aero drag and rolling resistance
            float speedSq = (wheelRPM * 2f * Mathf.PI * wheelRadius / 60f);
            speedSq *= speedSq;
            float roadForce = (dragCoeff * speedSq) + (rollingResistCoeff * vehicleMass * 9.81f);

            //fixed - add gravity component from slope so engine actually fights the hill
            float gradeForce = vehicleMass * 9.81f * Mathf.Sin(groundSlopeAngle * Mathf.Deg2Rad);
            roadForce += gradeForce;

            //calculate load seen at engine shaft through gearing
            float loadAtEngine = (roadForce * wheelRadius) / Mathf.Max(totalRatio, 0.01f);

            //inertia at engine accounting for drivetrain gearing and wheel inertia
            float effectiveInertia = vehicleMass * Mathf.Pow(wheelRadius / Mathf.Max(totalRatio, 0.01f), 2f);
            float totalInertia = engineInertia + effectiveInertia;

            //net torque at engine and update rpm
            float netTorque = engineTorque - frictionTorque - engineBrakingTorque - loadAtEngine;
            float rpmAccel = (netTorque / totalInertia) * (60f / (2f * Mathf.PI));
            
            currentRPM += rpmAccel * dt;
            currentRPM = Mathf.Clamp(currentRPM, idleRPM, redlineRPM);

            outputTorque = netTorque;

            //stall check - only stall if rpm falls below threshold under negative torque
            if (currentRPM <= stallRPM && netTorque < 0f)
            {
                isStalled = true;
                currentRPM = 0f;
                return 0f;
            }
        }
        else//clutch slipping or neutral - engine spins freely with no drivetrain load
        {
            //simulate engine reving with losses only no road load since clutch isnt engaged
            float netFreeRev = engineTorque - frictionTorque - engineBrakingTorque;
            float rpmAccel = (netFreeRev / engineInertia) * (60f / (2f * Mathf.PI));
            currentRPM += rpmAccel * dt;

            //idle return when no throttle in neutral
            if (gasInput < 0.05f && currentGear == NeutralGear)
                currentRPM = Mathf.Lerp(currentRPM, idleRPM, dt * idleReturnSpeed);

            currentRPM = Mathf.Clamp(currentRPM, stallRPM, redlineRPM);

            //output proportional to clutch engagement - slipping clutch transfers partial torque
            float slipTorque = engineTorque * clutchEngagement * transmissionEfficiency;
            outputTorque = Mathf.Clamp(slipTorque, -clutchTorqueCapacity, clutchTorqueCapacity);
        }

        //wheel torque after gearbox and transmission losses
        float wheelTorque = outputTorque * totalRatio * transmissionEfficiency;
        float clutchMaxTorque = clutchTorqueCapacity * clutchEngagement;

        //reverse - ensure torque always acts rearward
        if (currentGear == ReverseGear)
            wheelTorque = Mathf.Max(0f, wheelTorque);

        //return torque to wheels limited by clutch capacity when slipping
        return clutchLocked ? wheelTorque : Mathf.Clamp(wheelTorque, -clutchMaxTorque, clutchMaxTorque);
    }

    private float GetCurrentGearRatio()
    {
        if (currentGear == NeutralGear) return 0f;
        if (currentGear == ReverseGear) return reverseRatio;
        return gearRatios[Mathf.Clamp(currentGear, 0, gearRatios.Length - 1)];
    }

    public void ShiftUp()
    {
        if (currentGear < gearRatios.Length - 1) { currentGear++; shiftTimer = 0.2f; }
    }

    public void ShiftDown()
    {
        if (currentGear > ReverseGear) { currentGear--; shiftTimer = 0.2f; }
    }
}