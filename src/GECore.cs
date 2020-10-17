using HarmonyLib;
using System;
using Vintagestory.API.Common;

namespace gehelvehammer.src
{
    class GECore : ModSystem
    {

        public override void Start(ICoreAPI api)
        {
            api.World.Logger.Debug("Installing GE Helvehammer");
            try
            {
                var harmony = new Harmony("gehelvehammer");

                harmony.PatchAll();
            }
            catch (Exception e)
            {
                api.World.Logger.Log(EnumLogType.Debug, $"error registering GE Helvehammer: {e.StackTrace}");
                throw;
            }
        }
    }
}
