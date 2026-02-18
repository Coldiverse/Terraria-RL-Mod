using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace Terraria_RL_Mod
{
    public class RLSystem : ModSystem
    {
        private const int TileWindowSize = 21;
        private const int TileWindowRadius = TileWindowSize / 2;
        private const int TileChannels = 4;
        private const int MaxEntities = 8;

        private int _tick;

        public override void PostUpdatePlayers()
        {
            if (Main.gameMenu) return;

            Player player = Main.LocalPlayer;
            if (player == null) return;

            try
            {
                _tick++;
                ActionDTO action = BridgeServer.Instance.GetLatestAction();
                if (action != null)
                {
                    ApplyActionId(player, action.action_id);
                }
                StateDTO state = GatherState(player);
                BridgeServer.Instance.Init(Mod);
                BridgeServer.Instance.Update(player, state);
            }
            catch (Exception ex)
            {
                Mod?.Logger.Warn($"TeRL error in PostUpdatePlayers: {ex.Message}");
            }
        }

        private StateDTO GatherState(Player player)
        {
            int tick = _tick;
            bool left = player.controlLeft;
            bool right = player.controlRight;
            bool jump = player.controlJump;
            bool use = player.controlUseItem;

            int actionId;
            if (left && jump) actionId = 4;
            else if (right && jump) actionId = 5;
            else if (left) actionId = 1;
            else if (right) actionId = 2;
            else if (jump) actionId = 3;
            else if (use) actionId = 6;
            else actionId = 0;

            var state = new StateDTO
            {
                tick = tick,
                player = BuildPlayerDTO(player),
                inventory = BuildInventoryDTO(player),
                tile_window = BuildTileWindowDTO(player),
                nearby_entities = BuildNearbyEntities(player),
                action = new ActionDTO
                {
                    action_id = actionId
                }
            };

            return state;
        }

        private static void ApplyActionId(Player player, int actionId)
        {
            player.controlLeft = false;
            player.controlRight = false;
            player.controlJump = false;
            player.controlUseItem = false;
            switch (actionId)
            {
                case 1: player.controlLeft = true; break;
                case 2: player.controlRight = true; break;
                case 3: player.controlJump = true; break;
                case 4: player.controlLeft = true; player.controlJump = true; break;
                case 5: player.controlRight = true; player.controlJump = true; break;
                case 6: player.controlUseItem = true; break;
                default: break;
            }
        }

        private PlayerDTO BuildPlayerDTO(Player player)
        {
            float tileXFloat = player.Center.X / 16f;
            float tileYFloat = player.Center.Y / 16f;

            int tileX = (int)Math.Floor(tileXFloat);
            int tileY = (int)Math.Floor(tileYFloat);

            return new PlayerDTO
            {
                tile_x = tileX,
                tile_y = tileY,
                subtile_x = tileXFloat - tileX,
                subtile_y = tileYFloat - tileY,
                vx = player.velocity.X,
                vy = player.velocity.Y,
                facing = player.direction,
                on_ground = player.velocity.Y == 0f,
                hp = player.statLife,
                selected_hotbar_slot = player.selectedItem
            };
        }

        private InventoryDTO BuildInventoryDTO(Player player)
        {
            int[] ids = new int[10];
            int[] stacks = new int[10];

            for (int i = 0; i < 10; i++)
            {
                Item item = player.inventory[i];
                ids[i] = item?.type ?? 0;
                stacks[i] = item?.stack ?? 0;
            }

            return new InventoryDTO
            {
                hotbar_item_ids = ids,
                hotbar_stack_counts = stacks
            };
        }

        private TileWindowDTO BuildTileWindowDTO(Player player)
        {
            int centerX = (int)(player.Center.X / 16f);
            int centerY = (int)(player.Center.Y / 16f);

            List<int> flat = new List<int>(TileWindowSize * TileWindowSize * TileChannels);

            int maxX = Main.maxTilesX;
            int maxY = Main.maxTilesY;
            if (maxX <= 0 || maxY <= 0)
            {
                for (int i = 0; i < TileWindowSize * TileWindowSize * TileChannels; i++)
                    flat.Add(0);
                return new TileWindowDTO
                {
                    width = TileWindowSize,
                    height = TileWindowSize,
                    center_offset_x = TileWindowRadius,
                    center_offset_y = TileWindowRadius,
                    channels = TileChannels,
                    channel_order = new[] { "tile_id", "solid", "liquid", "slope" },
                    tiles_flat = flat.ToArray()
                };
            }

            for (int dy = -TileWindowRadius; dy <= TileWindowRadius; dy++)
            {
                for (int dx = -TileWindowRadius; dx <= TileWindowRadius; dx++)
                {
                    int worldX = centerX + dx;
                    int worldY = centerY + dy;

                    if (worldX < 0 || worldX >= maxX || worldY < 0 || worldY >= maxY)
                    {
                        flat.Add(0);
                        flat.Add(0);
                        flat.Add(0);
                        flat.Add(0);
                        continue;
                    }

                    Tile tile = Main.tile[worldX, worldY];

                    int tileId = tile.HasTile ? (int)tile.TileType : 0;
                    int solid = tile.HasTile ? 1 : 0;
                    int liquid = tile.LiquidAmount;
                    int slope = (int)tile.Slope;

                    flat.Add(tileId);
                    flat.Add(solid);
                    flat.Add(liquid);
                    flat.Add(slope);
                }
            }

            return new TileWindowDTO
            {
                width = TileWindowSize,
                height = TileWindowSize,
                center_offset_x = TileWindowRadius,
                center_offset_y = TileWindowRadius,
                channels = TileChannels,
                channel_order = new[] { "tile_id", "solid", "liquid", "slope" },
                tiles_flat = flat.ToArray()
            };
        }

        private List<EntityDTO> BuildNearbyEntities(Player player)
        {
            List<EntityDTO> entities = new List<EntityDTO>();
            Vector2 playerTile = new Vector2(player.Center.X / 16f, player.Center.Y / 16f);

            if (Main.npc == null)
                return PadEntities(entities);

            for (int i = 0; i < Main.npc.Length; i++)
            {
                NPC npc = Main.npc[i];
                if (npc == null || !npc.active) continue;

                Vector2 npcTile = new Vector2(npc.Center.X / 16f, npc.Center.Y / 16f);

                entities.Add(new EntityDTO
                {
                    type_id = npc.type,
                    tile_x = (int)npcTile.X,
                    tile_y = (int)npcTile.Y,
                    vx = npc.velocity.X,
                    vy = npc.velocity.Y,
                    hp = npc.life
                });
            }

            // Sort by distance
            entities.Sort((a, b) =>
            {
                float da = Vector2.Distance(new Vector2(a.tile_x, a.tile_y), playerTile);
                float db = Vector2.Distance(new Vector2(b.tile_x, b.tile_y), playerTile);
                return da.CompareTo(db);
            });

            if (entities.Count > MaxEntities)
                entities = entities.GetRange(0, MaxEntities);

            return PadEntities(entities);
        }

        private static List<EntityDTO> PadEntities(List<EntityDTO> entities)
        {
            while (entities.Count < MaxEntities)
                entities.Add(new EntityDTO());
            return entities;
        }

        private static ushort GetTileType(Tile tile)
        {
            return tile.TileType;;
        }
    }
}

