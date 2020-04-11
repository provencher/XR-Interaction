using System;
using System.Collections.Generic;
using UnityEngine;

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


        [SerializeField]
        private Transform _headpose = null;

        [SerializeField]
        private Transform _rightHand = null;

        [SerializeField]
        private Transform _leftHand = null;

        private void OnDestroy()
        {
            // Clear out static data on destroy
            _grabbables.Clear();
        }

        private bool _leftGripPressed = false;
        public void LeftGripInputChanged(bool isPressed)
        {
            _leftGripPressed = isPressed;
            Debug.Log($"Left Grip {isPressed}");
        }

        private bool _rightGripPressed = false;
        public void RightGripInputChanged(bool isPressed)
        {
            _rightGripPressed = isPressed;
            Debug.Log($"Right Grip {isPressed}");
        }

        private void Update()
        {
            Transform grabbingHand = null;

            float angleFromCamera = 360;

            Vector3 cameraForward = _headpose.transform.forward;
            Vector3 cameraPosition = _headpose.transform.position;

            GrabbityGrabbable newGrabbable = null;

            // Iterate over visible grabbables
            foreach (var grabbable in _grabbables)
            {
                // We consider a grabbable as viable if
                // 1 - 

                Vector3 toGrabbable = grabbable.transform.position - cameraPosition;
                float angleWithCamera = Vector3.Angle(toGrabbable, cameraForward);

                if (angleWithCamera < 30 && angleWithCamera < angleFromCamera)
                {
                    angleFromCamera = angleWithCamera;
                    newGrabbable = grabbable;
                }
            }

            if (newGrabbable != null && newGrabbable != _currentGrabbable)
            {
                if (_currentGrabbable != null)
                {
                    _currentGrabbable.OnObjectUnFocused();
                }
                _currentGrabbable = newGrabbable;
                _currentGrabbable.OnObjectFocused();
            }
            else if (newGrabbable == null && _currentGrabbable != null)
            {
                Vector3 toGrabbable = _currentGrabbable.transform.position - cameraPosition;
                float angleWithCamera = Vector3.Angle(toGrabbable, cameraForward);
                if (angleWithCamera > 50)
                {
                    _currentGrabbable.OnObjectUnFocused();
                    _currentGrabbable = null;
                }
            }
        }
    }
}