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
    private GUIStyle backBtnStyle;
    private GUIStyle playBtnStyle;

    private Texture2D texBtnNormal;
    private Texture2D texBtnHover;
    private Texture2D texBtnPressed;
    private Texture2D texBtnSelected;
    private Texture2D texBtnSelectedHover;
    private Texture2D texBtnQuit;
    private Texture2D texBtnQuitHover;
    private Texture2D texBtnPlay;
    private Texture2D texBtnPlayHover;
    private Texture2D texBtnBack;
    private Texture2D texBtnBackHover;
    private GUIStyle sliderLabelStyle;

    private void Awake()
    {
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
        GenerateTextures();
    }

    private void GenerateTextures()
    {
        texBtnNormal        = MakeMedievalTex(200, 40, new Color(0.18f, 0.13f, 0.07f), new Color(0.45f, 0.35f, 0.12f), 3);
        texBtnHover         = MakeMedievalTex(200, 40, new Color(0.28f, 0.20f, 0.10f), new Color(0.70f, 0.55f, 0.18f), 3);
        texBtnPressed       = MakeMedievalTex(200, 40, new Color(0.12f, 0.08f, 0.04f), new Color(0.40f, 0.30f, 0.10f), 3);
        texBtnSelected      = MakeMedievalTex(200, 40, new Color(0.30f, 0.20f, 0.05f), new Color(0.85f, 0.65f, 0.15f), 4);
        texBtnSelectedHover = MakeMedievalTex(200, 40, new Color(0.38f, 0.26f, 0.08f), new Color(1.0f,  0.80f, 0.20f), 4);
        texBtnPlay          = MakeMedievalTex(300, 62, new Color(0.08f, 0.22f, 0.08f), new Color(0.60f, 0.80f, 0.20f), 5);
        texBtnPlayHover     = MakeMedievalTex(300, 62, new Color(0.12f, 0.32f, 0.12f), new Color(0.75f, 1.0f,  0.25f), 5);
        texBtnQuit          = MakeMedievalTex(300, 62, new Color(0.25f, 0.06f, 0.06f), new Color(0.70f, 0.15f, 0.15f), 5);
        texBtnQuitHover     = MakeMedievalTex(300, 62, new Color(0.35f, 0.09f, 0.09f), new Color(0.90f, 0.20f, 0.20f), 5);
        texBtnBack          = MakeMedievalTex(220, 52, new Color(0.15f, 0.15f, 0.18f), new Color(0.50f, 0.50f, 0.60f), 4);
        texBtnBackHover     = MakeMedievalTex(220, 52, new Color(0.22f, 0.22f, 0.28f), new Color(0.70f, 0.70f, 0.85f), 4);
    }

    private void OnGUI()
    {
        InitStyles();

        if (backgroundTexture != null)
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), backgroundTexture, ScaleMode.ScaleAndCrop);

        GUI.color = new Color(0f, 0f, 0f, 0.42f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        switch (current)
        {
            case MenuScreen.Title:     DrawTitleScreen();     break;
            case MenuScreen.Selection: DrawSelectionScreen(); break;
            case MenuScreen.Options:   DrawOptionsScreen();   break;
        }
    }

    private void DrawMedievalTitle(string text, float x, float y, float w, float h)
    {
        GUIStyle shadow = new GUIStyle(titleStyle);
        shadow.normal.textColor = new Color(0f, 0f, 0f, 0.85f);
        for (int ox = -3; ox <= 3; ox++)
            for (int oy = -3; oy <= 3; oy++)
                if (ox != 0 || oy != 0)
                    GUI.Label(new Rect(x + ox, y + oy + 5f, w, h), text, shadow);

        GUIStyle outline = new GUIStyle(titleStyle);
        outline.normal.textColor = new Color(0.55f, 0.35f, 0.0f, 1f);
        for (int ox = -2; ox <= 2; ox++)
            for (int oy = -2; oy <= 2; oy++)
                if (ox != 0 || oy != 0)
                    GUI.Label(new Rect(x + ox, y + oy, w, h), text, outline);

        GUI.Label(new Rect(x, y, w, h), text, titleStyle);
    }

    private void DrawTitleScreen()
    {
        float sw = Screen.width;
        float sh = Screen.height;

        GUI.color = new Color(0f, 0f, 0f, 0.60f);
        GUI.DrawTexture(new Rect(0, sh * 0.07f, sw, 165f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.color = new Color(0.8f, 0.6f, 0.1f, 0.9f);
        GUI.DrawTexture(new Rect(sw * 0.1f, sh * 0.07f, sw * 0.8f, 2f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(sw * 0.1f, sh * 0.07f + 163f, sw * 0.8f, 2f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        DrawMedievalTitle("SupKonQuest", 0, sh * 0.09f, sw, 95f);

        GUI.Label(new Rect(0, sh * 0.09f + 96f, sw, 28f), "⚔  Stratégie — Conquête — Tactique  ⚔", subtitleStyle);

        float btnW = 300f;
        float btnH = 64f;
        float btnX = (sw - btnW) * 0.5f;
        float btnY = sh * 0.44f;

        GUIStyle hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 11,
            fontStyle = FontStyle.Italic,
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = new Color(0.8f, 0.7f, 0.3f, 0.8f) }
        };
        GUI.Label(new Rect(btnX, btnY - 22f, btnW, 20f), "— Commencer l'aventure —", hintStyle);

        if (GUI.Button(new Rect(btnX, btnY, btnW, btnH), L("play"), mainBtnStyle))
        {
            AudioManager.Instance?.PlayClick();
            current = MenuScreen.Selection;
        }

        btnY += btnH + 22f;

        if (GUI.Button(new Rect(btnX, btnY, btnW, btnH), L("options"), mainBtnStyle))
        {
            AudioManager.Instance?.PlayClick();
            current = MenuScreen.Options;
        }

        btnY += btnH + 22f; 

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

        GUI.color = new Color(0f, 0f, 0f, 0.60f);
        GUI.DrawTexture(new Rect(0, 0, sw, 72f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.color = new Color(0.8f, 0.6f, 0.1f, 0.9f);
        GUI.DrawTexture(new Rect(sw * 0.05f, 0f,  sw * 0.9f, 2f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(sw * 0.05f, 70f, sw * 0.9f, 2f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        DrawMedievalTitle("SupKonQuest", 0, 0f, sw, 68f);

        float panelW      = 480f;
        float panelH      = 395f;
        float panelX      = (sw - panelW) / 2f;
        float panelY      = sh * 0.12f;
        float startPanelY = panelY;

        // Bordure dorée panel
        GUI.color = new Color(0.7f, 0.5f, 0.1f, 0.9f);
        GUI.DrawTexture(new Rect(panelX - 22, panelY - 14, panelW + 44, 2f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(panelX - 22, panelY + panelH + 10, panelW + 44, 2f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(panelX - 22, panelY - 14, 2f, panelH + 26), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(panelX + panelW + 20, panelY - 14, 2f, panelH + 26), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.Box(new Rect(panelX - 20, panelY - 12, panelW + 40, panelH), "", panelStyle);

        // Langue
        GUI.Label(new Rect(panelX, panelY, panelW, 24f), L("language"), labelStyle);
        panelY += 28f;
        for (int i = 0; i < langLabels.Length; i++)
        {
            GUIStyle style = selectedLang == i ? selectedButtonStyle : buttonStyle;
            if (GUI.Button(new Rect(panelX + i * 82f, panelY, 74f, 32f), langLabels[i], style))
            {
                AudioManager.Instance?.PlayClick();
                selectedLang = i;
                LocalizationManager.Instance?.LoadLanguage(langCodes[i]);
            }
        }
        panelY += 38f;

        // Carte
        GUI.Label(new Rect(panelX, panelY, panelW, 24f), L("map"), labelStyle);
        panelY += 28f;
        float mapBtnW = (panelW - 8f) / 3f;
        string[] mapKeys = { "map_classic", "map_frozen", "map_island" };
        for (int i = 0; i < mapKeys.Length; i++)
        {
            GUIStyle style = selectedMap == i ? selectedButtonStyle : buttonStyle;
            if (GUI.Button(new Rect(panelX + i * (mapBtnW + 4f), panelY, mapBtnW, 34f), L(mapKeys[i]), style))
            {
                AudioManager.Instance?.PlayClick();
                selectedMap = i;
            }
        }
        panelY += 42f;

        // Adversaires IA
        GUI.Label(new Rect(panelX, panelY, panelW, 24f), L("opponents"), labelStyle);
        panelY += 28f;
        for (int i = 1; i <= 3; i++)
        {
            GUIStyle style = selectedAI == i ? selectedButtonStyle : buttonStyle;
            if (GUI.Button(new Rect(panelX + (i - 1) * 100f, panelY, 92f, 34f), i.ToString(), style))
            {
                AudioManager.Instance?.PlayClick();
                selectedAI = i;
            }
        }
        panelY += 42f;

        // Difficulté
        GUI.Label(new Rect(panelX, panelY, panelW, 24f), L("difficulty"), labelStyle);
        panelY += 28f;
        float diffBtnW = (panelW - 8f) / 3f;
        string[] diffKeys = { "diff_easy", "diff_medium", "diff_hard" };
        for (int i = 0; i < diffKeys.Length; i++)
        {
            GUIStyle style = selectedDiff == i ? selectedButtonStyle : buttonStyle;
            if (GUI.Button(new Rect(panelX + i * (diffBtnW + 4f), panelY, diffBtnW, 34f), L(diffKeys[i]), style))
            {
                AudioManager.Instance?.PlayClick();
                selectedDiff = i;
            }
        }
        panelY += 42f;

        // Race
        GUI.Label(new Rect(panelX, panelY, panelW, 28f), L("race"), labelStyle);
        panelY += 32f;
        string[] raceKeys = { "race_human", "race_elf", "race_demon" };
        for (int i = 0; i < raceKeys.Length; i++)
        {
            Color raceCol = raceColors[i];
            GUI.color = (i == selectedRace) ? raceCol : new Color(raceCol.r * 0.4f, raceCol.g * 0.4f, raceCol.b * 0.4f);
            if (GUI.Button(new Rect(panelX + i * 168f, panelY, 160f, 38f), L(raceKeys[i]), selectedButtonStyle))
            {
                AudioManager.Instance?.PlayClick();
                selectedRace = i;
            }
        }
        GUI.color = Color.white;

        // ── BACK gauche — JOUER droite ──
        float btnH   = 54f;
        float btnW   = 220f;
        float btnY   = Mathf.Min(startPanelY + panelH + 20f, Screen.height - btnH - 8f);

        // Ligne dorée séparatrice
        GUI.color = new Color(0.8f, 0.6f, 0.1f, 0.8f);
        GUI.DrawTexture(new Rect(panelX - 20, btnY - 8f, panelW + 40, 1f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // BACK ←
        if (GUI.Button(new Rect(panelX - 20, btnY, btnW, btnH), "← " + L("back"), backBtnStyle))
        {
            AudioManager.Instance?.PlayClick();
            current = MenuScreen.Title;
        }

        // JOUER →
        if (GUI.Button(new Rect(panelX + panelW + 20 - btnW, btnY, btnW, btnH), L("play") + " →", playBtnStyle))
        {
            AudioManager.Instance?.PlayClick();
            StartGame();
        }
    }

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
            fontSize  = 74,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = new Color(1f, 0.92f, 0.25f) }
        };

        subtitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 15,
            fontStyle = FontStyle.Italic,
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = new Color(0.90f, 0.82f, 0.55f) }
        };

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 16,
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
            fontSize  = 24,
            fontStyle = FontStyle.Bold,
            normal    = { background = texBtnPlay,      textColor = new Color(0.8f, 1f,   0.3f) },
            hover     = { background = texBtnPlayHover, textColor = new Color(0.9f, 1f,   0.5f) },
            active    = { background = texBtnPressed,   textColor = new Color(0.6f, 0.9f, 0.2f) },
            border    = new RectOffset(6, 6, 6, 6),
            padding   = new RectOffset(10, 10, 10, 10)
        };

        quitBtnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize  = 24,
            fontStyle = FontStyle.Bold,
            normal    = { background = texBtnQuit,      textColor = new Color(1f,   0.5f, 0.5f) },
            hover     = { background = texBtnQuitHover, textColor = new Color(1f,   0.7f, 0.7f) },
            active    = { background = texBtnPressed,   textColor = new Color(0.8f, 0.3f, 0.3f) },
            border    = new RectOffset(6, 6, 6, 6),
            padding   = new RectOffset(10, 10, 10, 10)
        };

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize  = 13,
            fontStyle = FontStyle.Bold,
            normal    = { background = texBtnNormal,  textColor = new Color(0.80f, 0.75f, 0.60f) },
            hover     = { background = texBtnHover,   textColor = new Color(1f,    0.95f, 0.70f) },
            active    = { background = texBtnPressed, textColor = new Color(0.70f, 0.65f, 0.50f) },
            border    = new RectOffset(4, 4, 4, 4),
            padding   = new RectOffset(6, 6, 4, 4)
        };

        selectedButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize  = 13,
            fontStyle = FontStyle.Bold,
            normal    = { background = texBtnSelected,      textColor = new Color(1f,   0.90f, 0.20f) },
            hover     = { background = texBtnSelectedHover, textColor = new Color(1f,   1f,    0.40f) },
            active    = { background = texBtnPressed,       textColor = new Color(0.9f, 0.75f, 0.15f) },
            border    = new RectOffset(5, 5, 5, 5),
            padding   = new RectOffset(6, 6, 4, 4)
        };

        backBtnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize  = 18,
            fontStyle = FontStyle.Bold,
            normal    = { background = texBtnBack,      textColor = new Color(0.85f, 0.85f, 0.90f) },
            hover     = { background = texBtnBackHover, textColor = new Color(1f,    1f,    1f   ) },
            active    = { background = texBtnPressed,   textColor = new Color(0.70f, 0.70f, 0.75f) },
            border    = new RectOffset(5, 5, 5, 5),
            padding   = new RectOffset(8, 8, 8, 8)
        };

        playBtnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize  = 18,
            fontStyle = FontStyle.Bold,
            normal    = { background = texBtnPlay,      textColor = new Color(0.5f, 1f,   0.4f) },
            hover     = { background = texBtnPlayHover, textColor = new Color(0.7f, 1f,   0.6f) },
            active    = { background = texBtnPressed,   textColor = new Color(0.3f, 0.8f, 0.3f) },
            border    = new RectOffset(5, 5, 5, 5),
            padding   = new RectOffset(8, 8, 8, 8)
        };

        panelStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = MakeTex(1, 1, new Color(0.04f, 0.03f, 0.01f, 0.80f)) }
        };
    }

    private static Texture2D MakeMedievalTex(int w, int h, Color fill, Color border, int bSize)
    {
        Texture2D tex    = new Texture2D(Mathf.Max(w, 1), Mathf.Max(h, 1));
        Color[]   pixels = new Color[tex.width * tex.height];
        int       W      = tex.width;
        int       H      = tex.height;

        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                bool onBorder = bSize > 0 && (x < bSize || x >= W - bSize || y < bSize || y >= H - bSize);
                bool onCorner = bSize > 0 && ((x < bSize + 2 && y < bSize + 2) || (x >= W - bSize - 2 && y < bSize + 2) || (x < bSize + 2 && y >= H - bSize - 2) || (x >= W - bSize - 2 && y >= H - bSize - 2));

                float nx    = (float)x / W;
                float ny    = (float)y / H;
                float noise = (Mathf.Sin(nx * 17f) * Mathf.Cos(ny * 13f) + Mathf.Sin(nx * 7f + ny * 11f)) * 0.025f;
                float grad  = 1f - ny * 0.15f;

                if (onCorner)
                    pixels[y * W + x] = border * 0.55f;
                else if (onBorder)
                    pixels[y * W + x] = new Color(Mathf.Clamp01(border.r + noise), Mathf.Clamp01(border.g + noise), Mathf.Clamp01(border.b + noise), 1f);
                else
                    pixels[y * W + x] = new Color(Mathf.Clamp01((fill.r + noise) * grad), Mathf.Clamp01((fill.g + noise) * grad), Mathf.Clamp01((fill.b + noise) * grad), 1f);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static Texture2D MakeTex(int w, int h, Color col)
    {
        Texture2D tex = new Texture2D(w, h);
        tex.SetPixel(0, 0, col);
        tex.Apply();
        return tex;
    }
}