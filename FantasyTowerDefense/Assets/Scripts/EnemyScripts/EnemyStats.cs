using UnityEngine;

public class EnemyStats : MonoBehaviour
{
    public float health = 3f;
    public float speed = 2f;

    public float GetHealth()
    {
        return health;
    }
    
    public void TakeDamage(float damage)
    {
        health -= damage;
        if (health <= 0)
        {
            Destroy(gameObject);
        }
    }
}
