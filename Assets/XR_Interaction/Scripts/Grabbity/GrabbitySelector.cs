using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace prvncher.XR_Interaction.Grabbity
{
    public class GrabbitySelector : MonoBehaviour
    {
        #region Static Helpers
        // Keep a static set of unique grabbables to iterate over and make object selection
        private static HashSet<GrabbityGrabbable> _grabbables = new HashSet<GrabbityGrabbable>();

        /// <summary>
        /// Register to set of grabbables
        /// </summary>
        public static void RegisterGrabbable(GrabbityGrabbable grabbable)
        {
            _grabbables.Add(grabbable);
        }

        /// <summary>
        /// Register to set of grabbables
        /// </summary>
        public static void DeRegisterGrabbable(GrabbityGrabbable grabbable)
        {
            _grabbables.Add(grabbable);
        }
        #endregion

        private GrabbityGrabbable _currentGrabbable = null;

        [Header("Thresholds")]
        [SerializeField]
        private float _initialSelectionAngleThreshold = 45f;

        [SerializeField]
        private float _selectionChangedAngleThreshold = 30f;

        [SerializeField]
        private float _deselectionTreshold = 50f;

        [SerializeField]
        private float _flickDelay = 1f;

        [Header("Pose transforms")]
        [SerializeField]
        private Transform _headpose = null;

        [SerializeField]
        private Transform _rightHand = null;

        [SerializeField]
        private Transform _leftHand = null;

        [Header("Event Responses")]
        public UnityEvent RightHandSelected = new UnityEvent();

        public UnityEvent LeftHandSelected = new UnityEvent();

        private Vector3 _leftHandPoseAtGrip = Vector3.zero;
        private Vector3 _rightHandPoseAtGrip = Vector3.zero;

        private void OnDestroy()
        {
            // Clear out static data on destroy
            _grabbables.Clear();
        }

        private bool _objectInFlight = false;

        // Primary hand input is determined by last pressed grip key
        private bool _rightHandIsPrimary = true;

        private bool PrimaryIsGripping => _rightHandIsPrimary ? _rightGripPressed : _leftGripPressed;

        #region Input management

        private bool _leftGripPressed = false;
        public void LeftGripInputChanged(bool isPressed)
        {
            _leftGripPressed = isPressed;
            if (isPressed)
            {
                _rightHandIsPrimary = false;
                _leftHandPoseAtGrip = _leftHand.transform.position;
            }
        }

        private bool _rightGripPressed = false;
        public void RightGripInputChanged(bool isPressed)
        {
            _rightGripPressed = isPressed;
            if (isPressed)
            {
                _rightHandIsPrimary = true;
                _rightHandPoseAtGrip = _rightHand.transform.position;
            }
        }

        private Vector3 _leftHandVelocity = Vector3.zero;
        public void LeftHandVelocityChanged(Vector3 newVelocity)
        {
            _leftHandVelocity = newVelocity;
        }

        private Vector3 _rightHandVelocity = Vector3.zero;
        public void RightHandVelocityChanged(Vector3 newVelocity)
        {
            _rightHandVelocity = newVelocity;
        }

        #endregion
        // Interaction system checks
        // Managed by XR Direct Interactor events
        #region XR Interaction Management

        private bool _rightHandGrabbing = false;
        public void RightHandGrabbing(bool isGrabbing)
        {
            _rightHandGrabbing = isGrabbing;
        }

        private bool _leftHandGrabbing = false;

        /// <param name="isGrabbing"></param>
        public void LeftHandGrabbing(bool isGrabbing)
        {
            _leftHandGrabbing = isGrabbing;
        }

        #endregion

        public GrabbityGrabbable CurrentGrabbable
        {
            get => _currentGrabbable;
            private set
            {
                if (_currentGrabbable != null)
                {
                    _currentGrabbable.OnObjectUnFocused();
                }

                _currentGrabbable = value;

                if (value != null)
                {
                    _currentGrabbable.OnObjectFocused();
                    if (_rightHandIsPrimary)
                    {
                        RightHandSelected.Invoke();
                    }
                    else
                    {
                        LeftHandSelected.Invoke();
                    }
                }
            }
        }

        private float _lastFlickTime = 0f;
        private void Update()
        {
            // If we are not currently gripping, we allow the system to select a new target if the last flick was far enough in the past
            if (_objectInFlight && (Time.time - _lastFlickTime) > _flickDelay)
            {
                _objectInFlight = false;
            }

            bool blockSelection = _leftHandGrabbing || _rightHandGrabbing;
            if (blockSelection)
            {
                CurrentGrabbable = null;
                return;
            }

            if (!PrimaryIsGripping && !_objectInFlight)
            {
                Selection();
            }
            else if (CurrentGrabbable != null)
            {
                if (!_objectInFlight)
                {
                    DetectFlick();
                }

                if (_objectInFlight)
                {
                    HomeToHand();
                }
            }
        }

        private void Selection()
        {
            Transform grabbingHand = _rightHandIsPrimary ? _rightHand : _leftHand;

            float angleFromCamera = 360;

            Vector3 sourceForward = grabbingHand.transform.forward;
            Vector3 sourcePosition = grabbingHand.transform.position;

            GrabbityGrabbable newGrabbable = null;

            float selectionThreshold = _currentGrabbable == null ? _initialSelectionAngleThreshold : _selectionChangedAngleThreshold;

            // Iterate over visible grabbables
            foreach (var grabbable in _grabbables)
            {
                // We consider a grabbable as viable if
                // 1 - Angle between controller forward, and object to controller vector, is less than selectionThreshold.
                // 2 - Angle is the smallest available among all grabbable objects

                Vector3 toGrabbable = grabbable.transform.position - sourcePosition;
                float angleWithCamera = Vector3.Angle(toGrabbable, sourceForward);

                if (angleWithCamera < selectionThreshold && angleWithCamera < angleFromCamera)
                {
                    angleFromCamera = angleWithCamera;
                    newGrabbable = grabbable;
                }
            }

            // If we have a new grabbable, we deselect the old one
            if (newGrabbable != null && newGrabbable != _currentGrabbable)
            {
                CurrentGrabbable = newGrabbable;
            }
            // If we have no grabbable, we assess if we should terminate all selection
            else if (newGrabbable == null && _currentGrabbable != null)
            {
                Vector3 toGrabbable = _currentGrabbable.transform.position - sourcePosition;
                float angleWithCamera = Vector3.Angle(toGrabbable, sourceForward);
                if (angleWithCamera > _deselectionTreshold)
                {
                    CurrentGrabbable = null;
                }
            }
        }

        private float _flickMagnitude = 0f;
        private void DetectFlick()
        {
            Vector3 handVelocity = _rightHandIsPrimary ? _rightHandVelocity : _leftHandVelocity;

            float velocityMagnitude = handVelocity.magnitude;
            if (velocityMagnitude > 3)
            {
                _flickMagnitude = velocityMagnitude;
                _objectInFlight = true;

                if (CurrentGrabbable != null)
                {
                    Vector3 positionTarget = _rightHandIsPrimary ? _rightHand.transform.position : _leftHand.transform.position;
                    CurrentGrabbable.RigidBodyComponent.velocity =
                        (positionTarget - CurrentGrabbable.transform.position).normalized * _flickMagnitude;
                }
            }

            if (_objectInFlight)
            {
                // If detect flick
                _lastFlickTime = Time.time;
            }
        }

        private void HomeToHand()
        {
            
        }
    }
}