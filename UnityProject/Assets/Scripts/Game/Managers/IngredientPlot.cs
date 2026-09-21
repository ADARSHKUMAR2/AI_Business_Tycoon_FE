using UnityEngine;

namespace AIBusinessTycoon.Managers 
{
    public enum IngredientType
    {
        Tomato, Onion, Lettuce, Herb, Flour, Rice
    }

    public class IngredientPlot : MonoBehaviour
    {
        [Header("Ingredient")]
        [SerializeField] private IngredientType ingredientType;
        [SerializeField] private int harvestYield = 3;

        [Header("Growth")]
        [SerializeField] private float growthDuration = 30f;
        [SerializeField] private bool autoStartOnEnable = true;

        private float growthProgress;
        private int currentStock;
        private bool isReady;

        public IngredientType IngredientType => ingredientType;
        public int HarvestYield => harvestYield;
        public float GrowthDuration => growthDuration;
        public float GrowthProgress => growthProgress;
        public bool IsReady => isReady;
        public int CurrentStock => currentStock;

        private void OnEnable()
        {
            if (autoStartOnEnable) StartGrowing();
        }

        private void Update()
        {
            if (isReady) return;

            growthProgress += Time.deltaTime;

            if (growthProgress >= growthDuration)
            {
                isReady = true;
                currentStock = harvestYield;
                growthProgress = growthDuration;
                UpdateVisuals();
            }
        }

        public void StartGrowing()
        {
            growthProgress = 0f;
            currentStock = 0;
            isReady = false;
            UpdateVisuals();
        }

        public bool TryTakeOne()
        {
            if (!isReady || currentStock <= 0) return false;

            currentStock--;
            UpdateVisuals();

            if (currentStock == 0) StartGrowing();

            return true;
        }

        public int Harvest()
        {
            if (TryTakeOne()) return 1;
            return 0;
        }

        private void UpdateVisuals()
        {
            TextMesh textMesh = GetComponentInChildren<TextMesh>();
            if (textMesh != null)
            {
                if (isReady) textMesh.text = currentStock.ToString();
                else textMesh.text = "Wait...";
            }
        }
    }
}
