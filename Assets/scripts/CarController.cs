using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct Wheel
{
    public WheelCollider collider;
    public GameObject mesh;
    public bool isSteerable;
    public bool isMotorized;
}

public class CarController : MonoBehaviour
{
    [Header("Settings")]
    public float maxMotorTorque = 1500f;
    public float maxSteeringAngle = 30f;
    public float brakeForce = 3000f;

    [Header("Wheels")]
    public List<Wheel> wheels;

    private float inputForward;
    private float inputSteer;
    private bool isBraking;

    void Update()
    {
        //change to new input sys ==============================
        inputForward = Input.GetAxis("Vertical");
        inputSteer = Input.GetAxis("Horizontal");
        isBraking = Input.GetKey(KeyCode.Space);

        UpdateWheelVisuals();
    }

    void FixedUpdate()
    {
        ApplyDrive();
        ApplySteering();
    }

    private void ApplyDrive()
    {
        float currentBrake = isBraking ? brakeForce : 0f;

        Debug.Log(inputForward + " " + inputSteer + " " + isBraking);

        foreach (var wheel in wheels)
        {
            if (wheel.isMotorized)
            {
                wheel.collider.motorTorque = -inputForward * maxMotorTorque;
            }
            wheel.collider.brakeTorque = currentBrake;
        }
    }

    private void ApplySteering()
    {
        float steerAngle = inputSteer * maxSteeringAngle;

        foreach (var wheel in wheels)
        {
            if (wheel.isSteerable)
                wheel.collider.steerAngle = steerAngle;
        }
    }

    private void UpdateWheelVisuals()
    {
        foreach (var wheel in wheels)
        {
            Vector3 pos;
            Quaternion rot;
            wheel.collider.GetWorldPose(out pos, out rot);
            wheel.mesh.transform.position = pos;
            wheel.mesh.transform.rotation = rot;
        }
    }
}