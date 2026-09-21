// using System.Collections.Generic;
// using System.Reflection;
// using TMPro;
// using UnityEditor;
// using UnityEngine;
// using UnityEngine.UI;

// public class RestaurantPrefabBuilderEditor : EditorWindow
// {
//     private const string MenuPath = "Tools/Restaurant/Create Restaurant";

//     private static readonly IngredientType[] IngredientOrder =
//     {
//         IngredientType.Tomato,
//         IngredientType.Onion,
//         IngredientType.Lettuce,
//         IngredientType.Herb,
//         IngredientType.Flour,
//         IngredientType.Rice
//     };

//     [MenuItem(MenuPath)]
//     public static void CreateRestaurant()
//     {
//         var root = new GameObject("Restaurant");
//         root.transform.position = Vector3.zero;

//         var manager = root.AddComponent<RestaurantManager>();

//         // Create UI
//         CreateInventoryCanvas(root.transform, manager);

//         // Create plots
//         var plotsRoot = new GameObject("IngredientPlots");
//         plotsRoot.transform.SetParent(root.transform);
//         plotsRoot.transform.localPosition = Vector3.zero;

//         var positions = new[]
//         {
//             new Vector3(-2.5f, 0f, -1.5f),
//             new Vector3(-0.8f, 0f, -1.5f),
//             new Vector3(0.9f, 0f, -1.5f),
//             new Vector3(2.6f, 0f, -1.5f),
//             new Vector3(-1.7f, 0f, 1.1f),
//             new Vector3(0.8f, 0f, 1.1f),
//         };

//         for (int i = 0; i < IngredientOrder.Length; i++)
//         {
//             var ingredient = IngredientOrder[i];
//             var plotObject = CreateIngredientPlot(plotsRoot.transform, positions[i], ingredient, manager);
//             CreatePlotTimer(plotObject, ingredient);
//         }

//         // Register each plot directly instead of calling a private method
//         foreach (var plot in plotsRoot.GetComponentsInChildren<IngredientPlot>())
//         {
//             manager.RegisterPlot(plot);
//         }

//         root.transform.localScale = Vector3.one;

//         Selection.activeGameObject = root;

//         // Optional prefab save if a folder is selected
//         var selectedFolder = GetSelectedFolder();
//         if (!string.IsNullOrEmpty(selectedFolder))
//         {
//             var prefabPath = selectedFolder + "/Restaurant.prefab";
//             var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);

//             if (prefab != null)
//             {
//                 Debug.Log($"Restaurant prefab created: {prefabPath}");
//             }

//             UnityEngine.Object.DestroyImmediate(root);
//             Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(prefabPath);
//         }
//     }

//     private static string GetSelectedFolder()
//     {
//         var obj = Selection.activeObject;
//         if (obj == null)
//             return "Assets";

//         var path = AssetDatabase.GetAssetPath(obj);
//         if (!string.IsNullOrEmpty(path) && System.IO.Directory.Exists(path))
//             return path;

//         if (!string.IsNullOrEmpty(path) && System.IO.Path.GetExtension(path) == "")
//             return path;

//         var folder = System.IO.Path.GetDirectoryName(path);
//         return string.IsNullOrEmpty(folder) ? "Assets" : folder;
//     }

//     private static GameObject CreateIngredientPlot(Transform parent, Vector3 position, IngredientType ingredientType, RestaurantManager manager)
//     {
//         var plotRoot = GameObject.CreatePrimitive(PrimitiveType.Cube);
//         plotRoot.name = "IngredientPlot_" + ingredientType.ToString();
//         plotRoot.transform.SetParent(parent);
//         plotRoot.transform.localPosition = position;
//         plotRoot.transform.localScale = new Vector3(1.2f, 0.2f, 1.2f);

//         var renderer = plotRoot.GetComponent<Renderer>();
//         renderer.material = new Material(Shader.Find("Standard"));
//         renderer.material.color = new Color(0.2f, 0.65f, 0.25f);

//         var boxCollider = plotRoot.GetComponent<BoxCollider>();
//         if (boxCollider != null)
//         {
//             boxCollider.isTrigger = false;
//         }

//         var plot = plotRoot.AddComponent<IngredientPlot>();
//         SetPrivateField(plot, "ingredientType", ingredientType);
//         SetPrivateField(plot, "harvestYield", 3);
//         SetPrivateField(plot, "growthDuration", 30f);
//         SetPrivateField(plot, "autoStartOnEnable", true);

//         var interactable = plotRoot.AddComponent<IngredientPlotInteractable>();
//         SetPrivateField(interactable, "restaurantManager", manager);

//         // plant visual
//         var plant = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
//         plant.name = "PlantVisual";
//         plant.transform.SetParent(plotRoot.transform);
//         plant.transform.localPosition = new Vector3(0f, 0.9f, 0f);
//         plant.transform.localScale = new Vector3(0.35f, 0.8f, 0.35f);

//         var plantRenderer = plant.GetComponent<Renderer>();
//         plantRenderer.material = new Material(Shader.Find("Standard"));
//         plantRenderer.material.color = new Color(0.1f, 0.7f, 0.2f);

//         plotRoot.AddComponent<IngredientPlotVisual>();

//         return plotRoot;
//     }

//     private static void CreatePlotTimer(GameObject plotObject, IngredientType ingredientType)
//     {
//         var timerTextObject = new GameObject("TimerText");
//         timerTextObject.transform.SetParent(plotObject.transform);
//         timerTextObject.transform.localPosition = new Vector3(0f, 1.8f, 0f);

//         var rect = timerTextObject.AddComponent<RectTransform>();
//         rect.sizeDelta = new Vector2(200f, 50f);

//         var canvas = timerTextObject.AddComponent<Canvas>();
//         canvas.renderMode = RenderMode.WorldSpace;
//         canvas.pixelPerfect = false;

//         var scaler = timerTextObject.AddComponent<CanvasScaler>();
//         scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
//         scaler.referenceResolution = new Vector2(1920, 1080);

//         var text = timerTextObject.AddComponent<TextMeshProUGUI>();
//         text.text = "30";
//         text.fontSize = 28;
//         text.alignment = TextAlignmentOptions.Center;
//         text.color = Color.white;
//         text.enableAutoSizing = false;

//         var timer = plotObject.AddComponent<GrowthTimer>();
//         SetPrivateField(timer, "plot", plotObject.GetComponent<IngredientPlot>());
//         SetPrivateField(timer, "timerText", text);
//         SetPrivateField(timer, "readyText", "Ready");
//     }

//     private static GameObject CreateInventoryCanvas(Transform parent, RestaurantManager manager)
//     {
//         var canvasObject = new GameObject("RestaurantUI");
//         canvasObject.transform.SetParent(parent);
//         canvasObject.transform.localPosition = Vector3.zero;

//         var canvas = canvasObject.AddComponent<Canvas>();
//         canvas.renderMode = RenderMode.ScreenSpaceOverlay;

//         var scaler = canvasObject.AddComponent<CanvasScaler>();
//         scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
//         scaler.referenceResolution = new Vector2(1920, 1080);

//         canvasObject.AddComponent<GraphicRaycaster>();

//         var inventoryUi = canvasObject.AddComponent<RestaurantInventoryUI>();
//         SetPrivateField(inventoryUi, "restaurantManager", manager);

//         var panel = new GameObject("InventoryPanel");
//         panel.transform.SetParent(canvasObject.transform);

//         var panelRect = panel.AddComponent<RectTransform>();
//         panelRect.anchorMin = new Vector2(0f, 1f);
//         panelRect.anchorMax = new Vector2(0f, 1f);
//         panelRect.pivot = new Vector2(0f, 1f);
//         panelRect.sizeDelta = new Vector2(420f, 280f);
//         panelRect.anchoredPosition = new Vector2(30f, -30f);

//         var panelImage = panel.AddComponent<Image>();
//         panelImage.color = new Color(0.1f, 0.1f, 0.12f, 0.85f);

//         var title = CreateText(panel.transform, "Restaurant Inventory", new Vector2(0f, 0f), new Vector2(360f, 40f), 24, TextAnchor.MiddleLeft);
//         var titleRect = title.GetComponent<RectTransform>();
//         titleRect.anchoredPosition = new Vector2(20f, -20f);

//         var entries = new[]
//         {
//             new { key = "Tomato", type = IngredientType.Tomato, y = -60f },
//             new { key = "Onion", type = IngredientType.Onion, y = -100f },
//             new { key = "Lettuce", type = IngredientType.Lettuce, y = -140f },
//             new { key = "Herb", type = IngredientType.Herb, y = -180f },
//             new { key = "Flour", type = IngredientType.Flour, y = -220f },
//             new { key = "Rice", type = IngredientType.Rice, y = -260f },
//         };

//         var fieldMap = new Dictionary<string, TMP_Text>();

//         foreach (var item in entries)
//         {
//             var textObject = CreateText(panel.transform, item.key + ": 0", new Vector2(0f, 0f), new Vector2(300f, 32f), 18, TextAnchor.MiddleLeft);
//             var rect = textObject.GetComponent<RectTransform>();
//             rect.anchoredPosition = new Vector2(20f, item.y);

//             fieldMap[item.key] = textObject.GetComponent<TMP_Text>();
//         }

//         SetPrivateField(inventoryUi, "tomatoText", fieldMap["Tomato"]);
//         SetPrivateField(inventoryUi, "onionText", fieldMap["Onion"]);
//         SetPrivateField(inventoryUi, "lettuceText", fieldMap["Lettuce"]);
//         SetPrivateField(inventoryUi, "herbText", fieldMap["Herb"]);
//         SetPrivateField(inventoryUi, "flourText", fieldMap["Flour"]);
//         SetPrivateField(inventoryUi, "riceText", fieldMap["Rice"]);

//         return canvasObject;
//     }

//     private static GameObject CreateText(Transform parent, string text, Vector2 anchorMin, Vector2 size, int fontSize, TextAnchor alignment)
//     {
//         var obj = new GameObject(text.Replace(" ", "") + "_Text");
//         obj.transform.SetParent(parent);

//         var rect = obj.AddComponent<RectTransform>();
//         rect.anchorMin = anchorMin;
//         rect.anchorMax = new Vector2(anchorMin.x + 1f, anchorMin.y + 1f);
//         rect.sizeDelta = size;

//         var tmp = obj.AddComponent<TextMeshProUGUI>();
//         tmp.text = text;
//         tmp.fontSize = fontSize;
//         tmp.alignment = ConvertAlignment(alignment);
//         tmp.color = Color.white;
//         tmp.raycastTarget = false;

//         return obj;
//     }

//     private static TextAlignmentOptions ConvertAlignment(TextAnchor anchor)
//     {
//         switch (anchor)
//         {
//             case TextAnchor.UpperLeft:
//             case TextAnchor.MiddleLeft:
//             case TextAnchor.LowerLeft:
//                 return TextAlignmentOptions.Left;

//             case TextAnchor.UpperCenter:
//             case TextAnchor.MiddleCenter:
//             case TextAnchor.LowerCenter:
//                 return TextAlignmentOptions.Center;

//             case TextAnchor.UpperRight:
//             case TextAnchor.MiddleRight:
//             case TextAnchor.LowerRight:
//                 return TextAlignmentOptions.Right;

//             default:
//                 return TextAlignmentOptions.Left;
//         }
//     }

//     private static void SetPrivateField(object target, string fieldName, object value)
//     {
//         var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
//         if (field != null)
//         {
//             field.SetValue(target, value);
//             return;
//         }

//         var prop = target.GetType().GetProperty(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
//         if (prop != null && prop.CanWrite)
//         {
//             prop.SetValue(target, value);
//         }
//     }
// }
