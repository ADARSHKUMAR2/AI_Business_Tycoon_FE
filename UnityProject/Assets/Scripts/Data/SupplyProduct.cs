using System;

namespace AIBusinessTycoon.Data
{
    public enum SupplyProduct
    {
        Unknown,
        Rice,
        Dal,
        Salt,
        Sugar,
        Oil,
        Flour,
        Tea,
        Tomato,
        Onion,
        Lettuce,
        Herb,
        Buns,
        Patty,
        Cheese,
        Dough,
        Sauce,
        Pepperoni, 
        Milk, 
        Bread
    }

    public static class SupplyProductExtensions
    {
        public static SupplyProduct ToSupplyProduct(this string itemKey)
        {
            if (string.IsNullOrWhiteSpace(itemKey))
                return SupplyProduct.Unknown;

            string normalizedKey = itemKey.Trim().ToLowerInvariant();
            return normalizedKey switch
            {
                "rice" => SupplyProduct.Rice,
                "dal" => SupplyProduct.Dal,
                "salt" => SupplyProduct.Salt,
                "sugar" => SupplyProduct.Sugar,
                "oil" => SupplyProduct.Oil,
                "flour" => SupplyProduct.Flour,
                "tea" => SupplyProduct.Tea,
                "tomato" => SupplyProduct.Tomato,
                "onion" => SupplyProduct.Onion,
                "lettuce" => SupplyProduct.Lettuce,
                "herb" => SupplyProduct.Herb,
                "buns" => SupplyProduct.Buns,
                "patty" => SupplyProduct.Patty,
                "cheese" => SupplyProduct.Cheese,
                "dough" => SupplyProduct.Dough,
                "sauce" => SupplyProduct.Sauce,
                "pepperoni" => SupplyProduct.Pepperoni,
                "milk" => SupplyProduct.Milk,
                "bread" => SupplyProduct.Bread,
                _ => SupplyProduct.Unknown
            };
        }
    }
}