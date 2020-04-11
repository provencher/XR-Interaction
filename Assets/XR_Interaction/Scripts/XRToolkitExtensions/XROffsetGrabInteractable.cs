using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace prvncher.XR_Interaction.XRToolkitExtensions
{
    /// <summary>
    /// Allows object to be held at the grab point, rather than object center
    /// Code taken from: https://www.youtube.com/watch?v=-a36GpPkW-Q&feature=emb_title
    /// </summary>
    public class XROffsetGrabInteractable : XRGrabInteractable
    {
        private Vector3 interactorPosition = Vector3.zero;
        private Quaternion interactorRotation = Quaternion.identity;

        protected override void OnSelectEnter(XRBaseInteractor interactor)
        {
            base.OnSelectEnter(interactor);
            StoreInteractor(interactor);
            MatchAttachmentPoints(interactor);
        }

        protected override void OnSelectExit(XRBaseInteractor interactor)
        {
            base.OnSelectExit(interactor);
            ResetAttachementPoints(interactor);
            ResetInteractor();
        }

        private void StoreInteractor(XRBaseInteractor interactor)
        {
            interactorPosition = interactor.attachTransform.localPosition;
            interactorRotation = interactor.attachTransform.localRotation;
        }

        private void ResetInteractor()
        {
            interactorPosition = Vector3.zero;
            interactorRotation = Quaternion.identity;
        }

        private void MatchAttachmentPoints(XRBaseInteractor interactor)
        {
            bool hasAttach = attachTransform != null;
            interactor.attachTransform.position = hasAttach ? attachTransform.position : transform.position;
            interactor.attachTransform.rotation = hasAttach ? attachTransform.rotation : transform.rotation;
        }

   
        private void ResetAttachementPoints(XRBaseInteractor interactor)
        {
            interactor.attachTransform.localPosition = interactorPosition;
            interactor.attachTransform.localRotation = interactorRotation;
        }
    }
}
