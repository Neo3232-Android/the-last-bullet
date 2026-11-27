using System.Collections;
using UnityEngine;
using TheLastBullet.Core;
using TheLastBullet.Data;

namespace TheLastBullet.Gameplay
{
    /// <summary>
    /// Mid-level random events + map-specific hazard events. This is what makes
    /// the big maps EVENTFUL: things keep happening beyond the waves.
    /// Fired by LevelDirector on a timer; each event lasts ~20s unless noted.
    /// </summary>
    public static class MapEvents
    {
        public static void Trigger(LevelEventId ev, MapDef map)
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            switch (ev)
            {
                case LevelEventId.Fog:
                    gm.StartCoroutine(FogRoutine(20f));
                    break;
                case LevelEventId.SupplyDrop:
                    DropSupplies(gm);
                    break;
                case LevelEventId.HordeSurge:
                    HordeSurge(gm, map, 8);
                    break;
                case LevelEventId.DoubleCoins:
                    gm.SetCoinMultiplier(2f, 60f);
                    break;
                case LevelEventId.LastStand:
                    gm.TriggerSlowMo(2f);
                    if (gm.Player != null)
                        gm.SpawnPickup(PickupKind.Health,
                            gm.Player.transform.position + Vector3.forward * 2f);
                    break;
                case LevelEventId.MapHazard:
                    TriggerMapHazard(gm, map);
                    break;
                case LevelEventId.NightFall:
                    gm.StartCoroutine(NightFallRoutine(20f));
                    break;
                case LevelEventId.RainStorm:
                    gm.StartCoroutine(FogRoutine(15f));
                    break;
            }
        }

        private static void DropSupplies(GameManager gm)
        {
            Vector3 center = gm.Player != null ? gm.Player.transform.position : Vector3.zero;
            gm.SpawnPickup(PickupKind.Health, center + new Vector3(2f, 0f, 2f));
            gm.SpawnPickup(PickupKind.Ammo, center + new Vector3(-2f, 0f, 2f));
            gm.SpawnPickup(PickupKind.Grenade, center + new Vector3(0f, 0f, -2f));
            gm.Juice.SpawnPickupSparkle(center, 12);
        }

        private static void HordeSurge(GameManager gm, MapDef map, int count)
        {
            var kinds = new System.Collections.Generic.List<ZombieKind>(map.ZombieMix.Keys);
            for (int i = 0; i < count; i++)
            {
                var kind = kinds[gm.Rng.Next(kinds.Count)];
                float x = (float)(gm.Rng.NextDouble() * 2 - 1) * map.ArenaWidth / 2f;
                float z = (float)(gm.Rng.NextDouble() * 2 - 1) * map.ArenaHeight / 2f;
                float edge = gm.Rng.Next(4);
                Vector3 pos;
                if (edge == 0) pos = new Vector3(x, 0f, -map.ArenaHeight / 2f + 0.5f);
                else if (edge == 1) pos = new Vector3(x, 0f, map.ArenaHeight / 2f - 0.5f);
                else if (edge == 2) pos = new Vector3(-map.ArenaWidth / 2f + 0.5f, 0f, z);
                else pos = new Vector3(map.ArenaWidth / 2f - 0.5f, 0f, z);
                gm.SpawnZombie(kind, pos, false,
                    gm.CurrentLevel != null ? gm.CurrentLevel.HealthScale : 1f, 1f);
            }
            gm.Juice.AddShake(0.5f);
        }

        /// <summary>Each map's signature hazard event.</summary>
        private static void TriggerMapHazard(GameManager gm, MapDef map)
        {
            switch (map.Id)
            {
                case MapId.Graveyard:
                    // Graves burst open — crawlers climb out around the player.
                    for (int i = 0; i < 3; i++)
                        gm.SummonNear(gm.Player != null ? gm.Player.transform.position : Vector3.zero,
                            ZombieKind.Crawler);
                    break;
                case MapId.Casino:
                    // Slot-machine jackpot: a shower of bonus coins.
                    gm.AddCoins(150);
                    if (gm.Player != null)
                        gm.Juice.SpawnPickupSparkle(gm.Player.transform.position, 20);
                    break;
                case MapId.Mall:
                    // Shutters slam — two free barricades drop in as cover.
                    if (gm.Player != null)
                    {
                        Vector3 p = gm.Player.transform.position;
                        gm.Barricades.Add(new BarricadeEntity(p + Vector3.left * 2f, 150f, 1));
                        gm.Barricades.Add(new BarricadeEntity(p + Vector3.right * 2f, 150f, 1));
                    }
                    break;
                case MapId.School:
                    // Fire alarm — a horde surge from the hallways.
                    HordeSurge(gm, map, 6);
                    break;
                default:
                    // Generic hazard: explosive barrels roll in (a surge of exploders).
                    for (int i = 0; i < 4; i++)
                    {
                        float x = (float)(gm.Rng.NextDouble() * 2 - 1) * map.ArenaWidth / 2f;
                        gm.SpawnZombie(ZombieKind.Exploder, new Vector3(x, 0f, -map.ArenaHeight / 2f + 0.5f),
                            false, gm.CurrentLevel != null ? gm.CurrentLevel.HealthScale : 1f, 1f);
                    }
                    break;
            }
        }

        private static IEnumerator FogRoutine(float duration)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.045f;
            RenderSettings.fogColor = new Color(0.6f, 0.65f, 0.6f);
            yield return new WaitForSeconds(duration);
            RenderSettings.fog = false;
        }

        private static IEnumerator NightFallRoutine(float duration)
        {
            var light = Object.FindFirstObjectByType<Light>();
            float previous = light != null ? light.intensity : 1f;
            if (light != null) light.intensity = 0.25f;
            yield return new WaitForSeconds(duration);
            if (light != null) light.intensity = previous;
        }
    }
}
