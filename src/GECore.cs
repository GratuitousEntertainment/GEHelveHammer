using gehelvehammer.src.behaviors;
using gehelvehammer.src.blocktypes;
using gehelvehammer.src.entities;
using gehelvehammer.src.itemtypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace gehelvehammer.src
{
    class GECore : ModSystem
    {

        public override void Start(ICoreAPI api)
        {
            api.World.Logger.Debug("Installing GE Helvehammer");
            try
            {
                api.RegisterItemClass("GEItemHammer", typeof(GEItemHammer));

                api.RegisterBlockClass("GEBlockAnvil", typeof(GEBlockAnvil));
                api.RegisterBlockClass("GEBlockToggle", typeof(GEBlockToggle));
                api.RegisterBlockClass("GEBlockHelveHammer", typeof(GEBlockHelveHammer));


                api.RegisterBlockEntityClass("GEAnvil", typeof(GEBEAnvil));
                api.RegisterBlockEntityClass("GEBEHelveHammer", typeof(GEBEHelveHammer));

                api.RegisterBlockEntityBehaviorClass("GEBEBehaviorMPToggle", typeof(GEBEBehaviorMPToggle));

            }
            catch (Exception e)
            {
                api.World.Logger.Log(EnumLogType.Debug, $"error registering GE Helvehammer: {e.StackTrace}");
                throw;
            }

            

        }
    }

    



}
