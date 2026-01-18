using System;
using _Scripts.Core;
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


        private void Start()
        {
            easyModeTrophy.SetActive(PlayerPrefs.GetInt("Trophy_Easy") == 1);
            hardModeTrophy.SetActive(PlayerPrefs.GetInt("Trophy_Hard") == 1);
            konamiModeTrophy.SetActive(PlayerPrefs.GetInt("Trophy_Konami") == 1);
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
