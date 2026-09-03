using TMPro;
using UnityEngine;

public class ScoreCounter : MonoBehaviour
{
    public TextMeshProUGUI scoreText;
    
    public int score;
    
    private void Start()
    {
        scoreText.text = score.ToString(); //sets score at start
    }

    public void AddScore(int scoreToAdd) //adds and sets score
    {
        score += scoreToAdd;
        scoreText.text = score.ToString();
    }
    
}
