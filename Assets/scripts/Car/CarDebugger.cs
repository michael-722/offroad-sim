using UnityEngine;

public class CarDebugger : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool showTelemetry = true;
    [SerializeField] private Color debugTextColor = Color.green;
    [Range(8, 20)] public int fontSize = 12;

    private CarController controller;
    private EngineSimulator engine;
    private Rigidbody rb;

    private void Start()
    {
        controller = GetComponent<CarController>();
        engine = GetComponentInChildren<EngineSimulator>();
        rb = GetComponent<Rigidbody>();
    }

    private void OnGUI()
    {
        if (!showTelemetry || controller == null || engine == null) return;

        GUIStyle style = new GUIStyle();
        style.normal.textColor = debugTextColor;
        style.fontSize = fontSize;
        style.fontStyle = FontStyle.Bold;

        float speedKmh = rb.linearVelocity.magnitude * 3.6f;

        string telemetry =
            $"SPD: {speedKmh:F1} km/h\n" +
            $"RPM: {engine.currentRPM:F0}\n" +
            $"GR:  {engine.currentGear + 1}\n" +
            $"GAS: {controller.gasInput:P0}\n" +
            $"BRK: {controller.brakeInput:P0}\n" +
            $"STR: {controller.steerInput:P0}\n";

        GUI.Label(new Rect(15, 15, 200, 150), telemetry, style);
    }
}