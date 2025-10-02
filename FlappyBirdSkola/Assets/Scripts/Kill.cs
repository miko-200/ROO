using System;
using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class Kill : MonoBehaviour
{
    
    
    private void OnCollisionEnter2D(Collision2D other)
    {
        Debug.Log(other.gameObject.name);

        if (other.gameObject.CompareTag("Player"))
        {
            GameObject.FindGameObjectWithTag("Canvas").GetComponent<GameOver>().ShowGameOverScreen();
        }
    }
}
