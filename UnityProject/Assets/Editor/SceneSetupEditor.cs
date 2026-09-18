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
            CreatePrefabs(); 
            
            ClearScene();
            CreateEventSystem();
            
            CreateEnvironment();
            CreatePlayerAvatar(); 
            
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
            CreateMaterial("Assets/Materials/Grid/EmptyTile.mat", new Color(0.6f, 0.6f, 0.6f, 0.3f), true);
            CreateMaterial("Assets/Materials/Grid/OwnedTile.mat", new Color(0.4f, 0.8f, 0.4f, 0.5f), true);
            CreateMaterial("Assets/Materials/Grid/ValidPlacement.mat", new Color(0.2f, 1f, 0.2f, 0.6f), true);
            CreateMaterial("Assets/Materials/Grid/InvalidPlacement.mat", new Color(1f, 0.2f, 0.2f, 0.6f), true);
            CreateMaterial("Assets/Materials/Grid/PurchasableTile.mat", new Color(0.2f, 0.6f, 1f, 0.5f), true);
            
            CreateMaterial("Assets/Materials/Buildings/Kirana_Theme.mat", new Color(0.95f, 0.85f, 0.7f), false);
            CreateMaterial("Assets/Materials/Buildings/Pizza_Theme.mat", new Color(0.95f, 0.7f, 0.7f), false);
            CreateMaterial("Assets/Materials/Buildings/Cafe_Theme.mat", new Color(0.85f, 0.75f, 0.65f), false);
            
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
            CreateInteriorPrefab("PizzaOutlet", "Assets/Materials/Buildings/Pizza_Theme.mat");
            CreateInteriorPrefab("Cafe", "Assets/Materials/Buildings/Cafe_Theme.mat");
            
            CreateCustomerPrefab();
        }

        private void CreateCustomerPrefab()
        {
            string path = "Assets/Prefabs/Buildings/Customer.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

            GameObject customer = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            customer.name = "Customer";
            customer.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f); 
            
            Rigidbody rb = customer.GetComponent<Rigidbody>();
            if (rb == null) rb = customer.AddComponent<Rigidbody>();
            rb.isKinematic = true; 
            
            DestroyImmediate(customer.GetComponent<CapsuleCollider>());

            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material mat = new Material(urpShader);
            mat.SetColor("_BaseColor", new Color(0.2f, 0.6f, 0.9f));
            customer.GetComponent<Renderer>().material = mat;

            GameObject face = GameObject.CreatePrimitive(PrimitiveType.Cube);
            face.name = "Face";
            face.transform.SetParent(customer.transform);
            face.transform.localScale = new Vector3(0.5f, 0.3f, 0.5f);
            face.transform.localPosition = new Vector3(0, 0.5f, 0.5f); 
            DestroyImmediate(face.GetComponent<Collider>()); 
            
            Material faceMat = new Material(urpShader);
            faceMat.SetColor("_BaseColor", new Color(0.1f, 0.1f, 0.1f));
            face.GetComponent<Renderer>().material = faceMat;

            var agent = customer.AddComponent<UnityEngine.AI.NavMeshAgent>();
            agent.speed = 4f;
            agent.angularSpeed = 720f; 
            agent.acceleration = 12f;
            agent.radius = 0.3f;
            agent.height = 1.2f;
            agent.baseOffset = 0.6f;

            customer.AddComponent<AIBusinessTycoon.Managers.CustomerAI>();

            PrefabUtility.SaveAsPrefabAsset(customer, path);
            DestroyImmediate(customer);
        }

        private void CreateInteriorPrefab(string name, string floorMatPath)
        {
            string path = $"Assets/Prefabs/Buildings/{name}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

            GameObject building = new GameObject(name);
            
            Material floorMat = AssetDatabase.LoadAssetAtPath<Material>(floorMatPath);
            Material wallMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Buildings/Prop_Counter.mat"); 
            Material counterMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Buildings/Prop_Counter.mat");
            Material shelfMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Buildings/Prop_Shelf.mat");

            float buildingSize = 9.6f;
            float wallHeight = 3.0f;
            float wallThickness = 0.4f;

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(building.transform);
            floor.transform.localPosition = new Vector3(0, 0.1f, 0);
            floor.transform.localScale = new Vector3(buildingSize, 0.2f, buildingSize);
            if (floorMat != null) floor.GetComponent<Renderer>().material = floorMat;

            GameObject backWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backWall.name = "BackWall";
            backWall.transform.SetParent(building.transform);
            backWall.transform.localPosition = new Vector3(0, wallHeight/2f, buildingSize/2f - wallThickness/2f);
            backWall.transform.localScale = new Vector3(buildingSize, wallHeight, wallThickness);
            if (wallMat != null) backWall.GetComponent<Renderer>().material = wallMat;

            GameObject leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftWall.name = "LeftWall";
            leftWall.transform.SetParent(building.transform);
            leftWall.transform.localPosition = new Vector3(-buildingSize/2f + wallThickness/2f, wallHeight/2f, 0);
            leftWall.transform.localScale = new Vector3(wallThickness, wallHeight, buildingSize);
            if (wallMat != null) leftWall.GetComponent<Renderer>().material = wallMat;

            GameObject rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightWall.name = "RightWall";
            rightWall.transform.SetParent(building.transform);
            rightWall.transform.localPosition = new Vector3(buildingSize/2f - wallThickness/2f, wallHeight/2f, 0);
            rightWall.transform.localScale = new Vector3(wallThickness, wallHeight, buildingSize);
            if (wallMat != null) rightWall.GetComponent<Renderer>().material = wallMat;

            GameObject counter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            counter.name = "CheckoutCounter";
            counter.transform.SetParent(building.transform);
            counter.transform.localPosition = new Vector3(0, 0.5f, -2.5f);
            counter.transform.localScale = new Vector3(3.5f, 1f, 1f);
            if (counterMat != null) counter.GetComponent<Renderer>().material = counterMat;

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

            int buildingLayer = LayerMask.NameToLayer("Building");
            if (buildingLayer > -1)
            {
                foreach(Transform child in building.GetComponentsInChildren<Transform>())
                {
                    child.gameObject.layer = buildingLayer;
                }
            }

            BoxCollider interactionCollider = building.AddComponent<BoxCollider>();
            interactionCollider.center = new Vector3(0, wallHeight / 2f, 0);
            interactionCollider.size = new Vector3(buildingSize, wallHeight, buildingSize);
            interactionCollider.isTrigger = true; 

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
                Light light = lightObj.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.2f;
                light.color = new Color(1f, 0.96f, 0.84f);
                lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);
            }

            GameObject ground = GameObject.Find("Ground");
            if (ground == null)
            {
                ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "Ground";
            }

            ground.transform.position = new Vector3(0, -0.1f, 0);
            ground.transform.localScale = new Vector3(100, 1, 100);
            ground.layer = LayerMask.NameToLayer("Grid");

            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material groundMat = new Material(urpShader);
            groundMat.SetColor("_BaseColor", new Color(0.3f, 0.4f, 0.3f)); 
            ground.GetComponent<Renderer>().material = groundMat;

            var navMeshSurface = ground.GetComponent<Unity.AI.Navigation.NavMeshSurface>();
            if (navMeshSurface == null)
            {
                navMeshSurface = ground.AddComponent<Unity.AI.Navigation.NavMeshSurface>();
            }

            navMeshSurface.layerMask = LayerMask.GetMask("Grid");
            navMeshSurface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
            navMeshSurface.BuildNavMesh();
        }

        private void CreatePlayerAvatar()
        {
            GameObject playerObj = GameObject.Find("PlayerAvatar");
            if (playerObj == null) playerObj = new GameObject("PlayerAvatar");
            
            CharacterController cc = playerObj.GetComponent<CharacterController>();
            if (cc == null) cc = playerObj.AddComponent<CharacterController>();
            cc.radius = 0.5f;
            cc.height = 2f;
            cc.center = new Vector3(0, 1f, 0);

            var playerController = playerObj.GetComponent<AIBusinessTycoon.Managers.PlayerController>();
            if (playerController == null) playerController = playerObj.AddComponent<AIBusinessTycoon.Managers.PlayerController>();

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
                face.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                face.transform.localPosition = new Vector3(0, 0.5f, 0.5f); 
            }
            else
            {
                visual = visualTrans.gameObject;
            }

            SetFieldValue(playerController, "avatarVisual", visual);
            SetFieldValue(playerController, "moveSpeed", 8f);

            playerObj.layer = LayerMask.NameToLayer("Default");
            playerObj.SetActive(false);
        }

        #endregion

        #region UI Creation (Fixed Layouts & Managers)

        private void CreateUISystem()
        {
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); 
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            
            canvasObj.AddComponent<GraphicRaycaster>();

            GameObject uiManagersObj = new GameObject("[ UI MANAGERS ]");
            var hudManager = uiManagersObj.AddComponent<AIBusinessTycoon.UI.HUDManager>();
            var buildMenuManager = uiManagersObj.AddComponent<AIBusinessTycoon.UI.BuildMenuManager>();
            var landPurchaseManager = uiManagersObj.AddComponent<AIBusinessTycoon.UI.LandPurchaseUIManager>();
            var storeUIManager = uiManagersObj.AddComponent<AIBusinessTycoon.UI.StoreUIManager>();

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

        private void CreateStoreUI(Transform parent, AIBusinessTycoon.UI.StoreUIManager storeUIManager)
        {
            GameObject storeUIObj = CreateUIObject("StoreUI_Visuals", parent);
            StretchToParent(storeUIObj);

            GameObject topBar = CreatePanel("StoreTopBar", storeUIObj.transform,
                new Vector2(0, 0.85f), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, Vector2.zero);
            topBar.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

            var storeNameObj = CreateText("StoreName", topBar.transform, "Kirana Store", 55, FontStyles.Bold, Color.white,
                new Vector2(0.05f, 0.5f), new Vector2(0.95f, 0.95f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            storeNameObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.BottomLeft;

            var storeTypeObj = CreateText("StoreType", topBar.transform, "KIRANA", 30, FontStyles.Normal, new Color(1f, 0.76f, 0.03f),
                new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            storeTypeObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.TopLeft;

            GameObject exitBtn = CreatePanel("ExitButton", topBar.transform,
                new Vector2(0.78f, 0.1f), new Vector2(0.98f, 0.9f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            exitBtn.GetComponent<Image>().color = new Color(0.85f, 0.2f, 0.2f);
            exitBtn.AddComponent<Button>();
            var exitTxt = CreateText("Text", exitBtn.transform, "Exit Store", 30, FontStyles.Bold, Color.white,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            exitTxt.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

            GameObject joystickPanel = CreateUIObject("JoystickPanel", storeUIObj.transform);
            RectTransform joystickRect = joystickPanel.GetComponent<RectTransform>();
            joystickRect.anchorMin = new Vector2(0, 0);
            joystickRect.anchorMax = new Vector2(0.35f, 0.25f);
            joystickRect.anchoredPosition = Vector2.zero;
            joystickRect.sizeDelta = Vector2.zero;

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
            SetFieldValue(joystickComp, "joystickHandle", joystickHandle.GetComponent<RectTransform>());
            SetFieldValue(joystickComp, "handleRange", 80f);

            SetFieldValue(storeUIManager, "storeUIPanel", storeUIObj);
            SetFieldValue(storeUIManager, "storeNameText", storeNameObj.GetComponent<TextMeshProUGUI>());
            SetFieldValue(storeUIManager, "storeTypeText", storeTypeObj.GetComponent<TextMeshProUGUI>());
            SetFieldValue(storeUIManager, "joystickPanel", joystickPanel);
            SetFieldValue(storeUIManager, "joystick", joystickComp);
            SetFieldValue(storeUIManager, "exitButton", exitBtn.GetComponent<Button>());

            storeUIObj.SetActive(false);
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

        private void CreateLandPurchasePopup(Transform parent, AIBusinessTycoon.UI.LandPurchaseUIManager landPurchaseManager)
        {
            GameObject popupObj = CreateUIObject("LandPurchasePopup_Visuals", parent);
            StretchToParent(popupObj);
            
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
            var gameManager = FindObjectOfType<AIBusinessTycoon.Managers.GameManager>();
            var buildMenuManager = FindObjectOfType<AIBusinessTycoon.UI.BuildMenuManager>();
            var placementManager = FindObjectOfType<AIBusinessTycoon.Managers.BuildingPlacementManager>();
            var gridManager = FindObjectOfType<AIBusinessTycoon.Managers.GridManager>();
            var cameraController = FindObjectOfType<AIBusinessTycoon.Managers.CameraController>();
            var playerController = FindObjectOfType<AIBusinessTycoon.Managers.PlayerController>(true);
            
            if (gameManager != null && playerController != null)
            {
                SetFieldValue(gameManager, "playerController", playerController);
            }

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
                SetFieldValue(gridManager, "tileSize", 10f); 
                SetFieldValue(gridManager, "emptyTileMaterial", AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Grid/EmptyTile.mat"));
                SetFieldValue(gridManager, "ownedTileMaterial", AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Grid/OwnedTile.mat"));
                SetFieldValue(gridManager, "validPlacementMaterial", AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Grid/ValidPlacement.mat"));
                SetFieldValue(gridManager, "invalidPlacementMaterial", AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Grid/InvalidPlacement.mat"));
                SetFieldValue(gridManager, "purchasableTileMaterial", AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Grid/PurchasableTile.mat"));
                SetFieldValue(gridManager, "tilePrefab", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Grid/TilePrefab.prefab"));
            }

            if (cameraController != null)
            {
                SetFieldValue(cameraController, "panSpeed", 50f);
                SetFieldValue(cameraController, "zoomSpeed", 20f);
                SetFieldValue(cameraController, "minZoom", 10f);
                SetFieldValue(cameraController, "maxZoom", 100f);
                Camera.main.transform.localPosition = new Vector3(0, 30, -30); 
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
    }
}
