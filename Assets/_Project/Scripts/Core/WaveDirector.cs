using System;
using System.Collections.Generic;
using UnityEngine;
using TheLastBullet.Data;

namespace TheLastBullet.Core
{
    /// <summary>
    /// Turns a wave into a timed stream of edge spawns. Port of the Kotlin
    /// WaveDirector. The engine calls Tick() every fixed step; it returns
    /// spawns due, which GameManager materialises as ZombieAI instances.
    /// Spawns happen on arena edges (and, from level 3, at the edges of
    /// newly-unlocked zones — the horde comes from the new ground you opened).
    /// </summary>
    public sealed class WaveDirector : MonoBehaviour
    {
        private struct Spawn
        {
            public ZombieKind? Kind;
            public BossKind? BossKind;
            public Vector3 Position;
        }

        private readonly Queue<Spawn> _queue = new Queue<Spawn>();
        private float _spawnTimer;
        private float _interval;
        private float _arenaHalfW = 11f;
        private float _arenaHalfH = 15f;
        private MapDef _map;
        private LevelDef _level;
        private bool _isEndless;
        private int _endlessWave;

        public bool IsQueueEmpty => _queue.Count == 0;
        public int PendingCount => _queue.Count;

        public void StartLevelWave(LevelDef level, int waveInLevel)
        {
            _level = level;
            _map = MapData.Get(level.Map);
            _isEndless = false;
            _arenaHalfW = _map.ArenaWidth / 2f;
            _arenaHalfH = _map.ArenaHeight / 2f;
            _queue.Clear();
            _spawnTimer = 0.5f;

            // Boss levels: the boss leads the LAST wave, the escort follows.
            bool isBossWave = level.IsBossLevel && waveInLevel == level.WaveCount;
            if (isBossWave)
                _queue.Enqueue(new Spawn { BossKind = level.Boss, Position = EdgePosition() });

            // Wave composition: the level's mix, scaled by the level's count scale.
            int baseCount = (int)((6 + waveInLevel * 3) * level.CountScale);
            var flat = new List<ZombieKind>();
            foreach (var kv in level.ZombieMix)
                for (int i = 0; i < kv.Value; i++) flat.Add(kv.Key);
            // Shuffle, then take the wave's share.
            var rng = GameManager.Instance.Rng;
            for (int i = flat.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                var tmp = flat[i];
                flat[i] = flat[j];
                flat[j] = tmp;
            }
            int take = Math.Min(baseCount, flat.Count);
            for (int i = 0; i < take; i++)
                _queue.Enqueue(new Spawn { Kind = flat[i], Position = EdgePosition() });
            // Late waves add a few brutes even on maps that lack them.
            if (waveInLevel >= 8 && rng.NextDouble() < 0.5f)
                _queue.Enqueue(new Spawn { Kind = ZombieKind.Brute, Position = EdgePosition() });

            _interval = level.SpawnIntervalSec;
        }

        public void StartEndlessWave(MapDef map, int waveNumber)
        {
            _map = map;
            _level = null;
            _isEndless = true;
            _endlessWave = waveNumber;
            _arenaHalfW = map.ArenaWidth / 2f;
            _arenaHalfH = map.ArenaHeight / 2f;
            _queue.Clear();
            _spawnTimer = 0.5f;

            var def = WaveBuilder.BuildWave(waveNumber, map, GameManager.Instance.Rng);
            if (def.IsBossWave)
            {
                var boss = ZombieData.BossForLevel(MapData.IndexOf(map.Id), waveNumber / 5 - 1);
                _queue.Enqueue(new Spawn { BossKind = boss.Kind, Position = EdgePosition() });
            }
            var flat = new List<ZombieKind>();
            foreach (var kv in def.Spawns)
                for (int i = 0; i < kv.Value; i++) flat.Add(kv.Key);
            var rng = GameManager.Instance.Rng;
            for (int i = flat.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                var tmp = flat[i];
                flat[i] = flat[j];
                flat[j] = tmp;
            }
            foreach (var kind in flat)
                _queue.Enqueue(new Spawn { Kind = kind, Position = EdgePosition() });
            _interval = def.SpawnIntervalSec;
        }

        public void Tick(float dt)
        {
            if (_queue.Count == 0) return;
            _spawnTimer -= dt;
            var gm = GameManager.Instance;
            while (_spawnTimer <= 0f && _queue.Count > 0)
            {
                var spawn = _queue.Dequeue();
                _spawnTimer += _interval;
                if (spawn.BossKind.HasValue)
                {
                    float scale = _isEndless
                        ? LevelData.EndlessHealthScale(_endlessWave)
                        : _level.HealthScale;
                    gm.SpawnBoss(spawn.BossKind.Value, spawn.Position, scale);
                }
                else if (spawn.Kind.HasValue)
                {
                    float healthScale = _isEndless
                        ? LevelData.EndlessHealthScale(_endlessWave)
                        : _level.HealthScale;
                    float speedScale = 1f;
                    bool isElite = false;
                    float eliteChance = _isEndless
                        ? ZombieData.EliteChanceForLevel(_endlessWave)
                        : _level.EliteChance;
                    if (gm.Rng.NextDouble() < eliteChance) isElite = true;
                    gm.SpawnZombie(spawn.Kind.Value, spawn.Position, isElite, healthScale, speedScale);
                }
            }
        }

        private Vector3 EdgePosition()
        {
            var rng = GameManager.Instance.Rng;
            switch (rng.Next(4))
            {
                case 0: return new Vector3((float)(rng.NextDouble() * 2 - 1) * _arenaHalfW, 0f, -_arenaHalfH + 0.5f);
                case 1: return new Vector3((float)(rng.NextDouble() * 2 - 1) * _arenaHalfW, 0f, _arenaHalfH - 0.5f);
                case 2: return new Vector3(-_arenaHalfW + 0.5f, 0f, (float)(rng.NextDouble() * 2 - 1) * _arenaHalfH);
                default: return new Vector3(_arenaHalfW - 0.5f, 0f, (float)(rng.NextDouble() * 2 - 1) * _arenaHalfH);
            }
        }
    }
}
