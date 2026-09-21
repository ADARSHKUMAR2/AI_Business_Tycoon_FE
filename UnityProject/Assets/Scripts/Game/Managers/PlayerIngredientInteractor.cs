using UnityEngine;

public class PlayerIngredientInteractor : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactionDistance = 3f;

    [Header("Input")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("Layers")]
    [SerializeField] private LayerMask ingredientPlotLayer;

    private IngredientPlotInteractable currentPlot;

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    private void Update()
    {
        UpdateCurrentTarget();

        if (Input.GetKeyDown(interactKey))
        {
            TryInteract();
        }
    }

    private void UpdateCurrentTarget()
    {
        currentPlot = null;

        if (playerCamera == null)
            return;

        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, ingredientPlotLayer))
        {
            if (hit.collider.TryGetComponent(out IngredientPlotInteractable interactable))
            {
                currentPlot = interactable;
            }
        }
    }

    private void TryInteract()
    {
        if (currentPlot == null)
            return;

        currentPlot.Interact();
    }
}
