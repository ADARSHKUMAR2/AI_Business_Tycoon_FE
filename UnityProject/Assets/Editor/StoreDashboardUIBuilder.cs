using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Creates a reference-inspired store dashboard in the current scene.</summary>
public static class StoreDashboardUIBuilder
{
    private const string RootName = "StoreDashboardUI_Generated";
    private static readonly Color Navy = new Color(0.015f, 0.055f, 0.105f, 0.97f);
    private static readonly Color Panel = new Color(0.025f, 0.095f, 0.16f, 0.96f);
    private static readonly Color Cyan = new Color(0.02f, 0.62f, 0.76f);
    private static readonly Color Gold = new Color(1f, 0.68f, 0.12f);

    [MenuItem("AI Business Tycoon/UI/Create Reference Store Dashboard")]
    public static void CreateDashboard()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            var canvasObject = new GameObject("StoreDashboardCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasObject, "Create store dashboard canvas");
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.matchWidthOrHeight = 0.5f;
        }

        Transform old = canvas.transform.Find(RootName);
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
        GameObject root = MakeRect(RootName, canvas.transform);
        Undo.RegisterCreatedObjectUndo(root, "Create reference store dashboard");
        Stretch(root.GetComponent<RectTransform>());
        Image background = root.AddComponent<Image>();
        background.color = Navy;
        background.raycastTarget = false;

        RectTransform top = MakeAnchoredPanel("TopBar", root.transform, new Vector2(0, 0.875f), new Vector2(1, 1), Navy);
        Text(top, "StoreName", "🏪  Store Name", 32, TextAlignmentOptions.Left, Color.white, new Vector2(0.04f, 0), new Vector2(0.48f, 1));
        Text(top, "Money", "🪙  Rs. 10,000", 27, TextAlignmentOptions.Center, Gold, new Vector2(0.37f, 0.12f), new Vector2(0.61f, 0.88f));
        Text(top, "DayWeather", "☀  DAY 1  |  CLEAR", 20, TextAlignmentOptions.Center, new Color(0.75f, 0.88f, 0.98f), new Vector2(0.77f, 0.12f), new Vector2(0.98f, 0.88f));

        GameObject body = MakeRect("DashboardBody", root.transform);
        Anchors(body.GetComponent<RectTransform>(), new Vector2(0.015f, 0.19f), new Vector2(0.985f, 0.865f));
        GameObject cards = MakeRect("SummaryCards", body.transform);
        Anchors(cards.GetComponent<RectTransform>(), new Vector2(0, 0.81f), new Vector2(0.75f, 1));
        MakeStatCard(cards.transform, "BusinessesCard", "🛒", "0", "Businesses", new Color(0.02f, 0.65f, 0.52f), 0f, 1f / 3f);
        MakeStatCard(cards.transform, "LandCard", "🌱", "1", "Land Tiles", Cyan, 1f / 3f, 2f / 3f);
        MakeStatCard(cards.transform, "RevenueCard", "🪙", "Rs. 0", "Revenue", Gold, 2f / 3f, 1f);

        RectTransform leaderboard = MakeAnchoredPanel("LeaderboardPanel", body.transform, new Vector2(0.765f, 0.02f), new Vector2(1, 1), Panel);
        Text(leaderboard, "LeaderboardTitle", "🏆  LEADERBOARD      ● Connected", 19, TextAlignmentOptions.Center, Color.white, new Vector2(0.03f, 0.88f), new Vector2(0.97f, 0.99f));
        Text(leaderboard, "LeaderboardColumns", "#       Player                 Revenue       Customers", 14, TextAlignmentOptions.Center, new Color(0.65f, 0.78f, 0.9f), new Vector2(0.02f, 0.79f), new Vector2(0.98f, 0.88f));
        Text(leaderboard, "LeaderboardEmpty", "🏆\n\nNo players yet\n\nBe the first to make your\nmark in the leaderboard!", 21, TextAlignmentOptions.Center, new Color(0.78f, 0.86f, 0.95f), new Vector2(0.06f, 0.25f), new Vector2(0.94f, 0.72f));

        RectTransform loading = MakeAnchoredPanel("LoadingOverlay", body.transform, new Vector2(0.29f, 0.25f), new Vector2(0.57f, 0.67f), new Color(0.02f, 0.07f, 0.13f, 0.94f));
        Text(loading, "LoadingMessage", "◌\nLoading...\nPreparing your store...", 25, TextAlignmentOptions.Center, Color.white, new Vector2(0.05f, 0.12f), new Vector2(0.95f, 0.9f));
        loading.gameObject.SetActive(false); // Optional; enable while data is loading.

        RectTransform footer = MakeAnchoredPanel("ActionBar", root.transform, new Vector2(0.015f, 0.035f), new Vector2(0.985f, 0.18f), Navy);
        Button exit = MakeActionButton(footer, "ExitStoreButton", "⇦   EXIT STORE       Esc", new Color(0.48f, 0.08f, 0.13f), new Vector2(0.015f, 0.14f), new Vector2(0.405f, 0.86f));
        Button hire = MakeActionButton(footer, "HireStaffButton", "♟   HIRE STAFF       H", new Color(0.02f, 0.53f, 0.42f), new Vector2(0.595f, 0.14f), new Vector2(0.985f, 0.86f));

        WireStoreManager(exit, hire);
        Selection.activeGameObject = root;
        Debug.Log("[StoreDashboardUIBuilder] Dashboard created. Replace placeholder summary values and connect live data as needed.");
    }

    private static void WireStoreManager(Button exit, Button hire)
    {
        var managerType = System.Type.GetType("AIBusinessTycoon.UI.StoreUIManager, AIBusinessTycoon.Managers");
        if (managerType == null) return;
        var manager = Object.FindObjectOfType(managerType);
        if (manager == null) return;
        var serialized = new SerializedObject(manager);
        Assign(serialized, "exitButton", exit);
        Assign(serialized, "hireStaffButton", hire);
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(manager);
    }

    private static void Assign(SerializedObject so, string propertyName, Object value)
    {
        var property = so.FindProperty(propertyName);
        if (property != null) property.objectReferenceValue = value;
    }

    private static void MakeStatCard(Transform parent, string name, string icon, string value, string label, Color accent, float minX, float maxX)
    {
        RectTransform card = MakeAnchoredPanel(name, parent, new Vector2(minX + 0.012f, 0.84f), new Vector2(maxX - 0.012f, 0.99f), Panel);
        Text(card, "Icon", icon, 34, TextAlignmentOptions.Center, accent, new Vector2(0.04f, 0.12f), new Vector2(0.25f, 0.88f));
        Text(card, "Value", value, 29, TextAlignmentOptions.Left, Color.white, new Vector2(0.29f, 0.46f), new Vector2(0.94f, 0.88f));
        Text(card, "Label", label, 19, TextAlignmentOptions.Left, new Color(0.82f, 0.89f, 0.96f), new Vector2(0.29f, 0.12f), new Vector2(0.94f, 0.47f));
    }

    private static Button MakeActionButton(Transform parent, string name, string label, Color color, Vector2 min, Vector2 max)
    {
        GameObject obj = MakeRect(name, parent);
        Anchors(obj.GetComponent<RectTransform>(), min, max);
        Image image = obj.AddComponent<Image>();
        image.color = color;
        Button button = obj.AddComponent<Button>();
        button.targetGraphic = image;
        Text(obj.transform, "Label", label, 26, TextAlignmentOptions.Center, Color.white, Vector2.zero, Vector2.one);
        return button;
    }

    private static RectTransform MakeAnchoredPanel(string name, Transform parent, Vector2 min, Vector2 max, Color color)
    {
        GameObject obj = MakeRect(name, parent);
        Anchors(obj.GetComponent<RectTransform>(), min, max);
        obj.AddComponent<Image>().color = color;
        return obj.GetComponent<RectTransform>();
    }

    private static TextMeshProUGUI Text(Transform parent, string name, string value, float size, TextAlignmentOptions alignment, Color color, Vector2 min, Vector2 max)
    {
        GameObject obj = MakeRect(name, parent);
        Anchors(obj.GetComponent<RectTransform>(), min, max);
        var text = obj.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.fontStyle = FontStyles.Bold;
        text.alignment = alignment;
        text.color = color;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null) text.font = TMP_Settings.defaultFontAsset;
        return text;
    }

    private static GameObject MakeRect(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private static void Stretch(RectTransform rect) => Anchors(rect, Vector2.zero, Vector2.one);

    private static void Anchors(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}