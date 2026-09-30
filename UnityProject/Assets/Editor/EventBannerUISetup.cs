using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using AIBusinessTycoon.UI;

namespace AIBusinessTycoon.Editor
{
    /// <summary>
    /// Unity Editor script to automatically create and configure the Event Banner UI.
    /// Usage: GameObject > UI > AI Business Tycoon > Create Event Banner
    /// </summary>
    public static class EventBannerUISetup
    {
        [MenuItem("GameObject/UI/AI Business Tycoon/Create Event Banner", false, 10)]
        private static void CreateEventBanner(MenuCommand menuCommand)
        {
            // Find or create Canvas
            Canvas canvas = FindOrCreateCanvas();
            
            // Create the Event Banner Panel
            GameObject bannerPanel = CreateBannerPanel(canvas.transform);
            
            // Register for undo
            Undo.RegisterCreatedObjectUndo(bannerPanel, "Create Event Banner");
            
            // Select the created object
            Selection.activeGameObject = bannerPanel;
            
            Debug.Log("[EventBannerUISetup] Event Banner UI created successfully! Configure in Inspector.");
        }
        
        private static Canvas FindOrCreateCanvas()
        {
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                
                Debug.Log("[EventBannerUISetup] Canvas created automatically.");
            }
            
            return canvas;
        }
        
        private static void ConfigurePanelTransform(RectTransform rect)
        {
            // Anchor to top-center
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            
            // Position and size
            rect.anchoredPosition = new Vector2(0f, -80f);
            rect.sizeDelta = new Vector2(600f, 120f);
        }
        
        private static GameObject CreateBackground(Transform parent)
        {
            GameObject bg = new GameObject("BackgroundPanel");
            bg.transform.SetParent(parent, false);
            
            RectTransform bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            bgRect.anchoredPosition = Vector2.zero;
            
            Image bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0f, 0f, 0f, 0.8f); // Semi-transparent black
            
            return bg;
        }
        
        private static GameObject CreateContentContainer(Transform parent)
        {
            GameObject container = new GameObject("ContentContainer");
            container.transform.SetParent(parent, false);
            
            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0f, 0f);
            containerRect.anchorMax = new Vector2(1f, 1f);
            containerRect.offsetMin = new Vector2(20f, 20f);
            containerRect.offsetMax = new Vector2(-180f, -20f);
            
            HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 15f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            
            // Create icon placeholder
            CreateIconPlaceholder(container.transform);
            
            // Create info container with title, status, details
            CreateInfoContainer(container.transform);
            
            return container;
        }
        
        private static void CreateInfoContainer(Transform parent)
        {
            GameObject infoContainer = new GameObject("InfoContainer");
            infoContainer.transform.SetParent(parent, false);
            
            RectTransform infoRect = infoContainer.AddComponent<RectTransform>();
            infoRect.sizeDelta = new Vector2(300f, 80f);
            
            VerticalLayoutGroup layout = infoContainer.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 5f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            
            // Title
            CreateTitleText(infoContainer.transform);
            
            // Status
            CreateStatusText(infoContainer.transform);
            
            // Details
            CreateDetailsText(infoContainer.transform);
        }
        
        private static GameObject CreateTitleText(Transform parent)
        {
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(parent, false);
            
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(300f, 30f);
            
            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "STARBUCKS TOURNAMENT";
            titleText.fontSize = 24;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = Color.white;
            titleText.alignment = TextAlignmentOptions.Left;
            
            return titleObj;
        }
        
        private static GameObject CreateStatusText(Transform parent)
        {
            GameObject statusObj = new GameObject("StatusText");
            statusObj.transform.SetParent(parent, false);
            
            RectTransform statusRect = statusObj.AddComponent<RectTransform>();
            statusRect.sizeDelta = new Vector2(150f, 20f);
            
            TextMeshProUGUI statusText = statusObj.AddComponent<TextMeshProUGUI>();
            statusText.text = "● ACTIVE";
            statusText.fontSize = 16;
            statusText.color = new Color(0.15f, 0.9f, 0.27f); // Green
            statusText.alignment = TextAlignmentOptions.Left;
            
            return statusObj;
        }
        
        private static GameObject CreateDetailsText(Transform parent)
        {
            GameObject detailsObj = new GameObject("DetailsText");
            detailsObj.transform.SetParent(parent, false);
            
            RectTransform detailsRect = detailsObj.AddComponent<RectTransform>();
            detailsRect.sizeDelta = new Vector2(300f, 20f);
            
            TextMeshProUGUI detailsText = detailsObj.AddComponent<TextMeshProUGUI>();
            detailsText.text = "Entry: ₹5,000 | Players: 0/10";
            detailsText.fontSize = 14;
            detailsText.color = new Color(1f, 1f, 1f, 0.7f);
            detailsText.alignment = TextAlignmentOptions.Left;
            
            return detailsObj;
        }
        
        private static void CreateIconPlaceholder(Transform parent)
        {
            GameObject icon = new GameObject("EventIcon");
            icon.transform.SetParent(parent, false);
            
            RectTransform iconRect = icon.AddComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(80f, 80f);
            
            Image iconImage = icon.AddComponent<Image>();
            iconImage.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        }
        
        private static GameObject CreateTimerText(Transform parent)
        {
            GameObject timerObj = new GameObject("TimerText");
            timerObj.transform.SetParent(parent, false);
            
            RectTransform timerRect = timerObj.AddComponent<RectTransform>();
            timerRect.anchorMin = new Vector2(1f, 0.5f);
            timerRect.anchorMax = new Vector2(1f, 0.5f);
            timerRect.pivot = new Vector2(1f, 0.5f);
            timerRect.anchoredPosition = new Vector2(-180f, 30f);
            timerRect.sizeDelta = new Vector2(150f, 25f);
            
            TextMeshProUGUI timerText = timerObj.AddComponent<TextMeshProUGUI>();
            timerText.text = "Ends in: 25:30";
            timerText.fontSize = 18;
            timerText.color = Color.white;
            timerText.alignment = TextAlignmentOptions.Right;
            
            return timerObj;
        }
        
        private static GameObject CreateJoinButton(Transform parent)
        {
            GameObject buttonObj = new GameObject("JoinButton");
            buttonObj.transform.SetParent(parent, false);
            
            RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(1f, 0.5f);
            buttonRect.anchorMax = new Vector2(1f, 0.5f);
            buttonRect.pivot = new Vector2(1f, 0.5f);
            buttonRect.anchoredPosition = new Vector2(-20f, -10f);
            buttonRect.sizeDelta = new Vector2(150f, 40f);
            
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.15f, 0.9f, 0.27f); // Green
            
            Button button = buttonObj.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.15f, 0.9f, 0.27f);
            colors.highlightedColor = new Color(0.2f, 1f, 0.35f);
            colors.pressedColor = new Color(0.1f, 0.7f, 0.2f);
            colors.disabledColor = new Color(0.4f, 0.4f, 0.4f);
            button.colors = colors;
            
            // Button text
            GameObject buttonTextObj = new GameObject("ButtonText");
            buttonTextObj.transform.SetParent(buttonObj.transform, false);
            
            RectTransform buttonTextRect = buttonTextObj.AddComponent<RectTransform>();
            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;
            buttonTextRect.sizeDelta = Vector2.zero;
            
            TextMeshProUGUI buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
            buttonText.text = "JOIN NOW";
            buttonText.fontSize = 16;
            buttonText.fontStyle = FontStyles.Bold;
            buttonText.color = Color.white;
            buttonText.alignment = TextAlignmentOptions.Center;
            
            return buttonObj;
        }
        
        private static GameObject CreateBannerPanel(Transform parent)
        {
            // Main panel
            GameObject panel = new GameObject("EventBannerPanel");
            panel.transform.SetParent(parent, false);
            
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            ConfigurePanelTransform(panelRect);
            
            // Add CanvasGroup for animations
            CanvasGroup canvasGroup = panel.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f; // Start hidden
            
            // Add EventBannerUI component
            EventBannerUI bannerUI = panel.AddComponent<EventBannerUI>();
            
            // Create child elements
            GameObject background = CreateBackground(panel.transform);
            GameObject contentContainer = CreateContentContainer(panel.transform);
            GameObject timer = CreateTimerText(panel.transform);
            GameObject joinButton = CreateJoinButton(panel.transform);
            
            // Wire up references
            WireUpReferences(bannerUI, panel, canvasGroup, contentContainer, timer, joinButton);
            
            return panel;
        }
        
        private static void WireUpReferences(EventBannerUI bannerUI, GameObject panel, CanvasGroup canvasGroup,
            GameObject contentContainer, GameObject timer, GameObject joinButton)
        {
            // Use SerializedObject to set private fields
            SerializedObject so = new SerializedObject(bannerUI);
            
            so.FindProperty("bannerPanel").objectReferenceValue = panel;
            so.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
            
            // Find child elements
            Transform infoContainer = contentContainer.transform.Find("InfoContainer");
            
            so.FindProperty("titleText").objectReferenceValue = infoContainer.Find("TitleText").GetComponent<TextMeshProUGUI>();
            so.FindProperty("statusText").objectReferenceValue = infoContainer.Find("StatusText").GetComponent<TextMeshProUGUI>();
            so.FindProperty("detailsText").objectReferenceValue = infoContainer.Find("DetailsText").GetComponent<TextMeshProUGUI>();
            so.FindProperty("timerText").objectReferenceValue = timer.GetComponent<TextMeshProUGUI>();
            so.FindProperty("joinButton").objectReferenceValue = joinButton.GetComponent<Button>();
            so.FindProperty("joinButtonText").objectReferenceValue = joinButton.transform.Find("ButtonText").GetComponent<TextMeshProUGUI>();
            
            so.ApplyModifiedProperties();
        }
    }
}
