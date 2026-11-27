using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TheLastBullet.Core;
using TheLastBullet.Data;

namespace TheLastBullet.UI
{
    /// <summary>
    /// Main menu, built entirely in code (uGUI (built-in Text)): map grid (20
    /// maps), level row (10 levels per map, locks + medals), campaign/endless
    /// start, character creator (skin, hair, outfit, accessory, starting
    /// weapon/perk), and settings (quality, mute). Created by Bootstrap;
    /// hides itself while a run is playing and returns when the run ends.
    /// </summary>
    public sealed class MenuController : MonoBehaviour
    {
        private Canvas _canvas;
        private Text _coinsText;
        private Text _selectedMapText;
        private Text _creatorText;
        private Text _settingsText;
        private Text _modeText;
        private Text _progressionText;
        private string _loginRewardNote = "";
        private readonly List<Button> _mapButtons = new List<Button>();
        private readonly List<Button> _levelButtons = new List<Button>();
        private MapId _selectedMap = MapId.CityStreet;
        private HUDController _hud;

        private static readonly string[] SkinTones =
            { "#F1C27D", "#E0AC69", "#C68642", "#8D5524", "#FFDBAC" };
        private static readonly string[] HairColors =
            { "#1A1A1A", "#4A2C0A", "#8B4513", "#C0C0C0", "#FFD700", "#FF4500" };
        private static readonly string[] OutfitColors =
            { "#FFD700", "#FF4500", "#1E90FF", "#32CD32", "#8A2BE2", "#FFFFFF" };

        private void Awake()
        {
            // Safety net: the menu is dead without an EventSystem, so make
            // sure one exists even if GameManager's Awake did not run first.
            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.InputSystemUIInputModule>();
                DontDestroyOnLoad(es);
            }
            _hud = gameObject.AddComponent<HUDController>();
            Build();
            ShowMenu();
        }

        public void ShowMenu()
        {
            var gm = GameManager.Instance;
            if (gm != null) gm.State.Phase = GamePhase.Menu;
            if (_canvas != null) _canvas.gameObject.SetActive(true);
            if (_hud != null) _hud.Hide();
            // Daily housekeeping: fresh missions when the day turns over, and
            // today's login reward if it has not been claimed yet.
            var save = SaveSystem.Instance;
            if (save != null)
            {
                save.RollMissionsIfNeeded();
                var reward = save.GrantLoginRewardIfDue();
                _loginRewardNote = reward != null
                    ? $"Login reward: +{save.LastLoginRewardGrantedCoins} coins (Day {save.Profile.LoginStreakDays} streak)!  "
                    : "";
            }
            Refresh();
        }

        public void HideMenu()
        {
            if (_canvas != null) _canvas.gameObject.SetActive(false);
            if (_hud != null) _hud.Show();
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || _canvas == null) return;
            bool inRun = gm.State.Phase == GamePhase.Playing || gm.State.Phase == GamePhase.BetweenWaves;
            if (inRun && _canvas.gameObject.activeSelf) _canvas.gameObject.SetActive(false);
            if (gm.State.Phase == GamePhase.Menu && !_canvas.gameObject.activeSelf)
                _canvas.gameObject.SetActive(true);
        }

        private void Build()
        {
            var canvasGo = new GameObject("MenuCanvas");
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 20;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720, 1280);
            canvasGo.AddComponent<GraphicRaycaster>();

            // Full-screen backdrop so the menu is not bare black. Added
            // first so it sits behind everything; raycastTarget off so it
            // never eats taps meant for the buttons.
            var bgGo = new GameObject("Backdrop");
            bgGo.transform.SetParent(canvasGo.transform, false);
            var bgRt = bgGo.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
            var bg = bgGo.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.04f, 0.07f, 1f);
            bg.raycastTarget = false;

            var title = MakeText(canvasGo.transform, "THE LAST BULLET", new Vector2(0, -40),
                new Vector2(700, 70), TextAnchor.UpperCenter);
            title.fontSize = 52;
            title.color = new Color(1f, 0.84f, 0f);
            MakeText(canvasGo.transform, "20 maps. 200 levels. Survive.", new Vector2(0, -110),
                new Vector2(700, 30), TextAnchor.UpperCenter).fontSize = 22;
            _coinsText = MakeText(canvasGo.transform, "Coins: 0", new Vector2(0, -150),
                new Vector2(700, 30), TextAnchor.UpperCenter);

            _selectedMapText = MakeText(canvasGo.transform, "", new Vector2(0, -185),
                new Vector2(700, 30), TextAnchor.UpperCenter);

            // Map grid: 4 columns x 5 rows. First row at -250 so it clears
            // the map-info line above it (build 14: the two overlapped).
            for (int i = 0; i < MapData.All.Count; i++)
            {
                var map = MapData.All[i];
                int col = i % 4, row = i / 4;
                var btn = MakeButton(canvasGo.transform, ShortName(map.Name),
                    new Vector2(-255 + col * 170, -250 - row * 64), new Vector2(160, 54),
                    () => { _selectedMap = map.Id; Refresh(); });
                _mapButtons.Add(btn);
            }

            // Level row: 10 levels for the selected map.
            for (int lvl = 1; lvl <= LevelData.LevelsPerMap; lvl++)
            {
                int level = lvl;
                var btn = MakeButton(canvasGo.transform, level.ToString(),
                    new Vector2(-297 + (lvl - 1) * 66, -590), new Vector2(58, 58),
                    () => StartLevel(_selectedMap, level));
                _levelButtons.Add(btn);
            }

            MakeButton(canvasGo.transform, "PLAY LEVEL", new Vector2(0, -680), new Vector2(300, 64),
                () => StartLevel(_selectedMap, 1));
            MakeButton(canvasGo.transform, "ENDLESS", new Vector2(0, -760), new Vector2(300, 64),
                () =>
                {
                    GameManager.Instance.StartEndless(_selectedMap);
                    HideMenu();
                });

            // Modes (build 2): each starts through the LevelDirector, which
            // self-bootstraps the run. Parent may switch these to the thin
            // GameManager wrappers (StartTimed etc.) — same behaviour.
            _modeText = MakeText(canvasGo.transform, "MODES", new Vector2(0, -815),
                new Vector2(700, 26), TextAnchor.UpperCenter);
            _modeText.fontSize = 20;
            _modeText.color = new Color(1f, 0.84f, 0f);
            MakeButton(canvasGo.transform, "TIMED 5:00", new Vector2(-235, -860), new Vector2(200, 50),
                () => StartModeTimed());
            MakeButton(canvasGo.transform, "DEFEND", new Vector2(0, -860), new Vector2(200, 50),
                () => StartModeDefend());
            MakeButton(canvasGo.transform, "ESCORT", new Vector2(235, -860), new Vector2(200, 50),
                () => StartModeEscort());
            MakeButton(canvasGo.transform, "BOSS RUSH", new Vector2(-235, -920), new Vector2(200, 50),
                () => StartModeBossRush());
            MakeButton(canvasGo.transform, "HARDCORE", new Vector2(0, -920), new Vector2(200, 50),
                () => StartModeHardcore());
            MakeButton(canvasGo.transform, "DAILY", new Vector2(235, -920), new Vector2(200, 50),
                () => StartModeDaily());

            // Character creator. Two lines at a smaller size: the one-line
            // version ran off the right edge of the screen (build 14).
            _creatorText = MakeText(canvasGo.transform, "", new Vector2(0, -985),
                new Vector2(700, 44), TextAnchor.UpperCenter);
            _creatorText.fontSize = 18;
            MakeButton(canvasGo.transform, "Skin", new Vector2(-240, -1040), new Vector2(140, 54),
                () => Cycle(SkinTones, () => SaveSystem.Instance.Character.SkinTone,
                    v => SaveSystem.Instance.Character.SkinTone = v));
            MakeButton(canvasGo.transform, "Hair", new Vector2(-80, -1040), new Vector2(140, 54),
                () => Cycle(HairColors, () => SaveSystem.Instance.Character.HairColor,
                    v => SaveSystem.Instance.Character.HairColor = v));
            MakeButton(canvasGo.transform, "Outfit", new Vector2(80, -1040), new Vector2(140, 54),
                () => Cycle(OutfitColors, () => SaveSystem.Instance.Character.OutfitColor,
                    v => SaveSystem.Instance.Character.OutfitColor = v));
            MakeButton(canvasGo.transform, "Style", new Vector2(240, -1040), new Vector2(140, 54),
                () =>
                {
                    var c = SaveSystem.Instance.Character;
                    c.HairStyle = (HairStyle)(((int)c.HairStyle + 1) % System.Enum.GetValues(typeof(HairStyle)).Length);
                    SaveSystem.Instance.Save();
                    Refresh();
                });

            // Settings.
            _settingsText = MakeText(canvasGo.transform, "", new Vector2(0, -1100),
                new Vector2(700, 30), TextAnchor.UpperCenter);
            MakeButton(canvasGo.transform, "Quality", new Vector2(-90, -1150), new Vector2(160, 54),
                () =>
                {
                    var save = SaveSystem.Instance;
                    save.QualitySetting = (Quality)(((int)save.QualitySetting + 1) % 4);
                    QualitySettingsApplier.Apply(save.QualitySetting);
                    save.Save();
                    Refresh();
                });
            MakeButton(canvasGo.transform, "Mute", new Vector2(90, -1150), new Vector2(160, 54),
                () =>
                {
                    var audio = Audio.SynthAudio.Instance;
                    if (audio != null) audio.Muted = !audio.Muted;
                    Refresh();
                });

            // Progression panel (text only): streak, bestiary, drone, today's missions.
            _progressionText = MakeText(canvasGo.transform, "", new Vector2(0, -1210),
                new Vector2(700, 60), TextAnchor.UpperCenter);
            _progressionText.fontSize = 18;
        }

        private void Cycle(string[] options, System.Func<string> get, System.Action<string> apply)
        {
            int idx = System.Array.IndexOf(options, get());
            if (idx < 0) idx = 0;
            apply(options[(idx + 1) % options.Length]);
            SaveSystem.Instance.Save();
            Refresh();
        }

        private void StartLevel(MapId map, int level)
        {
            var profile = SaveSystem.Instance.Profile;
            if (level > profile.UnlockedLevel(map)) return; // locked
            GameManager.Instance.StartCampaignLevel(map, level);
            HideMenu();
        }

        private LevelDirector ModeDirector()
        {
            var gm = GameManager.Instance;
            return gm != null ? gm.LevelDirector : null;
        }

        private void StartModeTimed()
        {
            var ld = ModeDirector();
            if (ld == null) return;
            ld.StartTimed(MapData.Get(_selectedMap), LevelDirector.DefaultTimedSeconds);
            HideMenu();
        }

        private void StartModeDefend()
        {
            var ld = ModeDirector();
            if (ld == null) return;
            ld.StartDefendPoint(MapData.Get(_selectedMap));
            HideMenu();
        }

        private void StartModeEscort()
        {
            var ld = ModeDirector();
            if (ld == null) return;
            ld.StartEscort(MapData.Get(_selectedMap));
            HideMenu();
        }

        private void StartModeBossRush()
        {
            var ld = ModeDirector();
            if (ld == null) return;
            ld.StartBossRush(MapData.Get(_selectedMap));
            HideMenu();
        }

        private void StartModeHardcore()
        {
            var ld = ModeDirector();
            if (ld == null) return;
            // Hardcore runs the hardest level unlocked on the selected map.
            int level = Mathf.Clamp(SaveSystem.Instance.Profile.UnlockedLevel(_selectedMap),
                1, LevelData.LevelsPerMap);
            ld.StartHardcore(MapData.Get(_selectedMap), level);
            HideMenu();
        }

        private void StartModeDaily()
        {
            var ld = ModeDirector();
            if (ld == null) return;
            ld.StartDailyChallenge();
            HideMenu();
        }

        private void Refresh()
        {
            var save = SaveSystem.Instance;
            var profile = save.Profile;
            _coinsText.text = $"Coins: {profile.Coins}    Best wave: {profile.BestWave}    Total kills: {profile.TotalKills}";
            var map = MapData.Get(_selectedMap);
            _selectedMapText.text = $"{map.Name}  —  {map.ArenaWidth:0}m x {map.ArenaHeight:0}m" +
                (map.IsDark ? "  (Night)" : "") + (map.IsOutdoor ? "  (Outdoor)" : "");
            int unlocked = profile.UnlockedLevel(_selectedMap);
            for (int i = 0; i < _levelButtons.Count; i++)
            {
                int lvl = i + 1;
                var label = _levelButtons[i].GetComponentInChildren<Text>();
                string medal = "";
                if (profile.BestMedalPerLevel.TryGetValue($"{_selectedMap}-{lvl}", out var m) && m != MedalTier.None)
                    medal = m == MedalTier.Platinum ? " P" : m == MedalTier.Gold ? " G" : m == MedalTier.Silver ? " S" : " B";
                label.text = lvl + medal;
                _levelButtons[i].interactable = lvl <= unlocked;
            }
            var c = save.Character;
            _creatorText.text = $"You: skin {c.SkinTone}  hair {c.HairStyle} {c.HairColor}  outfit {c.OutfitColor}\n" +
                $"weapon {c.StartingWeaponId}  perk {c.StartingPerkId}";
            _settingsText.text = $"Quality: {save.QualitySetting}    Sound: " +
                (Audio.SynthAudio.Instance != null && Audio.SynthAudio.Instance.Muted ? "Muted" : "On");
            var dailyMap = LevelDirector.TodayDailyMap();
            var mutation = LevelDirector.TodayDailyMutation();
            _modeText.text = "MODES  —  Today: " + dailyMap.Name +
                (mutation != null ? " (" + mutation.Name + ": " + mutation.Description + ")" : "");
            string missionText = "";
            var missions = save.TodaysDailyMissions();
            for (int i = 0; i < missions.Count; i++)
            {
                var m = missions[i];
                int p = profile.DailyMissionProgress.TryGetValue(m.Id, out int v) ? v : 0;
                bool done = profile.CompletedDailyMissions.Contains(m.Id);
                if (i > 0) missionText += "  •  ";
                missionText += m.Description + " (" + System.Math.Min(p, m.Target) + "/" + m.Target + ")" +
                    (done ? " DONE" : "");
            }
            _progressionText.text = _loginRewardNote +
                $"Streak: {profile.LoginStreakDays}d  •  Bestiary: {profile.BestiaryTotalCompletedCount()}/{PlayerProfile.BestiaryTotalCount()}  •  " +
                $"Drone: {(profile.HasDroneCompanion ? "Lv" + profile.DroneLevel : "No")}  •  Daily streak: {profile.DailyChallengeStreak}\n" +
                missionText;
        }

        private static string ShortName(string name) =>
            name.Length > 12 ? name.Substring(0, 11) + "…" : name;

        private Text MakeText(Transform parent, string text, Vector2 anchoredPos,
            Vector2 size, TextAnchor align)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            rt.pivot = new Vector2(0.5f, 1f);
            var tmp = go.AddComponent<Text>();
            tmp.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            tmp.text = text;
            tmp.fontSize = 24;
            tmp.color = Color.white;
            tmp.alignment = align;
            // Labels never eat taps: only buttons are raycast targets, so a
            // text that overlaps a button (e.g. the map-info line over the
            // grid in build 14) can never block that button.
            tmp.raycastTarget = false;
            return tmp;
        }

        private Button MakeButton(Transform parent, string label, Vector2 anchoredPos, Vector2 size,
            UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("Button_" + label);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
            rt.anchoredPosition = anchoredPos; rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.25f, 0.25f, 0.32f, 0.95f);
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(onClick);
            var textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);
            var textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero; textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero; textRt.offsetMax = Vector2.zero;
            var tmp = textGo.AddComponent<Text>();
            tmp.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            tmp.text = label;
            tmp.fontSize = 20;
            tmp.color = new Color(1f, 0.84f, 0f);
            tmp.alignment = TextAnchor.MiddleCenter;
            tmp.raycastTarget = false;
            return btn;
        }
    }
}
