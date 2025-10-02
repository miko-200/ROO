using System;
using UnityEngine;

public class CreatePipes : MonoBehaviour
{
    public GameObject generatePipes;
    
    private bool canGenerate = true;

    private void Start()
    {
        //transform.position = new Vector3(generatePipes.GetComponent<GeneratePipes>().minDistance - generatePipes.GetComponent<GeneratePipes>().distance, 0, 0);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log(other.gameObject.name);

        if (other.gameObject.CompareTag("Pipe"))
        {
            generatePipes.GetComponent<GeneratePipes>().CreatePipe();
            canGenerate = false;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Pipe"))
        {
            canGenerate = true;
        }
    }
}
