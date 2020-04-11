using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;

namespace prvncher.XR_Interaction.XRToolkitExtensions
{
    [Serializable]
    public class BoolUnityEvent : UnityEvent<bool>
    {
    }

    public class XRControllerInput : MonoBehaviour
    {
        private InputDevice _leftController;
        public InputDevice LeftController => _leftController;

        private InputDevice _rightController;
        public InputDevice RightController => _rightController;

        private bool _leftGripPressedState = false;
        public BoolUnityEvent _leftGripPressed = new BoolUnityEvent();

        private bool _lastRightGripIsPressed = false;
        public BoolUnityEvent _rightGripPressed = new BoolUnityEvent();

        private void OnEnable()
        {
            InputDevices.deviceConnected += RegisterDevices;
            List<InputDevice> devices = new List<InputDevice>();
            InputDevices.GetDevices(devices);
            for (int i = 0; i < devices.Count; i++)
                RegisterDevices(devices[i]);
        }

        private void OnDisable()
        {
            InputDevices.deviceConnected -= RegisterDevices;
        }

        void RegisterDevices(InputDevice connectedDevice)
        {
            if (connectedDevice.isValid)
            {
#if UNITY_2019_3_OR_NEWER
                if ((connectedDevice.characteristics & InputDeviceCharacteristics.Left) != 0)
#else
            if (connectedDevice.role == InputDeviceRole.LeftHanded)
#endif
                {
                    _leftController = connectedDevice;
                }
#if UNITY_2019_3_OR_NEWER
                else if ((connectedDevice.characteristics & InputDeviceCharacteristics.Right) != 0)
#else
            else if (connectedDevice.role == InputDeviceRole.RightHanded)
#endif
                {
                    _rightController = connectedDevice;
                }
            }
        }

        public void LeftHapticsResponse(float duration)
        {
            ApplyLeftHapticsResponse(duration: duration);
        }

        public void ApplyLeftHapticsResponse(uint channel = 0, float amplitude = 0.5f, float duration = 0.5f)
        {
            if (_leftController.isValid)
            {
                _leftController.SendHapticImpulse(channel, amplitude, duration);
            }
        }

        public void RightHapticsResponse(float duration)
        {
            ApplyRightHapticsResponse(duration: duration);
        }

        public void ApplyRightHapticsResponse(uint channel = 0, float amplitude = 0.5f, float duration = 0.5f)
        {
            if (_rightController.isValid)
            {
                _rightController.SendHapticImpulse(channel, amplitude, duration);
            }
        }

        private void Update()
        {
            // Poll Input
            // Grip
            bool leftGripIsPressed = false;
            if (_leftController.isValid)
            {
                _leftController.IsPressed(InputHelpers.Button.Grip, out leftGripIsPressed);
            }
            if (leftGripIsPressed != _leftGripPressedState)
            {
                _leftGripPressed.Invoke(leftGripIsPressed);
                _leftGripPressedState = leftGripIsPressed;
            }

            bool rightGripIsPressed = false;
            if (_rightController.isValid)
            {
                _rightController.IsPressed(InputHelpers.Button.Grip, out rightGripIsPressed);
            }
            if (rightGripIsPressed != _lastRightGripIsPressed)
            {
                _rightGripPressed.Invoke(rightGripIsPressed);
                _lastRightGripIsPressed = rightGripIsPressed;
            }
        }
    }
}
