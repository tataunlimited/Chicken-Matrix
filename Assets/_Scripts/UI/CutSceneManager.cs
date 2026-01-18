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

        public Image fadeToBlack;
        public float delayToFadeToBlack = 4;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        IEnumerator Start()
        {

            if (GameManager.Difficulty == Difficulty.Easy)
            {
                easyModeEnding.SetActive(true);
                hardModeEnding.SetActive(false);
            }
            else
            {
                easyModeEnding.SetActive(false);
                hardModeEnding.SetActive(true);
            }
            yield return new WaitForSeconds(delayToFadeToBlack);
            fadeToBlack.DOFade(1, 1);
        
            yield return new WaitForSeconds(1);

            SceneManager.LoadScene("MainMenu");
        }

       
    }
}
