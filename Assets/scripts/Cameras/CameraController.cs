using UnityEngine;
using Unity.Cinemachine;
using System.Collections.Generic;

[System.Serializable]
public struct CameraGroup
{
    public CinemachineCamera leftCam;
    public CinemachineCamera followCam;
    public CinemachineCamera rightCam;
    public CinemachineCamera interiorCam;
}

public class CameraController : MonoBehaviour
{
    public static CameraController Instance;

    [SerializeField] private CameraGroup cameras;

    private int currentExteriorIndex = 1;
    private bool flippedPanning = false;
    private List<CinemachineCamera> exteriorCycleList;
    private InteriorCameraController interiorCameraController;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        exteriorCycleList = new List<CinemachineCamera>
        {
            cameras.leftCam,
            cameras.followCam,
            cameras.rightCam
        };

        SetCamera(cameras.followCam);

        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnCycleCameraEvent += CycleCamera;
            InputManager.Instance.OnCameraLeftEvent += CameraLeft;
            InputManager.Instance.OnCameraRightEvent += CameraRight;
        }
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnCycleCameraEvent -= CycleCamera;
            InputManager.Instance.OnCameraLeftEvent -= CameraLeft;
            InputManager.Instance.OnCameraRightEvent -= CameraRight;
        }
    }

    private void SetCamera(CinemachineCamera camToActivate)
    {
        cameras.leftCam.Priority = 10;
        cameras.followCam.Priority = 10;
        cameras.rightCam.Priority = 10;
        cameras.interiorCam.Priority = 10;

        if (camToActivate != null)
            camToActivate.Priority = 20;
    }

    private void CameraLeft()
    {
        if (cameras.interiorCam.Priority == 20)
            interiorCameraController.LookLeft();
        else
        {
            SetCamera(cameras.leftCam);
            currentExteriorIndex = 0;
        }
    }

    private void CameraRight()
    {
        if (cameras.interiorCam.Priority == 20)
            interiorCameraController.LookRight();
        else
        { 
            SetCamera(cameras.rightCam);
            currentExteriorIndex = 2;
        }
    }

    private void CycleCamera()
    {
        if (cameras.interiorCam.Priority == 20)
        {
            CycleInteriorCamera();
            return;
        }

        if (flippedPanning) currentExteriorIndex--;
        else currentExteriorIndex++;

        if (currentExteriorIndex >= exteriorCycleList.Count - 1 || currentExteriorIndex <= 0)
            flippedPanning = !flippedPanning;

        SetCamera(exteriorCycleList[currentExteriorIndex]);
    }

    private void CycleInteriorCamera()
    {
        bool isInteriorCamActive = cameras.interiorCam.Priority > 10;

        if (!isInteriorCamActive)
        {
            SetCamera(cameras.interiorCam);
        }
        else if (interiorCameraController.IsLookingForward())
        {
            SetCamera(cameras.followCam);
            currentExteriorIndex = 1;
        }

        interiorCameraController.LookForward();
    }

    public void SetInteriorCameraController(InteriorCameraController controller)
    {
        interiorCameraController = controller;
    }
}