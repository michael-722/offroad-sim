using Unity.Cinemachine;
using UnityEngine;

[System.Serializable]
public struct LookPoints
{
    public Transform forward;
    public Transform right;
    public Transform left;
    public Transform gearstick;
    public Transform handbreak;
}

public class InteriorCameraController : MonoBehaviour
{
    [SerializeField] private LookPoints lookPoints;
    [SerializeField] private CinemachineCamera interiorCam;
    [SerializeField] private Transform camTarget;

    private Transform currentTarget;

    private void Start()
    {
        if (CameraController.Instance != null)
            CameraController.Instance.SetInteriorCameraController(this);
        else
            Debug.LogWarning("no interior camera found");

            SetTarget(lookPoints.forward);
    }

    public void LookForward()
    {
        SetTarget(lookPoints.forward);
    }

    public bool IsLookingForward()
    {
        return currentTarget == lookPoints.forward;
    }

    public void LookLeft()
    {
        Transform nextTarget = (currentTarget == lookPoints.left) ? lookPoints.forward : lookPoints.left;
        SetTarget(nextTarget);
    }

    public void LookRight()
    {
        Transform nextTarget = (currentTarget == lookPoints.right) ? lookPoints.forward : lookPoints.right;
        SetTarget(nextTarget);
    }

    private void SetTarget(Transform newPoint)
    {
        if (newPoint == null) return;
        //Debug.Log($"Target set to {newPoint.name} by {new System.Diagnostics.StackTrace().GetFrame(1).GetMethod().Name}");

        currentTarget = newPoint;
        camTarget.position = newPoint.position;
        camTarget.rotation = newPoint.rotation;

        interiorCam.LookAt = camTarget;
    }
}