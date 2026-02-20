using UnityEngine;

public class EngineSimulator : MonoBehaviour
{
    [Header("Engine Physics")]
    [SerializeField] private float maxTorque = 410f;
    [SerializeField] private float idleRPM = 800f;
    [SerializeField] private float redlineRPM = 7000f;
    [SerializeField] private AnimationCurve normalizedTorqueCurve = AnimationCurve.Linear(0, 0.8f, 1, 1f);
    [SerializeField] private float engineInertia = 0.5f;
    [SerializeField] private float engineFriction = 0.1f;
    [SerializeField] private float clutchStrength = 5f;

    [Header("Gearbox")]
    //ford 10R80 Ratios
    private float[] gearRatios = { 4.696f, 2.985f, 2.146f, 1.769f, 1.520f, 1.275f, 1.000f, 0.854f, 0.689f, 0.636f };
    [SerializeField] private float reverseRatio = -4.866f;
    [SerializeField] private float finalDriveRatio = 3.73f;

    [Range(-1, 10)]//-1 = reverse 0 = neutral 1-5 = gears
    public int currentGear = 0;

    [HideInInspector] public float CurrentRPM;
    [HideInInspector] public float CurrentHP;

    private void Start()
    {
        CurrentRPM = idleRPM;
    }

    public float CalculateTorque(float gasInput, float wheelRPM)
    {
        float gearRatio = gearRatios[Mathf.Clamp(currentGear - 1, 0, 9)];
        float totalRatio = gearRatio * finalDriveRatio;

        if (currentGear == -1)
        {
            CurrentRPM = Mathf.Lerp(CurrentRPM, idleRPM, Time.fixedDeltaTime * clutchStrength);
            return 0;
        }

        if (currentGear == -2)
            totalRatio = reverseRatio;

        float targetRPM = Mathf.Abs(wheelRPM * totalRatio);

        if (targetRPM > redlineRPM)
        {
            gasInput = 0;
            CurrentRPM = redlineRPM;
            return -engineFriction * totalRatio * (targetRPM / redlineRPM);
        }
        else
        {
            CurrentRPM = Mathf.Lerp(CurrentRPM, Mathf.Clamp(targetRPM, idleRPM, redlineRPM), Time.fixedDeltaTime * 10f);
        }

        float rpmPercent = Mathf.InverseLerp(0, redlineRPM, CurrentRPM);
        float engineTorque = normalizedTorqueCurve.Evaluate(rpmPercent) * maxTorque;

        CurrentHP = (engineTorque * CurrentRPM) / 9549f;

        float wheelTorque = engineTorque * totalRatio * gasInput;

        //Debug.Log($"RPM: {CurrentRPM:F0}, Torque: {engineTorque:F1} Nm, HP: {CurrentHP:F1}, Wheel Torque: {wheelTorque:F1} Nm"); 
        return wheelTorque;
    }

    public void ShiftUp() { if (currentGear < gearRatios.Length) currentGear++; }
    public void ShiftDown() { if (currentGear > -2) currentGear--; }
}