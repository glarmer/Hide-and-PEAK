using ExitGames.Client.Photon;
using Hide_and_PEAK.Patches;
using Hide_and_PEAK.UI;
using HideAndSeekMod;
using Peak.Afflictions;
using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hide_and_PEAK;

public class HideAndSeekPlayer : MonoBehaviour
{
    
    private Vector3 _lastPosition = Vector3.zero;
    public bool IsHider => PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("Team", out var teamObj) && (Team)teamObj == Team.Hider;
    public bool IsSeeker => PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("Team", out var teamObj) && (Team)teamObj == Team.Seeker;

    private Camera MainCamera = Camera.main;
    
    void Start()
    {
        GlobalEvents.OnCharacterDied += OnCharacterDied;
    }

    void OnDestroy()
    {
        GlobalEvents.OnCharacterDied -= OnCharacterDied;
    }
    
    private void OnCharacterDied(Character character)
    {
        if (Character.localCharacter == null || Character.localCharacter.refs == null || !HideAndSeekManager.Instance.IsGameActive)
            return;
        
        Plugin.Log.LogInfo($"Death event for {character.name}");
        
        if (character.characterName.Equals(Character.localCharacter.characterName) && (Character.localCharacter.data.dead || Character.localCharacter.refs.customization.isDead))
        {
            Plugin.Log.LogInfo($"[HideAndSeekPlayer] Local Character is dead :(");
            DeathLog.Instance.AddWorldDeath(Character.localCharacter.view);
            
            if (IsHider)
            {
                Plugin.Log.LogInfo($"[HideAndSeekPlayer] Local Character was a hider, reviving as seeker...");
                Hashtable props = new Hashtable
                {
                    { "Team", Team.Seeker },
                    { "OriginalRole", Team.Hider },
                    { "Caught_Time", PlayerStats.Instance._currentTimeString },
                    { "Catches", 0 }
                };
                PhotonNetwork.LocalPlayer.SetCustomProperties(props);
                
                Character.localCharacter.photonView.RPC("RPCA_ReviveAtPosition", RpcTarget.All, Plugin.Instance.lastDeathPosition, false, 0);
                Character.localCharacter.AddStamina(100);
                Character.localCharacter.AddIllegalStatus("BLIND", 10);
                Plugin.Log.LogInfo($"[HideAndSeekPlayer] Switched local player to seeker, revived them and applied blind!");
            }
        }
        
        if (HideAndSeekManager.Instance.IsHost())
        {
            StartCoroutine(HideAndSeekManager.Instance.CheckGameEndAfterDelay(0.5f));
        }
        
    }

    private void Update()
    {
        if (Character.localCharacter == null || Character.localCharacter.refs == null || !HideAndSeekManager.Instance.IsGameActive)
            return;

        Character.localCharacter.refs.afflictions.hungerPerSecond = 0;
        if (IsSeeker)
        {
            Character.localCharacter.infiniteStam = true;
        }
        
        if (IsSeeker && Character.localCharacter.input != null && Character.localCharacter.input.useSecondaryIsPressed)
        {
            TryRaycastConeCatch(1.4f, 15);
        } else if (IsSeeker)
        {
            TryRaycastConeCatch(0.9f, 35);
        } 

        
    }

    private void TryRaycastConeCatch(float distance, float spreadAngle)
    {
        if (MainCamera == null)
        {
            MainCamera = Camera.main;
            if (MainCamera == null)
            {
                Plugin.Log.LogError("[HideAndSeekPlayer] No main camera found for raycast.");
                return;
            }
        }

        if (!HideAndSeekManager.Instance || !HideAndSeekManager.Instance.IsGameActive)
            return;

        Vector3 origin = MainCamera.transform.position;
        Vector3 forward = MainCamera.transform.forward;
        
        Vector3[] directions = new Vector3[5];
        directions[0] = forward; // center
        directions[1] = Quaternion.Euler(spreadAngle, 0f, 0f) * forward;   // up
        directions[2] = Quaternion.Euler(-spreadAngle, 0f, 0f) * forward;  // down
        directions[3] = Quaternion.Euler(0f, spreadAngle, 0f) * forward;   // right
        directions[4] = Quaternion.Euler(0f, -spreadAngle, 0f) * forward;  // left

        foreach (var dir in directions)
        {
            if (Physics.Raycast(origin, dir, out var hit, distance))
            {
                Character targetCharacter = hit.collider.GetComponentInParent<Character>();
                if (targetCharacter == null) continue;
                if (Character.localCharacter != null && targetCharacter == Character.localCharacter) continue;

                Plugin.Log.LogInfo($"[HideAndSeekPlayer] Cone ray hit: {targetCharacter.characterName}");

                int seekerViewId = Character.localCharacter.refs.view.ViewID;
                int hiderViewId = targetCharacter.refs.view.ViewID;

                HideAndSeekManager.Instance.View.RPC(
                    "RPC_RequestCatch",
                    RpcTarget.MasterClient,
                    seekerViewId,
                    hiderViewId
                );
                break;
            }
        }
    }


}