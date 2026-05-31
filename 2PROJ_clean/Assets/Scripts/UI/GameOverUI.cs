using UnityEngine;
using UnityEngine.SceneManagement;

namespace SupKonQuest
{
    public class GameOverUI : MonoBehaviour
    {
        private bool isVisible = false;
        private string winnerName = "";
        private bool localPlayerWon = false;

        private GUIStyle overlayStyle;
        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle buttonStyle;

        private void Start()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameOver += HandleGameOver;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameOver -= HandleGameOver;
        }

        private void HandleGameOver(PlayerData winner)
        {
            isVisible = true;
            winnerName = winner.playerName;
            localPlayerWon = winner.playerId == GameManager.Instance.localPlayerId;
            Time.timeScale = 0f;
        }

        private void OnGUI()
        {
            if (!isVisible) return;
            InitStyles();

            GUI.color = new Color(0f, 0f, 0f, 0.7f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float w = 420f;
            float h = 220f;
            float x = (Screen.width - w) / 2f;
            float y = (Screen.height - h) / 2f;

            GUI.Box(new Rect(x - 10, y - 10, w + 20, h + 20), "", overlayStyle);

            string headline = localPlayerWon ? "Victoire !" : "DÃ©faite !";
            GUI.color = localPlayerWon ? Color.yellow : new Color(1f, 0.4f, 0.4f);
            GUI.Label(new Rect(x, y, w, 70f), headline, titleStyle);
            GUI.color = Color.white;

            GUI.Label(new Rect(x, y + 75f, w, 40f), $"{winnerName} a conquis la carte !", subtitleStyle);

            if (GUI.Button(new Rect(x + 30f, y + 140f, 160f, 50f), "Rejouer", buttonStyle))
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }

            if (GUI.Button(new Rect(x + 230f, y + 140f, 160f, 50f), "Menu principal", buttonStyle))
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene("MainMenu");
            }
        }

        private void InitStyles()
        {
            if (overlayStyle != null) return;

            overlayStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeTex(1, 1, new Color(0.1f, 0.1f, 0.1f, 0.95f)) }
            };

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 52,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.85f, 0.85f, 0.85f) }
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold
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
}
