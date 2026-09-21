using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AIBusinessTycoon.Editor
{
    public class BuildMenuAutoSetup : EditorWindow
    {
        private const string MenuName = "AIBusinessTycoon/Build Menu Auto Setup";

        [MenuItem(MenuName)]
        public static void ShowWindow()
        {
            GetWindow<BuildMenuAutoSetup>("Build Menu Auto Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("Auto-create build menu", EditorStyles.boldLabel);

            if (GUILayout.Button("Create Build Menu"))
            {
                CreateBuildMenu();
            }
        }

        private static void CreateBuildMenu()
        {
            var root = GameObject.Find("BuildMenu");
            if (root == null)
            {
                root = new GameObject("BuildMenu");
            }

            var canvas = root.GetComponentInChildren<Canvas>();
            if (canvas == null)
            {
                var canvasGO = new GameObject("BuildMenuCanvas");
                canvasGO.transform.SetParent(root.transform, false);
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                var scaler = canvasGO.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);

                canvasGO.AddComponent<GraphicRaycaster>();
            }

            var panelGO = GameObject.Find("BuildMenuPanel");
            if (panelGO == null)
            {
                panelGO = new GameObject("BuildMenuPanel");
                panelGO.transform.SetParent(canvas.transform, false);
            }

            var panelImage = panelGO.GetComponent<Image>();
            if (panelImage == null)
            {
                panelImage = panelGO.AddComponent<Image>();
                panelImage.color = new Color(0.08f, 0.08f, 0.12f, 0.94f);
            }

            var rect = panelGO.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = panelGO.AddComponent<RectTransform>();
            }

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(700f, 500f);
            rect.anchoredPosition = Vector2.zero;

            var layout = panelGO.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = panelGO.AddComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(20, 20, 20, 20);
                layout.spacing = 12;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
            }

            var buttonList = panelGO.GetComponent<ContentSizeFitter>();
            if (buttonList == null)
            {
                panelGO.AddComponent<ContentSizeFitter>();
            }

            // Title
            var titleGO = GameObject.Find("BuildMenuTitle");
            if (titleGO == null)
            {
                titleGO = new GameObject("BuildMenuTitle");
                titleGO.transform.SetParent(panelGO.transform, false);
            }

            var titleText = titleGO.GetComponent<TextMeshProUGUI>();
            if (titleText == null)
            {
                titleText = titleGO.AddComponent<TextMeshProUGUI>();
            }

            titleText.text = "BUILD MENU";
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontSize = 32;
            titleText.color = Color.white;
            titleText.fontStyle = FontStyles.Bold;

            var titleRect = titleGO.GetComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(0, 50);

            // Grid parent
            var gridGO = GameObject.Find("BuildGrid");
            if (gridGO == null)
            {
                gridGO = new GameObject("BuildGrid");
                gridGO.transform.SetParent(panelGO.transform, false);
            }

            var gridLayout = gridGO.GetComponent<GridLayoutGroup>();
            if (gridLayout == null)
            {
                gridLayout = gridGO.AddComponent<GridLayoutGroup>();
                gridLayout.cellSize = new Vector2(300, 180);
                gridLayout.spacing = new Vector2(20, 20);
                gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                gridLayout.constraintCount = 2;
                gridLayout.childAlignment = TextAnchor.MiddleCenter;
            }

            var gridRect = gridGO.GetComponent<RectTransform>();
            gridRect.sizeDelta = new Vector2(620, 360);

            // Create cards
            CreateCard(panelGO.transform, "KiranaCard", "Kirana Store", "₹5,000", "Basic grocery shop for daily needs", "kiranaButton");
            CreateCard(panelGO.transform, "PizzaCard", "Pizza Outlet", "₹15,000", "Fast food restaurant with delivery", "pizzaButton");
            CreateCard(panelGO.transform, "CafeCard", "Cafe", "₹25,000", "Cozy coffee shop with seating", "cafeButton");
            CreateCard(panelGO.transform, "RestaurantCard", "Restaurant", "₹18,000", "Ingredient kitchen with harvest & stock", "restaurantButton");

            // BuildMenuManager
            var buildMenuManager = root.GetComponent<AIBusinessTycoon.UI.BuildMenuManager>();
            if (buildMenuManager == null)
            {
                buildMenuManager = root.AddComponent<AIBusinessTycoon.UI.BuildMenuManager>();
            }

            var so = new SerializedObject(buildMenuManager);

            // Assign references
            var menuPanelProp = so.FindProperty("menuPanel");
            menuPanelProp.objectReferenceValue = panelGO;

            var closeButton = panelGO.transform.Find("CloseButton")?.GetComponent<Button>();
            if (closeButton != null)
            {
                so.FindProperty("closeButton").objectReferenceValue = closeButton;
            }

            var kiranaButton = GameObject.Find("kiranaButton")?.GetComponent<Button>();
            var pizzaButton = GameObject.Find("pizzaButton")?.GetComponent<Button>();
            var cafeButton = GameObject.Find("cafeButton")?.GetComponent<Button>();
            var restaurantButton = GameObject.Find("restaurantButton")?.GetComponent<Button>();

            so.FindProperty("kiranaButton").objectReferenceValue = kiranaButton;
            so.FindProperty("pizzaButton").objectReferenceValue = pizzaButton;
            so.FindProperty("cafeButton").objectReferenceValue = cafeButton;
            so.FindProperty("restaurantButton").objectReferenceValue = restaurantButton;

            // Info texts
            var kiranaText = GameObject.Find("kiranaInfoText")?.GetComponent<TextMeshProUGUI>();
            var pizzaText = GameObject.Find("pizzaInfoText")?.GetComponent<TextMeshProUGUI>();
            var cafeText = GameObject.Find("cafeInfoText")?.GetComponent<TextMeshProUGUI>();
            var restaurantText = GameObject.Find("restaurantInfoText")?.GetComponent<TextMeshProUGUI>();

            so.FindProperty("kiranaInfoText").objectReferenceValue = kiranaText;
            so.FindProperty("pizzaInfoText").objectReferenceValue = pizzaText;
            so.FindProperty("cafeInfoText").objectReferenceValue = cafeText;
            so.FindProperty("restaurantInfoText").objectReferenceValue = restaurantText;

            // Prefabs: assign in Inspector manually if you have them
            so.ApplyModifiedProperties();

            Undo.RegisterCreatedObjectUndo(root, "Create Build Menu");
            Selection.activeGameObject = root;
        }

        private static void CreateCard(Transform parent, string cardName, string title, string cost, string description, string buttonName)
        {
            var cardGO = new GameObject(cardName);
            cardGO.transform.SetParent(parent, false);

            var cardRT = cardGO.AddComponent<RectTransform>();
            cardRT.sizeDelta = new Vector2(300, 180);

            var cardImage = cardGO.AddComponent<Image>();
            cardImage.color = new Color(0.12f, 0.12f, 0.16f, 0.96f);

            var cardLayout = cardGO.AddComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(12, 12, 12, 12);
            cardLayout.spacing = 8;
            cardLayout.childForceExpandWidth = true;
            cardLayout.childForceExpandHeight = false;

            // Title
            var titleGO = new GameObject("TitleText");
            titleGO.transform.SetParent(cardGO.transform, false);

            var titleText = titleGO.AddComponent<TextMeshProUGUI>();
            titleText.text = title;
            titleText.fontSize = 22;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Color.white;

            var titleRect = titleGO.GetComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(0, 26);

            // Cost
            var costGO = new GameObject("CostText");
            costGO.transform.SetParent(cardGO.transform, false);

            var costText = costGO.AddComponent<TextMeshProUGUI>();
            costText.text = cost;
            costText.fontSize = 20;
            costText.alignment = TextAlignmentOptions.Center;
            costText.color = new Color(0.24f, 0.9f, 0.58f, 1f);

            var costRect = costGO.GetComponent<RectTransform>();
            costRect.sizeDelta = new Vector2(0, 24);

            // Description
            var descGO = new GameObject("DescText");
            descGO.transform.SetParent(cardGO.transform, false);

            var descText = descGO.AddComponent<TextMeshProUGUI>();
            descText.text = description;
            descText.fontSize = 13;
            descText.alignment = TextAlignmentOptions.Center;
            descText.color = new Color(0.85f, 0.85f, 0.85f, 1f);

            var descRect = descGO.GetComponent<RectTransform>();
            descRect.sizeDelta = new Vector2(0, 40);

            // Button
            var buttonGO = new GameObject(buttonName);
            buttonGO.transform.SetParent(cardGO.transform, false);

            var button = buttonGO.AddComponent<Button>();
            var buttonImage = buttonGO.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.75f, 0.4f, 1f);

            var buttonTextGO = new GameObject("Text");
            buttonTextGO.transform.SetParent(buttonGO.transform, false);

            var buttonText = buttonTextGO.AddComponent<TextMeshProUGUI>();
            buttonText.text = "Build";
            buttonText.fontSize = 18;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.color = Color.white;

            var buttonRect = buttonGO.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(0, 40);

            var buttonTextRect = buttonTextGO.GetComponent<RectTransform>();
            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;
            buttonTextRect.offsetMin = Vector2.zero;
            buttonTextRect.offsetMax = Vector2.zero;

            // Set names for serialization lookup
            if (buttonName == "kiranaButton")
            {
                buttonGO.name = "kiranaButton";
                titleText.gameObject.name = "kiranaInfoText";
                costText.gameObject.name = "kiranaCostText";
            }
            else if (buttonName == "pizzaButton")
            {
                buttonGO.name = "pizzaButton";
                titleText.gameObject.name = "pizzaInfoText";
                costText.gameObject.name = "pizzaCostText";
            }
            else if (buttonName == "cafeButton")
            {
                buttonGO.name = "cafeButton";
                titleText.gameObject.name = "cafeInfoText";
                costText.gameObject.name = "cafeCostText";
            }
            else if (buttonName == "restaurantButton")
            {
                buttonGO.name = "restaurantButton";
                titleText.gameObject.name = "restaurantInfoText";
                costText.gameObject.name = "restaurantCostText";
            }

            // Add default event target if BuildMenuManager exists
            var root = GameObject.Find("BuildMenu");
            if (root != null)
            {
                var manager = root.GetComponent<AIBusinessTycoon.UI.BuildMenuManager>();
                if (manager != null)
                {
                    // Buttons are assigned by BuildMenuManager script, not by direct event hooks.
                    // This keeps the script clean and supports inspector assignment.
                }
            }
        }
    }
}
