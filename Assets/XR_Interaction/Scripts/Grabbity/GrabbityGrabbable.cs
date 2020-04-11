using System.Collections;
using System.Collections.Generic;
using prvncher.XR_Interaction.FX;
using UnityEngine;

namespace prvncher.XR_Interaction.Grabbity
{
    [RequireComponent(typeof(Renderer))]
    [RequireComponent(typeof(Highlight))]
    [RequireComponent(typeof(Rigidbody))]
    public class GrabbityGrabbable : MonoBehaviour
    {
        private Highlight _highlightComponent = null;
        private Renderer _renderer = null;
        private Rigidbody _rigidbody = null;

        /// <summary>
        /// Rigidbody accessor - guaranteed to be non-null;
        /// </summary>
        public Rigidbody RigidBodyComponent
        {
            get
            {
                if (_rigidbody == null)
                {
                    _rigidbody = GetComponent<Rigidbody>();
                }
                return _rigidbody;
            }
        }

        private bool _isVisibleToCamera = false;
        private bool _isInteractable = false;

        private void Awake()
        {
            _highlightComponent = GetComponent<Highlight>();
            _renderer = GetComponent<Renderer>();
        }

        private void OnDisable()
        {
            DeRegisterGrabbable();
        }

        public void OnObjectFocused()
        {
            if (_highlightComponent != null)
            {
                _highlightComponent.ApplyHighlight();
            }
            Debug.Log($"{gameObject} selected");
        }

        public void OnObjectUnFocused()
        {
            if (_highlightComponent != null)
            {
                _highlightComponent.RemoveHighlight();
            }
            Debug.Log($"{gameObject} unselected");
        }

        private void Update()
        {
            _isVisibleToCamera = _renderer.isVisible;

            // If the object is visible to the camera, then we can grab it.
            if (_isVisibleToCamera)
            {
                RegisterGrabbable();
            }
            // If no longer visible, we de-register this component
            else
            {
                DeRegisterGrabbable();
            }
        }

        private void RegisterGrabbable()
        {
            if (!_isInteractable)
            {
                GrabbitySelector.RegisterGrabbable(this);
                _isInteractable = true;
            }
        }

        private void DeRegisterGrabbable()
        {
            GrabbitySelector.DeRegisterGrabbable(this);
            _isInteractable = false;
        }
    }
}