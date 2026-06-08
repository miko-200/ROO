using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // Add this for TextMeshProUGUI

public class ItemPathData : MonoBehaviour
{
    public string itemName;
    public Image ItemImage;
    public Button button;
    
    public string itemDescription; // This will hold the descriptive text

    // New properties
    public int numberOfEntranceTiles;
    public int numberOfPossibleRoutes;
    public PathData associatedPathData; // Reference to the actual PathData ScriptableObject

    private void Start()
    {
        // Ensure button is assigned, especially if not set in Inspector
        if (button == null)
        {
            button = GetComponentInChildren<Button>();
        }
    }

    // New method to set up the UI item
    public void Setup(PathData path, int entranceTiles, int possibleRoutes)
    {
        associatedPathData = path;
        itemName = path.tilemapName; // Assuming tilemapName is the display name
        numberOfEntranceTiles = entranceTiles;
        numberOfPossibleRoutes = possibleRoutes;

        // Set the itemDescription
        itemDescription = $"Entrances: {numberOfEntranceTiles}, Routes: {numberOfPossibleRoutes}";

        // Update UI elements if they exist
        // Find the TextMeshProUGUI for the item name
        var tmpNameText = GetComponentsInChildren<TextMeshProUGUI>().FirstOrDefault(t => t.name == "ItemNameText"); // Assuming a TextMeshProUGUI named "ItemNameText"
        if (tmpNameText != null) {
            tmpNameText.text = itemName;
        } else {
            var standardNameText = GetComponentsInChildren<UnityEngine.UI.Text>().FirstOrDefault(t => t.name == "ItemNameText"); // Assuming a Text named "ItemNameText"
            if (standardNameText != null) standardNameText.text = itemName;
        }

        // Find the TextMeshProUGUI for the item description
        var tmpDescText = GetComponentsInChildren<TextMeshProUGUI>().FirstOrDefault(t => t.name == "ItemDescriptionText"); // Assuming a TextMeshProUGUI named "ItemDescriptionText"
        if (tmpDescText != null) {
            tmpDescText.text = itemDescription;
        } else {
            var standardDescText = GetComponentsInChildren<UnityEngine.UI.Text>().FirstOrDefault(t => t.name == "ItemDescriptionText"); // Assuming a Text named "ItemDescriptionText"
            if (standardDescText != null) standardDescText.text = itemDescription;
        }

        // Hook up the button
        if (button != null) {
            // Clear existing listeners to prevent duplicates
            button.onClick.RemoveAllListeners(); 
            // The actual action will be assigned by PathManager
        }
    }
}
