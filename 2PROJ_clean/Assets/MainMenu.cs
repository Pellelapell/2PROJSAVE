using UnityEngine;
using UnityEngine.SceneManagement;
using SupKonQuest;

public class MainMenu : MonoBehaviour
{
    private int selectedMap = 0;
    private int selectedAI = 1;
    private int selectedDiff = 0;
    private int selectedLang = 0; // 0=FR, 1=EN, 2=ES

    private readonly string[] langCodes = { "fr", "en", "es" };
    private readonly string[] langLabels = { "FR", "EN", "ES" };

    private GUIStyle titleStyle;
    private GUIStyle labelStyle;
    private GUIStyle buttonStyle;
    private GUIStyle selectedButtonStyle;
    private GUIStyle panelStyle;

    private void Awake()
    {
        // Créer le LocalizationManager s'il n'existe pas encore
        if (LocalizationManager.Instance == null)
        {
            GameObject go = new GameObject("LocalizationManager");
            go.AddComponent<LocalizationManager>();
        }

        // Restaurer la langue sélectionnée
        string savedLang = PlayerPrefs.GetString("Language", "fr");
        for (int i = 0; i < langCodes.Length; i++)
            if (langCodes[i] == savedLang) { selectedLang = i; break; }
    }

    private void OnGUI()
    {
        InitStyles();

        float sw = Screen.width;
        float sh = Screen.height;

        // Fond
        GUI.color = new Color(0.08f, 0.08f, 0.15f);
        GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Titre
        GUI.Label(new Rect(0, sh * 0.06f, sw, 80f), "SupKonQuest", titleStyle);

        float panelW = 500f;
        float panelH = 400f;
        float panelX = (sw - panelW) / 2f;
        float panelY = sh * 0.20f;

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
                titleStyle = null; // Reset pour forcer InitStyles (pas nécessaire ici mais propre)
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

        // Jouer
        GUI.color = new Color(0.2f, 0.8f, 0.3f);
        if (GUI.Button(new Rect((sw - 220f) / 2f, panelY, 220f, 54f), L("play"), selectedButtonStyle))
            StartGame();
        GUI.color = Color.white;

        // Quitter
        if (GUI.Button(new Rect((sw - 130f) / 2f, panelY + 65f, 130f, 36f), L("quit"), buttonStyle))
            Application.Quit();
    }

    private static string L(string key) => LocalizationManager.Get(key);

    private void StartGame()
    {
        PlayerPrefs.SetInt("MapType", selectedMap);
        PlayerPrefs.SetInt("AICount", selectedAI);
        PlayerPrefs.SetInt("AIDifficulty", selectedDiff);
        PlayerPrefs.SetString("Language", langCodes[selectedLang]);
        PlayerPrefs.Save();
        string[] sceneNames = { "Classic", "Frozen_Peak", "Islands" };
        SceneManager.LoadScene(sceneNames[selectedMap]);
    }

    private void InitStyles()
    {
        if (titleStyle != null) return;

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 64,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.9f, 0.75f, 0.2f) }
        };

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
        };

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 14,
            normal = { textColor = Color.white }
        };

        selectedButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.yellow }
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
