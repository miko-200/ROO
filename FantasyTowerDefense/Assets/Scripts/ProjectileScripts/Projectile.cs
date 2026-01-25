using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 5f;
    public float damage = 1f;
    
    private Rigidbody2D rb;
    private Collider2D col;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(Vector2.right * speed * Time.deltaTime);
    }
}
