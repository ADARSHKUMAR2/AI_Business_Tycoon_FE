using UnityEngine;
using TMPro;

public class GrowthTimer : MonoBehaviour
{
    [SerializeField] private IngredientPlot plot;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private string readyText = "Ready";

    private void Update()
    {
        if (plot == null)
            return;

        if (timerText == null)
            return;

        if (plot.IsReady)
        {
            timerText.text = readyText;
            return;
        }

        float remaining = Mathf.Max(0f, plot.GrowthDuration - plot.GrowthProgress);
        timerText.text = Mathf.CeilToInt(remaining).ToString();
    }
}
