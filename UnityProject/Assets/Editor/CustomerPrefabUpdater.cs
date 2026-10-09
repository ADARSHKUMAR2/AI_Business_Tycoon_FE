using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public static class CustomerPrefabUpdater
{
    [MenuItem("AI Business Tycoon/Update Customer Prefab UI")]
    public static void UpdatePrefab()
    {
        string prefabPath = "Assets/Prefabs/Players/Customer.prefab";
        GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        
        if (prefabRoot == null)
        {
            Debug.LogError("Could not find prefab at: " + prefabPath);
            return;
        }

        using (var editingScope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
        {
            GameObject instance = editingScope.prefabContentsRoot;

            // 1. Setup EmojiCanvas
            Transform canvasTransform = instance.transform.Find("EmojiCanvas");
            if (canvasTransform == null)
            {
                GameObject canvasObj = new GameObject("EmojiCanvas");
                canvasObj.transform.SetParent(instance.transform, false);
                canvasObj.transform.localPosition = new Vector3(0, 2.2f, 0); 
                
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                
                RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
                canvasRect.sizeDelta = new Vector2(2f, 2f);
                canvasTransform = canvasObj.transform;
            }

            Transform legacyText = canvasTransform.Find("EmojiText");
            if (legacyText != null)
            {
                legacyText.gameObject.SetActive(false);
            }

            Transform iconTransform = canvasTransform.Find("StatusIcon");
            if (iconTransform == null)
            {
                GameObject iconObject = new GameObject("StatusIcon", typeof(RectTransform), typeof(Image));
                iconObject.transform.SetParent(canvasTransform, false);
                iconObject.transform.localPosition = Vector3.zero;
                iconObject.transform.localScale = Vector3.one;
                
                RectTransform iconRect = iconObject.GetComponent<RectTransform>();
                iconRect.sizeDelta = new Vector2(1.4f, 1.4f);
                
                Image img = iconObject.GetComponent<Image>();
                img.preserveAspect = true;
                img.raycastTarget = false;
                img.enabled = false;
                
                iconTransform = iconObject.transform;
            }

            // 2. Setup CarryPoint and Box Pool
            Transform carryPoint = instance.transform.Find("CarryPoint");
            if (carryPoint == null)
            {
                GameObject cp = new GameObject("CarryPoint");
                cp.transform.SetParent(instance.transform, false);
                cp.transform.localPosition = new Vector3(0, 0.6f, 0.5f); 
                carryPoint = cp.transform;
            }

            // Clean up old boxes if they were unstructured
            for (int i = carryPoint.childCount - 1; i >= 0; i--)
            {
                if (!carryPoint.GetChild(i).name.StartsWith("PooledBox_"))
                {
                    Object.DestroyImmediate(carryPoint.GetChild(i).gameObject);
                }
            }

            // Load ShelfBoxMaterial from the project (if available) or create a fallback
            Material boxMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Buildings/ShelfBoxMaterial.mat");

            for (int i = 0; i < 5; i++)
            {
                string boxName = $"PooledBox_{i}";
                Transform box = carryPoint.Find(boxName);
                if (box == null)
                {
                    GameObject newBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    newBox.name = boxName;
                    newBox.transform.SetParent(carryPoint, false);
                    newBox.transform.localPosition = new Vector3(0, i * 0.45f, 0);
                    newBox.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
                    
                    var renderer = newBox.GetComponent<Renderer>();
                    if (renderer != null && boxMat != null)
                    {
                        renderer.sharedMaterial = boxMat;
                    }
                    
                    var collider = newBox.GetComponent<Collider>();
                    if (collider != null) Object.DestroyImmediate(collider);
                    
                    newBox.SetActive(false);
                }
            }

            Debug.Log("Updated Customer prefab with pre-authored EmojiCanvas, StatusIcon, CarryPoint, and Box Pool.");
        }
    }
}