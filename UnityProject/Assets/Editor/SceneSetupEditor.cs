using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using System.IO;
using UnityEngine.EventSystems;

namespace AIBusinessTycoon.Editor
{
    public class SceneSetupEditor : EditorWindow
    {
        private Color primaryColor = new Color(0.12f, 0.53f, 0.90f); 
        private Color secondaryColor = new Color(0.18f, 0.80f, 0.44f); 
        private Color accentColor = new Color(1.0f, 0.76f, 0.03f); 
        private Color darkTextColor = new Color(0.13f, 0.13f, 0.13f); 
        private Color whiteColor = Color.white;
        
        [MenuItem("AI Business Tycoon/Setup Complete Scene")]
        public static void ShowWindow()
        {
            var window = GetWindow<SceneSetupEditor>("Scene Setup");
            window.minSize = new Vector2(400, 600);
            window.Show();
        }
        
        private void OnGUI()
        {
            GUILayout.Label("AI Business Tycoon - Scene Setup", EditorStyles.boldLabel);
            GUILayout.Space(20);
            
            if (GUILayout.Button("🎨 Re-Create Complete Scene", GUILayout.Height(50)))
            {
                CreateCompleteScene();
            }
        }
        
        private void CreateCompleteScene()
        {
            SetupLayers();
            CreateFolderStructure();
            CreateMaterials();
            CreatePrefabs(); // Will now generate open-room interiors!
            
            ClearScene();
            CreateEventSystem();
            CreateUISystem();
            ConnectComponents();
            
            Debug.Log("=== UI & Scene Setup Complete! ===");
        }

        #region Assets Generation (Materials & Prefabs)
        
        private void CreateFolderStructure()
        {
            CreateFolderIfNotExists("Assets/Prefabs/Buildings");
            CreateFolderIfNotExists("Assets/Prefabs/Grid");
            CreateFolderIfNotExists("Assets/Materials/Grid");
            CreateFolderIfNotExists("Assets/Materials/Buildings");
        }

        private void CreateFolderIfNotExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parentFolder = Path.GetDirectoryName(path).Replace("\\", "/");
                string folderName = Path.GetFileName(path);
                AssetDatabase.CreateFolder(parentFolder, folderName);
            }
        }

        private void CreateMaterials()
        {
            // Grid Materials
            CreateMaterial("Assets/Materials/Grid/EmptyTile.mat", new Color(0.6f, 0.6f, 0.6f, 0.3f), true);
            CreateMaterial("Assets/Materials/Grid/OwnedTile.mat", new Color(0.4f, 0.8f, 0.4f, 0.5f), true);
            CreateMaterial("Assets/Materials/Grid/ValidPlacement.mat", new Color(0.2f, 1f, 0.2f, 0.6f), true);
            CreateMaterial("Assets/Materials/Grid/InvalidPlacement.mat", new Color(1f, 0.2f, 0.2f, 0.6f), true);
            CreateMaterial("Assets/Materials/Grid/PurchasableTile.mat", new Color(0.2f, 0.6f, 1f, 0.5f), true);
            
            // Building Theme Materials (Floors/Walls)
            CreateMaterial("Assets/Materials/Buildings/Kirana_Theme.mat", new Color(0.95f, 0.85f, 0.7f), false);
            CreateMaterial("Assets/Materials/Buildings/Pizza_Theme.mat", new Color(0.95f, 0.7f, 0.7f), false);
            CreateMaterial("Assets/Materials/Buildings/Cafe_Theme.mat", new Color(0.85f, 0.75f, 0.65f), false);
            
            // Prop Materials
            CreateMaterial("Assets/Materials/Buildings/Prop_Counter.mat", new Color(0.8f, 0.9f, 0.8f), false);
            CreateMaterial("Assets/Materials/Buildings/Prop_Shelf.mat", new Color(0.6f, 0.4f, 0.2f), false);
            
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
        }

        private void CreatePrefabs()
        {
            // Grid Tile Prefab
            if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Grid/TilePrefab.prefab") == null)
            {
                GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Quad);
                tile.transform.rotation = Quaternion.Euler(90, 0, 0); 
                DestroyImmediate(tile.GetComponent<Collider>()); 
                PrefabUtility.SaveAsPrefabAsset(tile, "Assets/Prefabs/Grid/TilePrefab.prefab");
                DestroyImmediate(tile);
            }

            // NEW: Generate Interior-Style Buildings
            CreateInteriorPrefab("KiranaStore", "Assets/Materials/Buildings/Kirana_Theme.mat");
            CreateInteriorPrefab("PizzaOutlet", "Assets/Materials/Buildings/Pizza_Theme.mat");
            CreateInteriorPrefab("Cafe", "Assets/Materials/Buildings/Cafe_Theme.mat");
        }

        private void CreateInteriorPrefab(string name, string floorMatPath)
        {
            string path = $"Assets/Prefabs/Buildings/{name}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return; // Skip if exists

            // Base parent object
            GameObject building = new GameObject(name);
            
            // Materials
            Material floorMat = AssetDatabase.LoadAssetAtPath<Material>(floorMatPath);
            Material wallMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Buildings/Prop_Counter.mat"); // Light color for walls
            Material counterMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Buildings/Prop_Counter.mat");
            Material shelfMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Buildings/Prop_Shelf.mat");

            // Build dimensions (assuming TileSize is 10, building footprint is 9.6x9.6 to leave a small path)
            float buildingSize = 9.6f;
            float wallHeight = 3.0f;
            float wallThickness = 0.4f;

            // 1. Floor
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(building.transform);
            floor.transform.localPosition = new Vector3(0, 0.1f, 0);
            floor.transform.localScale = new Vector3(buildingSize, 0.2f, buildingSize);
            if (floorMat != null) floor.GetComponent<Renderer>().material = floorMat;

            // 2. Back Wall
            GameObject backWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backWall.name = "BackWall";
            backWall.transform.SetParent(building.transform);
            backWall.transform.localPosition = new Vector3(0, wallHeight/2f, buildingSize/2f - wallThickness/2f);
            backWall.transform.localScale = new Vector3(buildingSize, wallHeight, wallThickness);
            if (wallMat != null) backWall.GetComponent<Renderer>().material = wallMat;

            // 3. Left Wall
            GameObject leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftWall.name = "LeftWall";
            leftWall.transform.SetParent(building.transform);
            leftWall.transform.localPosition = new Vector3(-buildingSize/2f + wallThickness/2f, wallHeight/2f, 0);
            leftWall.transform.localScale = new Vector3(wallThickness, wallHeight, buildingSize);
            if (wallMat != null) leftWall.GetComponent<Renderer>().material = wallMat;

            // 4. Right Wall (Front is open so player can see inside)
            GameObject rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightWall.name = "RightWall";
            rightWall.transform.SetParent(building.transform);
            rightWall.transform.localPosition = new Vector3(buildingSize/2f - wallThickness/2f, wallHeight/2f, 0);
            rightWall.transform.localScale = new Vector3(wallThickness, wallHeight, buildingSize);
            if (wallMat != null) rightWall.GetComponent<Renderer>().material = wallMat;

            // 5. Checkout Counter
            GameObject counter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            counter.name = "CheckoutCounter";
            counter.transform.SetParent(building.transform);
            counter.transform.localPosition = new Vector3(0, 0.5f, -2.5f);
            counter.transform.localScale = new Vector3(3.5f, 1f, 1f);
            if (counterMat != null) counter.GetComponent<Renderer>().material = counterMat;

            // 6. Shelves (Left & Right)
            GameObject shelf1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shelf1.name = "Shelf_1";
            shelf1.transform.SetParent(building.transform);
            shelf1.transform.localPosition = new Vector3(-2.5f, 1f, 1.5f);
            shelf1.transform.localScale = new Vector3(1f, 2f, 4f);
            if (shelfMat != null) shelf1.GetComponent<Renderer>().material = shelfMat;

            GameObject shelf2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shelf2.name = "Shelf_2";
            shelf2.transform.SetParent(building.transform);
            shelf2.transform.localPosition = new Vector3(2.5f, 1f, 1.5f);
            shelf2.transform.localScale = new Vector3(1f, 2f, 4f);
            if (shelfMat != null) shelf2.GetComponent<Renderer>().material = shelfMat;

            // Put everything on the "Building" layer
            int buildingLayer = LayerMask.NameToLayer("Building");
            if (buildingLayer > -1)
            {
                foreach(Transform child in building.GetComponentsInChildren<Transform>())
                {
                    child.gameObject.layer = buildingLayer;
                }
            }

            // Save and clean up
            PrefabUtility.SaveAsPrefabAsset(building, path);
            DestroyImmediate(building);
        }

        #endregion

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
            {
                if (layers.GetArrayElementAtIndex(i).stringValue == layerName) return;
            }
            
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
        
        private void ClearScene()
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas != null) DestroyImmediate(canvas);
            
            var es = GameObject.Find("EventSystem");
            if (es != null) DestroyImmediate(es);
        }

        private void CreateEventSystem()
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        #region UI Creation (Fixed Layouts)

        private void CreateUISystem()
        {
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); // Mobile Portrait
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            
            canvasObj.AddComponent<GraphicRaycaster>();
            
            CreateHUD(canvasObj.transform);
            CreateBuildMenu(canvasObj.transform);
            CreateLandPurchasePopup(canvasObj.transform);
        }
        
        private void CreateHUD(Transform parent)
        {
            GameObject hudObj = CreateUIObject("HUD", parent);
            StretchToParent(hudObj);
            var hudManager = hudObj.AddComponent<AIBusinessTycoon.UI.HUDManager>();
            
            GameObject topPanel = CreatePanel("TopPanel", hudObj.transform, 
                new Vector2(0, 0.85f), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, Vector2.zero);
            topPanel.GetComponent<Image>().color = primaryColor;
            
            var nameObj = CreateText("PlayerName", topPanel.transform, "Player Name", 50, FontStyles.Bold, whiteColor,
                new Vector2(0.05f, 0.5f), new Vector2(0.95f, 0.9f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var nameTxt = nameObj.GetComponent<TextMeshProUGUI>();
            nameTxt.alignment = TextAlignmentOptions.BottomLeft;
            nameTxt.enableWordWrapping = false;
            
            var moneyObj = CreateText("Money", topPanel.transform, "Rs.10,000", 80, FontStyles.Bold, accentColor,
                new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var moneyTxt = moneyObj.GetComponent<TextMeshProUGUI>();
            moneyTxt.alignment = TextAlignmentOptions.TopLeft;
            moneyTxt.enableWordWrapping = false;

            GameObject statsContainer = CreateUIObject("StatsContainer", hudObj.transform);
            RectTransform statsRect = statsContainer.GetComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0, 0.77f);
            statsRect.anchorMax = new Vector2(1, 0.85f);
            statsRect.pivot = new Vector2(0.5f, 1);
            statsRect.anchoredPosition = Vector2.zero;
            statsRect.sizeDelta = Vector2.zero;

            GameObject bizCard = CreateStatCard("BizCard", statsContainer.transform, "0", "Businesses", secondaryColor, 
                new Vector2(0.02f, 0), new Vector2(0.32f, 1));
            GameObject lndCard = CreateStatCard("LandCard", statsContainer.transform, "1", "Land Tiles", new Color(0.4f, 0.6f, 0.9f), 
                new Vector2(0.35f, 0), new Vector2(0.65f, 1));
            GameObject revCard = CreateStatCard("RevCard", statsContainer.transform, "Rs.0", "Revenue", accentColor, 
                new Vector2(0.68f, 0), new Vector2(0.98f, 1));
            
            GameObject statusBar = CreatePanel("StatusBar", hudObj.transform,
                new Vector2(0, 0), new Vector2(1, 0.05f), new Vector2(0.5f, 0), Vector2.zero, Vector2.zero);
            statusBar.GetComponent<Image>().color = new Color(0, 0, 0, 0.8f);
            
            var statusTxtObj = CreateText("StatusText", statusBar.transform, "Ready to build!", 35, FontStyles.Normal, whiteColor,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var statusTxt = statusTxtObj.GetComponent<TextMeshProUGUI>();
            statusTxt.alignment = TextAlignmentOptions.Center;
            
            SetFieldValue(hudManager, "playerNameText", nameTxt);
            SetFieldValue(hudManager, "moneyText", moneyTxt);
            SetFieldValue(hudManager, "businessCountText", bizCard.transform.Find("Value").GetComponent<TextMeshProUGUI>());
            SetFieldValue(hudManager, "landTilesText", lndCard.transform.Find("Value").GetComponent<TextMeshProUGUI>());
            SetFieldValue(hudManager, "revenueText", revCard.transform.Find("Value").GetComponent<TextMeshProUGUI>());
            SetFieldValue(hudManager, "statusText", statusTxt);
        }

        private GameObject CreateStatCard(string name, Transform parent, string value, string label, Color color, Vector2 aMin, Vector2 aMax)
        {
            GameObject card = CreatePanel(name, parent, aMin, aMax, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            card.GetComponent<Image>().color = color;
            
            var vObj = CreateText("Value", card.transform, value, 45, FontStyles.Bold, whiteColor,
                new Vector2(0, 0.4f), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            vObj.GetComponent<TextMeshProUGUI>().enableWordWrapping = false;
            
            var lObj = CreateText("Label", card.transform, label, 30, FontStyles.Normal, whiteColor,
                new Vector2(0, 0), new Vector2(1, 0.4f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            lObj.GetComponent<TextMeshProUGUI>().enableWordWrapping = false;

            return card;
        }
        
        private void CreateBuildMenu(Transform parent)
        {
            GameObject menuObj = CreateUIObject("BuildMenu", parent);
            StretchToParent(menuObj);
            var buildMenuManager = menuObj.AddComponent<AIBusinessTycoon.UI.BuildMenuManager>();
            
            GameObject menuPanel = CreatePanel("MenuPanel", menuObj.transform,
                new Vector2(0, 0), new Vector2(1, 0.6f), new Vector2(0.5f, 0), Vector2.zero, Vector2.zero);
            menuPanel.GetComponent<Image>().color = new Color(0.95f, 0.95f, 0.95f, 0.98f);
            
            var title = CreateText("Title", menuPanel.transform, "BUILD MENU", 55, FontStyles.Bold, darkTextColor,
                new Vector2(0, 0.85f), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            title.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            
            GameObject cardsContainer = CreateUIObject("CardsContainer", menuPanel.transform);
            RectTransform cardsRect = cardsContainer.GetComponent<RectTransform>();
            cardsRect.anchorMin = new Vector2(0.05f, 0);
            cardsRect.anchorMax = new Vector2(0.95f, 0.85f);
            cardsRect.anchoredPosition = Vector2.zero;
            cardsRect.sizeDelta = Vector2.zero;
            
            GameObject kiranaCard = CreateBuildingButton("Card_Kirana", cardsContainer.transform, "Kirana Store", "Rs.5,000", secondaryColor,
                new Vector2(0, 0.68f), new Vector2(1, 0.98f));
            
            GameObject pizzaCard = CreateBuildingButton("Card_Pizza", cardsContainer.transform, "Pizza Outlet", "Rs.15,000", new Color(0.9f, 0.3f, 0.3f),
                new Vector2(0, 0.35f), new Vector2(1, 0.65f));
            
            GameObject cafeCard = CreateBuildingButton("Card_Cafe", cardsContainer.transform, "Cafe", "Rs.25,000", new Color(0.4f, 0.3f, 0.2f),
                new Vector2(0, 0.02f), new Vector2(1, 0.32f));
            
            SetFieldValue(buildMenuManager, "menuPanel", menuPanel);
            SetFieldValue(buildMenuManager, "kiranaButton", kiranaCard.GetComponent<Button>());
            SetFieldValue(buildMenuManager, "pizzaButton", pizzaCard.GetComponent<Button>());
            SetFieldValue(buildMenuManager, "cafeButton", cafeCard.GetComponent<Button>());
            
            menuPanel.SetActive(false);
        }

        private GameObject CreateBuildingButton(string name, Transform parent, string title, string cost, Color color, Vector2 aMin, Vector2 aMax)
        {
            GameObject btnObj = CreatePanel(name, parent, aMin, aMax, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            btnObj.GetComponent<Image>().color = color;
            Button btn = btnObj.AddComponent<Button>();

            var tTxt = CreateText("Title", btnObj.transform, title, 50, FontStyles.Bold, whiteColor,
                new Vector2(0.05f, 0), new Vector2(0.6f, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            tTxt.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;
            tTxt.GetComponent<TextMeshProUGUI>().enableWordWrapping = false;
                
            var cTxt = CreateText("Cost", btnObj.transform, cost, 50, FontStyles.Bold, whiteColor,
                new Vector2(0.6f, 0), new Vector2(0.95f, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            cTxt.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineRight;
            cTxt.GetComponent<TextMeshProUGUI>().enableWordWrapping = false;

            return btnObj;
        }

        private void CreateLandPurchasePopup(Transform parent)
        {
            GameObject popupObj = CreateUIObject("LandPurchasePopup", parent);
            StretchToParent(popupObj);
            var landPurchaseManager = popupObj.AddComponent<AIBusinessTycoon.UI.LandPurchaseUIManager>();
            
            GameObject overlay = CreatePanel("Overlay", popupObj.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            overlay.GetComponent<Image>().color = new Color(0, 0, 0, 0.6f);
            
            GameObject popupCard = CreatePanel("PopupCard", popupObj.transform, new Vector2(0.1f, 0.3f), new Vector2(0.9f, 0.7f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            popupCard.GetComponent<Image>().color = new Color(0.95f, 0.95f, 0.97f, 1f);
            
            GameObject accentBar = CreatePanel("AccentBar", popupCard.transform, new Vector2(0, 0.8f), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, Vector2.zero);
            accentBar.GetComponent<Image>().color = primaryColor;
            
            var titleObj = CreateText("Title", accentBar.transform, "Purchase Land?", 52, FontStyles.Bold, Color.white, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            
            var costObj = CreateText("Cost", popupCard.transform, "Rs.2,000", 80, FontStyles.Bold, primaryColor, new Vector2(0, 0.55f), new Vector2(1, 0.8f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var posObj = CreateText("Position", popupCard.transform, "Location: (0, 0)", 30, FontStyles.Normal, new Color(0.5f, 0.5f, 0.5f), new Vector2(0, 0.45f), new Vector2(1, 0.58f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var moneyObj = CreateText("PlayerMoney", popupCard.transform, "Your Balance: Rs.10,000", 32, FontStyles.Normal, new Color(0.2f, 0.2f, 0.2f), new Vector2(0, 0.33f), new Vector2(1, 0.46f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var affordObj = CreateText("Affordability", popupCard.transform, "Remaining: Rs.8,000", 32, FontStyles.Bold, Color.green, new Vector2(0, 0.22f), new Vector2(1, 0.35f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            
            GameObject cancelBtn = CreatePanel("CancelButton", popupCard.transform, new Vector2(0.05f, 0.03f), new Vector2(0.47f, 0.2f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            cancelBtn.GetComponent<Image>().color = new Color(0.8f, 0.8f, 0.8f);
            cancelBtn.AddComponent<Button>();
            CreateText("Text", cancelBtn.transform, "Cancel", 40, FontStyles.Bold, new Color(0.3f, 0.3f, 0.3f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            
            GameObject confirmBtn = CreatePanel("ConfirmButton", popupCard.transform, new Vector2(0.53f, 0.03f), new Vector2(0.95f, 0.2f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            confirmBtn.GetComponent<Image>().color = secondaryColor;
            confirmBtn.AddComponent<Button>();
            var confirmTxtObj = CreateText("Text", confirmBtn.transform, "Buy Land", 40, FontStyles.Bold, Color.white, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            
            SetFieldValue(landPurchaseManager, "popupPanel", popupObj);
            SetFieldValue(landPurchaseManager, "titleText", titleObj.GetComponent<TextMeshProUGUI>());
            SetFieldValue(landPurchaseManager, "costText", costObj.GetComponent<TextMeshProUGUI>());
            SetFieldValue(landPurchaseManager, "positionText", posObj.GetComponent<TextMeshProUGUI>());
            SetFieldValue(landPurchaseManager, "playerMoneyText", moneyObj.GetComponent<TextMeshProUGUI>());
            SetFieldValue(landPurchaseManager, "affordabilityText", affordObj.GetComponent<TextMeshProUGUI>());
            SetFieldValue(landPurchaseManager, "cancelButton", cancelBtn.GetComponent<Button>());
            SetFieldValue(landPurchaseManager, "confirmButton", confirmBtn.GetComponent<Button>());
            SetFieldValue(landPurchaseManager, "confirmButtonText", confirmTxtObj.GetComponent<TextMeshProUGUI>());
            
            popupObj.SetActive(false);
        }

        private void StretchToParent(GameObject obj)
        {
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
        }
        
        private GameObject CreateUIObject(string name, Transform parent) { var go = new GameObject(name); go.transform.SetParent(parent, false); go.AddComponent<RectTransform>(); return go; }
        private GameObject CreatePanel(string name, Transform parent, Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 size) { var go = CreateUIObject(name, parent); var rt = go.GetComponent<RectTransform>(); rt.anchorMin = min; rt.anchorMax = max; rt.pivot = pivot; rt.anchoredPosition = pos; rt.sizeDelta = size; go.AddComponent<Image>(); return go; }
        private GameObject CreateText(string name, Transform parent, string text, int size, FontStyles style, Color color, Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 delta) { var go = CreateUIObject(name, parent); var rt = go.GetComponent<RectTransform>(); rt.anchorMin = min; rt.anchorMax = max; rt.pivot = pivot; rt.anchoredPosition = pos; rt.sizeDelta = delta; var tmp = go.AddComponent<TextMeshProUGUI>(); tmp.text = text; tmp.fontSize = size; tmp.fontStyle = style; tmp.color = color; tmp.alignment = TextAlignmentOptions.Center; return go; }
        private void SetFieldValue(object obj, string fieldName, object value) { var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance); if (field != null) field.SetValue(obj, value); }
        
        #endregion

        #region Component Connection
        
        private void ConnectComponents()
        {
            var buildMenuManager = FindObjectOfType<AIBusinessTycoon.UI.BuildMenuManager>();
            var placementManager = FindObjectOfType<AIBusinessTycoon.Managers.BuildingPlacementManager>();
            var gridManager = FindObjectOfType<AIBusinessTycoon.Managers.GridManager>();
            var cameraController = FindObjectOfType<AIBusinessTycoon.Managers.CameraController>();
            
            if (buildMenuManager != null)
            {
                SetFieldValue(buildMenuManager, "kiranaPrefab", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/KiranaStore.prefab"));
                SetFieldValue(buildMenuManager, "pizzaPrefab", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/PizzaOutlet.prefab"));
                SetFieldValue(buildMenuManager, "cafePrefab", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Cafe.prefab"));
                SetFieldValue(buildMenuManager, "kiranaCost", 5000f);
                SetFieldValue(buildMenuManager, "pizzaCost", 15000f);
                SetFieldValue(buildMenuManager, "cafeCost", 25000f);
            }
            
            if (placementManager != null)
            {
                int gridLayerIndex = LayerMask.NameToLayer("Grid");
                if (gridLayerIndex != -1)
                {
                    LayerMask gridMask = 1 << gridLayerIndex;
                    SetFieldValue(placementManager, "groundLayer", gridMask);
                }
            }

            if (gridManager != null)
            {
                // CRITICAL: SET TILE SIZE TO 10
                SetFieldValue(gridManager, "tileSize", 10f); 

                SetFieldValue(gridManager, "emptyTileMaterial", AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Grid/EmptyTile.mat"));
                SetFieldValue(gridManager, "ownedTileMaterial", AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Grid/OwnedTile.mat"));
                SetFieldValue(gridManager, "validPlacementMaterial", AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Grid/ValidPlacement.mat"));
                SetFieldValue(gridManager, "invalidPlacementMaterial", AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Grid/InvalidPlacement.mat"));
                SetFieldValue(gridManager, "purchasableTileMaterial", AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Grid/PurchasableTile.mat"));
                SetFieldValue(gridManager, "tilePrefab", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Grid/TilePrefab.prefab"));
            }

            // CRITICAL: UPDATE CAMERA SETTINGS FOR LARGER SCALE
            if (cameraController != null)
            {
                SetFieldValue(cameraController, "panSpeed", 50f);
                SetFieldValue(cameraController, "zoomSpeed", 20f);
                SetFieldValue(cameraController, "minZoom", 10f);
                SetFieldValue(cameraController, "maxZoom", 100f);
                
                Camera.main.transform.localPosition = new Vector3(0, 30, -30); // Pull camera back initially
            }
            
            GameObject ground = GameObject.Find("Ground");
            if (ground != null) 
            {
                ground.layer = LayerMask.NameToLayer("Grid");
                ground.transform.localScale = new Vector3(100, 1, 100); // Scale up ground plane
            }
        }

        #endregion
    }
}
