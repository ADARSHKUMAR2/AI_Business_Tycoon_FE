using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using AIBusinessTycoon.Managers;

namespace AIBusinessTycoon.Editor
{
    public class ProceduralShelfBuilder : UnityEditor.Editor
    {
        [MenuItem("AI Business Tycoon/Build 3D Objects/Generate Procedural Shelf")]
        public static void GenerateShelf()
        {
            // 1. Create Root Object
            GameObject root = new GameObject("ProceduralRetailShelf");
            var interactableShelf = root.AddComponent<InteractableShelf>();

            // 2. Create Wood Material
            Material woodMat = new Material(Shader.Find("Standard")); // Fallback
            woodMat.color = new Color(0.35f, 0.20f, 0.10f); // Dark Wood
            
            var urpShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpShader != null) woodMat.shader = urpShader;

            // 3. Build Shelf Structure
            CreateShelfPart("LeftPanel", root.transform, woodMat, new Vector3(-0.48f, 0.6f, 0f), new Vector3(0.04f, 1.2f, 0.4f));
            CreateShelfPart("RightPanel", root.transform, woodMat, new Vector3(0.48f, 0.6f, 0f), new Vector3(0.04f, 1.2f, 0.4f));
            CreateShelfPart("BackPanel", root.transform, woodMat, new Vector3(0f, 0.6f, 0.18f), new Vector3(0.92f, 1.2f, 0.04f));
            CreateShelfPart("Shelf_Base", root.transform, woodMat, new Vector3(0f, 0.05f, -0.02f), new Vector3(0.92f, 0.1f, 0.36f));
            CreateShelfPart("Shelf_Tier1", root.transform, woodMat, new Vector3(0f, 0.4f, -0.02f), new Vector3(0.92f, 0.04f, 0.36f));
            CreateShelfPart("Shelf_Tier2", root.transform, woodMat, new Vector3(0f, 0.75f, -0.02f), new Vector3(0.92f, 0.04f, 0.36f));
            CreateShelfPart("Shelf_Top", root.transform, woodMat, new Vector3(0f, 1.1f, -0.02f), new Vector3(0.92f, 0.04f, 0.36f));

            // 4. Create Item Container
            GameObject itemContainer = new GameObject("ItemContainer");
            itemContainer.transform.SetParent(root.transform);
            itemContainer.transform.localPosition = new Vector3(0f, 0.1f, -0.1f);
            interactableShelf.GetType().GetField("itemContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(interactableShelf, itemContainer.transform);

            // 4b. Procedurally generate perfectly placed visual stock boxes
            // We have 4 shelves at world Y levels: 0.05f, 0.4f, 0.75f, 1.1f
            // ItemContainer is at Y=0.1f. The boxes are 0.15f tall, meaning their local center Y should be:
            float[] shelfHeights = new float[] { 0.05f, 0.4f, 0.75f, 1.1f };
            float[] boxXPositions = new float[] { -0.3f, -0.1f, 0.1f, 0.3f };
            
            Material boxMat = new Material(Shader.Find("Standard")) { color = new Color(0.85f, 0.85f, 0.85f) };
            if (urpShader != null) boxMat.shader = urpShader;

            int boxIndex = 0;
            for (int level = 0; level < shelfHeights.Length; level++)
            {
                for (int x = 0; x < boxXPositions.Length; x++)
                {
                    var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    box.name = $"VisualBox_{boxIndex}";
                    box.transform.SetParent(itemContainer.transform);
                    
                    // Center of the box sits exactly on top of the shelf
                    float localY = (shelfHeights[level] - itemContainer.transform.localPosition.y) + (0.15f / 2f);
                    
                    box.transform.localPosition = new Vector3(boxXPositions[x], localY, 0f);
                    box.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
                    
                    box.GetComponent<Renderer>().sharedMaterial = boxMat;
                    DestroyImmediate(box.GetComponent<Collider>());
                    
                    boxIndex++;
                }
            }


            // 5. Create UI Canvas for Stock Text
            GameObject canvasGO = new GameObject("ShelfCanvas");
            canvasGO.transform.SetParent(root.transform);
            canvasGO.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(2f, 1f);
            canvas.GetComponent<RectTransform>().localScale = new Vector3(0.5f, 0.5f, 0.5f);

            var stockTextGO = new GameObject("StockText");
            stockTextGO.transform.SetParent(canvasGO.transform);
            stockTextGO.transform.localPosition = Vector3.zero;
            stockTextGO.transform.localScale = Vector3.one;
            
            var stockText = stockTextGO.AddComponent<TextMeshProUGUI>();
            stockText.text = "Item x0";
            stockText.fontSize = 2.5f;
            stockText.alignment = TextAlignmentOptions.Center;
            stockText.color = Color.yellow;
            stockText.fontStyle = FontStyles.Bold;

            interactableShelf.GetType().GetField("stockTextUI", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(interactableShelf, stockText);
            
            var nameTextGO = new GameObject("NameText");
            nameTextGO.transform.SetParent(canvasGO.transform);
            nameTextGO.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            nameTextGO.transform.localScale = Vector3.one;
            
            var nameText = nameTextGO.AddComponent<TextMeshProUGUI>();
            nameText.text = "Shelf";
            nameText.fontSize = 2f;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.color = Color.white;
            
            interactableShelf.GetType().GetField("itemNameTextUI", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(interactableShelf, nameText);

            // 6. Add Hitbox Collider
            var collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.6f, 0f);
            collider.size = new Vector3(1f, 1.2f, 0.4f);

            // 7. Save to Prefabs
            string path = "Assets/Prefabs/ProceduralRetailShelf.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            
            // Clean up
            DestroyImmediate(root);
            
            Debug.Log($"[ProceduralShelfBuilder] Successfully generated detailed 3D shelf prefab at {path}");
        }

        private static void CreateShelfPart(string name, Transform parent, Material mat, Vector3 pos, Vector3 scale)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent);
            part.transform.localPosition = pos;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = mat;
            // Remove sub-colliders, the root will have the master collider
            DestroyImmediate(part.GetComponent<Collider>());
        }
    }
}