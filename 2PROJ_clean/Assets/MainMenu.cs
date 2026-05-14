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

    private readonly string[] langCodes  = { "fr", "en", "es" };
    private readonly string[] langLabels = { "FR", "EN", "ES" };

    private GUIStyle titleStyle;
    private GUIStyle subtitleStyle;
    private GUIStyle labelStyle;
    private GUIStyle mainBtnStyle;
    private GUIStyle buttonStyle;
    private GUIStyle selectedButtonStyle;
    private GUIStyle panelStyle;
    private GUIStyle quitBtnStyle;

    private void Awake()
    {
        if (LocalizationManager.Instance == null)
        {
            GameObject go = new GameObject("LocalizationManager");
            go.AddComponent<LocalizationManager>();
        }

        string savedLang = PlayerPrefs.GetString("Language", "fr");
        for (int i = 0; i < langCodes.Length; i++)
            if (langCodes[i] == savedLang) { selectedLang = i; break; }
    }

    private void OnGUI()
    {
        InitStyles();

        // Fond plein écran
        GUI.color = new Color(0.08f, 0.08f, 0.15f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        switch (current)
        {
            case MenuScreen.Title:     DrawTitleScreen();     break;
            case MenuScreen.Selection: DrawSelectionScreen(); break;
        }
    }

    // ── Écran titre ──────────────────────────────────────────────────

    private void DrawTitleScreen()
    {
        float sw = Screen.width;
        float sh = Screen.height;

        // Titre
        GUI.Label(new Rect(0, sh * 0.12f, sw, 90f), "SupKonQuest", titleStyle);
        GUI.Label(new Rect(0, sh * 0.12f + 90f, sw, 30f), "Stratégie — Conquête — Tactique", subtitleStyle);

        float btnW = 280f;
        float btnH = 58f;
        float btnX = (sw - btnW) * 0.5f;
        float btnY = sh * 0.44f;
        float gap  = 18f;

        if (GUI.Button(new Rect(btnX, btnY, btnW, btnH), L("play"), mainBtnStyle))
            current = MenuScreen.Selection;

        btnY += btnH + gap;

        GUI.color = new Color(1f, 0.35f, 0.35f);
        if (GUI.Button(new Rect(btnX, btnY, btnW, btnH), L("quit"), quitBtnStyle))
            Application.Quit();
        GUI.color = Color.white;
    }

    // ── Écran sélection ──────────────────────────────────────────────

    private void DrawSelectionScreen()
    {
        float sw = Screen.width;
        float sh = Screen.height;

        GUI.Label(new Rect(0, sh * 0.04f, sw, 70f), "SupKonQuest", titleStyle);

        float panelW = 500f;
        float panelH = 430f;
        float panelX = (sw - panelW) / 2f;
        float panelY = sh * 0.18f;

        GUI.Box(new Rect(panelX - 15, panelY - 15, panelW + 30, panelH + 30), "", panelStyle);

        // Langue
        GUI.Label(new Rect(panelX, panelY, panelW, 28f), L("language"), labelStyle);
        panelY += 32f;
        for (int i = 0; i < langLabels.Length; i++)
        {
            GUIStyle style = selectedLang == i ? selectedButtonStyle : buttonStyle;
            if (GUI.Button(new Rect(panelX + i * 80f, panelY, 72f, 34f), langLabels[i], style))
            {
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
                selectedMap = i;
        }
        panelY += 50f;

        // Adversaires IA
        GUI.Label(new Rect(panelX, panelY, panelW, 28f), L("opponents"), labelStyle);
        panelY += 32f;
        for (int i = 1; i <= 3; i++)
        {
            GUIStyle style = selectedAI == i ? selectedButtonStyle : buttonStyle;
            if (GUI.Button(new Rect(panelX + (i - 1) * 100f, panelY, 90f, 38f), i.ToString(), style))
                selectedAI = i;
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
                selectedDiff = i;
        }
        panelY += 62f;

        // Boutons Retour + Lancer
        float bw = 200f;
        float totalW = bw * 2f + 16f;
        float startX = (sw - totalW) * 0.5f;

        if (GUI.Button(new Rect(startX, panelY, bw, 50f), "← " + L("back"), buttonStyle))
            current = MenuScreen.Title;

        GUI.color = new Color(0.2f, 0.8f, 0.3f);
        if (GUI.Button(new Rect(startX + bw + 16f, panelY, bw, 50f), L("play") + " !", selectedButtonStyle))
            StartGame();
        GUI.color = Color.white;
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private static string L(string key) => LocalizationManager.Get(key);

    private void StartGame()
    {
        PlayerPrefs.SetInt("MapType",      selectedMap);
        PlayerPrefs.SetInt("AICount",      selectedAI);
        PlayerPrefs.SetInt("AIDifficulty", selectedDiff);
        PlayerPrefs.SetString("Language",  langCodes[selectedLang]);
        PlayerPrefs.Save();
        string[] sceneNames = { "Classic", "Frozen_Peak", "Islands" };
        SceneManager.LoadScene(sceneNames[selectedMap]);
    }

    // ── Styles ───────────────────────────────────────────────────────

    private void InitStyles()
    {
        if (titleStyle != null) return;

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 62,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = new Color(0.9f, 0.75f, 0.2f) }
        };

        subtitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 14,
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = new Color(0.55f, 0.55f, 0.65f) }
        };

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 16,
            fontStyle = FontStyle.Bold,
            normal    = { textColor = new Color(0.8f, 0.8f, 0.8f) }
        };

        mainBtnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize  = 18,
            fontStyle = FontStyle.Bold,
            normal    = { textColor = Color.white }
        };

        quitBtnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize  = 18,
            fontStyle = FontStyle.Bold,
            normal    = { textColor = Color.white }
        };

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 14,
            normal   = { textColor = Color.white }
        };

        selectedButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize  = 14,
            fontStyle = FontStyle.Bold,
            normal    = { textColor = Color.yellow }
        };

        panelStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = MakeTex(1, 1, new Color(0.15f, 0.15f, 0.25f, 0.9f)) }
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
