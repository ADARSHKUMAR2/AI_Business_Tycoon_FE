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
        // ── Colour Palette (unchanged from Phase 1) ───────────────────────────
        private Color primaryColor    = new Color(0.12f, 0.53f, 0.90f);
        private Color secondaryColor  = new Color(0.18f, 0.80f, 0.44f);
        private Color accentColor     = new Color(1.0f,  0.76f, 0.03f);
        private Color darkTextColor   = new Color(0.13f, 0.13f, 0.13f);
        private Color whiteColor      = Color.white;

        // ── Phase 2 Colours ───────────────────────────────────────────────────
        private Color cashierColor    = new Color(0.20f, 0.40f, 1.00f);   // Blue  — matches CashierAI.cs
        private Color restockerColor  = new Color(1.00f, 0.50f, 0.10f);   // Orange — matches RestockerAI.cs
        private Color hireButtonColor = new Color(0.10f, 0.65f, 0.35f);   // Green — "Hire Staff" CTA
        private Color darkPanelColor  = new Color(0.08f, 0.08f, 0.12f, 0.96f);
        private Color rowBgColor      = new Color(1f, 1f, 1f, 0.06f);

        // ─────────────────────────────────────────────────────────────────────

        [MenuItem("AI Business Tycoon/Setup Complete Scene")]
        public static void ShowWindow()
        {
            var window = GetWindow<SceneSetupEditor>("Scene Setup");
            window.minSize = new Vector2(420, 260);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("AI Business Tycoon — Scene Setup", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "FULL REBUILD: Clears & recreates the entire scene from scratch (use for fresh start).\n" +
                "PATCH ONLY: Adds the Phase 2 Hire UI into an existing running scene without touching anything else.",
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
        }

        // ═════════════════════════════════════════════════════════════════════
        #region Orchestrators

        private void CreateCompleteScene()
        {
            SetupLayers();
            CreateFolderStructure();
            CreateMaterials();
            CreateEmployeeMaterials(); // Phase 2: New
            CreatePrefabs();
            CreateEmployeePrefabs();   // Phase 2: New

            ClearScene();
            CreateEventSystem();

            CreateEnvironment();
            CreatePlayerAvatar();

            CreateUISystem();
            ConnectComponents();
            CreateSupplyZone();

            Debug.Log("=== UI & Scene Setup Complete (Phase 2) ===");
        }

        /// <summary>
        /// Phase 2 safe patch: finds the existing StoreUIManager in the scene,
        /// injects the Hire UI into StoreUI_Visuals, and wires all references.
        /// Does NOT touch any other part of the scene.
        /// </summary>
        private void PatchPhase2HireUI()
        {
            // Ensure prefabs exist first
            CreateFolderStructure();
            CreateEmployeeMaterials();
            CreateEmployeePrefabs();

            var storeUIManager = FindObjectOfType<AIBusinessTycoon.UI.StoreUIManager>();
            if (storeUIManager == null)
            {
                EditorUtility.DisplayDialog("Not Found",
                    "StoreUIManager not found in scene.\nRun 'Re-Create Complete Scene' first.", "OK");
                return;
            }

            // Find the existing StoreUI_Visuals container (created by CreateStoreUI)
            var canvasGo = GameObject.Find("Canvas");
            if (canvasGo == null)
            {
                EditorUtility.DisplayDialog("Not Found",
                    "Canvas not found in scene. Run 'Re-Create Complete Scene' first.", "OK");
                return;
            }

            Transform storeUIVisuals = canvasGo.transform.Find("StoreUI_Visuals");
            if (storeUIVisuals == null)
            {
                EditorUtility.DisplayDialog("Not Found",
                    "StoreUI_Visuals panel not found inside Canvas. Run 'Re-Create Complete Scene' first.", "OK");
                return;
            }

            // Remove old hire panel if it exists (idempotent)
            var oldHireBtn   = storeUIVisuals.Find("HireStaffButton");
            var oldHirePanel = storeUIVisuals.Find("HireMenuPanel");
            if (oldHireBtn   != null) DestroyImmediate(oldHireBtn.gameObject);
            if (oldHirePanel != null) DestroyImmediate(oldHirePanel.gameObject);

            // Build & wire the new Phase 2 UI
            BuildAndWireHireUI(storeUIVisuals, storeUIManager);

            Debug.Log("[SceneSetupEditor] ✅ Phase 2 Hire UI patched successfully!");
            EditorUtility.DisplayDialog("Done", "Phase 2 Hire UI has been patched into the scene!", "OK");
        }

        #endregion

        // ═════════════════════════════════════════════════════════════════════
        #region Assets (Materials & Prefabs)

        private void CreateFolderStructure()
        {
            CreateFolderIfNotExists("Assets/Prefabs/Buildings");
            CreateFolderIfNotExists("Assets/Prefabs/Grid");
            CreateFolderIfNotExists("Assets/Materials/Grid");
            CreateFolderIfNotExists("Assets/Materials/Buildings");
            CreateFolderIfNotExists("Assets/Materials/Employees"); // Phase 2
        }

        private void CreateFolderIfNotExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parentFolder = Path.GetDirectoryName(path).Replace("\\", "/");
                string folderName   = Path.GetFileName(path);
                AssetDatabase.CreateFolder(parentFolder, folderName);
            }
        }

        private void CreateMaterials()
        {
            CreateMaterial("Assets/Materials/Grid/EmptyTile.mat",          new Color(0.6f, 0.6f, 0.6f, 0.3f), true);
            CreateMaterial("Assets/Materials/Grid/OwnedTile.mat",          new Color(0.4f, 0.8f, 0.4f, 0.5f), true);
            CreateMaterial("Assets/Materials/Grid/ValidPlacement.mat",     new Color(0.2f, 1f,   0.2f, 0.6f), true);
            CreateMaterial("Assets/Materials/Grid/InvalidPlacement.mat",   new Color(1f,   0.2f, 0.2f, 0.6f), true);
            CreateMaterial("Assets/Materials/Grid/PurchasableTile.mat",    new Color(0.2f, 0.6f, 1f,   0.5f), true);

            CreateMaterial("Assets/Materials/Buildings/Kirana_Theme.mat",  new Color(0.95f, 0.85f, 0.70f), false);
            CreateMaterial("Assets/Materials/Buildings/Pizza_Theme.mat",   new Color(0.95f, 0.70f, 0.70f), false);
            CreateMaterial("Assets/Materials/Buildings/Cafe_Theme.mat",    new Color(0.85f, 0.75f, 0.65f), false);
            CreateMaterial("Assets/Materials/Buildings/Prop_Counter.mat",  new Color(0.80f, 0.90f, 0.80f), false);
            CreateMaterial("Assets/Materials/Buildings/Prop_Shelf.mat",    new Color(0.60f, 0.40f, 0.20f), false);

            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Phase 2: Creates coloured materials for the employee capsule visuals.
        /// Colours match what CashierAI.cs and RestockerAI.cs apply at runtime.
        /// </summary>
        private void CreateEmployeeMaterials()
        {
            // Blue — Cashier (matches CashierAI: new Color(0.2f, 0.4f, 1f))
            CreateMaterial("Assets/Materials/Employees/Cashier_AI.mat",
                new Color(0.20f, 0.40f, 1.00f), false);

            // Orange — Restocker (matches RestockerAI: new Color(1f, 0.5f, 0.1f))
            CreateMaterial("Assets/Materials/Employees/Restocker_AI.mat",
                new Color(1.00f, 0.50f, 0.10f), false);

            AssetDatabase.SaveAssets();
        }

        private void CreateMaterial(string path, Color color, bool isTransparent)
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(path) == null)
            {
                Shader urpShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                Material mat = new Material(urpShader);
                mat.SetColor("_BaseColor", color);

                if (isTransparent)
                {
                    mat.SetFloat("_Surface", 1);
                    mat.SetFloat("_Blend", 0);
                    mat.SetFloat("_AlphaClip", 0);
                    mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetFloat("_ZWrite", 0);
                    mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                }
                else
                {
                    mat.SetFloat("_Surface", 0);
                    mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
                    mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
                    mat.SetFloat("_ZWrite", 1);
                    mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
                }
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat.shader.name == "Standard")
                {
                    Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
                    if (urpShader != null)
                    {
                        mat.shader = urpShader;
                        mat.SetColor("_BaseColor", color);
                        EditorUtility.SetDirty(mat);
                    }
                }
            }
        }

        private void CreatePrefabs()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Grid/TilePrefab.prefab") == null)
            {
                GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Quad);
                tile.transform.rotation = Quaternion.Euler(90, 0, 0);
                DestroyImmediate(tile.GetComponent<Collider>());
                PrefabUtility.SaveAsPrefabAsset(tile, "Assets/Prefabs/Grid/TilePrefab.prefab");
                DestroyImmediate(tile);
            }

            CreateInteriorPrefab("KiranaStore", "Assets/Materials/Buildings/Kirana_Theme.mat");
            CreateInteriorPrefab("PizzaOutlet",  "Assets/Materials/Buildings/Pizza_Theme.mat");
            CreateInteriorPrefab("Cafe",         "Assets/Materials/Buildings/Cafe_Theme.mat");
            CreateCustomerPrefab();
        }

        /// <summary>
        /// Phase 2: Builds CashierPrefab.prefab and RestockerPrefab.prefab.
        /// Each is a Capsule with NavMeshAgent + the respective AI script.
        /// Skips creation if the prefab already exists (idempotent).
        /// </summary>
        private void CreateEmployeePrefabs()
        {
            CreateEmployeePrefab(
                "CashierPrefab",
                "Assets/Prefabs/Buildings/CashierPrefab.prefab",
                "Assets/Materials/Employees/Cashier_AI.mat",
                typeof(AIBusinessTycoon.Managers.CashierAI)
            );

            CreateEmployeePrefab(
                "RestockerPrefab",
                "Assets/Prefabs/Buildings/RestockerPrefab.prefab",
                "Assets/Materials/Employees/Restocker_AI.mat",
                typeof(AIBusinessTycoon.Managers.RestockerAI)
            );
        }

        private void CreateEmployeePrefab(string goName, string prefabPath, string matPath, System.Type aiScript)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null) return;

            // Root object
            GameObject root = new GameObject(goName);

            // Visual capsule (child so the AI script's GetComponentInChildren<Renderer> finds it)
            GameObject visual       = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name             = "Visual";
            visual.transform.SetParent(root.transform);
            visual.transform.localPosition = new Vector3(0, 1f, 0);
            visual.transform.localScale    = Vector3.one;

            // Apply the coloured material
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat != null)
                visual.GetComponent<Renderer>().sharedMaterial = mat;

            // Remove the visual's collider (the root's CharacterController owns physics)
            DestroyImmediate(visual.GetComponent<Collider>());

            // NavMeshAgent on root
            NavMeshAgent agent    = root.AddComponent<NavMeshAgent>();
            agent.radius          = 0.4f;
            agent.height          = 2.0f;
            agent.speed           = 4.0f;
            agent.angularSpeed    = 180f;
            agent.stoppingDistance= 0.5f;
            agent.autoBraking     = true;

            // AI script on root
            root.AddComponent(aiScript);

            // Save as prefab
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            DestroyImmediate(root);

            Debug.Log($"[SceneSetupEditor] Created prefab: {prefabPath}");
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
            SerializedObject tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
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
            if (canvas != null) DestroyImmediate(canvas);

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
                lightObj = new GameObject("Directional Light");
                Light light   = lightObj.AddComponent<Light>();
                light.type    = LightType.Directional;
                light.intensity = 1.2f;
                light.color   = new Color(1f, 0.96f, 0.84f);
                lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);
            }

            GameObject ground = GameObject.Find("Ground");
            if (ground == null)
            {
                ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "Ground";
            }

            ground.transform.position   = new Vector3(0, -0.1f, 0);
            ground.transform.localScale = new Vector3(100, 1, 100);
            ground.layer                = LayerMask.NameToLayer("Grid");

            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material groundMat = new Material(urpShader);
            groundMat.SetColor("_BaseColor", new Color(0.3f, 0.4f, 0.3f));
            ground.GetComponent<Renderer>().material = groundMat;

            var navMeshSurface = ground.GetComponent<Unity.AI.Navigation.NavMeshSurface>();
            if (navMeshSurface == null)
                navMeshSurface = ground.AddComponent<Unity.AI.Navigation.NavMeshSurface>();

            navMeshSurface.layerMask  = LayerMask.GetMask("Grid");
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
                visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                visual.name = "Visual";
                visual.transform.SetParent(playerObj.transform);
                visual.transform.localPosition = new Vector3(0, 1f, 0);

                GameObject face = GameObject.CreatePrimitive(PrimitiveType.Cube);
                face.name = "Face";
                face.transform.SetParent(visual.transform);
                face.transform.localScale    = new Vector3(0.5f, 0.5f, 0.5f);
                face.transform.localPosition = new Vector3(0, 0.5f, 0.5f);
            }
            else
            {
                visual = visualTrans.gameObject;
            }

            SetFieldValue(playerController, "avatarVisual", visual);
            SetFieldValue(playerController, "moveSpeed", 8f);

            playerObj.layer = LayerMask.NameToLayer("Default");
            playerObj.AddComponent<AIBusinessTycoon.Managers.PlayerInventory>();
            playerObj.SetActive(false);
        }

        private void CreateSupplyZone()
        {
            GameObject zone = GameObject.FindGameObjectWithTag("SupplyZone");
            if (zone == null)
            {
                zone      = GameObject.CreatePrimitive(PrimitiveType.Cube);
                zone.name = "SupplyZone";
                zone.tag  = "SupplyZone";

                zone.transform.position   = new Vector3(0, 0.5f, -6f);
                zone.transform.localScale = new Vector3(3f, 1f, 3f);

                // Make trigger
                var col         = zone.GetComponent<BoxCollider>();
                col.isTrigger   = true;

                Shader urpShader  = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                Material mat      = new Material(urpShader);
                mat.SetColor("_BaseColor", new Color(0.2f, 0.8f, 1f, 0.6f));
                zone.GetComponent<Renderer>().material = mat;
            }
        }

        #endregion

        // ═════════════════════════════════════════════════════════════════════
        #region UI Creation

        private void CreateUISystem()
        {
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas        = canvasObj.AddComponent<Canvas>();
            canvas.renderMode    = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler         = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode          = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution  = new Vector2(1080, 1920);
            scaler.screenMatchMode      = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight   = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            GameObject uiManagersObj    = new GameObject("[ UI MANAGERS ]");
            var hudManager              = uiManagersObj.AddComponent<AIBusinessTycoon.UI.HUDManager>();
            var buildMenuManager        = uiManagersObj.AddComponent<AIBusinessTycoon.UI.BuildMenuManager>();
            var landPurchaseManager     = uiManagersObj.AddComponent<AIBusinessTycoon.UI.LandPurchaseUIManager>();
            var storeUIManager          = uiManagersObj.AddComponent<AIBusinessTycoon.UI.StoreUIManager>();

            CreateHUD(canvasObj.transform, hudManager);
            CreateBuildMenu(canvasObj.transform, buildMenuManager);
            CreateStoreUI(canvasObj.transform, storeUIManager);       // ← now includes Hire UI
            CreateLandPurchasePopup(canvasObj.transform, landPurchaseManager);
        }

        // ─────────────────────────────────────────────────────────────────────
        // HUD
        // ─────────────────────────────────────────────────────────────────────
        private void CreateHUD(Transform parent, AIBusinessTycoon.UI.HUDManager hudManager)
        {
            GameObject hudObj = CreateUIObject("HUD_Visuals", parent);
            StretchToParent(hudObj);

            GameObject topPanel = CreatePanel("TopPanel", hudObj.transform,
                new Vector2(0, 0.85f), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, Vector2.zero);
            topPanel.GetComponent<Image>().color = primaryColor;

            var nameObj = CreateText("PlayerName", topPanel.transform, "Player Name", 50, FontStyles.Bold, whiteColor,
                new Vector2(0.05f, 0.5f), new Vector2(0.95f, 0.9f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var nameTxt = nameObj.GetComponent<TextMeshProUGUI>();
            nameTxt.alignment       = TextAlignmentOptions.BottomLeft;
            nameTxt.enableWordWrapping = false;

            var moneyObj = CreateText("Money", topPanel.transform, "Rs.10,000", 80, FontStyles.Bold, accentColor,
                new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var moneyTxt = moneyObj.GetComponent<TextMeshProUGUI>();
            moneyTxt.alignment      = TextAlignmentOptions.TopLeft;
            moneyTxt.enableWordWrapping = false;

            GameObject statsContainer = CreateUIObject("StatsContainer", hudObj.transform);
            RectTransform statsRect   = statsContainer.GetComponent<RectTransform>();
            statsRect.anchorMin       = new Vector2(0,    0.77f);
            statsRect.anchorMax       = new Vector2(1,    0.85f);
            statsRect.pivot           = new Vector2(0.5f, 1);
            statsRect.anchoredPosition = Vector2.zero;
            statsRect.sizeDelta       = Vector2.zero;

            GameObject bizCard = CreateStatCard("BizCard",  statsContainer.transform, "0", "Businesses", secondaryColor,
                new Vector2(0.02f, 0), new Vector2(0.32f, 1));
            GameObject lndCard = CreateStatCard("LandCard", statsContainer.transform, "1", "Land Tiles", new Color(0.4f, 0.6f, 0.9f),
                new Vector2(0.35f, 0), new Vector2(0.65f, 1));
            GameObject revCard = CreateStatCard("RevCard",  statsContainer.transform, "Rs.0", "Revenue",  accentColor,
                new Vector2(0.68f, 0), new Vector2(0.98f, 1));

            GameObject statusBar = CreatePanel("StatusBar", hudObj.transform,
                new Vector2(0, 0.73f), new Vector2(1, 0.77f), new Vector2(0.5f, 1), Vector2.zero, Vector2.zero);
            statusBar.GetComponent<Image>().color = new Color(0, 0, 0, 0.5f);

            var statusObj = CreateText("StatusText", statusBar.transform, "Ready to build!", 28, FontStyles.Normal, new Color(0.8f, 0.8f, 0.8f),
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            statusObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

            SetFieldValue(hudManager, "playerNameText",    nameObj.GetComponent<TextMeshProUGUI>());
            SetFieldValue(hudManager, "moneyText",         moneyObj.GetComponent<TextMeshProUGUI>());
            SetFieldValue(hudManager, "levelText",         bizCard.transform.Find("Value")?.GetComponent<TextMeshProUGUI>());
            SetFieldValue(hudManager, "businessCountText", bizCard.transform.Find("Value")?.GetComponent<TextMeshProUGUI>());
            SetFieldValue(hudManager, "landTilesText",     lndCard.transform.Find("Value")?.GetComponent<TextMeshProUGUI>());
            SetFieldValue(hudManager, "revenueText",       revCard.transform.Find("Value")?.GetComponent<TextMeshProUGUI>());
            SetFieldValue(hudManager, "statusText",        statusObj.GetComponent<TextMeshProUGUI>());
        }

        private GameObject CreateStatCard(string name, Transform parent, string value, string label, Color color, Vector2 aMin, Vector2 aMax)
        {
            GameObject card = CreatePanel(name, parent, aMin, aMax, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            card.GetComponent<Image>().color = color;

            var valObj = CreateText("Value", card.transform, value, 40, FontStyles.Bold, Color.white,
                new Vector2(0, 0.4f), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            valObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

            var lblObj = CreateText("Label", card.transform, label, 22, FontStyles.Normal, new Color(1, 1, 1, 0.8f),
                new Vector2(0, 0), new Vector2(1, 0.45f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            lblObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

            return card;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Store UI  ← UPDATED for Phase 2
        // ─────────────────────────────────────────────────────────────────────
        private void CreateStoreUI(Transform parent, AIBusinessTycoon.UI.StoreUIManager storeUIManager)
        {
            GameObject storeUIObj = CreateUIObject("StoreUI_Visuals", parent);
            StretchToParent(storeUIObj);

            // ── Top bar (unchanged) ──────────────────────────────────────────
            GameObject topBar = CreatePanel("StoreTopBar", storeUIObj.transform,
                new Vector2(0, 0.85f), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, Vector2.zero);
            topBar.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

            var storeNameObj = CreateText("StoreName", topBar.transform, "Kirana Store", 55, FontStyles.Bold, Color.white,
                new Vector2(0.05f, 0.5f), new Vector2(0.75f, 0.95f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            storeNameObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.BottomLeft;

            var storeTypeObj = CreateText("StoreType", topBar.transform, "KIRANA", 30, FontStyles.Normal, accentColor,
                new Vector2(0.05f, 0.1f), new Vector2(0.75f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            storeTypeObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.TopLeft;

            GameObject exitBtn = CreatePanel("ExitButton", topBar.transform,
                new Vector2(0.78f, 0.1f), new Vector2(0.98f, 0.9f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            exitBtn.GetComponent<Image>().color = new Color(0.85f, 0.2f, 0.2f);
            exitBtn.AddComponent<Button>();
            var exitTxt = CreateText("Text", exitBtn.transform, "Exit Store", 30, FontStyles.Bold, Color.white,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            exitTxt.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

            // ── Joystick (unchanged) ─────────────────────────────────────────
            GameObject joystickPanel = CreateUIObject("JoystickPanel", storeUIObj.transform);
            RectTransform joystickRect = joystickPanel.GetComponent<RectTransform>();
            joystickRect.anchorMin        = new Vector2(0,     0);
            joystickRect.anchorMax        = new Vector2(0.35f, 0.25f);
            joystickRect.anchoredPosition = Vector2.zero;
            joystickRect.sizeDelta        = Vector2.zero;

            GameObject joystickBg = CreatePanel("JoystickBackground", joystickPanel.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(220, 220));
            joystickBg.GetComponent<Image>().color = new Color(1, 1, 1, 0.15f);

            GameObject joystickHandle = CreatePanel("JoystickHandle", joystickBg.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(100, 100));
            joystickHandle.GetComponent<Image>().color = new Color(1, 1, 1, 0.6f);

            var joystickComp = joystickBg.AddComponent<AIBusinessTycoon.UI.JoystickController>();
            SetFieldValue(joystickComp, "joystickBackground", joystickBg.GetComponent<RectTransform>());
            SetFieldValue(joystickComp, "joystickHandle",     joystickHandle.GetComponent<RectTransform>());
            SetFieldValue(joystickComp, "handleRange",        80f);

            // ── Phase 2: "Hire Staff" floating button (bottom-right) ─────────
            GameObject hireStaffBtn = CreatePanel("HireStaffButton", storeUIObj.transform,
                new Vector2(0.65f, 0),    // anchored bottom-right area
                new Vector2(0.98f, 0.13f),
                new Vector2(0.5f, 0), Vector2.zero, Vector2.zero);
            hireStaffBtn.GetComponent<Image>().color = hireButtonColor;
            Button hireStaffBtnComp = hireStaffBtn.AddComponent<Button>();

            // Green tint on hover
            ColorBlock hireColors          = hireStaffBtnComp.colors;
            hireColors.highlightedColor    = new Color(0.15f, 0.80f, 0.45f);
            hireColors.pressedColor        = new Color(0.08f, 0.50f, 0.28f);
            hireStaffBtnComp.colors        = hireColors;

            // Icon + label inside the button
            CreateText("Icon", hireStaffBtn.transform, "👤", 55, FontStyles.Normal, Color.white,
                new Vector2(0,    0), new Vector2(0.3f, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero)
                .GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

            var hireStaffLabel = CreateText("Label", hireStaffBtn.transform, "Hire\nStaff", 32, FontStyles.Bold, Color.white,
                new Vector2(0.28f, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            hireStaffLabel.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;

            // ── Phase 2: Hire Menu Panel (modal popup card) ──────────────────
            GameObject hireMenuPanel = BuildAndWireHireUI(storeUIObj.transform, storeUIManager);

            // ── Wire ALL references to StoreUIManager ────────────────────────
            SetFieldValue(storeUIManager, "storeUIPanel",      storeUIObj);
            SetFieldValue(storeUIManager, "storeNameText",     storeNameObj.GetComponent<TextMeshProUGUI>());
            SetFieldValue(storeUIManager, "storeTypeText",     storeTypeObj.GetComponent<TextMeshProUGUI>());
            SetFieldValue(storeUIManager, "joystickPanel",     joystickPanel);
            SetFieldValue(storeUIManager, "joystick",          joystickComp);
            SetFieldValue(storeUIManager, "exitButton",        exitBtn.GetComponent<Button>());
            SetFieldValue(storeUIManager, "hireStaffButton",   hireStaffBtnComp);
            SetFieldValue(storeUIManager, "hireMenuPanel",     hireMenuPanel);

            storeUIObj.SetActive(false);
        }

        /// <summary>
        /// Builds the full Hire Menu Panel under 'parent' and wires all
        /// button / text / prefab references onto storeUIManager.
        /// Returns the root hireMenuPanel GameObject.
        /// Called both from CreateStoreUI (full build) and PatchPhase2HireUI (patch build).
        /// </summary>
        private GameObject BuildAndWireHireUI(Transform parent, AIBusinessTycoon.UI.StoreUIManager storeUIManager)
        {
            // ── Dimmed full-screen backdrop ───────────────────────────────────
            GameObject backdrop = CreatePanel("HireMenuPanel", parent,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            backdrop.GetComponent<Image>().color = new Color(0, 0, 0, 0.55f);

            // ── Card: centred modal ───────────────────────────────────────────
            // Anchored to centre, fixed pixel size so it looks the same on all screens
            GameObject card = CreatePanel("Card", backdrop.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(800, 700));
            card.GetComponent<Image>().color = darkPanelColor;

            // Rounded corners illusion: thin inner highlight border
            GameObject cardBorder = CreatePanel("CardBorder", card.transform,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-6, -6));
            cardBorder.GetComponent<Image>().color = new Color(1, 1, 1, 0.07f);

            // ── Title bar ────────────────────────────────────────────────────
            GameObject titleBar = CreatePanel("TitleBar", card.transform,
                new Vector2(0, 0.82f), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, Vector2.zero);
            titleBar.GetComponent<Image>().color = new Color(1, 1, 1, 0.05f);

            CreateText("TitleText", titleBar.transform, "👥  HIRE STAFF", 52, FontStyles.Bold, Color.white,
                new Vector2(0.04f, 0), new Vector2(0.82f, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero)
                .GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;

            // ✕ Close button inside title bar (top-right corner)
            GameObject closeBtn = CreatePanel("CloseHireMenuButton", titleBar.transform,
                new Vector2(0.84f, 0.1f), new Vector2(0.98f, 0.9f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            closeBtn.GetComponent<Image>().color = new Color(0.85f, 0.20f, 0.20f);
            Button closeBtnComp = closeBtn.AddComponent<Button>();
            CreateText("Text", closeBtn.transform, "✕", 46, FontStyles.Bold, Color.white,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero)
                .GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

            // ── Subtitle ──────────────────────────────────────────────────────
            GameObject subtitle = CreateText("Subtitle", card.transform,
                "Hire AI employees to automate your store.", 28, FontStyles.Normal, new Color(1, 1, 1, 0.55f),
                new Vector2(0.04f, 0.73f), new Vector2(0.96f, 0.82f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            subtitle.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;

            // ── Divider ───────────────────────────────────────────────────────
            GameObject divider = CreatePanel("Divider", card.transform,
                new Vector2(0.04f, 0.715f), new Vector2(0.96f, 0.718f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            divider.GetComponent<Image>().color = new Color(1, 1, 1, 0.12f);

            // ── Row helper (builds one employee row) ──────────────────────────
            // Cashier row: upper half of card body
            var (cashierRow,   cashierHireBtn,   cashierStatus)
                = CreateEmployeeRow(card.transform,
                    rowName:      "CashierRow",
                    anchorYMin:   0.38f,
                    anchorYMax:   0.70f,
                    emoji:        "💰",
                    roleName:     "Cashier",
                    roleDesc:     "Stands at the register & processes\ncustomers automatically.",
                    cost:         "Rs. 1,000",
                    accentCol:    cashierColor);

            // Restocker row: lower half of card body
            var (restockerRow, restockerHireBtn, restockerStatus)
                = CreateEmployeeRow(card.transform,
                    rowName:      "RestockerRow",
                    anchorYMin:   0.04f,
                    anchorYMax:   0.36f,
                    emoji:        "📦",
                    roleName:     "Restocker",
                    roleDesc:     "Autonomously walks to the supply zone\nand restocks empty shelves.",
                    cost:         "Rs. 1,500",
                    accentCol:    restockerColor);

            // ── Wire up StoreUIManager serialized fields ───────────────────────
            SetFieldValue(storeUIManager, "hireMenuPanel",        backdrop);
            SetFieldValue(storeUIManager, "hireCashierButton",    cashierHireBtn);
            SetFieldValue(storeUIManager, "hireRestockerButton",  restockerHireBtn);
            SetFieldValue(storeUIManager, "closeHireMenuButton",  closeBtnComp);
            SetFieldValue(storeUIManager, "cashierStatusText",    cashierStatus);
            SetFieldValue(storeUIManager, "restockerStatusText",  restockerStatus);

            // ── Wire employee prefabs ─────────────────────────────────────────
            var cashierPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Buildings/CashierPrefab.prefab");
            var restockerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Buildings/RestockerPrefab.prefab");

            if (cashierPrefab   != null) SetFieldValue(storeUIManager, "cashierPrefab",   cashierPrefab);
            if (restockerPrefab != null) SetFieldValue(storeUIManager, "restockerPrefab", restockerPrefab);

            backdrop.SetActive(false);
            return backdrop;
        }

        /// <summary>
        /// Builds one employee hire row inside the card.
        /// Returns (row root, hire Button component, status TextMeshProUGUI).
        /// </summary>
        private (GameObject row, Button hireBtn, TextMeshProUGUI statusTxt)
            CreateEmployeeRow(Transform parent,
                string rowName, float anchorYMin, float anchorYMax,
                string emoji, string roleName, string roleDesc,
                string cost, Color accentCol)
        {
            // Row background
            GameObject row = CreatePanel(rowName, parent,
                new Vector2(0.04f, anchorYMin), new Vector2(0.96f, anchorYMax),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            row.GetComponent<Image>().color = rowBgColor;

            // Left accent bar
            GameObject accentBar = CreatePanel("AccentBar", row.transform,
                new Vector2(0, 0), new Vector2(0.012f, 1),
                new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
            accentBar.GetComponent<Image>().color = accentCol;

            // Emoji / icon
            var emojiObj = CreateText("Emoji", row.transform, emoji, 70, FontStyles.Normal, Color.white,
                new Vector2(0.03f, 0), new Vector2(0.18f, 1),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            emojiObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

            // Role name
            var roleNameObj = CreateText("RoleName", row.transform, roleName, 44, FontStyles.Bold, Color.white,
                new Vector2(0.18f, 0.55f), new Vector2(0.65f, 1),
                new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
            roleNameObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;

            // Role description
            var roleDescObj = CreateText("RoleDesc", row.transform, roleDesc, 24, FontStyles.Normal, new Color(1, 1, 1, 0.6f),
                new Vector2(0.18f, 0), new Vector2(0.65f, 0.54f),
                new Vector2(0, 0), Vector2.zero, Vector2.zero);
            var roleDescTMP = roleDescObj.GetComponent<TextMeshProUGUI>();
            roleDescTMP.alignment        = TextAlignmentOptions.TopLeft;
            roleDescTMP.enableWordWrapping = true;

            // Status text (affordability / hired indicator)
            var statusObj = CreateText("StatusText", row.transform, "✔ Can Afford", 26, FontStyles.Normal, secondaryColor,
                new Vector2(0.18f, 0), new Vector2(0.65f, 0.32f),
                new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
            var statusTMP = statusObj.GetComponent<TextMeshProUGUI>();
            statusTMP.alignment = TextAlignmentOptions.BottomLeft;

            // Hire button (right side of row)
            GameObject hireBtn = CreatePanel("HireButton", row.transform,
                new Vector2(0.67f, 0.12f), new Vector2(0.97f, 0.88f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            hireBtn.GetComponent<Image>().color = accentCol;
            Button hireBtnComp = hireBtn.AddComponent<Button>();

            ColorBlock hireBtnColors    = hireBtnComp.colors;
            hireBtnColors.highlightedColor = new Color(
                Mathf.Min(accentCol.r + 0.15f, 1f),
                Mathf.Min(accentCol.g + 0.15f, 1f),
                Mathf.Min(accentCol.b + 0.15f, 1f));
            hireBtnColors.pressedColor  = new Color(
                Mathf.Max(accentCol.r - 0.2f, 0f),
                Mathf.Max(accentCol.g - 0.2f, 0f),
                Mathf.Max(accentCol.b - 0.2f, 0f));
            hireBtnComp.colors = hireBtnColors;

            // Cost label inside hire button
            var costObj = CreateText("CostText", hireBtn.transform, cost, 30, FontStyles.Bold, Color.white,
                new Vector2(0, 0.52f), new Vector2(1, 1),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            costObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

            // "HIRE" label inside hire button
            var hireLblObj = CreateText("HireLabel", hireBtn.transform, "HIRE", 26, FontStyles.Bold, new Color(1,1,1,0.85f),
                new Vector2(0, 0), new Vector2(1, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            hireLblObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

            return (row, hireBtnComp, statusTMP);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Build Menu (unchanged from Phase 1)
        // ─────────────────────────────────────────────────────────────────────
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

            GameObject cardsContainer = CreateUIObject("CardsContainer", menuPanel.transform);
            RectTransform cardsRect   = cardsContainer.GetComponent<RectTransform>();
            cardsRect.anchorMin       = new Vector2(0.05f, 0);
            cardsRect.anchorMax       = new Vector2(0.95f, 0.85f);
            cardsRect.anchoredPosition = Vector2.zero;
            cardsRect.sizeDelta       = Vector2.zero;

            GameObject kiranaCard = CreateBuildingButton("Card_Kirana",  cardsContainer.transform, "Kirana Store",  "Rs.5,000",  secondaryColor,          new Vector2(0, 0.68f), new Vector2(1, 0.98f));
            GameObject pizzaCard  = CreateBuildingButton("Card_Pizza",   cardsContainer.transform, "Pizza Outlet",  "Rs.15,000", new Color(0.9f, 0.3f, 0.3f), new Vector2(0, 0.35f), new Vector2(1, 0.65f));
            GameObject cafeCard   = CreateBuildingButton("Card_Cafe",    cardsContainer.transform, "Cafe",          "Rs.25,000", new Color(0.4f, 0.3f, 0.2f), new Vector2(0, 0.02f), new Vector2(1, 0.32f));

            SetFieldValue(buildMenuManager, "menuPanel",    menuPanel);
            SetFieldValue(buildMenuManager, "kiranaButton", kiranaCard.GetComponent<Button>());
            SetFieldValue(buildMenuManager, "pizzaButton",  pizzaCard.GetComponent<Button>());
            SetFieldValue(buildMenuManager, "cafeButton",   cafeCard.GetComponent<Button>());

            menuPanel.SetActive(false);
        }

        private GameObject CreateBuildingButton(string name, Transform parent, string title, string cost, Color color, Vector2 aMin, Vector2 aMax)
        {
            GameObject btnObj = CreatePanel(name, parent, aMin, aMax, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            btnObj.GetComponent<Image>().color = color;
            Button btn = btnObj.AddComponent<Button>();

            var tTxt = CreateText("Title", btnObj.transform, title, 50, FontStyles.Bold, whiteColor,
                new Vector2(0.05f, 0), new Vector2(0.6f, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            tTxt.GetComponent<TextMeshProUGUI>().alignment       = TextAlignmentOptions.MidlineLeft;
            tTxt.GetComponent<TextMeshProUGUI>().enableWordWrapping = false;

            var cTxt = CreateText("Cost", btnObj.transform, cost, 40, FontStyles.Bold, accentColor,
                new Vector2(0.6f, 0), new Vector2(0.98f, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            cTxt.GetComponent<TextMeshProUGUI>().alignment       = TextAlignmentOptions.MidlineRight;
            cTxt.GetComponent<TextMeshProUGUI>().enableWordWrapping = false;

            return btnObj;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Land Purchase Popup (unchanged from Phase 1)
        // ─────────────────────────────────────────────────────────────────────
        private void CreateLandPurchasePopup(Transform parent, AIBusinessTycoon.UI.LandPurchaseUIManager landPurchaseManager)
        {
            GameObject popupObj = CreateUIObject("LandPurchasePopup", parent);
            StretchToParent(popupObj);

            popupObj.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);

            GameObject popupCard = CreatePanel("PopupCard", popupObj.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(700, 600));
            popupCard.GetComponent<Image>().color = new Color(0.95f, 0.95f, 0.95f);

            var titleObj = CreateText("Title",    popupCard.transform, "Buy Land Tile",  60, FontStyles.Bold,   darkTextColor, new Vector2(0.05f, 0.8f),  new Vector2(0.95f, 1),    new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var posObj   = CreateText("Position", popupCard.transform, "Position: (0,0)", 30, FontStyles.Normal, darkTextColor, new Vector2(0.05f, 0.65f), new Vector2(0.95f, 0.8f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var costObj  = CreateText("Cost",     popupCard.transform, "Cost: Rs.2,000", 40, FontStyles.Bold,   primaryColor,  new Vector2(0.05f, 0.5f),  new Vector2(0.95f, 0.65f),new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var moneyObj = CreateText("Money",    popupCard.transform, "Your Money: Rs.10,000", 30, FontStyles.Normal, darkTextColor, new Vector2(0.05f, 0.38f), new Vector2(0.95f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var affordObj= CreateText("Afford",   popupCard.transform, "✔ You can afford this!", 30, FontStyles.Bold, secondaryColor, new Vector2(0.05f, 0.26f), new Vector2(0.95f, 0.38f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            foreach (var t in new[] { titleObj, posObj, costObj, moneyObj, affordObj })
                t.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

            GameObject cancelBtn  = CreatePanel("CancelButton",  popupCard.transform, new Vector2(0.05f, 0.03f), new Vector2(0.47f, 0.2f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            cancelBtn.GetComponent<Image>().color = new Color(0.8f, 0.8f, 0.8f);
            cancelBtn.AddComponent<Button>();
            CreateText("Text", cancelBtn.transform, "Cancel", 40, FontStyles.Bold, new Color(0.3f, 0.3f, 0.3f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            GameObject confirmBtn = CreatePanel("ConfirmButton", popupCard.transform, new Vector2(0.53f, 0.03f), new Vector2(0.95f, 0.2f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            confirmBtn.GetComponent<Image>().color = secondaryColor;
            confirmBtn.AddComponent<Button>();
            var confirmTxtObj = CreateText("Text", confirmBtn.transform, "Buy Land", 40, FontStyles.Bold, Color.white, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            SetFieldValue(landPurchaseManager, "popupPanel",        popupObj);
            SetFieldValue(landPurchaseManager, "titleText",         titleObj.GetComponent<TextMeshProUGUI>());
            SetFieldValue(landPurchaseManager, "costText",          costObj.GetComponent<TextMeshProUGUI>());
            SetFieldValue(landPurchaseManager, "positionText",      posObj.GetComponent<TextMeshProUGUI>());
            SetFieldValue(landPurchaseManager, "playerMoneyText",   moneyObj.GetComponent<TextMeshProUGUI>());
            SetFieldValue(landPurchaseManager, "affordabilityText", affordObj.GetComponent<TextMeshProUGUI>());
            SetFieldValue(landPurchaseManager, "cancelButton",      cancelBtn.GetComponent<Button>());
            SetFieldValue(landPurchaseManager, "confirmButton",     confirmBtn.GetComponent<Button>());
            SetFieldValue(landPurchaseManager, "confirmButtonText", confirmTxtObj.GetComponent<TextMeshProUGUI>());

            popupObj.SetActive(false);
        }

        #endregion

        // ═════════════════════════════════════════════════════════════════════
        #region Prefab Builders (Phase 1 helpers, unchanged)

        private void CreateCustomerPrefab()
        {
            string path = "Assets/Prefabs/Buildings/Customer.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

            GameObject customer   = new GameObject("Customer");
            customer.tag          = "Customer";

            GameObject visual     = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name           = "Visual";
            visual.transform.SetParent(customer.transform);
            visual.transform.localPosition = new Vector3(0, 1f, 0);
            DestroyImmediate(visual.GetComponent<Collider>());

            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material mat     = new Material(urpShader);
            mat.SetColor("_BaseColor", new Color(0.8f, 0.5f, 0.2f));
            visual.GetComponent<Renderer>().sharedMaterial = mat;

            NavMeshAgent agent    = customer.AddComponent<NavMeshAgent>();
            agent.radius          = 0.4f;
            agent.height          = 2.0f;
            agent.speed           = 3.5f;
            agent.angularSpeed    = 180f;
            agent.stoppingDistance= 0.3f;

            customer.AddComponent<AIBusinessTycoon.Managers.CustomerAI>();

            PrefabUtility.SaveAsPrefabAsset(customer, path);
            DestroyImmediate(customer);
        }

        private void CreateInteriorPrefab(string buildingName, string themeMaterialPath)
        {
            string path = $"Assets/Prefabs/Buildings/{buildingName}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

            Material themeMat   = AssetDatabase.LoadAssetAtPath<Material>(themeMaterialPath);
            Material counterMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Buildings/Prop_Counter.mat");
            Material shelfMat   = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Buildings/Prop_Shelf.mat");

            float buildingSize = 10f;
            float wallHeight   = 4f;

            GameObject building = new GameObject(buildingName);

            // Floor
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(building.transform);
            floor.transform.localPosition = new Vector3(0, 0, 0);
            floor.transform.localScale    = new Vector3(buildingSize, 0.2f, buildingSize);
            if (themeMat != null) floor.GetComponent<Renderer>().material = themeMat;

            // Walls
            CreateWall("Wall_Back",  building.transform, new Vector3(0, wallHeight / 2f, buildingSize / 2f),  new Vector3(buildingSize, wallHeight, 0.3f), themeMat);
            CreateWall("Wall_Left",  building.transform, new Vector3(-buildingSize / 2f, wallHeight / 2f, 0), new Vector3(0.3f, wallHeight, buildingSize), themeMat);
            CreateWall("Wall_Right", building.transform, new Vector3(buildingSize / 2f,  wallHeight / 2f, 0), new Vector3(0.3f, wallHeight, buildingSize), themeMat);

            // Checkout Counter
            GameObject counter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            counter.name = "CheckoutCounter";
            counter.transform.SetParent(building.transform);
            counter.transform.localPosition = new Vector3(0, 0.5f, -2.5f);
            counter.transform.localScale    = new Vector3(3.5f, 1f, 1f);
            if (counterMat != null) counter.GetComponent<Renderer>().material = counterMat;
            counter.AddComponent<AIBusinessTycoon.Managers.CheckoutCounter>();

            // Shelf 1 (left)
            GameObject shelf1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shelf1.name = "Shelf_1";
            shelf1.transform.SetParent(building.transform);
            shelf1.transform.localPosition = new Vector3(-2.5f, 1f, 1.5f);
            shelf1.transform.localScale    = new Vector3(1f, 2f, 4f);
            if (shelfMat != null) shelf1.GetComponent<Renderer>().material = shelfMat;
            shelf1.GetComponent<BoxCollider>().isTrigger = false;
            BoxCollider trigger1 = shelf1.AddComponent<BoxCollider>();
            trigger1.isTrigger = true;
            trigger1.size      = new Vector3(2.5f, 1.5f, 1.2f);
            shelf1.AddComponent<AIBusinessTycoon.Managers.InteractableShelf>();

            // Shelf 2 (right)
            GameObject shelf2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shelf2.name = "Shelf_2";
            shelf2.transform.SetParent(building.transform);
            shelf2.transform.localPosition = new Vector3(2.5f, 1f, 1.5f);
            shelf2.transform.localScale    = new Vector3(1f, 2f, 4f);
            if (shelfMat != null) shelf2.GetComponent<Renderer>().material = shelfMat;
            shelf2.GetComponent<BoxCollider>().isTrigger = false;
            BoxCollider trigger2 = shelf2.AddComponent<BoxCollider>();
            trigger2.isTrigger = true;
            trigger2.size      = new Vector3(2.5f, 1.5f, 1.2f);
            shelf2.AddComponent<AIBusinessTycoon.Managers.InteractableShelf>();

            // Set building layer
            int buildingLayer = LayerMask.NameToLayer("Building");
            if (buildingLayer > -1)
                foreach (Transform child in building.GetComponentsInChildren<Transform>())
                    child.gameObject.layer = buildingLayer;

            // Outer interaction collider
            BoxCollider outerCol = building.AddComponent<BoxCollider>();
            outerCol.center  = new Vector3(0, wallHeight / 2f, 0);
            outerCol.size    = new Vector3(buildingSize, wallHeight, buildingSize);
            outerCol.isTrigger = true;

            PrefabUtility.SaveAsPrefabAsset(building, path);
            DestroyImmediate(building);
        }

        private void CreateWall(string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent);
            wall.transform.localPosition = localPos;
            wall.transform.localScale    = localScale;
            if (mat != null) wall.GetComponent<Renderer>().material = mat;
        }

        #endregion

        // ═════════════════════════════════════════════════════════════════════
        #region Component Connection

        private void ConnectComponents()
        {
            var gameManager       = FindObjectOfType<AIBusinessTycoon.Managers.GameManager>();
            var buildMenuManager  = FindObjectOfType<AIBusinessTycoon.UI.BuildMenuManager>();
            var placementManager  = FindObjectOfType<AIBusinessTycoon.Managers.BuildingPlacementManager>();
            var gridManager       = FindObjectOfType<AIBusinessTycoon.Managers.GridManager>();
            var cameraController  = FindObjectOfType<AIBusinessTycoon.Managers.CameraController>();
            var playerController  = FindObjectOfType<AIBusinessTycoon.Managers.PlayerController>(true);
            var storeUIManager    = FindObjectOfType<AIBusinessTycoon.UI.StoreUIManager>();

            if (gameManager != null && playerController != null)
                SetFieldValue(gameManager, "playerController", playerController);

            if (buildMenuManager != null)
            {
                SetFieldValue(buildMenuManager, "kiranaPrefab", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/KiranaStore.prefab"));
                SetFieldValue(buildMenuManager, "pizzaPrefab",  AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/PizzaOutlet.prefab"));
                SetFieldValue(buildMenuManager, "cafePrefab",   AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Cafe.prefab"));
                SetFieldValue(buildMenuManager, "kiranaCost",   5000f);
                SetFieldValue(buildMenuManager, "pizzaCost",    15000f);
                SetFieldValue(buildMenuManager, "cafeCost",     25000f);
            }

            if (placementManager != null)
            {
                int gridLayerIndex = LayerMask.NameToLayer("Grid");
                if (gridLayerIndex != -1)
                    SetFieldValue(placementManager, "groundLayer", (LayerMask)(1 << gridLayerIndex));
            }

            if (gridManager != null)
            {
                SetFieldValue(gridManager, "tileSize",                20f);
                SetFieldValue(gridManager, "emptyTileMaterial",       AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Grid/EmptyTile.mat"));
                SetFieldValue(gridManager, "ownedTileMaterial",       AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Grid/OwnedTile.mat"));
                SetFieldValue(gridManager, "validPlacementMaterial",  AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Grid/ValidPlacement.mat"));
                SetFieldValue(gridManager, "invalidPlacementMaterial",AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Grid/InvalidPlacement.mat"));
                SetFieldValue(gridManager, "purchasableTileMaterial", AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Grid/PurchasableTile.mat"));
                SetFieldValue(gridManager, "tilePrefab",              AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Grid/TilePrefab.prefab"));
            }

            if (cameraController != null)
            {
                SetFieldValue(cameraController, "panSpeed",  50f);
                SetFieldValue(cameraController, "zoomSpeed", 20f);
                SetFieldValue(cameraController, "minZoom",   10f);
                SetFieldValue(cameraController, "maxZoom",   100f);
                Camera.main.transform.localPosition = new Vector3(0, 30, -30);
            }

            // Phase 2: wire employee prefabs onto StoreUIManager
            if (storeUIManager != null)
            {
                var cashierPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Prefabs/Buildings/CashierPrefab.prefab");
                var restockerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Prefabs/Buildings/RestockerPrefab.prefab");

                if (cashierPrefab   != null) SetFieldValue(storeUIManager, "cashierPrefab",   cashierPrefab);
                if (restockerPrefab != null) SetFieldValue(storeUIManager, "restockerPrefab", restockerPrefab);
            }

            // Customer Spawner
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
        #region UI Helpers (unchanged)

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

        private GameObject CreatePanel(string name, Transform parent, Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 size)
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

        private GameObject CreateText(string name, Transform parent, string text, int size, FontStyles style, Color color,
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
