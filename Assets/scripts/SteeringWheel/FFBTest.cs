using mozaAPI;
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;
using static mozaAPI.mozaAPI;

public class FFBTest : MonoBehaviour
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();


    void Start()
    {
        installMozaSDK();

        IntPtr hwnd = GetActiveWindow();

        IntPtr deviceHandle = GetID();

        if (deviceHandle == IntPtr.Zero)
        {
            Debug.LogError("not found");
            return;
        }

        ERRORCODE err = ERRORCODE.NORMAL;


        motorMoveTo(deviceHandle, 90f, 10f, ref err);

        if (err != ERRORCODE.NORMAL)
           Debug.LogError(err);
    }


    void Update()
    {
        
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
