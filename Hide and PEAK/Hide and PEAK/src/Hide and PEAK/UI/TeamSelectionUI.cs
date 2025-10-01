using ExitGames.Client.Photon;
using HideAndSeekMod;
using Photon.Pun;
using UnityEngine;

namespace Hide_and_PEAK.UI
{
    public class TeamSelectionUI : MenuWindow
    {
        public bool showUI = false;
        private bool isReady = false;

        private GUIStyle panelStyle;
        private GUIStyle buttonStyle;
        private GUIStyle readyButtonStyle;
        private GUIStyle labelStyle;
        private GUIStyle titleStyle;
        private bool stylesCreated = false;

        private Vector2 hidersScrollPos;
        private Vector2 seekersScrollPos;

        private readonly float buttonHeight = 50f;
        private readonly float buttonWidthFactor = 0.65f;
        private readonly float buttonBottomPadding = 25f;

        public static TeamSelectionUI Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            } else
            {
                Plugin.Log.LogWarning("[TeamSelectionUI] Instance already exists, destroying old one.");
                Destroy(Instance);
                Instance = this;
                return;
            }
            DontDestroyOnLoad(gameObject);
            StartClosed();
            Plugin.Log.LogInfo("[TeamSelectionUI] Awake: Team UI initialized.");
        }

        private void Update()
        {
            if (!PhotonNetwork.InRoom) return;
            if (showUI)
            {
                CheckForReady();
            }
        }

        public void SetTeamSelectionUI(bool visible)
        {
            showUI = visible;
            if (visible)
                base.Open();
            else
                base.Close();
        }

        private void OnGUI()
        {
            if (!showUI) return;

            if (!stylesCreated)
            {
                CreateGUIStyles();
                stylesCreated = true;
            }

            DrawTeamUI();
        }

        private void CreateGUIStyles()
        {
            Font unicodeFont = Resources.Load<Font>("Fonts/NotoSans-Regular");

            panelStyle = new GUIStyle(GUI.skin.box)
            {
                font = unicodeFont,
                normal = { background = MakeTex(4, 4, new Color(0.12f, 0.12f, 0.12f, 0.85f)) },
                border = new RectOffset(12, 12, 12, 12),
                padding = new RectOffset(15, 15, 15, 15)
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                font = unicodeFont,
                normal = { textColor = Color.white },
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(12, 12, 8, 8)
            };

            readyButtonStyle = new GUIStyle(buttonStyle);

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                font = unicodeFont,
                normal = { textColor = new Color(0.9f, 0.9f, 0.9f) },
            fontSize = 16,
            alignment = TextAnchor.MiddleLeft,
            richText = true
            };

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                font = unicodeFont,
                normal = { textColor = Color.white },
                fontSize = 28,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private void CheckForReady()
        {
            if (!isReady) return;
            if (HideAndSeekManager.Instance == null || HideAndSeekManager.Instance.IsGameActive) return;

            int total = PhotonNetwork.PlayerList.Length;
            int count = 0;
            foreach (var player in PhotonNetwork.PlayerList)
            {
                if (player.CustomProperties.TryGetValue("Ready", out var ready) && (bool)ready)
                    count++;
            }

            if (count >= total)
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    HideAndSeekManager.Instance.View.RPC("RPC_StartGame", RpcTarget.All);
                    HideAndSeekManager.Instance.View.RPC("RPC_SetTeamSelectionUI", RpcTarget.All, false);
                }
                SetReady(false);
            }
        }

        private void DrawTeamUI()
        {
            float halfWidth = Screen.width / 2f;
            float height = Screen.height;
            
            GUI.color = new Color(0f, 1f, 1f, 0.9f);
            GUI.DrawTexture(new Rect(halfWidth - 2f, 0, 4f, height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float listHeight = height - buttonHeight - 120;
            float teamButtonWidth = halfWidth * buttonWidthFactor;
            float buttonPadding = 10f;
            float buttonY = height - buttonHeight - buttonBottomPadding;
            
            GUILayout.BeginArea(new Rect(0, 0, halfWidth, height), panelStyle);
            GUILayout.Label("HIDERS", titleStyle);
            hidersScrollPos = GUILayout.BeginScrollView(hidersScrollPos, GUILayout.Height(listHeight));
            DrawPlayerList(Team.Hider);
            GUILayout.EndScrollView();

            if (GUI.Button(new Rect((halfWidth - teamButtonWidth) / 2f, buttonY, teamButtonWidth, buttonHeight),
                "Join Hiders", MakeColoredButton(new Color(0.25f, 0.5f, 1f))))
            {
                SetTeam(Team.Hider);
            }

            GUILayout.EndArea();
            
            GUILayout.BeginArea(new Rect(halfWidth, 0, halfWidth, height), panelStyle);
            GUILayout.Label("SEEKERS", titleStyle);
            seekersScrollPos = GUILayout.BeginScrollView(seekersScrollPos, GUILayout.Height(listHeight));
            DrawPlayerList(Team.Seeker);
            GUILayout.EndScrollView();

            if (GUI.Button(new Rect((halfWidth - teamButtonWidth) / 2f, buttonY, teamButtonWidth, buttonHeight),
                    "Join Seekers", MakeColoredButton(new Color(1f, 0.55f, 0.2f))))
            {
                SetTeam(Team.Seeker);
            }

            GUILayout.EndArea();
            
            GUIContent readyContent = new GUIContent(isReady ? "✓ Ready" : "X Not Ready");
            Vector2 readySize = readyButtonStyle.CalcSize(readyContent);
            float readyWidth = readySize.x + buttonPadding * 2f;

            readyButtonStyle.normal.background = MakeTex(4, 4,
                isReady ? new Color(0.1f, 0.6f, 0.1f, 0.95f) : new Color(0.7f, 0.15f, 0.15f, 0.95f));
            readyButtonStyle.hover.background = MakeTex(4, 4,
                isReady ? new Color(0.2f, 0.8f, 0.2f, 0.95f) : new Color(0.9f, 0.25f, 0.25f, 0.95f));

            if (GUI.Button(new Rect(Screen.width / 2f - readyWidth / 2f, buttonY, readyWidth, buttonHeight),
                readyContent, readyButtonStyle))
            {
                isReady = !isReady;
                SetReady(isReady);
            }
        }

        private void DrawPlayerList(Team team)
        {
            foreach (var player in PhotonNetwork.PlayerList)
            {
                if (player.CustomProperties.TryGetValue("Team", out var teamObj) && (Team)teamObj == team)
                {
                    bool isPlayerReady = player.CustomProperties.TryGetValue("Ready", out var ready) && (bool)ready;
                    string nameColor = "FFFFFF";
                    if (player.CustomProperties.TryGetValue("NameColor", out var colHex) && colHex is string hexStr && hexStr.Length >= 6)
                        nameColor = hexStr.Substring(0, 6);
                    string coloredName = $"<color=#{nameColor}>{player.NickName}</color>";
                    string label = coloredName + (player.IsLocal ? " (You)" : "");

                    GUIStyle entryStyle = new GUIStyle(labelStyle)
                    {
                        fontStyle = player.IsLocal ? FontStyle.Bold : FontStyle.Normal
                    };

                    Rect entryRect = GUILayoutUtility.GetRect(new GUIContent(label), entryStyle,
                        GUILayout.ExpandWidth(true), GUILayout.Height(28));
                    
                    Color pillColor = new Color(0.25f, 0.25f, 0.3f, 0.5f);
                    GUI.DrawTexture(entryRect, MakeTex(1, 1, pillColor));
                    
                    GUI.Label(new Rect(entryRect.x + 8, entryRect.y, entryRect.width - 30, entryRect.height), label, entryStyle);
                    
                    string icon = isPlayerReady ? "✓" : "X";
                    GUI.Label(new Rect(entryRect.xMax - 22, entryRect.y, 20, entryRect.height), icon, entryStyle);
                }
            }
        }

        private GUIStyle MakeColoredButton(Color baseColor)
        {
            var style = new GUIStyle(buttonStyle);
            style.normal.background = MakeTex(4, 4, baseColor);
            style.hover.background = MakeTex(4, 4, baseColor * 1.2f);
            style.active.background = MakeTex(4, 4, baseColor * 0.8f);
            return style;
        }

        private void SetTeam(Team team)
        {
            Hashtable props = new Hashtable
            {
                { "Team", team },
                { "OriginalRole", team },
                { "Caught_Time", "" },
                { "Catches", 0 },
                { "IsInGame", true }
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
            SetReady(false);
            Plugin.Log.LogInfo($"[TeamSelectionUI] You joined {team} team.");
        }

        private void SetReady(bool ready)
        {
            isReady = ready;
            Hashtable props = new Hashtable { { "Ready", ready } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
            Plugin.Log.LogInfo($"[TeamSelectionUI] You are {(ready ? "ready" : "not ready")}.");
        }
    }
}
