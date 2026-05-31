using UnityEngine;
using UnityEngine.SceneManagement;
using SupKonQuest;

public class MainMenu : MonoBehaviour
{
    private enum MenuScreen { Title, Selection, Options, Tutorial }
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

    private float musicVolume = 1f;
    private float sfxVolume   = 1f;

    private Texture2D backgroundTexture;

    private GUIStyle titleStyle;
    private GUIStyle subtitleStyle;
    private GUIStyle labelStyle;
    private GUIStyle headerStyle;
    private GUIStyle mainBtnStyle;
    private GUIStyle optionsBtnStyle;
    private GUIStyle quitBtnStyle;
    private GUIStyle buttonStyle;
    private GUIStyle selectedButtonStyle;
    private GUIStyle panelStyle;
    private GUIStyle backBtnStyle;
    private GUIStyle playBtnStyle;
    private GUIStyle sliderStyle;
    private GUIStyle sliderThumbStyle;

    private Texture2D texVignette;
    private Texture2D texPanel;

    private Texture2D texBtnNormal;
    private Texture2D texBtnHover;
    private Texture2D texBtnPressed;
    private Texture2D texBtnSelected;
    private Texture2D texBtnSelectedHover;

    private Texture2D texBtnPlay;
    private Texture2D texBtnPlayHover;
    private Texture2D texBtnQuit;
    private Texture2D texBtnQuitHover;
    private Texture2D texBtnOptions;
    private Texture2D texBtnOptionsHover;
    private Texture2D texBtnBack;
    private Texture2D texBtnBackHover;

    private Texture2D texSliderTrack;
    private Texture2D texSliderThumb;
    private Texture2D texSliderThumbHover;

    // --- Initialization ---

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

        texVignette = MakeVignetteTex(512, 512, new Color(0.02f, 0.05f, 0.12f, 0.85f), new Color(0.05f, 0.1f, 0.2f, 0.1f));
        

        texPanel = MakePremiumTex(64, 64, new Color(0.06f, 0.07f, 0.09f, 0.92f), new Color(0.5f, 0.4f, 0.2f, 0.8f), Color.clear, 2, 0f);


        texBtnNormal        = MakePremiumTex(200, 40, new Color(0.12f, 0.11f, 0.10f), new Color(0.45f, 0.35f, 0.15f), Color.clear, 3, 0f);
        texBtnHover         = MakePremiumTex(200, 40, new Color(0.18f, 0.16f, 0.14f), new Color(0.70f, 0.55f, 0.25f), new Color(0.8f, 0.6f, 0.2f, 0.5f), 3, 4f);
        texBtnPressed       = MakePremiumTex(200, 40, new Color(0.08f, 0.07f, 0.06f), new Color(0.30f, 0.25f, 0.10f), Color.clear, 3, 0f, true);
        
        texBtnSelected      = MakePremiumTex(200, 40, new Color(0.22f, 0.18f, 0.10f), new Color(0.85f, 0.70f, 0.20f), new Color(0.9f, 0.7f, 0.2f, 0.4f), 3, 3f);
        texBtnSelectedHover = MakePremiumTex(200, 40, new Color(0.28f, 0.24f, 0.15f), new Color(1.0f,  0.85f, 0.30f), new Color(1.0f, 0.8f, 0.3f, 0.7f), 3, 5f);


        texBtnPlay          = MakePremiumTex(320, 64, new Color(0.08f, 0.18f, 0.10f), new Color(0.50f, 0.75f, 0.25f), Color.clear, 4, 0f);
        texBtnPlayHover     = MakePremiumTex(320, 64, new Color(0.12f, 0.25f, 0.15f), new Color(0.70f, 1.0f,  0.35f), new Color(0.4f, 1.0f, 0.4f, 0.6f), 4, 6f);
        
        texBtnOptions       = MakePremiumTex(320, 64, new Color(0.08f, 0.10f, 0.20f), new Color(0.30f, 0.40f, 0.75f), Color.clear, 4, 0f);
        texBtnOptionsHover  = MakePremiumTex(320, 64, new Color(0.12f, 0.15f, 0.28f), new Color(0.45f, 0.60f, 1.0f),  new Color(0.3f, 0.6f, 1.0f, 0.6f), 4, 6f);
        
        texBtnQuit          = MakePremiumTex(320, 64, new Color(0.20f, 0.08f, 0.08f), new Color(0.75f, 0.25f, 0.25f), Color.clear, 4, 0f);
        texBtnQuitHover     = MakePremiumTex(320, 64, new Color(0.28f, 0.12f, 0.12f), new Color(1.0f,  0.35f, 0.35f), new Color(1.0f, 0.3f, 0.3f, 0.6f), 4, 6f);


        texBtnBack          = MakePremiumTex(220, 52, new Color(0.12f, 0.12f, 0.14f), new Color(0.45f, 0.45f, 0.55f), Color.clear, 3, 0f);
        texBtnBackHover     = MakePremiumTex(220, 52, new Color(0.18f, 0.18f, 0.22f), new Color(0.65f, 0.65f, 0.80f), new Color(0.6f, 0.6f, 0.8f, 0.5f), 3, 5f);


        texSliderTrack      = MakeSliderTrackTex(256, 12);
        texSliderThumb      = MakeSliderThumbTex(24, new Color(0.8f, 0.7f, 0.3f));
        texSliderThumbHover = MakeSliderThumbTex(24, new Color(1.0f, 0.9f, 0.5f));
    }

    // --- Main GUI Loop ---

    private void OnGUI()
    {
        InitStyles();


        if (backgroundTexture != null)
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), backgroundTexture, ScaleMode.ScaleAndCrop);


        if (texVignette != null)
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), texVignette, ScaleMode.StretchToFill);


        switch (current)
        {
            case MenuScreen.Title:     DrawTitleScreen();     break;
            case MenuScreen.Selection: DrawSelectionScreen(); break;
            case MenuScreen.Options:   DrawOptionsScreen();   break;
            case MenuScreen.Tutorial:  DrawTutorialScreen();  break;
        }
    }

    private void DrawMedievalTitle(string text, float x, float y, float w, float h)
    {

        GUIStyle shadow = new GUIStyle(titleStyle);
        shadow.normal.textColor = new Color(0f, 0f, 0f, 0.9f);
        shadow.hover.textColor = shadow.normal.textColor;
        shadow.active.textColor = shadow.normal.textColor;
        GUI.Label(new Rect(x + 4, y + 6, w, h), text, shadow);
        GUI.Label(new Rect(x + 2, y + 4, w, h), text, shadow);


        GUIStyle glow = new GUIStyle(titleStyle);
        glow.normal.textColor = new Color(0.8f, 0.5f, 0.0f, 0.3f);
        glow.hover.textColor = glow.normal.textColor;
        glow.active.textColor = glow.normal.textColor;
        for (int ox = -4; ox <= 4; ox+=2)
            for (int oy = -4; oy <= 4; oy+=2)
                if (ox != 0 || oy != 0)
                    GUI.Label(new Rect(x + ox, y + oy, w, h), text, glow);


        GUI.Label(new Rect(x, y, w, h), text, titleStyle);
    }

    // --- UI Screens ---

    private void DrawTitleScreen()
    {
        float sw = Screen.width;
        float sh = Screen.height;


        float titleAreaH = 175f;
        

        GUI.color = new Color(0f, 0f, 0f, 0.5f);
        GUI.DrawTexture(new Rect(0, sh * 0.05f, sw, titleAreaH), Texture2D.whiteTexture);
        GUI.color = Color.white;


        GUI.color = new Color(0.8f, 0.65f, 0.2f, 0.8f);
        GUI.DrawTexture(new Rect(sw * 0.05f, sh * 0.05f, sw * 0.9f, 2f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(sw * 0.05f, sh * 0.05f + titleAreaH, sw * 0.9f, 2f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        DrawMedievalTitle("SupKonQuest", 0, sh * 0.05f + 10f, sw, 120f);

        GUI.Label(new Rect(0, sh * 0.05f + 130f, sw, 30f),
                "⚔  Stratégie — Conquête — Tactique  ⚔", subtitleStyle);


        float btnW = 320f;
        float btnH = 64f;
        float gap  = 16f;

        float remainingH = sh - (sh * 0.05f + titleAreaH);
        float buttonsTotalH = (btnH * 4) + (gap * 3) + 40f;


        float startY = (sh * 0.05f + titleAreaH) + (remainingH - buttonsTotalH) * 0.4f;
        float btnX = (sw - btnW) * 0.5f;


        GUIStyle hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 14,
            fontStyle = FontStyle.Italic,
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = new Color(0.9f, 0.8f, 0.4f, 0.9f) }
        };
        GUI.Label(new Rect(btnX, startY, btnW, 20f), "— Entrez dans la légende —", hintStyle);
        
        float currentY = startY + 30f;

        string playText = L("play").ToLower();
        if (playText.Length > 0) playText = char.ToUpper(playText[0]) + playText.Substring(1);

        Rect playRect = new Rect(btnX, currentY, btnW, btnH);
        string playLabel = $"{playText}";

        if (GUI.Button(playRect, playLabel, mainBtnStyle))
        {
            AudioManager.Instance?.PlayClick();
            current = MenuScreen.Selection;
        }
        currentY += btnH + gap;

        string optionsText = L("options");
        Rect optionsRect = new Rect(btnX, currentY, btnW, btnH);
        string optionsLabel = $"⚙  {optionsText}  ⚙";

        if (GUI.Button(optionsRect, optionsLabel, optionsBtnStyle))
        {
            AudioManager.Instance?.PlayClick();
            current = MenuScreen.Options;
        }
        currentY += btnH + gap;

        Rect tutorialRect = new Rect(btnX, currentY, btnW, btnH);
        if (GUI.Button(tutorialRect, "?  " + L("tutorial") + "  ?", optionsBtnStyle))
        {
            AudioManager.Instance?.PlayClick();
            current = MenuScreen.Tutorial;
        }
        currentY += btnH + gap;

        string quitText = L("quit");
        Rect quitRect = new Rect(btnX, currentY, btnW, btnH);
        string quitLabel = $"✕  {quitText}  ✕";

        if (GUI.Button(quitRect, quitLabel, quitBtnStyle))
        {
            AudioManager.Instance?.PlayClick();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    private void DrawSelectionScreen()
    {
        float sw = Screen.width;
        float sh = Screen.height;


        GUI.color = new Color(0f, 0f, 0f, 0.70f);
        GUI.DrawTexture(new Rect(0, 0, sw, 110f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.color = new Color(0.9f, 0.7f, 0.2f, 0.9f);
        GUI.DrawTexture(new Rect(sw * 0.02f, 108f, sw * 0.96f, 2f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        DrawMedievalTitle("Configuration", 0, -5f, sw, 90f);
        GUI.Label(new Rect(0, 80f, sw, 25f), "Préparez vos forces avant la bataille", subtitleStyle);


        float panelW = 750f;
        float panelH = Mathf.Min(480f, sh - 140f);
        float panelX = (sw - panelW) / 2f;
        float panelY = 125f;

        GUI.Box(new Rect(panelX, panelY, panelW, panelH), "", panelStyle);


        DrawOrnateFrame(panelX, panelY, panelW, panelH);


        float colWidth = (panelW - 60f) / 2f;
        float col1X = panelX + 20f;
        float col2X = panelX + 40f + colWidth;
        float startY = panelY + 20f;


        GUI.color = new Color(0.5f, 0.4f, 0.2f, 0.3f);
        GUI.DrawTexture(new Rect(panelX + panelW/2 - 1, startY, 2f, panelH - 100f), Texture2D.whiteTexture);
        GUI.color = Color.white;


        float btnHeight = 36f;
        float contentH = 30f + (24f + btnHeight) * 3; 
        float availableH = panelH - 80f - 20f;
        float gap = Mathf.Max(8f, (availableH - contentH) / 2f);


        float y = startY;
        GUI.Label(new Rect(col1X, y, colWidth, 24f), "⚔ Environnement", headerStyle);
        y += 30f;


        GUI.Label(new Rect(col1X, y, colWidth, 20f), L("map"), labelStyle);
        y += 24f;
        float mapBtnW = (colWidth - 10f) / 3f;
        string[] mapKeys = { "map_classic", "map_frozen", "map_island" };
        for (int i = 0; i < mapKeys.Length; i++)
        {
            GUIStyle style = selectedMap == i ? selectedButtonStyle : buttonStyle;
            if (GUI.Button(new Rect(col1X + i * (mapBtnW + 5f), y, mapBtnW, btnHeight), L(mapKeys[i]), style))
            {
                AudioManager.Instance?.PlayClick();
                selectedMap = i;
            }
        }
        y += btnHeight + gap;


        GUI.Label(new Rect(col1X, y, colWidth, 20f), L("opponents"), labelStyle);
        y += 24f;
        float aiBtnW = (colWidth - 10f) / 3f;
        for (int i = 1; i <= 3; i++)
        {
            GUIStyle style = selectedAI == i ? selectedButtonStyle : buttonStyle;
            if (GUI.Button(new Rect(col1X + (i - 1) * (aiBtnW + 5f), y, aiBtnW, btnHeight), i.ToString(), style))
            {
                AudioManager.Instance?.PlayClick();
                selectedAI = i;
            }
        }
        y += btnHeight + gap;


        GUI.Label(new Rect(col1X, y, colWidth, 20f), L("difficulty"), labelStyle);
        y += 24f;
        string[] diffKeys = { "diff_easy", "diff_medium", "diff_hard" };
        for (int i = 0; i < diffKeys.Length; i++)
        {
            GUIStyle style = selectedDiff == i ? selectedButtonStyle : buttonStyle;
            if (GUI.Button(new Rect(col1X + i * (mapBtnW + 5f), y, mapBtnW, btnHeight), L(diffKeys[i]), style))
            {
                AudioManager.Instance?.PlayClick();
                selectedDiff = i;
            }
        }


        y = startY;
        GUI.Label(new Rect(col2X, y, colWidth, 24f), "⛨ Armée & Système", headerStyle);
        y += 30f;


        GUI.Label(new Rect(col2X, y, colWidth, 20f), L("race"), labelStyle);
        y += 24f;
        string[] raceKeys = { "race_human", "race_elf", "race_demon" };
        float raceBtnW = (colWidth - 10f) / 3f;
        for (int i = 0; i < raceKeys.Length; i++)
        {
            Color     raceCol = raceColors[i];
            GUIStyle  style   = new GUIStyle(selectedRace == i ? selectedButtonStyle : buttonStyle);
            
            Texture2D raceTex = selectedRace == i
                ? MakePremiumTex(200, 40, new Color(raceCol.r * 0.2f, raceCol.g * 0.2f, raceCol.b * 0.2f), raceCol, raceCol * 0.5f, 3, 3f)
                : MakePremiumTex(200, 40, new Color(raceCol.r * 0.05f, raceCol.g * 0.05f, raceCol.b * 0.05f), raceCol * 0.5f, Color.clear, 3, 0f);
            
            style.normal.background = raceTex;
            style.hover.background  = raceTex; 
            style.normal.textColor  = selectedRace == i ? Color.white : raceCol * 0.8f;
            style.hover.textColor   = Color.white;

            if (GUI.Button(new Rect(col2X + i * (raceBtnW + 5f), y, raceBtnW, btnHeight), L(raceKeys[i]), style))
            {
                AudioManager.Instance?.PlayClick();
                selectedRace = i;
            }
        }
        y += btnHeight + gap;


        GUI.Label(new Rect(col2X, y, colWidth, 20f), L("language"), labelStyle);
        y += 24f;
        float langBtnW = (colWidth - 10f) / 3f;
        for (int i = 0; i < langLabels.Length; i++)
        {
            GUIStyle style = selectedLang == i ? selectedButtonStyle : buttonStyle;
            if (GUI.Button(new Rect(col2X + i * (langBtnW + 5f), y, langBtnW, btnHeight), langLabels[i], style))
            {
                AudioManager.Instance?.PlayClick();
                selectedLang = i;
                LocalizationManager.Instance?.LoadLanguage(langCodes[i]);
            }
        }


        float actionY = panelY + panelH - 70f;
        
        GUI.color = new Color(0.8f, 0.65f, 0.2f, 0.6f);
        GUI.DrawTexture(new Rect(panelX + 20, actionY - 10, panelW - 40, 1f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float btnH = 50f;
        float btnActionW = 240f;

        if (GUI.Button(new Rect(panelX + 30, actionY, btnActionW, btnH), "← " + L("back"), backBtnStyle))
        {
            AudioManager.Instance?.PlayClick();
            current = MenuScreen.Title;
        }

        string playText = L("play").ToLower();
        if (playText.Length > 0) playText = char.ToUpper(playText[0]) + playText.Substring(1);

        if (GUI.Button(new Rect(panelX + panelW - btnActionW - 30, actionY, btnActionW, btnH), playText + " →", playBtnStyle))
        {
            AudioManager.Instance?.PlayClick();
            StartGame();
        }
    }

    private void DrawTutorialScreen()
    {
        float sw = Screen.width;
        float sh = Screen.height;

        GUI.color = new Color(0f, 0f, 0f, 0.70f);
        GUI.DrawTexture(new Rect(0, 0, sw, 110f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.color = new Color(0.9f, 0.7f, 0.2f, 0.9f);
        GUI.DrawTexture(new Rect(sw * 0.02f, 108f, sw * 0.96f, 2f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        DrawMedievalTitle(L("tutorial"), 0, -5f, sw, 90f);
        GUI.Label(new Rect(0, 80f, sw, 25f), L("tuto_subtitle"), subtitleStyle);

        float panelW = 820f;
        float panelH = Mathf.Min(600f, sh - 120f);
        float panelX = (sw - panelW) / 2f;
        float panelY = 125f;

        GUI.Box(new Rect(panelX, panelY, panelW, panelH), "", panelStyle);
        DrawOrnateFrame(panelX, panelY, panelW, panelH);

        float col1X = panelX + 30f;
        float col2X = panelX + panelW / 2f + 10f;
        float colW  = panelW / 2f - 40f;
        float y = panelY + 20f;

        GUIStyle keyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 13,
            fontStyle = FontStyle.Bold,
            normal    = { textColor = new Color(1f, 0.9f, 0.4f) }
        };
        GUIStyle descStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            normal   = { textColor = new Color(0.85f, 0.85f, 0.85f) }
        };

        // --- Colonne gauche : Contrôles ---
        GUI.Label(new Rect(col1X, y, colW, 22f), "⚔  " + L("tuto_controls"), headerStyle);
        y += 28f;

        (string k, string desc)[] controls =
        {
            (L("tuto_k_lclick"),       L("tuto_d_select")),
            (L("tuto_k_shift_lclick"), L("tuto_d_multisel")),
            (L("tuto_k_drag"),         L("tuto_d_areasel")),
            (L("tuto_k_rclick_gnd"),   L("tuto_d_move")),
            (L("tuto_k_attack_key"),    L("tuto_d_atkmove")),
            (L("tuto_k_a_lclick"),     L("tuto_d_attack")),
            (L("tuto_k_spell_key"),    L("tuto_d_spell")),
            ("E",                      L("tuto_d_disembark")),
            ("Escape",                 L("tuto_d_menu")),
        };

        float keyW  = 152f;
        float descX = col1X + keyW + 6f;
        float descW = colW - keyW - 6f;
        foreach (var (k, desc) in controls)
        {
            GUI.Label(new Rect(col1X, y, keyW,  18f), k,    keyStyle);
            GUI.Label(new Rect(descX, y, descW, 18f), desc, descStyle);
            y += 24f;
        }

        // --- Colonne droite : Objectif + Bonus de race ---
        float y2 = panelY + 20f;

        GUI.Label(new Rect(col2X, y2, colW, 22f), "🏰  " + L("tuto_objective"), headerStyle);
        y2 += 28f;

        string[] rules =
        {
            L("tuto_obj_1"), L("tuto_obj_2"), L("tuto_obj_3"),
            L("tuto_obj_4"), L("tuto_obj_5"),
            L("tuto_obj_6"), L("tuto_obj_7"),
        };
        foreach (string line in rules)
        {
            GUI.Label(new Rect(col2X, y2, colW, 20f), line, descStyle);
            y2 += 20f;
        }

        y2 += 8f;
        GUI.color = new Color(0.8f, 0.65f, 0.2f, 0.4f);
        GUI.DrawTexture(new Rect(col2X, y2, colW, 1f), Texture2D.whiteTexture);
        GUI.color = Color.white;
        y2 += 8f;

        GUI.Label(new Rect(col2X, y2, colW, 22f), "⚜  " + L("tuto_race_bonus"), headerStyle);
        y2 += 26f;

        GUIStyle raceNameStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 15,
            fontStyle = FontStyle.Bold,
        };
        GUIStyle raceBonusStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            normal   = { textColor = new Color(0.88f, 0.92f, 0.88f) }
        };

        GUI.color = new Color(0.3f, 0.5f, 1.0f);
        GUI.Label(new Rect(col2X, y2, colW, 20f), L("race_human"), raceNameStyle);
        GUI.color = Color.white;
        y2 += 22f;
        GUI.Label(new Rect(col2X + 10f, y2, colW - 10f, 20f), L("tuto_human_b"), raceBonusStyle);
        y2 += 26f;

        GUI.color = new Color(0.2f, 0.85f, 0.3f);
        GUI.Label(new Rect(col2X, y2, colW, 20f), L("race_elf"), raceNameStyle);
        GUI.color = Color.white;
        y2 += 22f;
        GUI.Label(new Rect(col2X + 10f, y2, colW - 10f, 20f), L("tuto_elf_b1"), raceBonusStyle);
        y2 += 20f;
        GUI.Label(new Rect(col2X + 10f, y2, colW - 10f, 20f), L("tuto_elf_b2"), raceBonusStyle);
        y2 += 26f;

        GUI.color = new Color(1.0f, 0.3f, 0.3f);
        GUI.Label(new Rect(col2X, y2, colW, 20f), L("race_demon"), raceNameStyle);
        GUI.color = Color.white;
        y2 += 22f;
        GUI.Label(new Rect(col2X + 10f, y2, colW - 10f, 20f), L("tuto_demon_b1"), raceBonusStyle);
        y2 += 20f;
        GUI.Label(new Rect(col2X + 10f, y2, colW - 10f, 20f), L("tuto_demon_b2"), raceBonusStyle);

        float bw = 240f;
        float bx = (sw - bw) * 0.5f;
        float by = panelY + panelH - 70f;

        GUI.color = new Color(0.8f, 0.65f, 0.2f, 0.6f);
        GUI.DrawTexture(new Rect(panelX + 20, by - 10, panelW - 40, 1f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        if (GUI.Button(new Rect(bx, by, bw, 52f), "← " + L("back"), backBtnStyle))
        {
            AudioManager.Instance?.PlayClick();
            current = MenuScreen.Title;
        }
    }

    private void DrawOptionsScreen()
    {
        float sw = Screen.width;
        float sh = Screen.height;


        GUI.color = new Color(0f, 0f, 0f, 0.70f);
        GUI.DrawTexture(new Rect(0, 0, sw, 110f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.color = new Color(0.4f, 0.6f, 0.9f, 0.9f);
        GUI.DrawTexture(new Rect(sw * 0.02f, 108f, sw * 0.96f, 2f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        DrawMedievalTitle(L("options"), 0, -5f, sw, 90f);
        GUI.Label(new Rect(0, 80f, sw, 25f), "Ajustez vos sens", subtitleStyle);

        float panelW = 500f;
        float panelH = Mathf.Min(300f, sh - 140f);
        float panelX = (sw - panelW) / 2f;
        float panelY = 125f;

        GUI.Box(new Rect(panelX, panelY, panelW, panelH), "", panelStyle);
        DrawOrnateFrame(panelX, panelY, panelW, panelH, new Color(0.4f, 0.6f, 0.9f, 0.8f));

        float contentX = panelX + 40f;
        float contentW = panelW - 80f;
        float y = panelY + 40f;

        GUI.Label(new Rect(panelX, y - 20, panelW, 24f), "♫ Paramètres Audio", headerStyle);
        y += 30f;


        GUI.Label(new Rect(contentX, y, contentW, 24f), $"{L("options_music")} : {Mathf.RoundToInt(musicVolume * 100f)}%", labelStyle);
        y += 30f;
        float newMusic = GUI.HorizontalSlider(new Rect(contentX, y, contentW, 24f), musicVolume, 0f, 1f, sliderStyle, sliderThumbStyle);
        if (!Mathf.Approximately(newMusic, musicVolume))
        {
            musicVolume = newMusic;
            AudioManager.Instance?.SetMusicVolume(musicVolume);
            PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        }
        y += 50f;


        GUI.Label(new Rect(contentX, y, contentW, 24f), $"{L("options_sfx")} : {Mathf.RoundToInt(sfxVolume * 100f)}%", labelStyle);
        y += 30f;
        float newSFX = GUI.HorizontalSlider(new Rect(contentX, y, contentW, 24f), sfxVolume, 0f, 1f, sliderStyle, sliderThumbStyle);
        if (!Mathf.Approximately(newSFX, sfxVolume))
        {
            sfxVolume = newSFX;
            AudioManager.Instance?.SetSFXVolume(sfxVolume);
            PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        }


        float bw = 240f;
        float bx = (sw - bw) * 0.5f;
        float by = panelY + panelH - 70f;
        
        GUI.color = new Color(0.4f, 0.6f, 0.9f, 0.3f);
        GUI.DrawTexture(new Rect(panelX + 20, by - 10, panelW - 40, 1f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        if (GUI.Button(new Rect(bx, by, bw, 52f), "← " + L("back"), backBtnStyle))
        {
            AudioManager.Instance?.PlayClick();
            PlayerPrefs.Save();
            current = MenuScreen.Title;
        }
    }

    private void DrawOrnateFrame(float x, float y, float w, float h, Color? customColor = null)
    {
        Color col = customColor ?? new Color(0.8f, 0.65f, 0.2f, 0.8f);
        GUI.color = col;
        float bSize = 3f;
        GUI.DrawTexture(new Rect(x, y, w, bSize), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x, y + h - bSize, w, bSize), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x, y, bSize, h), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x + w - bSize, y, bSize, h), Texture2D.whiteTexture);
        

        float corner = 12f;
        GUI.DrawTexture(new Rect(x-2, y-2, corner, corner), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x+w-corner+2, y-2, corner, corner), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x-2, y+h-corner+2, corner, corner), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x+w-corner+2, y+h-corner+2, corner, corner), Texture2D.whiteTexture);
        
        GUI.color = Color.white;
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
            fontSize  = 85,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = new Color(1f, 0.95f, 0.6f) },
            hover     = { textColor = new Color(1f, 0.95f, 0.6f) },
            active    = { textColor = new Color(1f, 0.95f, 0.6f) },
            focused   = { textColor = new Color(1f, 0.95f, 0.6f) }
        };

        subtitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 18,
            fontStyle = FontStyle.Italic,
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = new Color(0.7f, 0.8f, 0.9f) },
            hover     = { textColor = new Color(0.7f, 0.8f, 0.9f) }
        };

        headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = new Color(1f, 0.9f, 0.7f) },
            hover     = { textColor = new Color(1f, 0.9f, 0.7f) }
        };

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 16,
            fontStyle = FontStyle.Bold,
            normal    = { textColor = new Color(0.85f, 0.75f, 0.55f) },
            hover     = { textColor = new Color(0.85f, 0.75f, 0.55f) }
        };


        mainBtnStyle = CreateBtnStyle(texBtnPlay, texBtnPlayHover, texBtnPressed, 26, new Color(0.8f, 1f, 0.5f), new Color(1f, 1f, 0.8f));
        optionsBtnStyle = CreateBtnStyle(texBtnOptions, texBtnOptionsHover, texBtnPressed, 26, new Color(0.6f, 0.8f, 1.0f), new Color(0.9f, 0.95f, 1.0f));
        quitBtnStyle = CreateBtnStyle(texBtnQuit, texBtnQuitHover, texBtnPressed, 26, new Color(1.0f, 0.6f, 0.6f), new Color(1.0f, 0.8f, 0.8f));


        RectOffset smallPadding = new RectOffset(2, 2, 2, 2);
        buttonStyle = CreateBtnStyle(texBtnNormal, texBtnHover, texBtnPressed, 13, new Color(0.80f, 0.75f, 0.65f), new Color(1f, 0.95f, 0.85f), smallPadding);
        selectedButtonStyle = CreateBtnStyle(texBtnSelected, texBtnSelectedHover, texBtnPressed, 13, new Color(1f, 0.90f, 0.20f), new Color(1f, 1f, 0.5f), smallPadding);
        
        backBtnStyle = CreateBtnStyle(texBtnBack, texBtnBackHover, texBtnPressed, 20, new Color(0.85f, 0.85f, 0.90f), Color.white);
        playBtnStyle = CreateBtnStyle(texBtnPlay, texBtnPlayHover, texBtnPressed, 20, new Color(0.6f, 1f, 0.5f), Color.white);

        panelStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = texPanel },
            border = new RectOffset(4, 4, 4, 4)
        };


        sliderStyle = new GUIStyle(GUI.skin.horizontalSlider)
        {
            normal = { background = texSliderTrack },
            fixedHeight = 12,
            border = new RectOffset(4, 4, 0, 0)
        };

        sliderThumbStyle = new GUIStyle(GUI.skin.horizontalSliderThumb)
        {
            normal = { background = texSliderThumb },
            hover = { background = texSliderThumbHover },
            active = { background = texSliderThumb },
            fixedWidth = 24,
            fixedHeight = 24
        };
    }

    private GUIStyle CreateBtnStyle(Texture2D normal, Texture2D hover, Texture2D active, int fontSize, Color textNormal, Color textHover, RectOffset customPadding = null)
    {
        return new GUIStyle(GUI.skin.button)
        {
            fontSize  = fontSize,
            fontStyle = FontStyle.Bold,
            normal    = { background = normal, textColor = textNormal },
            hover     = { background = hover,  textColor = textHover },
            active    = { background = active, textColor = textNormal * 0.7f },
            border    = new RectOffset(8, 8, 8, 8),
            padding   = customPadding ?? new RectOffset(10, 10, 10, 10),
            alignment = TextAnchor.MiddleCenter
        };
    }



    // --- Texture Generators ---

    private static Texture2D MakeVignetteTex(int w, int h, Color edgeColor, Color centerColor)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[w * h];
        float cx = w / 2f;
        float cy = h / 2f;
        float maxDist = Mathf.Sqrt(cx * cx + cy * cy);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                float t = Mathf.SmoothStep(0f, 1f, dist / maxDist);
                pixels[y * w + x] = Color.Lerp(centerColor, edgeColor, t);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static Texture2D MakePremiumTex(int w, int h, Color fill, Color border, Color glow, int bSize, float glowSpread, bool isPressed = false)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[w * h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float nx = (float)x / w;
                float ny = (float)y / h;
                

                float noise = (Mathf.PerlinNoise(nx * 15f, ny * 15f) - 0.5f) * 0.15f;
                

                bool isBorder = x < bSize || x >= w - bSize || y < bSize || y >= h - bSize;
                
                if (isBorder)
                {

                    float bevel = 1f;
                    if (y >= h - bSize) bevel = isPressed ? 1.2f : 0.5f;
                    else if (y < bSize) bevel = isPressed ? 0.5f : 1.2f;
                    else if (x >= w - bSize) bevel = 0.7f;
                    else if (x < bSize) bevel = 0.9f;

                    pixels[y * w + x] = new Color(
                        Mathf.Clamp01(border.r * bevel),
                        Mathf.Clamp01(border.g * bevel),
                        Mathf.Clamp01(border.b * bevel), 1f);
                }
                else
                {

                    float distToEdge = Mathf.Min(Mathf.Min(x, w - x), Mathf.Min(y, h - y));
                    float glowFactor = 0f;
                    if (glowSpread > 0 && distToEdge < glowSpread + bSize)
                    {
                        glowFactor = 1f - ((distToEdge - bSize) / glowSpread);
                        glowFactor = Mathf.SmoothStep(0f, 1f, glowFactor);
                    }

                    Color baseFill = new Color(
                        Mathf.Clamp01(fill.r + noise),
                        Mathf.Clamp01(fill.g + noise),
                        Mathf.Clamp01(fill.b + noise), fill.a);

                    if (glowFactor > 0 && glow.a > 0)
                    {
                        pixels[y * w + x] = Color.Lerp(baseFill, new Color(glow.r, glow.g, glow.b, baseFill.a), glowFactor * glow.a);
                    }
                    else
                    {
                        pixels[y * w + x] = baseFill;
                    }
                }
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static Texture2D MakeSliderTrackTex(int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {

                float ny = (float)y / h;
                float val = Mathf.Lerp(0.05f, 0.25f, ny); 
                if (y == 0 || y == h - 1) val = 0.5f;
                pixels[y * w + x] = new Color(val, val, val * 0.9f, 0.9f);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static Texture2D MakeSliderThumbTex(int size, Color coreColor)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        float radius = size / 2f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius));
                if (dist > radius)
                {
                    pixels[y * size + x] = Color.clear;
                }
                else
                {
                    float t = 1f - (dist / radius);
                    t = Mathf.Pow(t, 1.5f);
                    Color c = Color.Lerp(new Color(0.1f, 0.1f, 0.1f, 1f), coreColor, t);
                    if (dist > radius - 2f) c = new Color(0.3f, 0.3f, 0.2f, 1f);
                    pixels[y * size + x] = c;
                }
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }
}
