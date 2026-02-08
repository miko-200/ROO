using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 5f;
    public float damage = 1f;
    public float lifetime = 5f;
    public int pierce = 1;
    public float firerate = 1f;

    public bool hasExplosion = false;
    public float explosionRadius = 3f;
    public float explosionDamage = 1f;
    public ParticleSystem deathParticles;
    
    private bool dying = false;
    
    private Rigidbody2D rb;
    private CircleCollider2D col;
    //private List<GameObject> explosionTargets = new List<GameObject>();
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CircleCollider2D>();

        if (GetComponentInParent<WizardStats>() != null)
        {
            WizardStats stats = GetComponentInParent<WizardStats>();
            speed = stats.projectileSpeed;
            damage = stats.damage;
            lifetime = stats.projectileLifeTime;
            pierce = stats.projectilePierce;
            firerate = stats.firerate;
            hasExplosion = stats.hasExplosion;
            explosionRadius = stats.projectileExplosionRadius;
            explosionDamage = stats.projectileExplosionDamage;
        }
        
        transform.SetParent(null);
        
        Invoke(nameof(DestroyProjectile), lifetime);
    }

    public int segments = 64; // higher = smoother circle

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;

        Vector3 center = transform.position;
        Vector3 prevPoint = center + new Vector3(explosionRadius, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * explosionRadius, Mathf.Sin(angle) * explosionRadius, 0);

            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
    
    // Update is called once per frame
    void Update()
    {
        transform.Translate(Vector2.up * speed * Time.deltaTime);
    }

    private void DestroyProjectile()
    {
        if (deathParticles != null)
        {
            Instantiate(deathParticles, transform.position, Quaternion.identity);
        }

        if (hasExplosion)
        {
            Explode();
            dying = true;
            col.radius = explosionRadius;
            Invoke(nameof(Explode), 0.1f);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Explode()
    {
        
        Collider2D[] explosionTargets = Physics2D.OverlapCircleAll(transform.position, explosionRadius, 1 << LayerMask.NameToLayer("EnemyLayer"));

        foreach (Collider2D expT in explosionTargets)
        {
            
            Rigidbody2D rb = expT.GetComponent<Rigidbody2D>();
            if (rb != null && rb.tag == "Enemy")
            {
                rb.gameObject.GetComponent<EnemyStats>().TakeDamage(explosionDamage);
                Debug.Log(expT.name + $"took explosion {explosionDamage} damage");
            }
        }
        Destroy(gameObject);
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            if (pierce <= 1)
            {
                other.GameObject().GetComponent<EnemyStats>().TakeDamage(damage);
                DestroyProjectile();
            }
            else
            {
                float enemyHealth = other.GameObject().GetComponent<EnemyStats>().GetHealth();
                other.GameObject().GetComponent<EnemyStats>().TakeDamage(damage);

                if (damage <= enemyHealth)
                {
                    DestroyProjectile();
                }
                else
                {
                    damage -= enemyHealth;
                    pierce--;
                }
            }
            
        }
    }
}

