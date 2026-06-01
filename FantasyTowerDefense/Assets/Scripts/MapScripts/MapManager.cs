using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    [Header("Managers")]
    public PathManager pathManager;
    public ObjectManager objectManager;

    [Header("Global Map Settings")]
    public RectInt mapBounds = new RectInt(0, 0, 15, 10);

    public void SaveMap(string mapFileName)
    {
        MapSaveData save = new MapSaveData();
        save.mapName = mapFileName;

        // 1. Ask PathManager for its data
        foreach (PathData data in pathManager.allMapPaths) {
            save.allPaths.Add(new SerializablePath {
                pathName = data.name,
                entranceTiles = new List<Vector3Int>(data.entranceTiles),
                connections = new List<SerializableConnection>(data.savedConnections)
            });
        }

        // 2. Ask ObjectManager for its data
        foreach (var obj in objectManager.spawnedObjects) {
            save.allObjects.Add(new PlacedObjectData {
                prefabID = obj.prefabID,
                position = obj.instance.transform.position
            });
        }

        string json = JsonUtility.ToJson(save, true);
        string filePath = Path.Combine(Application.persistentDataPath, mapFileName + ".json");
        File.WriteAllText(filePath, json);
        Debug.Log("Map Saved to: " + filePath);
    }

    // You will expand this later to load paths AND objects
    public void LoadMap(string mapFileName) {
        // ... Load logic ...
    }

    void OnDrawGizmos() {
        Gizmos.color = Color.yellow;
        Vector3 center = new Vector3(mapBounds.x + mapBounds.width / 2f, mapBounds.y + mapBounds.height / 2f, 0);
        Gizmos.DrawWireCube(center, new Vector3(mapBounds.width, mapBounds.height, 0.1f));
    }
    
    public bool IsInBounds(Vector3 worldPos) {
        // We allow the position to be anywhere from xMin to xMax 
        // without integer rounding interference.
        return worldPos.x >= mapBounds.xMin && 
               worldPos.x <= mapBounds.xMax && 
               worldPos.y >= mapBounds.yMin && 
               worldPos.y <= mapBounds.yMax;
    }
    
    
}