using System;
using StardewModdingAPI;
using StardewValley;
using Harmony;
using StardewModdingAPI.Events;
using System.Collections.Generic;
using System.Linq;

namespace GrowthFix
{
    public class ModEntry : Mod
    {

        private ModConfig Config;
        public override void Entry(IModHelper helper)
        {
            this.Config = this.Helper.ReadConfig<ModConfig>();

            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;

            HarmonyPatch.Monitor = Monitor;
            HarmonyPatch.locations = Config.FixLocations;
            var harmony = HarmonyInstance.Create(this.ModManifest.UniqueID);
            HarmonyPatch.ApplyPatch(harmony);
        }


        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            HarmonyPatch.ApplyFixNextTick = false;            
        }

        private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
        {
            HarmonyPatch.ApplyFixNextTick = true;
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            HarmonyPatch.ApplyFixNextTick = true;
        }

        private static class HarmonyPatch
        {
            public static bool ApplyFixNextTick = true;
            public static IMonitor Monitor;
            public static List<string> locations;

            public static void ApplyPatch(HarmonyInstance harmony)
            {
                Monitor.Log("Prefixing SDVs method HoeDirt.dayUpdate");
                harmony.Patch(
                    original: AccessTools.Method(typeof(StardewValley.TerrainFeatures.HoeDirt), nameof(StardewValley.TerrainFeatures.HoeDirt.dayUpdate), new[] { typeof(GameLocation), typeof(Microsoft.Xna.Framework.Vector2) }),
                    prefix: new HarmonyMethod(typeof(HarmonyPatch), nameof(HoeDirt_dayUpdate_condSubstitute))
                );
            }

            private static bool HoeDirt_dayUpdate_condSubstitute(StardewValley.TerrainFeatures.HoeDirt __instance,  GameLocation environment, Microsoft.Xna.Framework.Vector2 tileLocation)
            {
                try
                {
                    if (ApplyFixNextTick && locations.Contains(environment.name, StringComparer.OrdinalIgnoreCase))
                    {
                        HarmonyPatch.Monitor.Log("Prevented " + tileLocation.X + " " + tileLocation.Y + " in " + environment.name);
                        return false;
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    Monitor.Log($"Failed in {nameof(HoeDirt_dayUpdate_condSubstitute)}:\n{ex}", LogLevel.Error);
                    return true;
                }
            }
        }
    }
}