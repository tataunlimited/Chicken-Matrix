using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class CutSceneManager : MonoBehaviour
{

    public Image fadeToBlack;
    public float delayToFadeToBlack = 4;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    IEnumerator Start()
    {
        
        yield return new WaitForSeconds(delayToFadeToBlack);
        fadeToBlack.DOFade(1, 1);

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
