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
    [SerializeField] private float frictionCoeff = 0.005f;
    [SerializeField] private float engineBrakingCoeff = 0.003f;

    [Header("gearbox")]
    public DrivetrainType drivetrainMode = DrivetrainType.AllWheelDrive;
    [SerializeField] private float[] gearRatios = { 4.696f, 2.985f, 2.146f, 1.769f, 1.520f, 1.275f, 1.000f, 0.854f, 0.689f, 0.636f };
    [SerializeField] private float reverseRatio = 4.866f;
    [SerializeField] private float finalDriveRatio = 4.10f;
    [SerializeField] private float transmissionEfficiency = 0.97f;
    [SerializeField] private float rpmSyncThreshold = 300f;

    [Header("clutch")]
    [SerializeField] private float clutchTorqueCapacity = 700f;
    [SerializeField] private float clutchEngageSpeed = 4f;

    [HideInInspector] public int currentGear = 0;
    [HideInInspector] public float currentRPM;
    [HideInInspector] public float drivetrainRPM;
    [HideInInspector] public bool isStalled = false;
    [HideInInspector] public float groundSlopeAngle = 0f;
    [HideInInspector] public float outputWheelTorque = 0f;

    private const int NeutralGear = -1;
    private const int ReverseGear = -2;

    [Range(0f, 1f)] private float clutchEngagement = 0f;
    private float clutchInput = 1f;
    private float shiftTimer = 0f;
    private bool clutchLocked = false;

    private CarController car;

    [SerializeField]
    private AnimationCurve torqueCurve = new AnimationCurve(
        new Keyframe(0.00f, 0.30f, 0f, 2.0f),
        new Keyframe(0.15f, 0.70f, 2.0f, 1.2f),
        new Keyframe(0.40f, 0.95f, 0.8f, 0.4f),
        new Keyframe(0.65f, 1.00f, 0.1f, -0.3f),
        new Keyframe(0.80f, 0.92f, -0.6f, -0.8f),
        new Keyframe(1.00f, 0.65f, -1.2f, 0f));

    private void Start()
    {
        car = GetComponent<CarController>();
        currentRPM = idleRPM;
    }

    public float UpdateEngine(float gasInput, float wheelRPM)
    {
        float dt = Time.fixedDeltaTime;

        //stall recovery - neutral + gas to restart
        if (isStalled)
        {
            if (currentGear == NeutralGear && gasInput > 0.1f)
            {
                isStalled = false;
                currentRPM = idleRPM;
            }
            else return 0f;
        }

        //power cut during gear change
        shiftTimer -= dt;
        clutchInput = shiftTimer > 0f ? 0f : 1f;

        float gearRatio = GetCurrentGearRatio();
        float totalRatio = gearRatio * finalDriveRatio;

        //clutch engagement lerps toward target
        float targetClutch = currentGear == NeutralGear ? 0f : clutchInput;
        clutchEngagement = Mathf.Lerp(clutchEngagement, targetClutch, dt * clutchEngageSpeed);

        //engine torque from throttle and RPM curve
        float rpmPercent = Mathf.InverseLerp(idleRPM, redlineRPM, currentRPM);
        float curveSample = torqueCurve.Evaluate(rpmPercent);
        bool revLimiter = currentRPM >= redlineRPM * 0.98f;
        float engineTorque = revLimiter ? 0f : curveSample * maxTorque * gasInput;

        //idle creep - small torque at zero throttle to stop stalling from standstill
        //fades out as RPM rises above idle band
        if (currentGear != NeutralGear && currentGear != ReverseGear && !isStalled)
        {
            float idleCreep = Mathf.Lerp(maxTorque * 0.08f, 0f, rpmPercent * 3f);
            engineTorque += idleCreep;
        }

        //shaft losses - intentionally small, wheel collider handles road resistance
        float frictionTorque = currentRPM * frictionCoeff;
        float engineBrakingTorque = clutchEngagement > 0.1f ? currentRPM * engineBrakingCoeff * (1f - gasInput) : 0f;

        drivetrainRPM = wheelRPM * totalRatio;

        float rpmDelta = currentRPM - drivetrainRPM;
        if (!clutchLocked)
        {
            //RPMs must be close before locking
            clutchLocked = clutchEngagement >= 0.95f
                && Mathf.Abs(rpmDelta) < rpmSyncThreshold * 0.5f
                && currentGear != NeutralGear;
        }
        else
        {
            //prevents unlocking on minor speed variation
            clutchLocked = clutchEngagement >= 0.65f
                && Mathf.Abs(rpmDelta) < rpmSyncThreshold * 2.0f
                && currentGear != NeutralGear;
        }

        float outputTorque;

        if (clutchLocked)
        {
            float correctionTorque = (drivetrainRPM - currentRPM) * 60f;

            float netTorque = engineTorque - frictionTorque - engineBrakingTorque + correctionTorque;
            float rpmAccel = (netTorque / engineInertia) * (60f / (2f * Mathf.PI));

            currentRPM += rpmAccel * dt;
            currentRPM = Mathf.Clamp(currentRPM, idleRPM, redlineRPM);

            outputTorque = netTorque - correctionTorque;

            if (currentRPM <= stallRPM && netTorque < 0f)
            {
                isStalled = true;
                currentRPM = 0f;
                return 0f;
            }
        }
        else
        {
            //clutch slipping or neutral - engine freewheels, no road load
            float netFreeRev = engineTorque - frictionTorque - engineBrakingTorque;
            float rpmAccel = (netFreeRev / engineInertia) * (60f / (2f * Mathf.PI));
            currentRPM += rpmAccel * dt;

            if (gasInput < 0.05f)
            {
                float targetIdleRPM = currentGear == NeutralGear ? idleRPM : Mathf.Max(idleRPM, drivetrainRPM);
                currentRPM = Mathf.Lerp(currentRPM, targetIdleRPM, dt * idleReturnSpeed);
            }

            currentRPM = Mathf.Clamp(currentRPM, stallRPM, redlineRPM);

            //slipping clutch passes torque proportional to engagement
            float slipTorque = engineTorque * clutchEngagement * transmissionEfficiency;
            outputTorque = Mathf.Clamp(slipTorque, -clutchTorqueCapacity, clutchTorqueCapacity);
        }

        //calculate torque at the engine/clutch level
        float clutchMaxTorque = clutchTorqueCapacity * clutchEngagement;
        float limitedOutputTorque = Mathf.Clamp(outputTorque, -clutchMaxTorque, clutchMaxTorque);

        //scale that limited torque through the gears to the wheels
        float wheelTorque = limitedOutputTorque * totalRatio * transmissionEfficiency;

        //reverse
        if (currentGear == ReverseGear)
            wheelTorque = -Mathf.Abs(wheelTorque);

        outputWheelTorque = wheelTorque;

        return wheelTorque;
    }

    private float GetCurrentGearRatio()
    {
        if (currentGear == NeutralGear) return 0f;
        if (currentGear == ReverseGear) return reverseRatio;
        return gearRatios[Mathf.Clamp(currentGear, 0, gearRatios.Length - 1)];
    }

    public void ShiftUp(float currentWheelRPM)
    {
        if (!car.canDrive) return;
        if (currentGear >= gearRatios.Length - 1) return;

        currentGear++;
        shiftTimer = 0.25f;
        clutchLocked = false;

        SnapRPMToGear(currentWheelRPM);
    }

    public void ShiftDown(float currentWheelRPM)
    {
        if (!car.canDrive) return;
        if (currentGear <= ReverseGear) return;

        currentGear--;
        shiftTimer = 0.25f;
        clutchLocked = false;

        SnapRPMToGear(currentWheelRPM);
    }

    private void SnapRPMToGear(float wheelRPM)
    {
        // calculate where RPM should be in the new gear based on current wheel speed
        float newRatio = GetCurrentGearRatio() * finalDriveRatio;
        float targetRPM = wheelRPM * newRatio;
        currentRPM = Mathf.Clamp(targetRPM, idleRPM, redlineRPM);
    }
}