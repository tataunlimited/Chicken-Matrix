using System;
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
        
        public GameObject easyModeEndingTxt;
        public GameObject hardModeEndingTxt;
        public GameObject konamiModeEndingTxt;

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
            
            switch (GameManager.Difficulty)
            {
                case Difficulty.Easy:
                    easyModeEnding.SetActive(true);
                    break;
                case Difficulty.Hard:
                    hardModeEnding.SetActive(true);
                    break;
                case Difficulty.KonamiMode:
                    konamiModeEnding.SetActive(true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            yield return new WaitForSeconds(delayToFadeToBlack);
            fadeToBlack.DOFade(1, 1);
        
            yield return new WaitForSeconds(1);
            
            switch (GameManager.Difficulty)
            {
                case Difficulty.Easy:
                    easyModeEndingTxt.SetActive(true);
                    break;
                case Difficulty.Hard:
                    hardModeEndingTxt.SetActive(true);
                    break;
                case Difficulty.KonamiMode:
                    konamiModeEndingTxt.SetActive(true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            yield return new WaitForSeconds(delayAfterFadeToBlack);
            
            SceneManager.LoadScene("MainMenu");
        }

       
    }
}
