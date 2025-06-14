using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;


public class InventoryDataHandler
{
    public static Action onItemAmountUpdate;
    public static Dictionary<int, HashSet<ItemSlot>> itemAmounts {get; private set;} = new Dictionary<int, HashSet<ItemSlot>>();

    public static int GetItemAmount(int id)
    {
        int ret = 0;

        if (itemAmounts.ContainsKey(id))
        {
            itemAmounts[id].ToList().ForEach(slot => {
                if (slot.Data != null && slot.Data.Id == id)
                    ret += slot.Stack;
                else
                    itemAmounts[id].Remove(slot); 
            });
        }
            
        return ret;  
    }

    public void ItemAmountUpdate(int id, ItemSlot itemSlot)
    {
        if (itemAmounts.ContainsKey(id))
        {
            if (itemSlot.Stack == 0)
                itemAmounts[id].Remove(itemSlot);
            else
                itemAmounts[id].Add(itemSlot);
        }
        else if (itemSlot.Stack > 0)
            itemAmounts[id] = new HashSet<ItemSlot> {itemSlot}; 

        onItemAmountUpdate?.Invoke();
        Debug.Log($"ItemAmountUpdate: {id} {GetItemAmount(id)}"); 
    }

    public void RemoveItem(int id, int amount)
    {
        RemoveItem(Managers.Data.items.GetByIndex(id), amount);
    }

    public void RemoveItem(ItemData itemData, int amount)
    {
        if (!itemAmounts.ContainsKey(itemData.Id) || itemAmounts[itemData.Id].Sum(slot => slot.Stack) < amount)
            return;

        foreach (var slot in itemAmounts[itemData.Id].ToList())
        {
            if (amount <= 0)
                break;

            int diff = slot.Stack - amount;
            if (diff >= 0)
            {
                slot.Setup(itemData, diff);
                amount = 0;
            }
            else 
            { 
                amount -= slot.Stack;
                slot.Setup(null, 0);
            } 
        } 
    }

}
