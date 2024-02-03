using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using PiShock.Patches;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace PiShock
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class PiShock : BaseUnityPlugin
    {
        private const string modGUID = "PiShock";
        private const string modName = "PiShock";
        private const string modVersion = "1.0.2";

        private readonly Harmony harmony = new Harmony("PiShock");
        internal readonly string pishockLogId = "PiShock (Lethal company)";

        internal static PiShock Instance = null;

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
            mls = BepInEx.Logging.Logger.CreateLogSource("PiShock");
            mls.LogMessage("PiShock " + modVersion + " - by TRIPPYTRASH");
            
            // Bind config entries and info
            PiShockUsername = Config.Bind("PiShock API Authentication", "PiShockUsername", "ZAP_xdx");
            PiShockAPIKey = Config.Bind("PiShock API Authentication", "PiShockAPIKey", "ABCDEF-123456-GHIJKL-7890XZ");
            PiShockShockerCode = Config.Bind("PiShock API Authentication", "PiShockShockerCode", "A1B2C3D4E5F6");

            shockOnDamage = Config.Bind("Shocking Events", "shockOnDamage", true, "Get shocked when you take damage");
            shockOnDeath = Config.Bind("Shocking Events", "shockOnDeath", true, "Get shocked when you die");
            shockOnFired = Config.Bind("Shocking Events", "shockOnFired", true, "Get shocked when you do not reach the quota");
            shockBasedOnHealth = Config.Bind("Shocking Events", "shockBasedOnHealth", false, "Enable to calculate shock intensity based on remaining health instead of the damage taken (shockOnDeath must be enabled)");

            maxIntensity = Config.Bind("Intensity Sliders", "maximum", 1, new ConfigDescription("Maximum intensity of shock/vibration", new AcceptableValueRange<int>(1, 100)));
            minIntensity = Config.Bind("Intensity Sliders", "minimum", 1, new ConfigDescription("Minimum intensity of shock/vibration", new AcceptableValueRange<int>(1, 100)));
            intensityDeath = Config.Bind("Intensity Sliders", "intensityDeath", 10, new ConfigDescription("Intensity of shock/vibration when you die", new AcceptableValueRange<int>(1, 100)));
            intensityFired = Config.Bind("Intensity Sliders", "intensityFired", 10, new ConfigDescription("Intensity of shock/vibration when you do not reach the quota", new AcceptableValueRange<int>(1, 100)));

            duration = Config.Bind("Durations Sliders", "duration", 1, new ConfigDescription("General duration of shock/vibration", new AcceptableValueRange<int>(1, 10)));
            durationFired = Config.Bind("Durations Sliders", "durationFired", 3, new ConfigDescription("Duration of shock/vibration when you do not reach the quota", new AcceptableValueRange<int>(1, 10)));
            durationDeath = Config.Bind("Durations Sliders", "durationDeath", 1, new ConfigDescription("Duration of shock/vibration when you die", new AcceptableValueRange<int>(1, 10)));

            vibrateOnly = Config.Bind("Misc", "vibrateOnly", false, "Use vibration instead of shock");
            enableInterval = Config.Bind("Misc", "enableInterval", true, "Should there be a delay between shocks? (makes constant damage like bees bearable)");
            interval = Config.Bind("Misc", "damageInterval", 10, "Interval between damage shocks (enable interval must = true)");

            lastShock = DateTime.Now;

            mls.LogMessage("Running for user: " + PiShockUsername.Value);

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
            mls.LogDebug("Running DoOperation for shocker code");
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
        private static void DeathPatch(ref PlayerControllerB __instance)
        {
            if (__instance.IsOwner)
            {
                PiShock.Instance.DoDeath();
            }
        }

        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.DamagePlayer))]
        [HarmonyPostfix]
        private static void DamagePatch(int ___health, int damageNumber)
        {
            PiShock.Instance.DoDamage(damageNumber, ___health);
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
