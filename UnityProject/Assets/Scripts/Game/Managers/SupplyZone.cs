using UnityEngine;
using TMPro;

namespace AIBusinessTycoon.Managers
{
    /// <summary>
    /// Attached to the SupplyZone prefab root.
    /// Receives item configuration from StoreInteractionManager after instantiation.
    /// </summary>
    public class SupplyZone : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Renderer platformRenderer;
        [SerializeField] private TextMeshProUGUI itemNameLabel;

        /// <summary>
        /// The item key this zone is associated with (e.g. "rice", "dal").
        /// </summary>
        public string ItemKey { get; private set; }

        /// <summary>
        /// Called by StoreInteractionManager right after Instantiate() to configure this zone.
        /// </summary>
        public void Initialize(string itemKey, string displayName, Color zoneColor)
        {
            ItemKey = itemKey;
            gameObject.name = $"SupplyZone_{itemKey}";

            if (itemNameLabel != null)
                itemNameLabel.text = $"📦 {displayName}";

            if (platformRenderer != null)
            {
                // Instance the material so zones don't share the same material asset
                platformRenderer.material = new Material(platformRenderer.sharedMaterial);
                platformRenderer.material.color = zoneColor;
            }
        }
    }
}
