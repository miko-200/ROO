using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOver : MonoBehaviour
{
    public ScoreCounter scoreTrackerS;
    public HighscoreTracker highscoreTrackerS;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI highscoreText;
    
    public GameObject deathScreen; //shadow over the screen, score & highsocre text in middle, home and retry button
    public GameObject scoreTracker;
    
    private void CalculateScore()
    {
        highscoreTrackerS = GameObject.FindGameObjectWithTag("HighscoreTracker").GetComponent<HighscoreTracker>();
        if (highscoreTrackerS != null)
        {
            int score = scoreTrackerS.GetComponent<ScoreCounter>().score;
            highscoreTrackerS.CheckHighscore(score);

            scoreText.text = "Score\n" + score;
            highscoreText.text = "Highscore\n" + highscoreTrackerS.highscore;
        }
        else
        {
            Debug.LogWarning("No HighscoreTracker Found");
        }
    }

    public void Retry()
    {
        this.gameObject.GetComponent<Pause>().PauseGame(); //unpauses game
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ShowGameOverScreen()
    {
        this.gameObject.GetComponent<Pause>().PauseGame(); //pauses the game
        CalculateScore();
        deathScreen.SetActive(true); // enables death screen
        scoreTracker.SetActive(false); //disables score text
    }
}
