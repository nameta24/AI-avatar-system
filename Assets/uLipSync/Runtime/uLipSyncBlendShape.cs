using UnityEngine;
using System.Collections.Generic;

namespace uLipSync
{

    [ExecuteAlways]
    public class uLipSyncBlendShape : AnimationBakableMonoBehaviour
    {
        [System.Serializable]
        public class BlendShapeInfo
        {
            public string phoneme;
            public int index = -1;
            public float maxWeight = 1f;

            public float weight { get; set; } = 0f;
            public float weightVelocity { get; set; } = 0f;
        }

        // =========================================================================
        // FULL FACE shape library — jaw + lips + cheeks + nose + EYES + brows
        // Every phoneme drives the whole face exactly like a real human speaking.
        // =========================================================================
        private static readonly Dictionary<string, (string shape, float w)[]> ShapesByClass =
            new Dictionary<string, (string, float)[]>
        {
        // ── A  (father, hot) ─────────────────────────────────────────────────
        // Big open jaw, eyes widen with effort, brows shoot up
        { "A", new[] {
            ("jawOpen",              0.20f),
            ("mouthShrugLower",      0.28f),
            ("mouthShrugUpper",      0.12f),
            ("mouthSmileLeft",       0.08f),
            ("mouthSmileRight",      0.08f),
            ("cheekPuff",            0.06f),
            ("noseSneerLeft",        0.05f),
            ("noseSneerRight",       0.05f),
            ("cheekSquintLeft",      0.14f),
            ("cheekSquintRight",     0.14f),
            ("browInnerUp",          0.14f),
            ("browOuterUpLeft",      0.10f),
            ("browOuterUpRight",     0.10f),
            ("eyeWideLeft",          0.08f),
            ("eyeWideRight",         0.08f),
            ("eyeSquintLeft",        0.08f),
            ("eyeSquintRight",       0.08f),
        }},

        // ── I  (see, feet) ───────────────────────────────────────────────────
        // Smile lifts cheeks, squints eyes naturally — like "cheese"
        { "I", new[] {
            ("jawOpen",              0.14f),
            ("mouthSmileLeft",       0.38f),
            ("mouthSmileRight",      0.38f),
            ("mouthStretchLeft",     0.18f),
            ("mouthStretchRight",    0.18f),
            ("mouthShrugUpper",      0.08f),
            ("mouthUpperUpLeft",     0.14f),
            ("mouthUpperUpRight",    0.14f),
            ("mouthLowerDownLeft",   0.22f),
            ("mouthLowerDownRight",  0.22f),
            ("cheekSquintLeft",      0.20f),
            ("cheekSquintRight",     0.20f),
            ("eyeSquintLeft",        0.08f),
            ("eyeSquintRight",       0.08f),
            ("browOuterUpLeft",      0.10f),
            ("browOuterUpRight",     0.10f),
        }},

        // ── U  (you, boot) ───────────────────────────────────────────────────
        // Pucker + concentration — brows furrow, eyes narrow
        { "U", new[] {
            ("jawOpen",              0.16f),
            ("mouthPucker",          0.45f),
            ("mouthFunnel",          0.35f),
            ("mouthRollLower",       0.16f),
            ("mouthRollUpper",       0.16f),
            ("cheekSquintLeft",      0.10f),
            ("cheekSquintRight",     0.10f),
            ("browDownLeft",         0.12f),
            ("browDownRight",        0.12f),
            ("browInnerUp",          0.08f),
            ("eyeSquintLeft",        0.10f),
            ("eyeSquintRight",       0.10f),
            ("eyeLookInLeft",        0.05f),
            ("eyeLookInRight",       0.05f),
        }},

        // ── E  (bed, said) ───────────────────────────────────────────────────
        // Open smile, eyes bright and open, cheeks lift
        { "E", new[] {
            ("jawOpen",              0.13f),
            ("mouthSmileLeft",       0.26f),
            ("mouthSmileRight",      0.26f),
            ("mouthStretchLeft",     0.14f),
            ("mouthStretchRight",    0.14f),
            ("mouthShrugUpper",      0.14f),
            ("mouthUpperUpLeft",     0.10f),
            ("mouthUpperUpRight",    0.10f),
            ("cheekSquintLeft",      0.14f),
            ("cheekSquintRight",     0.14f),
            ("browInnerUp",          0.10f),
            ("browOuterUpLeft",      0.08f),
            ("browOuterUpRight",     0.08f),
            ("eyeWideLeft",          0.06f),
            ("eyeWideRight",         0.06f),
            ("eyeSquintLeft",        0.06f),
            ("eyeSquintRight",       0.06f),
        }},

        // ── O  (go, show) ────────────────────────────────────────────────────
        // Round open mouth, slight eye widening, brows up
        { "O", new[] {
            ("jawOpen",              0.28f),
            ("mouthFunnel",          0.40f),
            ("mouthPucker",          0.18f),
            ("mouthShrugLower",      0.14f),
            ("cheekSquintLeft",      0.08f),
            ("cheekSquintRight",     0.08f),
            ("jawForward",           0.08f),
            ("browInnerUp",          0.12f),
            ("browOuterUpLeft",      0.10f),
            ("browOuterUpRight",     0.10f),
            ("eyeWideLeft",          0.08f),
            ("eyeWideRight",         0.08f),
        }},

        // ── PP  (p, b, m) — bilabial ─────────────────────────────────────────
        // Full lip press — tight mouth, slight brow concentration
        { "PP", new[] {
            ("mouthClose",           0.50f),
            ("mouthPressLeft",       0.42f),
            ("mouthPressRight",      0.42f),
            ("mouthRollLower",       0.22f),
            ("mouthRollUpper",       0.22f),
            ("jawOpen",              0.02f),
            ("browDownLeft",         0.10f),
            ("browDownRight",        0.10f),
            ("eyeSquintLeft",        0.06f),
            ("eyeSquintRight",       0.06f),
        }},

        // ── FF  (f, v) — labiodental ─────────────────────────────────────────
        // Lower lip to teeth, nose sneer, eyes focus with slight squint
        { "FF", new[] {
            ("mouthLowerDownLeft",   0.45f),
            ("mouthLowerDownRight",  0.45f),
            ("mouthUpperUpLeft",     0.28f),
            ("mouthUpperUpRight",    0.28f),
            ("mouthClose",           0.28f),
            ("jawOpen",              0.15f),
            ("noseSneerLeft",        0.20f),
            ("noseSneerRight",       0.20f),
            ("browDownLeft",         0.14f),
            ("browDownRight",        0.14f),
            ("eyeSquintLeft",        0.14f),
            ("eyeSquintRight",       0.14f),
            ("eyeLookDownLeft",      0.08f),
            ("eyeLookDownRight",     0.08f),
        }},

        // ── TH  (think, this) — dental ───────────────────────────────────────
        // Tongue tip, concentration, eyes steady and slightly narrowed
        { "TH", new[] {
            ("jawOpen",              0.20f),
            ("mouthClose",           0.14f),
            ("mouthLowerDownLeft",   0.28f),
            ("mouthLowerDownRight",  0.28f),
            ("mouthStretchLeft",     0.10f),
            ("mouthStretchRight",    0.10f),
            ("browDownLeft",         0.10f),
            ("browDownRight",        0.10f),
            ("eyeSquintLeft",        0.06f),
            ("eyeSquintRight",       0.06f),
            ("eyeLookOutLeft",       0.04f),
            ("eyeLookOutRight",      0.04f),
        }},

        // ── DD  (t, d) — alveolar stop ───────────────────────────────────────
        // Crisp stop — tongue to palate, slight brow pull
        { "DD", new[] {
            ("jawOpen",              0.18f),
            ("mouthClose",           0.44f),
            ("mouthUpperUpLeft",     0.16f),
            ("mouthUpperUpRight",    0.16f),
            ("mouthLowerDownLeft",   0.26f),
            ("mouthLowerDownRight",  0.26f),
            ("browDownLeft",         0.08f),
            ("browDownRight",        0.08f),
        }},

        // ── N  (n, nasal) ────────────────────────────────────────────────────
        // Lips closed, nasal, eyes drift down softly and relax
        { "N", new[] {
            ("jawOpen",              0.15f),
            ("mouthClose",           0.50f),
            ("mouthRollLower",       0.25f),
            ("mouthPressLeft",       0.20f),
            ("mouthPressRight",      0.20f),
            ("noseSneerLeft",        0.12f),
            ("noseSneerRight",       0.12f),
            ("browDownLeft",         0.10f),
            ("browDownRight",        0.10f),
            ("eyeSquintLeft",        0.12f),
            ("eyeSquintRight",       0.12f),
            ("eyeLookDownLeft",      0.10f),
            ("eyeLookDownRight",     0.10f),
        }},

        // ── SS  (s, z) — sibilant ────────────────────────────────────────────
        // Teeth show, sharp focus, eyes squint like aiming
        { "SS", new[] {
            ("jawOpen",              0.18f),
            ("mouthSmileLeft",       0.25f),
            ("mouthSmileRight",      0.25f),
            ("mouthUpperUpLeft",     0.25f),
            ("mouthUpperUpRight",    0.25f),
            ("mouthLowerDownLeft",   0.20f),
            ("mouthLowerDownRight",  0.20f),
            ("mouthStretchLeft",     0.14f),
            ("mouthStretchRight",    0.14f),
            ("cheekSquintLeft",      0.15f),
            ("cheekSquintRight",     0.15f),
            ("eyeSquintLeft",        0.08f),
            ("eyeSquintRight",       0.08f),
            ("browDownLeft",         0.12f),
            ("browDownRight",        0.12f),
        }},

        // ── CH  (ch, sh, j) — postalveolar ───────────────────────────────────
        // Lips round and protrude, eyes squint with effort
        { "CH", new[] {
            ("jawOpen",              0.24f),
            ("mouthFunnel",          0.38f),
            ("mouthPucker",          0.28f),
            ("mouthUpperUpLeft",     0.20f),
            ("mouthUpperUpRight",    0.20f),
            ("mouthLowerDownLeft",   0.16f),
            ("mouthLowerDownRight",  0.16f),
            ("cheekSquintLeft",      0.20f),
            ("cheekSquintRight",     0.20f),
            ("eyeSquintLeft",        0.16f),
            ("eyeSquintRight",       0.16f),
            ("browDownLeft",         0.12f),
            ("browDownRight",        0.12f),
        }},

        // ── KK  (k, g) — velar ───────────────────────────────────────────────
        // Back-of-mouth effort, slight brow raise
        { "KK", new[] {
            ("jawOpen",              0.14f),
            ("mouthClose",           0.16f),
            ("mouthShrugLower",      0.18f),
            ("mouthShrugUpper",      0.08f),
            ("browDownLeft",         0.10f),
            ("browDownRight",        0.10f),
            ("browInnerUp",          0.08f),
            ("eyeSquintLeft",        0.06f),
            ("eyeSquintRight",       0.06f),
        }},

        // ── RR  (r) — rhotic ─────────────────────────────────────────────────
        // Lip funnel + rolls, concentration brow, eyes slightly narrow
        { "RR", new[] {
            ("jawOpen",              0.20f),
            ("mouthFunnel",          0.26f),
            ("mouthRollLower",       0.32f),
            ("mouthRollUpper",       0.24f),
            ("mouthPucker",          0.14f),
            ("cheekSquintLeft",      0.08f),
            ("cheekSquintRight",     0.08f),
            ("browDownLeft",         0.10f),
            ("browDownRight",        0.10f),
            ("eyeSquintLeft",        0.06f),
            ("eyeSquintRight",       0.06f),
        }},

        // ── L  (l) — lateral ─────────────────────────────────────────────────
        // Tongue tip up, upper lip raises, eyes neutral
        { "L", new[] {
            ("jawOpen",              0.18f),
            ("mouthUpperUpLeft",     0.20f),
            ("mouthUpperUpRight",    0.20f),
            ("mouthSmileLeft",       0.12f),
            ("mouthSmileRight",      0.12f),
            ("mouthClose",           0.08f),
            ("eyeSquintLeft",        0.04f),
            ("eyeSquintRight",       0.04f),
            ("browOuterUpLeft",      0.06f),
            ("browOuterUpRight",     0.06f),
        }},

        // ── SIL  (silence) ───────────────────────────────────────────────────
        // Everything returns to zero
        { "SIL", new (string, float)[0] },
        };

        // Maps every phoneme name (case-insensitive) → class key
        private static readonly Dictionary<string, string> PhonemeToClass =
            new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
        {
        { "A",   "A"   }, { "aa",  "A"   }, { "ah",  "A"   }, { "ae",  "A"   },
        { "I",   "I"   }, { "ih",  "I"   }, { "iy",  "I"   },
        { "U",   "U"   }, { "ou",  "U"   }, { "uw",  "U"   }, { "oo",  "U"   },
        { "E",   "E"   }, { "eh",  "E"   }, { "ey",  "E"   },
        { "O",   "O"   }, { "oh",  "O"   }, { "ow",  "O"   }, { "ao",  "O"   }, { "aw", "O" },
        { "PP",  "PP"  }, { "P",   "PP"  }, { "B",   "PP"  }, { "M",   "PP"  },
        { "FF",  "FF"  }, { "F",   "FF"  }, { "V",   "FF"  },
        { "TH",  "TH"  }, { "dh",  "TH"  },
        { "DD",  "DD"  }, { "T",   "DD"  }, { "D",   "DD"  },
        { "N",   "N"   }, { "nn",  "N"   }, { "NG",  "N"   },
        { "SS",  "SS"  }, { "S",   "SS"  }, { "Z",   "SS"  },
        { "CH",  "CH"  }, { "SH",  "CH"  }, { "ZH",  "CH"  }, { "JH",  "CH"  },
        { "kk",  "KK"  }, { "K",   "KK"  }, { "G",   "KK"  },
        { "RR",  "RR"  }, { "R",   "RR"  }, { "er",  "RR"  },
        { "L",   "L"   },
        { "sil", "SIL" }, { "-",   "SIL" }, { "_",   "SIL" },
        };

        // =========================================================================

        public UpdateMethod updateMethod = UpdateMethod.LateUpdate;
        public SkinnedMeshRenderer skinnedMeshRenderer;
        public List<BlendShapeInfo> blendShapes = new List<BlendShapeInfo>();
        public float maxBlendShapeValue = 100f;
        public float minVolume = -2.3f;
        public float maxVolume = -1.5f;
        [Range(0f, 0.3f)] public float smoothness = 0.07f;
        public bool usePhonemeBlend = true;

        LipSyncInfo _info = new LipSyncInfo();
        bool _lipSyncUpdated = false;
        float _volume = 0f;
        float _openCloseVelocity = 0f;
        protected float volume => _volume;

        // ── Auto-blink system ─────────────────────────────────────────────────────
        [Header("Auto Blink")]
        public bool autoBlinkEnabled = true;
        [Range(0.5f, 8f)] public float blinkIntervalMin = 1.5f;
        [Range(1f, 15f)] public float blinkIntervalMax = 6.0f;
        [Range(0.05f, 0.2f)] public float blinkSpeed = 0.10f;

        int _blinkIndexLeft = -1;
        int _blinkIndexRight = -1;
        float _blinkTimer = 0f;
        float _blinkInterval = 3f;
        float _blinkWeight = 0f;
        float _blinkVelocity = 0f;
        bool _isBlinking = false;
        float _blinkPhase = 0f;   // 0=open→close, 1=close→open

#if UNITY_EDITOR
        bool _isAnimationBaking = false;
        float _animBakeDeltaTime = 1f / 60;
#endif

        // ── Auto-populate ─────────────────────────────────────────────────────────

        [ContextMenu("Auto-Populate Blend Shape Table")]
        public void AutoPopulateBlendShapeTable()
        {
            if (!skinnedMeshRenderer)
            {
                Debug.LogWarning("[uLipSync] Assign a SkinnedMeshRenderer first.");
                return;
            }

            var mesh = skinnedMeshRenderer.sharedMesh;
            if (mesh == null)
            {
                Debug.LogWarning("[uLipSync] SkinnedMeshRenderer has no mesh.");
                return;
            }

            // 1. Read phoneme names from Profile
            string[] phonemeNames = null;
            var lipSync = GetComponent<uLipSync>();
            if (lipSync != null && lipSync.profile != null)
            {
                phonemeNames = lipSync.profile.GetPhonemeNames();
                Debug.Log($"[uLipSync] Profile '{lipSync.profile.name}' phonemes: {string.Join(", ", phonemeNames)}");
            }
            else
            {
                Debug.LogWarning("[uLipSync] No Profile found on uLipSync component — using fallback list.");
                phonemeNames = new string[]
                {
                "A","I","U","E","O","N",
                "aa","ih","ou","oh",
                "PP","FF","TH","DD","nn","SS","CH","kk","RR","L","sil","-"
                };
            }

            // 2. Mesh blendshape name → index
            var nameToIndex = new Dictionary<string, int>();
            for (int i = 0; i < mesh.blendShapeCount; i++)
                nameToIndex[mesh.GetBlendShapeName(i)] = i;

            // 3. Fill
            blendShapes.Clear();
            int added = 0;
            var unmatched = new List<string>();

            foreach (string phoneme in phonemeNames)
            {
                if (string.IsNullOrEmpty(phoneme)) continue;

                if (!PhonemeToClass.TryGetValue(phoneme, out string cls))
                {
                    unmatched.Add(phoneme);
                    continue;
                }

                if (!ShapesByClass.TryGetValue(cls, out var shapes) || shapes.Length == 0)
                    continue;

                foreach (var (shapeName, maxW) in shapes)
                {
                    if (!nameToIndex.TryGetValue(shapeName, out int idx))
                        continue;

                    blendShapes.Add(new BlendShapeInfo
                    {
                        phoneme = phoneme,
                        index = idx,
                        maxWeight = maxW,
                    });
                    added++;
                }
            }

            if (unmatched.Count > 0)
                Debug.LogWarning($"[uLipSync] Unknown phonemes (skipped): {string.Join(", ", unmatched)}");

            Debug.Log($"[uLipSync] Done — {added} entries added across {phonemeNames.Length} phonemes.");

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        void Start()
        {
            if (blendShapes.Count == 0 && skinnedMeshRenderer != null)
                AutoPopulateBlendShapeTable();
            InitBlink();
        }

        void InitBlink()
        {
            if (!skinnedMeshRenderer) return;
            var mesh = skinnedMeshRenderer.sharedMesh;
            if (mesh == null) return;
            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                string n = mesh.GetBlendShapeName(i);
                if (n == "eyeBlinkLeft") _blinkIndexLeft = i;
                if (n == "eyeBlinkRight") _blinkIndexRight = i;
            }
            _blinkInterval = Random.Range(blinkIntervalMin, blinkIntervalMax);
            _blinkTimer = _blinkInterval * 0.5f; // first blink sooner
        }

        // ── Core logic ────────────────────────────────────────────────────────────

        void UpdateLipSync()
        {
            UpdateVolume();
            UpdateVowels();
            _lipSyncUpdated = false;
        }

        void UpdateBlink()
        {
            if (!autoBlinkEnabled) return;
            if (_blinkIndexLeft < 0 && _blinkIndexRight < 0) return;

            _blinkTimer -= Time.deltaTime;

            if (!_isBlinking && _blinkTimer <= 0f)
            {
                _isBlinking = true;
                _blinkPhase = 0f;
            }

            if (_isBlinking)
            {
                // Phase 0: close (fast), Phase 1: open (slightly slower)
                float speed = (_blinkPhase == 0f) ? 1f / blinkSpeed : 0.7f / blinkSpeed;
                if (_blinkPhase == 0f)
                {
                    _blinkWeight = Mathf.MoveTowards(_blinkWeight, 1f, speed * Time.deltaTime);
                    if (_blinkWeight >= 0.99f) _blinkPhase = 1f;
                }
                else
                {
                    _blinkWeight = Mathf.MoveTowards(_blinkWeight, 0f, speed * Time.deltaTime);
                    if (_blinkWeight <= 0.01f)
                    {
                        _blinkWeight = 0f;
                        _isBlinking = false;
                        _blinkInterval = Random.Range(blinkIntervalMin, blinkIntervalMax);
                        _blinkTimer = _blinkInterval;
                    }
                }
            }

            float val = _blinkWeight * maxBlendShapeValue;
            if (_blinkIndexLeft >= 0) skinnedMeshRenderer.SetBlendShapeWeight(_blinkIndexLeft, val);
            if (_blinkIndexRight >= 0) skinnedMeshRenderer.SetBlendShapeWeight(_blinkIndexRight, val);
        }

        public void OnLipSyncUpdate(LipSyncInfo info)
        {
            _info = info;
            _lipSyncUpdated = true;
            if (updateMethod == UpdateMethod.LipSyncUpdateEvent)
            {
                UpdateLipSync();
                OnApplyBlendShapes();
            }
        }

        void Update()
        {
#if UNITY_EDITOR
            if (_isAnimationBaking) return;
#endif
            if (updateMethod != UpdateMethod.LipSyncUpdateEvent)
                UpdateLipSync();

            if (updateMethod == UpdateMethod.Update)
                OnApplyBlendShapes();
        }

        void LateUpdate()
        {
#if UNITY_EDITOR
            if (_isAnimationBaking) return;
#endif
            if (updateMethod == UpdateMethod.LateUpdate)
            {
                OnApplyBlendShapes();
                if (Application.isPlaying) UpdateBlink();
            }
        }

        void FixedUpdate()
        {
#if UNITY_EDITOR
            if (_isAnimationBaking) return;
#endif
            if (updateMethod == UpdateMethod.FixedUpdate)
                OnApplyBlendShapes();
        }

        float SmoothDamp(float value, float target, ref float velocity)
        {
#if UNITY_EDITOR
            if (_isAnimationBaking)
                return Mathf.SmoothDamp(value, target, ref velocity, smoothness, Mathf.Infinity, _animBakeDeltaTime);
#endif
            return Mathf.SmoothDamp(value, target, ref velocity, smoothness);
        }

        void UpdateVolume()
        {
            float normVol = 0f;
            if (_lipSyncUpdated && _info.rawVolume > 0f)
            {
                normVol = Mathf.Log10(_info.rawVolume);
                normVol = (normVol - minVolume) / Mathf.Max(maxVolume - minVolume, 1e-4f);
                normVol = Mathf.Clamp(normVol, 0f, 1f);
            }
            _volume = SmoothDamp(_volume, normVol, ref _openCloseVelocity);
        }

        void UpdateVowels()
        {
            var ratios = _info.phonemeRatios;

            var targets = new float[blendShapes.Count];
            for (int i = 0; i < blendShapes.Count; i++)
            {
                var bs = blendShapes[i];
                float targetWeight = 0f;
                if (usePhonemeBlend)
                {
                    if (ratios != null && !string.IsNullOrEmpty(bs.phoneme))
                        ratios.TryGetValue(bs.phoneme, out targetWeight);
                }
                else
                {
                    targetWeight = (bs.phoneme == _info.phoneme) ? 1f : 0f;
                }
                targets[i] = targetWeight;
            }

            for (int i = 0; i < blendShapes.Count; i++)
            {
                var bs = blendShapes[i];
                float vel = bs.weightVelocity;
                bs.weight = SmoothDamp(bs.weight, targets[i], ref vel);
                bs.weightVelocity = vel;
            }

            // No global normalisation — individual maxWeight values are already tuned.
            // Clamping each weight prevents overflow without suppressing expression.
            foreach (var bs in blendShapes)
                bs.weight = Mathf.Clamp01(bs.weight);
        }

        public void ApplyBlendShapes()
        {
            if (updateMethod == UpdateMethod.External)
                OnApplyBlendShapes();
        }

        protected virtual void OnApplyBlendShapes()
        {
            if (!skinnedMeshRenderer) return;

            // Accumulate the MAXIMUM contribution per blend shape index.
            // Critical: multiple phoneme entries share the same blend shape (e.g.
            // jawOpen appears in A, I, U, O, RR, L, TH, DD...). Summing them causes
            // massive over-exaggeration. Taking the MAX means only the dominant
            // phoneme drives each shape — exactly how a real face works.
            var accum = new Dictionary<int, float>();
            foreach (var bs in blendShapes)
            {
                if (bs.index < 0) continue;

                // Gentle power curve: slightly boosts mid-range expression
                // while softening the very top end to avoid cartoon extremes.
                float w = Mathf.Pow(Mathf.Clamp01(bs.weight), 0.85f);
                float val = w * bs.maxWeight * volume * maxBlendShapeValue;

                if (accum.TryGetValue(bs.index, out float existing))
                    accum[bs.index] = Mathf.Max(existing, val);
                else
                    accum[bs.index] = val;
            }

            // Zero every involved index first, then apply the single accumulated value.
            foreach (var kv in accum)
                skinnedMeshRenderer.SetBlendShapeWeight(kv.Key, 0f);
            foreach (var kv in accum)
                skinnedMeshRenderer.SetBlendShapeWeight(
                    kv.Key, Mathf.Clamp(kv.Value, 0f, maxBlendShapeValue));
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        public BlendShapeInfo GetBlendShapeInfo(string phoneme)
        {
            foreach (var info in blendShapes)
                if (info.phoneme == phoneme) return info;
            return null;
        }

        public BlendShapeInfo AddBlendShape(string phoneme, string blendShape)
        {
            var bs = GetBlendShapeInfo(phoneme);
            if (bs == null) bs = new BlendShapeInfo() { phoneme = phoneme };

            blendShapes.Add(bs);

            if (!skinnedMeshRenderer) return bs;
            bs.index = Util.GetBlendShapeIndex(skinnedMeshRenderer, blendShape);

            return bs;
        }

        // ── Animation baking ──────────────────────────────────────────────────────

#if UNITY_EDITOR
        public override GameObject target => skinnedMeshRenderer?.gameObject;

        public override List<string> GetPropertyNames()
        {
            var names = new List<string>();
            var mesh = skinnedMeshRenderer.sharedMesh;
            var seen = new HashSet<int>();
            foreach (var bs in blendShapes)
            {
                if (bs.index < 0 || seen.Contains(bs.index)) continue;
                seen.Add(bs.index);
                names.Add("blendShape." + mesh.GetBlendShapeName(bs.index));
            }
            return names;
        }

        public override List<float> GetPropertyWeights()
        {
            var accum = new Dictionary<int, float>();
            foreach (var bs in blendShapes)
            {
                if (bs.index < 0) continue;
                float w = Mathf.Pow(Mathf.Clamp01(bs.weight), 0.85f);
                float add = w * bs.maxWeight * volume * maxBlendShapeValue;
                if (accum.ContainsKey(bs.index))
                    accum[bs.index] = Mathf.Max(accum[bs.index], add);
                else
                    accum[bs.index] = add;
            }

            var weights = new List<float>();
            var used = new HashSet<int>();
            foreach (var bs in blendShapes)
            {
                if (bs.index < 0 || used.Contains(bs.index)) continue;
                used.Add(bs.index);
                weights.Add(accum[bs.index]);
            }
            return weights;
        }

        public override float maxWeight => 100f;
        public override float minWeight => 0f;

        public override void OnAnimationBakeStart() => _isAnimationBaking = true;
        public override void OnAnimationBakeEnd() => _isAnimationBaking = false;

        public override void OnAnimationBakeUpdate(LipSyncInfo info, float dt)
        {
            _info = info;
            _animBakeDeltaTime = dt;
            _lipSyncUpdated = true;
            UpdateLipSync();
        }
#endif
    }

}