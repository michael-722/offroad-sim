using UnityEngine;

public enum DrivetrainType { RearWheelDrive, FrontWheelDrive, AllWheelDrive }

public class EngineSimulator : MonoBehaviour
{
    [Header("engine")]
    [SerializeField] private float maxTorque = 450f;
    [SerializeField] private float redlineRPM = 7000f;
    [SerializeField] private float idleRPM = 800f;
    [SerializeField] private float idleReturnSpeed = 2f;
    [SerializeField] private float engineInertia = 0.8f;
    [SerializeField] private float frictionCoeff = 0.004f;
    [SerializeField] private float engineBrakingCoeff = 0.12f;

    [Header("gearbox")]
    public DrivetrainType drivetrainMode = DrivetrainType.AllWheelDrive;
    [SerializeField] private float[] gearRatios = { 4.696f, 2.985f, 2.146f, 1.769f, 1.520f, 1.275f, 1.000f, 0.854f, 0.689f, 0.636f };
    [SerializeField] private float reverseRatio = 4.866f;
    [SerializeField] private float finalDriveRatio = 4.10f;
    [SerializeField] private float transmissionEfficiency = 0.97f;
    [SerializeField] private float rpmSyncThreshold = 300f;

    [Header("drivetrain load")]//need to automatic links 
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

    private const int NeutralGear = -1;
    private const int ReverseGear = -2;

    [Range(0f, 1f)] private float clutchEngagement = 0f;
    private float clutchInput = 1f;
    private float shiftTimer = 0f;
    private AnimationCurve torqueCurve;

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

    public float UpdateEngine(float gasInput, float breakInput, float wheelRPM, float speedMS)
    {
        float dt = Time.fixedDeltaTime;

        shiftTimer -= dt;
        clutchInput = shiftTimer > 0f ? 0f : 1f;

        float gearRatio = GetCurrentGearRatio();
        float totalRatio = gearRatio * finalDriveRatio;

        float targetClutch = currentGear == NeutralGear ? 0f : clutchInput;
        clutchEngagement = Mathf.Lerp(clutchEngagement, targetClutch, dt * clutchEngageSpeed);

        float rpmPercent = Mathf.InverseLerp(idleRPM, redlineRPM, currentRPM);
        float curveSample = torqueCurve.Evaluate(rpmPercent);
        float engineTorque = curveSample * maxTorque * gasInput;

        float frictionTorque = currentRPM * frictionCoeff;
        float engineBrakingTorque = currentRPM * engineBrakingCoeff * (1f - gasInput) * breakInput;

        float absWheelRPM = Mathf.Abs(wheelRPM);

        drivetrainRPM = absWheelRPM * totalRatio;

        float rpmDelta = currentRPM - drivetrainRPM;
        bool clutchLocked = clutchEngagement >= 0.95f
                            && Mathf.Abs(rpmDelta) < rpmSyncThreshold
                            && currentGear != NeutralGear;

        float outputTorque;

        if (clutchLocked)
        {
            currentRPM = Mathf.Clamp(drivetrainRPM, idleRPM * 0.5f, redlineRPM);

            if (currentRPM >= redlineRPM)
            {
                outputTorque = 0f;
            }
            else
            {
                float speedSq = speedMS * speedMS;
                float roadForce = (dragCoeff * speedSq) + (rollingResistCoeff * vehicleMass * 9.81f);
                float loadAtEngine = (roadForce * wheelRadius) / Mathf.Max(totalRatio, 0.01f);

                float effectiveInertia = vehicleMass * Mathf.Pow(wheelRadius / Mathf.Max(totalRatio, 0.01f), 2f);
                float totalInertia = engineInertia + effectiveInertia;

                float netTorque = engineTorque - frictionTorque - engineBrakingTorque - loadAtEngine;
                float rpmAccel = (netTorque / totalInertia) * (60f / (2f * Mathf.PI));
                currentRPM += rpmAccel * dt;
                currentRPM = Mathf.Clamp(currentRPM, idleRPM * 0.5f, redlineRPM);

                outputTorque = netTorque;
            }
        }
        else
        {
            float netTorque = engineTorque - frictionTorque - engineBrakingTorque;
            float rpmAccel = (netTorque / engineInertia) * (60f / (2f * Mathf.PI));
            currentRPM += rpmAccel * dt;

            if (gasInput < 0.05f && currentGear == NeutralGear)
                currentRPM = Mathf.Lerp(currentRPM, idleRPM, dt * idleReturnSpeed);

            currentRPM = Mathf.Clamp(currentRPM, idleRPM * 0.5f, redlineRPM);

            float slipTorque = rpmDelta * clutchEngagement * 0.5f;
            outputTorque = Mathf.Clamp(slipTorque, -clutchTorqueCapacity, clutchTorqueCapacity);
        }

        float wheelTorque = outputTorque * totalRatio * transmissionEfficiency;
        float clutchMaxTorque = clutchTorqueCapacity * clutchEngagement;

        if (currentGear == ReverseGear)
            wheelTorque = Mathf.Min(0f, -Mathf.Abs(wheelTorque));

        return Mathf.Clamp(wheelTorque, -clutchMaxTorque, clutchMaxTorque);
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