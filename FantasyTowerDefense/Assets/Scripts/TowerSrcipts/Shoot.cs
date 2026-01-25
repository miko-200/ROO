using UnityEngine;

public class Shoot : MonoBehaviour
{
    
    public GameObject bulletPrefab;
    public Transform bulletSpawn;

    public void ShootProjectile()
    {
        Instantiate(bulletPrefab, bulletSpawn.position, bulletSpawn.rotation, transform);
        Debug.Log("Bullet shot");
    }
}
