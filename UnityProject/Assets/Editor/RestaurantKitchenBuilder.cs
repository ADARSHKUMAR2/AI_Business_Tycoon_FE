// using UnityEditor;
// using UnityEngine;

// public class RestaurantKitchenBuilder : EditorWindow
// {
//     [MenuItem("Tools/Restaurant/Create Kitchen")]
//     public static void CreateKitchen()
//     {
//         // Create root parent
//         GameObject kitchenRoot = GameObject.Find("RestaurantKitchen");
//         if (kitchenRoot != null)
//         {
//             Undo.DestroyObjectImmediate(kitchenRoot);
//         }

//         kitchenRoot = new GameObject("RestaurantKitchen");
//         Undo.RegisterCreatedObjectUndo(kitchenRoot, "Create Restaurant Kitchen");

//         // Create burger area
//         GameObject burgerArea = new GameObject("BurgerArea");
//         burgerArea.transform.SetParent(kitchenRoot.transform);

//         CreateShelf(burgerArea, "BurgerBreadShelf", new Vector3(-4f, 0f, 0f), "bread");
//         CreateShelf(burgerArea, "BurgerTomatoShelf", new Vector3(-2f, 0f, 0f), "tomato");
//         CreateShelf(burgerArea, "BurgerOnionShelf", new Vector3(0f, 0f, 0f), "onion");

//         GameObject burgerTable = CreateTable(burgerArea, "BurgerTable", new Vector3(3f, 0f, 0f), RecipeType.Burger);

//         // Create pizza area
//         GameObject pizzaArea = new GameObject("PizzaArea");
//         pizzaArea.transform.SetParent(kitchenRoot.transform);

//         CreateShelf(pizzaArea, "PizzaFlourShelf", new Vector3(-4f, 0f, 2.5f), "flour");
//         CreateShelf(pizzaArea, "PizzaTomatoShelf", new Vector3(-2f, 0f, 2.5f), "tomato");
//         CreateShelf(pizzaArea, "PizzaOnionShelf", new Vector3(0f, 0f, 2.5f), "onion");
//         CreateShelf(pizzaArea, "PizzaHerbsShelf", new Vector3(2f, 0f, 2.5f), "herbs");

//         GameObject pizzaTable = CreateTable(pizzaArea, "PizzaTable", new Vector3(5f, 0f, 2.5f), RecipeType.Pizza);

//         // Optional neat visuals
//         CreateTableVisuals(burgerTable, Color.green);
//         CreateTableVisuals(pizzaTable, Color.cyan);

//         Selection.activeGameObject = kitchenRoot;
//         Debug.Log("Restaurant kitchen generated successfully.");
//     }

//     private static GameObject CreateShelf(GameObject parent, string name, Vector3 position, string ingredientId)
//     {
//         GameObject shelf = new GameObject(name);
//         shelf.transform.SetParent(parent.transform);
//         shelf.transform.position = position;

//         // Add basic shelf visuals
//         var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
//         cube.transform.SetParent(shelf.transform);
//         cube.transform.localPosition = Vector3.zero;
//         cube.transform.localScale = new Vector3(1.2f, 0.8f, 1.2f);

//         // Add item label for easier debug visual
//         var label = new GameObject("IngredientLabel");
//         label.transform.SetParent(shelf.transform);
//         label.transform.localPosition = new Vector3(0f, 1.2f, 0f);

//         var text = label.AddComponent<TextMesh>();
//         text.text = ingredientId;
//         text.characterSize = 0.1f;
//         text.fontSize = 40;
//         text.anchor = TextAnchor.MiddleCenter;

//         // Add our actual shelf component
//         IngredientShelf ingredientShelf = shelf.AddComponent<IngredientShelf>();
//         ingredientShelf.SetIngredientId(ingredientId);

//         Undo.RegisterCreatedObjectUndo(shelf, "Create Ingredient Shelf");
//         return shelf;
//     }

//     private static GameObject CreateTable(GameObject parent, string name, Vector3 position, RecipeType type)
//     {
//         GameObject table = new GameObject(name);
//         table.transform.SetParent(parent.transform);
//         table.transform.position = position;

//         var baseCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
//         baseCube.transform.SetParent(table.transform);
//         baseCube.transform.localPosition = new Vector3(0f, 0.2f, 0f);
//         baseCube.transform.localScale = new Vector3(2.2f, 0.5f, 2.2f);

//         var topCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
//         topCube.transform.SetParent(table.transform);
//         topCube.transform.localPosition = new Vector3(0f, 1.2f, 0f);
//         topCube.transform.localScale = new Vector3(1.8f, 0.2f, 1.8f);

//         CraftingTable craftingTable = table.AddComponent<CraftingTable>();
//         SetRecipeType(craftingTable, type);

//         Undo.RegisterCreatedObjectUndo(table, "Create Crafting Table");
//         return table;
//     }

//     private static void CreateTableVisuals(GameObject table, Color color)
//     {
//         Renderer[] renderers = table.GetComponentsInChildren<Renderer>();
//         foreach (Renderer renderer in renderers)
//         {
//             Material mat = new Material(Shader.Find("Standard"));
//             mat.color = color;
//             renderer.sharedMaterial = mat;
//         }
//     }

//     private static void SetRecipeType(CraftingTable table, RecipeType type)
//     {
//         SerializedObject so = new SerializedObject(table);
//         SerializedProperty prop = so.FindProperty("recipeType");
//         if (prop != null)
//         {
//             prop.enumValueIndex = (int)type;
//             so.ApplyModifiedProperties();
//         }
//     }
// }
