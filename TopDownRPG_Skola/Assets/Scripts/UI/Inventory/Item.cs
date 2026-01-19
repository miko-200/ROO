using UnityEngine;
using UnityEngine.UI;

public class Item : MonoBehaviour
{
    public int ID;
    public string name;

    public virtual void Pickup()
    {
        Sprite itemIcon = GetComponent<Image>().sprite;
        if (ItemPickupIUController.Instance != null)
        {
            ItemPickupIUController.Instance.ShowItemPickup(name, itemIcon);
        }
    }

    public virtual void UseItem()
    {
        Debug.Log("Using Item " + name);
    }
}
