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
    
    public GameObject deathScreen;
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
        this.gameObject.GetComponent<Pause>().PauseGame();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ShowGameOverScreen()
    {
        this.gameObject.GetComponent<Pause>().PauseGame();
        CalculateScore();
        deathScreen.SetActive(true);
        scoreTracker.SetActive(false);
    }
}
