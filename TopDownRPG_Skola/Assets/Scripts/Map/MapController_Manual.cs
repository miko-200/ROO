using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class MapController_Manual : MonoBehaviour
{
    public static MapController_Manual Instance { get; set; }
    
    public GameObject mapParent;
    private List<Image> mapImages;
    
    public Color highlightColor = Color.green;
    public Color dimmedColor = new Color(1f, 1f, 1f, 0.5f);
    
    public RectTransform playerIconTransform;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
        mapImages = mapParent.GetComponentsInChildren<Image>().ToList();
    }

    public void HighlightArea(string areaName)
    {
        foreach (Image area in mapImages)
        {
            area.color = dimmedColor;
        }
        
        Image currentArea = mapImages.Find(x => x.name == areaName);

        if (currentArea != null)
        {
            currentArea.color = highlightColor;
            
            playerIconTransform.position = currentArea.GetComponent<RectTransform>().position;
        }
        else
        {
            Debug.LogWarning("Area not found " + areaName);
        }
    }
    
}
