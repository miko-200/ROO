using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.IO;
using System.Linq;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PathManager : MonoBehaviour
{
    [Header("References")]
    public PathPainter painter;
    public Grid grid; // The parent Grid object
    
    [Header("Visual Merging")]
    public Tilemap masterTilemap; // Create a new Tilemap in your Grid for this

    [Header("Settings")]
    public string folderName = "MapPaths";
    public float ghostAlpha = 0.2f;

    [Header("Active State")]
    public List<PathData> allMapPaths = new List<PathData>();
    private PathData currentActivePath;
    private GameObject currentTilemapObj;

    [Header("UI Interactions")]
    public GameObject CreateNewPathButton;
    public GameObject SavePathButton;
    public GameObject DeletePathButton;
    
    [Header("UI List")]
    // Assign your 'Content' object here
    public Transform pathUIContainer;     
    // Assign your 'Item' prefab here
    public GameObject pathItemPrefab; 

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

                // 4. Hook up the Button
                // The Setup method in ItemPathData already clears listeners, so we just add it here.
                if (itemPathData.button != null) {
                    itemPathData.button.onClick.AddListener(() => EditExistingPath(path));
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
    }
    
    private void TrySelectPathFromScene()
    {
        Vector3 mousePos = Mouse.current.position.ReadValue();
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, Camera.main.nearClipPlane));
        worldPos.z = 0;
        
        Vector3Int cellPos = masterTilemap.WorldToCell(worldPos);

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
            EditExistingPath(clickedPaths[0]);
        }
    }
    

    public void TogglePainter(bool isOn)
    {
        if (isOn) StartNewPath();
        else BakeAndExit();
    }

    public void StartNewPath()
    {
        Debug.Log("Starting new path");
        
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
        if (currentActivePath == null) return;

        // Remove from list
        allMapPaths.Remove(currentActivePath);

        // Delete File
#if UNITY_EDITOR
        string path = AssetDatabase.GetAssetPath(currentActivePath);
        AssetDatabase.DeleteAsset(path);
#endif

        // Delete Tilemap
        if (currentTilemapObj != null) DestroyImmediate(currentTilemapObj);
        SwitchButtons();
        ExitPainterMode();
    }

    private void ExitPainterMode()
    {
        painter.enabled = false;
        painter.activePath = null;
        painter.mainTilemap = null;
        currentActivePath = null;
        currentTilemapObj = null;
        
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
            if (tm != null && tm != painter.ghostTilemap) {
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
}