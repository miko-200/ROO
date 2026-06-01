using System.Collections.Generic;
using UnityEngine;

// This is the ONLY place this struct should exist in your whole project
[System.Serializable]
public struct SerializableConnection {
    public Vector3Int pos;
    public int mask;
}

[CreateAssetMenu(fileName = "NewPath", menuName = "PathSystem/PathData")]
public class PathData : ScriptableObject
{
    public List<Vector3Int> entranceTiles = new List<Vector3Int>();
    
    // Notice we removed the "PathPainter." prefix here
    public List<SerializableConnection> savedConnections = new List<SerializableConnection>();

    public List<PathPainter.EnemyRoute> subpathRoutes = new List<PathPainter.EnemyRoute>();
    public List<PathPainter.EnemyRoute> reverseSubpathRoutes = new List<PathPainter.EnemyRoute>();
    
    [HideInInspector] public string tilemapName; 
}