using System;
using UnityEngine;
using Object = UnityEngine.Object;

public class DontDestroy : MonoBehaviour
{
    [HideInInspector] public string objectID;

    private void Awake()
    {
        objectID = name + transform.position.ToString();
    }

    void Start()
    {
        for (int i = 0; i < Object.FindObjectsOfType<DontDestroy>().Length; i++)
        {
            if (Object.FindObjectsOfType<DontDestroy>()[i] != this)
            {
                if (Object.FindObjectsOfType<DontDestroy>()[i].objectID == objectID)
                {
                    Destroy(gameObject);
                }
            }
        }
        
        DontDestroyOnLoad(gameObject);
    }
}
