using UnityEngine;

public class SteeringWheel : MonoBehaviour
{
    [SerializeField] private Transform steeringWheelMesh;

    [SerializeField] private float maxSteerAngle = 180f;//275 for f150 --- need to test with wheel 
    [SerializeField] private float smoothSpeed = 10f;

    private CarController controller;
    private float smoothedAngle;
    private Vector3 baseEuler;

    private void Start()
    {
        maxSteerAngle = SessionData.Instance.FFBlimit;

        controller = GetComponentInParent<CarController>();
        baseEuler = steeringWheelMesh.localEulerAngles;
    }

    public void UpdateSteerLimit()
    {
        maxSteerAngle = SessionData.Instance.FFBlimit;
    }

    private void Update()
    {
        //if ()
        //    return;

        float targetAngle = controller.steerInput * maxSteerAngle;
        smoothedAngle = Mathf.Lerp(smoothedAngle, targetAngle, Time.deltaTime * smoothSpeed);

        steeringWheelMesh.localEulerAngles = new Vector3(baseEuler.x, baseEuler.y, baseEuler.z + smoothedAngle);
    }
}