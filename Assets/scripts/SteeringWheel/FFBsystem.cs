using mozaAPI;
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using static mozaAPI.mozaAPI;

public class FFBsystem : MonoBehaviour
{
    [SerializeField] private WheelCollider frontLeft;
    [SerializeField] private WheelCollider frontRight;
    [SerializeField] private float forceScale = 8000f;

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    private IntPtr hwnd;
    private ETConstantForce ffbForce;

    private void Start()
    {
        hwnd = GetActiveWindow();
        installMozaSDK();

        ERRORCODE err = ERRORCODE.NORMAL;
        ffbForce = createWheelbaseETConstantForce(hwnd, ref err);
        ffbForce.setDuration(int.MaxValue);
        ffbForce.start();
    }

    private void FixedUpdate()
    {
        float force = 0f;

        frontLeft.GetGroundHit(out WheelHit flHit);
        frontRight.GetGroundHit(out WheelHit frHit);

        //surface normal - how much ground is tilting left/right under wheel
        //positive = ground slopes right = wheel wants to roll left = push wheel left
        force += flHit.normal.x;
        force += frHit.normal.x;
        force *= 0.5f;

        //Debug.Log(force);

        ffbForce.setMagnitude(Mathf.RoundToInt(force * forceScale));
    }

    private void OnDisable()
    {
        if (ffbForce != null) ffbForce.stop();
    }
}