using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace gehelvehammer.src
{
    [HarmonyPatch(typeof(BlockEntityAnvil))]
    public class GEBlockEntityAnvil
    {
        static GEBlockEntityAnvil()
        {
            smallMetalSparks = new SimpleParticleProperties(
                2, 5,
                ColorUtil.ToRgba(255, 255, 233, 83),
                new Vec3d(), new Vec3d(),
                new Vec3f(-3f, 8f, -3f),
                new Vec3f(3f, 12f, 3f),
                0.1f,
                1f,
                0.25f, 0.25f,
                EnumParticleModel.Quad
            );
            smallMetalSparks.VertexFlags = 128;
            smallMetalSparks.AddPos.Set(1 / 16f, 0, 1 / 16f);
            smallMetalSparks.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.05f);
            smallMetalSparks.ParticleModel = EnumParticleModel.Quad;
            smallMetalSparks.LifeLength = 0.03f;
            smallMetalSparks.MinVelocity = new Vec3f(-1f, 1f, -1f);
            smallMetalSparks.AddVelocity = new Vec3f(2f, 2f, 2f);
            smallMetalSparks.MinQuantity = 4;
            smallMetalSparks.AddQuantity = 6;
            smallMetalSparks.MinSize = 0.1f;
            smallMetalSparks.MaxSize = 0.1f;
            smallMetalSparks.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.1f);

            bigMetalSparks = new SimpleParticleProperties(
                2, 8,
                ColorUtil.ToRgba(255, 255, 233, 83),
                new Vec3d(), new Vec3d(),
                new Vec3f(-1f, 0.5f, -1f),
                new Vec3f(2f, 1.5f, 2f),
                0.5f,
                1f,
                0.25f, 0.25f
            );
            bigMetalSparks.VertexFlags = 128;
            bigMetalSparks.AddPos.Set(1 / 16f, 0, 1 / 16f);
            bigMetalSparks.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.25f);

            slagPieces = new SimpleParticleProperties(
                2, 12,
                ColorUtil.ToRgba(255, 255, 233, 83),
                new Vec3d(), new Vec3d(),
                new Vec3f(-1f, 0.5f, -1f),
                new Vec3f(2f, 1.5f, 2f),
                0.5f,
                1f,
                0.25f, 0.5f
            );
            slagPieces.AddPos.Set(1 / 16f, 0, 1 / 16f);
            slagPieces.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.25f);

        }

        #region helevehammerhit
        [HarmonyPatch("OnHelveHammerHit")]
        [HarmonyPrefix]
        public static bool OnHelveHammerHitPatch(ref BlockEntityAnvil __instance)
        {
            var oworkItemStack = __instance.GetType().GetField("workItemStack", BindingFlags.NonPublic | BindingFlags.Instance);
            var workItemStack = oworkItemStack.GetValue(__instance) as ItemStack;

            if (workItemStack == null || !__instance.CanWorkCurrent || __instance.SelectedRecipe == null) return false;
            //the selectedrecipie test might break something else.  needs testing.

            SmithingRecipe recipe = __instance.SelectedRecipe;

            // Helve hammer can only work plates and iron bloom
            if (!recipe.Output.Code.Path.Contains("plate") && !recipe.Output.Code.Path.Contains("arrow") && !__instance.IsIronBloom) return false;

            __instance.rotation = 0;
            int ymax = recipe.QuantityLayers;
            Vec3i usableMetalVoxel;
            if (!__instance.IsIronBloom)
            {
                usableMetalVoxel = findFreeMetalVoxel(ref __instance);

                if (usableMetalVoxel != null)
                {

                    for (int x = 0; x < 16; x++)
                    {
                        for (int z = 0; z < 16; z++)
                        {
                            for (int y = 0; y < 5; y++)
                            {
                                bool requireMetalHere = y >= ymax ? false : recipe.Voxels[x, y, z];

                                EnumVoxelMaterial mat = (EnumVoxelMaterial)__instance.Voxels[x, y, z];

                                if (requireMetalHere && mat == EnumVoxelMaterial.Empty)
                                {
                                    __instance.Voxels[x, y, z] = (byte)EnumVoxelMaterial.Metal;
                                    __instance.Voxels[usableMetalVoxel.X, usableMetalVoxel.Y, usableMetalVoxel.Z] = (byte)EnumVoxelMaterial.Empty;

                                    if (__instance.Api.World.Side == EnumAppSide.Client)
                                    {
                                        spawnParticles(new Vec3i(x, y, z), mat == EnumVoxelMaterial.Empty ? EnumVoxelMaterial.Metal : mat, null, ref __instance);
                                        spawnParticles(usableMetalVoxel, EnumVoxelMaterial.Metal, null, ref __instance);
                                    }
                                    RegenMeshAndSelectionBoxes(ref __instance);
                                    __instance.CheckIfFinished(null);
                                    return false;
                                }

                            }
                        }
                    }

                    __instance.Voxels[usableMetalVoxel.X, usableMetalVoxel.Y, usableMetalVoxel.Z] = (byte)EnumVoxelMaterial.Empty;
                    if (__instance.Api.World.Side == EnumAppSide.Client)
                    {
                        spawnParticles(usableMetalVoxel, EnumVoxelMaterial.Metal, null, ref __instance);
                    }
                    RegenMeshAndSelectionBoxes(ref __instance);
                    __instance.CheckIfFinished(null);
                }
            }
            else
            {

                for (int y = 5; y >= 0; y--)
                {
                    for (int z = 0; z < 16; z++)
                    {
                        for (int x = 0; x < 16; x++)
                        {
                            bool requireMetalHere = y >= ymax ? false : recipe.Voxels[x, y, z];

                            EnumVoxelMaterial mat = (EnumVoxelMaterial)__instance.Voxels[x, y, z];

                            if (requireMetalHere && mat == EnumVoxelMaterial.Metal) continue;
                            if (!requireMetalHere && mat == EnumVoxelMaterial.Empty) continue;

                            if (__instance.Api.World.Side == EnumAppSide.Client)
                            {
                                spawnParticles(new Vec3i(x, y, z), mat == EnumVoxelMaterial.Empty ? EnumVoxelMaterial.Metal : mat, null, ref __instance);
                            }

                            if (requireMetalHere && mat == EnumVoxelMaterial.Empty)
                            {
                                __instance.Voxels[x, y, z] = (byte)EnumVoxelMaterial.Metal;
                            }
                            else
                            {
                                __instance.Voxels[x, y, z] = (byte)EnumVoxelMaterial.Empty;
                            }

                            RegenMeshAndSelectionBoxes(ref __instance);
                            __instance.CheckIfFinished(null);

                            return false;
                        }
                    }
                }
            }

            return false;
        }

        public static SimpleParticleProperties bigMetalSparks;
        public static SimpleParticleProperties smallMetalSparks;
        public static SimpleParticleProperties slagPieces;

        public static float voxYOff = 10 / 16f;

        static int bitsPerByte = 2;
        static int partsPerByte = 8 / bitsPerByte;

        public static Vec3i findFreeMetalVoxel(ref BlockEntityAnvil __instance)
        {
            SmithingRecipe recipe = __instance.SelectedRecipe;
            int ymax = recipe.QuantityLayers;

            for (int y = 5; y >= 0; y--)
            {
                for (int z = 0; z < 16; z++)
                {
                    for (int x = 0; x < 16; x++)
                    {
                        bool requireMetalHere = y >= ymax ? false : recipe.Voxels[x, y, z];
                        EnumVoxelMaterial mat = (EnumVoxelMaterial)__instance.Voxels[x, y, z];

                        if (!requireMetalHere && mat == EnumVoxelMaterial.Metal) return new Vec3i(x, y, z);
                    }
                }
            }

            return null;
        }

        public static void RegenMeshAndSelectionBoxes(ref BlockEntityAnvil __instance)
        {
            var oworkItemStack = __instance.GetType().GetField("workItemStack", BindingFlags.NonPublic | BindingFlags.Instance);
            var workItemStack = oworkItemStack.GetValue(__instance) as ItemStack;

            var oworkitemRenderer = __instance.GetType().GetField("workitemRenderer", BindingFlags.NonPublic | BindingFlags.Instance);
            var workitemRenderer = oworkitemRenderer.GetValue(__instance) as AnvilWorkItemRenderer;

            var oselectionBoxes = __instance.GetType().GetField("selectionBoxes", BindingFlags.NonPublic | BindingFlags.Instance);
            var selectionBoxes = oselectionBoxes.GetValue(__instance) as Cuboidf[];

            if (workitemRenderer != null)
            {
                workitemRenderer.RegenMesh(workItemStack, __instance.Voxels, __instance.recipeVoxels);
            }

            oworkItemStack.SetValue(__instance, workItemStack);

            List<Cuboidf> boxes = new List<Cuboidf>();
            boxes.Add(null);

            for (int x = 0; x < 16; x++)
            {
                for (int y = 0; y < 6; y++)
                {
                    for (int z = 0; z < 16; z++)
                    {
                        if (__instance.Voxels[x, y, z] != (byte)EnumVoxelMaterial.Empty)
                        {
                            float py = y + 10;
                            boxes.Add(new Cuboidf(x / 16f, py / 16f, z / 16f, x / 16f + 1 / 16f, py / 16f + 1 / 16f, z / 16f + 1 / 16f));
                        }
                    }
                }
            }

            oselectionBoxes.SetValue(__instance, boxes.ToArray());
        }

        public static void spawnParticles(Vec3i voxelPos, EnumVoxelMaterial voxelMat, IPlayer byPlayer, ref BlockEntityAnvil __instance)
        {
            var oworkItemStack = __instance.GetType().GetField("workItemStack", BindingFlags.NonPublic | BindingFlags.Instance);
            var workItemStack = oworkItemStack.GetValue(__instance) as ItemStack;

            float temp = workItemStack.Collectible.GetTemperature(__instance.Api.World, workItemStack);

            if (voxelMat == EnumVoxelMaterial.Metal && temp > 800)
            {
                bigMetalSparks.MinPos = __instance.Pos.ToVec3d().AddCopy(voxelPos.X / 16f, voxYOff + voxelPos.Y / 16f + 0.0625f, voxelPos.Z / 16f);
                bigMetalSparks.VertexFlags = (byte)GameMath.Clamp((int)(temp - 700) / 2, 32, 128);
                __instance.Api.World.SpawnParticles(bigMetalSparks, byPlayer);

                smallMetalSparks.MinPos = __instance.Pos.ToVec3d().AddCopy(voxelPos.X / 16f, voxYOff + voxelPos.Y / 16f + 0.0625f, voxelPos.Z / 16f);
                smallMetalSparks.VertexFlags = (byte)GameMath.Clamp((int)(temp - 770) / 3, 32, 128);
                __instance.Api.World.SpawnParticles(smallMetalSparks, byPlayer);
            }

            if (voxelMat == EnumVoxelMaterial.Slag)
            {
                slagPieces.Color = workItemStack.Collectible.GetRandomColor(__instance.Api as ICoreClientAPI, workItemStack);
                slagPieces.MinPos = __instance.Pos.ToVec3d().AddCopy(voxelPos.X / 16f, voxYOff + voxelPos.Y / 16f + 0.0625f, voxelPos.Z / 16f);

                __instance.Api.World.SpawnParticles(slagPieces, byPlayer);
            }
        }
        #endregion

        [HarmonyPatch("TryPut")]
        [HarmonyPrefix]
        public static bool TryPut(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref bool __result, ref BlockEntityAnvil __instance)
        {
            var OMETALSBYCODE = __instance.GetType().GetField("metalsByCode", BindingFlags.NonPublic | BindingFlags.Instance);
            var OWORKITEMSTACK = __instance.GetType().GetField("workItemStack", BindingFlags.NonPublic | BindingFlags.Instance);
            var OBASEMATERIAL = __instance.GetType().GetField("baseMaterial", BindingFlags.NonPublic | BindingFlags.Instance);
            var OSELECTEDRECIPEID = __instance.GetType().GetField("selectedRecipeId", BindingFlags.NonPublic | BindingFlags.Instance);


            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (slot.Itemstack == null)
            {
                __result = false;
                return false;
            }

            ItemStack stack = slot.Itemstack;

            var METALSBYCODE = OMETALSBYCODE.GetValue(__instance) as Dictionary<string, MetalPropertyVariant>;

            string metalType = stack.Collectible.LastCodePart();
            bool viableTier = METALSBYCODE.ContainsKey(metalType) && METALSBYCODE[metalType].Tier <= __instance.OwnMetalTier + 1;
            bool viableIngot = stack.Collectible is ItemIngot && __instance.CanWork(stack) && viableTier;

            // Place ingot
            var WORKITEMSTACK = OWORKITEMSTACK.GetValue(__instance) as ItemStack;
            var BASEMATERIAL = OBASEMATERIAL.GetValue(__instance) as ItemStack;
            var SELECTEDRECIPEID = (int)OSELECTEDRECIPEID.GetValue(__instance);

            if (viableIngot && (WORKITEMSTACK == null || WORKITEMSTACK.Collectible.LastCodePart().Equals(stack.Collectible.LastCodePart())))
            {
                if (WORKITEMSTACK == null)
                {
                    if (world is IClientWorldAccessor)
                    {
                        OpenDialog(stack, __instance);
                    }

                    CreateVoxelsFromIngot(ref __instance);

                    OWORKITEMSTACK.SetValue(__instance, new ItemStack(__instance.Api.World.GetItem(new AssetLocation("workitem-" + stack.Collectible.LastCodePart()))));
                    WORKITEMSTACK = OWORKITEMSTACK.GetValue(__instance) as ItemStack;

                    WORKITEMSTACK.Collectible.SetTemperature(__instance.Api.World, WORKITEMSTACK, stack.Collectible.GetTemperature(__instance.Api.World, stack));
                    OWORKITEMSTACK.SetValue(__instance, WORKITEMSTACK);

                    OBASEMATERIAL.SetValue(__instance, new ItemStack(__instance.Api.World.GetItem(new AssetLocation("ingot-" + stack.Collectible.LastCodePart()))));
                    BASEMATERIAL = OBASEMATERIAL.GetValue(__instance) as ItemStack;

                    List<SmithingRecipe> recipes = __instance.Api.World.SmithingRecipes
                        .Where(r => r.Ingredient.SatisfiesAsIngredient(BASEMATERIAL))
                        .OrderBy(r => r.Output.ResolvedItemstack.Collectible.Code)
                        .ToList()
                    ;

                    //OSELECTEDRECIPEID.SetValue(__instance, -1);  GE removed may need a value (I think I had to handle a null value elsewhere - Take???)
                    //AvailableMetalVoxels += 16;
                }
                else
                {
                    AddVoxelsFromIngot(ref __instance);

                    //AvailableMetalVoxels += 32;
                }


                slot.TakeOut(1);
                slot.MarkDirty();

                RegenMeshAndSelectionBoxes(ref __instance);
                __instance.MarkDirty();
                __result = true;
                return false;
            }

            // Place workitem
            bool viableWorkItem = stack.Collectible.FirstCodePart().Equals("workitem") && viableTier;
            if (viableWorkItem && WORKITEMSTACK == null)
            {
                try
                {
                    __instance.Voxels = deserializeVoxels(stack.Attributes.GetBytes("voxels"), ref __instance);
                    //AvailableMetalVoxels = stack.Attributes.GetInt("availableVoxels");

                    //SELECTEDRECIPEID = (int)OSELECTEDRECIPEID.GetValue(__instance);
                    SELECTEDRECIPEID = stack.Attributes.GetInt("selectedRecipeId");
                    OSELECTEDRECIPEID.SetValue(__instance, SELECTEDRECIPEID);

                    WORKITEMSTACK = stack.Clone();
                    OWORKITEMSTACK.SetValue(__instance, WORKITEMSTACK);
                }
                catch (Exception)
                {

                }

                if (SELECTEDRECIPEID < 0 && world is IClientWorldAccessor)
                {
                    OpenDialog(stack, __instance);
                }

                slot.TakeOut(1);
                slot.MarkDirty();

                RegenMeshAndSelectionBoxes(ref __instance);
                __instance.CheckIfFinished(byPlayer);
                __instance.MarkDirty();
                __result = true;
                return false;
            }

            // Place iron bloom
            bool viableBloom = stack.Collectible.FirstCodePart().Equals("ironbloom") && __instance.OwnMetalTier >= 2;
            if (viableBloom && WORKITEMSTACK == null)
            {
                if (stack.Attributes.HasAttribute("voxels"))
                {
                    try
                    {
                        __instance.Voxels = deserializeVoxels(stack.Attributes.GetBytes("voxels"), ref __instance);
                        //AvailableMetalVoxels = stack.Attributes.GetInt("availableVoxels");
                        SELECTEDRECIPEID = stack.Attributes.GetInt("selectedRecipeId");
                        OSELECTEDRECIPEID.SetValue(__instance, SELECTEDRECIPEID);
                    }
                    catch (Exception)
                    {
                        CreateVoxelsFromIronBloom(ref __instance);
                    }
                }
                else
                {
                    CreateVoxelsFromIronBloom(ref __instance);
                }


                WORKITEMSTACK = stack.Clone();
                WORKITEMSTACK.StackSize = 1;
                WORKITEMSTACK.Collectible.SetTemperature(__instance.Api.World, WORKITEMSTACK, stack.Collectible.GetTemperature(__instance.Api.World, stack));
                OWORKITEMSTACK.SetValue(__instance, WORKITEMSTACK);

                List<SmithingRecipe> recipes = __instance.Api.World.SmithingRecipes
                        .Where(r => r.Ingredient.SatisfiesAsIngredient(stack))
                        .OrderBy(r => r.Output.ResolvedItemstack.Collectible.Code)
                        .ToList()
                    ;

                SELECTEDRECIPEID = recipes[0].RecipeId;
                OSELECTEDRECIPEID.SetValue(__instance, SELECTEDRECIPEID);

                BASEMATERIAL = stack.Clone();
                BASEMATERIAL.StackSize = 1;
                OBASEMATERIAL.SetValue(__instance, BASEMATERIAL);

                RegenMeshAndSelectionBoxes(ref __instance);
                __instance.CheckIfFinished(byPlayer);

                slot.TakeOut(1);
                slot.MarkDirty();
                __instance.MarkDirty();
                __result = true;
                return false;
            }

            __result = false;
            return false;
        }

        public static void OpenDialog(ItemStack ingredient, BlockEntityAnvil __instance)
        {
            var OSELECTEDRECIPEID = __instance.GetType().GetField("selectedRecipeId", BindingFlags.NonPublic | BindingFlags.Instance);

            if (ingredient.Collectible is ItemWorkItem)
            {
                ingredient = new ItemStack(__instance.Api.World.GetItem(new AssetLocation("ingot-" + ingredient.Collectible.LastCodePart())));
            }

            List<SmithingRecipe> recipes = __instance.Api.World.SmithingRecipes
                .Where(r => r.Ingredient.SatisfiesAsIngredient(ingredient))
                .OrderBy(r => r.Output.ResolvedItemstack.Collectible.Code) // Cannot sort by name, thats language dependent!
                .ToList()
            ;

            List<ItemStack> stacks = recipes
                .Select(r => r.Output.ResolvedItemstack)
                .ToList()
            ;

            IClientWorldAccessor clientWorld = (IClientWorldAccessor)__instance.Api.World;
            ICoreClientAPI capi = __instance.Api as ICoreClientAPI;

            BlockPos Pos = __instance.Pos;

            var jjjjjjj = new CallBack(ref __instance,capi,recipes,Pos);

            GuiDialog dlg = new GuiDialogBlockEntityRecipeSelector(
                Lang.Get("Select smithing recipe"),
                stacks.ToArray(),
                jjjjjjj.OnSelect,
                () => {
                    capi.Network.SendBlockEntityPacket(Pos.X, Pos.Y, Pos.Z, (int)EnumClayFormingPacket.CancelSelect);
                },
                __instance.Pos,
                __instance.Api as ICoreClientAPI
            );

            dlg.TryOpen();
        }


        public static void CreateVoxelsFromIngot(ref BlockEntityAnvil __instance)
        {
            __instance.Voxels = new byte[16, 6, 16];

            for (int x = 0; x < 7; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    for (int z = 0; z < 3; z++)
                    {
                        __instance.Voxels[4 + x, y, 6 + z] = (byte)EnumVoxelMaterial.Metal;
                    }

                }
            }
        }

        public static void AddVoxelsFromIngot(ref BlockEntityAnvil __instance)
        {
            for (int x = 0; x < 7; x++)
            {
                for (int z = 0; z < 3; z++)
                {
                    int y = 0;
                    int added = 0;
                    while (y < 6 && added < 2)
                    {
                        if (__instance.Voxels[4 + x, y, 6 + z] == (byte)EnumVoxelMaterial.Empty)
                        {
                            __instance.Voxels[4 + x, y, 6 + z] = (byte)EnumVoxelMaterial.Metal;
                            added++;
                        }

                        y++;
                    }
                }
            }
        }

        public static byte[,,] deserializeVoxels(byte[] data, ref BlockEntityAnvil __instance)
        {
            byte[,,] voxels = new byte[16, 6, 16];

            if (data == null || data.Length < 16 * 6 * 16 / partsPerByte) return voxels;

            int pos = 0;

            for (int x = 0; x < 16; x++)
            {
                for (int y = 0; y < 6; y++)
                {
                    for (int z = 0; z < 16; z++)
                    {
                        int bitpos = bitsPerByte * (pos % partsPerByte);
                        voxels[x, y, z] = (byte)((data[pos / partsPerByte] >> bitpos) & 0x3);

                        pos++;
                    }
                }
            }

            return voxels;
        }

        public static byte[] serializeVoxels(byte[,,] voxels, ref BlockEntityAnvil __instance)
        {
            byte[] data = new byte[16 * 6 * 16 / partsPerByte];
            int pos = 0;

            for (int x = 0; x < 16; x++)
            {
                for (int y = 0; y < 6; y++)
                {
                    for (int z = 0; z < 16; z++)
                    {
                        int bitpos = bitsPerByte * (pos % partsPerByte);
                        data[pos / partsPerByte] |= (byte)((voxels[x, y, z] & 0x3) << bitpos);
                        pos++;
                    }
                }
            }

            return data;
        }

        public static void CreateVoxelsFromIronBloom(ref BlockEntityAnvil __instance)
        {
            CreateVoxelsFromIngot(ref __instance);

            Random rand = __instance.Api.World.Rand;

            for (int dx = -1; dx < 8; dx++)
            {
                for (int y = 0; y < 5; y++)
                {
                    for (int dz = -1; dz < 5; dz++)
                    {
                        int x = 4 + dx;
                        int z = 6 + dz;

                        if (y == 0 && __instance.Voxels[x, y, z] == (byte)EnumVoxelMaterial.Metal) continue;

                        float dist = Math.Max(0, Math.Abs(x - 7) - 1) + Math.Max(0, Math.Abs(z - 8) - 1) + Math.Max(0, y - 1f);

                        if (rand.NextDouble() < dist / 3f - 0.4f + (y - 1.5f) / 4f)
                        {
                            continue;
                        }

                        if (rand.NextDouble() > dist / 2f)
                        {
                            __instance.Voxels[x, y, z] = (byte)EnumVoxelMaterial.Metal;
                        }
                        else
                        {
                            __instance.Voxels[x, y, z] = (byte)EnumVoxelMaterial.Slag;
                        }
                    }
                }
            }
        }

        [HarmonyPatch("TryTake")]
        [HarmonyPrefix]
        public static bool TryTake(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref bool __result, ref BlockEntityAnvil __instance)
        {
            var OWORKITEMSTACK = __instance.GetType().GetField("workItemStack", BindingFlags.NonPublic | BindingFlags.Instance);
            var WORKITEMSTACK = OWORKITEMSTACK.GetValue(__instance) as ItemStack;

            var OSELECTEDRECIPEID = __instance.GetType().GetField("selectedRecipeId", BindingFlags.NonPublic | BindingFlags.Instance);
            var SELECTEDRECIPEID = (int)OSELECTEDRECIPEID.GetValue(__instance);

            if (WORKITEMSTACK == null) {
                __result = false;
                return false; }

            WORKITEMSTACK.Attributes.SetBytes("voxels", serializeVoxels(__instance.Voxels,ref __instance));
            //workItemStack.Attributes.SetInt("availableVoxels", AvailableMetalVoxels);
            WORKITEMSTACK.Attributes.SetInt("selectedRecipeId", SELECTEDRECIPEID);

            if (WORKITEMSTACK.Collectible is ItemIronBloom bloomItem)
            {
                WORKITEMSTACK.Attributes.SetInt("hashCode", bloomItem.GetWorkItemHashCode(WORKITEMSTACK));
            }

            if (SELECTEDRECIPEID >= 0)
            {
                if (!byPlayer.InventoryManager.TryGiveItemstack(WORKITEMSTACK))
                {
                    __instance.Api.World.SpawnItemEntity(WORKITEMSTACK, __instance.Pos.ToVec3d().Add(0.5, 0.5, 0.5));
                }
            }
            else
            {
                //make ingot
                var ingot = new ItemStack(__instance.Api.World.GetItem(new AssetLocation("ingot-" + WORKITEMSTACK.Collectible.LastCodePart())));
                ingot.Collectible.SetTemperature(__instance.Api.World, ingot, WORKITEMSTACK.Collectible.GetTemperature(__instance.Api.World, WORKITEMSTACK), false);
                if (!byPlayer.InventoryManager.TryGiveItemstack(ingot))
                {
                    __instance.Api.World.SpawnItemEntity(ingot, __instance.Pos.ToVec3d().Add(0.5, 0.5, 0.5));
                }

            }

            OWORKITEMSTACK.SetValue(__instance, null);
            __instance.Voxels = new byte[16, 6, 16];
            //AvailableMetalVoxels = 0;

            RegenMeshAndSelectionBoxes(ref __instance);
            __instance.MarkDirty();
            __instance.rotation = 0;
            OSELECTEDRECIPEID.SetValue(__instance, -1);
            __result = true;
            return false;
        }

    }

    class CallBack{

        BlockEntityAnvil local;
        ICoreClientAPI capi;
        List<SmithingRecipe> recipes;
        BlockPos Pos;

        public CallBack(ref BlockEntityAnvil __instance, ICoreClientAPI _capi, List<SmithingRecipe> recipes, BlockPos Pos)
        {
            local = __instance;
            capi = _capi;
            this.recipes = recipes;
            this.Pos = Pos;
        }

        public void OnSelect(int selectedIndex)
        {
            var OSELECTEDRECIPEID = local.GetType().GetField("selectedRecipeId", BindingFlags.NonPublic | BindingFlags.Instance);
            OSELECTEDRECIPEID.SetValue(local, recipes[selectedIndex].RecipeId);
            capi.Network.SendBlockEntityPacket(Pos.X, Pos.Y, Pos.Z, (int)EnumClayFormingPacket.SelectRecipe, SerializerUtil.Serialize(recipes[selectedIndex].RecipeId));
        }
    }
}
