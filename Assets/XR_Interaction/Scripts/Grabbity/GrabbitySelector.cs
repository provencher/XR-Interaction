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
            //Debug.Log($"left gripping: {_leftGripPressed}");

            if (isPressed)
            {
                _rightHandIsPrimary = false;
                _leftHandPoseAtGrip = _leftHand.transform.position;
            }
            else if (!_rightHandIsPrimary)
            {
                OnPrimaryGripReleased();
            }
        }

        private bool _rightGripPressed = false;
        public void RightGripInputChanged(bool isPressed)
        {
            _rightGripPressed = isPressed;
            //Debug.Log($"right gripping: {_rightGripPressed}");

            if (isPressed)
            {
                _rightHandIsPrimary = true;
                _rightHandPoseAtGrip = _rightHand.transform.position;
            }
            else if (_rightHandIsPrimary)
            {
                OnPrimaryGripReleased();
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

        private GrabbityGrabbable _selectedGrabbable = null;

        private float _lastFlickTime = 0f;
        private void Update()
        {
            // If we are not currently gripping, we allow the system to select a new target if the last flick was far enough in the past
            bool blockSelection = _leftHandGrabbing || _rightHandGrabbing;
            if (blockSelection || (_objectInFlight && (Time.time - _lastFlickTime) > _flickDelay))
            {
                ResetSelection();
                return;
            }

            // Debug.Log($"gripping: {PrimaryIsGripping}");

            if (PrimaryIsGripping && CurrentGrabbable != null)
            {
                _selectedGrabbable = CurrentGrabbable;
                //Debug.Log($"{_selectedGrabbable} selected");
            }

            if (!PrimaryIsGripping && _selectedGrabbable == null && !_objectInFlight)
            {
                Selection();
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

        private void HomeToHand()
        {
            if ((Time.time - _lastFlickTime) < 0.25f) return;
            if (_selectedGrabbable == null) return;

            Vector3 handPose = _rightHandIsPrimary ? _rightHand.position : _leftHand.position;
            Vector3 toHand = handPose - _selectedGrabbable.transform.position;

            // Ensure the object is front of the user - if not we stop what we're doing
            if (Vector3.Dot(toHand, _headpose.forward) < 0)
            {
                ResetSelection();
                return;
            }

            float toHandMagnitude = toHand.magnitude;
            Vector3 currentVelocity = _selectedGrabbable.RigidBodyComponent.velocity;

            Vector3 newVelocityDirection = currentVelocity;
            if (toHandMagnitude < 0.75f)
            {
                // Dampen velocity
                newVelocityDirection -= (0.05f * Time.deltaTime * toHand);
            }

            _selectedGrabbable.RigidBodyComponent.velocity = newVelocityDirection;
        }

        void ParabolaToHand()
        {
            Vector3 handPose = _rightHandIsPrimary ? _rightHand.position : _leftHand.position;
            Transform handTransform = _rightHandIsPrimary ? _rightHand : _leftHand;

            Launch(handTransform);
        }

        private void ResetSelection()
        {
            _objectInFlight = false;
            CurrentGrabbable = null;
            _selectedGrabbable = null;
        }

        private void OnPrimaryGripReleased()
        {
            if (_selectedGrabbable != null && !_objectInFlight)
            {
                Transform handTransform = _rightHandIsPrimary ? _rightHand : _leftHand;
                Vector3 toHand = handTransform.position - _selectedGrabbable.transform.position;

                Vector3 handVelocity = _rightHandIsPrimary ? _rightHandVelocity : _leftHandVelocity;
                //if (Vector3.Dot(handVelocity.normalized, toHand.normalized) < 0) return;

                float velocityMagnitude = handVelocity.magnitude;
                if (velocityMagnitude < 0.5f)
                {
                    _selectedGrabbable.RigidBodyComponent.velocity = handVelocity;
                }
                else
                {
                    Launch(handTransform);
                }

                _objectInFlight = true;
                _lastFlickTime = Time.time;
            }
        }

        private void Launch(Transform target, float launchAngle = 45f)
        {
            Transform grabbableTransform = _selectedGrabbable.transform;

            // think of it as top-down view of vectors: 
            //   we don't care about the y-component(height) of the initial and target position.
            Vector3 projectileXZPos = new Vector3(grabbableTransform.position.x, 0.0f, grabbableTransform.position.z);
            Vector3 targetXZPos = new Vector3(target.position.x, 0.0f, target.position.z);

            // rotate the object to face the target
            //transform.LookAt(targetXZPos);

            // rotate the object to face the target
            grabbableTransform.LookAt(targetXZPos);

            // shorthands for the formula
            float R = Vector3.Distance(projectileXZPos, targetXZPos);
            float G = Physics.gravity.y;
            float tanAlpha = Mathf.Tan(launchAngle * Mathf.Deg2Rad);
            float H = Mathf.Abs(target.position.y - grabbableTransform.position.y);

            // calculate the local space components of the velocity 
            // required to land the projectile on the target object 
            float divisor = (2.0f * (H - R * tanAlpha));

            float Vz = Mathf.Sqrt(Mathf.Abs(G * R * R / divisor));
            float Vy = tanAlpha * Vz;

            // create the velocity vector in local space and get it in global space
            Vector3 localVelocity = new Vector3(0f, Vy, Vz);
            Vector3 globalVelocity = grabbableTransform.TransformDirection(localVelocity);

            // launch the object by setting its initial velocity and flipping its state
            _selectedGrabbable.RigidBodyComponent.velocity = globalVelocity;
        }
    }
}