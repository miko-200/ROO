using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class WizardStats : MonoBehaviour
{
    public float damage;
    public float projectileSpeed;
    public float projectileLifeTime;
    public int projectilePierce;
    public float projectileExplosionRadius;
    public float projectileExplosionDamage;
    public bool hasExplosion;
    public float firerate;
    
    
    public float radius = 10f;
    public List<GameObject> targets = new List<GameObject>();
    
    private CircleCollider2D col;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        col = GetComponent<CircleCollider2D>();
        col.radius = radius;
    }

    // Update is called once per frame
    void Update()
    {
        if (targets.Count > 0 && targets[0] != null)
        {   
            //Debug.Log("Target " + targets[0]);
            Vector2 dirToEnemy = (Vector2)targets[0].transform.position - (Vector2)transform.position;
            float angle = Mathf.Atan2(dirToEnemy.y, dirToEnemy.x) * Mathf.Rad2Deg - 90;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
        else
        {
            //Debug.Log("No targets found");
        }
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        
        //Debug.Log(other.gameObject.name + "Entered");
        if (other.gameObject.CompareTag("Enemy") && !targets.Contains(other.gameObject))
        {
            targets.Add(other.gameObject);
            //Debug.Log("Added " + other.gameObject.name);
        }
    }

    public void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Enemy") && targets.Contains(other.gameObject))
        {
            targets.Remove(other.gameObject);
            //Debug.Log("Removed " + other.gameObject.name);
        }
    }
}
