using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using System.IO;
using UnityEngine.EventSystems;
using UnityEngine.AI;

namespace AIBusinessTycoon.Editor
{
    public class SceneSetupEditor : EditorWindow
    {
        // ── Colour Palette ────────────────────────────────────────────────────
        private Color primaryColor    = new Color(0.12f, 0.53f, 0.90f);
        private Color secondaryColor  = new Color(0.18f, 0.80f, 0.44f);
        private Color accentColor     = new Color(1.0f,  0.76f, 0.03f);
        private Color darkTextColor   = new Color(0.13f, 0.13f, 0.13f);
        private Color whiteColor      = Color.white;

        private Color cashierColor    = new Color(0.20f, 0.40f, 1.00f);   
        private Color restockerColor  = new Color(1.00f, 0.50f, 0.10f);   
        private Color hireButtonColor = new Color(0.10f, 0.65f, 0.35f);   
        private Color darkPanelColor  = new Color(0.08f, 0.08f, 0.12f, 0.96f);
        private Color rowBgColor      = new Color(1f, 1f, 1f, 0.06f);

        private Color cleanerColor    = new Color(0.20f, 0.80f, 0.30f);   
        private Color ratingGoldColor = new Color(1.00f, 0.85f, 0.10f);   

        [MenuItem("AI Business Tycoon/Setup Complete Scene")]
        public static void ShowWindow()
        {
            var window = GetWindow<SceneSetupEditor>("Scene Setup");
            window.minSize = new Vector2(420, 380);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("AI Business Tycoon — Scene Setup", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "FULL REBUILD: Clears & recreates the entire scene from scratch (use for fresh start).\n" +
                "PATCH UI BUTTONS: Injects new Phase UI elements directly into the current scene safely.",
                MessageType.Info);

            GUILayout.Space(10);

            if (GUILayout.Button("🎨  Re-Create Complete Scene  (Full Rebuild)", GUILayout.Height(50)))
            {
                CreateCompleteScene();
            }

            GUILayout.Space(8);
            if (GUILayout.Button("⚡  Patch Phase 2 — Hire UI Only  (Safe)", GUILayout.Height(40)))
            {
                PatchPhase2HireUI();
            }

            GUILayout.Space(8);
            GUI.backgroundColor = new Color(0.20f, 0.80f, 0.30f); 
            if (GUILayout.Button("🧹  Patch Phase 3 — Cleaner UI + Rating Bar  (Safe)", GUILayout.Height(40)))
            {
                PatchPhase3UI();
            }

            GUILayout.Space(8);
            GUI.backgroundColor = new Color(0.20f, 0.60f, 0.90f); 
            if (GUILayout.Button("📈  Patch Phase 3 — Employee Upgrade UI  (Safe)", GUILayout.Height(40)))
            {
                PatchEmployeeUpgradeUI();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(8);
            GUI.backgroundColor = new Color(0.80f, 0.40f, 0.10f); // Orange tint
            if (GUILayout.Button("📦  Patch Phase 3 — Shelf Upgrade UI  (Safe)", GUILayout.Height(40)))
            {
                PatchShelfUpgradeUI();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(8);
            GUI.backgroundColor = new Color(0.60f, 0.20f, 0.80f); // Purple tint
            if (GUILayout.Button("🏪  Patch Phase 3 — Upgrade Store Layouts", GUILayout.Height(40)))
            {
                PatchStoreLayouts();
            }

            

        }

        // ═════════════════════════════════════════════════════════════════════
        #region Orchestrators

        private void CreateCompleteScene()
        {
            SetupLayers();
            CreateFolderStructure();
            CreateMaterials();
            CreateEmployeeMaterials();  
            CreateCleanerMaterial();    
            CreatePrefabs();
            CreateEmployeePrefabs();    
            CreateCleanerPrefab();      

            ClearScene();
            CreateEventSystem();

            CreateEnvironment();
            CreatePlayerAvatar();

            CreateUISystem();
            ConnectComponents();
            CreateSupplyZone();

            // Apply the final upgrades to prefabs & UI
            PatchEmployeeUpgradeUI();

            Debug.Log("=== UI & Scene Setup Complete (Phase 3) ===");
        }

        private void PatchPhase2HireUI()
        {
            CreateFolderStructure();
            CreateEmployeeMaterials();
            CreateEmployeePrefabs();

            var storeUIManager = FindObjectOfType<AIBusinessTycoon.UI.StoreUIManager>();
            if (storeUIManager == null) return;

            var storeUIVisuals = GameObject.Find("StoreUI_Visuals");
            if (storeUIVisuals == null) return;

            var existing = GameObject.Find("HireMenuPanel");
            if (existing != null) DestroyImmediate(existing);

            CreateHireMenuPanel(storeUIVisuals.transform, storeUIManager);
            Debug.Log("[SceneSetupEditor] Phase 2 Hire UI patch complete.");
        }

        private void PatchPhase3UI()
        {
            CreateFolderStructure();
            CreateCleanerMaterial();
            CreateCleanerPrefab();

            var storeUIManager = FindObjectOfType<AIBusinessTycoon.UI.StoreUIManager>();
            if (storeUIManager == null) return;

            var hirePanel = GameObject.Find("HireMenuPanel");
            if (hirePanel == null) return;

            var card = hirePanel.transform.Find("HireCard");
            if (card == null) return;

            var cashierRow   = card.Find("CashierRow");
            var restockerRow = card.Find("RestockerRow");

            if (cashierRow != null)
                RepositionRow(cashierRow.GetComponent<RectTransform>(), 0.55f, 0.70f);

            if (restockerRow != null)
                RepositionRow(restockerRow.GetComponent<RectTransform>(), 0.30f, 0.53f);

            var existingCleanerRow = card.Find("CleanerRow");
            if (existingCleanerRow != null) DestroyImmediate(existingCleanerRow.gameObject);

            var existingRating = card.Find("StoreRatingBar");
            if (existingRating != null) DestroyImmediate(existingRating.gameObject);

            var ratingBar = CreateStoreRatingBar(card);

            var (_, cleanerHireBtn, cleanerStatus) = CreateEmployeeRow(
                card, "CleanerRow", 0.04f, 0.28f, "🧹", "Cleaner",
                "Roams the store cleaning trash.\nKeeps your Store Rating high.", "Rs. 800", cleanerColor);

            SetFieldValue(storeUIManager, "hireCleanerButton",  cleanerHireBtn);
            SetFieldValue(storeUIManager, "cleanerStatusText",  cleanerStatus);
            SetFieldValue(storeUIManager, "storeRatingText",    ratingBar);

            var cleanerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/CleanerPrefab.prefab");
            if (cleanerPrefab != null) SetFieldValue(storeUIManager, "cleanerPrefab", cleanerPrefab);

            var gameManager = FindObjectOfType<AIBusinessTycoon.Managers.GameManager>();
            if (gameManager != null && cleanerPrefab != null) SetFieldValue(gameManager, "cleanerPrefab", cleanerPrefab);

            Debug.Log("[SceneSetupEditor] ✅ Phase 3 UI patch complete.");
        }

        /// <summary>
        /// Phase 3 Patch: Modifies employee prefabs to be clickable and builds the Upgrade Popup UI.
        /// </summary>
        private void PatchEmployeeUpgradeUI()
        {
            // 1. Attach Interaction Script & Collider to Prefabs so they can be clicked
            string[] prefabPaths = {
                "Assets/Prefabs/Buildings/CashierPrefab.prefab",
                "Assets/Prefabs/Buildings/RestockerPrefab.prefab",
                "Assets/Prefabs/Buildings/CleanerPrefab.prefab"
            };

            foreach (var path in prefabPaths)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    if (prefab.GetComponent<CapsuleCollider>() == null)
                    {
                        var col = prefab.AddComponent<CapsuleCollider>();
                        col.radius = 0.5f;
                        col.height = 2f;
                        col.center = new Vector3(0, 1f, 0);
                    }
                    if (prefab.GetComponent<AIBusinessTycoon.Managers.EmployeeInteractionManager>() == null)
                    {
                        prefab.AddComponent<AIBusinessTycoon.Managers.EmployeeInteractionManager>();
                    }
                    EditorUtility.SetDirty(prefab);
                }
            }
            AssetDatabase.SaveAssets();

            // 2. Build the Popup UI
            var uiManagersObj = GameObject.Find("[ UI MANAGERS ]");
            if (uiManagersObj == null) return;

            var upgradeMgr = uiManagersObj.GetComponent<AIBusinessTycoon.UI.EmployeeUpgradeUIManager>();
            if (upgradeMgr == null) upgradeMgr = uiManagersObj.AddComponent<AIBusinessTycoon.UI.EmployeeUpgradeUIManager>();

            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return;

            var existingPanel = GameObject.Find("EmployeeUpgradePanel");
            if (existingPanel != null) DestroyImmediate(existingPanel);

            // Dim Background
            GameObject backdrop = CreatePanel("EmployeeUpgradePanel", canvas.transform,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            backdrop.GetComponent<Image>().color = new Color(0, 0, 0, 0.65f);

            // Main Card
            GameObject card = CreatePanel("UpgradeCard", backdrop.transform,
                new Vector2(0.15f, 0.25f), new Vector2(0.85f, 0.75f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            card.GetComponent<Image>().color = darkPanelColor;

            // Title
            GameObject titleBar = CreatePanel("TitleBar", card.transform,
                new Vector2(0, 0.82f), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, Vector2.zero);
            titleBar.GetComponent<Image>().color = new Color(1, 1, 1, 0.05f);

            CreateText("TitleText", titleBar.transform, "📈  UPGRADE EMPLOYEE", 45, FontStyles.Bold, Color.white,
                new Vector2(0.04f, 0), new Vector2(0.82f, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero)
                .GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;

            // Close btn
            GameObject closeBtn = CreatePanel("CloseButton", titleBar.transform,
                new Vector2(0.84f, 0.1f), new Vector2(0.98f, 0.9f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            closeBtn.GetComponent<Image>().color = new Color(0.85f, 0.20f, 0.20f);
            Button closeBtnComp = closeBtn.AddComponent<Button>();
            CreateText("Text", closeBtn.transform, "✕", 40, FontStyles.Bold, Color.white,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero)
                .GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

            // Employee Info Section
            var nameTxt = CreateText("NameText", card.transform, "Employee Name", 40, FontStyles.Bold, accentColor,
                new Vector2(0.05f, 0.68f), new Vector2(0.95f, 0.80f), new Vector2(0, 0.5f), Vector2.zero, Vector2.zero)
                .GetComponent<TextMeshProUGUI>();
            nameTxt.alignment = TextAlignmentOptions.BottomLeft;

            var roleTxt = CreateText("RoleText", card.transform, "ROLE", 25, FontStyles.Normal, new Color(1,1,1,0.6f),
                new Vector2(0.05f, 0.58f), new Vector2(0.95f, 0.68f), new Vector2(0, 0.5f), Vector2.zero, Vector2.zero)
                .GetComponent<TextMeshProUGUI>();
            roleTxt.alignment = TextAlignmentOptions.TopLeft;

            // Speed Upgrade Row
            var (speedVal, speedBtn, speedCost) = CreateUpgradeRow(card.transform, "SpeedRow", 0.32f, 0.55f, "🏃 Speed");
            
            // Capacity Upgrade Row
            var (capVal, capBtn, capCost) = CreateUpgradeRow(card.transform, "CapacityRow", 0.05f, 0.28f, "📦 Carry Capacity");

            // Wire everything to EmployeeUpgradeUIManager
            SetFieldValue(upgradeMgr, "popupPanel",            backdrop);
            SetFieldValue(upgradeMgr, "nameText",              nameTxt);
            SetFieldValue(upgradeMgr, "roleText",              roleTxt);
            SetFieldValue(upgradeMgr, "closeButton",           closeBtnComp);

            SetFieldValue(upgradeMgr, "speedValueText",        speedVal);
            SetFieldValue(upgradeMgr, "upgradeSpeedButton",    speedBtn);
            SetFieldValue(upgradeMgr, "speedCostText",         speedCost);

            SetFieldValue(upgradeMgr, "capacityValueText",     capVal);
            SetFieldValue(upgradeMgr, "upgradeCapacityButton", capBtn);
            SetFieldValue(upgradeMgr, "capacityCostText",      capCost);

            backdrop.SetActive(false);
            Debug.Log("[SceneSetupEditor] ✅ Employee Upgrade UI built & Prefabs patched!");
        }

        private (TextMeshProUGUI val, Button btn, TextMeshProUGUI cost) CreateUpgradeRow(Transform parent, string name, float yMin, float yMax, string labelStr)
        {
            GameObject row = CreatePanel(name, parent,
                new Vector2(0.04f, yMin), new Vector2(0.96f, yMax),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            row.GetComponent<Image>().color = rowBgColor;

            CreateText("Label", row.transform, labelStr, 35, FontStyles.Bold, Color.white,
                new Vector2(0.05f, 0), new Vector2(0.4f, 1), new Vector2(0, 0.5f), Vector2.zero, Vector2.zero)
                .GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;

            var valTxt = CreateText("Value", row.transform, "50 / 100", 35, FontStyles.Bold, primaryColor,
                new Vector2(0.4f, 0), new Vector2(0.65f, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero)
                .GetComponent<TextMeshProUGUI>();
            valTxt.alignment = TextAlignmentOptions.Center;

            GameObject btnObj = CreatePanel("UpgradeBtn", row.transform,
                new Vector2(0.68f, 0.15f), new Vector2(0.97f, 0.85f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            btnObj.GetComponent<Image>().color = secondaryColor;
            Button btn = btnObj.AddComponent<Button>();

            CreateText("UPGRADE", btnObj.transform, "UPGRADE", 25, FontStyles.Bold, Color.white,
                new Vector2(0, 0.5f), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero)
                .GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

            var costTxt = CreateText("Cost", btnObj.transform, "Rs. 1000", 25, FontStyles.Bold, accentColor,
                new Vector2(0, 0), new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero)
                .GetComponent<TextMeshProUGUI>();
            costTxt.alignment = TextAlignmentOptions.Center;

            return (valTxt, btn, costTxt);
        }

        /// <summary>
        /// Phase 3 Patch: Builds the Shelf Upgrade Popup UI.
        /// </summary>
        private void PatchShelfUpgradeUI()
        {
            var uiManagersObj = GameObject.Find("[ UI MANAGERS ]");
            if (uiManagersObj == null) return;

            var upgradeMgr = uiManagersObj.GetComponent<AIBusinessTycoon.UI.ShelfUpgradeUIManager>();
            if (upgradeMgr == null) upgradeMgr = uiManagersObj.AddComponent<AIBusinessTycoon.UI.ShelfUpgradeUIManager>();

            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return;

            var existingPanel = GameObject.Find("ShelfUpgradePanel");
            if (existingPanel != null) DestroyImmediate(existingPanel);

            // Dim Background
            GameObject backdrop = CreatePanel("ShelfUpgradePanel", canvas.transform,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            backdrop.GetComponent<Image>().color = new Color(0, 0, 0, 0.65f);

            // Main Card
            GameObject card = CreatePanel("UpgradeCard", backdrop.transform,
                new Vector2(0.2f, 0.35f), new Vector2(0.8f, 0.65f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            card.GetComponent<Image>().color = darkPanelColor;

            // Title
            GameObject titleBar = CreatePanel("TitleBar", card.transform,
                new Vector2(0, 0.75f), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, Vector2.zero);
            titleBar.GetComponent<Image>().color = new Color(1, 1, 1, 0.05f);

            CreateText("TitleText", titleBar.transform, "📦  UPGRADE SHELF", 45, FontStyles.Bold, Color.white,
                new Vector2(0.04f, 0), new Vector2(0.82f, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero)
                .GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;

            // Close btn
            GameObject closeBtn = CreatePanel("CloseButton", titleBar.transform,
                new Vector2(0.84f, 0.1f), new Vector2(0.98f, 0.9f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            closeBtn.GetComponent<Image>().color = new Color(0.85f, 0.20f, 0.20f);
            Button closeBtnComp = closeBtn.AddComponent<Button>();
            CreateText("Text", closeBtn.transform, "✕", 40, FontStyles.Bold, Color.white,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero)
                .GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

            // Content
            var nameTxt = CreateText("ItemName", card.transform, "Item Name", 45, FontStyles.Bold, accentColor,
                new Vector2(0.05f, 0.45f), new Vector2(0.95f, 0.70f), new Vector2(0, 0.5f), Vector2.zero, Vector2.zero)
                .GetComponent<TextMeshProUGUI>();
            nameTxt.alignment = TextAlignmentOptions.MidlineLeft;

            var capTxt = CreateText("CapacityText", card.transform, "Upgrade Capacity: 10 ➔ 20", 30, FontStyles.Normal, Color.white,
                new Vector2(0.05f, 0.25f), new Vector2(0.5f, 0.45f), new Vector2(0, 0.5f), Vector2.zero, Vector2.zero)
                .GetComponent<TextMeshProUGUI>();
            capTxt.alignment = TextAlignmentOptions.MidlineLeft;

            // Upgrade Btn
            GameObject btnObj = CreatePanel("UpgradeBtn", card.transform,
                new Vector2(0.6f, 0.15f), new Vector2(0.95f, 0.45f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            btnObj.GetComponent<Image>().color = secondaryColor;
            Button upgBtn = btnObj.AddComponent<Button>();

            CreateText("UPGRADE", btnObj.transform, "UPGRADE", 25, FontStyles.Bold, Color.white,
                new Vector2(0, 0.5f), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero)
                .GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

            var costTxt = CreateText("Cost", btnObj.transform, "Rs. 2000", 25, FontStyles.Bold, accentColor,
                new Vector2(0, 0), new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero)
                .GetComponent<TextMeshProUGUI>();
            costTxt.alignment = TextAlignmentOptions.Center;

            // Wire up
            SetFieldValue(upgradeMgr, "popupPanel",     backdrop);
            SetFieldValue(upgradeMgr, "itemNameText",   nameTxt);
            SetFieldValue(upgradeMgr, "capacityText",   capTxt);
            SetFieldValue(upgradeMgr, "costText",       costTxt);
            SetFieldValue(upgradeMgr, "upgradeButton",  upgBtn);
            SetFieldValue(upgradeMgr, "closeButton",    closeBtnComp);

            backdrop.SetActive(false);
            Debug.Log("[SceneSetupEditor] ✅ Shelf Upgrade UI built!");
        }

        #endregion

        // ═════════════════════════════════════════════════════════════════════
        #region UI Creation

        private void CreateUISystem()
        {
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas        = canvasObj.AddComponent<Canvas>();
            canvas.renderMode    = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler        = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight  = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            GameObject uiManagersObj = new GameObject("[ UI MANAGERS ]");
            var hudManager           = uiManagersObj.AddComponent<AIBusinessTycoon.UI.HUDManager>();
            var buildMenuManager     = uiManagersObj.AddComponent<AIBusinessTycoon.UI.BuildMenuManager>();
            var landPurchaseManager  = uiManagersObj.AddComponent<AIBusinessTycoon.UI.LandPurchaseUIManager>();
            var storeUIManager       = uiManagersObj.AddComponent<AIBusinessTycoon.UI.StoreUIManager>();

            CreateHUD(canvasObj.transform, hudManager);
            CreateBuildMenu(canvasObj.transform, buildMenuManager);
            CreateStoreUI(canvasObj.transform, storeUIManager);
            CreateLandPurchasePopup(canvasObj.transform, landPurchaseManager);
        }

        private void CreateHUD(Transform parent, AIBusinessTycoon.UI.HUDManager hudManager)
        {
            GameObject hudObj = CreateUIObject("HUD_Visuals", parent);
            StretchToParent(hudObj);

            GameObject topPanel = CreatePanel("TopPanel", hudObj.transform,
                new Vector2(0, 0.85f), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, Vector2.zero);
            topPanel.GetComponent<Image>().color = primaryColor;

            var nameObj = CreateText("PlayerName", topPanel.transform, "Player Name", 50, FontStyles.Bold, whiteColor,
                new Vector2(0.05f, 0.5f), new Vector2(0.95f, 0.9f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            nameObj.GetComponent<TextMeshProUGUI>().alignment       = TextAlignmentOptions.BottomLeft;
            nameObj.GetComponent<TextMeshProUGUI>().enableWordWrapping = false;

            var moneyObj = CreateText("Money", topPanel.transform, "Rs.10,000", 80, FontStyles.Bold, accentColor,
                new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            moneyObj.GetComponent<TextMeshProUGUI>().alignment      = TextAlignmentOptions.TopLeft;
            moneyObj.GetComponent<TextMeshProUGUI>().enableWordWrapping = false;

            GameObject statsContainer = CreateUIObject("StatsContainer", hudObj.transform);
            RectTransform statsRect   = statsContainer.GetComponent<RectTransform>();
            statsRect.anchorMin       = new Vector2(0,    0.77f);
            statsRect.anchorMax       = new Vector2(1,    0.85f);
            statsRect.pivot           = new Vector2(0.5f, 1);
            statsRect.anchoredPosition = Vector2.zero;
            statsRect.sizeDelta       = Vector2.zero;

            CreateStatCard("BizCard",  statsContainer.transform, "0", "Businesses", secondaryColor,
                new Vector2(0.02f, 0), new Vector2(0.32f, 1));
            CreateStatCard("LandCard", statsContainer.transform, "1", "Land Tiles", new Color(0.4f, 0.6f, 0.9f),
                new Vector2(0.35f, 0), new Vector2(0.65f, 1));
            CreateStatCard("RevCard",  statsContainer.transform, "Rs.0", "Revenue", accentColor,
                new Vector2(0.68f, 0), new Vector2(0.98f, 1));

            var statusObj = CreateText("StatusText", hudObj.transform, "Initializing...", 32, FontStyles.Normal, whiteColor,
                new Vector2(0, 0.72f), new Vector2(1, 0.77f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            statusObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

            GameObject loadingPanel = CreatePanel("LoadingPanel", hudObj.transform,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            loadingPanel.GetComponent<Image>().color = new Color(0, 0, 0, 0.7f);
            CreateText("LoadingText", loadingPanel.transform, "Loading...", 60, FontStyles.Bold, whiteColor,
                new Vector2(0.1f, 0.45f), new Vector2(0.9f, 0.55f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            SetFieldValue(hudManager, "playerNameText",   nameObj.GetComponent<TextMeshProUGUI>());
            SetFieldValue(hudManager, "moneyText",        moneyObj.GetComponent<TextMeshProUGUI>());
            SetFieldValue(hudManager, "statusText",       statusObj.GetComponent<TextMeshProUGUI>());
            SetFieldValue(hudManager, "loadingPanel",     loadingPanel);

            foreach (Transform child in statsContainer.transform)
            {
                var valueTexts = child.GetComponentsInChildren<TextMeshProUGUI>();
                if (child.name == "BizCard"  && valueTexts.Length >= 1) SetFieldValue(hudManager, "businessCountText", valueTexts[0]);
                if (child.name == "LandCard" && valueTexts.Length >= 1) SetFieldValue(hudManager, "landTilesText",     valueTexts[0]);
                if (child.name == "RevCard"  && valueTexts.Length >= 1) SetFieldValue(hudManager, "revenueText",       valueTexts[0]);
            }
        }

        private GameObject CreateStatCard(string name, Transform parent, string value, string label, Color color, Vector2 aMin, Vector2 aMax)
        {
            GameObject card = CreatePanel(name, parent, aMin, aMax, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            card.GetComponent<Image>().color = color;

            var valObj = CreateText("Value", card.transform, value, 48, FontStyles.Bold, whiteColor,
                new Vector2(0, 0.45f), Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            valObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

            var lblObj = CreateText("Label", card.transform, label, 26, FontStyles.Normal, new Color(1,1,1,0.8f),
                Vector2.zero, new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            lblObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

            return card;
        }

        private void CreateStoreUI(Transform parent, AIBusinessTycoon.UI.StoreUIManager storeUIManager)
        {
            GameObject storeUIObj = CreateUIObject("StoreUI_Visuals", parent);
            StretchToParent(storeUIObj);

            GameObject topBar = CreatePanel("StoreInfoBar", storeUIObj.transform,
                new Vector2(0, 0.88f), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, Vector2.zero);
            topBar.GetComponent<Image>().color = new Color(0.10f, 0.15f, 0.25f, 0.95f);

            var storeNameTxt = CreateText("StoreName", topBar.transform, "Store Name", 55, FontStyles.Bold, whiteColor,
                new Vector2(0.03f, 0.5f), new Vector2(0.75f, 1), new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
            storeNameTxt.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;

            var storeTypeTxt = CreateText("StoreType", topBar.transform, "kirana", 30, FontStyles.Normal, new Color(1,1,1,0.6f),
                new Vector2(0.03f, 0.05f), new Vector2(0.75f, 0.5f), new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
            storeTypeTxt.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;

            GameObject bottomBar = CreatePanel("BottomBar", storeUIObj.transform,
                new Vector2(0, 0), new Vector2(1, 0.12f), new Vector2(0.5f, 0), Vector2.zero, Vector2.zero);
            bottomBar.GetComponent<Image>().color = new Color(0.10f, 0.15f, 0.25f, 0.95f);

            GameObject exitBtnObj = CreatePanel("ExitButton", bottomBar.transform,
                new Vector2(0.03f, 0.1f), new Vector2(0.45f, 0.9f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            exitBtnObj.GetComponent<Image>().color = new Color(0.85f, 0.20f, 0.20f);
            Button exitBtn = exitBtnObj.AddComponent<Button>();
            CreateText("Text", exitBtnObj.transform, "🚪 EXIT STORE", 38, FontStyles.Bold, whiteColor,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero)
                .GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

            GameObject hireBtnObj = CreatePanel("HireStaffButton", bottomBar.transform,
                new Vector2(0.55f, 0.1f), new Vector2(0.97f, 0.9f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            hireBtnObj.GetComponent<Image>().color = hireButtonColor;
            Button hireBtn = hireBtnObj.AddComponent<Button>();
            CreateText("Text", hireBtnObj.transform, "👥 HIRE STAFF", 38, FontStyles.Bold, whiteColor,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero)
                .GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

            GameObject joystickPanel = CreateUIObject("JoystickPanel", storeUIObj.transform);
            var joystickRect         = joystickPanel.GetComponent<RectTransform>();
            joystickRect.anchorMin   = new Vector2(0.02f, 0.12f);
            joystickRect.anchorMax   = new Vector2(0.48f, 0.40f);
            joystickRect.pivot       = new Vector2(0, 0);
            joystickRect.anchoredPosition = Vector2.zero;
            joystickRect.sizeDelta   = Vector2.zero;
            joystickPanel.SetActive(false);

            SetFieldValue(storeUIManager, "storeUIPanel",    storeUIObj);
            SetFieldValue(storeUIManager, "storeNameText",   storeNameTxt.GetComponent<TextMeshProUGUI>());
            SetFieldValue(storeUIManager, "storeTypeText",   storeTypeTxt.GetComponent<TextMeshProUGUI>());
            SetFieldValue(storeUIManager, "exitButton",      exitBtn);
            SetFieldValue(storeUIManager, "hireStaffButton", hireBtn);
            SetFieldValue(storeUIManager, "joystickPanel",   joystickPanel);

            CreateHireMenuPanel(storeUIObj.transform, storeUIManager);
        }

        private void CreateHireMenuPanel(Transform parent, AIBusinessTycoon.UI.StoreUIManager storeUIManager)
        {
            GameObject backdrop = CreatePanel("HireMenuPanel", parent,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            backdrop.GetComponent<Image>().color = new Color(0, 0, 0, 0.65f);

            GameObject card = CreatePanel("HireCard", backdrop.transform,
                new Vector2(0.04f, 0.10f), new Vector2(0.96f, 0.90f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            card.GetComponent<Image>().color = darkPanelColor;

            GameObject titleBar = CreatePanel("TitleBar", card.transform,
                new Vector2(0, 0.88f), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, Vector2.zero);
            titleBar.GetComponent<Image>().color = new Color(1, 1, 1, 0.05f);

            CreateText("TitleText", titleBar.transform, "👥  HIRE STAFF", 52, FontStyles.Bold, Color.white,
                new Vector2(0.04f, 0), new Vector2(0.82f, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero)
                .GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;

            GameObject closeBtn = CreatePanel("CloseHireMenuButton", titleBar.transform,
                new Vector2(0.84f, 0.1f), new Vector2(0.98f, 0.9f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            closeBtn.GetComponent<Image>().color = new Color(0.85f, 0.20f, 0.20f);
            Button closeBtnComp = closeBtn.AddComponent<Button>();
            CreateText("Text", closeBtn.transform, "✕", 46, FontStyles.Bold, Color.white,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero)
                .GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

            CreateText("Subtitle", card.transform,
                "Hire AI employees to automate your store.", 28, FontStyles.Normal, new Color(1, 1, 1, 0.55f),
                new Vector2(0.04f, 0.80f), new Vector2(0.96f, 0.88f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero)
                .GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;

            GameObject divider = CreatePanel("Divider", card.transform,
                new Vector2(0.04f, 0.775f), new Vector2(0.96f, 0.778f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            divider.GetComponent<Image>().color = new Color(1, 1, 1, 0.12f);

            var ratingTMP = CreateStoreRatingBar(card.transform);

            var (_, cashierHireBtn, cashierStatus) = CreateEmployeeRow(card.transform, "CashierRow", 0.54f, 0.76f, "💰", "Cashier", "Stands at the register & processes\ncustomers automatically.", "Rs. 1,000", cashierColor);
            var (_, restockerHireBtn, restockerStatus) = CreateEmployeeRow(card.transform, "RestockerRow", 0.29f, 0.52f, "📦", "Restocker", "Autonomously walks to the supply zone\nand restocks empty shelves.", "Rs. 1,500", restockerColor);
            var (_, cleanerHireBtn, cleanerStatus) = CreateEmployeeRow(card.transform, "CleanerRow", 0.04f, 0.27f, "🧹", "Cleaner", "Roams the store cleaning trash.\nKeeps your Store Rating high.", "Rs. 800", cleanerColor);

            SetFieldValue(storeUIManager, "hireMenuPanel",       backdrop);
            SetFieldValue(storeUIManager, "hireCashierButton",   cashierHireBtn);
            SetFieldValue(storeUIManager, "hireRestockerButton", restockerHireBtn);
            SetFieldValue(storeUIManager, "hireCleanerButton",   cleanerHireBtn);    
            SetFieldValue(storeUIManager, "closeHireMenuButton", closeBtnComp);
            SetFieldValue(storeUIManager, "cashierStatusText",   cashierStatus);
            SetFieldValue(storeUIManager, "restockerStatusText", restockerStatus);
            SetFieldValue(storeUIManager, "cleanerStatusText",   cleanerStatus);     
            SetFieldValue(storeUIManager, "storeRatingText",     ratingTMP);         

            var cashierPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/CashierPrefab.prefab");
            var restockerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/RestockerPrefab.prefab");
            var cleanerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/CleanerPrefab.prefab");

            if (cashierPrefab   != null) SetFieldValue(storeUIManager, "cashierPrefab",   cashierPrefab);
            if (restockerPrefab != null) SetFieldValue(storeUIManager, "restockerPrefab", restockerPrefab);
            if (cleanerPrefab   != null) SetFieldValue(storeUIManager, "cleanerPrefab",   cleanerPrefab); 

            backdrop.SetActive(false);
        }

        private TextMeshProUGUI CreateStoreRatingBar(Transform cardParent)
        {
            GameObject ratingBar = CreatePanel("StoreRatingBar", cardParent,
                new Vector2(0.04f, 0.745f), new Vector2(0.96f, 0.775f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            ratingBar.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.04f);

            var labelObj = CreateText("RatingLabel", ratingBar.transform, "Store Rating", 26, FontStyles.Bold, new Color(1f, 1f, 1f, 0.70f),
                Vector2.zero, new Vector2(0.45f, 1f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            labelObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;

            var valueObj = CreateText("RatingValue", ratingBar.transform, "⭐ 5.0/5.0", 26, FontStyles.Bold, ratingGoldColor,
                new Vector2(0.50f, 0f), Vector2.one, new Vector2(1f, 0.5f), Vector2.zero, Vector2.zero);
            var valueTMP = valueObj.GetComponent<TextMeshProUGUI>();
            valueTMP.alignment = TextAlignmentOptions.MidlineRight;

            return valueTMP;
        }

        private (GameObject row, Button hireBtn, TextMeshProUGUI statusTxt)
            CreateEmployeeRow(Transform parent, string rowName, float anchorYMin, float anchorYMax, string emoji, string roleName, string roleDesc, string cost, Color accentCol)
        {
            GameObject row = CreatePanel(rowName, parent, new Vector2(0.04f, anchorYMin), new Vector2(0.96f, anchorYMax), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            row.GetComponent<Image>().color = rowBgColor;

            GameObject accentBar = CreatePanel("AccentBar", row.transform, new Vector2(0, 0), new Vector2(0.012f, 1), new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
            accentBar.GetComponent<Image>().color = accentCol;

            var emojiObj = CreateText("Emoji", row.transform, emoji, 70, FontStyles.Normal, Color.white, new Vector2(0.03f, 0), new Vector2(0.18f, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            emojiObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

            var roleNameObj = CreateText("RoleName", row.transform, roleName, 44, FontStyles.Bold, Color.white, new Vector2(0.18f, 0.55f), new Vector2(0.65f, 1), new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
            roleNameObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;

            var roleDescObj = CreateText("RoleDesc", row.transform, roleDesc, 24, FontStyles.Normal, new Color(1, 1, 1, 0.6f), new Vector2(0.18f, 0), new Vector2(0.65f, 0.54f), new Vector2(0, 0), Vector2.zero, Vector2.zero);
            var roleDescTMP = roleDescObj.GetComponent<TextMeshProUGUI>();
            roleDescTMP.alignment         = TextAlignmentOptions.TopLeft;
            roleDescTMP.enableWordWrapping = true;

            var statusObj = CreateText("StatusText", row.transform, "✔ Can Afford", 26, FontStyles.Normal, secondaryColor, new Vector2(0.18f, 0), new Vector2(0.65f, 0.32f), new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
            var statusTMP = statusObj.GetComponent<TextMeshProUGUI>();
            statusTMP.alignment = TextAlignmentOptions.BottomLeft;

            GameObject hireBtnObj = CreatePanel("HireButton", row.transform, new Vector2(0.67f, 0.12f), new Vector2(0.97f, 0.88f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            hireBtnObj.GetComponent<Image>().color = accentCol;
            Button hireBtnComp = hireBtnObj.AddComponent<Button>();

            ColorBlock colors       = hireBtnComp.colors;
            colors.highlightedColor = new Color(Mathf.Min(accentCol.r + 0.15f, 1f), Mathf.Min(accentCol.g + 0.15f, 1f), Mathf.Min(accentCol.b + 0.15f, 1f));
            colors.pressedColor = new Color(Mathf.Max(accentCol.r - 0.2f, 0f), Mathf.Max(accentCol.g - 0.2f, 0f), Mathf.Max(accentCol.b - 0.2f, 0f));
            hireBtnComp.colors = colors;

            var costObj = CreateText("CostText", hireBtnObj.transform, cost, 30, FontStyles.Bold, Color.white, new Vector2(0, 0.52f), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            costObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

            var hireLblObj = CreateText("HireLabel", hireBtnObj.transform, "HIRE", 26, FontStyles.Bold, new Color(1, 1, 1, 0.85f), new Vector2(0, 0), new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            hireLblObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

            return (row, hireBtnComp, statusTMP);
        }

        private void CreateBuildMenu(Transform parent, AIBusinessTycoon.UI.BuildMenuManager buildMenuManager)
        {
            GameObject menuObj = CreateUIObject("BuildMenu_Visuals", parent);
            StretchToParent(menuObj);

            GameObject menuPanel = CreatePanel("MenuPanel", menuObj.transform,
                new Vector2(0, 0), new Vector2(1, 0.6f), new Vector2(0.5f, 0), Vector2.zero, Vector2.zero);
            menuPanel.GetComponent<Image>().color = new Color(0.95f, 0.95f, 0.95f, 0.98f);

            var title = CreateText("Title", menuPanel.transform, "BUILD MENU", 55, FontStyles.Bold, darkTextColor,
                new Vector2(0, 0.85f), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            title.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

            GameObject closeBtn = CreatePanel("CloseButton", menuPanel.transform,
                new Vector2(0.85f, 0.85f), new Vector2(0.98f, 0.98f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            closeBtn.GetComponent<Image>().color = new Color(0.85f, 0.20f, 0.20f);
            Button closeBtnComp = closeBtn.AddComponent<Button>();
            CreateText("Text", closeBtn.transform, "✕", 40, FontStyles.Bold, whiteColor,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero)
                .GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

            GameObject cardsContainer    = CreateUIObject("CardsContainer", menuPanel.transform);
            RectTransform cardsRect      = cardsContainer.GetComponent<RectTransform>();
            cardsRect.anchorMin          = new Vector2(0.05f, 0);
            cardsRect.anchorMax          = new Vector2(0.95f, 0.85f);
            cardsRect.anchoredPosition   = Vector2.zero;
            cardsRect.sizeDelta          = Vector2.zero;

            GameObject kiranaCard = CreateBuildingButton("Card_Kirana", cardsContainer.transform, "Kirana Store", "Rs.5,000",  secondaryColor, new Vector2(0, 0.68f), new Vector2(1, 0.98f));
            GameObject pizzaCard  = CreateBuildingButton("Card_Pizza",  cardsContainer.transform, "Pizza Outlet", "Rs.15,000", new Color(0.9f, 0.3f, 0.3f), new Vector2(0, 0.35f), new Vector2(1, 0.65f));
            GameObject cafeCard   = CreateBuildingButton("Card_Cafe",   cardsContainer.transform, "Cafe",         "Rs.25,000", new Color(0.4f, 0.3f, 0.2f), new Vector2(0, 0.02f), new Vector2(1, 0.32f));

            var kiranaInfo = kiranaCard.transform.Find("Title").GetComponent<TextMeshProUGUI>();
            var pizzaInfo  = pizzaCard.transform.Find("Title").GetComponent<TextMeshProUGUI>();
            var cafeInfo   = cafeCard.transform.Find("Title").GetComponent<TextMeshProUGUI>();

            var kPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/KiranaStore.prefab");
            var pPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/PizzaOutlet.prefab");
            var cPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Cafe.prefab");

            SetFieldValue(buildMenuManager, "menuPanel",      menuPanel);
            SetFieldValue(buildMenuManager, "closeButton",    closeBtnComp);
            SetFieldValue(buildMenuManager, "kiranaButton",   kiranaCard.GetComponent<Button>());
            SetFieldValue(buildMenuManager, "pizzaButton",    pizzaCard.GetComponent<Button>());
            SetFieldValue(buildMenuManager, "cafeButton",     cafeCard.GetComponent<Button>());
            SetFieldValue(buildMenuManager, "kiranaInfoText", kiranaInfo);
            SetFieldValue(buildMenuManager, "pizzaInfoText",  pizzaInfo);
            SetFieldValue(buildMenuManager, "cafeInfoText",   cafeInfo);

            if (kPrefab != null) SetFieldValue(buildMenuManager, "kiranaPrefab", kPrefab);
            if (pPrefab != null) SetFieldValue(buildMenuManager, "pizzaPrefab", pPrefab);
            if (cPrefab != null) SetFieldValue(buildMenuManager, "cafePrefab", cPrefab);

            menuPanel.SetActive(false);
        }

        private GameObject CreateBuildingButton(string name, Transform parent, string title, string cost, Color color, Vector2 aMin, Vector2 aMax)
        {
            GameObject btnObj = CreatePanel(name, parent, aMin, aMax, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            btnObj.GetComponent<Image>().color = color;
            Button btn = btnObj.AddComponent<Button>();

            var tTxt = CreateText("Title", btnObj.transform, title, 50, FontStyles.Bold, whiteColor, new Vector2(0.05f, 0), new Vector2(0.6f, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            tTxt.GetComponent<TextMeshProUGUI>().alignment         = TextAlignmentOptions.MidlineLeft;
            tTxt.GetComponent<TextMeshProUGUI>().enableWordWrapping = false;

            var cTxt = CreateText("Cost", btnObj.transform, cost, 40, FontStyles.Bold, accentColor, new Vector2(0.6f, 0), new Vector2(0.98f, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            cTxt.GetComponent<TextMeshProUGUI>().alignment         = TextAlignmentOptions.MidlineRight;
            cTxt.GetComponent<TextMeshProUGUI>().enableWordWrapping = false;

            return btnObj;
        }

        private void CreateLandPurchasePopup(Transform parent, AIBusinessTycoon.UI.LandPurchaseUIManager manager)
        {
            GameObject popup = CreatePanel("LandPurchasePopup", parent,
                new Vector2(0.05f, 0.30f), new Vector2(0.95f, 0.70f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            popup.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.97f);

            var titleText = CreateText("Title", popup.transform, "🏠 BUY LAND", 50, FontStyles.Bold, whiteColor,
                new Vector2(0, 0.8f), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero)
                .GetComponent<TextMeshProUGUI>();
            titleText.alignment = TextAlignmentOptions.Center;

            var posText = CreateText("PositionText", popup.transform, "Location: (1, 0)", 32, FontStyles.Normal, new Color(1,1,1,0.8f),
                new Vector2(0, 0.65f), new Vector2(1, 0.8f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero)
                .GetComponent<TextMeshProUGUI>();
            posText.alignment = TextAlignmentOptions.Center;

            var costText = CreateText("CostText", popup.transform, "Cost: Rs.2,400", 40, FontStyles.Bold, accentColor,
                new Vector2(0, 0.5f), new Vector2(1, 0.65f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero)
                .GetComponent<TextMeshProUGUI>();
            costText.alignment = TextAlignmentOptions.Center;

            var moneyText = CreateText("PlayerMoneyText", popup.transform, "Your Money: Rs.10,000", 30, FontStyles.Normal, secondaryColor,
                new Vector2(0, 0.35f), new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero)
                .GetComponent<TextMeshProUGUI>();
            moneyText.alignment = TextAlignmentOptions.Center;

            var affordText = CreateText("AffordabilityText", popup.transform, "Can Afford", 30, FontStyles.Bold, secondaryColor,
                new Vector2(0, 0.25f), new Vector2(1, 0.35f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero)
                .GetComponent<TextMeshProUGUI>();
            affordText.alignment = TextAlignmentOptions.Center;

            GameObject confirmObj = CreatePanel("ConfirmButton", popup.transform,
                new Vector2(0.05f, 0.05f), new Vector2(0.47f, 0.22f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            confirmObj.GetComponent<Image>().color = secondaryColor;
            Button confirmBtn = confirmObj.AddComponent<Button>();
            var confirmBtnText = CreateText("Text", confirmObj.transform, "✔ BUY", 40, FontStyles.Bold, whiteColor,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero)
                .GetComponent<TextMeshProUGUI>();
            confirmBtnText.alignment = TextAlignmentOptions.Center;

            GameObject cancelObj = CreatePanel("CancelButton", popup.transform,
                new Vector2(0.53f, 0.05f), new Vector2(0.95f, 0.22f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            cancelObj.GetComponent<Image>().color = new Color(0.8f, 0.2f, 0.2f);
            Button cancelBtn = cancelObj.AddComponent<Button>();
            CreateText("Text", cancelObj.transform, "✕ CANCEL", 40, FontStyles.Bold, whiteColor,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero)
                .GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

            SetFieldValue(manager, "popupPanel",        popup);
            SetFieldValue(manager, "titleText",         titleText);
            SetFieldValue(manager, "positionText",      posText);
            SetFieldValue(manager, "costText",          costText);
            SetFieldValue(manager, "playerMoneyText",   moneyText);
            SetFieldValue(manager, "affordabilityText", affordText);
            SetFieldValue(manager, "confirmButton",     confirmBtn);
            SetFieldValue(manager, "confirmButtonText", confirmBtnText);
            SetFieldValue(manager, "cancelButton",      cancelBtn);

            popup.SetActive(false);
        }

        #endregion

        // ═════════════════════════════════════════════════════════════════════
        #region Asset Creation

        private void CreateFolderStructure()
        {
            string[] folders = {
                "Assets/Scripts", "Assets/Scripts/Config", "Assets/Scripts/Data", "Assets/Scripts/Game",
                "Assets/Scripts/Game/Managers", "Assets/Scripts/Game/Services", "Assets/Materials",
                "Assets/Materials/Grid", "Assets/Materials/Buildings", "Assets/Materials/Employees",
                "Assets/Prefabs", "Assets/Prefabs/Grid", "Assets/Prefabs/Buildings", "Assets/Editor"
            };

            foreach (string path in folders)
            {
                if (!AssetDatabase.IsValidFolder(path))
                {
                    string parent = Path.GetDirectoryName(path);
                    string folder = Path.GetFileName(path);
                    AssetDatabase.CreateFolder(parent, folder);
                }
            }
        }

        private void CreateMaterials()
        {
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            CreateMaterial("Assets/Materials/Grid/EmptyTile.mat",        urpShader, new Color(0.35f, 0.35f, 0.35f));
            CreateMaterial("Assets/Materials/Grid/OwnedTile.mat",        urpShader, new Color(0.20f, 0.60f, 0.30f));
            CreateMaterial("Assets/Materials/Grid/ValidPlacement.mat",   urpShader, new Color(0.10f, 0.80f, 0.20f, 0.5f));
            CreateMaterial("Assets/Materials/Grid/InvalidPlacement.mat", urpShader, new Color(0.90f, 0.10f, 0.10f, 0.5f));
            CreateMaterial("Assets/Materials/Grid/PurchasableTile.mat",  urpShader, new Color(0.90f, 0.70f, 0.10f, 0.6f));
            CreateMaterial("Assets/Materials/Buildings/Prop_Shelf.mat",  urpShader, new Color(0.55f, 0.40f, 0.25f));
            CreateMaterial("Assets/Materials/Buildings/Prop_Counter.mat",urpShader, new Color(0.70f, 0.65f, 0.55f));
            CreateMaterial("Assets/Materials/Buildings/Kirana.mat",      urpShader, new Color(0.95f, 0.90f, 0.80f));
            CreateMaterial("Assets/Materials/Buildings/Kirana_Theme.mat",urpShader, new Color(0.10f, 0.70f, 0.30f));
            CreateMaterial("Assets/Materials/Buildings/Pizza.mat",       urpShader, new Color(1.00f, 0.85f, 0.60f));
            CreateMaterial("Assets/Materials/Buildings/Pizza_Theme.mat", urpShader, new Color(0.90f, 0.20f, 0.10f));
            CreateMaterial("Assets/Materials/Buildings/Cafe.mat",        urpShader, new Color(0.60f, 0.40f, 0.20f));
            CreateMaterial("Assets/Materials/Buildings/Cafe_Theme.mat",  urpShader, new Color(0.35f, 0.20f, 0.10f));
        }

        private void CreateEmployeeMaterials()
        {
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            CreateMaterial("Assets/Materials/Employees/Cashier_AI.mat",   urpShader, new Color(0.20f, 0.40f, 1.00f)); 
            CreateMaterial("Assets/Materials/Employees/Restocker_AI.mat", urpShader, new Color(1.00f, 0.50f, 0.10f)); 
        }

        private void CreateCleanerMaterial()
        {
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            CreateMaterial("Assets/Materials/Employees/Cleaner_AI.mat", urpShader, new Color(0.20f, 0.80f, 0.30f)); 
        }

        private void CreateMaterial(string path, Shader shader, Color color)
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
            Material mat = new Material(shader);
            mat.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(mat, path);
        }

        private void CreatePrefabs()
        {
            CreateTilePrefab();
            CreateBuildingPrefabs();
            CreateCustomerPrefab();
        }

        private void CreateEmployeePrefabs()
        {
            CreateEmployeePrefab("Assets/Prefabs/Buildings/CashierPrefab.prefab", "Assets/Materials/Employees/Cashier_AI.mat", typeof(AIBusinessTycoon.Managers.CashierAI));
            CreateEmployeePrefab("Assets/Prefabs/Buildings/RestockerPrefab.prefab", "Assets/Materials/Employees/Restocker_AI.mat", typeof(AIBusinessTycoon.Managers.RestockerAI));
        }

        private void CreateCleanerPrefab()
        {
            CreateEmployeePrefab("Assets/Prefabs/Buildings/CleanerPrefab.prefab", "Assets/Materials/Employees/Cleaner_AI.mat", typeof(AIBusinessTycoon.Managers.CleanerAI));
        }

        private void CreateEmployeePrefab(string prefabPath, string matPath, System.Type aiScript)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null) return;

            GameObject root   = new GameObject(Path.GetFileNameWithoutExtension(prefabPath));
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name       = "Visual";
            visual.transform.SetParent(root.transform);
            visual.transform.localPosition = new Vector3(0, 1f, 0);
            visual.transform.localScale    = Vector3.one;

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat != null) visual.GetComponent<Renderer>().sharedMaterial = mat;
            DestroyImmediate(visual.GetComponent<Collider>());

            NavMeshAgent agent  = root.AddComponent<NavMeshAgent>();
            agent.radius        = 0.4f;
            agent.height        = 2.0f;
            agent.speed         = 4.0f;
            agent.angularSpeed  = 180f;
            agent.stoppingDistance = 0.5f;
            agent.autoBraking   = true;

            root.AddComponent(aiScript);

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            DestroyImmediate(root);
        }

        private void CreateTilePrefab()
        {
            const string path = "Assets/Prefabs/Grid/TilePrefab.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

            GameObject tile   = GameObject.CreatePrimitive(PrimitiveType.Quad);
            tile.name         = "TilePrefab";
            tile.transform.rotation = Quaternion.Euler(90, 0, 0);

            Material mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Grid/EmptyTile.mat");
            if (mat != null) tile.GetComponent<Renderer>().sharedMaterial = mat;

            PrefabUtility.SaveAsPrefabAsset(tile, path);
            DestroyImmediate(tile);
        }

        private void CreateBuildingPrefabs()
        {
            CreateStorePrefab("Assets/Prefabs/Buildings/KiranaStore.prefab", "Assets/Materials/Buildings/Kirana.mat", "Assets/Materials/Buildings/Kirana_Theme.mat");
            CreateStorePrefab("Assets/Prefabs/Buildings/PizzaOutlet.prefab", "Assets/Materials/Buildings/Pizza.mat", "Assets/Materials/Buildings/Pizza_Theme.mat");
            CreateStorePrefab("Assets/Prefabs/Buildings/Cafe.prefab", "Assets/Materials/Buildings/Cafe.mat", "Assets/Materials/Buildings/Cafe_Theme.mat");
        }

        private void CreateStorePrefab(string prefabPath, string wallMatPath, string themeMatPath)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null) return;

            GameObject root = new GameObject(Path.GetFileNameWithoutExtension(prefabPath));

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name       = "Floor";
            floor.transform.SetParent(root.transform);
            floor.transform.localPosition = new Vector3(0, -0.1f, 0);
            floor.transform.localScale    = new Vector3(10, 0.2f, 10);
            Material wallMat = AssetDatabase.LoadAssetAtPath<Material>(wallMatPath);
            if (wallMat != null) floor.GetComponent<Renderer>().sharedMaterial = wallMat;

            BoxCollider clickCollider = root.AddComponent<BoxCollider>();
            clickCollider.center = new Vector3(0, 1f, 0);
            clickCollider.size   = new Vector3(10, 2f, 10);

            GameObject counter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            counter.name       = "CheckoutCounter";
            counter.transform.SetParent(root.transform);
            counter.transform.localPosition = new Vector3(2f, 0.5f, -3f);
            counter.transform.localScale    = new Vector3(2f, 1f, 1f);
            Material counterMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Buildings/Prop_Counter.mat");
            if (counterMat != null) counter.GetComponent<Renderer>().sharedMaterial = counterMat;
            counter.AddComponent<AIBusinessTycoon.Managers.CheckoutCounter>();

            Material shelfMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Buildings/Prop_Shelf.mat");
            for (int i = 0; i < 3; i++)
            {
                GameObject shelf = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shelf.name       = $"Shelf_{i}";
                shelf.transform.SetParent(root.transform);
                shelf.transform.localPosition = new Vector3(-3f + i * 2f, 0.75f, 2f);
                shelf.transform.localScale    = new Vector3(1.5f, 1.5f, 0.5f);
                if (shelfMat != null) shelf.GetComponent<Renderer>().sharedMaterial = shelfMat;
                shelf.AddComponent<AIBusinessTycoon.Managers.InteractableShelf>();
            }

            root.AddComponent<AIBusinessTycoon.Managers.StoreInteractionManager>();

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            DestroyImmediate(root);
        }

        private void CreateCustomerPrefab()
        {
            const string path = "Assets/Prefabs/Buildings/Customer.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

            GameObject root   = new GameObject("Customer");
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name       = "Visual";
            visual.transform.SetParent(root.transform);
            visual.transform.localPosition = new Vector3(0, 1f, 0);
            DestroyImmediate(visual.GetComponent<Collider>());

            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material mat     = new Material(urpShader);
            mat.SetColor("_BaseColor", new Color(0.6f, 0.2f, 0.8f));
            visual.GetComponent<Renderer>().material = mat;

            NavMeshAgent agent  = root.AddComponent<NavMeshAgent>();
            agent.radius        = 0.4f;
            agent.height        = 2.0f;
            agent.speed         = 3.5f;
            agent.angularSpeed  = 200f;
            agent.stoppingDistance = 0.5f;

            root.AddComponent<AIBusinessTycoon.Managers.CustomerAI>();

            PrefabUtility.SaveAsPrefabAsset(root, path);
            DestroyImmediate(root);
        }

        #endregion

        // ═════════════════════════════════════════════════════════════════════
        #region Scene Wiring

        private void ConnectComponents()
        {
            var gameManager    = FindObjectOfType<AIBusinessTycoon.Managers.GameManager>();
            var storeUIManager = FindObjectOfType<AIBusinessTycoon.UI.StoreUIManager>();

            if (gameManager != null)
            {
                var config = AssetDatabase.LoadAssetAtPath<AIBusinessTycoon.Config.BackendConfig>("Assets/BackendConfig.asset");
                if (config != null) SetFieldValue(gameManager, "backendConfig", config);

                var gridManager = FindObjectOfType<AIBusinessTycoon.Managers.GridManager>();
                if (gridManager != null) SetFieldValue(gameManager, "gridManager", gridManager);

                var camController = FindObjectOfType<AIBusinessTycoon.Managers.CameraController>();
                if (camController != null) SetFieldValue(gameManager, "cameraController", camController);

                var bpm = FindObjectOfType<AIBusinessTycoon.Managers.BuildingPlacementManager>();
                if (bpm != null) SetFieldValue(gameManager, "buildingPlacementManager", bpm);

                var playerController = FindObjectOfType<AIBusinessTycoon.Managers.PlayerController>();
                if (playerController != null) SetFieldValue(gameManager, "playerController", playerController);

                SetFieldValue(gameManager, "kiranaPrefab", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/KiranaStore.prefab"));
                SetFieldValue(gameManager, "pizzaPrefab", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/PizzaOutlet.prefab"));
                SetFieldValue(gameManager, "cafePrefab", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Cafe.prefab"));
                SetFieldValue(gameManager, "cashierPrefab", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/CashierPrefab.prefab"));
                SetFieldValue(gameManager, "restockerPrefab", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/RestockerPrefab.prefab"));
                SetFieldValue(gameManager, "cleanerPrefab", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/CleanerPrefab.prefab"));
            }

            if (storeUIManager != null)
            {
                var config = AssetDatabase.LoadAssetAtPath<AIBusinessTycoon.Config.BackendConfig>("Assets/BackendConfig.asset");
                if (config != null) SetFieldValue(storeUIManager, "backendConfig", config);

                SetFieldValue(storeUIManager, "cashierPrefab", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/CashierPrefab.prefab"));
                SetFieldValue(storeUIManager, "restockerPrefab", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/RestockerPrefab.prefab"));
                SetFieldValue(storeUIManager, "cleanerPrefab", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/CleanerPrefab.prefab"));
            }

            var spawner = FindObjectOfType<AIBusinessTycoon.Managers.CustomerSpawner>();
            if (spawner == null)
            {
                GameObject spawnerObj = new GameObject("CustomerSpawner");
                spawner = spawnerObj.AddComponent<AIBusinessTycoon.Managers.CustomerSpawner>();
            }

            GameObject spawnPointObj = GameObject.Find("CustomerSpawnPoint");
            if (spawnPointObj == null)
            {
                spawnPointObj = new GameObject("CustomerSpawnPoint");
                spawnPointObj.transform.position = new Vector3(0, 0.5f, -20f);
            }

            SetFieldValue(spawner, "spawnPoint", spawnPointObj.transform);
            var custPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Customer.prefab");
            if (custPrefab != null) SetFieldValue(spawner, "customerPrefab", custPrefab);
        }

        #endregion

        // ═════════════════════════════════════════════════════════════════════
        #region Layer Setup

        private void SetupLayers()
        {
            CreateLayer("Grid");
            CreateLayer("Building");
        }

        private void CreateLayer(string layerName)
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");

            for (int i = 8; i < layers.arraySize; i++)
                if (layers.GetArrayElementAtIndex(i).stringValue == layerName) return;

            for (int i = 8; i < layers.arraySize; i++)
            {
                SerializedProperty layerSP = layers.GetArrayElementAtIndex(i);
                if (layerSP.stringValue == "")
                {
                    layerSP.stringValue = layerName;
                    tagManager.ApplyModifiedProperties();
                    return;
                }
            }
        }

        #endregion

        // ═════════════════════════════════════════════════════════════════════
        #region World Setup

        private void ClearScene()
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas  != null) DestroyImmediate(canvas);

            var es = GameObject.Find("EventSystem");
            if (es != null) DestroyImmediate(es);

            var uiMgr = GameObject.Find("[ UI MANAGERS ]");
            if (uiMgr != null) DestroyImmediate(uiMgr);
        }

        private void CreateEventSystem()
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private void CreateEnvironment()
        {
            GameObject lightObj = GameObject.Find("Directional Light");
            if (lightObj == null)
            {
                lightObj        = new GameObject("Directional Light");
                Light light     = lightObj.AddComponent<Light>();
                light.type      = LightType.Directional;
                light.intensity = 1.2f;
                light.color     = new Color(1f, 0.96f, 0.84f);
                lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);
            }

            GameObject ground = GameObject.Find("Ground");
            if (ground == null)
            {
                ground      = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "Ground";
            }

            ground.transform.position   = new Vector3(0, -0.1f, 0);
            ground.transform.localScale = new Vector3(100, 1, 100);
            ground.layer                = LayerMask.NameToLayer("Grid");

            Shader urpShader   = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material groundMat = new Material(urpShader);
            groundMat.SetColor("_BaseColor", new Color(0.3f, 0.4f, 0.3f));
            ground.GetComponent<Renderer>().material = groundMat;

            var navMeshSurface = ground.GetComponent<Unity.AI.Navigation.NavMeshSurface>();
            if (navMeshSurface == null)
                navMeshSurface = ground.AddComponent<Unity.AI.Navigation.NavMeshSurface>();

            navMeshSurface.layerMask   = LayerMask.GetMask("Grid");
            navMeshSurface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
            navMeshSurface.BuildNavMesh();
        }

        private void CreatePlayerAvatar()
        {
            GameObject playerObj = GameObject.Find("PlayerAvatar");
            if (playerObj == null) playerObj = new GameObject("PlayerAvatar");
            playerObj.tag = "Player";

            CharacterController cc = playerObj.GetComponent<CharacterController>();
            if (cc == null) cc = playerObj.AddComponent<CharacterController>();
            cc.radius = 0.5f;
            cc.height = 2f;
            cc.center = new Vector3(0, 1f, 0);

            var playerController = playerObj.GetComponent<AIBusinessTycoon.Managers.PlayerController>();
            if (playerController == null)
                playerController = playerObj.AddComponent<AIBusinessTycoon.Managers.PlayerController>();

            Transform visualTrans = playerObj.transform.Find("Visual");
            GameObject visual;
            if (visualTrans == null)
            {
                visual      = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                visual.name = "Visual";
                visual.transform.SetParent(playerObj.transform);
                visual.transform.localPosition = new Vector3(0, 1f, 0);

                GameObject face = GameObject.CreatePrimitive(PrimitiveType.Cube);
                face.name       = "Face";
                face.transform.SetParent(visual.transform);
                face.transform.localScale    = new Vector3(0.5f, 0.5f, 0.5f);
                face.transform.localPosition = new Vector3(0, 0.5f, 0.5f);
            }
            else
            {
                visual = visualTrans.gameObject;
            }

            SetFieldValue(playerController, "avatarVisual", visual);
            SetFieldValue(playerController, "moveSpeed",    8f);

            playerObj.layer = LayerMask.NameToLayer("Default");
            if (playerObj.GetComponent<AIBusinessTycoon.Managers.PlayerInventory>() == null)
                playerObj.AddComponent<AIBusinessTycoon.Managers.PlayerInventory>();

            playerObj.SetActive(false);
        }

        private void CreateSupplyZone()
        {
            GameObject zone = GameObject.FindGameObjectWithTag("SupplyZone");
            if (zone != null) return;

            zone      = GameObject.CreatePrimitive(PrimitiveType.Cube);
            zone.name = "SupplyZone";
            zone.tag  = "SupplyZone";
            zone.transform.position   = new Vector3(0, 0.5f, -6f);
            zone.transform.localScale = new Vector3(3f, 1f, 3f);

            var col       = zone.GetComponent<BoxCollider>();
            col.isTrigger = true;

            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material mat     = new Material(urpShader);
            mat.SetColor("_BaseColor", new Color(0.2f, 0.8f, 1f, 0.6f));
            zone.GetComponent<Renderer>().material = mat;
        }

        // ═════════════════════════════════════════════════════════════════════
        #region Store Layout Patcher

        /// <summary>
        /// Rewrites the existing prefabs to have the exact number of shelves 
        /// needed for their inventory (Kirana = 8, Pizza/Cafe = 4).
        /// </summary>
        private void PatchStoreLayouts()
        {
            UpdateStorePrefab("Assets/Prefabs/Buildings/KiranaStore.prefab", 8);
            UpdateStorePrefab("Assets/Prefabs/Buildings/PizzaOutlet.prefab", 4);
            UpdateStorePrefab("Assets/Prefabs/Buildings/Cafe.prefab", 4);
            
            Debug.Log("[SceneSetupEditor] ✅ Store layouts upgraded with proper aisles!");
            EditorUtility.DisplayDialog("Layouts Upgraded", "All store prefabs now have the correct number of shelves for their inventory!", "Awesome");
        }

        private void UpdateStorePrefab(string path, int shelfCount)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) return;
            
            // Instantiate the prefab temporarily to modify it safely
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            
            // Delete the old 3-shelf layout
            var oldShelves = instance.GetComponentsInChildren<AIBusinessTycoon.Managers.InteractableShelf>();
            foreach (var s in oldShelves) DestroyImmediate(s.gameObject);
            
            Material shelfMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Buildings/Prop_Shelf.mat");
            
            // Build the new shelves in proper aisles (4 shelves per row)
            for (int i = 0; i < shelfCount; i++)
            {
                GameObject shelf = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shelf.name       = $"Shelf_{i}";
                shelf.transform.SetParent(instance.transform);
                
                int row = i / 4; 
                int col = i % 4;
                
                // Calculates a clean grid layout inside the 10x10 store
                float posX = -3.0f + (col * 2.0f);
                float posZ =  2.5f - (row * 2.0f); 
                
                shelf.transform.localPosition = new Vector3(posX, 0.75f, posZ);
                shelf.transform.localScale    = new Vector3(1.5f, 1.5f, 0.5f);
                
                if (shelfMat != null) shelf.GetComponent<Renderer>().sharedMaterial = shelfMat;
                
                shelf.AddComponent<AIBusinessTycoon.Managers.InteractableShelf>();
            }
            
            // Save changes and clean up
            PrefabUtility.SaveAsPrefabAsset(instance, path);
            DestroyImmediate(instance);
        }

        #endregion


        #endregion

        // ═════════════════════════════════════════════════════════════════════
        #region Store Layout Patcher

        /// <summary>
        /// Doubles the size of the grid, adjusts the camera, and rebuilds the 
        /// store prefabs with proper aisles and exact shelf counts.
        /// </summary>
        private void PatchExpandStoresTo20x20()
        {
            // 1. Double the City Grid Size
            var gridManager = FindObjectOfType<AIBusinessTycoon.Managers.GridManager>();
            if (gridManager != null) {
                SetFieldValue(gridManager, "tileSize", 20f);
            }
            
            // 2. Move Spawner back to accommodate bigger tiles
            GameObject spawnPointObj = GameObject.Find("CustomerSpawnPoint");
            if (spawnPointObj != null) {
                spawnPointObj.transform.position = new Vector3(0, 0.5f, -40f);
            }

            // 3. Pull the camera back so we can see the massive new stores
            var cam = FindObjectOfType<AIBusinessTycoon.Managers.CameraController>();
            if (cam != null) {
                SetFieldValue(cam, "microViewOffset", new Vector3(0f, 15f, -12f)); // Higher up in store
                SetFieldValue(cam, "currentZoom", 80f);                            // Further out in city
            }

            // 4. Update Prefabs with their correct backend item counts
            ExpandPrefab("Assets/Prefabs/Buildings/KiranaStore.prefab", 8); // Kirana has 8 items
            ExpandPrefab("Assets/Prefabs/Buildings/PizzaOutlet.prefab", 4); // Pizza has 4
            ExpandPrefab("Assets/Prefabs/Buildings/Cafe.prefab", 4);        // Cafe has 4

            Debug.Log("[SceneSetupEditor] ✅ Stores expanded to 20x20!");
            EditorUtility.DisplayDialog("Layouts Upgraded", "All store prefabs are now 20x20 with proper aisles!", "Awesome");
        }

        private void ExpandPrefab(string path, int shelfCount)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) return;
            
            // Instantiate the prefab temporarily to modify it safely
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

            // Expand Floor
            var floor = instance.transform.Find("Floor");
            if (floor != null) floor.localScale = new Vector3(20, 0.2f, 20);

            // ── NEW: Expand Custom Walls (if they exist) ──
            var backWall = instance.transform.Find("BackWall");
            if (backWall != null) {
                backWall.localPosition = new Vector3(0, backWall.localPosition.y, 10f); // Push to back edge
                backWall.localScale = new Vector3(20f, backWall.localScale.y, backWall.localScale.z); // Stretch width
            }

            var leftWall = instance.transform.Find("LeftWall");
            if (leftWall != null) {
                leftWall.localPosition = new Vector3(-10f, leftWall.localPosition.y, 0); // Push to left edge
                leftWall.localScale = new Vector3(leftWall.localScale.x, leftWall.localScale.y, 20f); // Stretch depth
            }

            var rightWall = instance.transform.Find("RightWall");
            if (rightWall != null) {
                rightWall.localPosition = new Vector3(10f, rightWall.localPosition.y, 0); // Push to right edge
                rightWall.localScale = new Vector3(rightWall.localScale.x, rightWall.localScale.y, 20f); // Stretch depth
            }
            // ──────────────────────────────────────────────

            // Expand Click Collider
            var col = instance.GetComponent<BoxCollider>();
            if (col != null) col.size = new Vector3(20, 2f, 20);

            // Move Checkout Counter further back
            var counter = instance.transform.Find("CheckoutCounter");
            if (counter != null) counter.localPosition = new Vector3(6f, 0.5f, -6f);

            // Move the Entrance Point
            var entrance = instance.transform.Find("EntrancePoint");
            if (entrance == null) {
                entrance = new GameObject("EntrancePoint").transform;
                entrance.SetParent(instance.transform);
                var sim = instance.GetComponent<AIBusinessTycoon.Managers.StoreInteractionManager>();
                SetFieldValue(sim, "entrancePoint", entrance);
            }
            entrance.localPosition = new Vector3(0, 0.1f, -8f);

            // Delete old shelves
            var oldShelves = instance.GetComponentsInChildren<AIBusinessTycoon.Managers.InteractableShelf>();
            foreach (var s in oldShelves) DestroyImmediate(s.gameObject);
            
            Material shelfMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Buildings/Prop_Shelf.mat");
            
            // Build the new shelves in proper aisles (4 shelves per row)
            for (int i = 0; i < shelfCount; i++)
            {
                GameObject shelf = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shelf.name       = $"Shelf_{i}";
                shelf.transform.SetParent(instance.transform);
                
                int row = i / 4; 
                int colId = i % 4;
                
                // Calculates a clean, wide grid layout inside the 20x20 store
                float posX = -6.0f + (colId * 4.0f);
                float posZ =  4.0f - (row * 5.0f); 
                
                shelf.transform.localPosition = new Vector3(posX, 0.75f, posZ);
                shelf.transform.localScale    = new Vector3(2f, 1.5f, 1f); // Slightly bigger shelves!
                
                if (shelfMat != null) shelf.GetComponent<Renderer>().sharedMaterial = shelfMat;
                
                shelf.AddComponent<AIBusinessTycoon.Managers.InteractableShelf>();
            }

            // Save changes and clean up
            PrefabUtility.SaveAsPrefabAsset(instance, path);
            DestroyImmediate(instance);
        }


        #endregion

        // ═════════════════════════════════════════════════════════════════════
        #region UI Helpers

        private void RepositionRow(RectTransform rt, float newAnchorYMin, float newAnchorYMax)
        {
            if (rt == null) return;
            rt.anchorMin = new Vector2(rt.anchorMin.x, newAnchorYMin);
            rt.anchorMax = new Vector2(rt.anchorMax.x, newAnchorYMax);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta        = Vector2.zero;
        }

        private void StretchToParent(GameObject obj)
        {
            RectTransform rect    = obj.GetComponent<RectTransform>();
            rect.anchorMin        = Vector2.zero;
            rect.anchorMax        = Vector2.one;
            rect.sizeDelta        = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
        }

        private GameObject CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private GameObject CreatePanel(string name, Transform parent,
            Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = CreateUIObject(name, parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = min;
            rt.anchorMax        = max;
            rt.pivot            = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta        = size;
            go.AddComponent<Image>();
            return go;
        }

        private GameObject CreateText(string name, Transform parent,
            string text, int size, FontStyles style, Color color,
            Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 delta)
        {
            var go = CreateUIObject(name, parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = min;
            rt.anchorMax        = max;
            rt.pivot            = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta        = delta;
            var tmp             = go.AddComponent<TextMeshProUGUI>();
            tmp.text            = text;
            tmp.fontSize        = size;
            tmp.fontStyle       = style;
            tmp.color           = color;
            tmp.alignment       = TextAlignmentOptions.Center;
            return go;
        }

        private void SetFieldValue(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public    |
                System.Reflection.BindingFlags.Instance);
            if (field != null)
                field.SetValue(obj, value);
            else
                Debug.LogWarning($"[SceneSetupEditor] Field '{fieldName}' not found on {obj.GetType().Name}");
        }

        #endregion
    }
}
