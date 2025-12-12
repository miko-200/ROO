using UnityEngine;
using UnityEngine.UI;

public class TabController : MonoBehaviour
{
    public GameObject[] tabImages;
    public GameObject[] pages;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ActivateTab(0);
    }

    public void ActivateTab(int tabNo)
    {
        for (int i = 0; i < pages.Length; i++)
        {
            tabImages[i].SetActive(true);
            pages[i].SetActive(false);
        }
        tabImages[tabNo].SetActive(false);
        pages[tabNo].SetActive(true);
    }
}
