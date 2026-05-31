using UnityEngine;
using UnityEngine.SceneManagement;

namespace SupKonQuest
{
    public class GameOptionsMenu : MonoBehaviour
    {
        [Header("Touche d'ouverture (défaut : Escape)")]
        public KeyCode toggleKey = KeyCode.Escape;

        private bool isOpen;

        private float musicVolume;
        private float sfxVolume;
        private float hudScale;

        private GUIStyle panelStyle;
        private GUIStyle titleStyle;
        private GUIStyle labelStyle;
        private GUIStyle btnStyle;
        private GUIStyle menuBtnStyle;
        private GUIStyle quitBtnStyle;
        private bool stylesReady;

        private static string L(string key) => LocalizationManager.Get(key);

        private void Start()
        {
            musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
            sfxVolume   = PlayerPrefs.GetFloat("SFXVolume",   1f);
            hudScale    = PlayerPrefs.GetFloat("HUDScale",    1f);
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                isOpen = !isOpen;
                Time.timeScale = isOpen ? 0f : 1f;
            }
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
        }

        private void OnGUI()
        {
            if (!isOpen) return;
            InitStyles();

            Matrix4x4 oldMatrix = GUI.matrix;
            float s = HUDManager.HudScale;
            GUIUtility.ScaleAroundPivot(new Vector2(s, s), Vector2.zero);
            float sw = Screen.width  / s;
            float sh = Screen.height / s;

            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float pw = 420f;
            float ph = 390f;
            float px = (sw - pw) * 0.5f;
            float py = (sh - ph) * 0.5f;

            GUI.Box(new Rect(px - 15, py - 15, pw + 30, ph + 30), GUIContent.none, panelStyle);

            float y = py;
            GUI.Label(new Rect(px, y, pw, 32f), L("options"), titleStyle);
            y += 40f;

            GUI.Label(new Rect(px, y, pw, 22f), $"{L("options_music")} : {Mathf.RoundToInt(musicVolume * 100f)}%", labelStyle);
            y += 26f;
            float newMusic = GUI.HorizontalSlider(new Rect(px, y, pw, 18f), musicVolume, 0f, 1f);
            if (!Mathf.Approximately(newMusic, musicVolume))
            {
                musicVolume = newMusic;
                AudioManager.Instance?.SetMusicVolume(musicVolume);
                PlayerPrefs.SetFloat("MusicVolume", musicVolume);
            }
            y += 32f;

            GUI.Label(new Rect(px, y, pw, 22f), $"{L("options_sfx")} : {Mathf.RoundToInt(sfxVolume * 100f)}%", labelStyle);
            y += 26f;
            float newSFX = GUI.HorizontalSlider(new Rect(px, y, pw, 18f), sfxVolume, 0f, 1f);
            if (!Mathf.Approximately(newSFX, sfxVolume))
            {
                sfxVolume = newSFX;
                AudioManager.Instance?.SetSFXVolume(sfxVolume);
                PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
            }
            y += 32f;

            GUI.Label(new Rect(px, y, pw, 22f), $"Taille HUD : {Mathf.RoundToInt(hudScale * 100f)}%", labelStyle);
            y += 26f;
            float newHud = GUI.HorizontalSlider(new Rect(px, y, pw, 18f), hudScale, 0.5f, 2f);
            if (!Mathf.Approximately(newHud, hudScale))
            {
                hudScale = newHud;
                PlayerPrefs.SetFloat("HUDScale", hudScale);
            }
            y += 40f;

            float bw = 190f;
            float gap = 20f;
            float totalW = bw * 2f + gap;
            float bx = (sw - totalW) * 0.5f;

            if (GUI.Button(new Rect(bx, y, bw, 44f), L("options_close"), btnStyle))
            {
                AudioManager.Instance?.PlayClick();
                PlayerPrefs.Save();
                isOpen = false;
                Time.timeScale = 1f;
            }

            GUI.color = new Color(0.3f, 0.7f, 1f);
            if (GUI.Button(new Rect(bx + bw + gap, y, bw, 44f), L("main_menu"), menuBtnStyle))
            {
                PlayerPrefs.Save();
                Time.timeScale = 1f;
                SceneManager.LoadScene("MainMenu");
            }
            GUI.color = Color.white;
            y += 56f;

            float qw = 200f;
            float qx = (sw - qw) * 0.5f;
            GUI.color = new Color(1f, 0.35f, 0.35f);
            if (GUI.Button(new Rect(qx, y, qw, 36f), L("quit_game"), quitBtnStyle))
            {
                PlayerPrefs.Save();
                Time.timeScale = 1f;
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
            GUI.color = Color.white;
            GUI.matrix = oldMatrix;
        }

        private void InitStyles()
        {
            if (stylesReady) return;
            stylesReady = true;

            panelStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeTex(new Color(0.1f, 0.1f, 0.2f, 0.97f)) }
            };

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = new Color(0.9f, 0.75f, 0.2f) }
            };

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 14,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = new Color(0.85f, 0.85f, 0.85f) }
            };

            btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize  = 15,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = Color.white }
            };

            menuBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize  = 15,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = Color.white }
            };

            quitBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize  = 13,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = Color.white }
            };
        }

        private static Texture2D MakeTex(Color col)
        {
            Texture2D t = new Texture2D(1, 1);
            t.SetPixel(0, 0, col);
            t.Apply();
            return t;
        }
    }
}
