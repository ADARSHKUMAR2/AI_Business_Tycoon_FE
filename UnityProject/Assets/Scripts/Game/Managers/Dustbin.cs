using UnityEngine;

namespace AIBusinessTycoon.Managers
{
    public class Dustbin : MonoBehaviour
    {
        public bool TryDiscardItem(PlayerInventory inventory)
        {
            if (inventory == null || !inventory.HasItem())
            {
                Debug.Log("Your hands are already empty!");
                return false;
            }

            string discardedItem = inventory.DropHeldItem();
            Debug.Log($"Threw away {discardedItem} into the dustbin!");
            return true;
        }
    }
}
