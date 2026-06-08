using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.IO;
using System.Linq;
using UnityEngine.InputSystem;
using UnityEngine.UI; // Added for Button
using UnityEngine.EventSystems; // NEW: Added for UI event checking
using TMPro; // NEW: Added for TextMeshProUGUI

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PathManager : MonoBehaviour
{
    [Header("References")]
    public PathPainter painter;
    public Grid grid; // The parent Grid object
    public MapManager mapManager;
    
    [Header("Visual Merging")]
    public Tilemap masterTilemap; // Create a new Tilemap in your Grid for this

    [Header("Settings")]
    public string folderName = "MapPaths";
    public float ghostAlpha = 0.2f;

    [Header("Active State")]
    public List<PathData> allMapPaths = new List<PathData>();
    private PathData currentActivePath;
    private GameObject currentTilemapObj;
    private PathData selectedPath; // NEW: Stores the currently selected path (not necessarily being edited)

    [Header("UI Interactions")]
    public GameObject CreateNewPathButton;
    public GameObject SavePathButton;
    public GameObject DeletePathButton; // This is likely the button for the *active* path, not selected.
    
    [Header("UI List")]
    // Assign your 'Content' object here
    public Transform pathUIContainer;     
    // Assign your 'Item' prefab here
    public GameObject pathItemPrefab;
    public GameObject mainSelectionContainer;

    [Header("Selected Path UI")] // NEW: UI elements for the selected path card
    public GameObject pathInfoCard; // The UI panel/card that displays selected path info
    public Button editSelectedPathButton; // Button on the card to enter edit mode
    public Button deleteSelectedPathBtn; // Button on the card to delete the selected path

    // NEW: Text fields for the info card
    public TextMeshProUGUI pathNameText;
    public TextMeshProUGUI pathDescriptionText;
    public TextMeshProUGUI numRoutesText;
    public TextMeshProUGUI numEntrancesText;
    public TextMeshProUGUI numExitsText;


    private void Start()
    {
        ClearSelection();
        // Hook up the main DeletePathButton if it's meant for the currentActivePath
        if (DeletePathButton != null)
        {
            Button btn = DeletePathButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(DeleteCurrentPath);
            }
        }
    }

    public void RefreshPathUI()
    {
        if (pathUIContainer == null || pathItemPrefab == null) return;
        Debug.Log("Refreshing UI List");
        // 1. Clear existing items
        foreach (Transform child in pathUIContainer) {
            Destroy(child.gameObject);
        }

        // 2. Spawn an Item for every saved path
        foreach (PathData path in allMapPaths)
        {
            GameObject itemObj = Instantiate(pathItemPrefab, pathUIContainer);
            ItemPathData itemPathData = itemObj.GetComponent<ItemPathData>();

            if (itemPathData != null)
            {
                // Calculate new properties
                int entranceTilesCount = path.entranceTiles != null ? path.entranceTiles.Count : 0;
                int possibleRoutesCount = path.subpathRoutes != null ? path.subpathRoutes.Count : 0;

                // 3. Use the new Setup method
                itemPathData.Setup(path, entranceTilesCount, possibleRoutesCount);

                // 4. Hook up the Button to SELECT the path, not edit it directly
                if (itemPathData.button != null) {
                    itemPathData.button.onClick.RemoveAllListeners(); // Clear existing listeners
                    itemPathData.button.onClick.AddListener(() => SelectPath(path)); // Changed to SelectPath
                }
            }
            else
            {
                Debug.LogWarning("PathItemPrefab does not have an ItemPathData component.");
            }
        }
    }
    
    void Update()
    {
        // If we are NOT painting, listen for clicks to select paths
        if (!painter.enabled && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TrySelectPathFromScene();
        }

        // NEW: If painter is enabled (in edit mode), continuously update outlines
        if (painter.enabled && currentActivePath != null)
        {
            painter.DrawPathOutlines(currentActivePath, true);
        }
    }
    
    private void TrySelectPathFromScene()
    {
        // NEW: Check if the pointer is over a UI element
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return; // Do not process world-space clicks if UI is being interacted with
        }

        Vector3 mousePos = Mouse.current.position.ReadValue();
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, Camera.main.nearClipPlane));
        worldPos.z = 0;
        
        Vector3Int cellPos = masterTilemap.WorldToCell(worldPos);
        if (mapManager != null)
        {
            if (cellPos.x < mapManager.mapBounds.xMin || cellPos.x > mapManager.mapBounds.xMax ||
                cellPos.y < mapManager.mapBounds.yMin || cellPos.y > mapManager.mapBounds.yMax)
            {
                ClearSelection(); // Clear selection if click is outside map bounds
                return;
            }
        }

        // Find which path(s) contain this clicked cell
        List<PathData> clickedPaths = new List<PathData>();
        foreach (var path in allMapPaths)
        {
            // Using LINQ to check if any saved connection matches the clicked position
            if (path.savedConnections.Any(c => c.pos == cellPos)) {
                clickedPaths.Add(path);
            }
        }

        if (clickedPaths.Count > 0) {
            // If they click an intersection, we just grab the first one. 
            // They can use the UI list if they meant to grab the underlying one.
            SelectPath(clickedPaths[0]); // Changed to SelectPath
        } else {
            ClearSelection(); // NEW: Clear selection if no path is clicked
        }
    }

    // NEW: Method to select a path and refresh UI
    public void SelectPath(PathData path)
    {
        if (painter.enabled) return; // Cannot select while editing

        selectedPath = path;
        RefreshSelectedPathUI();
        painter.DrawPathOutlines(selectedPath, false); // Draw outlines for selected path
    }

    // NEW: Method to clear the current path selection
    public void ClearSelection()
    {
        selectedPath = null;
        RefreshSelectedPathUI();
        painter.ClearPathOutlines(); // Clear any drawn outlines
    }

    // NEW: Method to enter edit mode from the selected path
    public void EnterEditModeFromSelection()
    {
        if (selectedPath == null) return;
        EditExistingPath(selectedPath);
        // The EditExistingPath method will handle calling painter.DrawPathOutlines(currentActivePath, true);
    }
    

    public void TogglePainter(bool isOn)
    {
        if (isOn) StartNewPath();
        else BakeAndExit();
    }

    public void StartNewPath()
    {
        Debug.Log("Starting new path");
        
        ClearSelection(); // NEW: Clear any existing selection when starting a new path
        
        SwitchButtons();
        
        // 1. Create Folder
        string folderPath = "Assets/Assets/" + folderName;
        if (!Directory.Exists(folderPath)) {
            Directory.CreateDirectory(folderPath);
            Debug.Log("Folder created in: " + folderPath);
#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
        }
        
        // 2. Create the PathData File
        Debug.Log("Creating new path file");
        PathData newData = ScriptableObject.CreateInstance<PathData>();
        int count = allMapPaths.Count + 1;
        string fileName = $"Path_Route_{count}";
        Debug.Log("Path file created");
        
#if UNITY_EDITOR
        AssetDatabase.CreateAsset(newData, $"{folderPath}/{fileName}.asset");
        AssetDatabase.SaveAssets();
#endif

        // 3. Create the Tilemap for this path
        Debug.Log("Creating a new Tilemap for this path");
        GameObject go = new GameObject(fileName);

// CRITICAL FIX: Pass 'false' as the second argument. 
// This tells Unity to match the parent's local coordinate system exactly, 
// instead of trying to maintain its world position.
        go.transform.SetParent(grid.transform, false);

        Tilemap tm = go.AddComponent<Tilemap>();
        go.AddComponent<TilemapRenderer>();

        currentTilemapObj = go;
        currentActivePath = newData;
        newData.tilemapName = fileName;
        Debug.Log("Tilemap created cleanly aligned with Grid space.");

        // 4. Setup Painter
        Debug.Log("Setting up Painter");
        painter.activePath = newData;
        painter.mainTilemap = tm;
        painter.enabled = true;
        painter.LoadFromActivePath();

        UpdateGhosting();
        // painter.DrawPathOutlines(currentActivePath, true); // Moved to Update() for continuous refresh
        Debug.Log("Painter ready to use");
    }

    private void SwitchButtons()
    {
        if (CreateNewPathButton != null && SavePathButton != null && DeletePathButton != null)
        {
            if (!SavePathButton.activeSelf)
            {
                Debug.Log(SavePathButton.activeSelf);
                SavePathButton.SetActive(true);
                DeletePathButton.SetActive(true);
                CreateNewPathButton.SetActive(false);
            }
            else
            {
                SavePathButton.SetActive(false);
                DeletePathButton.SetActive(false);
                CreateNewPathButton.SetActive(true);
            }

            Debug.Log("Buttons Switched");
        }
        else
        {
            Debug.LogError("Some button is not assigned!");
        }
    }

    public void BakeAndExit()
    {
        if (currentActivePath == null) return;
    
        // IMPORTANT: Make sure the painter saves its latest work before validating
        painter.SaveToActivePath(); 
    
        bool success = painter.BakeActivePath();
    
        if (success) {
            if (!allMapPaths.Contains(currentActivePath)) allMapPaths.Add(currentActivePath);
            SwitchButtons();
            ExitPainterMode();
        } else {
            // Here you could trigger a UI animation or shake the "Bake" button
            Debug.LogWarning("Cannot Bake: Path is incomplete.");
        }
    }

    public void DeleteCurrentPath()
    {
        // If we are in edit mode, currentActivePath is the one to delete.
        // If we are not in edit mode, but a path is selected, delete that one.
        PathData pathToDelete = currentActivePath != null ? currentActivePath : selectedPath;

        if (pathToDelete == null) return;

        // Remove from list
        allMapPaths.Remove(pathToDelete);

        // Delete File
#if UNITY_EDITOR
        string path = AssetDatabase.GetAssetPath(pathToDelete);
        AssetDatabase.DeleteAsset(path);
#endif

        // Delete Tilemap if it exists and belongs to the path being deleted
        if (currentTilemapObj != null && currentTilemapObj.name == pathToDelete.tilemapName) DestroyImmediate(currentTilemapObj);
        
        // If the deleted path was the active one, exit painter mode
        if (currentActivePath == pathToDelete)
        {
            ExitPainterMode();
        }
        else // If it was just a selected path, clear selection and refresh UI
        {
            ClearSelection();
            RefreshPathUI();
        }
    }

    private void ExitPainterMode()
    {
        painter.enabled = false;
        painter.activePath = null;
        painter.mainTilemap = null;
        currentActivePath = null;
        currentTilemapObj = null; // Clear the reference to the tilemap GameObject
        
        ClearSelection(); // NEW: Clear selection and outlines when exiting painter mode
        
        // Return all paths to full visibility
        SetAllAlphas(1.0f);
        UpdateMasterVisuals();
        
        RefreshPathUI();
    }
    
    public void EditExistingPath(PathData pathToEdit)
    {
        if (pathToEdit == null || painter.enabled) return;

        Debug.Log("Entering Edit Mode for: " + pathToEdit.tilemapName);
        currentActivePath = pathToEdit;
        selectedPath = pathToEdit; // Ensure selectedPath is also set when entering edit mode

        // 1. Find the path's specific Tilemap in the Grid
        Transform existingMap = grid.transform.Find(pathToEdit.tilemapName);
        if (existingMap != null) {
            currentTilemapObj = existingMap.gameObject;
        } else {
            // Fallback just in case the object was destroyed but the data remains
            GameObject go = new GameObject(pathToEdit.tilemapName);
            go.transform.SetParent(grid.transform, false);
            currentTilemapObj = go;
            currentTilemapObj.AddComponent<Tilemap>();
            currentTilemapObj.AddComponent<TilemapRenderer>();
        }

        // 2. Setup Painter
        painter.activePath = currentActivePath;
        painter.mainTilemap = currentTilemapObj.GetComponent<Tilemap>();
        painter.enabled = true;
        painter.LoadFromActivePath();

        SwitchButtons();
        UpdateGhosting();
        RefreshSelectedPathUI(); // Refresh UI to show editing state
        // painter.DrawPathOutlines(currentActivePath, true); // Moved to Update() for continuous refresh
    }

    public bool HasPathAt(Vector3Int cellPos) {
        if (masterTilemap == null) return false;
        return masterTilemap.HasTile(cellPos);
    }

    private void UpdateGhosting()
    {
        SetAllAlphas(ghostAlpha);
        // Ensure the one we are drawing on is 100% visible
        if (currentTilemapObj != null) {
            currentTilemapObj.GetComponent<Tilemap>().color = Color.white;
        }
    }

    private void SetAllAlphas(float alpha)
    {
        foreach (Transform child in grid.transform) {
            Tilemap tm = child.GetComponent<Tilemap>();
            // Exclude ghostTilemap and outlineTilemap from alpha changes
            if (tm != null && tm != painter.ghostTilemap && tm != painter.outlineTilemap) {
                Color c = tm.color;
                c.a = alpha;
                tm.color = c;
            }
        }
    }
    
    public void UpdateMasterVisuals()
    {
        if (masterTilemap == null) return;

        masterTilemap.ClearAllTiles();
    
        // A dictionary to store the "Combined Mask" for every coordinate
        Dictionary<Vector3Int, int> mergedMasks = new Dictionary<Vector3Int, int>();

        // 1. Collect masks from ALL saved paths
        foreach (PathData path in allMapPaths)
        {
            foreach (var connection in path.savedConnections)
            {
                if (mergedMasks.ContainsKey(connection.pos)) {
                    // Combine the masks using Bitwise OR (|)
                    mergedMasks[connection.pos] |= connection.mask;
                } else {
                    mergedMasks[connection.pos] = connection.mask;
                }
            }
        }

        // 2. Draw the merged results to the Master Tilemap
        foreach (var kvp in mergedMasks)
        {
            // Ask the painter to give us the right tile based on the combined mask
            Tile combinedTile = painter.GetTileFromMask(kvp.Value);
            masterTilemap.SetTile(kvp.Key, combinedTile);
        }
    }

    // NEW: Refreshes the UI card for the selected path
    public void RefreshSelectedPathUI()
    {
        if (pathInfoCard == null || mainSelectionContainer == null) return;

        if (painter.enabled) // If we are in edit mode
        {
            mainSelectionContainer.SetActive(true); // Show the main panel (with Save/Delete for active path)
            pathInfoCard.SetActive(false);        // Hide the info card
        }
        else // Not in edit mode
        {
            if (selectedPath == null)
            {
                mainSelectionContainer.SetActive(true);
                pathInfoCard.SetActive(false);

                // Clear text fields when no path is selected
                if (pathNameText != null) pathNameText.text = "";
                if (pathDescriptionText != null) pathDescriptionText.text = "";
                if (numRoutesText != null) numRoutesText.text = "";
                if (numEntrancesText != null) numEntrancesText.text = "";
                if (numExitsText != null) numExitsText.text = "";
            }
            else // A path is selected, but not being edited
            {
                mainSelectionContainer.SetActive(false);
                pathInfoCard.SetActive(true);
                
                // Populate UI card with selectedPath data
                if (pathNameText != null) pathNameText.text = selectedPath.tilemapName;
                
                int entranceCount = selectedPath.entranceTiles != null ? selectedPath.entranceTiles.Count : 0;
                int routeCount = selectedPath.subpathRoutes != null ? selectedPath.subpathRoutes.Count : 0;

                if (pathDescriptionText != null) pathDescriptionText.text = $"A path with {entranceCount} entrances and {routeCount} distinct routes.";
                if (numRoutesText != null) numRoutesText.text = $"Possible Routes: {routeCount}";
                if (numEntrancesText != null) numEntrancesText.text = $"Entrance Tiles: {entranceCount}";
                if (numExitsText != null) numExitsText.text = $"Exits: {routeCount}"; // Assuming one exit per route

                Debug.Log($"Selected Path: {selectedPath.tilemapName}");
            }
        }

        // Hook up the Edit button on the card
        if (editSelectedPathButton != null)
        {
            editSelectedPathButton.onClick.RemoveAllListeners(); // Clear previous listeners
            editSelectedPathButton.onClick.AddListener(EnterEditModeFromSelection);
            // Only interactable if not editing AND a path is selected
            editSelectedPathButton.interactable = !painter.enabled && selectedPath != null; 
        }

        // Hook up the Delete button on the card
        if (deleteSelectedPathBtn != null)
        {
            deleteSelectedPathBtn.onClick.RemoveAllListeners(); // Clear previous listeners
            deleteSelectedPathBtn.onClick.AddListener(DeleteCurrentPath); // Calls the existing delete method
            // Only interactable if not editing AND a path is selected
            deleteSelectedPathBtn.interactable = !painter.enabled && selectedPath != null; 
        }
    }
}