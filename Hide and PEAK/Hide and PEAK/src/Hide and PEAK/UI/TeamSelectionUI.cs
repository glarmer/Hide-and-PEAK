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
                Instance = this;
            else
            {
                Destroy(Instance);
                Instance = this;
                return;
            }

            DontDestroyOnLoad(gameObject);
            StartClosed();
        }

        private void Update()
        {
            if (!PhotonNetwork.InRoom) return;
            if (showUI) CheckForReady();
        }

        public void SetTeamSelectionUI(bool visible)
        {
            showUI = visible;
            if (visible) Open();
            else Close();
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
            panelStyle = UIHelper.CreatePanelStyle();

            buttonStyle = UIHelper.CreateButtonStyle();

            readyButtonStyle = new GUIStyle(buttonStyle);

            labelStyle = UIHelper.CreateLabelStyle(16, TextAnchor.MiddleLeft);

            titleStyle = UIHelper.CreateLabelStyle(28, TextAnchor.MiddleCenter, true);
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
            float buttonY = height - buttonHeight - buttonBottomPadding;

            GUILayout.BeginArea(new Rect(0, 0, halfWidth, height), panelStyle);
            GUILayout.Label("HIDERS", titleStyle);

            hidersScrollPos = GUILayout.BeginScrollView(hidersScrollPos, GUILayout.Height(listHeight));
            DrawPlayerList(Team.Hider);
            GUILayout.EndScrollView();

            if (GUI.Button(
                new Rect((halfWidth - teamButtonWidth) / 2f, buttonY, teamButtonWidth, buttonHeight),
                "Join Hiders",
                UIHelper.CreateColouredButton(buttonStyle, new Color(0.25f, 0.5f, 1f))))
            {
                SetTeam(Team.Hider);
            }

            GUILayout.EndArea();

            GUILayout.BeginArea(new Rect(halfWidth, 0, halfWidth, height), panelStyle);
            GUILayout.Label("SEEKERS", titleStyle);

            seekersScrollPos = GUILayout.BeginScrollView(seekersScrollPos, GUILayout.Height(listHeight));
            DrawPlayerList(Team.Seeker);
            GUILayout.EndScrollView();

            if (GUI.Button(
                new Rect((halfWidth - teamButtonWidth) / 2f, buttonY, teamButtonWidth, buttonHeight),
                "Join Seekers",
                UIHelper.CreateColouredButton(buttonStyle, new Color(1f, 0.55f, 0.2f))))
            {
                SetTeam(Team.Seeker);
            }

            GUILayout.EndArea();

            DrawReadyButton(buttonY);
        }

        private void DrawReadyButton(float buttonY)
        {
            GUIContent readyContent = new GUIContent(isReady ? "✓ Ready" : "X Not Ready");

            Vector2 readySize = readyButtonStyle.CalcSize(readyContent);
            float readyWidth = readySize.x + 20;

            readyButtonStyle.normal.background = UIHelper.MakeTex(4, 4,
                isReady ? new Color(0.1f, 0.6f, 0.1f, 0.95f) : new Color(0.7f, 0.15f, 0.15f, 0.95f));

            if (GUI.Button(
                new Rect(Screen.width / 2f - readyWidth / 2f, buttonY, readyWidth, buttonHeight),
                readyContent,
                readyButtonStyle))
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

                    string nameColour = "FFFFFF";
                    if (player.CustomProperties.TryGetValue("NameColour", out var colHex) && colHex is string hexStr)
                        nameColour = hexStr.Substring(0, 6);

                    string label = $"<color=#{nameColour}>{player.NickName}</color>";

                    if (player.IsLocal) label += " (You)";

                    GUIStyle entryStyle = new GUIStyle(labelStyle)
                    {
                        fontStyle = player.IsLocal ? FontStyle.Bold : FontStyle.Normal
                    };

                    Rect entryRect = GUILayoutUtility.GetRect(new GUIContent(label), entryStyle,
                        GUILayout.ExpandWidth(true), GUILayout.Height(28));

                    UIHelper.DrawBackground(entryRect, new Color(0.25f, 0.25f, 0.3f, 0.5f));

                    GUI.Label(new Rect(entryRect.x + 8, entryRect.y, entryRect.width - 30, entryRect.height),
                        label, entryStyle);

                    string icon = isPlayerReady ? "✓" : "X";

                    GUI.Label(new Rect(entryRect.xMax - 22, entryRect.y, 20, entryRect.height), icon, entryStyle);
                }
            }
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
        }

        private void SetReady(bool ready)
        {
            isReady = ready;

            Hashtable props = new Hashtable { { "Ready", ready } };

            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }
    }
}