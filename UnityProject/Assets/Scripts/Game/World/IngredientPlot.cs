using UnityEngine;

public enum IngredientType
{
    Tomato,
    Onion,
    Lettuce,
    Herb,
    Flour,
    Rice
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
    private bool isReady;

    public IngredientType IngredientType => ingredientType;
    public int HarvestYield => harvestYield;
    public float GrowthDuration => growthDuration;
    public float GrowthProgress => growthProgress;
    public bool IsReady => isReady;

    private void OnEnable()
    {
        if (autoStartOnEnable)
            StartGrowing();
    }

    private void Update()
    {
        if (isReady)
            return;

        growthProgress += Time.deltaTime;

        if (growthProgress >= growthDuration)
        {
            isReady = true;
            growthProgress = growthDuration;

            Debug.Log($"[IngredientPlot] {ingredientType} is ready to harvest.");
        }
    }

    public void StartGrowing()
    {
        growthProgress = 0f;
        isReady = false;
    }

    public int Harvest()
    {
        if (!isReady)
            return 0;

        int amount = harvestYield;
        isReady = false;
        growthProgress = 0f;

        Debug.Log($"[IngredientPlot] Harvested {amount} {ingredientType}.");
        return amount;
    }
}
