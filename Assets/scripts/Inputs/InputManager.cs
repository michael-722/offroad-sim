using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class InputManager : MonoBehaviour, InputSystemActions.IPlayerActions
{
    public static InputManager Instance;

    private @InputSystemActions _controls;

    public bool IsControllerMode { get; private set; }
    public event Action<bool> OnInputSourceChanged;

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
        InputSystem.onActionChange += HandleInputSourceChange;
    }

    private void OnDisable()
    {
        _controls.Disable();
        InputSystem.onActionChange -= HandleInputSourceChange;
    }

    private void OnDestroy()
    {
        _controls.Dispose();
    }

    private void Start()
    {
        //default to controller/wheel if connected
        if (Gamepad.all.Count > 0 || Joystick.all.Count > 0)
            SetInputMode(true);
        else
            SetInputMode(false);
    }

    private void HandleInputSourceChange(object obj, InputActionChange change)
    {
        if (change != InputActionChange.ActionPerformed)
            return;

        var inputAction = (InputAction)obj;
        var control = inputAction.activeControl;
        var device = control.device;

        bool isControllerDevice = device is Gamepad || device is Joystick;
        bool isKeyboardMouse = device is Keyboard || device is Mouse;

        //check if should change to wheel/controller
        if (isControllerDevice)
        {
            if (IsControllerMode) return;//already in controller mode

            //ignore pedals for switching modes 
            if (inputAction.name == "Accelerate" || inputAction.name == "Brake") return;

            //button press set trigger
            if (control is ButtonControl button && button.isPressed)
            {
                SetInputMode(true);
                return;
            }

            //stick or steering whele set triggers
            if (control is AxisControl axis && Mathf.Abs(axis.ReadValue()) > 0.3f)
            {
                SetInputMode(true);
                return;
            }
            if (control is Vector2Control stick && stick.ReadValue().magnitude > 0.5f)
            {
                SetInputMode(true);
                return;
            }
        }
        //check if should change to mk
        else if (isKeyboardMouse)
        {
            if (!IsControllerMode) return;//already in mk mode

            //keyboard press reset trigger
            if (control is ButtonControl button && button.isPressed)
            {
                SetInputMode(false);
                return;
            }

            //mouse movement reset trigger
            if (device is Mouse mouse && control == mouse.delta)
            {
                if (mouse.delta.ReadValue().magnitude > 3.0f)
                    SetInputMode(false);
            }
        }
    }

    private void SetInputMode(bool controllerMode)
    {
        IsControllerMode = controllerMode;

        if (IsControllerMode)
        {
            //controller/wheel mode
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;//hides cursor
        }
        else
        {
            //mk mode
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;//shows cursor
        }

        OnInputSourceChanged?.Invoke(IsControllerMode);
        Debug.Log($"switched to: {(IsControllerMode ? "controller/wheel" : "mk")}");
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