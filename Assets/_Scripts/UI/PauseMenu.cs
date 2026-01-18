using _Scripts.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{

    public GameObject pauseMenu;
    private bool _isPaused = false;

    // Update is called once per frame
    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    private void Pause()
    {
        _isPaused = true;
        pauseMenu.SetActive(true);
        Time.timeScale = 0f;

        // Pause game music and play pause menu music
        if (SoundController.Instance != null)
        {
            SoundController.Instance.PauseMusic();
            SoundController.Instance.PlayPauseMenuMusic();
        }
    }

    public void Resume()
    {
        _isPaused = false;
        pauseMenu.SetActive(false);
        Time.timeScale = 1f;

        // Stop pause menu music and resume game music
        if (SoundController.Instance != null)
        {
            SoundController.Instance.StopPauseMenuMusic();
            SoundController.Instance.ResumeMusic();
        }
    }

    public void OnMainMenu()
    {
        // Stop pause menu music before leaving
        if (SoundController.Instance != null)
        {
            SoundController.Instance.StopPauseMenuMusic();
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
