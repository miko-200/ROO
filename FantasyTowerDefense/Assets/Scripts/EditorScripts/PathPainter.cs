using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class PathPainter : MonoBehaviour
{
    [Header("Active File")]
    public PathData activePath; 
    public float ghostAlpha = 0.3f;

    [Header("Grid Settings")]
    private RectInt paintableBounds = new RectInt(-13, -7, 20, 14);
    public bool autoConnectToEdge = true;

    [Header("Tilemaps")]
    public Tilemap mainTilemap;
    public Tilemap ghostTilemap;
    
    [Header("Inputs")]
    public InputAction paintAction;
    public InputAction eraseAction;
    
    [Header("Sprites")]
    public Sprite single, horizontal, vertical, fourWay;
    public Sprite cornerUR, cornerRD, cornerDL, cornerLU;
    public Sprite tUp, tRight, tDown, tLeft, endUp, endRight, endDown, endLeft;

    private Dictionary<Vector3Int, ConnectionData> currentSessionData = new();
    private Dictionary<int, Tile> tileLookup = new();
    private Vector3Int? lastPaintedCell = null;
    private Vector3Int? strokeStartCell = null;
    private Vector3Int lastGhostPos;
    private bool isPaintingStroke = false;
    private Camera cam;

    const int UP = 1, RIGHT = 2, DOWN = 4, LEFT = 8;
    public class ConnectionData { public int Mask = 0; }

    [System.Serializable]
    public class EnemyRoute { public List<Vector3> nodes = new List<Vector3>(); }
    
    
    private bool objectIsBlockingPath = false;
    private ObjectManager objManager;

    void Awake() {
        cam = Camera.main;
        paintableBounds = FindObjectOfType<MapManager>().mapBounds;
        objManager = FindObjectOfType<ObjectManager>();
        BuildTileLookup();
    }

    void Start() { LoadFromActivePath(); }

    void OnEnable() { paintAction.Enable(); eraseAction.Enable(); }
    void OnDisable() { paintAction.Disable(); eraseAction.Disable(); }

    public void LoadFromActivePath() {
        mainTilemap.ClearAllTiles();
        currentSessionData.Clear();
        if (activePath == null) return;

        foreach (var conn in activePath.savedConnections) {
            currentSessionData[conn.pos] = new ConnectionData { Mask = conn.mask };
            RefreshTile(conn.pos);
        }
    }
    
    bool IsPlacementValid(Vector3Int cell)
    {
        // Check 1: Is an object blocking?
        if (objManager.IsObjectBlockingPath(cell)) return false;

        // Check 2: Adjacency (The Vine Rule)
        // If it's the first tile, it must be on the edge.
        if (currentSessionData.Count == 0) return IsOnEdge(cell);

        // If it's already part of the path, it's valid to hover/paint
        if (currentSessionData.ContainsKey(cell)) return true;

        // Is it touching an existing tile?
        bool isTouching = currentSessionData.ContainsKey(cell + Vector3Int.up) ||
                          currentSessionData.ContainsKey(cell + Vector3Int.down) ||
                          currentSessionData.ContainsKey(cell + Vector3Int.left) ||
                          currentSessionData.ContainsKey(cell + Vector3Int.right);

        return isTouching;
    }

    void Update() {
        if (activePath == null) return;

        Vector3 mousePos = Mouse.current.position.ReadValue();
        Vector3 world = cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, cam.nearClipPlane));
        world.z = 0;
        
        Vector3Int currentCell = mainTilemap.WorldToCell(world);
        bool isInside = paintableBounds.Contains(new Vector2Int(currentCell.x, currentCell.y));

        bool isAllowed = IsPlacementValid(currentCell);

        if (isInside) {
            UpdateGhost(currentCell, isAllowed);
        } else {
            ghostTilemap.ClearAllTiles();
        }

        // 1. Paint Logic
        if (paintAction.IsPressed() && isInside && isAllowed) {
            if (!isPaintingStroke) {
                isPaintingStroke = true;
                strokeStartCell = currentCell;
                if (IsOnEdge(currentCell) && activePath.entranceTiles.Count == 0) {
                    activePath.entranceTiles.Add(currentCell);
                }
                PaintInitialCell(currentCell);
                lastPaintedCell = currentCell;
            }
            if (currentCell != lastPaintedCell) {
                DrawPathTo(lastPaintedCell.Value, currentCell);
                lastPaintedCell = currentCell;
            }
        } 
        else if (isPaintingStroke && !paintAction.IsPressed()) {
            // SNAP TO EDGE: Ensures the path actually hits the boundary to trigger Bake
            if (autoConnectToEdge) {
                if (strokeStartCell.HasValue) HandleEdgeConnection(strokeStartCell.Value, world);
                if (lastPaintedCell.HasValue) HandleEdgeConnection(lastPaintedCell.Value, world);
            }
            isPaintingStroke = false;
            strokeStartCell = null;
            lastPaintedCell = null;
            SaveToActivePath();
        }

        // 2. Erase Logic
        if (eraseAction.IsPressed() && isInside) {
            RemoveTile(currentCell);
            SaveToActivePath();
        }
        FindObjectOfType<PathManager>().UpdateMasterVisuals();
    }
    
    public bool IsPathValid(out string errorMessage)
    {
        Debug.Log($"Checking path validity. Total tiles: {currentSessionData.Count}");
        errorMessage = "";
        if (currentSessionData.Count == 0) {
            errorMessage = "The path is empty!";
            return false;
        }

        int exitCount = 0;
        // Inside IsPathValid, right before the loop
        Debug.Log($"Bounds: Min({paintableBounds.xMin}, {paintableBounds.yMin}) Max({paintableBounds.xMax-1}, {paintableBounds.yMax-1})");
        foreach (var kvp in currentSessionData)
        {
            Vector3Int pos = kvp.Key;
            int connections = GetConnectionCount(kvp.Value.Mask);
    
            // Add this to see what the system thinks is "on edge"
            bool onEdge = IsOnEdge(pos);
            Debug.Log($"Checking Tile {pos}: Connections={connections}, OnEdge={onEdge}");
            // 0 connections means a floating, broken tile
            if (connections == 0) {
                errorMessage = $"Floating tile detected at {pos}.";
                return false;
            }

            // 1 connection means a dead end
            if (connections == 1)
            {
                // If it's NOT on the edge, it's a branch that stopped in the middle
                if (!IsValidEndpoint(pos)) 
                {
                    errorMessage = $"Branch stopped at {pos}. All paths must end at the map edge.";
                    return false;
                }
            
                // If it IS on the edge, it's a valid end/start point
                exitCount++;
            }
        }

        // A valid path needs at least 2 ends (Start and End)
        if (exitCount < 2) {
            errorMessage = "Path must start and end at the edge of the map.";
            return false;
        }

        return true;
    }

    // --- Path Generation ---
    public bool BakeActivePath() {
        
        string error;
        if (!IsPathValid(out error)) {
            Debug.LogError("Bake Failed: " + error);
            // If you have a UI text element, you could set it here:
            // myErrorText.text = error; 
            return false; 
        }

        if (activePath == null || activePath.entranceTiles.Count == 0) return false;

        activePath.subpathRoutes.Clear();
        activePath.reverseSubpathRoutes.Clear();

        Vector3Int startNode = activePath.entranceTiles[0];
        List<Vector3Int> allExits = currentSessionData.Keys.Where(pos => IsOnEdge(pos) && pos != startNode).ToList();
        List<List<Vector3Int>> rawResults = new List<List<Vector3Int>>();
        
        foreach (var endNode in allExits) {
            FindRoutesRecursive(startNode, endNode, new List<Vector3Int>(), new HashSet<(Vector3Int, Vector3Int)>(), rawResults);
        }

        // FILTER: Keep only the longest paths (removes shortcuts that skip loops)
        var filtered = rawResults.OrderByDescending(p => p.Count).ToList();
        List<List<Vector3Int>> finalPaths = new List<List<Vector3Int>>();

        foreach (var path in filtered) {
            bool isSubset = false;
            foreach (var existing in finalPaths) {
                if (IsPathSubset(path, existing)) { isSubset = true; break; }
            }
            if (!isSubset) finalPaths.Add(path);
        }

        foreach (var p in finalPaths) SaveRoute(p);
        Debug.Log($"Baked {activePath.subpathRoutes.Count} Unique Paths.");
        return true;
    }

    bool IsPathSubset(List<Vector3Int> small, List<Vector3Int> large) {
        if (small.Count >= large.Count) return false;
        // Checks if all tiles in 'small' appear in 'large' in the same order
        int lastIndex = -1;
        foreach (var pos in small) {
            int index = large.IndexOf(pos, lastIndex + 1);
            if (index == -1) return false;
            lastIndex = index;
        }
        return true;
    }

    private void FindRoutesRecursive(Vector3Int current, Vector3Int target, List<Vector3Int> currentPath, HashSet<(Vector3Int, Vector3Int)> visited, List<List<Vector3Int>> results) {
        List<Vector3Int> branch = new List<Vector3Int>(currentPath) { current };
        if (current == target) {
            results.Add(branch);
        } else {
            int mask = currentSessionData[current].Mask;
            Vector3Int[] neighbors = { current + Vector3Int.up, current + Vector3Int.right, current + Vector3Int.down, current + Vector3Int.left };
            int[] bits = { UP, RIGHT, DOWN, LEFT };

            for (int i = 0; i < 4; i++) {
                Vector3Int next = neighbors[i];
                if ((mask & bits[i]) != 0 && !visited.Contains((current, next)) && currentSessionData.ContainsKey(next)) {
                    var nextVisited = new HashSet<(Vector3Int, Vector3Int)>(visited) { (current, next), (next, current) };
                    FindRoutesRecursive(next, target, branch, nextVisited, results);
                }
            }
        }
    }

    // --- Neighbor Updating Eraser ---
    void RemoveTile(Vector3Int pos) {
        mainTilemap.SetTile(pos, null);
        if (!currentSessionData.ContainsKey(pos)) return;

        // Tell neighbors to "Forget" this tile
        DisconnectNeighbor(pos + Vector3Int.up, DOWN);
        DisconnectNeighbor(pos + Vector3Int.down, UP);
        DisconnectNeighbor(pos + Vector3Int.right, LEFT);
        DisconnectNeighbor(pos + Vector3Int.left, RIGHT);

        currentSessionData.Remove(pos);
        if (activePath.entranceTiles.Contains(pos)) activePath.entranceTiles.Remove(pos);
    }

    void DisconnectNeighbor(Vector3Int nPos, int maskToClear) {
        if (currentSessionData.TryGetValue(nPos, out ConnectionData data)) {
            data.Mask &= ~maskToClear;
            RefreshTile(nPos);
        }
    }

    // --- Logic Helpers ---
    void HandleEdgeConnection(Vector3Int cell, Vector3 mouseWorldPos) {
        if (!currentSessionData.TryGetValue(cell, out ConnectionData data)) return;
        if (GetConnectionCount(data.Mask) > 1) return; // Don't auto-connect if it's already a junction

        int bestDir = 0;
        if (cell.x == paintableBounds.xMin) bestDir = LEFT;
        else if (cell.x == paintableBounds.xMax - 1) bestDir = RIGHT;
        else if (cell.y == paintableBounds.yMin) bestDir = DOWN;
        else if (cell.y == paintableBounds.yMax - 1) bestDir = UP;

        if (bestDir == LEFT) ConnectTiles(cell, cell + Vector3Int.left);
        else if (bestDir == RIGHT) ConnectTiles(cell, cell + Vector3Int.right);
        else if (bestDir == DOWN) ConnectTiles(cell, cell + Vector3Int.down);
        else if (bestDir == UP) ConnectTiles(cell, cell + Vector3Int.up);
    }

    void RefreshTile(Vector3Int pos) { 
        if (currentSessionData.TryGetValue(pos, out var d)) {
            mainTilemap.SetTile(pos, tileLookup.ContainsKey(d.Mask) ? tileLookup[d.Mask] : tileLookup[0]);
        }
    }

    void SaveRoute(List<Vector3Int> cellPath) {
        EnemyRoute fwd = new EnemyRoute();
        foreach (var c in cellPath) {
            fwd.nodes.Add(mainTilemap.GetCellCenterWorld(c));
        }
        // Matching your new naming convention
        activePath.subpathRoutes.Add(fwd); 

        EnemyRoute rev = new EnemyRoute();
        rev.nodes = new List<Vector3>(fwd.nodes);
        rev.nodes.Reverse();
        activePath.reverseSubpathRoutes.Add(rev);
    }

    // (Standard Helpers: BuildTileLookup, ConnectTiles, etc. remain the same)
    void ConnectTiles(Vector3Int a, Vector3Int b) {
        if (!currentSessionData.ContainsKey(a)) currentSessionData[a] = new ConnectionData();
        if (!currentSessionData.ContainsKey(b)) currentSessionData[b] = new ConnectionData();
        Vector3Int dir = b - a;
        if (dir == Vector3Int.up) { currentSessionData[a].Mask |= UP; currentSessionData[b].Mask |= DOWN; }
        else if (dir == Vector3Int.down) { currentSessionData[a].Mask |= DOWN; currentSessionData[b].Mask |= UP; }
        else if (dir == Vector3Int.right) { currentSessionData[a].Mask |= RIGHT; currentSessionData[b].Mask |= LEFT; }
        else if (dir == Vector3Int.left) { currentSessionData[a].Mask |= LEFT; currentSessionData[b].Mask |= RIGHT; }
        RefreshTile(a); RefreshTile(b);
    }

    void BuildTileLookup() {
        tileLookup[0] = CreateTile(single); tileLookup[LEFT | RIGHT] = CreateTile(horizontal); tileLookup[UP | DOWN] = CreateTile(vertical);
        tileLookup[UP | RIGHT] = CreateTile(cornerUR); tileLookup[RIGHT | DOWN] = CreateTile(cornerRD); tileLookup[DOWN | LEFT] = CreateTile(cornerDL);
        tileLookup[LEFT | UP] = CreateTile(cornerLU); tileLookup[LEFT | RIGHT | UP] = CreateTile(tUp); tileLookup[UP | DOWN | RIGHT] = CreateTile(tRight);
        tileLookup[LEFT | RIGHT | DOWN] = CreateTile(tDown); tileLookup[UP | DOWN | LEFT] = CreateTile(tLeft); tileLookup[UP | RIGHT | DOWN | LEFT] = CreateTile(fourWay);
        tileLookup[UP] = CreateTile(endUp); tileLookup[RIGHT] = CreateTile(endRight); tileLookup[DOWN] = CreateTile(endDown); tileLookup[LEFT] = CreateTile(endLeft);
    }
    Tile CreateTile(Sprite s) { Tile t = ScriptableObject.CreateInstance<Tile>(); t.sprite = s; return t; }
    public void SaveToActivePath() {
        if (activePath == null) return; 
    
        activePath.savedConnections.Clear();
    
        foreach (var kvp in currentSessionData) {
            // Use the struct from PathData
            SerializableConnection conn = new SerializableConnection { 
                pos = kvp.Key, 
                mask = kvp.Value.Mask 
            };
            activePath.savedConnections.Add(conn);
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(activePath);
#endif
    }
    bool IsOnEdge(Vector3Int p) 
    {
        // Check if the tile is within 1 unit of the boundary limits
        bool isXEdge = (p.x <= paintableBounds.xMin || p.x >= paintableBounds.xMax - 1);
        bool isYEdge = (p.y <= paintableBounds.yMin || p.y >= paintableBounds.yMax - 1);
    
        return isXEdge || isYEdge;
    } 
    bool IsValidEndpoint(Vector3Int p)
    {
        // A tile is a valid end if it is on the edge, 
        // OR if it is exactly one tile outside the edge (caused by the snap)
        bool onEdgeX = (p.x == paintableBounds.xMin || p.x == paintableBounds.xMax - 1);
        bool onEdgeY = (p.y == paintableBounds.yMin || p.y == paintableBounds.yMax - 1);
    
        // Add "Snap" tolerance: one tile past the min/max
        bool snapEdgeX = (p.x == paintableBounds.xMin - 1 || p.x == paintableBounds.xMax);
        bool snapEdgeY = (p.y == paintableBounds.yMin - 1 || p.y == paintableBounds.yMax);
    
        return (onEdgeX || onEdgeY || snapEdgeX || snapEdgeY);
    }
    int GetConnectionCount(int m) => (new[] { UP, RIGHT, DOWN, LEFT }).Count(b => (m & b) != 0);
    void PaintInitialCell(Vector3Int p) { if (!currentSessionData.ContainsKey(p)) { currentSessionData[p] = new ConnectionData(); RefreshTile(p); } }
    void DrawPathTo(Vector3Int start, Vector3Int end) {
        Vector3Int diff = end - start; int steps = Mathf.Max(Mathf.Abs(diff.x), Mathf.Abs(diff.y));
        for (int i = 1; i <= steps; i++) {
            Vector3Int curr = new Vector3Int(Mathf.RoundToInt(Mathf.Lerp(start.x, end.x, (float)i/steps)), Mathf.RoundToInt(Mathf.Lerp(start.y, end.y, (float)i/steps)), 0);
            ConnectTiles(start, curr); start = curr;
        }
    }
    void UpdateGhost(Vector3Int cell, bool isAllowed) {
        if (cell != lastGhostPos) {
            ghostTilemap.ClearAllTiles();
            Color c = isAllowed ? new Color(1, 1, 1, ghostAlpha) : new Color(1, 0, 0, ghostAlpha);
        
            Tile t = ScriptableObject.CreateInstance<Tile>(); 
            t.sprite = single; 
            t.color = c;
        
            ghostTilemap.SetTile(cell, t); 
            lastGhostPos = cell;
        }
    }
    
    public Tile GetTileFromMask(int mask) {
        if (tileLookup.ContainsKey(mask)) return tileLookup[mask];
        return tileLookup[0]; // Default to single tile
    }
}