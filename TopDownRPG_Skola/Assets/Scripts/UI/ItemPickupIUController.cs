using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemPickupIUController : MonoBehaviour
{
    public static ItemPickupIUController Instance { get; private set; }
    
    public GameObject popupPrefab;
    public int maxPopups = 5;
    public float popupDuration = 1.5f;

    public readonly Queue<GameObject> activePopups = new();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple instances of ItemPickupIUController! Destroying the extra one.");
            Destroy(gameObject);
        }
    }

    public void ShowItemPickup(string itemName, Sprite itemIcon)
    {
        GameObject newPopup = Instantiate(popupPrefab, transform);
        newPopup.GetComponentInChildren<TMP_Text>().text = itemName;
        
        Image itemImage = newPopup.transform.Find("ItemIcon")?.GetComponent<Image>();
    }
}
