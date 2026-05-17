using UnityEngine;

namespace SupKonQuest
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance;

        [Header("Music")]
        public AudioSource musicSource;
        public AudioClip menuMusic;
        public AudioClip gameMusic;

        [Header("SFX")]
        public AudioSource sfxSource;
        public AudioClip clickSound;
        public AudioClip attackSound;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void PlayMenuMusic()
        {
            PlayMusic(menuMusic);
        }

        public void PlayGameMusic()
        {
            PlayMusic(gameMusic);
        }

        private void PlayMusic(AudioClip clip)
        {
            if (clip == null || musicSource == null) return;

            if (musicSource.clip == clip && musicSource.isPlaying) return;

            musicSource.clip = clip;
            musicSource.loop = true;
            musicSource.Play();
        }

        public void PlayClick()
        {
            PlaySFX(clickSound);
        }

        public void PlayAttack()
        {
            PlaySFX(attackSound);
        }

        private void PlaySFX(AudioClip clip)
        {
            if (clip == null || sfxSource == null) return;
            sfxSource.PlayOneShot(clip);
        }
    }
}