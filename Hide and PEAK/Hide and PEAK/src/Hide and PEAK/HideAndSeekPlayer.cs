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
            TryRaycastCatch(2f);
        } else if (IsSeeker && Character.localCharacter.input != null && !Character.localCharacter.input.useSecondaryIsPressed)
        {
            TryRaycastCatch(0.8f);
        } 

        if (Character.localCharacter.data.dead || Character.localCharacter.refs.customization.isDead)
        {
            Plugin.Log.LogInfo($"[HideAndSeekPlayer] Hider is dead, reviving them and switching to seeker");
            Team team = IsHider ? Team.Seeker : Team.Hider;
            Hashtable props = new Hashtable
            {
                { "Team", Team.Seeker },
                { "OriginalRole", team },
                { "Caught_Time", PlayerStats.Instance._currentTimeString },
                { "Catches", 0 }
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
            DeathLog.Instance.AddWorldDeath(Character.localCharacter.view);
            Plugin.Log.LogInfo($"[HideAndSeekPlayer] Switched local player to seeker");
        }
    }

    private void TryRaycastCatch(float distance)
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

        
        Ray ray = new Ray(MainCamera.transform.position, MainCamera.transform.forward);
        if (Physics.Raycast(ray, out var hit, distance))
        {
            Character targetCharacter = hit.collider.GetComponentInParent<Character>();
            if (targetCharacter == null) return;
            if (Character.localCharacter != null && targetCharacter == Character.localCharacter) return;

            Plugin.Log.LogInfo($"[HideAndSeekPlayer] UseSecondary hit: {targetCharacter.characterName}");

            if (HideAndSeekManager.Instance == null)
            {
                Plugin.Log.LogError("[HideAndSeekPlayer] HideAndSeekManager is null! Cannot catch hider.");
                return;
            }
            
            int seekerViewId = Character.localCharacter.refs.view.ViewID;
            int hiderViewId = targetCharacter.refs.view.ViewID;

            HideAndSeekManager.Instance.View.RPC(
                "RPC_RequestCatch",
                RpcTarget.MasterClient,
                seekerViewId,
                hiderViewId
            );
        }
    }
}