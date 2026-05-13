using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    private int selectedMap = 0;   // 0=Classic, 1=FrozenPeaks, 2=Island
    private int selectedAI = 1;    // number of AI opponents (1-3)
    private int selectedDiff = 0;  // 0=Easy, 1=Medium, 2=Hard

    private readonly string[] mapNames = { "Classique", "Sommets Glacés", "Îles" };
    private readonly string[] diffNames = { "Facile", "Moyen", "Difficile" };

    private GUIStyle titleStyle;
    private GUIStyle labelStyle;
    private GUIStyle buttonStyle;
    private GUIStyle selectedButtonStyle;
    private GUIStyle panelStyle;

    private void OnGUI()
    {
        InitStyles();

        float sw = Screen.width;
        float sh = Screen.height;

        // Background
        GUI.color = new Color(0.08f, 0.08f, 0.15f);
        GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Title
        GUI.Label(new Rect(0, sh * 0.08f, sw, 80f), "SupKonQuest", titleStyle);

        float panelW = 480f;
        float panelH = 320f;
        float panelX = (sw - panelW) / 2f;
        float panelY = sh * 0.22f;

        GUI.Box(new Rect(panelX - 15, panelY - 15, panelW + 30, panelH + 30), "", panelStyle);

        // Map selection
        GUI.Label(new Rect(panelX, panelY, panelW, 28f), "Carte", labelStyle);
        panelY += 32f;
        for (int i = 0; i < mapNames.Length; i++)
        {
            GUIStyle style = selectedMap == i ? selectedButtonStyle : buttonStyle;
            if (GUI.Button(new Rect(panelX + i * 162f, panelY, 155f, 38f), mapNames[i], style))
                selectedMap = i;
        }
        panelY += 50f;

        // AI count
        GUI.Label(new Rect(panelX, panelY, panelW, 28f), "Adversaires IA", labelStyle);
        panelY += 32f;
        for (int i = 1; i <= 3; i++)
        {
            GUIStyle style = selectedAI == i ? selectedButtonStyle : buttonStyle;
            if (GUI.Button(new Rect(panelX + (i - 1) * 100f, panelY, 90f, 38f), i.ToString(), style))
                selectedAI = i;
        }
        panelY += 50f;

        // Difficulty
        GUI.Label(new Rect(panelX, panelY, panelW, 28f), "Difficulté", labelStyle);
        panelY += 32f;
        for (int i = 0; i < diffNames.Length; i++)
        {
            GUIStyle style = selectedDiff == i ? selectedButtonStyle : buttonStyle;
            if (GUI.Button(new Rect(panelX + i * 162f, panelY, 155f, 38f), diffNames[i], style))
                selectedDiff = i;
        }
        panelY += 62f;

        // Play button
        GUI.color = new Color(0.2f, 0.8f, 0.3f);
        if (GUI.Button(new Rect((sw - 220f) / 2f, panelY, 220f, 54f), "JOUER", selectedButtonStyle))
            StartGame();
        GUI.color = Color.white;

        // Quit button
        if (GUI.Button(new Rect((sw - 130f) / 2f, panelY + 65f, 130f, 36f), "Quitter", buttonStyle))
            Application.Quit();
    }

    private void StartGame()
    {
        PlayerPrefs.SetInt("MapType", selectedMap);
        PlayerPrefs.SetInt("AICount", selectedAI);
        PlayerPrefs.SetInt("AIDifficulty", selectedDiff);
        PlayerPrefs.Save();
        SceneManager.LoadScene("Proto_01");
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
