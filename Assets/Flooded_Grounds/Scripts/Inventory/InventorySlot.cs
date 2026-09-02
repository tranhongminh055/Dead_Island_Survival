using System;

namespace HorrorGame.Inventory
{
    [Serializable]
    public class InventorySlot
    {
        public ItemData item;
        public int amount;

        public InventorySlot(ItemData item, int amount)
        {
            this.item = item;
            this.amount = amount;
        }

        public void AddAmount(int value)
        {
            amount += value;
        }

        public void RemoveAmount(int value)
        {
            amount -= value;
            if (amount <= 0)
            {
                ClearSlot();
            }
        }

        public void ClearSlot()
        {
            item = null;
            amount = 0;
        }

        public bool IsEmpty()
        {
            return item == null;
        }
    }
}
