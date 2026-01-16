using UnityEngine;

namespace _Scripts.Core
{
    public class SoundController : MonoBehaviour
    {
        public static SoundController Instance;

        [Header("Music Tracks")]
        [Tooltip("10 music iterations - switches every 10 combo")]
        [SerializeField] private AudioClip[] musicTracks = new AudioClip[10];

        [Header("Audio Settings")]
        [SerializeField] private float musicVolume = 1f;
        [SerializeField] private bool loopTracks = true;

        private AudioSource audioSource;
        private int currentTrackIndex = -1;

        private void Awake()
        {
            Instance = this;

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.loop = loopTracks;
            audioSource.volume = musicVolume;
        }

        private void Start()
        {
            // Start playing the first track
            PlayTrackForCombo(1);
        }

        /// <summary>
        /// Call this from GameManager when combo changes to update the music track
        /// </summary>
        public void UpdateMusicForCombo(int combo)
        {
            PlayTrackForCombo(combo);
        }

        private void PlayTrackForCombo(int combo)
        {
            // Calculate track index: every 10 combo = next track
            // Combo 1-10 = track 0, 11-20 = track 1, etc.
            int trackIndex = Mathf.Clamp((combo - 1) / 10, 0, musicTracks.Length - 1);

            // Only switch if track index changed
            if (trackIndex != currentTrackIndex)
            {
                currentTrackIndex = trackIndex;
                PlayTrack(trackIndex);
            }
        }

        private void PlayTrack(int index)
        {
            if (index < 0 || index >= musicTracks.Length)
            {
                Debug.LogWarning($"SoundController: Track index {index} out of range");
                return;
            }

            if (musicTracks[index] == null)
            {
                Debug.LogWarning($"SoundController: Track {index} is not assigned");
                return;
            }

            audioSource.clip = musicTracks[index];
            audioSource.Play();
            Debug.Log($"SoundController: Playing track {index + 1} for combo tier");
        }

        public void SetVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            if (audioSource != null)
            {
                audioSource.volume = musicVolume;
            }
        }

        public void PauseMusic()
        {
            audioSource?.Pause();
        }

        public void ResumeMusic()
        {
            audioSource?.UnPause();
        }

        public void StopMusic()
        {
            audioSource?.Stop();
        }
    }
}
