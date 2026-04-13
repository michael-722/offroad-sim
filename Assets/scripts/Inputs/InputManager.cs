using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour, InputSystemActions.IPlayerActions
{
    public static InputManager Instance;

    private @InputSystemActions _controls;

    //set script execution order to before default time to ensure its initialized before other scripts need to subscribe
    public event Action<float> OnAccelerateEvent;
    public event Action<float> OnBrakeEvent;
    public event Action<float> OnSteeringEvent;

    public event Action OnGearUpEvent;
    public event Action OnGearDownEvent;

    public event Action OnResetEvent;
    public event Action OnPauseEvent;

    public event Action OnCycleCameraEvent;
    public event Action OnCameraLeftEvent;
    public event Action OnCameraRightEvent;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        _controls = new @InputSystemActions();
        _controls.Player.SetCallbacks(this);
    }

    private void OnEnable()
    {
        _controls.Enable();
    }

    private void OnDisable()
    {
        _controls.Disable();
    }

    private void OnDestroy()
    {
        _controls.Dispose();
    }

    public void OnAccelerate(InputAction.CallbackContext context)
    {
        float val = Mathf.Lerp(0, 1, context.ReadValue<float>());
        OnAccelerateEvent?.Invoke(val);
    }

    public void OnBrake(InputAction.CallbackContext context)
    {
        float val = Mathf.Lerp(0, 1, context.ReadValue<float>());
        OnBrakeEvent?.Invoke(val);
    }

    public void OnSteering(InputAction.CallbackContext context)
    {
        float steeringValue = context.ReadValue<float>();
        OnSteeringEvent?.Invoke(steeringValue);
    }
    public void OnGearUp(InputAction.CallbackContext context)
    {
        if (context.performed)
            OnGearUpEvent?.Invoke();
    }

    public void OnGearDown(InputAction.CallbackContext context)
    {
        if (context.performed)
            OnGearDownEvent?.Invoke();
    }

    public void OnReset(InputAction.CallbackContext context)
    {
        if (context.performed)
            OnResetEvent?.Invoke();
    }

    public void OnPause(InputAction.CallbackContext context)
    {
        if (context.performed)
            OnPauseEvent?.Invoke();
    }

    public void OnCycleCamera(InputAction.CallbackContext context)
    {
        if (context.performed)
            OnCycleCameraEvent?.Invoke();
    }

    public void OnCameraLeft(InputAction.CallbackContext context)
    {
        if (context.started)
            OnCameraLeftEvent?.Invoke();
    }

    public void OnCameraRight(InputAction.CallbackContext context)
    {
        if (context.started)
            OnCameraRightEvent?.Invoke();
    }
}