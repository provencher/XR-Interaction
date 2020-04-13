using UnityEngine;

namespace prvncher.XR_Interaction.FX
{
    [RequireComponent(typeof(Renderer))]
    public class Highlight : MonoBehaviour
    {
        private Renderer _rendererComponent = null;

        private Material _materialInstance = null;

        static string _albedoColor = "AlbedoTintColor";
        static string _highEnabled = "BOOLEAN_C8256B8_ON";

        [SerializeField]
        [Tooltip("Color applied as highlight color leveraging the emission color property")]
        private Color _highlightColor = Color.cyan;

        private Material MaterialInstance
        {
            get
            {
                if (_rendererComponent == null)
                {
                    _rendererComponent = GetComponent<Renderer>();
                }

                if (_materialInstance == null)
                {
                    // Create duplicate of material to enable modifications
                    _materialInstance = _rendererComponent.material;
                }
                return _materialInstance;
            }
        }

        private void OnDestroy()
        {
            // Clean up created materials
            if (_materialInstance != null)
            {
                Destroy(_materialInstance);
                _materialInstance = null;
            }
        }

        public void ApplyHighlight()
        {
            MaterialInstance.SetColor(_albedoColor, _highlightColor);
            MaterialInstance.EnableKeyword(_highEnabled);
        }

        public void RemoveHighlight()
        {
            MaterialInstance.SetColor(_albedoColor, Color.white);
            MaterialInstance.DisableKeyword(_highEnabled);
        }
    }
}
