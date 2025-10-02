using UnityEngine;

public class DestroyPipes : MonoBehaviour
{
    public GameObject generatePipes;
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log(other.gameObject.name);

        if (other.gameObject.CompareTag("Pipe") || other.gameObject.CompareTag("Cloud") || other.gameObject.CompareTag("Building"))
        {
            Destroy(other.gameObject);
            //Debug.LogWarning("Pipe destroyed");
            //generatePipes.GetComponent<GeneratePipes>().CreatePipe();
        }
    }
}
