﻿using System;
using System.Collections.Generic;
using System.Linq;

public class Storage
{
    protected List<ItemSlot> itemSlots = new();
    public Action<int, ItemSlot> onAddItem;

    public Action<int, ItemSlot> onChangeSlot;

    public Storage() {}
    public Storage(int size)
    {
        for (int i = 0; i < size; i++)
            CreateSlot();
    }

    public int Count => itemSlots.Count; 

    public void AddItemSlot(ItemSlot itemSlot)
    {
        itemSlots.Add(itemSlot);
    }

    public ItemSlot GetSlotByIdx(int idx)
    {
        if (idx < 0 || idx >= itemSlots.Count)
            return null;

        return itemSlots[idx];
    }

    public List<ItemSlot> GetSlotsByCondition(Func<ItemSlot, bool> condition)
    {
        return itemSlots.Where(condition).ToList(); 
    }

    public ItemSlot CreateSlot()
    {
        ItemSlot slot = new ItemSlot(Count);  
        slot.onAddItem += (id, amount) => onAddItem?.Invoke(id, amount);
        itemSlots.Add(slot);
        return slot; 
    }


    public bool AddItem(int idx, int amount, int durability = 0)
    {
        if (idx < 0 || idx >= itemSlots.Count)
            return false;

        ItemSlot slot = itemSlots[idx];
        return AddItem(slot, amount, durability);  
    }

    public bool AddItem(ItemData itemData, int amount = 1, int durability = 0)
    {
        ItemSlot slot = FindSlotByItemData(itemData, amount); 
        if (slot == null)
        {
            slot = FindFirstEmptySlot();
            if (slot == null)
                return false;

            slot.Setup(itemData); 
        }
        
        return AddItem(slot, amount, durability); 
    }

    public bool AddItem(ItemSlot slot, int amount = 1, int durability = 0)
    {
        if (slot.AddStack(slot.Data, amount, durability))
        {
    
            return true;
        }
        return false;
    }

    public bool RemoveItem(int idx, int amount = 1)
    {
        return AddItem(idx, -amount);
    }

    public bool RemoveItem(ItemData data, int amount = 1)
    {
        return AddItem(data, -amount);
    }

    public bool RemoveItem(ItemSlot slot, int amount = 1)
    {
        return AddItem(slot, -amount);
    }

    public ItemSlot FindSlotByItemData(ItemData itemData, int amount = 1)
    {
        if (itemData.CanStack == false)
            return null;

        return itemSlots.Find(x => x.Data != null && x.Data.Id == itemData.Id && x.IsAddable(amount));   
    }

    public ItemSlot FindFirstEmptySlot()
    {
        return itemSlots.Find(x => x.Data == null || x.Stack == 0);  
    }

    public bool HasEmptySlot()
    {
        return itemSlots.Find(x => x.Data == null || x.Stack == 0) != null; 
    }
}
