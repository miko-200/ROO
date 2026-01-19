using System;
using System.IO;
using Cinemachine;
using UnityEngine;

public class SaveController : MonoBehaviour
{
    
    private string saveLocation;
    private InventoryController inventoryController;
    private HotbarController hotbarController;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Define save location
        saveLocation = Path.Combine(Application.persistentDataPath, "saveData.json");
        Debug.Log("Save location: " + saveLocation);
        inventoryController = FindObjectOfType<InventoryController>();
        hotbarController = FindObjectOfType<HotbarController>();
        
        LoadGame();
    }

    public void SaveGame()
    {
        SaveData saveData = new SaveData
        {
            playerPosition = GameObject.FindGameObjectWithTag("Player").transform.position,
            mapBoundary = FindObjectOfType<CinemachineConfiner>().m_BoundingShape2D.gameObject.name,
            inventorySaveData = inventoryController.GetInventoryItems(),
            hotabrSaveData = hotbarController.GetHotbarItems()
        };
        
        File.WriteAllText(saveLocation, JsonUtility.ToJson(saveData));
        Debug.Log("Data saved successfully!");
    }

    public void LoadGame()
    {
        if (File.Exists(saveLocation))
        {
            SaveData saveData = JsonUtility.FromJson<SaveData>(File.ReadAllText(saveLocation));
            
            GameObject.FindGameObjectWithTag("Player").transform.position = saveData.playerPosition;
            
            FindObjectOfType<CinemachineConfiner>().m_BoundingShape2D = GameObject.Find(saveData.mapBoundary).GetComponent<PolygonCollider2D>();
            
            MapController_Manual.Instance?.HighlightArea(saveData.mapBoundary);
            
            inventoryController.SetInventoryItems(saveData.inventorySaveData);
            hotbarController.SetHotbarItems(saveData.hotabrSaveData);
        }
        else
        {
            SaveGame();
            LoadGame();
        }
    }
}
