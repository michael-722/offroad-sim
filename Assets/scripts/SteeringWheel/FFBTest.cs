using mozaAPI;
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using static mozaAPI.mozaAPI;

public class FFBTest : MonoBehaviour
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    private IntPtr hwnd;

    private ETConstantForce pushForce;
    private ETSpring springForce;

    void OnDestroy()
    {
        stopForceFeedback();
        motorStopMove();
        removeMozaSDK();
    }

    void Start()
    {
        installMozaSDK();

        //HWND as id param
        hwnd = GetActiveWindow();
        Debug.Log(GetDeviceName(hwnd));
        Debug.Log($"HWND: {hwnd}");

        //motor move to - rotate wheel to 0 degrees
        ERRORCODE moveErr = ERRORCODE.NORMAL;
        motorMoveTo(hwnd, 0f, 30f, ref moveErr);
        if (moveErr != ERRORCODE.NORMAL)
            Debug.LogError($"motor move to error: {moveErr}");
        else
            Debug.Log("motor moving to center.");

        //force feedback - constant force test
        ApplyConstantForce(50);
        
        //force feedback - spring test
        ApplySpring();
    }

    void ApplyConstantForce(int magnitude)
    {
        ERRORCODE err = ERRORCODE.NORMAL;
        ETConstantForce effect = createWheelbaseETConstantForce(hwnd, ref err);

        if (err != ERRORCODE.NORMAL || effect == null)
        {
            Debug.LogError($"createWheelbaseETConstantForce failed: {err}");
            return;
        }

        effect.setMagnitude(magnitude);
        effect.start();
        pushForce = effect;
        Debug.Log("constant FFB started");
    }

    void ApplySpring()
    {
        ERRORCODE err = ERRORCODE.NORMAL;
        ETSpring effect = createWheelbaseETSpring(hwnd, ref err);

        if (err != ERRORCODE.NORMAL || effect == null)
        {
            Debug.LogError($"createWheelbaseETSpring failed: {err}");
            return;
        }

        effect.start();
        springForce = effect;
        Debug.Log("Spring FFB started.");
    }

    private IntPtr GetID()
    {
        string deviceID = SystemInfo.deviceUniqueIdentifier;
        IntPtr idPtr = Marshal.StringToHGlobalAnsi(deviceID);

        try
        {
            Debug.Log($"hardware id: {idPtr}");
        }
        finally
        {
            Marshal.FreeHGlobal(idPtr);
        }
        Debug.Log("got hardware id for: " + GetDeviceName(idPtr));
        return idPtr;
    }

    private string GetDeviceName(IntPtr handle)
    {
        byte[] buffer = new byte[64];
        return "Moza Device (Handle: " + handle.ToString() + ")";
    }

}
