using System;
using System.Collections;
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
            if (grabbable == CurrentGrabbable)
            {
                CurrentGrabbable = null;
            }

            if (_launchedRigidbody == grabbable.RigidBodyComponent)
            {
                _launchedRigidbody = null;
            }
        }
        #endregion

        private static GrabbityGrabbable _currentGrabbable = null;

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

        bool _rightTriggerPressed;
        bool _leftTriggerPressed;
        Vector3 _launchOffset = new Vector3(0.0f, 0.5f, 0.0f);

        public bool GripBased = true;

        private void OnDestroy()
        {
            // Clear out static data on destroy
            _grabbables.Clear();
        }

        private bool _objectInFlight = false;

        // Primary hand input is determined by last pressed grip key
        private static bool _rightHandIsPrimary = true;

        private bool PrimaryIsGripping => _rightHandIsPrimary ? _rightGripPressed : _leftGripPressed;
        bool PrimaryIsLockedOn => _rightHandIsPrimary ? _rightTriggerPressed : _leftTriggerPressed;

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
                if (GripBased)
                {
                    OnPrimaryGripReleased();
                }
            }
        }

        private bool _rightGripPressed = false;
        public void RightGripInputChanged(bool isPressed)
        {
            _rightGripPressed = isPressed;
            Debug.Log($"right gripping: {_rightGripPressed}");

            if (isPressed)
            {
                _rightHandIsPrimary = true;
                _rightHandPoseAtGrip = _rightHand.transform.position;
            }
            else if (_rightHandIsPrimary)
            {
                if (GripBased)
                {
                    OnPrimaryGripReleased();
                }
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

        public void RightTriggerInputChanged(bool isPressed)
        {
            _rightTriggerPressed = isPressed;

            if (isPressed)
            {
                _rightHandIsPrimary = true;
                _rightHandPoseAtGrip = _rightHand.transform.position;
            }
        }

        public void LeftTriggerInputChanged(bool isPressed)
        {
            _leftTriggerPressed = isPressed;

            if (isPressed)
            {
                _rightHandIsPrimary = false;
                _leftHandPoseAtGrip = _leftHand.transform.position;
            }
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

        public static GrabbityGrabbable CurrentGrabbable
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
                }
            }
        }

        private float _lastFlickTime = 0f;
        private void Update()
        {
            if (GripBased)
            {
                // If we are not currently gripping, we allow the system to select a new target if the last flick was far enough in the past
                bool blockSelection = _leftHandGrabbing || _rightHandGrabbing;
                if (blockSelection || _objectInFlight)
                {
                    ResetSelection();
                    return;
                }

                if (!PrimaryIsGripping)
                {
                    Selection();
                }
                else
                {
                    HomeToHand();
                }
            }
            else
            {
                bool LockedOn = _rightHandIsPrimary ? _rightTriggerPressed : _leftTriggerPressed;
                if (!LockedOn)
                {
                    Selection();
                }
                else
                {
                    Vector3 primaryHandVelocity = _rightHandIsPrimary ? _rightHandVelocity : _leftHandVelocity;
                    if (primaryHandVelocity.magnitude > 2f)
                    {
                        //HomeToHand();
                        Transform _primaryHand = _rightHandIsPrimary ? _rightHand : _leftHand;
                        if (_currentGrabbable != null)
                        {
                            Launch(_currentGrabbable, _primaryHand);
                        }

                        ResetSelection();
                    }
                }
            }
        }

        private static Rigidbody _launchedRigidbody = null;

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

                if (_rightHandIsPrimary)
                {
                    RightHandSelected.Invoke();
                }
                else
                {
                    LeftHandSelected.Invoke();
                }
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
        /*
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
        */

        private void HomeToHand()
        {
            // Wait a few frames after launch to do anything
            if (Time.time - _lastFlickTime < 0.25f) return;
            if (_launchedRigidbody != null)
            {
                Transform handTransform = _rightHandIsPrimary ? _rightHand : _leftHand;
                Vector3 toHand = handTransform.position - _launchedRigidbody.transform.position;
                float toHandMagnitude = toHand.magnitude;
                // if object is inert or out of bounds, we clear it out
                if (toHandMagnitude > 50f || _launchedRigidbody.velocity.magnitude < 0.2f)
                {
                    _launchedRigidbody = null;
                    return;
                }

                if (toHandMagnitude > 2f || toHandMagnitude < 0.25f) return;

                Vector3 currentVelocity = _launchedRigidbody.velocity;
                Vector3 adjustedVelocity = currentVelocity + ((toHand.normalized * currentVelocity.magnitude - currentVelocity * 2) * (0.1f * Time.deltaTime));
                _launchedRigidbody.velocity = adjustedVelocity;
            }
        }

        private void ResetSelection()
        {
            _objectInFlight = false;
            CurrentGrabbable = null;
            //_launchedRigidbody = null;
        }

        private void OnPrimaryGripReleased()
        {
            if (CurrentGrabbable == null || _objectInFlight) return;

            Transform handTransform = _rightHandIsPrimary ? _rightHand : _leftHand;
            Vector3 toHand = handTransform.position - CurrentGrabbable.transform.position;

            Vector3 handVelocity = _rightHandIsPrimary ? _rightHandVelocity : _leftHandVelocity;
            //if (Vector3.Dot(handVelocity.normalized, toHand.normalized) < 0) return;

            if (handVelocity.magnitude < 0.5f)
            {
                return;
            }

            Launch(CurrentGrabbable, handTransform);

            _objectInFlight = true;
            _lastFlickTime = Time.time;
        }

        private void DoLaunch(GrabbityGrabbable grabbable, Transform target)
        {
            target.transform.position += _launchOffset;
            Transform grabbableTransform = grabbable.transform;

            // think of it as top-down view of vectors: 
            //   we don't care about the y-component(height) of the initial and target position.
            Vector3 projectileXZPos = new Vector3(grabbableTransform.position.x, 0.0f, grabbableTransform.position.z);
            Vector3 targetXZPos = new Vector3(target.position.x, 0.0f, target.position.z);

            // rotate the object to face the target

            Quaternion lookRotation = Quaternion.LookRotation(targetXZPos - grabbableTransform.position);

            //grabbableTransform.LookAt(targetXZPos);

            // shorthands for the formula
            float R = Vector3.Distance(projectileXZPos, targetXZPos);

            float G = Physics.gravity.y;
            float H = target.position.y - grabbableTransform.position.y;


            // If the object is very close by, we make the launch angle more aggressive
            float launchAngle;
            if (H < 0)
            {
                launchAngle = 55f;
            }
            else
            {
                launchAngle = 30f + 10f * Mathf.Clamp(H * 5f, 0f, 2f);
            }

            float tanAlpha = Mathf.Tan(launchAngle * Mathf.Deg2Rad);
            Debug.Log(launchAngle);

            // calculate the local space components of the velocity 
            // required to land the projectile on the target object 

            float grr = G * R * R;
            float hrTan = (H - R * tanAlpha);
            //Debug.Log($"{VzSquarred} {G * R * R} {H - R * tanAlpha}");

            Vector3 globalVelocity;

            // if hrTan is > 0f, it means that the target is almost straight above the projectile.
            // If that happens, we simply shoot the object up
            if (hrTan > 0f)
            {
                globalVelocity = (target.position + Vector3.up * 0.05f - grabbable.transform.position).normalized * 4f;
            }
            else
            {
                float VzSquarred = grr / (2.0f * hrTan);
                float Vz = Mathf.Sqrt(VzSquarred);
                float Vy = tanAlpha * Vz;

                // create the velocity vector in local space and get it in global space
                Vector3 localVelocity = new Vector3(0f, Vy, Vz);
                globalVelocity = lookRotation * localVelocity;
            }

            // launch the object by setting its initial velocity and flipping its state
            grabbable.RigidBodyComponent.velocity = globalVelocity;
            _launchedRigidbody = grabbable.RigidBodyComponent;

            //Debug.Log($"Launched {grabbable.gameObject} with velocity {globalVelocity} and launch angle {launchAngle}");
        }

        private void Launch(GrabbityGrabbable grabbable, Transform target)
        {
            if (_doingLaunch) return;
            StartCoroutine(LaunchEnumerator(grabbable, target));
        }

        private bool _doingLaunch = false;
        private IEnumerator LaunchEnumerator(GrabbityGrabbable grabbable, Transform target)
        {
            _doingLaunch = true;

            float startTime = Time.time;

            float startVerticalOffset = target.transform.position.y - grabbable.transform.position.y;

            float upwardAccelScalar = 1f;
            if (startVerticalOffset < 0)
            {
                upwardAccelScalar = 1 + Mathf.Clamp01(-upwardAccelScalar);
            }
            grabbable.RigidBodyComponent.velocity = Vector3.up * 6f * upwardAccelScalar;

            while (grabbable.transform.position.y < target.transform.position.y + 0.5f && (Time.time - startTime) < 0.5f)
            {
                yield return null;
            }

            DoLaunch(grabbable, target);

            _doingLaunch = false;
        }
    }
}