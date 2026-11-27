using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TheLastBullet.Core;
using TheLastBullet.Data;
using TheLastBullet.Gameplay;

namespace TheLastBullet.UI
{
    /// <summary>
    /// In-run HUD, built entirely in code (uGUI (built-in Text)): health/XP bars,
    /// coins/kills/wave, ammo, boss bar, perk-choice overlay (the game pauses
    /// its simulation while choices are pending), buildable shop buttons,
    /// dodge/swap/pause, and the game-over panel. Created once by
    /// MenuController; hidden while in the menu.
    /// </summary>
    public sealed class HUDController : MonoBehaviour
    {
        private Canvas _canvas;
        private Text _coinsText, _killsText, _waveText, _ammoText, _timerText;
        private Image _healthFill, _xpFill, _bossFill;
        private Text _bossNameText;
        private GameObject _bossBar, _perkPanel, _gameOverPanel;
        private Text _gameOverText;
        private readonly List<Button> _perkButtons = new List<Button>();
        private readonly List<Text> _perkLabels = new List<Text>();
        private Button _superPerkButton;
        private Text _superPerkLabel;
        private bool _shown;

        public void Show() { _shown = true; if (_canvas != null) _canvas.gameObject.SetActive(true); }
        public void Hide() { _shown = false; if (_canvas != null) _canvas.gameObject.SetActive(false); }

        private void Awake()
        {
            Build();
            Hide();
        }

        private void Build()
        {
            var canvasGo = new GameObject("HUDCanvas");
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 5;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720, 1280);
            canvasGo.AddComponent<GraphicRaycaster>();

            // Top-left: health bar + XP bar.
            _healthFill = MakeBar(canvasGo.transform, new Vector2(20, -20), new Vector2(300, 28),
                new Color(0.85f, 0.15f, 0.2f));
            _xpFill = MakeBar(canvasGo.transform, new Vector2(20, -56), new Vector2(300, 10),
                new Color(0.3f, 0.7f, 1f));

            // Top-right: coins / kills / wave / timer.
            _coinsText = MakeText(canvasGo.transform, "Coins: 0", new Vector2(-20, -20),
                new Vector2(300, 30), TextAnchor.UpperRight);
            _killsText = MakeText(canvasGo.transform, "Kills: 0", new Vector2(-20, -52),
                new Vector2(300, 26), TextAnchor.UpperRight);
            _waveText = MakeText(canvasGo.transform, "Wave 1", new Vector2(-20, -84),
                new Vector2(300, 26), TextAnchor.UpperRight);
            _timerText = MakeText(canvasGo.transform, "0:00", new Vector2(-20, -116),
                new Vector2(300, 26), TextAnchor.UpperRight);

            // Boss bar (top-center, hidden unless a boss is alive).
            _bossBar = new GameObject("BossBar");
            _bossBar.transform.SetParent(canvasGo.transform, false);
            var bossRt = _bossBar.AddComponent<RectTransform>();
            bossRt.anchorMin = new Vector2(0.5f, 1f); bossRt.anchorMax = new Vector2(0.5f, 1f);
            bossRt.anchoredPosition = new Vector2(0, -150); bossRt.sizeDelta = new Vector2(500, 24);
            var bossBg = _bossBar.AddComponent<Image>();
            bossBg.color = new Color(0f, 0f, 0f, 0.5f);
            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(_bossBar.transform, false);
            _bossFill = fillGo.AddComponent<Image>();
            _bossFill.color = new Color(0.7f, 0.05f, 0.1f);
            _bossFill.type = Image.Type.Filled;
            _bossFill.fillMethod = Image.FillMethod.Horizontal;
            var bossFillRt = _bossFill.rectTransform;
            bossFillRt.anchorMin = Vector2.zero; bossFillRt.anchorMax = Vector2.one;
            bossFillRt.offsetMin = Vector2.zero; bossFillRt.offsetMax = Vector2.zero;
            _bossNameText = MakeText(canvasGo.transform, "", new Vector2(0, -180),
                new Vector2(500, 26), TextAnchor.UpperCenter);
            _bossBar.SetActive(false);

            // Ammo (bottom-right, above the aim stick).
            _ammoText = MakeText(canvasGo.transform, "", new Vector2(-20, 200),
                new Vector2(300, 30), TextAnchor.LowerRight);

            // Build buttons (bottom-center).
            MakeButton(canvasGo.transform, "Barricade 100", new Vector2(-140, 40), new Vector2(130, 56),
                () => TryBuild(() =>
                {
                    var gm = GameManager.Instance;
                    return gm.Player != null && gm.PlaceBarricade(gm.Player.transform.position + Vector3.forward * 2f, 1);
                }));
            MakeButton(canvasGo.transform, "Turret 300", new Vector2(0, 40), new Vector2(130, 56),
                () => TryBuild(() =>
                {
                    var gm = GameManager.Instance;
                    return gm.Player != null && gm.PlaceTurret(gm.Player.transform.position + Vector3.forward * 2f);
                }));
            MakeButton(canvasGo.transform, "Trap 75", new Vector2(140, 40), new Vector2(130, 56),
                () => TryBuild(() =>
                {
                    var gm = GameManager.Instance;
                    return gm.Player != null && gm.PlaceTrap(TrapKind.Spike, gm.Player.transform.position + Vector3.forward * 2f);
                }));

            // Dodge + swap (bottom-right cluster, above aim stick).
            MakeButton(canvasGo.transform, "DODGE", new Vector2(-90, 130), new Vector2(110, 56),
                () => GameManager.Instance.Player?.DodgeRoll());
            MakeButton(canvasGo.transform, "SWAP", new Vector2(-210, 130), new Vector2(110, 56),
                () => GameManager.Instance.Player?.SwapWeapon());

            // Pause (top-center).
            MakeButton(canvasGo.transform, "II", new Vector2(0, -20), new Vector2(56, 56),
                () =>
                {
                    var gm = GameManager.Instance;
                    if (gm.IsPaused) gm.Resume(); else gm.Pause();
                }, topAnchor: true);

            BuildPerkPanel(canvasGo.transform);
            BuildGameOverPanel(canvasGo.transform);
        }

        private void TryBuild(System.Func<bool> place)
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.State.Phase != GamePhase.Playing) return;
            place();
        }

        private Image MakeBar(Transform parent, Vector2 anchoredPos, Vector2 size, Color color)
        {
            var bg = new GameObject("Bar");
            bg.transform.SetParent(parent, false);
            var bgRt = bg.AddComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0f, 1f); bgRt.anchorMax = new Vector2(0f, 1f);
            bgRt.anchoredPosition = anchoredPos; bgRt.sizeDelta = size;
            bgRt.pivot = new Vector2(0f, 1f);
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.55f);
            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(bg.transform, false);
            var fill = fillGo.AddComponent<Image>();
            fill.color = color;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            var fillRt = fill.rectTransform;
            fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero; fillRt.offsetMax = Vector2.zero;
            return fill;
        }

        private Text MakeText(Transform parent, string text, Vector2 anchoredPos,
            Vector2 size, TextAnchor align)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            bool right = align == TextAnchor.UpperRight || align == TextAnchor.LowerRight;
            bool top = align == TextAnchor.UpperRight || align == TextAnchor.UpperCenter;
            bool centered = align == TextAnchor.MiddleCenter || align == TextAnchor.UpperCenter;
            rt.anchorMin = new Vector2(centered ? 0.5f : right ? 1f : 0f, top ? 1f : 0f);
            rt.anchorMax = rt.anchorMin;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            rt.pivot = new Vector2(centered ? 0.5f : right ? 1f : 0f, top ? 1f : 0f);
            var tmp = go.AddComponent<Text>();
            tmp.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            tmp.text = text;
            tmp.fontSize = 24;
            tmp.color = Color.white;
            tmp.alignment = align;
            // Labels never eat taps: only buttons are raycast targets.
            tmp.raycastTarget = false;
            return tmp;
        }

        private Button MakeButton(Transform parent, string label, Vector2 anchoredPos, Vector2 size,
            UnityEngine.Events.UnityAction onClick, bool topAnchor = false)
        {
            var go = new GameObject("Button_" + label);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, topAnchor ? 1f : 0f);
            rt.anchorMax = new Vector2(0.5f, topAnchor ? 1f : 0f);
            rt.anchoredPosition = anchoredPos; rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.26f, 0.85f);
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

        private void BuildPerkPanel(Transform parent)
        {
            _perkPanel = new GameObject("PerkPanel");
            _perkPanel.transform.SetParent(parent, false);
            var rt = _perkPanel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(640, 540);
            var bg = _perkPanel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.8f);
            var title = MakeText(_perkPanel.transform, "LEVEL UP — pick a perk", new Vector2(0, 160),
                new Vector2(600, 40), TextAnchor.MiddleCenter);
            title.fontSize = 30;
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                var btn = MakeButton(_perkPanel.transform, "", new Vector2(0, 80 - i * 100),
                    new Vector2(560, 80), () =>
                    {
                        var gm = GameManager.Instance;
                        if (gm != null && index < gm.PendingPerkChoices.Count)
                            gm.ApplyPerk(gm.PendingPerkChoices[index]);
                    });
                _perkButtons.Add(btn);
                var label = btn.GetComponentInChildren<Text>();
                _perkLabels.Add(label);
            }
            // Gold super-perk offer (rare level-up roll, shown only when present).
            _superPerkButton = MakeButton(_perkPanel.transform, "", new Vector2(0, -210),
                new Vector2(560, 80), () =>
                {
                    var gm = GameManager.Instance;
                    if (gm != null && gm.PendingSuperPerkOffer != null)
                        gm.ApplySuperPerk(gm.PendingSuperPerkOffer.Id);
                });
            var goldImg = _superPerkButton.GetComponent<Image>();
            if (goldImg != null) goldImg.color = new Color(0.85f, 0.65f, 0.1f, 0.95f);
            _superPerkLabel = _superPerkButton.GetComponentInChildren<Text>();
            _perkPanel.SetActive(false);
        }

        private void BuildGameOverPanel(Transform parent)
        {
            _gameOverPanel = new GameObject("GameOverPanel");
            _gameOverPanel.transform.SetParent(parent, false);
            var rt = _gameOverPanel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var bg = _gameOverPanel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.85f);
            _gameOverText = MakeText(_gameOverPanel.transform, "", new Vector2(0, 100),
                new Vector2(640, 300), TextAnchor.MiddleCenter);
            _gameOverText.fontSize = 28;
            MakeButton(_gameOverPanel.transform, "RETRY", new Vector2(0, -120), new Vector2(220, 70),
                () =>
                {
                    var gm = GameManager.Instance;
                    if (gm == null) return;
                    // Retry the mode that was just played, not just Endless.
                    switch (gm.State.Mode)
                    {
                        case GameMode.Timed: gm.StartTimed(gm.State.MapId); break;
                        case GameMode.DefendPoint: gm.StartDefendPoint(gm.State.MapId); break;
                        case GameMode.Escort: gm.StartEscort(gm.State.MapId); break;
                        case GameMode.BossRush: gm.StartBossRush(gm.State.MapId); break;
                        case GameMode.Hardcore: gm.StartHardcore(gm.State.MapId, gm.State.Level); break;
                        case GameMode.DailyChallenge: gm.StartDailyChallenge(); break;
                        default:
                            if (gm.CurrentLevel != null)
                                gm.StartCampaignLevel(gm.CurrentLevel.Map, gm.CurrentLevel.Level);
                            else
                                gm.StartEndless(gm.State.MapId);
                            break;
                    }
                });
            MakeButton(_gameOverPanel.transform, "MENU", new Vector2(0, -210), new Vector2(220, 70),
                () =>
                {
                    var menu = FindFirstObjectByType<MenuController>();
                    if (menu != null) menu.ShowMenu();
                    Hide();
                });
            _gameOverPanel.SetActive(false);
        }

        private void Update()
        {
            if (!_shown) return;
            var gm = GameManager.Instance;
            if (gm == null) return;
            var ps = gm.State.Player;

            _healthFill.fillAmount = ps.MaxHealth > 0 ? (float)ps.Health / ps.MaxHealth : 0f;
            int xpNeed = PerkData.XpForLevel(ps.Level);
            _xpFill.fillAmount = xpNeed > 0 ? (float)ps.Xp / xpNeed : 0f;
            _coinsText.text = $"Coins: {gm.RunCoins}";
            _killsText.text = $"Kills: {gm.Kills}  (x{gm.ScoreMultiplier():0.0})";
            string levelText = gm.CurrentLevel != null
                ? $"Map {gm.CurrentLevel.Map} — L{gm.CurrentLevel.Level}  Wave {gm.LevelDirector.CurrentWaveInLevel}/{gm.CurrentLevel.WaveCount}"
                : $"Endless — Wave {gm.LevelDirector.EndlessWave}";
            _waveText.text = levelText;
            int t = (int)gm.State.TimeSurvivedSec;
            _timerText.text = $"{t / 60}:{t % 60:00}";

            if (gm.Player != null)
            {
                var def = WeaponData.Get(gm.Player.WeaponId);
                _ammoText.text = def.MagSize <= 0
                    ? $"{def.Name} — ∞"
                    : $"{def.Name} — {gm.Player.AmmoInMag}/{gm.EffectiveMagSize(gm.Player.WeaponId)}";
            }

            // Boss bar.
            ZombieAI boss = null;
            foreach (var z in gm.Zombies)
                if (z.IsBoss && z.IsAlive) { boss = z; break; }
            if (boss != null)
            {
                _bossBar.SetActive(true);
                _bossFill.fillAmount = boss.MaxHealth > 0 ? boss.Health / boss.MaxHealth : 0f;
                _bossNameText.text = ZombieData.GetBoss(boss.BossKind.Value).Name;
            }
            else
            {
                _bossBar.SetActive(false);
                _bossNameText.text = "";
            }

            // Perk choices (+ rare gold super-perk offer).
            bool showPerks = gm.PendingPerkChoices.Count > 0 || gm.PendingSuperPerkOffer != null;
            _perkPanel.SetActive(showPerks);
            if (showPerks)
            {
                for (int i = 0; i < 3; i++)
                {
                    bool has = i < gm.PendingPerkChoices.Count;
                    _perkButtons[i].gameObject.SetActive(has);
                    if (has)
                    {
                        var id = gm.PendingPerkChoices[i];
                        var def = PerkData.Get(id);
                        _perkLabels[i].text = $"{def.Name}  (Lv {ps.PerkStacks(id) + 1})\n{def.Description}";
                    }
                }
                bool hasSuper = gm.PendingSuperPerkOffer != null;
                if (_superPerkButton != null) _superPerkButton.gameObject.SetActive(hasSuper);
                if (hasSuper && _superPerkLabel != null)
                {
                    var offer = gm.PendingSuperPerkOffer;
                    _superPerkLabel.text = $"★ SUPER PERK: {offer.Name}\n{offer.Description}";
                }
            }

            // Game over.
            bool gameOver = gm.State.Phase == GamePhase.GameOver;
            _gameOverPanel.SetActive(gameOver);
            if (gameOver)
            {
                int acc = gm.ShotsFired > 0 ? (int)(100f * gm.ShotsHit / gm.ShotsFired) : 0;
                _gameOverText.text = gm.Won ? "LEVEL CLEAR!" : "YOU DIED";
                _gameOverText.text += $"\n\nScore: {gm.Score}\nCoins: {gm.RunCoins}\nKills: {gm.Kills}   Headshots: {gm.Headshots}\nAccuracy: {acc}%";
            }
        }
    }
}
