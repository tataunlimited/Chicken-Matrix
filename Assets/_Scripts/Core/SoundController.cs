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
        [Tooltip("Final victory track - plays when reaching 100 combo")]
        [SerializeField] private AudioClip finalTrack;

        [Header("Sound Effects")]
        [SerializeField] private AudioClip enemyDeathSoundClip;
        [SerializeField] private AudioClip allyDeathSoundClip;
        [SerializeField] private AudioClip neutralDeathSoundClip;
        [SerializeField] private AudioClip comboFailSoundClip;
        [SerializeField] private AudioClip directionFlipSoundClip;
        [SerializeField] private AudioClip rankUpSoundClip;
        [SerializeField] private AudioClip buttonHoverSoundClip;
        [SerializeField] private AudioClip buttonClickSoundClip;
        [SerializeField] private AudioClip eggCollectSoundClip;
        [SerializeField] private AudioClip eggBadCollectSoundClip;
        [SerializeField] private float sfxVolume = 1f;
        [SerializeField] private float buttonHoverPitch = 1f;
        [SerializeField] private float buttonClickPitch = 1f;

        [Header("Audio Settings")]
        [SerializeField] private float musicVolume = 1f;
        [SerializeField] private float crossfadeDuration = 1f;
        [SerializeField] private bool loopTracks = true;
        [Tooltip("Volume multiplier applied per track (e.g., 1.15 = 15% louder each track)")]
        [SerializeField] private float volumeIncreasePerTrack = 1.15f;

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
        private float currentTrackVolume; // Cached volume for current track

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

        /// <summary>
        /// Calculate the volume for a given track index.
        /// Each track is 15% louder than the previous (configurable via volumeIncreasePerTrack).
        /// </summary>
        private float GetVolumeForTrack(int trackIndex)
        {
            // Track 0 = musicVolume * 1.0
            // Track 1 = musicVolume * 1.15
            // Track 2 = musicVolume * 1.15^2
            // etc.
            return Mathf.Min(musicVolume * Mathf.Pow(volumeIncreasePerTrack, trackIndex), 1f);
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

            // Calculate the target volume for the new track
            float targetVolume = GetVolumeForTrack(newTrackIndex);

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
                fadeInSource.volume = Mathf.Lerp(0f, targetVolume, t);

                yield return null;
            }

            fadeOutSource.volume = 0f;
            fadeOutSource.Stop();
            fadeInSource.volume = targetVolume;
            currentTrackVolume = targetVolume;

            activeSource = fadeInSource;
            crossfadeCoroutine = null;

            Debug.Log($"SoundController: Crossfaded to track {newTrackIndex + 1} (volume: {targetVolume:F2})");
        }

        public void SetVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            // Recalculate current track volume based on new base volume
            if (currentTrackIndex >= 0)
            {
                currentTrackVolume = GetVolumeForTrack(currentTrackIndex);
            }
            if (activeSource != null && activeSource.isPlaying)
            {
                activeSource.volume = currentTrackVolume;
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

        public void PlayEnemyDeathSound()
        {
            if (enemyDeathSoundClip != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(enemyDeathSoundClip, sfxVolume);
            }
        }

        public void PlayAllyDeathSound()
        {
            if (allyDeathSoundClip != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(allyDeathSoundClip, sfxVolume);
            }
        }

        public void PlayNeutralDeathSound()
        {
            if (neutralDeathSoundClip != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(neutralDeathSoundClip, sfxVolume);
            }
        }

        public void PlayComboFailSound()
        {
            if (comboFailSoundClip != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(comboFailSoundClip, sfxVolume);
            }
        }

        public void PlayDirectionFlipSound()
        {
            if (directionFlipSoundClip != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(directionFlipSoundClip, sfxVolume);
            }
        }

        public void PlayRankUpSound()
        {
            if (rankUpSoundClip != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(rankUpSoundClip, sfxVolume);
            }
        }

        public void PlayButtonHoverSound()
        {
            if (buttonHoverSoundClip != null && sfxSource != null)
            {
                sfxSource.pitch = buttonHoverPitch;
                sfxSource.PlayOneShot(buttonHoverSoundClip, sfxVolume);
                sfxSource.pitch = 1f;
            }
        }

        public void PlayButtonClickSound()
        {
            if (buttonClickSoundClip != null && sfxSource != null)
            {
                sfxSource.pitch = buttonClickPitch;
                sfxSource.PlayOneShot(buttonClickSoundClip, sfxVolume);
                sfxSource.pitch = 1f;
            }
        }

        public void PlayEggCollectSound()
        {
            if (eggCollectSoundClip != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(eggCollectSoundClip, sfxVolume);
            }
        }

        public void PlayEggBadCollectSound()
        {
            if (eggBadCollectSoundClip != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(eggBadCollectSoundClip, sfxVolume);
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
            // Immediately boost volume based on current track's volume
            float punchedVolume = Mathf.Min(currentTrackVolume * volumePunchMultiplier, 1f);
            activeSource.volume = punchedVolume;

            // Fade back to normal
            float elapsed = 0f;
            while (elapsed < volumePunchFadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / volumePunchFadeDuration;
                activeSource.volume = Mathf.Lerp(punchedVolume, currentTrackVolume, t);
                yield return null;
            }

            activeSource.volume = currentTrackVolume;
            volumePunchCoroutine = null;
        }

        /// <summary>
        /// Sync the music position to match a specific combo value.
        /// Called when combo resets to align music with gameplay timing.
        /// Each segment is 16 seconds, each combo point ~1.6 seconds.
        /// </summary>
        public void SetTrackTimeForCombo(int combo)
        {
            int comboInSegment = (combo - 1) % 10; // 0-9
            float timeInSegment = comboInSegment * 1.6f;

            if (activeSource != null && activeSource.clip != null)
            {
                activeSource.time = timeInSegment % activeSource.clip.length;
            }
        }

        /// <summary>
        /// Play the final victory track (non-looping).
        /// Returns the track duration, or 0 if no track is assigned.
        /// </summary>
        public float PlayFinalTrack()
        {
            if (finalTrack == null)
            {
                Debug.LogWarning("SoundController: Final track is not assigned");
                return 0f;
            }

            if (crossfadeCoroutine != null)
            {
                StopCoroutine(crossfadeCoroutine);
            }

            crossfadeCoroutine = StartCoroutine(CrossfadeToFinalTrack());
            return finalTrack.length;
        }

        private IEnumerator CrossfadeToFinalTrack()
        {
            AudioSource fadeOutSource = activeSource;
            AudioSource fadeInSource = (activeSource == audioSourceA) ? audioSourceB : audioSourceA;

            // Final track plays at the highest track volume (track 9 equivalent)
            float finalVolume = GetVolumeForTrack(musicTracks.Length - 1);

            // Set up the final track (non-looping, start from beginning)
            fadeInSource.clip = finalTrack;
            fadeInSource.loop = false;
            fadeInSource.time = 0f;
            fadeInSource.Play();

            float startVolume = fadeOutSource.volume;
            float elapsed = 0f;

            while (elapsed < crossfadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / crossfadeDuration;

                fadeOutSource.volume = Mathf.Lerp(startVolume, 0f, t);
                fadeInSource.volume = Mathf.Lerp(0f, finalVolume, t);

                yield return null;
            }

            fadeOutSource.volume = 0f;
            fadeOutSource.Stop();
            fadeInSource.volume = finalVolume;
            currentTrackVolume = finalVolume;

            activeSource = fadeInSource;
            crossfadeCoroutine = null;

            Debug.Log($"SoundController: Crossfaded to final track (volume: {finalVolume:F2})");
        }

        /// <summary>
        /// Gets the remaining time of the currently playing track.
        /// </summary>
        public float GetRemainingTrackTime()
        {
            if (activeSource != null && activeSource.clip != null && activeSource.isPlaying)
            {
                return activeSource.clip.length - activeSource.time;
            }
            return 0f;
        }
    }
}
