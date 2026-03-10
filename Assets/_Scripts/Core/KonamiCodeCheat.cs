using UnityEngine;
using System.Collections.Generic;
using _Scripts.Core;

public class KonamiCodeCheat : MonoBehaviour
{
    // The defined Konami Code sequence
    private List<KeyCode> konamiCode;
    
    // Tracks current position in the sequence
    private int _currentIndex = 0;

    void Start()
    {
        // Initialize the code: Up, Up, Down, Down, Left, Right, Left, Right, B, A, Start
        konamiCode = new List<KeyCode>()
        {
            KeyCode.UpArrow,
            KeyCode.UpArrow,
            KeyCode.DownArrow,
            KeyCode.DownArrow,
            KeyCode.LeftArrow,
            KeyCode.RightArrow,
            KeyCode.LeftArrow,
            KeyCode.RightArrow,
            KeyCode.B,
            KeyCode.A,
            KeyCode.Return // "Enter" acts as Start
        };
    }

    void Update()
    {
        // Optimization: Only run logic if a key was actually pressed this frame
        if (Input.anyKeyDown)
        {
            // Check if the key pressed matches the ONE we are currently looking for
            if (Input.GetKeyDown(konamiCode[_currentIndex]))
            {
                // Correct key pressed: Move to next step
                _currentIndex++;
                
                //Debug.Log($"Konami Code Progress: {_currentIndex}/{konamiCode.Count}");
            }
            else
            {
                // Wrong key pressed: Reset the sequence!
                // (Unless the wrong key happened to be 'UpArrow', in which case we restart at 1)
                _currentIndex = Input.GetKeyDown(konamiCode[0]) ? 1 : 0;
            }

            // Check if the sequence is complete
            if (_currentIndex == konamiCode.Count)
            {
                TriggerCheatEffect();
                _currentIndex = 0; // Reset for next time
            }
        }
    }

    private void TriggerCheatEffect()
    {
        GameManager.Instance.TriggerKonamiEffect();
    }
}