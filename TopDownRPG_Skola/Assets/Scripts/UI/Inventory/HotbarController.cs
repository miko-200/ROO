using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class HotbarController : MonoBehaviour
{
    public GameObject hotbarPanel;
    public GameObject slotPrefab;
    public int slotCount = 10;
    
    private ItemDictionary itemDictionary;
    
    private Key[] hotbarKeys;

    private void Awake()
    {
        itemDictionary = FindObjectOfType<ItemDictionary>();
        
        hotbarKeys = new Key[slotCount];

        for (int i = 0; i < slotCount; i++)
        {
            hotbarKeys[i] = i < 9 ? (Key)((int)Key.Digit1 + i) : Key.Digit0;
        }
    }

    private void Update()
    {
        //Check for key presses
        for (int i = 0; i < slotCount; i++)
        {
            if (Keyboard.current[hotbarKeys[i]].wasPressedThisFrame)
            {
                UseItemInSlot(i);
            }
        }
    }

    private void UseItemInSlot(int index)
    {
        Slot slot = hotbarPanel.transform.GetChild(index).GetComponent<Slot>();
        if (slot.currentItem != null)
        {
            Item item = slot.currentItem.GetComponent<Item>();
            item.UseItem();
        }
    }
    
    public List<InventorySaveData> GetHotbarItems()
    {
        List<InventorySaveData> hotbarSaveData = new List<InventorySaveData>();
        foreach (Transform slotTransform in hotbarPanel.transform)
        {
            Slot slot = slotTransform.GetComponent<Slot>();
            if (slot.currentItem != null)
            {
                Item item = slot.currentItem.GetComponent<Item>();
                hotbarSaveData.Add(new InventorySaveData{itemID = item.ID, slotIndex = slotTransform.GetSiblingIndex()});
            }
        }
        return hotbarSaveData;
    }

    public void SetHotbarItems(List<InventorySaveData> inventorySaveData)
    {
        //Clear inventory panel - avoid duplicates
        foreach (Transform child in hotbarPanel.transform)
        {
            Destroy(child.gameObject);
        }
        
        //Create new slots
        for (int i = 0; i < slotCount; i++)
        {
            Instantiate(slotPrefab, hotbarPanel.transform);
        }
        
        //Populate slots with saved items
        foreach (InventorySaveData data in inventorySaveData)
        {
            if (data.slotIndex < slotCount)
            {
                Slot slot = hotbarPanel.transform.GetChild(data.slotIndex).GetComponent<Slot>();
                GameObject itemPrefab = itemDictionary.GetItemPrefab(data.itemID);
                if (itemPrefab != null)
                {
                    GameObject item = Instantiate(itemPrefab, slot.transform);
                    item.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                    slot.currentItem = item;
                }
            }
        }
    }
}
