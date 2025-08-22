using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour {
    public static AudioManager I { get; private set; }

    [Header("Data")]
    [SerializeField] private AudioLibrary library;

    [Header("Pool")]
    [SerializeField] private int sfxPoolSize = 16;
    [SerializeField] private Transform sfxRoot;

    [Header("Mixer (optional but recommended)")]
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private AudioMixerSnapshot defaultSnapshot;
    public const string SFX_VOL = "SFXVolume";
    public const string BGM_VOL = "BGMVolume";

    [Header("BGM")]
    [SerializeField] private AudioSource bgmSource;

    // ---- Runtime ----
    private readonly Queue<AudioSource> _pool = new();
    private readonly Dictionary<SoundKey, int> _playingCount = new();

    // 監視対象（コルーチンが止まっても回収する用）
    [System.Serializable] private class ActiveSfx {
        public AudioSource src;
        public SoundKey key;
        public double deadline; // Time.unscaledTimeAsDouble の期限
    }
    private readonly List<ActiveSfx> _active = new();

    // BGMフェード専用コルーチン
    private Coroutine _bgmCo;

    // インスペクタで見えるデバッグ
    [Header("Debug")]
    [SerializeField] private int poolRemain;
    public int PoolRemain => _pool.Count;

    void Awake() {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        if (sfxRoot == null) sfxRoot = transform;

        // SFXプール生成
        for (int i = 0; i < sfxPoolSize; i++) {
            var go = new GameObject($"SFX_{i}");
            go.transform.SetParent(sfxRoot);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            _pool.Enqueue(src);
        }

        // BGMソース
        if (bgmSource == null) {
            var go = new GameObject("BGM");
            go.transform.SetParent(transform);
            bgmSource = go.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;
            bgmSource.loop = true;
        }

        // Mixer初期適用（任意）
        if (mixer != null) {
            float sfx = PlayerPrefs.GetFloat("vol_sfx", 1f);
            float bgm = PlayerPrefs.GetFloat("vol_bgm", 1f);
            mixer.SetFloat(SFX_VOL, LinearToDb(sfx));
            mixer.SetFloat(BGM_VOL, LinearToDb(bgm));
        }
    }

    void Start() {
        // デフォルトSnapshotへ遷移（任意）
        defaultSnapshot?.TransitionTo(0f);
    }

    void Update() {
        // 監視（ウォッチドッグ）：期限超過 or 再生終了を検出して強制回収
        for (int i = _active.Count - 1; i >= 0; i--) {
            var a = _active[i];
            bool finished = a.src == null || !a.src.isActiveAndEnabled || !a.src.isPlaying;
            bool timeout  = Time.unscaledTimeAsDouble > a.deadline;
            if (finished || timeout) {
                Recycle(a.src, a.key);
                _active.RemoveAt(i);
            }
        }

        // デバッグ表示（任意）
        poolRemain = _pool.Count;
    }

    void OnDisable() => ForceRecycleAll();
    void OnDestroy() => ForceRecycleAll();

    private void ForceRecycleAll() {
        // sfxRoot 配下の AudioSource を全回収
        if (sfxRoot != null) {
            foreach (Transform t in sfxRoot) {
                var src = t.GetComponent<AudioSource>();
                if (src != null && src.clip != null) {
                    SafeStopAndClear(src);
                    if (!_pool.Contains(src)) _pool.Enqueue(src);
                }
            }
        }
        _active.Clear();
        _playingCount.Clear();
    }

    // --------- Public API ---------

    public void PlaySFX(SoundKey key, Vector3? worldPos = null) {
        var se = library.Get(key);
        if (se == null || se.clip == null) {
            Debug.LogWarning($"[Audio] SFX not found: {key}");
            return;
        }

        // クールダウン
        // ※ もし library / AudioManager に cooldown 実装があるならここでチェック
        // 例: if (BlockedByCooldown(key, se.cooldownSec)) return;

        // 同時数制限
        if (se.maxSimultaneous > 0 &&
            _playingCount.TryGetValue(key, out var c) && c >= se.maxSimultaneous) {
            // 古いのを止めて差し替える運用にしたい場合はここで実装
            return;
        }

        if (_pool.Count == 0) {
            Debug.LogWarning("[Audio] SFX pool empty. Increase size or fix recycle.");
            return;
        }

        var src = _pool.Dequeue();
        ApplyToSource(src, se);
        src.loop = false; // SFXは常にfalse（念押し）
        src.transform.position = worldPos ?? Vector3.zero;
        if (!se.spatialize2D) src.spatialBlend = 0f; // 2D前提（必要なら個別に上げる）

        // 期限（ピッチを考慮した実長 + 少しの余裕）
        var length = se.clip.length / Mathf.Max(0.01f, Mathf.Abs(se.pitch));
        _active.Add(new ActiveSfx {
            src = src,
            key = key,
            deadline = Time.unscaledTimeAsDouble + length + 0.5
        });

        StartCoroutine(PlayAndRecycle(src, key));
    }

    public void PlayBGM(SoundKey key, float fadeSec = 0.5f) {
        var se = library.Get(key);
        if (se == null || se.clip == null) return;

        if (_bgmCo != null) StopCoroutine(_bgmCo);
        _bgmCo = StartCoroutine(FadeInBGM(se, fadeSec));
    }

    public void StopBGM(float fadeSec = 0.5f) {
        if (_bgmCo != null) StopCoroutine(_bgmCo);
        _bgmCo = StartCoroutine(FadeOutBGM(fadeSec));
    }

    // --------- Internals ---------

    private void ApplyToSource(AudioSource src, SoundEntry se) {
        src.outputAudioMixerGroup = se.output;
        src.clip = se.clip;
        src.volume = se.volume;
        src.pitch = se.pitch;
        src.spatialBlend = se.spatialize2D ? se.spatialBlend : 0f; // 2Dが基本
    }

    private IEnumerator PlayAndRecycle(AudioSource src, SoundKey key) {
        _playingCount[key] = _playingCount.TryGetValue(key, out var c) ? c + 1 : 1;

        src.Play();

        // 通常は isPlaying が false まで待つ
        while (src != null && src.isActiveAndEnabled && src.isPlaying) {
            yield return null;
        }

        // ここに来たら通常経路で終了（ウォッチドッグが既に回収済みでも二重返却しないよう注意）
        Recycle(src, key);
    }

    private void Recycle(AudioSource src, SoundKey key) {
        if (src == null) return;

        // すでにPoolに戻っていれば何もしない
        // （厳密判定は難しいので、clipの有無で最小限の重複回避）
        if (src.clip == null && src.outputAudioMixerGroup == null) return;

        SafeStopAndClear(src);
        if (!_pool.Contains(src)) _pool.Enqueue(src);

        if (_playingCount.TryGetValue(key, out var cc))
            _playingCount[key] = Mathf.Max(0, cc - 1);
    }

    private void SafeStopAndClear(AudioSource src) {
        src.Stop();
        src.clip = null;
        src.loop = false;
        src.pitch = 1f;
        // MixerGroupを一度外しておくと設定漏れ検知に役立つ（任意）
        src.outputAudioMixerGroup = null;
    }

    private IEnumerator FadeInBGM(SoundEntry se, float sec) {
        // 既存BGMをフェードアウト
        if (bgmSource.isPlaying)
            yield return FadeOutBGM(sec);

        // 新曲を適用してフェードイン
        bgmSource.outputAudioMixerGroup = se.output;
        bgmSource.clip = se.clip;
        bgmSource.loop = true;
        float target = se.volume;
        bgmSource.volume = 0f;
        bgmSource.pitch = se.pitch;
        bgmSource.Play();

        float t = 0f;
        while (t < sec) {
            t += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(0f, target, t / sec);
            yield return null;
        }
        bgmSource.volume = target;
    }

    private IEnumerator FadeOutBGM(float sec) {
        if (!bgmSource.isPlaying) yield break;
        float start = bgmSource.volume;
        float t = 0f;
        while (t < sec) {
            t += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(start, 0f, t / sec);
            yield return null;
        }
        bgmSource.Stop();
    }

    private float LinearToDb(float v) => v <= 0f ? -80f : Mathf.Log10(v) * 20f;
}
