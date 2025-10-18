using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Demos.DemoPunVoice;
using Hide_and_PEAK;
using Hide_and_PEAK.UI;
using Hide_and_PEAK.Voice;
using UnityEngine;
using Photon;
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace HideAndSeekMod;

public class HideAndSeekManager : MonoBehaviourPunCallbacks
{
    public static HideAndSeekManager Instance;
    private readonly HashSet<int> _processingCaught = new HashSet<int>();
    private bool _gameEndSequenceActive = false;
    private Coroutine _endGameCoroutine;
    
    public bool IsHider => PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("Team", out var teamObj) && (Team)teamObj == Team.Hider;
    public bool IsSeeker => PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("Team", out var teamObj) && (Team)teamObj == Team.Seeker;
    public PhotonView View;
    
    public float HiderGracePeriod = Plugin.ConfigurationHandler.ConfigHiderGracePeriod.Value;

    public bool IsGameActive = false;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        } else
        {
            Plugin.Log.LogWarning("[HideAndSeekManager] Instance already exists, destroying old one.");
            Destroy(Instance);
            Instance = this;
            return;
        }
        View = transform.GetComponent<PhotonView>();
        
        if (View == null)
            Plugin.Log.LogError("No PhotonView attached!");

        Plugin.Log.LogInfo($"PhotonView ID: {View.ViewID}, IsMine: {View.IsMine}, Owner: {View.Owner}");
        
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        CheckIfInGame();
    }

    private void CheckIfInGame()
    {
        Plugin.Log.LogInfo("[HideAndSeekManager] Checking if in game...");
        if (PhotonNetwork.MasterClient.CustomProperties.TryGetValue("IsInGame", out var isInGame) && (bool)isInGame)
        {
            Plugin.Log.LogInfo("[HideAndSeekManager] Found game: Starting game...");
            RejoinGame();
        }
    }


    [PunRPC]
    public void RPC_StartGame()
    {
        StartGame();
    }
    
    [PunRPC]
    public void RPC_RequestCurrentTimeString(int requesterActorNumber)
    {
        if (PlayerStats.Instance == null) return;

        float currentTime = PlayerStats.Instance._currentTime;

        
        Photon.Realtime.Player requester = PhotonNetwork.CurrentRoom.Players[requesterActorNumber];
        if (requester != null)
        {
            View.RPC("RPC_ReceiveCurrentTimeString", requester, currentTime);
            Plugin.Log.LogInfo($"[RPC_RequestCurrentTimeString] Sent _currentTimeString to {requester.NickName}");
        }
    }
    
    [PunRPC]
    public void RPC_ReceiveCurrentTimeString(float currentTime)
    {
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.SetCurrentTimeFromHost(currentTime);
            Plugin.Log.LogInfo($"[RPC_ReceiveCurrentTimeString] Updated _currentTime: {currentTime}");
        }
    }
    
    public void RejoinGame()
    {
        PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("Team", out var teamVar);
        
        Team team = (Team)teamVar;
        this.gameObject.AddComponent<HideAndSeekPlayer>();
        
        Plugin.Log.LogInfo("InitiatingUI");
        DeathLog.InitiateDeathLog();
        this.IsGameActive = true;
        
        View.RPC("RPC_RequestCurrentTimeString", PhotonNetwork.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
        Plugin.Log.LogInfo("[RequestCurrentTimeString] Requested _currentTimeString from host");

    }

    public void StartGame()
    {
        PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("Team", out var teamVar);
    
        Team team = (Team)teamVar;
        this.gameObject.AddComponent<HideAndSeekPlayer>();
    
        Plugin.Log.LogInfo("InitiatingUI");
        DeathLog.InitiateDeathLog();
        PlayerStats.InitiatePlayerStats();
        this.IsGameActive = true;
        
        
        if (IsHost())
        {
            View.RPC("RPC_SetHiderGracePeriod", RpcTarget.Others, HiderGracePeriod);
            Plugin.Log.LogInfo($"[HideAndSeekManager] Broadcasting HiderGracePeriod ({HiderGracePeriod}) to clients");

            
            View.RPC("RPC_SetSoundIntervals", RpcTarget.Others,
                SoundPlayer.Instance?.nextSoundTime ?? Plugin.ConfigurationHandler.ConfigTauntStartTime.Value,
                SoundPlayer.Instance?.soundInterval ?? Plugin.ConfigurationHandler.ConfigTauntIntervalTime.Value
            );
            Plugin.Log.LogInfo("[HideAndSeekManager] Broadcasting SoundPlayer intervals to clients");
        }
        if (SoundPlayer.Instance != null)
        {
            SoundPlayer.Instance.nextSoundTime = PlayerStats.Instance._currentTime + Plugin.ConfigurationHandler.ConfigTauntStartTime.Value;
        }
    }
    
    [PunRPC]
    public void RPC_SetSoundIntervals(float nextSound, float interval)
    {
        if (SoundPlayer.Instance != null)
        {
            SoundPlayer.Instance.nextSoundTime = nextSound;
            SoundPlayer.Instance.soundInterval = interval;
            Plugin.Log.LogInfo($"[HideAndSeekManager] Sound intervals synced from host: nextSound={nextSound}, interval={interval}");
        }
    }

    
    [PunRPC]
    public void RPC_SetHiderGracePeriod(float gracePeriod)
    {
        HiderGracePeriod = gracePeriod;
        Plugin.Log.LogInfo($"[HideAndSeekManager] HiderGracePeriod set to {gracePeriod} by host.");
    }

    
    [PunRPC]
    public void RPC_SetFrozenState(int viewId, bool isFrozen, float frozenHeight, float frozenStamina)
    {
        var pv = PhotonView.Find(viewId);
        if (pv == null)
        {
            Plugin.Log.LogWarning($"[FreezeSync] Could not find PhotonView for {viewId}");
            return;
        }

        var character = pv.GetComponent<Character>();
        if (character == null)
        {
            Plugin.Log.LogWarning($"[FreezeSync] No Character component on PhotonView {viewId}");
            return;
        }

        if (!isFrozen)
        {
            Plugin.Log.LogInfo($"[FreezeSync] Unfreezing {character.characterName}");
            return; 
        }

        
        var torso = character.GetBodypart(BodypartType.Torso);
        if (torso?.Rig != null)
        {
            torso.Rig.position = torso.Rig.position with { y = frozenHeight };
            torso.Rig.linearVelocity = Vector3.zero;
            torso.Rig.angularVelocity = Vector3.zero;
        }

        character.data.currentStamina = frozenStamina;
        character.input.movementInput = Vector2.zero;
        character.input.jumpIsPressed = false;
        character.input.crouchIsPressed = false;

        Plugin.Log.LogInfo($"[FreezeSync] Applied freeze to {character.characterName}, height={frozenHeight}");
    }

    public void SyncFreezeState(Character character, bool isFrozen, float frozenHeight, float frozenStamina)
    {
        if (character?.refs?.view == null) return;
        View.RPC("RPC_SetFrozenState", RpcTarget.Others, character.refs.view.ViewID, isFrozen, frozenHeight, frozenStamina);
    }
    
    
    [PunRPC]
    public void RPC_RequestCatch(int seekerViewId, int hiderViewId)
    {
        if (!IsHost()) return;

        Character seeker = Character.AllCharacters.Find(c => c.refs.view.ViewID == seekerViewId);
        Character hider = Character.AllCharacters.Find(c => c.refs.view.ViewID == hiderViewId);

        if (seeker == null || hider == null) return;

        Plugin.Log.LogInfo($"[HideAndSeekPlayer] Host processing catch: {seeker.characterName} -> {hider.characterName}");
        CatchHider(seeker, hider);
    }

    public void CatchHider(Character seekerCharacter, Character hiderCharacter)
    {
        if (!IsHost()) return;
        if (PlayerStats.Instance._currentTime < HiderGracePeriod)
        {
            Plugin.Log.LogInfo("[HideAndSeekPlayer] Catch ignored, hider grace period.");
            return;
        }

        Photon.Realtime.Player hider = hiderCharacter.view.Controller;
        Photon.Realtime.Player seeker = seekerCharacter.view.Controller;
    
        hider.CustomProperties.TryGetValue("Team", out var hiderTeam);
        seeker.CustomProperties.TryGetValue("Team", out var seekerTeam);

        if ((Team)hiderTeam == Team.Hider && (Team)seekerTeam == Team.Seeker)
        {
            
            int hiderViewId = hiderCharacter.refs.view.ViewID;
            if (_processingCaught.Contains(hiderViewId)) return;
            _processingCaught.Add(hiderViewId);

            Plugin.Log.LogInfo("Team swap 1");
            View.RPC("RPC_ResetStamina", hider);
            View.RPC("RPC_AddDeath", RpcTarget.All, seekerCharacter.refs.view.ViewID, hiderCharacter.refs.view.ViewID);
            Plugin.Log.LogInfo("Team swap 2");
        
            Hashtable props = new Hashtable
            {
                { "Team", Team.Seeker },
                { "OriginalRole", Team.Hider },
                { "Caught_Time", PlayerStats.Instance._currentTimeString }
            };
            hider.SetCustomProperties(props);
            
            
            seeker.CustomProperties.TryGetValue("Catches", out var catches);
            int catchesInt = (int)catches + 1;
            Hashtable propsSeeker = new Hashtable
            {
                { "Catches", catchesInt }
            };
            seeker.SetCustomProperties(propsSeeker);
            Plugin.Log.LogInfo($"{seeker.NickName} now has {catchesInt} catches.");

            StartCoroutine(ClearProcessingAfterDelay(hiderViewId, 1f));

            if (IsHost())
            {
                StartCoroutine(CheckGameEndAfterDelay(0.5f));
            }
        }
    }
    
    [PunRPC]
    public void RPC_ResetStamina()
    {
        Plugin.Log.LogInfo("[HideAndSeekManager] Resetting local character stamina");
        Character.localCharacter?.AddStamina(1f);
        Character.localCharacter?.refs.afflictions.ClearAllStatus(false);
    }

    private IEnumerator ClearProcessingAfterDelay(int viewId, float delay)
    {
        yield return new WaitForSeconds(delay);
        _processingCaught.Remove(viewId);
    }

    private IEnumerator CheckGameEndAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        CheckForGameEnd();
    }
    
    private void CheckForGameEnd()
    {
        if (!IsHost() || !IsGameActive || _gameEndSequenceActive) return;

        int originalHiders = 0;
        int remainingHiders = 0;

        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (player.CustomProperties.TryGetValue("OriginalRole", out var role) && (Team)role == Team.Hider)
            {
                originalHiders++;
                if (player.CustomProperties.TryGetValue("Team", out var team) && (Team)team == Team.Hider)
                {
                    remainingHiders++;
                }
            }
        }

        Plugin.Log.LogInfo($"[GameEnd Check] Original Hiders: {originalHiders}, Remaining: {remainingHiders}");
        
        if (originalHiders > 0 && remainingHiders == 0)
        {
            Plugin.Log.LogInfo("[GameEnd] All hiders caught! Ending game...");
            _gameEndSequenceActive = true;
            View.RPC("RPC_ShowEndGameScoreboard", RpcTarget.All);
            View.RPC("RPC_ResetInfiniteStamina", RpcTarget.All);
        }
    }

    [PunRPC]
    private void RPC_ResetInfiniteStamina()
    {
        Plugin.Log.LogInfo("[HideAndSeekManager] Resetting local character infinite stamina");
        Character.localCharacter.infiniteStam = false;
    }
    
    private void SaveCurrentMatch()
    {
        MatchResult match = new MatchResult
        {
            Date = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            Duration = PlayerStats.Instance != null ? PlayerStats.Instance._currentTime : 0f
        };

        foreach (var player in PhotonNetwork.PlayerList)
        {
            
            match.PlayerResults.Add(new PlayerResult
            {
                PlayerName = player.NickName,
                Team = player.CustomProperties.TryGetValue("Team", out var teamObj) ? (Team)teamObj : Team.Seeker,
                OriginalRole = player.CustomProperties.TryGetValue("OriginalRole", out var origObj) ? (Team)origObj : Team.Seeker,
                CaughtTime = player.CustomProperties.TryGetValue("Caught_Time", out var caughtObj) ? caughtObj.ToString() : "",
                Catches = player.CustomProperties.TryGetValue("Catches", out var catchesObj) ? (int)catchesObj : 0
            });
        }

        MatchHistoryManager.SaveMatch(match);
    }

    

    [PunRPC]
    public void RPC_ShowEndGameScoreboard()
    {
        Plugin.Log.LogInfo("[GameEnd] Received RPC: ShowEndGameScoreboard");
        ShowEndGameScoreboard();
    }

    private void ShowEndGameScoreboard()
    {
        if (ScoreBoardUI.Instance != null)
        {
            ScoreBoardUI.Instance.isGameOverMode = true;
            ScoreBoardUI.Instance.gameOverCountdown = 30f;
            ScoreBoardUI.Instance.SetScoreBoardUI(true);
            ScoreBoardUI.Instance.IsOpenMode = true;
            Plugin.Log.LogInfo("[GameEnd] Scoreboard opened");
            Hashtable props = new Hashtable
            {
                { "IsInGame", false }
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }
        
        SaveCurrentMatch();
        if (_endGameCoroutine != null)
        {
            StopCoroutine(_endGameCoroutine);
        }
        _endGameCoroutine = StartCoroutine(EndGameTimer());
    }

    private IEnumerator EndGameTimer()
    {
        Plugin.Log.LogInfo("[GameEnd] Starting 30-second countdown...");
        float elapsed = 0f;
        float duration = 30f;

        while (elapsed < duration)
        {
            if (ScoreBoardUI.Instance != null)
            {
                ScoreBoardUI.Instance.gameOverCountdown = duration - elapsed;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (elapsed >= duration)
        {
            Plugin.Log.LogInfo("[GameEnd] 30 seconds elapsed, restarting game");
            ScoreBoardUI.Instance.IsOpenMode = false;
        }
        
        if (ScoreBoardUI.Instance != null && ScoreBoardUI.Instance.showUI)
        {
            ScoreBoardUI.Instance.isGameOverMode = false;
            ScoreBoardUI.Instance.gameOverCountdown = 0f;
            ScoreBoardUI.Instance.SetScoreBoardUI(false);
        }
        
        if (IsHost())
        {
            RestartGameForAll();
            _gameEndSequenceActive = false;
        }

        _endGameCoroutine = null;
    }

    [PunRPC]
    public void RPC_AddDeath(int seekerViewId, int hiderViewId)
    {
        var seeker = PhotonView.Find(seekerViewId);
        var hider = PhotonView.Find(hiderViewId);

        if (seeker == null || hider == null)
        {
            Plugin.Log.LogError($"[DeathLog] Could not resolve PhotonViews! seeker={seekerViewId}, hider={hiderViewId}");
            return;
        }

        if (DeathLog.Instance == null)
        {
            DeathLog.InitiateDeathLog();
        }
        DeathLog.Instance?.AddDeath(seeker, hider);
    }
    

    private void Update()
    {
        if (PhotonNetwork.InRoom)
        {
            if (IsHost() && Plugin.ConfigurationHandler.TeamSelectionUIAction.WasPressedThisFrame())
            {
                View.RPC("RPC_SetTeamSelectionUI", RpcTarget.All, !TeamSelectionUI.Instance.showUI);
            }
            if (Plugin.ConfigurationHandler.ScoreBoardAction.IsPressed()) 
            {
                if (!ScoreBoardUI.Instance.showUI)
                {
                    ScoreBoardUI.Instance.SetScoreBoardUI(true);
                }
                else if (ScoreBoardUI.Instance.IsOpenMode)
                {
                    ScoreBoardUI.Instance.IsOpenMode = false;
                    ScoreBoardUI.Instance.SetScoreBoardUI(false);
                }
            }
            else
            {
                if (!ScoreBoardUI.Instance.IsOpenMode && ScoreBoardUI.Instance.showUI)
                {
                    ScoreBoardUI.Instance.SetScoreBoardUI(false);
                }
            }
        }
    }

    [PunRPC]
    public void RPC_SetTeamSelectionUI(bool visible)
    {
        Plugin.Log.LogInfo($"Received RPC: SetTeamSelectionUI -> {visible}");
        if (TeamSelectionUI.Instance == null)
        {
            Plugin.Log.LogInfo("TeamSelectionUI.Instance was null. Creating UI GameObject on demand.");
            var go = new GameObject("Team Selection UI");
            go.AddComponent<TeamSelectionUI>();
        }
        TeamSelectionUI.Instance.SetTeamSelectionUI(visible);
    }

    [PunRPC]
    public void RPC_TestDeath()
    {
        try
        {
            if (DeathLog.Instance == null)
                DeathLog.InitiateDeathLog();

            var localChar = Character.localCharacter;
            var localPV = localChar != null ? localChar.refs?.view : null;
            if (localPV == null)
            {
                Plugin.Log.LogError("[DeathLog Test] Local PhotonView not found.");
                return;
            }
            
            var otherPV = Character.AllCharacters
                .Where(c => c != null && c.refs?.view != null && c.refs.view.ViewID != localPV.ViewID)
                .Select(c => c.refs.view)
                .FirstOrDefault() ?? localPV;
            
            DeathLog.Instance?.AddDeath(localPV, otherPV);
        }
        catch (Exception e)
        {
            Plugin.Log.LogError($"[DeathLog Test] Exception: {e}");
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public bool IsHost()
    {
        return PhotonNetwork.IsMasterClient;
    }
    
    public void DestroyGame()
    {
        Plugin.Log.LogInfo("[HideAndSeekManager] Destroying game state...");
        
        IsGameActive = false;
        
        _processingCaught.Clear();
        
        var hsPlayer = gameObject.GetComponent<HideAndSeekPlayer>();
        if (hsPlayer != null)
        {
            Destroy(hsPlayer);
            Plugin.Log.LogInfo("[HideAndSeekManager] Removed HideAndSeekPlayer component");
        }
        
        if (PhotonNetwork.LocalPlayer != null)
        {
            Hashtable props = new Hashtable
            {
                { "Team", null },
                { "Ready", false },
                { "OriginalRole", null },
                { "Caught_Time", "" },
                { "Catches", 0 }
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
            Plugin.Log.LogInfo("[HideAndSeekManager] Reset local player properties");
        }
        
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.ResetStats();
            Plugin.Log.LogInfo("[HideAndSeekManager] Reset PlayerStats");
        }
        
        if (DeathLog.Instance != null)
        {
            Destroy(DeathLog.Instance.gameObject);
            DeathLog.Instance = null;
            Plugin.Log.LogInfo("[HideAndSeekManager] Cleared DeathLog");
        }
        
        if (TeamSelectionUI.Instance != null && TeamSelectionUI.Instance.showUI)
        {
            TeamSelectionUI.Instance.SetTeamSelectionUI(false);
            Plugin.Log.LogInfo("[HideAndSeekManager] Closed TeamSelectionUI");
        }
        
        if (ScoreBoardUI.Instance != null && ScoreBoardUI.Instance.showUI)
        {
            ScoreBoardUI.Instance.SetScoreBoardUI(false);
            Plugin.Log.LogInfo("[HideAndSeekManager] Closed ScoreBoardUI");
        }

        Plugin.Log.LogInfo("[HideAndSeekManager] Game destroyed successfully");
    }
    
    public void InitializeNewGame()
    {
        Plugin.Log.LogInfo("[HideAndSeekManager] Initializing new game...");

        try { Plugin.ConfigurationHandler?.PushNameColorToPhoton(); } catch {}

        if (TeamSelectionUI.Instance != null)
        {
            TeamSelectionUI.Instance.SetTeamSelectionUI(true);
            Plugin.Log.LogInfo("[HideAndSeekManager] Opened TeamSelectionUI for new game");
        }
        else
        {
            Plugin.Log.LogWarning("[HideAndSeekManager] TeamSelectionUI.Instance is null, cannot open");
        }

        Plugin.Log.LogInfo("[HideAndSeekManager] New game initialized");
    }
    
    public void RestartGame()
    {
        Plugin.Log.LogInfo("[HideAndSeekManager] Restarting game...");
        DestroyGame();
        InitializeNewGame();
        Plugin.Log.LogInfo("[HideAndSeekManager] Game restarted successfully");
    }

    [PunRPC]
    public void RPC_DestroyGame()
    {
        Plugin.Log.LogInfo("[HideAndSeekManager] Received RPC: DestroyGame");
        DestroyGame();
    }

    [PunRPC]
    public void RPC_InitializeNewGame()
    {
        Plugin.Log.LogInfo("[HideAndSeekManager] Received RPC: InitializeNewGame");
        InitializeNewGame();
    }

    [PunRPC]
    public void RPC_RestartGame()
    {
        Plugin.Log.LogInfo("[HideAndSeekManager] Received RPC: RestartGame");
        RestartGame();
    }
    
    public void DestroyGameForAll()
    {
        if (!IsHost())
        {
            Plugin.Log.LogWarning("[HideAndSeekManager] Only host can destroy game for all");
            return;
        }
        Plugin.Log.LogInfo("[HideAndSeekManager] Broadcasting destroy to all clients");
        View.RPC("RPC_DestroyGame", RpcTarget.All);
    }
    
    public void InitializeNewGameForAll()
    {
        if (!IsHost())
        {
            Plugin.Log.LogWarning("[HideAndSeekManager] Only host can initialize new game for all");
            return;
        }
        Plugin.Log.LogInfo("[HideAndSeekManager] Broadcasting new game initialization to all clients");
        View.RPC("RPC_InitializeNewGame", RpcTarget.All);
    }
    
    public void RestartGameForAll()
    {
        if (!IsHost())
        {
            Plugin.Log.LogWarning("[HideAndSeekManager] Only host can restart game for all");
            return;
        }
        Plugin.Log.LogInfo("[HideAndSeekManager] Broadcasting game restart to all clients");
        View.RPC("RPC_RestartGame", RpcTarget.All);
    }
    
}