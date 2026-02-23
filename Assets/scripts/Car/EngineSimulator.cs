using UnityEngine;

public class EngineSimulator : MonoBehaviour
{
    [Header("engine physics")]
    [SerializeField] private float maxTorque = 410f;
    [SerializeField] private float idleRPM = 800f;
    [SerializeField] private float redlineRPM = 7000f;
    [SerializeField][Range(0, 1000)] float revLimitVariance = 400f;
    [SerializeField] private AnimationCurve normalizedTorqueCurve = AnimationCurve.Linear(0, 0.8f, 1, 1f);

    [Header("engine breaking")]
    [SerializeField] private float engineFriction = 0.1f;
    [SerializeField] private float breakingFactor = 35f;

    //ford 10R80 Ratios
    [Header("gearbox")]
    private float[] gearRatios = { 4.696f, 2.985f, 2.146f, 1.769f, 1.520f, 1.275f, 1.000f, 0.854f, 0.689f, 0.636f };
    [SerializeField] private float reverseRatio = -4.866f;
    [SerializeField] private float finalDriveRatio = 3.73f;

    [Range(-1, 10)]//-1 = reverse 0 = neutral
    public int currentGear = 0;

    [HideInInspector] public float CurrentRPM;
    [HideInInspector] public float CurrentHP;

    private float engineTorque;
    private float wheelTorque;

    private void Start()
    {
        CurrentRPM = idleRPM;
    }

    public float CalculateTorque(float gasInput, float wheelRPM)
    {
        float gearRatio = gearRatios[Mathf.Clamp(currentGear - 1, 0, 9)];
        float totalRatio = gearRatio * finalDriveRatio;

        //rmp
        float targetRPM = Mathf.Abs(wheelRPM * totalRatio);

        if (targetRPM > redlineRPM)
        {
            gasInput = 0;

            CurrentRPM = redlineRPM + Random.Range(-revLimitVariance, revLimitVariance);

            return -engineFriction * totalRatio * (targetRPM / redlineRPM);
        }
        else
            CurrentRPM = Mathf.Lerp(CurrentRPM, Mathf.Clamp(targetRPM, idleRPM, redlineRPM), Time.fixedDeltaTime * 10f);

        //torque and hp
        float rpmPercent = Mathf.InverseLerp(0, redlineRPM, CurrentRPM);
        engineTorque = normalizedTorqueCurve.Evaluate(rpmPercent) * maxTorque;
        CurrentHP = (engineTorque * CurrentRPM) / 9549f;
        wheelTorque = engineTorque * totalRatio * gasInput;

        //Debug.Log($"RPM: {CurrentRPM:F0}, Torque: {engineTorque:F1} Nm, HP: {CurrentHP:F1}, Wheel Torque: {wheelTorque:F1} Nm"); 

        //neutral
        if (currentGear == -1)
        {
            float revSpeed = gasInput * 7000f;
            CurrentRPM += revSpeed * Time.fixedDeltaTime * 10;
            CurrentRPM = Mathf.Clamp(CurrentRPM, idleRPM, redlineRPM);

            return 0;
        }

        //reverse or normal
        if (currentGear == -2)
            return -wheelTorque / 1.5f;
        else
            return wheelTorque;
    }

    public void ShiftUp() { if (currentGear < gearRatios.Length) currentGear++; }
    public void ShiftDown() { if (currentGear > -2) currentGear--; }

    public float ApplyEngineBreaking()
    {
        float gearRatio = gearRatios[Mathf.Clamp(currentGear - 1, 0, 9)];
        float totalRatio = gearRatio * finalDriveRatio;

        return engineFriction * totalRatio * CurrentRPM / breakingFactor;
    }
}