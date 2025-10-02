using UnityEngine;

public class AddScore : MonoBehaviour
{
    private ScoreCounter scoreCounterS;
    void Start()
    {
        scoreCounterS = GameObject.Find("Canvas").GetComponent<ScoreCounter>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log(other.gameObject.name);

        if (other.gameObject.CompareTag("Player"))
        {
            scoreCounterS.AddScore(1);
            Destroy(this.gameObject);
        }
    }
}
