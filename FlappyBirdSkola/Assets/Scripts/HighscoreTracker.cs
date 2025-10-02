using UnityEngine;

public class HighscoreTracker : MonoBehaviour
{
    
    public int highscore;
    public void CheckHighscore(int score)
    {
        if (score > highscore)
        {
            highscore = score;
        } 
    }
}
