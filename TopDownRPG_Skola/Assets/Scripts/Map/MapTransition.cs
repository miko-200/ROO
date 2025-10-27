using System;
using Cinemachine;
using UnityEngine;

public class MapTransition : MonoBehaviour
{
   [SerializeField] private PolygonCollider2D mapBoundry;
   [SerializeField] Direction direction;
   
   private float transitionTime;
   private CinemachineConfiner confiner;
   private enum Direction { Up, Down, Left, Right}

   private void Awake()
   {
      confiner = FindObjectOfType<CinemachineConfiner>();
   }

   private void OnTriggerEnter2D(Collider2D other)
   {
      if (other.gameObject.CompareTag("Player"))
      {
            confiner.m_BoundingShape2D = mapBoundry;
            UpdatePlayerPosition(other.gameObject);
      }
   }

   private void UpdatePlayerPosition(GameObject player)
   {
      Vector3 newPos = player.transform.position;

      switch (direction)
      {
         case Direction.Up:
            newPos.y +=2;
            break;
         case Direction.Down:
            newPos.y -=2;
            break;
         case Direction.Left:
            newPos.x -=2;
            break;
         case Direction.Right:
            newPos.x +=2;
            break;
      }
      player.transform.position = newPos;
   }
   
}
