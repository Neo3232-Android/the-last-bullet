using System;
using UnityEngine;

namespace TheLastBullet.Core
{
    /// <summary>
    /// Runtime animation driver for real FBX models.
    ///
    /// No AnimatorController exists (none was authored in the Editor), so this plays
    /// the FBX's own AnimationClips through the legacy Animation component, matched by
    /// name keyword so it survives naming variations (Walk vs Zombie_Walk, Run vs
    /// FreeRun, Crawl). Clips are loaded from the same Resources path as the model
    /// prefab. When no clips are found the model still feels alive via a procedural
    /// bob driven by speed. Attached by ModelLibrary when a real model is instantiated.
    /// Never throws: every load/play is guarded so a bad clip can never break a run.
    /// </summary>
    public sealed class ModelAnimator : MonoBehaviour
    {
        private Animation _anim;
        private AnimationClip _walk, _run, _idle, _death, _attack, _hit, _scream;
        private string _current = "";
        private float _oneShotUntil;
        private bool _hasClips;
        private bool _dead;
        private float _bobPhase;
        private Vector3 _baseLocalPos;
        private bool _baseCaptured;

        public bool HasClips => _hasClips;
        public bool IsDead => _dead;

        /// <summary>Load the model's clips from its Resources path (no extension).</summary>
        public void Init(string resourcesPath)
        {
            if (string.IsNullOrEmpty(resourcesPath)) return;
            try
            {
                var clips = Resources.LoadAll<AnimationClip>(resourcesPath);
                if (clips == null || clips.Length == 0) return;

                _walk = FindClip(clips, "walk");
                if (_walk == null) _walk = FindClip(clips, "crawl"); // crawler
                _run = FindClip(clips, "run");
                _idle = FindClip(clips, "idle");
                _death = FindClip(clips, "death");
                _attack = FindClip(clips, "attack", "bite");
                _hit = FindClip(clips, "hit");
                _scream = FindClip(clips, "scream");

                _anim = gameObject.GetComponent<Animation>();
                if (_anim == null) _anim = gameObject.AddComponent<Animation>();
                _anim.playAutomatically = false;

                AddClip(_walk, "walk", WrapMode.Loop);
                AddClip(_run, "run", WrapMode.Loop);
                AddClip(_idle, "idle", WrapMode.Loop);
                AddClip(_death, "death", WrapMode.Once);
                AddClip(_attack, "attack", WrapMode.Once);
                AddClip(_hit, "hit", WrapMode.Once);
                AddClip(_scream, "scream", WrapMode.Once);

                _hasClips = _walk != null || _run != null || _idle != null;
                if (_hasClips) PlayClip("idle", 0.15f);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[ModelAnimator] clip load failed for " + resourcesPath + ": " + ex.Message);
                _hasClips = false;
            }
        }

        private void AddClip(AnimationClip clip, string key, WrapMode mode)
        {
            if (clip == null || _anim == null) return;
            try
            {
                clip.legacy = true;
                _anim.AddClip(clip, key);
                _anim[key].wrapMode = mode;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[ModelAnimator] AddClip " + key + " failed: " + ex.Message);
            }
        }

        private static AnimationClip FindClip(AnimationClip[] clips, params string[] keywords)
        {
            foreach (var clip in clips)
            {
                if (clip == null || string.IsNullOrEmpty(clip.name)) continue;
                string lower = clip.name.ToLowerInvariant();
                foreach (var kw in keywords)
                {
                    if (lower.Contains(kw)) return clip;
                }
            }
            return null;
        }

        /// <summary>Call every frame with the model's current speed (units/sec).</summary>
        public void SetSpeed(float speed)
        {
            if (_dead) return;
            if (!_baseCaptured)
            {
                _baseLocalPos = transform.localPosition;
                _baseCaptured = true;
            }
            // Procedural bob: keeps even clip-less models alive and adds weight.
            _bobPhase += Time.deltaTime * (2f + speed * 1.6f);
            float bob = Mathf.Sin(_bobPhase * 2f) * 0.02f * Mathf.Clamp01(speed);
            transform.localPosition = _baseLocalPos + new Vector3(0f, bob, 0f);

            if (!_hasClips || _anim == null) return;
            if (Time.time < _oneShotUntil) return; // let a one-shot (attack/hit) finish
            string want = speed < 0.15f ? "idle" : (speed > 2.2f ? "run" : "walk");
            if (want == "run" && _run == null) want = "walk";
            if (want == "walk" && _walk == null) want = "idle";
            if (want == "idle" && _idle == null) return;
            PlayClip(want, 0.2f);
        }

        public void PlayDeath()
        {
            _dead = true;
            if (_hasClips && _death != null) PlayClip("death", 0.1f);
        }

        public void PlayAttack()
        {
            if (_dead || !_hasClips || _attack == null) return;
            _oneShotUntil = Time.time + Mathf.Max(0.25f, _attack.length);
            PlayClip("attack", 0.08f);
        }

        public void PlayHit()
        {
            if (_dead || !_hasClips || _hit == null) return;
            _oneShotUntil = Time.time + Mathf.Max(0.2f, _hit.length);
            PlayClip("hit", 0.05f);
        }

        public void PlayScream()
        {
            if (_dead || !_hasClips || _scream == null) return;
            _oneShotUntil = Time.time + Mathf.Max(0.4f, _scream.length);
            PlayClip("scream", 0.1f);
        }

        private void PlayClip(string key, float fade)
        {
            if (_anim == null) return;
            if (_current == key && _anim.isPlaying) return;
            try
            {
                _anim.CrossFade(key, fade);
                _current = key;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[ModelAnimator] PlayClip " + key + " failed: " + ex.Message);
            }
        }
    }
}
