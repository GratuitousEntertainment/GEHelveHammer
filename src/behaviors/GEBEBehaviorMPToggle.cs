﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent.Mechanics;

namespace gehelvehammer.src.behaviors
{
    public class GEBEBehaviorMPToggle : BEBehaviorMPBase
    {
        BlockFacing[] orients = new BlockFacing[2];

        BlockFacing[] sides = new BlockFacing[2];

        ICoreClientAPI capi;
        string orientations;

        public GEBEBehaviorMPToggle(BlockEntity blockentity) : base(blockentity)
        {
        }

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);

            if (api.Side == EnumAppSide.Client)
            {
                capi = api as ICoreClientAPI;
            }

            orientations = Block.Variant["orientation"];
            switch (orientations)
            {
                case "ns":
                    AxisSign = new int[] { 0, 0, -1 };
                    orients[0] = BlockFacing.NORTH;
                    orients[1] = BlockFacing.SOUTH;

                    sides[0] = BlockFacing.WEST;
                    sides[1] = BlockFacing.EAST;
                    break;

                case "we":
                    AxisSign = new int[] { -1, 0, 0 };
                    orients[0] = BlockFacing.WEST;
                    orients[1] = BlockFacing.EAST;

                    sides[0] = BlockFacing.NORTH;
                    sides[1] = BlockFacing.SOUTH;
                    break;
            }
        }

        public override float GetResistance()
        {
            BEHelveHammer behh = Api.World.BlockAccessor.GetBlockEntity(Position.AddCopy(sides[0])) as BEHelveHammer;
            if (behh != null && behh.HammerStack != null)
            {
                return 0.2f;
            }

            behh = Api.World.BlockAccessor.GetBlockEntity(Position.AddCopy(sides[1])) as BEHelveHammer;
            if (behh != null && behh.HammerStack != null)
            {
                return 0.2f;
            }

            return 0.0005f;
        }

        public bool IsAttachedToBlock()
        {
            if (orientations == "ns" || orientations == "we")
            {
                return
                    Api.World.BlockAccessor.GetBlock(Position.X, Position.Y - 1, Position.Z).SideSolid[BlockFacing.UP.Index] ||
                    Api.World.BlockAccessor.GetBlock(Position.X, Position.Y + 1, Position.Z).SideSolid[BlockFacing.DOWN.Index]
                ;
            }

            return false;
        }


        MeshData getStandMesh(string orient)
        {
            return ObjectCacheUtil.GetOrCreate(Api, "toggle-" + orient + "-stand", () =>
            {
                Shape shape = capi.Assets.TryGet("shapes/block/wood/mechanics/toggle-stand.json").ToObject<Shape>();
                MeshData mesh;
                capi.Tesselator.TesselateShape(Block, shape, out mesh);

                if (orient == "ns")
                {
                    mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, GameMath.PIHALF, 0);
                }

                return mesh;
            });

        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {
            MeshData mesh = getStandMesh(Block.Variant["orientation"]);
            mesher.AddMeshData(mesh);

            return base.OnTesselation(mesher, tesselator);
        }


    }
}
