using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Hide_and_PEAK.Configuration;
using Hide_and_PEAK.Patches;
using Hide_and_PEAK.UI;
using Hide_and_PEAK.Voice;
using HideAndSeekMod;
using Photon.Pun;
using UnityEngine;

namespace Hide_and_PEAK;

[BepInAutoPlugin]
public partial class Plugin : BaseUnityPlugin
{
    internal static ManualLogSource Log { get; private set; } = null!;
    private Harmony _harmony;
    public static ConfigurationHandler ConfigurationHandler;
    private ModConfigurationUI _ui;
    private SoundPlayer _soundPlayer;

    
    public bool _isInitialised = false;
    private bool _pushedNameColor = false;
    private GameObject _teamSelectionUI;
    private GameObject _scoreBoardUI;
    private HideAndSeekManager _manager;
    private RichPresenceService _richPresenceService = null;
    public static Plugin Instance;

    private RichPresenceState lastState = RichPresenceState.Status_MainMenu;

    private void Awake()
    {
        Instance = this;
        Log = Logger;
        Log.LogInfo($"Plugin {Name} v{Version} loaded!");

        _harmony = new Harmony(Id);
        _harmony.PatchAll(typeof(BodypartOnCollisionEnterPatch));
        _harmony.PatchAll(typeof(VoiceObscuranceFilterPatch));
        _harmony.PatchAll(typeof(CharacterMovementUpdatePatch));
        _harmony.PatchAll(typeof(RunManagerStartRunPatch));
        _harmony.PatchAll(typeof(CharacterDeathPosPatch));
        
        ConfigurationHandler = new ConfigurationHandler();
        
        var go = new GameObject("HideAndPEAKConfigUI");
        DontDestroyOnLoad(go);
        _ui = go.AddComponent<ModConfigurationUI>();
        _ui.Init(new List<Option>
        {
            Option.InputAction("Menu Key", ConfigurationHandler.ConfigMenuKey),
            Option.InputAction("Freeze Key", ConfigurationHandler.ConfigFreezeKey),
            Option.InputAction("Team Selection UI Key", ConfigurationHandler.ConfigTeamSelectionUIKey),
            Option.InputAction("Score Board Key", ConfigurationHandler.ConfigScoreBoardKey),
            Option.Int("Taunt Start Time (seconds)", ConfigurationHandler.ConfigTauntStartTime, 1, 900, 10),
            Option.Int("Taunt Interval Time (seconds)", ConfigurationHandler.ConfigTauntIntervalTime, 1, 600, 10),
            Option.Int("Hider Grace Period (seconds)", ConfigurationHandler.ConfigHiderGracePeriod, 1, 60, 1),
            Option.Bool("Seeker Voice", ConfigurationHandler.ConfigSeekerVoice),
            Option.Colour("Name Colour", ConfigurationHandler.NameColourR, ConfigurationHandler.NameColourG, ConfigurationHandler.NameColourB)

        });
    }

    private void Update()
    {
        _richPresenceService = GameHandler.GetService<RichPresenceService>();
        RichPresenceState currentState = _richPresenceService?.m_currentState ?? RichPresenceState.Status_MainMenu;
        if (_richPresenceService != null && lastState != currentState)
        {
            Log.LogInfo($"[Plugin] Rich presence state changed from {lastState} to {_richPresenceService.m_currentState}");

            switch (_richPresenceService.m_currentState)
            {
                case RichPresenceState.Status_MainMenu:
                    if (_isInitialised)
                    {
                        Log.LogInfo($"[Plugin] tearing down mod");
                        TeardownMod();
                    }
                    break;
                case RichPresenceState.Status_Airport:
                    if (_isInitialised)
                    {
                        Log.LogInfo($"[Plugin] tearing down mod");
                        TeardownMod();
                    }
                    break;
                
            }
            lastState = _richPresenceService.m_currentState;
        }
        
        if (ConfigurationHandler.MenuAction.WasPerformedThisFrame())
        {
            ModConfigurationUI.Instance.ToggleMenu();
        }
        
        
        if (Input.GetKeyDown(KeyCode.K) && _soundPlayer != null)
        {
            _soundPlayer.PlayRandomSound();
        }
        
        
        if (!_pushedNameColor && PhotonNetwork.InRoom)
        {
            try { ConfigurationHandler?.PushNameColorToPhoton(); } catch {}
            _pushedNameColor = true;
        }
    }

    public void InitialiseMod()
    {
        if (_isInitialised) return;
        if (Character.localCharacter == null) return;
        Character.localCharacter.refs.afflictions.hungerPerSecond = 0;
        Log.LogInfo("[Plugin] Initialising Hide and PEAK mod...");

        
        Character host = Character.localCharacter;
        foreach (Character character in Character.AllCharacters)
        {
            if (Equals(character.player.view.Controller, PhotonNetwork.MasterClient))
            {
                Log.LogInfo("[Plugin] Found the host character");
                host = character;
            }
        }

        ConfigurationHandler.PushNameColorToPhoton();

        
        _teamSelectionUI = new GameObject("Team Selection UI");
        _teamSelectionUI.AddComponent<TeamSelectionUI>();
        DontDestroyOnLoad(_teamSelectionUI);

        
        _scoreBoardUI = new GameObject("Scoreboard UI");
        _scoreBoardUI.AddComponent<ScoreBoardUI>();
        DontDestroyOnLoad(_scoreBoardUI);

        
        _manager = host.gameObject.AddComponent<HideAndSeekManager>();

        
        _soundPlayer = host.gameObject.AddComponent<SoundPlayer>();

        
        PlayerStats.InitiatePlayerStats();

        _isInitialised = true;
        Log.LogInfo("[Plugin] Initialisation complete");
    }

    public void TeardownMod()
    {
        if (!_isInitialised) return;
        Log.LogInfo("[Plugin] Tearing down Hide and PEAK mod...");

        
        if (_manager != null)
        {
            Destroy(_manager);
            _manager = null;
        }

        
        if (_soundPlayer != null)
        {
            Destroy(_soundPlayer);
            _soundPlayer = null;
        }

        
        if (_teamSelectionUI != null) Destroy(_teamSelectionUI);
        if (_scoreBoardUI != null) Destroy(_scoreBoardUI);
        _teamSelectionUI = null;
        _scoreBoardUI = null;

        
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.ResetStats();
        }

        _pushedNameColor = false;
        _isInitialised = false;
        Log.LogInfo("[Plugin] Teardown complete, ready to initialise again");
    }

    private void OnDestroy()
    {
        TeardownMod();
        _harmony?.UnpatchSelf();
    }
    
    public static bool IsHost()
    {
        return PhotonNetwork.IsMasterClient;
    }
}
