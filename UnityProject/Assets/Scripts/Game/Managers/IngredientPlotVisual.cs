using UnityEngine;
using AIBusinessTycoon.Managers; 

[RequireComponent(typeof(IngredientPlot))]
public class IngredientPlotVisual : MonoBehaviour
{
    [SerializeField] private Renderer plantRenderer;
    [SerializeField] private Color readyColor = Color.green;
    [SerializeField] private Color growingColor = Color.gray;

    private IngredientPlot plot;

    private void Awake()
    {
        plot = GetComponent<IngredientPlot>();
    }

    private void Update()
    {
        if (plot == null)
            return;

        if (plantRenderer == null)
            return;

        plantRenderer.material.color = plot.IsReady ? readyColor : growingColor;
    }
}
