using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Hide_and_PEAK.UI;
using HideAndSeekMod;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Networking;

namespace Hide_and_PEAK.Voice;

public class SoundPlayer : MonoBehaviour
{
    private List<AudioClip> loadedClips = new List<AudioClip>();
    private Dictionary<string, AudioClip> clipLookup = new Dictionary<string, AudioClip>();
    private bool isLoaded = false;
    public static SoundPlayer Instance;

    public float nextSoundTime; 
    public float soundInterval = Plugin.ConfigurationHandler.ConfigTauntIntervalTime.Value;

    private void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Plugin.Log.LogWarning("[SoundPlayer] Instance already exists, destroying old one.");
            Destroy(Instance);
            Instance = this;
            return;
        }

        StartCoroutine(LoadSounds());
    }

    private void Update()
    {
        if (!isLoaded || PlayerStats.Instance == null || !HideAndSeekManager.Instance.IsGameActive) return;
        if (!HideAndSeekManager.Instance.IsHost()) return; 

        float currentTime = PlayerStats.Instance._currentTime;

        if (currentTime >= nextSoundTime)
        {
            PlayRandomSound();
            nextSoundTime += soundInterval;
            Plugin.Log.LogInfo($"[SoundPlayer] Host triggered sound at {currentTime:F2}s, next at {nextSoundTime:F2}s");
        }
    }

    private IEnumerator LoadSounds()
    {
        string soundsFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "sounds");

        if (!Directory.Exists(soundsFolder))
        {
            Plugin.Log.LogWarning($"[SoundPlayer] Sounds folder not found: {soundsFolder}");
            yield break;
        }

        string[] files = Directory.GetFiles(soundsFolder, "*.wav");
        if (files.Length == 0)
        {
            Plugin.Log.LogWarning($"[SoundPlayer] No WAV files found in: {soundsFolder}");
            yield break;
        }

        Plugin.Log.LogInfo($"[SoundPlayer] Loading {files.Length} sound files...");

        foreach (string file in files)
        {
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file:///" + file, AudioType.WAV))
            {
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Plugin.Log.LogError($"[SoundPlayer] Failed to load {Path.GetFileName(file)}: {www.error}");
                }
                else
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                    clip.name = Path.GetFileNameWithoutExtension(file);

                    loadedClips.Add(clip);
                    clipLookup[clip.name] = clip;

                    Plugin.Log.LogInfo($"[SoundPlayer] Loaded: {clip.name}");
                }
            }
        }

        
        isLoaded = true;

        
        float startTime = Plugin.ConfigurationHandler.ConfigTauntStartTime.Value;
        float currentTime = PlayerStats.Instance != null ? PlayerStats.Instance._currentTime : 0f;
        nextSoundTime = currentTime + startTime;

        Plugin.Log.LogInfo($"[SoundPlayer] Finished loading {loadedClips.Count} sounds. First sound at {nextSoundTime:F2}s");
    }

    public void PlayRandomSound()
    {
        if (!isLoaded || loadedClips.Count == 0)
        {
            Plugin.Log.LogWarning("[SoundPlayer] No sounds loaded yet!");
            return;
        }

        if (!HideAndSeekManager.Instance.IsHost()) return;

        foreach (Character character in Character.AllCharacters)
        {
            Team team = character.view.Controller.CustomProperties.TryGetValue("Team", out var teamObj)
                ? (Team)teamObj
                : Team.Seeker;

            if (team == Team.Hider)
            {
                AudioClip clip = loadedClips[Random.Range(0, loadedClips.Count)];
                Vector3 pos = character.GetCameraPos(0f);
                HideAndSeekManager.Instance.View.RPC("RPC_PlaySound", RpcTarget.All, clip.name, pos);
                Plugin.Log.LogInfo($"[SoundPlayer] Host broadcasting sound: {clip.name} at {pos}");
            }
        }
    }

    [PunRPC]
    public void RPC_PlaySound(string clipName, Vector3 pos)
    {
        if (!clipLookup.TryGetValue(clipName, out var clip))
        {
            Plugin.Log.LogWarning($"[SoundPlayer] Clip not found on client: {clipName}");
            return;
        }

        SFX_Instance sfx = ScriptableObject.CreateInstance<SFX_Instance>();
        sfx.clips = new AudioClip[] { clip };
        sfx.settings = new SFX_Settings
            
        {
            volume = 1f,
            pitch = 1f,
            pitch_Variation = 0.2f,
            cooldown = 0.1f,
            spatialBlend = 1f,
            volume_Variation = 0.4f,
            dopplerLevel = 0f,
            range = 450f
        };
        
        sfx.Play(pos);

        Plugin.Log.LogInfo($"[SoundPlayer] Playing clip via RPC: {clipName} at {pos}");
    }
}
