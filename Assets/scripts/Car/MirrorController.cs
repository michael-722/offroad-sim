using UnityEngine;
using Unity.Cinemachine;

public class MirrorController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool flipRotation = false;
    [SerializeField] private float rotationSpeed = 10f;

    private void LateUpdate()
    {
        if (CameraController.Instance == null) return;

        CinemachineCamera targetCam = CameraController.Instance.ActiveCamera;

        if (targetCam != null)
        {
            Vector3 targetPosition = targetCam.transform.position;
            Vector3 direction = targetPosition - transform.position;

            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);

                if (flipRotation)
                    targetRotation *= Quaternion.Euler(0, 180, 0);
                

                if (rotationSpeed > 0)
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
                else
                    transform.rotation = targetRotation;
            }
        }
    }
}