using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CarGauges : MonoBehaviour
{
    [Header("speed")]
    [SerializeField] private RectTransform speedNeedle;
    [SerializeField] private float speedMinAngle = -110f;
    [SerializeField] private float speedMaxAngle = 110f;
    [SerializeField] private float speedMaxKmh = 220f;

    [Header("RPM")]
    [SerializeField] private RectTransform rpmNeedle;
    [SerializeField] private float rpmMinAngle = -110f;
    [SerializeField] private float rpmMaxAngle = 110f;
    [SerializeField] private float rpmMax = 8000f;

    [Header("both guages")]
    [SerializeField] private float needleSmoothSpeed = 8f;

    [Header("gear")]
    [SerializeField] private TMP_Text gearTxt;

    private EngineSimulator engine;
    private Rigidbody rb;

    private float smoothedSpeedAngle;
    private float smoothedRpmAngle;

    private Vector3 speedBaseEuler;
    private Vector3 rpmBaseEuler;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        engine = GetComponentInChildren<EngineSimulator>();

        speedBaseEuler = speedNeedle.localEulerAngles;
        rpmBaseEuler = rpmNeedle.localEulerAngles;

        smoothedSpeedAngle = speedMinAngle;
        smoothedRpmAngle = rpmMinAngle;
    }

    private void Update()
    {
        UpdateRPM();
        UpdateSpeed();
        UpdateGear();
    }

    private void UpdateGear()
    {
        gearTxt.text = engine.currentGear switch
        {
            -2 => "R",
            -1 => "N",
            _ => (engine.currentGear + 1).ToString()
        };
    }

    private void UpdateSpeed()
    {
        float speedKmh = Mathf.Max(0f, rb.linearVelocity.magnitude * 3.6f);
        float speedDelta = Mathf.Clamp01(speedKmh / speedMaxKmh);
        float targetAngle = Mathf.Lerp(speedMinAngle, speedMaxAngle, speedDelta);

        smoothedSpeedAngle = Mathf.Lerp(smoothedSpeedAngle, targetAngle, Time.deltaTime * needleSmoothSpeed);
        speedNeedle.localEulerAngles = new Vector3(speedBaseEuler.x, speedBaseEuler.y, smoothedSpeedAngle);
    }

    private void UpdateRPM()
    {
        float rpmDelta = Mathf.Clamp01(engine.currentRPM / rpmMax);
        float targetAngle = Mathf.Lerp(rpmMinAngle, rpmMaxAngle, rpmDelta);

        smoothedRpmAngle = Mathf.Lerp(smoothedRpmAngle, targetAngle, Time.deltaTime * needleSmoothSpeed);
        rpmNeedle.localEulerAngles = new Vector3(rpmBaseEuler.x, rpmBaseEuler.y, smoothedRpmAngle);
    }
}