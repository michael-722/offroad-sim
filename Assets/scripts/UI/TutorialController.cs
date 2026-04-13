using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.HID;

public class TutorialController : MonoBehaviour
{
    [SerializeField] private Transform carSpawnPoint;

    [SerializeField] private float stepCompletePause = 0.6f;
    [SerializeField] private float tutoiralEndTime = 10f;

    [Header("UI")]
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private GameObject tutorialPanel;

    public enum InputDevice { Unknown, KeyboardMouse, Gamepad, WheelAndPedals }

    private InputDevice _currentDevice = InputDevice.Unknown;

    private bool tutorialActive = false;

    private float lastThrottle;
    private float lastBrake;
    private float lastSteer;
    private bool gearUpFired;
    private bool gearDownFired;
    private bool resetFired;
    private bool cameraCycleFired;
    private bool cameraLeft;
    private bool cameraRight;

    private void OnEnable()
    {
        InputSystem.onDeviceChange += OnDeviceChanged;

        if (InputManager.Instance == null) return;
        InputManager.Instance.OnAccelerateEvent += v => lastThrottle = v;
        InputManager.Instance.OnBrakeEvent += v => lastBrake = v;
        InputManager.Instance.OnSteeringEvent += v => lastSteer = v;
        InputManager.Instance.OnGearUpEvent += () => gearUpFired = true;
        InputManager.Instance.OnGearDownEvent += () => gearDownFired = true;
        InputManager.Instance.OnResetEvent += () => resetFired = true;
        InputManager.Instance.OnCycleCameraEvent += () => cameraCycleFired = true;
        InputManager.Instance.OnCameraRightEvent += () => cameraRight = true;
        InputManager.Instance.OnCameraLeftEvent += () => cameraLeft = true;
    }

    private void OnDisable()
    {
        InputSystem.onDeviceChange -= OnDeviceChanged;

        if (InputManager.Instance == null) return;
        InputManager.Instance.OnAccelerateEvent -= v => lastThrottle = v;
        InputManager.Instance.OnBrakeEvent -= v => lastBrake = v;
        InputManager.Instance.OnSteeringEvent -= v => lastSteer = v;
        InputManager.Instance.OnGearUpEvent -= () => gearUpFired = true;
        InputManager.Instance.OnGearDownEvent -= () => gearDownFired = true;
        InputManager.Instance.OnResetEvent -= () => resetFired = true;
        InputManager.Instance.OnCycleCameraEvent -= () => cameraCycleFired = true;
        InputManager.Instance.OnCameraRightEvent -= () => cameraRight = true;
        InputManager.Instance.OnCameraLeftEvent -= () => cameraLeft = true;
    }

    private void Start()
    {
        StartTutorial();
    }

    private void Update()
    {
        if (!tutorialActive) return;

        DetectDeviceFromCurrentInput();
    }

    public void StartTutorial()
    {
        if (tutorialActive) return;
        tutorialActive = true;
        tutorialPanel.SetActive(true);
        StartCoroutine(TutorialRoutine());
    }

    public void ForceStop()
    {
        StopAllCoroutines();
        FinishTutorial();
    }

    private IEnumerator TutorialRoutine()
    {
        //device detection
        yield return RunStep(
            "Welcome to basic training! Press any button to start.",
            () => _currentDevice != InputDevice.Unknown
        );

        //gas
        ClearFlags();
        yield return RunStep(
            Prompt("Apply throttle to move forward.", "W", "Right trigger", "Throttle pedal"),
            () => lastThrottle > 0.1f
        );

        //breaking
        ClearFlags();
        yield return RunStep(
            Prompt("Now apply the brake.", "S", "Left trigger", "Brake pedal"),
            () => lastBrake > 0.1f
        );

        //steering
        ClearFlags();
        yield return RunStep(
            Prompt("Steer left and right.", "A / D", "Left stick", "Steering wheel"),
            () => Mathf.Abs(lastSteer) > 0.15f
        );

        //shift up
        ClearFlags();
        yield return RunStep(
            Prompt("Shift up a gear.", "SHIFT", "Right bumber", "Right paddle"),
            () => gearUpFired
        );

        //shift down
        ClearFlags();
        yield return RunStep(
            Prompt("Shift down a gear.", "SPACE", "Left bumper", "Left paddle"),
            () => gearDownFired
        );

        //resetting
        ClearFlags();
        yield return RunStep(
            Prompt("If you get stuck, you can reset your position.", "R", "gamepad north", "D-pad up === NEED TO CHECK THIS"),
            () => resetFired
        );

        //camera left
        ClearFlags();
        yield return RunStep(
            Prompt("Look left", "X", "D-pad left", "D-pad left"),
            () => cameraLeft
            );

        //camera right
        ClearFlags();
        yield return RunStep(
            Prompt("Look left", "C", "D-pad right", "D-pad right"),
            () => cameraRight
            );

        //camera mode
        ClearFlags();
        yield return RunStep(
            Prompt("Cycle through camera views.", "Z", "D-pad down", "D-pad down"),
            () => cameraCycleFired
        );

        //free practice
        ClearFlags();
        float freeDriveTimer = 0f;
        yield return RunStep(
            "Good work! Have a free drive to get comfortable. The tutorial will end shortly.",
            () =>
            {
                freeDriveTimer += Time.deltaTime;
                return freeDriveTimer >= tutoiralEndTime;
            }
        );

        FinishTutorial();
    }

    private IEnumerator RunStep(string message, Func<bool> completionCondition)
    {
        SetInstructionText(message);

        while (!completionCondition())
            yield return null;

        yield return new WaitForSeconds(stepCompletePause);
    }

    private string Prompt(string baseMessage,
                          string kbmHint,
                          string gamepadHint,
                          string wheelHint)
    {
        string hint = _currentDevice switch
        {
            InputDevice.KeyboardMouse => kbmHint,
            InputDevice.Gamepad => gamepadHint,
            InputDevice.WheelAndPedals => wheelHint,
            _ => $"{kbmHint} / {gamepadHint} / {wheelHint}"
        };

        return $"{baseMessage}\n<color=#ff0000>[{hint}]</color>";
    }


    private void DetectDeviceFromCurrentInput()
    {
        foreach (var device in InputSystem.devices)
        {
            Debug.Log(device.name);
            if (device is Keyboard || device is Mouse)
            {
                if (IsKeyboardOrMouseActive())
                {
                    SetDevice(InputDevice.KeyboardMouse);
                    return;
                }
            }

            if (device is Gamepad && IsDeviceActive(device))
            {
                SetDevice(InputDevice.Gamepad);
                return;
            }

            //only works for the moza R3 wheel
            if (device.name.Contains("R3") && IsDeviceActive(device))
            {
                SetDevice(InputDevice.WheelAndPedals);
                return;
            }
        }
    }

    private bool IsKeyboardOrMouseActive()
    {
        return Keyboard.current != null && Keyboard.current.anyKey.isPressed
            || Mouse.current != null && Mouse.current.leftButton.isPressed;
    }

    private bool IsDeviceActive(UnityEngine.InputSystem.InputDevice device)
    {
        foreach (var control in device.allControls)
        {
            if (control is UnityEngine.InputSystem.Controls.ButtonControl btn && btn.isPressed)
                return true;
            if (control is UnityEngine.InputSystem.Controls.AxisControl axis
                && Mathf.Abs(axis.ReadValue()) > 0.2f)
                return true;
        }
        return false;
    }

    private void OnDeviceChanged(UnityEngine.InputSystem.InputDevice device, InputDeviceChange change)
    {
        if (change == InputDeviceChange.Reconnected || change == InputDeviceChange.Added)
            Debug.Log($"device changed: {device.displayName} -> {change}");
    }

    private void SetDevice(InputDevice device)
    {
        if (_currentDevice == device) return;
        _currentDevice = device;

        Debug.Log($"input device set to: {_currentDevice}");
    }

    private void ClearFlags()
    {
        gearUpFired = false;
        gearDownFired = false;
        resetFired = false;
        cameraCycleFired = false;
    }

    private void SetInstructionText(string text)
    {
        if (instructionText != null)
            instructionText.text = text;
    }

    private void FinishTutorial()
    {
        tutorialActive = false;

        tutorialPanel.SetActive(false);

        //return to menu
    }
}