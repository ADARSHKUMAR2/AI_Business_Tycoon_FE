using UnityEngine;
using UnityEditor;
using TMPro;
using AIBusinessTycoon.Managers;

namespace AIBusinessTycoon.Editor
{
    public static class InteractableShelfSetup
    {
        // Define how many boxes to pre-generate in the pool (e.g., 20)
        private const int MAX_POOL_CAPACITY = 20;

        [MenuItem("GameObject/AI Business Tycoon/Setup Selected Shelves", false, 0)]
        private static void SetupShelves(MenuCommand menuCommand)
        {
            if (Selection.gameObjects.Length == 0)
            {
                Debug.LogWarning("[InteractableShelfSetup] No GameObjects selected.");
                return;
            }

            int processedCount = 0;

            foreach (GameObject obj in Selection.gameObjects)
            {
                InteractableShelf shelf = obj.GetComponent<InteractableShelf>();
                if (shelf == null) continue;

                Undo.RecordObject(obj, "Setup Interactable Shelf");
                
                SetupCollider(obj);
                Transform container = SetupItemContainer(obj);
                
                // Pre-generate the object pool inside the container
                GameObject boxPrefab = GetOrCreateBoxPrefab();
                PopulateObjectPool(container, boxPrefab, MAX_POOL_CAPACITY);

                SetupFloatingUI(obj, shelf);

                // Wire the container
                SerializedObject serializedShelf = new SerializedObject(shelf);
                serializedShelf.FindProperty("itemContainer").objectReferenceValue = container;
                serializedShelf.ApplyModifiedProperties();

                processedCount++;
            }

            Debug.Log($"[InteractableShelfSetup] Successfully configured {processedCount} shelves with object pools.");
        }

        private static void SetupCollider(GameObject shelfObj)
        {
            BoxCollider boxCol = shelfObj.GetComponent<BoxCollider>();
            if (boxCol == null)
            {
                boxCol = Undo.AddComponent<BoxCollider>(shelfObj);
                boxCol.center = new Vector3(0, 0.5f, 0);
                boxCol.size = new Vector3(1.5f, 1.5f, 0.5f);
            }

            Collider[] colliders = shelfObj.GetComponents<Collider>();
            foreach (Collider c in colliders)
            {
                Undo.RecordObject(c, "Set Collider to Trigger");
                c.isTrigger = true;
            }
        }

        private static Transform SetupItemContainer(GameObject shelfObj)
        {
            Transform existing = shelfObj.transform.Find("ItemContainer");
            if (existing != null) return existing;

            GameObject container = new GameObject("ItemContainer");
            container.transform.SetParent(shelfObj.transform, false);
            container.transform.localPosition = new Vector3(0, 1.2f, 0);
            Undo.RegisterCreatedObjectUndo(container, "Create ItemContainer");

            return container.transform;
        }

        private static GameObject GetOrCreateBoxPrefab()
        {
            string prefabPath = "Assets/Prefabs/ShelfBox.prefab";
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existingPrefab != null) return existingPrefab;

            GameObject tempBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tempBox.name = "ShelfBox";
            tempBox.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            
            Collider boxCol = tempBox.GetComponent<Collider>();
            if (boxCol != null) Object.DestroyImmediate(boxCol);

            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");

            GameObject newPrefab = PrefabUtility.SaveAsPrefabAsset(tempBox, prefabPath);
            Object.DestroyImmediate(tempBox);
            
            return newPrefab;
        }

        private static void PopulateObjectPool(Transform container, GameObject boxPrefab, int capacity)
        {
            // Count existing boxes
            int existingCount = container.childCount;

            // Add missing boxes
            for (int i = existingCount; i < capacity; i++)
            {
                GameObject box = (GameObject)PrefabUtility.InstantiatePrefab(boxPrefab, container);
                box.name = $"Box_{i}";
                
                // Position them in a grid (3 wide)
                float xOffset = -0.4f + (i % 3) * 0.4f;
                float yOffset = (i / 3) * 0.4f;
                box.transform.localPosition = new Vector3(xOffset, yOffset, 0);

                // Keep them enabled in Editor so you can see them, script hides them on Start
                box.SetActive(true); 

                Undo.RegisterCreatedObjectUndo(box, "Create Pooled Box");
            }
        }

        private static void SetupFloatingUI(GameObject shelfObj, InteractableShelf shelfScript)
        {
            Transform existingCanvas = shelfObj.transform.Find("ShelfCanvas");
            GameObject canvasObj;
            
            if (existingCanvas != null)
            {
                canvasObj = existingCanvas.gameObject;
            }
            else
            {
                canvasObj = new GameObject("ShelfCanvas");
                canvasObj.transform.SetParent(shelfObj.transform, false);
                canvasObj.transform.localPosition = new Vector3(0, 1.5f, 0);
                
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                
                RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
                canvasRect.sizeDelta = new Vector2(300, 150);
                canvasObj.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                
                Undo.RegisterCreatedObjectUndo(canvasObj, "Create ShelfCanvas");
            }

            Transform existingName = canvasObj.transform.Find("ItemNameText");
            if (existingName == null)
            {
                GameObject nameObj = new GameObject("ItemNameText");
                nameObj.transform.SetParent(canvasObj.transform, false);
                
                TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
                nameText.text = "Item Name";
                nameText.fontSize = 28;
                nameText.alignment = TextAlignmentOptions.Center;
                nameText.color = new Color(1f, 0.8f, 0.2f);
                nameText.outlineWidth = 0.2f;
                nameText.outlineColor = new Color(0, 0, 0, 0.8f);
                
                RectTransform nameRect = nameObj.GetComponent<RectTransform>();
                nameRect.sizeDelta = new Vector2(300, 50);
                nameRect.localPosition = new Vector3(0, 40, 0);
                
                SerializedObject so = new SerializedObject(shelfScript);
                so.FindProperty("itemNameTextUI").objectReferenceValue = nameText;
                so.ApplyModifiedProperties();
                
                Undo.RegisterCreatedObjectUndo(nameObj, "Create ItemNameText");
            }

            Transform existingStock = canvasObj.transform.Find("StockText");
            if (existingStock == null)
            {
                GameObject stockObj = new GameObject("StockText");
                stockObj.transform.SetParent(canvasObj.transform, false);
                
                TextMeshProUGUI stockText = stockObj.AddComponent<TextMeshProUGUI>();
                stockText.text = "0/10";
                stockText.fontSize = 36;
                stockText.alignment = TextAlignmentOptions.Center;
                stockText.fontStyle = FontStyles.Bold;
                stockText.outlineWidth = 0.2f;
                stockText.outlineColor = new Color(0, 0, 0, 0.8f);
                
                RectTransform stockRect = stockObj.GetComponent<RectTransform>();
                stockRect.sizeDelta = new Vector2(300, 100);
                stockRect.localPosition = new Vector3(0, -10, 0);
                
                SerializedObject so = new SerializedObject(shelfScript);
                so.FindProperty("stockTextUI").objectReferenceValue = stockText;
                so.ApplyModifiedProperties();
                
                Undo.RegisterCreatedObjectUndo(stockObj, "Create StockText");
            }
        }
    }
}
