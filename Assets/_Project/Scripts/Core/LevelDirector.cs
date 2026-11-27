using System;
using System.Collections.Generic;
using UnityEngine;
using TheLastBullet.Data;

namespace TheLastBullet.Core
{
    /// <summary>
    /// Level flow: owns the current level's waves, the between-wave breather,
    /// level completion (medal, coins, unlock next level), failure, and the
    /// Endless mode that opens after a map's level 10.
    ///
    /// A level = 3 waves (boss levels 5 and 10 = 2 waves + boss). The old
    /// game's "endless waves" survives only as the Endless mode.
    /// </summary>
    public sealed class LevelDirector : MonoBehaviour
    {
        public LevelDef CurrentLevel { get; private set; }
        public MapDef CurrentMap { get; private set; }
        public int CurrentWaveInLevel { get; private set; }
        public bool IsEndless { get; private set; }
        public int EndlessWave { get; private set; }

        private WaveDirector _waveDirector;
        private float _breatherTimer;
        private bool _betweenWaves;
        private float _eventTimer;
        private float _levelStartTime;
        private float _damageTakenThisLevel;
        private bool _levelFinished;

        public bool IsWaveQueueEmpty => _waveDirector == null || _waveDirector.IsQueueEmpty;

        private void Awake()
        {
            _waveDirector = gameObject.AddComponent<WaveDirector>();
        }

        public void StartLevel(LevelDef level)
        {
            CurrentLevel = level;
            CurrentMap = MapData.Get(level.Map);
            IsEndless = false;
            _levelFinished = false;
            _levelStartTime = GameManager.Instance.SimTime;
            _damageTakenThisLevel = 0f;
            CurrentWaveInLevel = 1;
            MapBuilder.BuildArena(CurrentMap, level.ZonesOpen);
            _waveDirector.StartLevelWave(level, 1);
            _eventTimer = 20f; // first random event ~20s in
            GameManager.Instance.State.Wave = 1;
            GameManager.Instance.State.Phase = GamePhase.Playing;
        }

        public void StartEndless(MapDef map)
        {
            CurrentMap = map;
            CurrentLevel = null;
            IsEndless = true;
            _levelFinished = false;
            EndlessWave = 1;
            MapBuilder.BuildArena(map, map.Zones.Count);
            _waveDirector.StartEndlessWave(map, 1);
            GameManager.Instance.State.Wave = 1;
            GameManager.Instance.State.Phase = GamePhase.Playing;
        }

        /// <summary>Called every fixed step by GameManager.</summary>
        public void Tick(float dt)
        {
            if (GameManager.Instance.State.Phase != GamePhase.Playing) return;

            if (_betweenWaves)
            {
                _breatherTimer -= dt;
                if (_breatherTimer <= 0f)
                {
                    _betweenWaves = false;
                    StartNextWave();
                }
                return;
            }

            _waveDirector.Tick(dt);
            TickEvents(dt);
        }

        /// <summary>Called by GameManager when the wave queue is empty and nothing is alive.</summary>
        public void OnWaveCleared()
        {
            if (_levelFinished) return;
            int totalWaves = CurrentLevel != null ? CurrentLevel.WaveCount : int.MaxValue;
            if (CurrentWaveInLevel >= totalWaves)
            {
                CompleteLevel();
                return;
            }
            // Between-wave breather: short pause, shop hook, then the next wave.
            _betweenWaves = true;
            _breatherTimer = 4f;
            GameManager.Instance.State.Phase = GamePhase.BetweenWaves;
        }

        private void StartNextWave()
        {
            CurrentWaveInLevel++;
            GameManager.Instance.State.Wave = CurrentWaveInLevel;
            GameManager.Instance.State.Phase = GamePhase.Playing;
            if (IsEndless)
            {
                EndlessWave = CurrentWaveInLevel;
                _waveDirector.StartEndlessWave(CurrentMap, EndlessWave);
            }
            else
            {
                _waveDirector.StartLevelWave(CurrentLevel, CurrentWaveInLevel);
            }
        }

        private void CompleteLevel()
        {
            _levelFinished = true;
            var gm = GameManager.Instance;
            float clearTime = gm.SimTime - _levelStartTime;
            var medal = LevelData.MedalForRun(CurrentLevel, clearTime,
                _damageTakenThisLevel, gm.State.Player.MaxHealth);
            var profile = SaveSystem.Instance.Profile;
            profile.Coins += CurrentLevel.CoinReward;
            SaveSystem.Instance.RecordLevelClear(CurrentLevel.Map, CurrentLevel.Level, medal);
            gm.State.Phase = GamePhase.GameOver; // level-complete screen reuses the end screen
            gm.Won = true;
            // The UI shows the level-complete panel (medal, coins, next level button).
        }

        /// <summary>Called by GameManager.EndRun — records run stats, best wave, accuracy.</summary>
        public void OnRunEnded(bool won)
        {
            var gm = GameManager.Instance;
            var profile = SaveSystem.Instance.Profile;
            profile.TotalRuns++;
            profile.TotalKills += gm.Kills;
            profile.BestWave = Math.Max(profile.BestWave, gm.State.Wave);
            profile.MostKills = Math.Max(profile.MostKills, gm.Kills);
            profile.LongestSurvivalSec = Math.Max(profile.LongestSurvivalSec,
                (int)gm.State.TimeSurvivedSec);
            if (gm.ShotsFired > 0)
            {
                float acc = 100f * gm.ShotsHit / gm.ShotsFired;
                profile.BestAccuracyPct = Math.Max(profile.BestAccuracyPct, acc);
            }
            profile.Coins += gm.RunCoins;
            SaveSystem.Instance.Save();
        }

        public void RecordDamageTaken(float amount) => _damageTakenThisLevel += amount;

        // ---- random events during a level (see Gameplay/MapEvents) ----

        private void TickEvents(float dt)
        {
            if (CurrentLevel == null || CurrentLevel.Events.Count == 0) return;
            _eventTimer -= dt;
            if (_eventTimer > 0f) return;
            _eventTimer = 25f + (float)GameManager.Instance.Rng.NextDouble() * 15f;
            var events = CurrentLevel.Events;
            var ev = events[GameManager.Instance.Rng.Next(events.Count)];
            Gameplay.MapEvents.Trigger(ev, CurrentMap);
        }
    }
}
