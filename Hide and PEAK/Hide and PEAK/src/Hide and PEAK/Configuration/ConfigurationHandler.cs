using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using ExitGames.Client.Photon;
using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hide_and_PEAK.Configuration
{
    public class ConfigurationHandler
    {
        private readonly ConfigFile _config;

        
        public InputAction MenuAction { get; private set; }
        public InputAction FreezeAction { get; private set; }
        public InputAction ScoreBoardAction { get; private set; }
        public InputAction TeamSelectionUIAction { get; private set; }
        
        public ConfigEntry<string> ConfigMenuKey;
        public ConfigEntry<string> ConfigFreezeKey;
        public ConfigEntry<string> ConfigScoreBoardKey;
        public ConfigEntry<string> ConfigTeamSelectionUIKey;
        public ConfigEntry<bool> ConfigSeekerVoice;
        public ConfigEntry<int> ConfigTauntStartTime;
        public ConfigEntry<int> ConfigTauntIntervalTime;
        public ConfigEntry<int> ConfigHiderGracePeriod;
        public ConfigEntry<int> NameColourR;
        public ConfigEntry<int> NameColourG;
        public ConfigEntry<int> NameColourB;

        public ConfigurationHandler()
        {
            _config = new ConfigFile(Path.Combine(Paths.ConfigPath, Plugin.Name + ".cfg"), true);

            
            ConfigFreezeKey = _config.Bind(
                "General",
                "Config Freeze Key",
                "<Keyboard>/h",
                "Button to lock position"
            );
            Plugin.Log.LogInfo("ConfigurationHandler: Config Freeze Key: " + ConfigFreezeKey.Value);
            FreezeAction = SetupInputAction(ConfigFreezeKey.Value);
            ConfigFreezeKey.SettingChanged += OnFreezeKeyChanged;

            
            ConfigMenuKey = _config.Bind(
                "General",
                "Config Menu Key",
                "<Keyboard>/f9",
                "Control path for opening the mod configuration menu (e.g. <Keyboard>/f2, <Keyboard>/space, <Keyboard>/escape)"
            );
            Plugin.Log.LogInfo("ConfigurationHandler: Config Menu Key: " + ConfigMenuKey.Value);
            MenuAction = SetupInputAction(ConfigMenuKey.Value);
            ConfigMenuKey.SettingChanged += OnMenuKeyChanged;
            
            ConfigScoreBoardKey = _config.Bind(
                "General",
                "Config Score Board Key",
                "<Keyboard>/tab",
                "Control path for opening the mod score board (e.g. <Keyboard>/f2, <Keyboard>/space, <Keyboard>/escape)"
            );
            Plugin.Log.LogInfo("ConfigurationHandler: Config Score Board Key: " + ConfigScoreBoardKey.Value);
            ScoreBoardAction = SetupInputAction(ConfigScoreBoardKey.Value);
            ConfigScoreBoardKey.SettingChanged += OnScoreBoardKeyChanged;
            
            ConfigTeamSelectionUIKey = _config.Bind(
                "General",
                "Config Team Selection UI Key",
                "<Keyboard>/f4",
                "Control path for opening the mod team selection UI (e.g. <Keyboard>/f2, <Keyboard>/space, <Keyboard>/escape)"
            );
            Plugin.Log.LogInfo("ConfigurationHandler: Config Team Selection UI Key: " + ConfigTeamSelectionUIKey.Value);
            TeamSelectionUIAction = SetupInputAction(ConfigTeamSelectionUIKey.Value);
            ConfigTeamSelectionUIKey.SettingChanged += OnTeamSelectionUIKeyChanged;
            
            
            ConfigSeekerVoice = _config.Bind(
                "Gameplay",
                "Seeker Voice",
                true,
                "If enabled, seekers will be able to hear each other from any distance."
            );
            Plugin.Log.LogInfo("ConfigurationHandler: Seeker Voice Enabled: " + ConfigSeekerVoice.Value);
            
            ConfigTauntStartTime = _config.Bind(
                "Gameplay",
                "Taunt Start Time (seconds)",
                480,
                "The time before taunts start playing in seconds."
            );
            Plugin.Log.LogInfo("ConfigurationHandler: Taunt Start Time: " + ConfigTauntStartTime.Value);
            
            ConfigTauntIntervalTime = _config.Bind(
                "Gameplay",
                "Taunt Interval Time (seconds)",
                180,
                "The time between taunts in seconds."
            );
            Plugin.Log.LogInfo("ConfigurationHandler: Taunt Interval Time: " + ConfigTauntIntervalTime.Value);
            
            ConfigHiderGracePeriod = _config.Bind(
                "Gameplay",
                "Hider Grace Period",
                20,
                "The time before hiders can be caught in seconds."
            );
            Plugin.Log.LogInfo("ConfigurationHandler: Hider Grace Period: " + ConfigHiderGracePeriod.Value);

            
            NameColourR = _config.Bind("Visual", "Name Colour R", 85, new ConfigDescription("Name colour red (0-255)", new AcceptableValueRange<int>(0, 255)));
            NameColourG = _config.Bind("Visual", "Name Colour G", 170, new ConfigDescription("Name colour green (0-255)", new AcceptableValueRange<int>(0, 255)));
            NameColourB = _config.Bind("Visual", "Name Colour B", 255, new ConfigDescription("Name colour blue (0-255)", new AcceptableValueRange<int>(0, 255)));

            NameColourR.SettingChanged += (_, __) => PushNameColorToPhoton();
            NameColourG.SettingChanged += (_, __) => PushNameColorToPhoton();
            NameColourB.SettingChanged += (_, __) => PushNameColorToPhoton();
        }

        private void OnMenuKeyChanged(object sender, System.EventArgs e)
        {
            MenuAction?.Dispose();
            MenuAction = SetupInputAction(ConfigMenuKey.Value);
        }

        private void OnFreezeKeyChanged(object sender, System.EventArgs e)
        {
            FreezeAction?.Dispose();
            FreezeAction = SetupInputAction(ConfigFreezeKey.Value);
        }
        
        private void OnTeamSelectionUIKeyChanged(object sender, System.EventArgs e)
        {
            TeamSelectionUIAction?.Dispose();
            TeamSelectionUIAction = SetupInputAction(ConfigTeamSelectionUIKey.Value);
        }

        private void OnScoreBoardKeyChanged(object sender, System.EventArgs e)
        {
            ScoreBoardAction?.Dispose();
            ScoreBoardAction = SetupInputAction(ConfigScoreBoardKey.Value);
        }
        
        private InputAction SetupInputAction(string binding)
        {
            var action = new InputAction(type: InputActionType.Button);
            action.AddBinding(binding);
            action.Enable();
            return action;
        }

        public string GetHexColorRGB()
        {
            int r = Mathf.Clamp(NameColourR.Value, 0, 255);
            int g = Mathf.Clamp(NameColourG.Value, 0, 255);
            int b = Mathf.Clamp(NameColourB.Value, 0, 255);
            return r.ToString("X2") + g.ToString("X2") + b.ToString("X2");
        }

        public void PushNameColorToPhoton()
        {
            try
            {
                if (!PhotonNetwork.InRoom || PhotonNetwork.LocalPlayer == null) return;
                var hex = GetHexColorRGB();
                Hashtable props = new Hashtable { { "NameColor", hex } };
                PhotonNetwork.LocalPlayer.SetCustomProperties(props);
                Plugin.Log.LogInfo($"[Config] Updated NameColor to #{hex}");
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"[Config] Failed to update NameColor: {e}");
            }
        }
    }
}
