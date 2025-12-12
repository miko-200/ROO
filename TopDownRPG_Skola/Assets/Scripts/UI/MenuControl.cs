using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

public class MenuControl : MonoBehaviour
{
    public GameObject menuCanvas;
    public List<Key> menuKey = new List<Key>();
    public bool affectWalk = true;
    
    private GameObject player;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        menuCanvas.SetActive(false);
        player = GameObject.FindGameObjectWithTag("Player");
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < menuKey.Count; i++)
        {
            if (Keyboard.current[menuKey[i]].wasPressedThisFrame)
            {
                GetComponentInParent<CheckMenus>().DisableMenus(menuCanvas);
                menuCanvas.SetActive(!menuCanvas.activeSelf);
                if (affectWalk)
                {
                    if (menuCanvas.activeSelf)
                    {
                        player.GetComponent<PlayerMovement>().canWalk = false;
                    }
                    else
                    {
                        player.GetComponent<PlayerMovement>().canWalk = true;
                    }
                }
            }
        }
    }
}
