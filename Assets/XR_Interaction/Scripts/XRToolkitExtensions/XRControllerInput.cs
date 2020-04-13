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

    [Serializable]
    public class Vector3UnityEvent : UnityEvent<Vector3>
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

        bool _lastRightTriggerIsPressed = false;
        public BoolUnityEvent _rightTriggerPressed = new BoolUnityEvent();

        bool _lastLeftTriggerIsPressed = false;
        public BoolUnityEvent _leftTriggerPressed = new BoolUnityEvent();


        private Vector3 _leftVelocity = Vector3.zero;
        public Vector3UnityEvent LeftVelocityUpdate = new Vector3UnityEvent();

        private Vector3 _rightVelocity = Vector3.zero;
        public Vector3UnityEvent RightVelocityUpdate = new Vector3UnityEvent();

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

            // Polling velocity requires the node states... Unity is weird
            List<XRNodeState> nodes = new List<XRNodeState>();
            InputTracking.GetNodeStates(nodes);

            foreach (XRNodeState ns in nodes)
            {
                if (ns.nodeType == XRNode.LeftHand)
                {
                    ns.TryGetVelocity(out _leftVelocity);
                }
                if (ns.nodeType == XRNode.RightHand)
                {
                    ns.TryGetVelocity(out _rightVelocity);
                }
            }

            LeftVelocityUpdate.Invoke(_leftVelocity);
            RightVelocityUpdate.Invoke(_rightVelocity);
            
            // pool input trigger
            bool leftTriggerIsPressed = false;
            if (_leftController.isValid)
            {
                _leftController.IsPressed(InputHelpers.Button.Trigger, out leftTriggerIsPressed);
            }

            if (leftTriggerIsPressed != _lastLeftTriggerIsPressed)
            {
                _leftTriggerPressed.Invoke(leftTriggerIsPressed);
                _lastLeftTriggerIsPressed = leftTriggerIsPressed;
            }
            
            bool rightTriggerIsPressed = false;
            if (_rightController.isValid)
            {
                _rightController.IsPressed(InputHelpers.Button.Trigger, out rightTriggerIsPressed);
            }
            if (rightTriggerIsPressed != _lastRightTriggerIsPressed)
            {
                _rightTriggerPressed.Invoke(rightTriggerIsPressed);
                _lastRightTriggerIsPressed = rightTriggerIsPressed;
            }
        }
    }
}
