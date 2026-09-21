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
                        Ingredients = new List<string> { "bread", "tomato", "onion" },
                        OutputItemName = "burger"
                    }
                },
                {
                    RecipeType.Pizza,
                    new RecipeDefinition
                    {
                        Type = RecipeType.Pizza,
                        Ingredients = new List<string> { "flour", "tomato", "onion", "herbs" },
                        OutputItemName = "pizza"
                    }
                }
            };
    }
}
