using UnityEngine;
using UnityEngine.SceneManagement;
using SupKonQuest;

public class MainMenu : MonoBehaviour
{
    private enum MenuScreen { Title, Selection }
    private MenuScreen current = MenuScreen.Title;

    private int selectedMap  = 0;
    private int selectedAI   = 1;
    private int selectedDiff = 0;
    private int selectedLang = 0;
    private int selectedRace = 0;

    private static readonly string[] raceLabels = { "Humain", "Elfe", "Démon" };
    private static readonly Color[]  raceColors =
    {
        new Color(0.3f, 0.5f, 1.0f),
        new Color(0.2f, 0.85f, 0.3f),
        new Color(1.0f, 0.25f, 0.25f)
    };

    private readonly string[] langCodes  = { "fr", "en", "es" };
    private readonly string[] langLabels = { "FR", "EN", "ES" };

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

        AudioManager.Instance?.PlayMenuMusic();
    }

    private void OnGUI()
    {
        InitStyles();

        // Background plein écran
        if (backgroundTexture != null)
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height),
                            backgroundTexture, ScaleMode.ScaleAndCrop);

        // Overlay sombre léger
        GUI.color = new Color(0f, 0f, 0f, 0.45f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        switch (current)
        {
            case MenuScreen.Title:     DrawTitleScreen();     break;
            case MenuScreen.Selection: DrawSelectionScreen(); break;
        }
    }

    private void DrawTitleScreen()
    {
        float sw = Screen.width;
        float sh = Screen.height;

        // Ombre derrière le titre
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(new Rect(0, sh * 0.08f, sw, 150f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.Label(new Rect(0, sh * 0.10f, sw, 90f), "SupKonQuest", titleStyle);
        GUI.Label(new Rect(0, sh * 0.10f + 88f, sw, 30f),
                  "Stratégie — Conquête — Tactique", subtitleStyle);

        float btnW = 300f;
        float btnH = 62f;
        float btnX = (sw - btnW) * 0.5f;
        float btnY = sh * 0.42f;
        float gap  = 20f;

        if (GUI.Button(new Rect(btnX, btnY, btnW, btnH), L("play"), mainBtnStyle))
        {
            AudioManager.Instance?.PlayClick();
            current = MenuScreen.Selection;
        }

        btnY += btnH + gap;

        if (GUI.Button(new Rect(btnX, btnY, btnW, btnH), L("quit"), quitBtnStyle))
        {
            AudioManager.Instance?.PlayClick();
            Application.Quit();
        }
    }

    private void DrawSelectionScreen()
    {
        float sw = Screen.width;
        float sh = Screen.height;

        // Ombre derrière le titre
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(new Rect(0, 0, sw, 70f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.Label(new Rect(0, 2f, sw, 65f), "SupKonQuest", titleStyle);
        

        float panelW = 480f;
        float panelH = 430f; 
        float panelX = (sw - panelW) / 2f;
        float panelY = sh * 0.14f;

        // Panel semi-transparent
        GUI.Box(new Rect(panelX - 20, panelY - 12, panelW + 40, panelH + 24), "", panelStyle);

        // Langue
        GUI.Label(new Rect(panelX, panelY, panelW, 26f), L("language"), labelStyle);
        panelY += 30f;
        for (int i = 0; i < langLabels.Length; i++)
        {
            GUIStyle style = selectedLang == i ? selectedButtonStyle : buttonStyle;
            if (GUI.Button(new Rect(panelX + i * 82f, panelY, 74f, 34f), langLabels[i], style))
            {
                AudioManager.Instance?.PlayClick();
                selectedLang = i;
                LocalizationManager.Instance?.LoadLanguage(langCodes[i]);
            }
        }
        panelY += 42f;

        // Carte
        GUI.Label(new Rect(panelX, panelY, panelW, 26f), L("map"), labelStyle);
        panelY += 30f;
        string[] mapKeys = { "map_classic", "map_frozen", "map_island" };
        for (int i = 0; i < mapKeys.Length; i++)
        {
            GUIStyle style = selectedMap == i ? selectedButtonStyle : buttonStyle;
            if (GUI.Button(new Rect(panelX + i * 162f, panelY, 154f, 38f), L(mapKeys[i]), style))
            {
                AudioManager.Instance?.PlayClick();
                selectedMap = i;
            }
        }
        panelY += 46f;

        // Adversaires IA
        GUI.Label(new Rect(panelX, panelY, panelW, 26f), L("opponents"), labelStyle);
        panelY += 30f;
        for (int i = 1; i <= 3; i++)
        {
            GUIStyle style = selectedAI == i ? selectedButtonStyle : buttonStyle;
            if (GUI.Button(new Rect(panelX + (i - 1) * 100f, panelY, 92f, 38f), i.ToString(), style))
            {
                AudioManager.Instance?.PlayClick();
                selectedAI = i;
            }
        }
        panelY += 46f;

        // Difficulté
        GUI.Label(new Rect(panelX, panelY, panelW, 26f), L("difficulty"), labelStyle);
        panelY += 30f;
        string[] diffKeys = { "diff_easy", "diff_medium", "diff_hard" };
        for (int i = 0; i < diffKeys.Length; i++)
        {
            GUIStyle style = selectedDiff == i ? selectedButtonStyle : buttonStyle;
            if (GUI.Button(new Rect(panelX + i * 162f, panelY, 154f, 38f), L(diffKeys[i]), style))
            {
                AudioManager.Instance?.PlayClick();
                selectedDiff = i;
            }
        }
        panelY += 52f;

        // Race
        GUI.Label(new Rect(panelX, panelY, panelW, 26f), "Race", labelStyle);
        panelY += 30f;
        for (int i = 0; i < raceLabels.Length; i++)
        {
            Color raceCol = raceColors[i];
            GUI.color = (i == selectedRace)
                ? raceCol
                : new Color(raceCol.r * 0.35f, raceCol.g * 0.35f, raceCol.b * 0.35f);
            if (GUI.Button(new Rect(panelX + i * 162f, panelY, 154f, 38f),
                           raceLabels[i], selectedButtonStyle))
            {
                AudioManager.Instance?.PlayClick();
                selectedRace = i;
            }
        }
        GUI.color = Color.white;
        panelY += 52f;

        // Boutons Retour + Lancer
        float bw     = 220f;
        float totalW = bw * 2f + 16f;
        float startX = (sw - totalW) * 0.5f;

        if (GUI.Button(new Rect(startX, panelY, bw, 52f), "← " + L("back"), backBtnStyle))
        {
            AudioManager.Instance?.PlayClick();
            current = MenuScreen.Title;
        }

        if (GUI.Button(new Rect(startX + bw + 16f, panelY, bw, 52f),
                       L("play") + " !", playBtnStyle))
        {
            AudioManager.Instance?.PlayClick();
            StartGame();
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
            fontSize  = 58,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = new Color(1f, 0.85f, 0.15f) }
        };

        subtitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 15,
            fontStyle = FontStyle.Italic,
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = new Color(0.95f, 0.95f, 0.95f) }
        };

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 17,
            fontStyle = FontStyle.Bold,
            normal    = { textColor = new Color(1f, 0.85f, 0.2f) }
        };

        // Bouton JOUER principal
        mainBtnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize  = 22,
            fontStyle = FontStyle.Bold,
            normal    = { textColor = new Color(1f, 0.85f, 0.15f) }
        };

        // Bouton QUITTER
        quitBtnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize  = 22,
            fontStyle = FontStyle.Bold,
            normal    = { textColor = new Color(1f, 0.3f, 0.3f) }
        };

        // Boutons non sélectionnés
        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize  = 14,
            fontStyle = FontStyle.Bold,
            normal    = { textColor = new Color(0.75f, 0.75f, 0.75f) }
        };

        // Boutons sélectionnés
        selectedButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize  = 14,
            fontStyle = FontStyle.Bold,
            normal    = { textColor = new Color(1f, 0.85f, 0.1f) }
        };

        // Bouton BACK
        backBtnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize  = 18,
            fontStyle = FontStyle.Bold,
            normal    = { textColor = new Color(0.85f, 0.85f, 0.85f) }
        };

        // Bouton JOUER !
        playBtnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize  = 18,
            fontStyle = FontStyle.Bold,
            normal    = { textColor = new Color(0.2f, 1f, 0.3f) }
        };

        panelStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = MakeTex(1, 1, new Color(0.03f, 0.02f, 0.0f, 0.72f)) }
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