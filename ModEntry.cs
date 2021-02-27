using System;
using StardewModdingAPI;
using StardewValley;
using Harmony;
using StardewModdingAPI.Events;

namespace GrowthFix
{
    public class ModEntry : Mod
    {
        public override void Entry(IModHelper helper)
        {
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;

            HarmonyPatch.Monitor = Monitor;
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

            public static void ApplyPatch(HarmonyInstance harmony)
            {
                Monitor.Log("Prefixing SDVs method HoeDirt.dayUpdate");
                harmony.Patch(
                    original: AccessTools.Method(typeof(StardewValley.TerrainFeatures.HoeDirt), nameof(StardewValley.TerrainFeatures.HoeDirt.dayUpdate), new[] { typeof(GameLocation), typeof(Microsoft.Xna.Framework.Vector2) }),
                    prefix: new HarmonyMethod(typeof(HarmonyPatch), nameof(hoeDirt_dayUpdate_condSubstitute))
                );
            }

            private static bool hoeDirt_dayUpdate_condSubstitute(StardewValley.TerrainFeatures.HoeDirt __instance,  GameLocation environment, Microsoft.Xna.Framework.Vector2 tileLocation)
            {
                try
                {
                    if (ApplyFixNextTick)
                    {
                        HarmonyPatch.Monitor.Log("Prevented " + tileLocation.X + " " + tileLocation.Y + " in " + environment.name);
                    }
                    return !ApplyFixNextTick;
                }
                catch (Exception ex)
                {
                    Monitor.Log($"Failed in {nameof(hoeDirt_dayUpdate_condSubstitute)}:\n{ex}", LogLevel.Error);
                    return true;
                }
            }
        }
    }
}