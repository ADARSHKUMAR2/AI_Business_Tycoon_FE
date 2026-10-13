using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using AIBusinessTycoon.UI;

namespace AIBusinessTycoon.Editor
{
    /// <summary>Creates a reference-inspired dark, tabbed, scrollable build menu.</summary>
    public static class ReferenceBuildMenuSetup
    {
        private const string CanvasName = "Build Menu Canvas";
        private const string RootName = "Reference Build Menu";
        private static readonly Color Navy = new Color(0.018f, 0.065f, 0.12f, 0.98f);
        private static readonly Color Card = new Color(0.035f, 0.12f, 0.20f, 0.98f);
        private static readonly Color Green = new Color(0f, 0.66f, 0.45f, 1f);
        private static readonly Color Gold = new Color(1f, 0.74f, 0.12f, 1f);
        private static readonly Color Muted = new Color(0.60f, 0.78f, 0.91f, 1f);

        [MenuItem("AI Business Tycoon/UI/Create Reference Build Menu")]
        public static void CreateBuildMenu()
        {
            Canvas canvas = GetCanvas();
            EnsureEventSystem();
            Transform previous = canvas.transform.Find(RootName);
            if (previous != null) Undo.DestroyObjectImmediate(previous.gameObject);

            GameObject root = MakeRect(RootName, canvas.transform);
            Undo.RegisterCreatedObjectUndo(root, "Create Reference Build Menu");
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.02f, 0.035f);
            rootRect.anchorMax = new Vector2(0.98f, 0.965f);
            rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;
            Image rootImage = root.AddComponent<Image>();
            rootImage.color = Navy;
            SetRounded(rootImage);

            BuildHeader(root.transform);
            Transform tabs = BuildTabs(root.transform);
            ScrollRect scroll = BuildScroll(root.transform);
            BuildMenuManager manager = GetOrCreateBuildMenuManager();
            GameObject[] cards = BuildCards(scroll.content, manager);

            BuildMenuTabFilter tabFilter = root.AddComponent<BuildMenuTabFilter>();
            tabFilter.Configure(tabs.GetComponentsInChildren<Button>(true),
                new[] { "ALL", "FOOD", "RETAIL", "SERVICES" },
                cards,
                new[] { "RETAIL", "FOOD", "FOOD", "FOOD" },
                scroll);

            WireManager(root, manager);
            root.SetActive(false);
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Selection.activeGameObject = root;
            Debug.Log("[ReferenceBuildMenuSetup] Build menu created. BuildMenuManager opens it with B/Tab.");
        }

        private static Canvas GetCanvas()
        {
            GameObject go = GameObject.Find(CanvasName);
            if (go == null)
            {
                go = new GameObject(CanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                Undo.RegisterCreatedObjectUndo(go, "Create Build Menu Canvas");
            }
            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30;
            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = 0.5f;
            if (go.GetComponent<GraphicRaycaster>() == null) go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            GameObject go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(go, "Create Build Menu EventSystem");
        }

        private static void BuildHeader(Transform root)
        {
            RectTransform header = Panel("Header", root, new Vector2(0f, 0.865f), Vector2.one, new Color(0.035f, 0.10f, 0.17f, 1f));
            Label(header, "Title", "🛠  BUILD MENU", 34, Color.white, TextAlignmentOptions.MidlineLeft,
                new Vector2(0.035f, 0f), new Vector2(0.78f, 1f), FontStyles.Bold);
            Button close = MakeButton(header, "CloseButton", "✕", new Color(0.04f, 0.16f, 0.25f),
                new Vector2(0.93f, 0.18f), new Vector2(0.985f, 0.82f), 30);
            close.onClick.AddListener(() => root.gameObject.SetActive(false));
        }

        private static Transform BuildTabs(Transform root)
        {
            RectTransform bar = Panel("CategoryTabs", root, new Vector2(0.025f, 0.775f), new Vector2(0.975f, 0.85f), new Color(0.025f, 0.095f, 0.16f, 1f));
            string[] labels = { "ALL", "FOOD", "RETAIL", "SERVICES" };
            for (int i = 0; i < labels.Length; i++)
            {
                float min = i / 4f;
                float max = (i + 1) / 4f;
                MakeButton(bar, "Tab_" + labels[i], labels[i], i == 0 ? new Color(0.02f, 0.48f, 0.56f) : new Color(0.03f, 0.13f, 0.21f),
                    new Vector2(min + 0.003f, 0.06f), new Vector2(max - 0.003f, 0.94f), 19);
            }
            return bar;
        }

        private static ScrollRect BuildScroll(Transform root)
        {
            GameObject scrollObject = MakeRect("BuildingScrollView", root);
            RectTransform scrollRect = scrollObject.GetComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.025f, 0.025f);
            scrollRect.anchorMax = new Vector2(0.975f, 0.755f);
            scrollRect.offsetMin = scrollRect.offsetMax = Vector2.zero;
            scrollObject.AddComponent<Image>().color = Color.clear;
            ScrollRect scroll = scrollObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 35f;

            GameObject viewport = MakeRect("Viewport", scrollObject.transform);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = viewportRect.offsetMax = Vector2.zero;
            viewport.AddComponent<Image>().color = Color.white;
            Mask mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject content = MakeRect("Content", viewport.transform);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = Vector2.zero;
            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 8, 2, 4);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewportRect;
            scroll.content = contentRect;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            return scroll;
        }

        private static GameObject[] BuildCards(RectTransform content, BuildMenuManager manager)
        {
            SerializedObject managerData = new SerializedObject(manager);
            return new[]
            {
                CreateBuildingCard(content, "Kirana Store", "Daily essentials for your customers.", GetCostLabel(managerData, "kiranaCost", 5000f), "🛒", Green, "kiranaButton"),
                CreateBuildingCard(content, "Pizza Outlet", "Fresh pizza, great taste, more customers.", GetCostLabel(managerData, "pizzaCost", 15000f), "🍕", new Color(0.85f, 0.25f, 0.18f), "pizzaButton"),
                CreateBuildingCard(content, "Cafe", "Coffee, snacks and happy vibes.", GetCostLabel(managerData, "cafeCost", 25000f), "☕", new Color(0.50f, 0.30f, 0.18f), "cafeButton"),
                CreateBuildingCard(content, "Restaurant", "Fine dining for a better tomorrow.", GetCostLabel(managerData, "restaurantCost", 18000f), "🍽", new Color(0.35f, 0.26f, 0.18f), "restaurantButton")
            };
        }

        private static string GetCostLabel(SerializedObject managerData, string propertyName, float fallbackCost)
        {
            SerializedProperty costProperty = managerData.FindProperty(propertyName);
            float cost = costProperty != null ? costProperty.floatValue : fallbackCost;
            return "₹" + cost.ToString("N0");
        }

        private static GameObject CreateBuildingCard(Transform parent, string title, string description, string price, string icon, Color accent, string buttonName)
        {
            GameObject card = MakeRect(title.Replace(" ", "") + "Card", parent);
            LayoutElement element = card.AddComponent<LayoutElement>();
            element.minHeight = element.preferredHeight = 164f;
            Image cardImage = card.AddComponent<Image>();
            cardImage.color = Card;
            SetRounded(cardImage);

            RectTransform iconBox = Panel("IconPlaceholder", card.transform, new Vector2(0.015f, 0.08f), new Vector2(0.19f, 0.92f), new Color(accent.r, accent.g, accent.b, 0.22f));
            Label(iconBox, "Icon", icon, 54, accent, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, FontStyles.Normal);
            RectTransform info = Panel("BuildingInfo", card.transform, new Vector2(0.215f, 0.12f), new Vector2(0.58f, 0.88f), Color.clear);
            Label(info, "Title", title, 26, Color.white, TextAlignmentOptions.MidlineLeft,
                new Vector2(0f, 0.52f), new Vector2(1f, 1f), FontStyles.Bold);
            Label(info, "Description", description, 17, Muted, TextAlignmentOptions.TopLeft,
                new Vector2(0f, 0f), new Vector2(1f, 0.52f), FontStyles.Normal);
            Panel("PriceDivider", card.transform, new Vector2(0.60f, 0.13f), new Vector2(0.603f, 0.87f), new Color(0.05f, 0.30f, 0.45f, 1f));
            Label(card.transform, "Price", "🪙  " + price, 25, Gold, TextAlignmentOptions.Center,
                new Vector2(0.62f, 0.15f), new Vector2(0.81f, 0.85f), FontStyles.Bold);
            MakeButton(card.transform, buttonName, "BUILD", Green,
                new Vector2(0.83f, 0.22f), new Vector2(0.98f, 0.78f), 22);
            return card;
        }

        private static BuildMenuManager GetOrCreateBuildMenuManager()
        {
            BuildMenuManager manager = Object.FindFirstObjectByType<BuildMenuManager>();
            if (manager == null)
            {
                GameObject host = GameObject.Find("[ UI MANAGERS ]");
                if (host == null)
                {
                    host = new GameObject("[ UI MANAGERS ]");
                    Undo.RegisterCreatedObjectUndo(host, "Create UI Managers");
                }
                manager = Undo.AddComponent<BuildMenuManager>(host);
            }
            return manager;
        }

        private static void WireManager(GameObject root, BuildMenuManager manager)
        {
            SerializedObject so = new SerializedObject(manager);
            SetRef(so, "menuPanel", root);
            SetRef(so, "closeButton", root.transform.Find("Header/CloseButton")?.GetComponent<Button>());
            SetRef(so, "kiranaButton", FindButton(root.transform, "KiranaStoreCard/kiranaButton"));
            SetRef(so, "pizzaButton", FindButton(root.transform, "PizzaOutletCard/pizzaButton"));
            SetRef(so, "cafeButton", FindButton(root.transform, "CafeCard/cafeButton"));
            SetRef(so, "restaurantButton", FindButton(root.transform, "RestaurantCard/restaurantButton"));
            AssignPrefab(so, "kiranaPrefab", "Assets/Prefabs/Buildings/KiranaStore.prefab");
            AssignPrefab(so, "pizzaPrefab", "Assets/Prefabs/Buildings/PizzaOutlet.prefab");
            AssignPrefab(so, "cafePrefab", "Assets/Prefabs/Buildings/Cafe.prefab");
            AssignPrefab(so, "restaurantPrefab", "Assets/Prefabs/Buildings/Restaurant.prefab");
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(manager);
        }

        private static Button FindButton(Transform root, string path) => root.Find("BuildingScrollView/Viewport/Content/" + path)?.GetComponent<Button>();
        private static void AssignPrefab(SerializedObject so, string property, string path) => SetRef(so, property, AssetDatabase.LoadAssetAtPath<GameObject>(path));

        private static void SetRef(SerializedObject so, string name, Object value)
        {
            SerializedProperty property = so.FindProperty(name);
            if (property != null && value != null) property.objectReferenceValue = value;
        }

        private static RectTransform Panel(string name, Transform parent, Vector2 min, Vector2 max, Color color)
        {
            GameObject go = MakeRect(name, parent);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            Image image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = color.a > 0f;
            SetRounded(image);
            return rect;
        }

        private static Button MakeButton(Transform parent, string name, string label, Color color, Vector2 min, Vector2 max, float fontSize)
        {
            RectTransform rect = Panel(name, parent, min, max, color);
            Image image = rect.GetComponent<Image>();
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Label(rect, "Label", label, fontSize, Color.white, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, FontStyles.Bold);
            return button;
        }

        private static TextMeshProUGUI Label(Transform parent, string name, string value, float size, Color color,
            TextAlignmentOptions alignment, Vector2 min, Vector2 max, FontStyles style)
        {
            GameObject go = MakeRect(name, parent);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.fontStyle = style;
            text.alignment = alignment;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            if (TMP_Settings.defaultFontAsset != null) text.font = TMP_Settings.defaultFontAsset;
            return text;
        }

        private static GameObject MakeRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void SetRounded(Image image)
        {
            Sprite sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
            }
        }
    }
}