using System.Collections.Generic;
using UnityEngine;

public class CheckMenus : MonoBehaviour
{
    public List<GameObject> menus = new List<GameObject>();

    public void DisableMenus(GameObject menuCanvas) // the canvas you want to ignore
    {
        for (int i = 0; i < menus.Count; i++)
        {
            if (menuCanvas != menus[i])
            {
                menus[i].SetActive(false);
            }
        }
    }
}
