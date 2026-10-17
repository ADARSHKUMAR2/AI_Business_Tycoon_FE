using UnityEngine;
using TMPro;
using System;
using System.Collections.Generic;
using AIBusinessTycoon.Data;

namespace AIBusinessTycoon.Managers
{
    /// <summary>
    /// Attached to the SupplyZone prefab root.
    /// Receives item configuration from StoreInteractionManager after instantiation.
    /// </summary>
    public class SupplyZone : MonoBehaviour
    {
        [Serializable]
        private class ProductSpriteEntry
        {
            public SupplyProduct product;
            public Sprite sprite;
        }

        [Header("References")]
        [SerializeField] private Renderer platformRenderer;
        [SerializeField] private TextMesh itemNameLabel;
        [SerializeField] private Transform productAnchor;
        [SerializeField] private SpriteRenderer productSpriteRenderer;

        [Header("Product Visuals")]
        [SerializeField] private string productResourceFolder = "Generated/Products";
        [SerializeField] private List<ProductSpriteEntry> productSpriteMappings = new List<ProductSpriteEntry>();

        private GameObject _productVisual;

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
                itemNameLabel.text = displayName;

            if (platformRenderer != null)
            {
                // Instance the material so zones don't share the same material asset
                platformRenderer.material = new Material(platformRenderer.sharedMaterial);
                platformRenderer.material.color = zoneColor;
            }

            ApplyProductSprite(itemKey);
            SpawnProductVisual(itemKey);
        }

        private void ApplyProductSprite(string itemKey)
        {
            if (productSpriteRenderer == null)
                return;

            for (int index = 0; index < productSpriteMappings.Count; index++)
            {
                ProductSpriteEntry entry = productSpriteMappings[index];
                if (entry != null && entry.product == itemKey.ToSupplyProduct())
                {
                    productSpriteRenderer.sprite = entry.sprite;
                    return;
                }
            }

            productSpriteRenderer.sprite = null;
        }

        private void SpawnProductVisual(string itemKey)
        {
            if (productAnchor == null)
                return;

            if (_productVisual != null)
                Destroy(_productVisual);

            string resourceName = ToProductResourceName(itemKey);
            GameObject productPrefab = Resources.Load<GameObject>($"{productResourceFolder}/{resourceName}");
            if (productPrefab == null)
            {
                Debug.LogWarning($"[SupplyZone] Could not load product visual at Resources/{productResourceFolder}/{resourceName}.prefab.");
                return;
            }

            _productVisual = Instantiate(productPrefab, productAnchor, false);
            _productVisual.name = $"ProductVisual_{itemKey}";
        }

        private static string ToProductResourceName(string itemKey)
        {
            if (string.IsNullOrWhiteSpace(itemKey))
                return string.Empty;

            string[] words = itemKey.Trim().Split('_', '-');
            for (int index = 0; index < words.Length; index++)
            {
                if (words[index].Length == 0)
                    continue;

                words[index] = char.ToUpperInvariant(words[index][0]) + words[index].Substring(1);
            }

            return string.Join("_", words);
        }
    }
}
