using UnityEngine;
using UnityEngine.SceneManagement;
using SupKonQuest;

public class MainMenu : MonoBehaviour
{
    private enum MenuScreen { Title, Selection, Options }
    private MenuScreen current = MenuScreen.Title;

    private int selectedMap  = 0;
    private int selectedAI   = 1;
    private int selectedDiff = 0;
    private int selectedLang = 0;
    private int selectedRace = 0;

    private static readonly Color[] raceColors =
    {
        new Color(0.3f, 0.5f, 1.0f),
        new Color(0.2f, 0.85f, 0.3f),
        new Color(1.0f, 0.25f, 0.25f)
    };

    private readonly string[] langCodes  = { "fr", "en", "es" };
    private readonly string[] langLabels = { "FR", "EN", "ES" };

    // Options (volume 0–1)
    private float musicVolume = 1f;
    private float sfxVolume   = 1f;

    private Texture2D backgroundTexture;

    private GUIStyle titleStyle;
    private GUIStyle subtitleStyle;
    private GUIStyle labelStyle;
    private GUIStyle mainBtnStyle;
    private GUIStyle buttonStyle;
    private GUIStyle selectedButtonStyle;
    private GUIStyle panelStyle;
    private GUIStyle quitBtnStyle;
    private GUIStyle sliderLabelStyle;

    private void Awake()
    {
        // Charge l'image depuis Assets/Resources/
        backgroundTexture = Resources.Load<Texture2D>("background-principal");

        if (LocalizationManager.Instance == null)
        {
            GameObject go = new GameObject("LocalizationManager");
            go.AddComponent<LocalizationManager>();
        }

        string savedLang = PlayerPrefs.GetString("Language", "fr");
        for (int i = 0; i < langCodes.Length; i++)
            if (langCodes[i] == savedLang) { selectedLang = i; break; }

        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        sfxVolume   = PlayerPrefs.GetFloat("SFXVolume",   1f);
        AudioManager.Instance?.SetMusicVolume(musicVolume);
        AudioManager.Instance?.SetSFXVolume(sfxVolume);
        AudioManager.Instance?.PlayMenuMusic();
    }

    private void OnGUI()
    {
        InitStyles();

        // 1. Image de fond plein écran
        if (backgroundTexture != null)
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height),
                            backgroundTexture, ScaleMode.ScaleAndCrop);

        // 2. Overlay sombre pour lisibilité
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        switch (current)
        {
            case MenuScreen.Title:     DrawTitleScreen();     break;
            case MenuScreen.Selection: DrawSelectionScreen(); break;
            case MenuScreen.Options:   DrawOptionsScreen();   break;
        }
    }

    private void DrawTitleScreen()
    {
        float sw = Screen.width;
        float sh = Screen.height;

        // Ombre derrière le titre pour lisibilité
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(new Rect(0, sh * 0.08f, sw, 140f), Texture2D.whiteTexture);
        GUI.color = Color.white;


        GUI.Label(new Rect(0, sh * 0.12f, sw, 90f), "SupKonQuest", titleStyle);
        GUI.Label(new Rect(0, sh * 0.12f + 90f, sw, 30f),
                  "Stratégie — Conquête — Tactique", subtitleStyle);

        float btnW = 280f;
        float btnH = 58f;
        float btnX = (sw - btnW) * 0.5f;
        float btnY = sh * 0.44f;
        float gap  = 18f;

        if (GUI.Button(new Rect(btnX, btnY, btnW, btnH), L("play"), mainBtnStyle))
        {
            AudioManager.Instance?.PlayClick();
            current = MenuScreen.Selection;
        }

        btnY += btnH + gap;

        if (GUI.Button(new Rect(btnX, btnY, btnW, btnH), L("options"), mainBtnStyle))
        {
            AudioManager.Instance?.PlayClick();
            current = MenuScreen.Options;
        }

        btnY += btnH + gap;

        GUI.color = new Color(1f, 0.35f, 0.35f);
        if (GUI.Button(new Rect(btnX, btnY, btnW, btnH), L("quit"), quitBtnStyle))
        {
            AudioManager.Instance?.PlayClick();
            Application.Quit();
        }
        GUI.color = Color.white;
    }

    private void DrawSelectionScreen()
    {
        float sw = Screen.width;
        float sh = Screen.height;

        // Ombre derrière le titre
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(new Rect(0, sh * 0.02f, sw, 80f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.Label(new Rect(0, sh * 0.04f, sw, 70f), "SupKonQuest", titleStyle);

        float panelW = 500f;
        float panelH = 510f;
        float panelX = (sw - panelW) / 2f;
        float panelY = sh * 0.16f;

        GUI.Box(new Rect(panelX - 15, panelY - 15, panelW + 30, panelH + 30), "", panelStyle);

        // Langue
        GUI.Label(new Rect(panelX, panelY, panelW, 28f), L("language"), labelStyle);
        panelY += 32f;
        for (int i = 0; i < langLabels.Length; i++)
        {
            GUIStyle style = selectedLang == i ? selectedButtonStyle : buttonStyle;
            if (GUI.Button(new Rect(panelX + i * 80f, panelY, 72f, 34f), langLabels[i], style))
            {
                AudioManager.Instance?.PlayClick();
                selectedLang = i;
                LocalizationManager.Instance?.LoadLanguage(langCodes[i]);
            }
        }
        panelY += 46f;

        // Carte
        GUI.Label(new Rect(panelX, panelY, panelW, 28f), L("map"), labelStyle);
        panelY += 32f;
        string[] mapKeys = { "map_classic", "map_frozen", "map_island" };
        for (int i = 0; i < mapKeys.Length; i++)
        {
            GUIStyle style = selectedMap == i ? selectedButtonStyle : buttonStyle;
            if (GUI.Button(new Rect(panelX + i * 168f, panelY, 160f, 38f), L(mapKeys[i]), style))
            {
                AudioManager.Instance?.PlayClick();
                selectedMap = i;
            }
        }
        panelY += 50f;

        // Adversaires IA
        GUI.Label(new Rect(panelX, panelY, panelW, 28f), L("opponents"), labelStyle);
        panelY += 32f;
        for (int i = 1; i <= 3; i++)
        {
            GUIStyle style = selectedAI == i ? selectedButtonStyle : buttonStyle;
            if (GUI.Button(new Rect(panelX + (i - 1) * 100f, panelY, 90f, 38f), i.ToString(), style))
            {
                AudioManager.Instance?.PlayClick();
                selectedAI = i;
            }
        }
        panelY += 50f;

        // Difficulté
        GUI.Label(new Rect(panelX, panelY, panelW, 28f), L("difficulty"), labelStyle);
        panelY += 32f;
        string[] diffKeys = { "diff_easy", "diff_medium", "diff_hard" };
        for (int i = 0; i < diffKeys.Length; i++)
        {
            GUIStyle style = selectedDiff == i ? selectedButtonStyle : buttonStyle;
            if (GUI.Button(new Rect(panelX + i * 168f, panelY, 160f, 38f), L(diffKeys[i]), style))
            {
                AudioManager.Instance?.PlayClick();
                selectedDiff = i;
            }
        }
        panelY += 62f;

        // Race
        GUI.Label(new Rect(panelX, panelY, panelW, 28f), L("race"), labelStyle);
        panelY += 32f;
        string[] raceKeys = { "race_human", "race_elf", "race_demon" };
        for (int i = 0; i < raceKeys.Length; i++)
        {
            Color raceCol = raceColors[i];
            GUI.color = (i == selectedRace)
                ? raceCol
                : new Color(raceCol.r * 0.4f, raceCol.g * 0.4f, raceCol.b * 0.4f);
            if (GUI.Button(new Rect(panelX + i * 168f, panelY, 160f, 38f), L(raceKeys[i]), selectedButtonStyle))
            {
                AudioManager.Instance?.PlayClick();
                selectedRace = i;
            }
        }
        GUI.color = Color.white;
        panelY += 50f;

        // Boutons Retour + Lancer
        float bw     = 200f;
        float totalW = bw * 2f + 16f;
        float startX = (sw - totalW) * 0.5f;

        if (GUI.Button(new Rect(startX, panelY, bw, 50f), "← " + L("back"), buttonStyle))
        {
            AudioManager.Instance?.PlayClick();
            current = MenuScreen.Title;
        }

        GUI.color = new Color(0.2f, 0.8f, 0.3f);
        if (GUI.Button(new Rect(startX + bw + 16f, panelY, bw, 50f),
                       L("play") + " !", selectedButtonStyle))
        {
            AudioManager.Instance?.PlayClick();
            StartGame();
        }
        GUI.color = Color.white;
    }

    // ── Écran options ─────────────────────────────────────────────────

    private void DrawOptionsScreen()
    {
        float sw = Screen.width;
        float sh = Screen.height;

        GUI.Label(new Rect(0, sh * 0.12f, sw, 70f), L("options"), titleStyle);

        float panelW = 400f;
        float panelH = 200f;
        float panelX = (sw - panelW) / 2f;
        float panelY = sh * 0.38f;

        GUI.Box(new Rect(panelX - 15, panelY - 15, panelW + 30, panelH + 30), "", panelStyle);

        // Musique
        GUI.Label(new Rect(panelX, panelY, panelW, 28f), $"{L("options_music")} : {Mathf.RoundToInt(musicVolume * 100f)}%", labelStyle);
        panelY += 32f;
        float newMusic = GUI.HorizontalSlider(new Rect(panelX, panelY, panelW, 20f), musicVolume, 0f, 1f);
        if (!Mathf.Approximately(newMusic, musicVolume))
        {
            musicVolume = newMusic;
            AudioManager.Instance?.SetMusicVolume(musicVolume);
            PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        }
        panelY += 36f;

        // Sons
        GUI.Label(new Rect(panelX, panelY, panelW, 28f), $"{L("options_sfx")} : {Mathf.RoundToInt(sfxVolume * 100f)}%", labelStyle);
        panelY += 32f;
        float newSFX = GUI.HorizontalSlider(new Rect(panelX, panelY, panelW, 20f), sfxVolume, 0f, 1f);
        if (!Mathf.Approximately(newSFX, sfxVolume))
        {
            sfxVolume = newSFX;
            AudioManager.Instance?.SetSFXVolume(sfxVolume);
            PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        }
        panelY += 50f;

        float bw = 220f;
        float bx = (sw - bw) * 0.5f;
        if (GUI.Button(new Rect(bx, panelY, bw, 50f), "← " + L("back"), buttonStyle))
        {
            AudioManager.Instance?.PlayClick();
            PlayerPrefs.Save();
            current = MenuScreen.Title;
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────


    private static string L(string key) => LocalizationManager.Get(key);

    private void StartGame()
    {
        PlayerPrefs.SetInt("MapType",      selectedMap);
        PlayerPrefs.SetInt("AICount",      selectedAI);
        PlayerPrefs.SetInt("AIDifficulty", selectedDiff);
        PlayerPrefs.SetInt("PlayerRace",   selectedRace);
        PlayerPrefs.SetString("Language",  langCodes[selectedLang]);
        PlayerPrefs.Save();
        string[] sceneNames = { "Classic", "Frozen_Peak", "Islands" };
        AudioManager.Instance?.PlayGameMusic();
        SceneManager.LoadScene(sceneNames[selectedMap]);
    }

    private void InitStyles()
    {
        if (titleStyle != null) return;

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 62,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = new Color(1f, 0.85f, 0.2f) }
        };

        subtitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 16,
            fontStyle = FontStyle.Italic,
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = new Color(1f, 1f, 1f) }
        };

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 18,
            fontStyle = FontStyle.Bold,
            normal    = { textColor = new Color(1f, 0.85f, 0.2f) }
        };

        sliderLabelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 14,
            normal    = { textColor = new Color(0.75f, 0.75f, 0.75f) }
        };

        mainBtnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize  = 22,
            fontStyle = FontStyle.Bold,
            normal    = { textColor = new Color(1f, 0.85f, 0.2f) }
        };

        quitBtnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize  = 22,
            fontStyle = FontStyle.Bold,
            normal    = { textColor = Color.white }
        };

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize  = 15,
            fontStyle = FontStyle.Bold,
            normal    = { textColor = new Color(0.85f, 0.85f, 0.85f) }
        };

        selectedButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize  = 15,
            fontStyle = FontStyle.Bold,
            normal    = { textColor = new Color(1f, 0.85f, 0.1f) }
        };

        panelStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = MakeTex(1, 1, new Color(0.05f, 0.03f, 0.0f, 0.92f)) }
        };
    }

    private static Texture2D MakeTex(int w, int h, Color col)
    {
        Texture2D tex = new Texture2D(w, h);
        tex.SetPixel(0, 0, col);
        tex.Apply();
        return tex;
    }
}