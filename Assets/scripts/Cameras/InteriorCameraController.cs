using Unity.Cinemachine;
using UnityEngine;

[System.Serializable]
public struct lookPoints
{
    public Transform forward;
    public Transform right;
    public Transform left;
    public Transform gearstick;
    public Transform handbreak;
}

public class InteriorCameraController : MonoBehaviour
{
    [SerializeField] private lookPoints LookPoints;
    [SerializeField] private CinemachineCamera interiorCam;
    [SerializeField] private Transform camTarget;

    private Transform currentTarget;

    private void Start()
    {
        if (CameraController.Instance != null)
            CameraController.Instance.SetInteriorCameraController(this);
        else
            Debug.LogWarning("no interior camera found");

            SetTarget(LookPoints.forward);
    }

    public void LookForward()
    {
        SetTarget(LookPoints.forward);
    }

    public void LookLeft()
    {
        Transform nextTarget = (currentTarget == LookPoints.left) ? LookPoints.forward : LookPoints.left;
        SetTarget(nextTarget);
    }

    public void LookRight()
    {
        Transform nextTarget = (currentTarget == LookPoints.right) ? LookPoints.forward : LookPoints.right;
        SetTarget(nextTarget);
    }

    private void SetTarget(Transform newPoint)
    {
        if (newPoint == null) return;
        Debug.Log($"Target set to {newPoint.name} by {new System.Diagnostics.StackTrace().GetFrame(1).GetMethod().Name}");

        currentTarget = newPoint;
        camTarget.position = newPoint.position;
        camTarget.rotation = newPoint.rotation;

        interiorCam.LookAt = camTarget;
    }
}