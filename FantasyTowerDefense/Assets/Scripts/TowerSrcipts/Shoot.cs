using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Shoot : MonoBehaviour
{
    
    public GameObject bulletPrefab;
    public Transform bulletSpawn;

    public List<Key> shootKey = new List<Key>();

    public void Update()
    {
        if (shootKey.Count > 0)
        {
            for (int i = 0; i < shootKey.Count; i++)
            {
                if (Keyboard.current[shootKey[i]].wasPressedThisFrame)
                {
                    ShootProjectile();
                }
            }
        }
    }
    
    public void ShootProjectile()
    {
        Instantiate(bulletPrefab, bulletSpawn.position, bulletSpawn.rotation, transform);
        Debug.Log("Bullet shot");
    }
}
