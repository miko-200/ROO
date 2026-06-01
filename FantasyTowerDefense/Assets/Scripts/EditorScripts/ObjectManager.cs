using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ObjectManager : MonoBehaviour
{
    [System.Serializable]
    public class PlaceablePrefab {
        public string prefabID;
        public GameObject prefab;
        public bool canPlaceOnPath; 
    }

    [Header("References")]
    public MapManager mapManager;
    public Transform objectContainer; // An empty GameObject to keep the hierarchy clean

    [Header("Prefabs")]
    public List<PlaceablePrefab> availablePrefabs;
    public int activePrefabIndex = 0;

    [Header("Inputs")]
    public InputAction placeAction;

    // We store a custom class/struct on the live objects so we can save them easily later
    public class PlacedObject {
        public string prefabID;
        public GameObject instance;
    }
    public List<PlacedObject> spawnedObjects = new List<PlacedObject>();
    
    [Header("Ghost Settings")]
    public float ghostAlpha = 0.5f;
    private GameObject currentGhost;
    private SpriteRenderer[] ghostRenderers;

    private Camera cam;
    public bool isPlacingModeActive = false; // Toggle this via UI when user wants to place objects
    
    void Awake()
    {
        cam = Camera.main;
    }

    void OnEnable()
    {
        placeAction.Enable();
    }
    void OnDisable() { placeAction.Disable(); }

    public void TogglePlacingMode()
    {
        isPlacingModeActive = !isPlacingModeActive;
        Debug.Log("Placing mode active: " + isPlacingModeActive);
    }

    void Update()
    {
        if (!isPlacingModeActive || availablePrefabs.Count == 0)
        {
            if (currentGhost != null) currentGhost.SetActive(false);
            return;
        }
        
        Vector3 mousePos = Mouse.current.position.ReadValue();
        Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, cam.nearClipPlane));
        worldPos.z = 0;

        // 1. Manage the Ghost Instance
        UpdateGhost(worldPos);

        // 2. Perform Validation
        bool inBounds = mapManager.IsInBounds(worldPos);
        bool pathBlocked = false;

        if (!availablePrefabs[activePrefabIndex].canPlaceOnPath)
        {
            Vector3Int cell = mapManager.pathManager.masterTilemap.WorldToCell(worldPos);
            pathBlocked = mapManager.pathManager.HasPathAt(cell);
        }

        bool isValid = inBounds && !pathBlocked;

        // 3. Visual Feedback
        SetGhostVisuals(isValid);

        // 4. Placement Logic
        if (placeAction.WasPressedThisFrame() && isValid)
        {
            PlaceObject(worldPos);
        }

        if (placeAction.WasPressedThisFrame())
        {
            mousePos = Mouse.current.position.ReadValue();
            worldPos = cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, cam.nearClipPlane));
            worldPos.z = 0; // Ensure it sits flat on the 2D plane

            PlaceablePrefab activePrefab = availablePrefabs[activePrefabIndex];

            // 1. Check Bounds (using float coordinates against the int bounds)
            if (!mapManager.IsInBounds(worldPos)) {
                Debug.Log("Cannot place: Out of bounds.");
                return;
            }

            // 2. Check Path Collision
            if (!activePrefab.canPlaceOnPath)
            {
                // Use FloorToInt to ensure 14.9 maps to tile 14, not 15
                Vector3Int cellPosition = new Vector3Int(
                    Mathf.FloorToInt(worldPos.x),
                    Mathf.FloorToInt(worldPos.y),
                    0
                );

                if (mapManager.pathManager.HasPathAt(cellPosition))
                {
                    Debug.Log("Cannot place: Path is in the way.");
                    return;
                }
            }

            // 3. Spawn Object
            GameObject newObj = Instantiate(activePrefab.prefab, worldPos, Quaternion.identity, objectContainer);
            spawnedObjects.Add(new PlacedObject { prefabID = activePrefab.prefabID, instance = newObj });
        }
    }
    
    private void UpdateGhost(Vector3 position)
    {
        // If we don't have a ghost, or we changed prefabs, recreate it
        string targetID = availablePrefabs[activePrefabIndex].prefabID;
    
        if (currentGhost == null || currentGhost.name != targetID + "_Ghost")
        {
            if (currentGhost != null) Destroy(currentGhost);
        
            currentGhost = Instantiate(availablePrefabs[activePrefabIndex].prefab, position, Quaternion.identity);
            currentGhost.name = targetID + "_Ghost";
        
            // Strip out components that shouldn't run on a ghost (Colliders, AI scripts, etc.)
            foreach (var comp in currentGhost.GetComponentsInChildren<Collider2D>()) Destroy(comp);
        
            ghostRenderers = currentGhost.GetComponentsInChildren<SpriteRenderer>();
        }

        currentGhost.SetActive(true);
        currentGhost.transform.position = position;
    }

    private void SetGhostVisuals(bool isValid)
    {
        Color color = isValid ? Color.white : Color.red;
        color.a = ghostAlpha;

        foreach (var renderer in ghostRenderers)
        {
            renderer.color = color;
        }
    }
    
    private void PlaceObject(Vector3 position)
    {
        GameObject newObj = Instantiate(availablePrefabs[activePrefabIndex].prefab, position, Quaternion.identity, objectContainer);
        spawnedObjects.Add(new PlacedObject { 
            prefabID = availablePrefabs[activePrefabIndex].prefabID, 
            instance = newObj 
        });
    }

    public bool IsObjectBlockingPath(Vector3Int cell)
    {
        foreach (var obj in spawnedObjects)
        {
            // 1. Get the cell position of the placed object
            Vector3Int objCell = mapManager.pathManager.masterTilemap.WorldToCell(obj.instance.transform.position);
        
            if (objCell == cell)
            {
                // 2. Check if this specific prefab is ALLOWED on paths
                var settings = availablePrefabs.Find(p => p.prefabID == obj.prefabID);
                if (settings != null && !settings.canPlaceOnPath) 
                {
                    return true; // It's a blocker!
                }
            }
        }
        return false;
    }
}