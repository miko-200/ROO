using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SerializablePath {
    public string pathName;
    public List<Vector3Int> entranceTiles;
    public List<SerializableConnection> connections;
}

[System.Serializable]
public class PlacedObjectData {
    public string prefabID;
    public Vector3 position;
}

[System.Serializable]
public class MapSaveData {
    public string mapName;
    public List<SerializablePath> allPaths = new List<SerializablePath>();
    public List<PlacedObjectData> allObjects = new List<PlacedObjectData>();
}