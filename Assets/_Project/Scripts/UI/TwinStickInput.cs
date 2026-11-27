using UnityEngine;
using UnityEngine.UI;
using TheLastBullet.Core;
using TheLastBullet.Data;

namespace TheLastBullet.UI
{
    /// <summary>
    /// Twin-stick touch input, hand-rolled. Left half of the screen = move
    /// stick, right half = aim/fire stick. Each stick anchors where the finger
    /// lands and tracks that finger until lift. Mouse input works in the
    /// Editor (left button = move, right button = aim) so the game can be
    /// played without a device.
    ///
    /// Built entirely in code (Canvas + two knob Images) — no serialized
    /// .inputactions asset, no prefab references — so the scene stays
    /// headless-safe and the first Actions build can't fail on a missing asset.
    /// PlayerController reads MoveInput / AimInput / Firing every FixedUpdate.
    /// </summary>
    public sealed class TwinStickInput : MonoBehaviour
    {
        public static TwinStickInput Instance { get; private set; }

        public Vector2 MoveInput { get; private set; }
        public Vector2 AimInput { get; private set; }
        public bool Firing { get; private set; }

        private const float StickRadiusPx = 90f;

        private Canvas _canvas;
        private Image _moveBase, _moveKnob, _aimBase, _aimKnob;
        private int _moveFingerId = -1;
        private int _aimFingerId = -1;
        private Vector2 _moveAnchor;
        private Vector2 _aimAnchor;
        private Vector2 _moveCurrent;
        private Vector2 _aimCurrent;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildVisuals();
        }

        private void BuildVisuals()
        {
            var canvasGo = new GameObject("TwinStickCanvas");
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 10;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();

            _moveBase = MakeCircle(canvasGo.transform, new Color(1f, 1f, 1f, 0.10f), 220f);
            _moveKnob = MakeCircle(canvasGo.transform, new Color(1f, 0.84f, 0f, 0.55f), 110f);
            _aimBase = MakeCircle(canvasGo.transform, new Color(1f, 1f, 1f, 0.10f), 220f);
            _aimKnob = MakeCircle(canvasGo.transform, new Color(1f, 0.25f, 0.25f, 0.55f), 110f);
            SetVisible(_moveBase, false); SetVisible(_aimBase, false);
        }

        private static Image MakeCircle(Transform parent, Color color, float size)
        {
            var go = new GameObject("Stick");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.sizeDelta = new Vector2(size, size);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
            return img;
        }

        private static void SetVisible(Image img, bool visible) => img.gameObject.SetActive(visible);

        private void Update()
        {
            var gm = GameManager.Instance;
            bool playing = gm != null && gm.State.Phase == GamePhase.Playing;
            if (!playing)
            {
                MoveInput = Vector2.zero; AimInput = Vector2.zero; Firing = false;
                SetVisible(_moveBase, false); SetVisible(_aimBase, false);
                _moveFingerId = _aimFingerId = -1;
                return;
            }

            // Touch input.
            foreach (var touch in Input.touches)
            {
                HandleTouch(touch.fingerId, touch.position, touch.phase);
            }

            // Mouse fallback (Editor / standalone): left = move, right = aim.
            if (Input.touches.Length == 0)
            {
                if (Input.GetMouseButtonDown(0)) BeginStick(true, Input.mousePosition);
                if (Input.GetMouseButtonDown(1)) BeginStick(false, Input.mousePosition);
                if (Input.GetMouseButton(0)) DragStick(true, Input.mousePosition);
                if (Input.GetMouseButton(1)) DragStick(false, Input.mousePosition);
                if (Input.GetMouseButtonUp(0)) EndStick(true);
                if (Input.GetMouseButtonUp(1)) EndStick(false);
            }

            UpdateVisuals();
        }

        private void HandleTouch(int fingerId, Vector2 pos, TouchPhase phase)
        {
            switch (phase)
            {
                case TouchPhase.Began:
                    BeginStick(pos.x < Screen.width / 2f, pos, fingerId);
                    break;
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    if (fingerId == _moveFingerId) DragStick(true, pos);
                    else if (fingerId == _aimFingerId) DragStick(false, pos);
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (fingerId == _moveFingerId) EndStick(true);
                    else if (fingerId == _aimFingerId) EndStick(false);
                    break;
            }
        }

        private void BeginStick(bool isMove, Vector2 pos, int fingerId = -2)
        {
            if (isMove)
            {
                if (_moveFingerId != -1) return;
                _moveFingerId = fingerId;
                _moveAnchor = pos;
                _moveCurrent = pos;
                SetVisible(_moveBase, true);
            }
            else
            {
                if (_aimFingerId != -1) return;
                _aimFingerId = fingerId;
                _aimAnchor = pos;
                _aimCurrent = pos;
                SetVisible(_aimBase, true);
            }
        }

        private void DragStick(bool isMove, Vector2 pos)
        {
            if (isMove && _moveFingerId != -1)
            {
                _moveCurrent = pos;
                Vector2 delta = pos - _moveAnchor;
                if (delta.magnitude > StickRadiusPx) delta = delta.normalized * StickRadiusPx;
                MoveInput = delta / StickRadiusPx;
            }
            else if (!isMove && _aimFingerId != -1)
            {
                _aimCurrent = pos;
                Vector2 delta = pos - _aimAnchor;
                if (delta.magnitude > StickRadiusPx) delta = delta.normalized * StickRadiusPx;
                AimInput = delta / StickRadiusPx;
                Firing = true;
            }
        }

        private void EndStick(bool isMove)
        {
            if (isMove)
            {
                _moveFingerId = -1;
                MoveInput = Vector2.zero;
                SetVisible(_moveBase, false);
            }
            else
            {
                _aimFingerId = -1;
                AimInput = Vector2.zero;
                Firing = false;
                SetVisible(_aimBase, false);
            }
        }

        private void UpdateVisuals()
        {
            if (_moveFingerId != -1)
            {
                _moveBase.rectTransform.position = _moveAnchor;
                _moveKnob.rectTransform.position = _moveAnchor + MoveInput * StickRadiusPx;
                _moveKnob.gameObject.SetActive(true);
            }
            else _moveKnob.gameObject.SetActive(false);

            if (_aimFingerId != -1)
            {
                _aimBase.rectTransform.position = _aimAnchor;
                _aimKnob.rectTransform.position = _aimAnchor + AimInput * StickRadiusPx;
                _aimKnob.gameObject.SetActive(true);
            }
            else _aimKnob.gameObject.SetActive(false);
        }
    }
}
