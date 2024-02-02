using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using PiShock.Patches;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace PiShock
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class PiShock : BaseUnityPlugin
    {
        private const string modGUID = "TRIPPYTRASH.PiShockMod";
        private const string modName = "PiShock Mod";
        private const string modVersion = "1.0.0";

        private readonly Harmony harmony = new Harmony("PiShock");
        internal readonly string pishockLogId = "LethalShock (Lethal company)";

        internal static PiShock Instance = null;

        //private static PiShock Instance;

        internal ManualLogSource mls;

        // Define Config Entries
        internal ConfigEntry<string> PiShockUsername;
        internal ConfigEntry<string> PiShockAPIKey;
        internal ConfigEntry<string> PiShockShockerCode;

        internal ConfigEntry<bool> shockOnDeath;
        internal ConfigEntry<bool> shockOnDamage;
        internal ConfigEntry<bool> shockOnFired;
        internal ConfigEntry<bool> shockBasedOnHealth;
        internal ConfigEntry<bool> vibrateOnly;

        internal ConfigEntry<int> duration;
        internal ConfigEntry<int> maxIntensity;
        internal ConfigEntry<int> minIntensity;
        internal ConfigEntry<int> durationDeath;
        internal ConfigEntry<int> intensityDeath;
        internal ConfigEntry<int> durationFired;
        internal ConfigEntry<int> intensityFired;

        internal ConfigEntry<bool> enableInterval;
        internal ConfigEntry<int> interval;

        internal DateTime lastShock;

        private void Awake()
        {
            
            mls = BepInEx.Logging.Logger.CreateLogSource("TRIPPY PiShockMod");
            mls.LogInfo("PiShock Mod initiated");
            
            // Bind config entries and info
            PiShockUsername = Config.Bind("PiShock", "PiShockUsername", "");
            PiShockAPIKey = Config.Bind("PiShock", "PiShockAPIKey", "");
            PiShockShockerCode = Config.Bind("PiShock", "PiShockShockerCode", "");

            shockOnDeath = Config.Bind("PiShock", "shockOnDeath", true, "Get shocked when you die");
            shockOnDamage = Config.Bind("PiShock", "shockOnDamage", true, "Get shocked when you take damage");
            shockOnFired = Config.Bind("PiShock", "shockOnFired", true, "Get shocked when you do not reach the quota");
            shockBasedOnHealth = Config.Bind("PiShock", "shockBasedOnHealth", false, "Enable to calculate shock intensity based on remaining health instead of the damage taken (shockOnDeath must be enabled)");

            vibrateOnly = Config.Bind("PiShock", "vibrateOnly", false, "Use vibration instead of shock");
            duration = Config.Bind("PiShock", "duration", 1, "Duration of shock/vibration");
            maxIntensity = Config.Bind("PiShock", "maximum", 1, new ConfigDescription("Maximum intensity of the shock/vibration", new AcceptableValueRange<int>(1, 100)));
            minIntensity = Config.Bind("PiShock", "minimum", 1, new ConfigDescription("Minimum intensity of the shock/vibration", new AcceptableValueRange<int>(1, 100)));
            durationDeath = Config.Bind("PiShock", "durationDeath", 1, "Duration of shock/vibration when you die");
            intensityDeath = Config.Bind("PiShock", "intensityDeath", 10, "Duration of shock/vibration when you die");
            durationFired = Config.Bind("PiShock", "durationFired", 3, "Duration of shock/vibration when you do not reach the quota");
            intensityFired = Config.Bind("PiShock", "intensityFired", 10, "Intensity of shock/vibration when you do not reach the quota");

            enableInterval = Config.Bind("PiShock", "enableInterval", true, "Should there be a delay between shocks? (makes constant damage like bees bearable");
            interval = Config.Bind("PiShock", "damaageInterval", 10, "Interval between damage shocks (enable interval must = true)");

            lastShock = DateTime.Now;

            harmony.PatchAll(typeof(PiShock));
            harmony.PatchAll(typeof(PlayerControllerBPatch));
            harmony.PatchAll(typeof(StartOfRoundPatch));

            if (Instance == null)
            {
                Instance = this;
            }
        }

        internal void DoDamage(int dmg, int health)
        {
            TimeSpan calculatedTime = DateTime.Now - lastShock;
            if (enableInterval.Value && calculatedTime < TimeSpan.FromSeconds(interval.Value))
            {
                Logger.LogDebug("Didn't shick due to interval. LastShock; " + lastShock.ToLongTimeString());
                return;
            }
            int maxDmgShock = Mathf.Clamp(dmg, minIntensity.Value, maxIntensity.Value);
            int shockHealth = 100 - health;
            int maxHealthShock = Mathf.Clamp(shockHealth, minIntensity.Value, maxIntensity.Value);
            if (shockBasedOnHealth.Value)
            {
                mls.LogInfo("Shocking based on health for " + maxHealthShock);
                DoOperation(maxHealthShock, duration.Value);
            }
            else if (shockOnDamage.Value)
            {
                mls.LogInfo("Shocking based on damage for " + maxDmgShock);
                DoOperation(maxDmgShock, duration.Value);
            }
            lastShock = DateTime.Now;
        }

        private bool DidDeath = false;
        internal void DoDeath()
        {
            if (DidDeath || !shockOnDeath.Value) return;
            Logger.LogInfo("Death shock");
            DoOperation(intensityDeath.Value, durationDeath.Value);
            DidDeath = true;
            Task.Run(async () =>
            {
                await Task.Delay(20000);
                DidDeath = false;
            });
        }

        private bool DidFired = false;
        internal void DoFired()
        {
            if (DidFired) return;
            mls.LogInfo("Fired Shock");
            DoOperation(intensityFired.Value, durationFired.Value);
            DidFired = true;
            Task.Run(async () =>
            {
                await Task.Delay(durationFired.Value * 1000);
                DidFired = false;
            });
        }
        private async void DoOperation(int intensity, int duration)
        {
            mls.LogDebug("Running DoOperation for shocker ");
            PiShockAPI user = new()
            {
                username = PiShockUsername.Value,
                apiKey = PiShockAPIKey.Value,
                code = PiShockShockerCode.Value,
                senderName = pishockLogId
            };

            if (vibrateOnly.Value)
            {
                await user.Vibrate(intensity, duration);
                mls.LogDebug("Vibrating");
            }
            else
            {
                await user.Shock(intensity, duration);
            }
        }
    }
}

namespace PiShock.Patches
{
    internal class PlayerControllerBPatch
    {
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.KillPlayer))]
        [HarmonyPostfix]
        private static void DeathPatch(PlayerControllerB __Instance)
        {
            if (__Instance.IsOwner)
            {
                PiShock.Instance.DoDeath();
            }
        }
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.DamagePlayer))]
        [HarmonyPostfix]
        private static void DamagePatch(int __heath, int damageNumber)
        {
            PiShock.Instance.DoDamage(damageNumber, __heath);
        }
    }
    internal class StartOfRoundPatch
    {
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.FirePlayersAfterDeadlineClientRpc))]
        [HarmonyPostfix]
        private static void FirePlayersAfterDeadlinePatch()
        {
            PiShock.Instance.DoFired();
        }
    }
}
