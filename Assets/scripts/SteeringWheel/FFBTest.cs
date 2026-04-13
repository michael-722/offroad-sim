using mozaAPI;
using System;
using System.Runtime.InteropServices;
using UnityEditor.PackageManager;
using UnityEngine;

using static mozaAPI.mozaAPI;

public class FFBTest : MonoBehaviour
{
    [SerializeField] private bool moveMotorTest = false;
    [SerializeField] private bool constForceTest = false;
    [SerializeField] private bool SpringForceTest = false;
    [SerializeField] private bool frictionForceTest = false;

    private float steerInput;

    //ref for moza sdk to use when creating effects
    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    private IntPtr hwnd;

    private ETConstantForce pushForce;
    private ETSpring springForce;
    private ETFriction frictionForce;

    void Start()
    {
        installMozaSDK();
        InputManager.Instance.OnSteeringEvent += (val) => steerInput = val;

        //effect id param
        hwnd = GetActiveWindow();
    }

    private void Update()
    {
        if (moveMotorTest)
            MoveMotorTest();

        if (constForceTest)
            ApplyConstantForceTest();

        if (SpringForceTest)
            ApplySpringTest();

        if (frictionForceTest)
            ApplyFriction();
    }

    private void OnDisable()
    {
        //stops effects frpm still running on wheel after disable or stopping playing

        if (pushForce != null)
            pushForce.stop();

        if (springForce != null)
            springForce.stop();

        if (frictionForce != null)
            frictionForce.stop();
    }

    private void MoveMotorTest()
    {
        //not working
        ERRORCODE err = ERRORCODE.NORMAL;

        motorMoveTo(hwnd, 0f, 30f, ref err);

        if (err != ERRORCODE.NORMAL)
            Debug.LogError($"motor move to error: {err}");
        else
            Debug.Log("motor moving to center.");
    }

    private void ApplyConstantForceTest()
    {
        //working
        ERRORCODE err = ERRORCODE.NORMAL;

        if (pushForce == null)
        {
            pushForce = createWheelbaseETConstantForce(hwnd, ref err);
            Debug.Log("created constant force");
        }
        if (err != ERRORCODE.NORMAL || pushForce == null)
        {
            Debug.LogError($"createWheelbaseETConstantForce failed: {err}");
            return;
        }

        pushForce.setDuration(2000);

        if (steerInput < 0)
            pushForce.setMagnitude(-3500);
        else
            pushForce.setMagnitude(3500);

        try { pushForce.start(); }
        catch (Exception e) { Debug.LogException(e); }
    }


    private void ApplySpringTest()
    {
        //working
        ERRORCODE err = ERRORCODE.NORMAL;

        if (springForce == null)
        {
            springForce = createWheelbaseETSpring(hwnd, ref err);
            Debug.Log("created spring force");
        }

        if (err != ERRORCODE.NORMAL || springForce == null)
        {
            Debug.LogError($"createWheelbaseETSpring failed: {err}");
            return;
        }

        springForce.setDuration(20000);
        springForce.setAttackLevel(5);

        try { springForce.start(); }
        catch (Exception e) { Debug.LogException(e); }
    }

    private void ApplyFriction()
    {
        //not sure if working

        ERRORCODE err = ERRORCODE.NORMAL;

        if (frictionForce == null)
        {
            frictionForce = createWheelbaseETFriction(hwnd, ref err);
            Debug.Log("created friciton force");
        }

        if (err != ERRORCODE.NORMAL || frictionForce == null)
        {
            Debug.Log($"createWheelbaseETFriction failed: {err}");
            return;
        }

        frictionForce.setDuration(10000);

        //Debug.Log($"offset: {frictionForce.offset()} deadband: {frictionForce.deadBand()}");
        //Debug.Log($"positive coiff: {frictionForce.positiveCoefficient()} negative coiff: {frictionForce.negativeCoefficient()}");
        //Debug.Log($"positive saturation: {frictionForce.positiveSaturation()} negative saturation: {frictionForce.negativeSaturation()}");

        try { frictionForce.start(); }
        catch (Exception e) { Debug.LogException(e); }
    }
};