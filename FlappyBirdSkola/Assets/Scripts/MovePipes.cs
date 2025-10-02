using UnityEngine;

public class MovePipes : MonoBehaviour
{
    [HideInInspector] public bool canMove;
    public float speed = 1f;
    
    void Update()
    {
        if (canMove)
        {
            transform.position += Vector3.right * Time.deltaTime * -speed;
        }
    }
}
