using System.Collections;
using UnityEngine;

namespace _Scripts.Core
{
    public class SoundController : MonoBehaviour
    {
        public static SoundController Instance;

        [Header("Music Tracks")]
        [Tooltip("10 music iterations - switches every 10 combo")]
        [SerializeField] private AudioClip[] musicTracks = new AudioClip[10];

        [Header("Sound Effects")]
        [SerializeField] private AudioClip deathSoundClip;
        [SerializeField] private float sfxVolume = 1f;

        [Header("Audio Settings")]
        [SerializeField] private float musicVolume = 1f;
        [SerializeField] private float crossfadeDuration = 1f;
        [SerializeField] private bool loopTracks = true;

        [Header("Volume Punch")]
        [SerializeField] private float volumePunchMultiplier = 1.25f;
        [SerializeField] private float volumePunchFadeDuration = 0.15f;

        private AudioSource audioSourceA;
        private AudioSource audioSourceB;
        private AudioSource activeSource;
        private AudioSource sfxSource;
        private int currentTrackIndex = -1;
        private Coroutine crossfadeCoroutine;
        private Coroutine volumePunchCoroutine;

        private void Awake()
        {
            Instance = this;

            // Create two audio sources for crossfading
            audioSourceA = gameObject.AddComponent<AudioSource>();
            audioSourceB = gameObject.AddComponent<AudioSource>();

            audioSourceA.loop = loopTracks;
            audioSourceB.loop = loopTracks;
            audioSourceA.volume = 0f;
            audioSourceB.volume = 0f;

            activeSource = audioSourceA;

            // Create SFX audio source
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }

        private void Start()
        {
            // Start playing the first track
            PlayTrackForCombo(1);
        }

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
                CrossfadeToTrack(trackIndex);
            }
        }

        private void CrossfadeToTrack(int index)
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

            if (crossfadeCoroutine != null)
            {
                StopCoroutine(crossfadeCoroutine);
            }

            crossfadeCoroutine = StartCoroutine(CrossfadeCoroutine(index));
        }

        private IEnumerator CrossfadeCoroutine(int newTrackIndex)
        {
            AudioSource fadeOutSource = activeSource;
            AudioSource fadeInSource = (activeSource == audioSourceA) ? audioSourceB : audioSourceA;

            // Set up the new track at the same playback position
            fadeInSource.clip = musicTracks[newTrackIndex];
            fadeInSource.time = fadeOutSource.time % musicTracks[newTrackIndex].length;
            fadeInSource.Play();

            float startVolume = fadeOutSource.volume;
            float elapsed = 0f;

            while (elapsed < crossfadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / crossfadeDuration;

                fadeOutSource.volume = Mathf.Lerp(startVolume, 0f, t);
                fadeInSource.volume = Mathf.Lerp(0f, musicVolume, t);

                yield return null;
            }

            fadeOutSource.volume = 0f;
            fadeOutSource.Stop();
            fadeInSource.volume = musicVolume;

            activeSource = fadeInSource;
            crossfadeCoroutine = null;

            Debug.Log($"SoundController: Crossfaded to track {newTrackIndex + 1}");
        }

        public void SetVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            if (activeSource != null && activeSource.isPlaying)
            {
                activeSource.volume = musicVolume;
            }
        }

        public void PauseMusic()
        {
            audioSourceA?.Pause();
            audioSourceB?.Pause();
        }

        public void ResumeMusic()
        {
            audioSourceA?.UnPause();
            audioSourceB?.UnPause();
        }

        public void StopMusic()
        {
            audioSourceA?.Stop();
            audioSourceB?.Stop();
        }

        public void PlayDeathSound()
        {
            if (deathSoundClip != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(deathSoundClip, sfxVolume);
            }
        }

        /// <summary>
        /// Returns the currently active AudioSource for spectrum analysis.
        /// Used by AudioSpectrumVisualizer to read audio data.
        /// </summary>
        public AudioSource GetActiveAudioSource()
        {
            return activeSource;
        }

        public void PunchVolume()
        {
            if (activeSource == null || !activeSource.isPlaying) return;

            if (volumePunchCoroutine != null)
            {
                StopCoroutine(volumePunchCoroutine);
            }

            volumePunchCoroutine = StartCoroutine(VolumePunchCoroutine());
        }

        private IEnumerator VolumePunchCoroutine()
        {
            // Immediately boost volume
            float punchedVolume = Mathf.Min(musicVolume * volumePunchMultiplier, 1f);
            activeSource.volume = punchedVolume;

            // Fade back to normal
            float elapsed = 0f;
            while (elapsed < volumePunchFadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / volumePunchFadeDuration;
                activeSource.volume = Mathf.Lerp(punchedVolume, musicVolume, t);
                yield return null;
            }

            activeSource.volume = musicVolume;
            volumePunchCoroutine = null;
        }
    }
}
