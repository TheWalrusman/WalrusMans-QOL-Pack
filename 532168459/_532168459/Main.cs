using DG.Tweening;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Root.Strings;
using Kingmaker.Cargo;
using Kingmaker.Cheats;
using Kingmaker.Code.UI.MVVM.View.LoadingScreen;
using Kingmaker.Code.UI.MVVM.View.MainMenu.PC;
using Kingmaker.Code.UI.MVVM.VM.MessageBox;
using Kingmaker.Code.UI.MVVM.VM.ServiceWindows.Inventory;
using Kingmaker.Code.UI.MVVM.VM.ShipCustomization;
using Kingmaker.Code.UI.MVVM.VM.Slots;
using Kingmaker.Code.UI.MVVM.VM.WarningNotification;
using Kingmaker.Controllers.Combat;
using Kingmaker.Controllers.MapObjects;
using Kingmaker.Designers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.GameCommands;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.Networking;
using Kingmaker.Pathfinding;
using Kingmaker.PubSubSystem;
using Kingmaker.UI;
using Kingmaker.UI.Common;
using Kingmaker.UI.Sound;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.Utility;
using Kingmaker.View.Covers;
using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using Warhammer.SpaceCombat.Blueprints;
using static Kingmaker.UnitLogic.Abilities.AbilityData;
using UnityModManagerNet;
using System.Reflection;
using Kingmaker.PubSubSystem.Core;
using Kingmaker.Achievements;
using Kingmaker.Blueprints.Root;
using Kingmaker.Blueprints;


namespace _532168459
{
    static class Main
    {
        public static bool Enabled;

        static bool Load(UnityModManager.ModEntry modEntry)
        {
            modEntry.OnToggle = OnToggle;
            var harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            return true;
        }
        static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            Enabled = value;
            return true;
        }
    }

    [HarmonyPatch(typeof(AchievementEntity), nameof(AchievementEntity.IsDisabled), MethodType.Getter)]
    public static class AchievementEntity_IsDisabled_Patch
    {
        private static void Postfix(ref bool __result, AchievementEntity __instance)
        {
            if (!Main.Enabled) return;
            if (!__instance.Data.OnlyMainCampaign && Game.Instance.Player.Campaign && !Game.Instance.Player.Campaign.IsMainGameContent)
            {
                __result = true;
                return;
            }
            BlueprintCampaignReference specificCampaign = __instance.Data.SpecificCampaign;
            BlueprintCampaign blueprintCampaign = ((specificCampaign != null) ? specificCampaign.Get() : null);
            __result = (!__instance.Data.OnlyMainCampaign && blueprintCampaign != null && Game.Instance.Player.Campaign != blueprintCampaign);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.ModsUser), MethodType.Getter)]
    public static class Player_ModsUser_Patch
    {
        public static void Postfix(ref bool __result)
        {
            if (!Main.Enabled) return;
            __result = false;
            return;
        }
    }

    [HarmonyPatch(typeof(Player), (nameof(Player.GetRespecCost)))]
    public static class Player_GetRespecCost_Patch
    {
        public static void Postfix(ref int __result)
        {
            if (!Main.Enabled) return;
            __result = 0;
            return;
        }
    }

    [HarmonyPatch(typeof(Player), (nameof(Player.GetCustomCompanionCost)))]
    static class Player_GetCustomCompanionCost_Patch
    {
        //Postfix must be spelt correctly to be applied
        static void Postfix(ref int __result)
        {
            if (!Main.Enabled) return;
            // Harmony parameters are determined by name, __result 
            // is the current cost of the mercenary. Because it is a 
            // ref parameter, we can modify it's value
            __result = 0;
            return;
        }
    }



    [HarmonyPatch(typeof(LoadingScreenBaseView))]
    public static class LoadingScreenBaseViewPatch
    {
        [HarmonyPatch(nameof(LoadingScreenBaseView.ShowUserInputWaiting))]
        [HarmonyPrefix]
        private static bool ShowUserInputLayer(LoadingScreenBaseView __instance, bool state)
        {
            if (!Main.Enabled) return true;
            if (!state)
                return false;
            __instance.m_ProgressBarContainer.DOFade(0.0f, 1f).OnComplete(() => __instance.StartPressAnyKeyLoopAnimation()).SetUpdate(true);
            __instance.AddDisposable(MainThreadDispatcher.UpdateAsObservable()
                                                         .Subscribe(_ => {
                                                             UISounds.Instance.Sounds.Buttons.ButtonClick.Play();
                                                             if (PhotonManager.Lobby.IsLoading)
                                                                 PhotonManager.Instance.ContinueLoading();
                                                             EventBus.RaiseEvent((Action<IContinueLoadingHandler>)(h => h.HandleContinueLoading()));
                                                         }));
            return false;
        }
    }
}
