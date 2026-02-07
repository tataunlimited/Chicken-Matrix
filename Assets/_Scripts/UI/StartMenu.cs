using System;
using _Scripts.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Scripts.UI
{
    public class StartMenu : MonoBehaviour
    {

        public GameObject difficultyMenu;
        public GameObject startMenu;

        public GameObject easyModeTrophy;
        public GameObject hardModeTrophy;
        public GameObject konamiModeTrophy;

        [Header("High Score")]
        public TMP_Text eggHighScoreText;


        private void Start()
        {
            easyModeTrophy.SetActive(PlayerPrefs.GetInt("Trophy_Easy") == 1);
            hardModeTrophy.SetActive(PlayerPrefs.GetInt("Trophy_Hard") == 1);
            konamiModeTrophy.SetActive(PlayerPrefs.GetInt("Trophy_Konami") == 1);

            // Display egg high score
            if (eggHighScoreText != null)
            {
                int highScore = EggManager.HighScore;
                eggHighScoreText.text = highScore.ToString();
            }
        }

        public void OnStartGame()
        {
            difficultyMenu.SetActive(true);
        }
        
        public void OnBackToMenu()
        {
            difficultyMenu.SetActive(false);
            startMenu.SetActive(true);
        }

        public void OnQuitGame()
        {
            Application.Quit();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OnBackToMenu();
            }
        }

        public void OnEasyMode()
        {
            GameManager.Difficulty = Difficulty.Easy;
            SceneManager.LoadScene("GameScene");
        }

        public void OnHardMode()
        {
            GameManager.Difficulty = Difficulty.Hard;
            SceneManager.LoadScene("GameScene");
        }
    }
}
