using mozaAPI;
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using static mozaAPI.mozaAPI;

public class FFBSystem : MonoBehaviour
{
    [SerializeField] private WheelCollider frontLeft;
    [SerializeField] private WheelCollider frontRight;
    [SerializeField] private float forceScale = 8000f;

    [Header("Equilibrium")]
    [Tooltip("How strongly the wheel resists going past the natural settle angle")]
    [SerializeField] private float resistanceScale = 6000f;

    [Header("Surface Rumble")]
    [SerializeField] private SurfaceRumble[] rumbleProfiles;

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    private IntPtr hwnd;
    private ETConstantForce ffbForce;

    private CarController car;

    private float rumbleTimer = 0f;
    private float currentRumbleOffset = 0f;

    [Serializable]
    public struct SurfaceRumble
    {
        public string materialTag;
        public float frequency;
        public float amplitude;
    }

    private void Start()
    {
        car = GetComponent<CarController>();

        hwnd = GetActiveWindow();
        installMozaSDK();

        ERRORCODE err = ERRORCODE.NORMAL;
        ffbForce = createWheelbaseETConstantForce(hwnd, ref err);
        ffbForce.setDuration(int.MaxValue);
        ffbForce.start();
    }

    private void FixedUpdate()
    {
        if (!SessionData.Instance.FFBstate) return;

        //surface normal force
        frontLeft.GetGroundHit(out WheelHit flHit);
        frontRight.GetGroundHit(out WheelHit frHit);

        //angle the surface wants to push the wheel to
        float avgNormalX = (flHit.normal.x + frHit.normal.x) * -0.5f;

        //equilibrium limit
        float steerInput = car.steerInput; // -1 to 1
        float equilibrium = Mathf.Clamp(avgNormalX, -1f, 1f);

        //overshoot
        float overSteer = steerInput - equilibrium;

        //resist in opposite dir to overshoot
        float resistForce = -overSteer * resistanceScale;

        //surface rumble
        SurfaceFriction flSurface = car.GetWheelSurface(0);
        SurfaceFriction frSurface = car.GetWheelSurface(1);

        //fl for ground force -- probably not worth avging out need to test==========
        SurfaceRumble rumble = GetRumbleProfile(flSurface.materialTag);

        rumbleTimer += Time.fixedDeltaTime * rumble.frequency;
        currentRumbleOffset = Mathf.Sin(rumbleTimer) * rumble.amplitude;

        //output force
        float totalForce = (avgNormalX * forceScale) + resistForce + currentRumbleOffset;
        totalForce *= SessionData.Instance.FFBstrength;

        ffbForce.setMagnitude(Mathf.RoundToInt(Mathf.Clamp(totalForce, -10000f, 10000f)));
    }

    private SurfaceRumble GetRumbleProfile(string materialTag)
    {
        for (int i = rumbleProfiles.Length - 1; i >= 0; i--)
        {
            if (materialTag.IndexOf(rumbleProfiles[i].materialTag,
                StringComparison.OrdinalIgnoreCase) >= 0)
                return rumbleProfiles[i];
        }

        return new SurfaceRumble { frequency = 8f, amplitude = 80f };
    }

    private void OnDisable()
    {
        if (ffbForce != null)
            ffbForce.stop();
    }
}