using UnityEngine;

public class Pause : MonoBehaviour
{
   private bool isPaused = false;
   
   public void PauseGame()
   {
      if (!isPaused)
      {
         Time.timeScale = 0;
      }
      else
      {
         Time.timeScale = 1;
      }
      isPaused = !isPaused;
   }
}
