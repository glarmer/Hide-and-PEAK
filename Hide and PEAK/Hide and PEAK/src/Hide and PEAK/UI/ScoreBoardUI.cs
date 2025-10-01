using System.Collections.Generic;
using ExitGames.Client.Photon;
using HideAndSeekMod;
using Photon.Pun;
using UnityEngine;

namespace Hide_and_PEAK.UI
{
    public class ScoreBoardUI : MenuWindow
    {
        public bool showUI = false;
        public bool isGameOverMode = false;
        public float gameOverCountdown = 0f;

        private int _currentMatchIndex = -1; 

        private GUIStyle panelStyle;
        private GUIStyle headerStyle;
        private GUIStyle teamHeaderStyle;
        private GUIStyle playerStyle;
        private GUIStyle buttonStyle;
        private bool stylesCreated = false;

        public bool IsOpenMode = false;

        private Vector2 scrollPos;

        private string elapsedTime = "00:00";

        public static ScoreBoardUI Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Plugin.Log.LogWarning("[ScoreBoardUI] Instance already exists, destroying old one.");
                Destroy(Instance);
                Instance = this;
                return;
            }
            DontDestroyOnLoad(gameObject);
            StartClosed();
            Plugin.Log.LogInfo("[ScoreBoardUI] Awake: Scoreboard UI initialized.");
        }

        private void OnGUI()
        {
            if (!showUI) return;

            if (!stylesCreated)
            {
                CreateGUIStyles();
                stylesCreated = true;
            }

            UpdateElapsedTime();
            DrawScoreBoard();
        }

        public void SetScoreBoardUI(bool visible)
        {
            showUI = visible;
            if (visible)
                base.Open();
            else
                base.Close();
        }

        public void UpdateElapsedTime()
        {
            if (PlayerStats.Instance != null)
                elapsedTime = PlayerStats.Instance._currentTimeString;
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

            headerStyle = new GUIStyle(GUI.skin.label)
            {
                font = unicodeFont,
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
                alignment = TextAnchor.MiddleCenter
            };

            teamHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                font = unicodeFont,
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
                alignment = TextAnchor.MiddleLeft
            };

            playerStyle = new GUIStyle(GUI.skin.label)
            {
                font = unicodeFont,
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.9f, 0.9f, 0.9f) },
                alignment = TextAnchor.MiddleLeft,
                richText = true
            };

            buttonStyle = new GUIStyle(GUI.skin.label)
            {
                font = unicodeFont,
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white },
                hover = { textColor = Color.white },
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

        private void DrawScoreBoard()
        {
            float panelWidth = Screen.width * 0.75f;
            float panelHeight = Screen.height * 0.8f;
            float panelX = Screen.width / 2f - panelWidth / 2f;
            float panelY = Screen.height * 0.1f;

            GUILayout.BeginArea(new Rect(panelX, panelY, panelWidth, panelHeight), panelStyle);

            float headerHeight = panelHeight * 0.12f;

            Rect titleRect = new Rect(0, 0, panelWidth, headerHeight * 0.6f);
            string title;

            if (_currentMatchIndex >= 0 && _currentMatchIndex < MatchHistoryManager.Matches.Count)
            {
                var match = MatchHistoryManager.Matches[_currentMatchIndex];
                title = $"Match: {match.Date} | Duration: {FormatTime(match.Duration)}";
            }
            else
            {
                title = isGameOverMode ? "GAME OVER!" : "Hide & Seek Scoreboard";
            }

            GUI.Label(titleRect, title, headerStyle);

            string rightText = isGameOverMode
                ? $"Restarting: {Mathf.CeilToInt(gameOverCountdown)}s"
                : (_currentMatchIndex >= 0 ? "" : $"Time: {elapsedTime}");
            Vector2 timeSize = headerStyle.CalcSize(new GUIContent(rightText));
            Rect timeRect = new Rect(panelWidth - timeSize.x - 10, 0, timeSize.x, headerHeight * 0.6f);
            GUI.Label(timeRect, rightText, headerStyle);

            GUILayout.Space(headerHeight * 0.65f);

            scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.ExpandHeight(true));
            float rowHeight = panelHeight * 0.05f;

            
            GUILayout.BeginHorizontal();
            if (DrawFlatButton("< Previous", new Color(0.25f, 0.5f, 1f)))
                ShowPreviousMatch();
            GUILayout.FlexibleSpace();
            if (DrawFlatButton("Next >", new Color(1f, 0.55f, 0.2f)))
                ShowNextMatch();
            GUILayout.EndHorizontal();

            if (_currentMatchIndex >= 0 && _currentMatchIndex < MatchHistoryManager.Matches.Count)
            {
                DrawMatch(MatchHistoryManager.Matches[_currentMatchIndex], rowHeight);
            }
            else
            {
                DrawLiveGame(rowHeight);
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private bool DrawFlatButton(string text, Color baseColor)
        {
            Vector2 size = buttonStyle.CalcSize(new GUIContent(text));
            Rect rect = GUILayoutUtility.GetRect(size.x + 20, size.y + 8);

            
            Color drawColor = baseColor;
            if (rect.Contains(Event.current.mousePosition))
                drawColor *= 1.2f;

            GUI.DrawTexture(rect, MakeTex((int)rect.width, (int)rect.height, drawColor));
            GUI.Label(rect, text, buttonStyle);

            return GUI.Button(rect, GUIContent.none, GUIStyle.none);
        }

        private void DrawMatch(MatchResult match, float rowHeight)
        {
            DrawTeamHeaderFromHistory(match.PlayerResults, Team.Hider);
            foreach (var player in match.PlayerResults)
            {
                if (player.Team == Team.Hider)
                    DrawPlayerRowFromHistory(player, rowHeight);
            }

            DrawTeamHeaderFromHistory(match.PlayerResults, Team.Seeker);
            foreach (var player in match.PlayerResults)
            {
                if (player.Team == Team.Seeker)
                    DrawPlayerRowFromHistory(player, rowHeight);
            }
        }

        private void DrawTeamHeaderFromHistory(List<PlayerResult> players, Team team)
        {
            int teamCount = 0;
            int formerHiders = 0;

            foreach (var player in players)
            {
                if (player.Team == team) teamCount++;
                if (team == Team.Hider && player.OriginalRole == Team.Hider && player.Team != Team.Hider)
                    formerHiders++;
            }

            string label = team == Team.Hider
                ? $"Hiders ({teamCount}) | Former Hiders: {formerHiders}"
                : $"Seekers ({teamCount})";

            Color headerColor = team == Team.Hider
                ? new Color(0.25f, 0.5f, 1f, 0.3f)
                : new Color(1f, 0.55f, 0.2f, 0.3f);

            Rect rect = GUILayoutUtility.GetRect(new GUIContent(label), teamHeaderStyle,
                GUILayout.ExpandWidth(true), GUILayout.Height(30));

            GUI.DrawTexture(rect, MakeTex(1, 1, headerColor));
            GUI.Label(new Rect(rect.x + 8, rect.y, rect.width - 16, rect.height), label, teamHeaderStyle);
        }

        private void DrawLiveGame(float rowHeight)
        {
            DrawTeamHeader(Team.Hider);
            foreach (var player in PhotonNetwork.PlayerList)
            {
                if (player.CustomProperties.TryGetValue("Team", out var teamObj) && (Team)teamObj == Team.Hider)
                    DrawPlayerRow(player, Team.Hider, rowHeight);
            }

            DrawTeamHeader(Team.Seeker);
            foreach (var player in PhotonNetwork.PlayerList)
            {
                if (player.CustomProperties.TryGetValue("Team", out var teamObj) && (Team)teamObj == Team.Seeker)
                    DrawPlayerRow(player, Team.Seeker, rowHeight);
            }
        }

        private void ShowPreviousMatch()
        {
            if (MatchHistoryManager.Matches.Count == 0) return;
            if (_currentMatchIndex == -1) _currentMatchIndex = MatchHistoryManager.Matches.Count;
            _currentMatchIndex = Mathf.Max(0, _currentMatchIndex - 1);
        }

        private void ShowNextMatch()
        {
            if (_currentMatchIndex == -1) return;
            _currentMatchIndex++;
            if (_currentMatchIndex >= MatchHistoryManager.Matches.Count) _currentMatchIndex = -1;
        }

        private void DrawPlayerRowFromHistory(PlayerResult player, float rowHeight)
        {
            string coloredName = $"<color=#FFFFFF>{player.PlayerName}</color>";
            string label = coloredName;

            if (player.OriginalRole != player.Team)
                label += $" | Original: {player.OriginalRole}";

            if (!string.IsNullOrEmpty(player.CaughtTime))
                label += $" | Caught: {player.CaughtTime}";

            if (player.Team == Team.Seeker)
                label += $" | Catches: {player.Catches}";

            Color rowColor = new Color(0.25f, 0.25f, 0.3f, 0.5f);
            Rect rowRect = GUILayoutUtility.GetRect(new GUIContent(label), playerStyle,
                GUILayout.ExpandWidth(true), GUILayout.Height(rowHeight));

            GUI.DrawTexture(rowRect, MakeTex(1, 1, rowColor));
            GUI.Label(new Rect(rowRect.x + 8, rowRect.y, rowRect.width - 16, rowRect.height), label, playerStyle);
        }

        private void DrawTeamHeader(Team team)
        {
            int teamCount = 0;
            int formerHiders = 0;

            foreach (var player in PhotonNetwork.PlayerList)
            {
                if (player.CustomProperties.TryGetValue("Team", out var teamObj))
                {
                    if ((Team)teamObj == team) teamCount++;
                }

                if (team == Team.Hider &&
                    player.CustomProperties.TryGetValue("OriginalRole", out var role) &&
                    (Team)role == Team.Hider &&
                    player.CustomProperties.TryGetValue("Team", out var currentTeamObj) &&
                    (Team)currentTeamObj != Team.Hider)
                {
                    formerHiders++;
                }
            }

            string label = team == Team.Hider
                ? $"Hiders ({teamCount}) | Former Hiders: {formerHiders}"
                : $"Seekers ({teamCount})";

            Color headerColor = team == Team.Hider
                ? new Color(0.25f, 0.5f, 1f, 0.3f)
                : new Color(1f, 0.55f, 0.2f, 0.3f);

            Rect rect = GUILayoutUtility.GetRect(new GUIContent(label), teamHeaderStyle,
                GUILayout.ExpandWidth(true), GUILayout.Height(30));

            GUI.DrawTexture(rect, MakeTex(1, 1, headerColor));
            GUI.Label(new Rect(rect.x + 8, rect.y, rect.width - 16, rect.height), label, teamHeaderStyle);
        }

        private void DrawPlayerRow(Photon.Realtime.Player player, Team team, float rowHeight)
        {
            Team currentTeam = team;
            Team originalRole = currentTeam;
            if (player.CustomProperties.TryGetValue("OriginalRole", out var original))
            {
                originalRole = (Team)original;
            }

            string caughtTime = player.CustomProperties.TryGetValue("Caught_Time", out var ct) ? (string)ct : "";
            int catchesValue = player.CustomProperties.TryGetValue("Catches", out var catches) ? (int)catches : 0;

            string nameColor = "FFFFFF";
            if (player.CustomProperties.TryGetValue("NameColor", out var colHex) && colHex is string hexStr && hexStr.Length >= 6)
                nameColor = hexStr.Substring(0, 6);
            string coloredName = $"<color=#{nameColor}>{player.NickName}</color>";
            string label = coloredName + (player.IsLocal ? " (You)" : "");
            if (originalRole != currentTeam)
                label += $" | Original: {originalRole}";
            if (!string.IsNullOrEmpty(caughtTime))
                label += $" | Caught: {caughtTime}";
            if (team == Team.Seeker)
                label += $" | Catches: {catchesValue}";

            Color rowColor = new Color(0.25f, 0.25f, 0.3f, 0.5f);

            Rect rowRect = GUILayoutUtility.GetRect(new GUIContent(label), playerStyle,
                GUILayout.ExpandWidth(true), GUILayout.Height(rowHeight));

            GUI.DrawTexture(rowRect, MakeTex(1, 1, rowColor));
            GUI.Label(new Rect(rowRect.x + 8, rowRect.y, rowRect.width - 16, rowRect.height), label, playerStyle);
        }

        private string FormatTime(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            return $"{minutes:00}:{secs:00}";
        }
    }
}
