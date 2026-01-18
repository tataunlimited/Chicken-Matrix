using System.Collections;
using _Scripts.Core;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace _Scripts.UI
{
    public class CutSceneManager : MonoBehaviour
    {
        public GameObject easyModeEnding;
        public GameObject hardModeEnding;
        public GameObject konamiModeEnding;

        public Image fadeToBlack;
        public float delayToFadeToBlack = 4;
        public float delayAfterFadeToBlack = 9;

        public bool overrideDifficulty;
        public Difficulty difficulty;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        IEnumerator Start()
        {
            
            if (overrideDifficulty)
            {
                GameManager.Difficulty = difficulty;
            }
            
            
            if (GameManager.Difficulty == Difficulty.Easy)
            {
                easyModeEnding.SetActive(true);
            }
            else if(GameManager.Difficulty == Difficulty.Hard)
            {
                hardModeEnding.SetActive(true);
            }
            else
            {
                konamiModeEnding.SetActive(true);
            }
            yield return new WaitForSeconds(delayToFadeToBlack);
            fadeToBlack.DOFade(1, 1);
        
            yield return new WaitForSeconds(delayAfterFadeToBlack);

            SceneManager.LoadScene("MainMenu");
        }

       
    }
}
