using UnityEngine;
using System;
using System.Runtime.InteropServices.WindowsRuntime;

[Serializable]
public struct SurfaceFriction
{
    public string materialTag;
    public float extremumSlip;
    public float extremumValue;
    public float asymptoteSlip;
    public float asymptoteValue;
    public float sidewaysStiffness;
    public float tcsSlipLimit;
}

public class CarController : MonoBehaviour
{
    [SerializeField] public bool canDrive = false;
    [SerializeField] private EngineSimulator engine;
    [SerializeField] private WheelData[] wheels;

    [Header("settings")]
    [SerializeField] private float maxSteerAngle = 30f;
    [SerializeField] private float brakeForce = 3000f;

    [Header("friction settings")]
    [SerializeField]
    private SurfaceFriction[] surfaceProfiles;

    [Header("traction control")]
    [SerializeField] private bool tcsEnabled = true;
    [SerializeField] private float tcsMinSpeed = 1.5f;
    [SerializeField] private float tcsResponseSpeed = 10f;
    [HideInInspector] public float tcsTorqueFactor = 1f;

    [Header("launch")]
    [SerializeField] private float launchBlendSpeed = 4f;//speed at which launch curve fully fades
    [SerializeField] private float launchExtrumumBoost = 0.6f;//added to surface extremumValue at standstill
    [SerializeField] private float launchSlipReduction = 0.08f;//how much extremumSlip tightens at launch

    [Header("drag")]
    [SerializeField] private float aeroDragCoeff = 0.45f;
    [SerializeField] private float rollingResistanceCoeff = 0.015f;
    [SerializeField] private float engineBrakingForce = 800f;

    private Rigidbody rb;
    [HideInInspector] public float gasInput;
    [HideInInspector] public float brakeInput;
    [HideInInspector] public float steerInput;

    private int activeWheels = 0;
    private float smoothedWheelRPM = 0f;
    private SurfaceFriction[] wheelSurfaces;

    //cache terrain - its static
    private Terrain _terrain;
    private TerrainData _terrainData;
    private Vector3 _terrainPos;
    private float speedMS;

    public float GetSpeedMS() => speedMS;
    public bool SetCanDrive(bool state) => canDrive = state;

    public SurfaceFriction GetWheelSurface(int wheelIndex) => wheelSurfaces[wheelIndex];

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        InputManager.Instance.OnAccelerateEvent += (val) => gasInput = val;
        InputManager.Instance.OnBrakeEvent += (val) => brakeInput = val;
        InputManager.Instance.OnSteeringEvent += (val) => steerInput = val;
        InputManager.Instance.OnGearUpEvent += () => engine.ShiftUp(GetAverageDrivenWheelRPM());
        InputManager.Instance.OnGearDownEvent += () => engine.ShiftDown(GetAverageDrivenWheelRPM());

        activeWheels = 0;
        wheelSurfaces = new SurfaceFriction[wheels.Length];

        for (int i = 0; i < wheels.Length; i++)
        {
            if (IsWheelInDrivetrain(wheels[i])) activeWheels++;
            wheelSurfaces[i] = surfaceProfiles[0];
        }

        CacheTerrain();
    }

    private void OnDestroy()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnAccelerateEvent -= (val) => gasInput = val;
            InputManager.Instance.OnBrakeEvent -= (val) => brakeInput = val;
            InputManager.Instance.OnSteeringEvent -= (val) => steerInput = val;
            InputManager.Instance.OnGearUpEvent -= () => engine.ShiftUp(GetAverageDrivenWheelRPM());
            InputManager.Instance.OnGearDownEvent -= () => engine.ShiftDown(GetAverageDrivenWheelRPM());
        }
    }

    private void CacheTerrain()
    {
        _terrain = Terrain.activeTerrain;
        if (_terrain == null) return;
        _terrainData = _terrain.terrainData;
        _terrainPos = _terrain.transform.position;
    }

    private void FixedUpdate()
    {
        speedMS = Vector3.Dot(rb.linearVelocity, transform.forward);

        //signed slope angle - positive = uphill negative = downhill
        engine.groundSlopeAngle = 0f;
        foreach (var w in wheels)
        {
            WheelHit hit;
            if (w.collider.GetGroundHit(out hit))
            {
                float unsignedAngle = Vector3.Angle(hit.normal, Vector3.up);
                float sign = -Mathf.Sign(transform.forward.y);
                engine.groundSlopeAngle = Mathf.Lerp(engine.groundSlopeAngle,
                    unsignedAngle * sign, Time.fixedDeltaTime * 10f);
                break;
            }
        }

        //engine updating
        float avgDrivenWheelRPM = GetAverageDrivenWheelRPM();
        float totalOutputTorque = engine.UpdateEngine(canDrive ? gasInput : 0, avgDrivenWheelRPM);

        //traction control
        if (tcsEnabled) ApplyTractionControl(speedMS);

        //distribute torque to drive wheels
        float torquePerWheel = activeWheels > 0 ? (totalOutputTorque / activeWheels) * tcsTorqueFactor : 0f;

        //apply steering torque and braking to each wheel
        for (int i = 0; i < wheels.Length; i++)
        {
            var wheel = wheels[i];

            if (wheel.isSteerable)
                wheel.collider.steerAngle = steerInput * maxSteerAngle;

            //negative bc wheel colliders are backwards in game space
            wheel.collider.motorTorque = IsWheelInDrivetrain(wheel) ? -torquePerWheel / 2: 0f;
            wheel.collider.brakeTorque = brakeInput * brakeForce;

            //update friction based on surface and current speed
            UpdateWheelFriction(i, speedMS);
        }

        //drag
        ApplyDrag(speedMS);
    }

    //average RPM across all driven wheels - gives engine a realistic load signal
    //using avg rather than max prevents a single spinning wheel from inflating the reading
    public float GetAverageDrivenWheelRPM()
    {
        float total = 0f;
        int count = 0;

        foreach (var w in wheels)
        {
            if (!IsWheelInDrivetrain(w)) continue;
            total += Mathf.Abs(w.collider.rpm);
            count++;
        }

        float raw = count > 0 ? total / count : 0f;
        smoothedWheelRPM = Mathf.Lerp(smoothedWheelRPM, raw, Time.fixedDeltaTime * 10f);
        return smoothedWheelRPM;
    }

    private SurfaceFriction GetSurfaceProfile(WheelCollider wheel)
    {
        WheelHit hit;
        if (!wheel.GetGroundHit(out hit)) return surfaceProfiles[0];

        //sample terrain atcontact point
        if (_terrain != null && hit.collider == _terrain.GetComponent<Collider>())
        {
            int layerIndex = GetDominantTerrainLayer(hit.point);
            if (layerIndex >= 0 && layerIndex < _terrainData.terrainLayers.Length)
            {
                string layerName = _terrainData.terrainLayers[layerIndex].name;
                for (int i = surfaceProfiles.Length - 1; i >= 0; i--)
                {
                    if (layerName.IndexOf(surfaceProfiles[i].materialTag,
                        StringComparison.OrdinalIgnoreCase) >= 0)
                        return surfaceProfiles[i];
                }
            }
        }

        //non-terrain obj - uses physics material
        string mat = hit.collider.sharedMaterial != null ? hit.collider.sharedMaterial.name : "";
        for (int i = surfaceProfiles.Length - 1; i >= 0; i--)
        {
            if (mat.IndexOf(surfaceProfiles[i].materialTag,
                StringComparison.OrdinalIgnoreCase) >= 0)
                return surfaceProfiles[i];
        }

        return surfaceProfiles[0];
    }

    //sample nearest texel at sample pos
    private int GetDominantTerrainLayer(Vector3 worldPos)
    {
        if (_terrainData == null) return 0;

        //world pos to normalised terrain
        float normX = (worldPos.x - _terrainPos.x) / _terrainData.size.x;
        float normZ = (worldPos.z - _terrainPos.z) / _terrainData.size.z;

        normX = Mathf.Clamp01(normX);
        normZ = Mathf.Clamp01(normZ);

        //alphamap coords - z maps to y axis in alphamap
        int mapX = Mathf.FloorToInt(normX * (_terrainData.alphamapWidth - 1));
        int mapZ = Mathf.FloorToInt(normZ * (_terrainData.alphamapHeight - 1));

        float[,,] alphaMap = _terrainData.GetAlphamaps(mapX, mapZ, 1, 1);

        int dominant = 0;
        float max = 0f;
        for (int layer = 0; layer < alphaMap.GetLength(2); layer++)
        {
            if (alphaMap[0, 0, layer] > max)
            {
                max = alphaMap[0, 0, layer];
                dominant = layer;
            }
        }

        return dominant;
    }

    private void UpdateWheelFriction(int wheelIndex, float carSpeedMS)
    {
        WheelCollider wheel = wheels[wheelIndex].collider;
        WheelHit hit;
        if (!wheel.GetGroundHit(out hit)) return;

        SurfaceFriction surf = GetSurfaceProfile(wheel);
        wheelSurfaces[wheelIndex] = surf;

        float launchBlend = 1f - Mathf.Clamp01(Mathf.Abs(carSpeedMS) / launchBlendSpeed);

        WheelFrictionCurve fwd = wheel.forwardFriction;
        fwd.extremumSlip = surf.extremumSlip - (launchSlipReduction * launchBlend);
        fwd.extremumValue = surf.extremumValue + (launchExtrumumBoost * launchBlend);
        fwd.asymptoteSlip = surf.asymptoteSlip;
        fwd.asymptoteValue = surf.asymptoteValue;
        fwd.stiffness = 1f;
        wheel.forwardFriction = fwd;

        WheelFrictionCurve side = wheel.sidewaysFriction;
        side.stiffness = surf.sidewaysStiffness;
        wheel.sidewaysFriction = side;
    }

    private void ApplyTractionControl(float carSpeedMS)
    {
        if (!tcsEnabled || Mathf.Abs(carSpeedMS) < tcsMinSpeed)
        {
            tcsTorqueFactor = Mathf.Lerp(tcsTorqueFactor, 1f, Time.fixedDeltaTime * tcsResponseSpeed);
            return;
        }

        float worstSlipRatio = 0f;

        for (int i = 0; i < wheels.Length; i++)
        {
            if (!wheels[i].isMotorized) continue;
            WheelHit hit;
            if (!wheels[i].collider.GetGroundHit(out hit)) continue;

            float slip = Mathf.Abs(hit.forwardSlip);
            float limit = wheelSurfaces[i].tcsSlipLimit;
            float ratio = Mathf.Clamp01((slip - limit) / limit);
            if (ratio > worstSlipRatio) worstSlipRatio = ratio;
        }

        float targetFactor = 1f - worstSlipRatio;
        tcsTorqueFactor = Mathf.Lerp(tcsTorqueFactor, targetFactor, Time.fixedDeltaTime * tcsResponseSpeed);
        tcsTorqueFactor = Mathf.Clamp01(tcsTorqueFactor);
    }

    private void ApplyDrag(float speedMS)
    {
        //aero drag - exponential
        float drag = aeroDragCoeff * speedMS * speedMS;

        //only when moving
        if (Mathf.Abs(speedMS) > 1.0f)
            drag += rollingResistanceCoeff * rb.mass * 9.81f;

        //engine braking - in gear clutch engaged off throttle
        bool inGear = !engine.isStalled && engine.currentGear >= 0;
        if (inGear && gasInput < 0.05f && speedMS > 0.5f)
        {
            //scale with gears - lower gear = more engine braking
            float gearFactor = Mathf.Clamp01(engine.drivetrainRPM / Mathf.Max(engine.currentRPM, 1f));
            float engineBrake = engineBrakingForce * gearFactor * (1f - gasInput);

            //low speed idle creep - fade engine braking out so it doesnt hold car at rest
            float speedFade = Mathf.InverseLerp(2f, 4f, speedMS);
            drag += engineBrake * speedFade;
        }

        if (Mathf.Abs(speedMS) > 0.1f)
            rb.AddForce(-rb.linearVelocity.normalized * drag, ForceMode.Force);
    }

    private void Update()
    {
        //updating the wheel visuals
        foreach (var wheel in wheels)
        {
            wheel.collider.GetWorldPose(out Vector3 pos, out Quaternion rot);
            wheel.visual.position = pos;
            wheel.visual.rotation = rot;
        }
    }

    public void ResetWheels()
    {
        for (int i = 0; i < wheels.Length; i++)
            wheels[i].collider.rotationSpeed = 0;
    }

    private bool IsWheelInDrivetrain(WheelData wheel)
    {
        if (!wheel.isMotorized) return false;
        return (DrivetrainType)engine.drivetrainMode switch
        {
            DrivetrainType.AllWheelDrive => true,
            DrivetrainType.RearWheelDrive => !wheel.isFrontWheel,
            DrivetrainType.FrontWheelDrive => wheel.isFrontWheel,
            _ => false
        };
    }

    [Serializable]
    public struct WheelData
    {
        public WheelCollider collider;
        public Transform visual;
        public bool isSteerable;
        public bool isMotorized;
        public bool isFrontWheel;
    }
}