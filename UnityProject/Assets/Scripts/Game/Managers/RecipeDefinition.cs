using System.Collections.Generic;

namespace AIBusinessTycoon.Managers
{
    public enum RecipeType
    {
        Burger,
        Pizza
    }

    public class RecipeDefinition
    {
        public RecipeType Type;
        public List<string> Ingredients;
        public string OutputItemName;
    }

    public static class RecipeDatabase
    {
        public static readonly Dictionary<RecipeType, RecipeDefinition> Recipes =
            new Dictionary<RecipeType, RecipeDefinition>
            {
                {
                    RecipeType.Burger,
                    new RecipeDefinition
                    {
                        Type = RecipeType.Burger,
                        // Kitchen items + Farm items (Must be completely lowercase!)
                        Ingredients = new List<string> { "buns", "patty", "cheese", "tomato", "lettuce" },
                        OutputItemName = "burger"
                    }
                },
                {
                    RecipeType.Pizza,
                    new RecipeDefinition
                    {
                        Type = RecipeType.Pizza,
                        // Kitchen items + Farm items
                        Ingredients = new List<string> { "dough", "sauce", "pepperoni", "flour", "herb", "onion" },
                        OutputItemName = "pizza"
                    }
                }
            };
    }
}
