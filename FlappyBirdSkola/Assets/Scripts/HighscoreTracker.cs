using UnityEngine;

public class HighscoreTracker : MonoBehaviour
{
    
    public int highscore; //Stores highscore
    public void CheckHighscore(int score) //Checks if current score is more than highscore, if yes changes highscore to the current score
    {
        if (score > highscore)
        {
            highscore = score;
        } 
    }
}
