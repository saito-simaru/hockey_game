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
    [Header("BGM")]
    [SerializeField] private AudioSource bgmSource;

    private readonly Queue<AudioSource> _pool = new();
    private readonly Dictionary<SoundKey, float> _lastPlayed = new();
    private readonly Dictionary<SoundKey, int> _playingCount = new();

    void Awake() {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
        if (sfxRoot == null) sfxRoot = transform;
        // プール生成
        for (int i=0; i<sfxPoolSize; i++) {
            var go = new GameObject($"SFX_{i}");
            go.transform.SetParent(sfxRoot);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            _pool.Enqueue(src);
        }
        if (bgmSource == null) {
            var go = new GameObject("BGM");
            go.transform.SetParent(transform);
            bgmSource = go.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
        }
    }

    // --- Public API ---
    public void PlaySFX(SoundKey key, Vector3? worldPos = null) {
        var se = library.Get(key);
        if (se == null || se.clip == null) return;

        // クールダウン
        if (se.cooldownSec > 0 && 
            _lastPlayed.TryGetValue(key, out var t) && Time.unscaledTime - t < se.cooldownSec)
            return;

        // 同時数制限
        if (se.maxSimultaneous > 0 && 
            _playingCount.TryGetValue(key, out var c) && c >= se.maxSimultaneous)
            return;

        if (_pool.Count == 0) return; // 足りなければ諦める（必要なら拡張）
        var src = _pool.Dequeue();

        ApplyToSource(src, se);
        src.transform.position = worldPos ?? Vector3.zero;
        src.spatialBlend = se.spatialize2D ? se.spatialBlend : 0f;
        src.loop = false;

        StartCoroutine(PlayAndRecycle(src, key));
        _lastPlayed[key] = Time.unscaledTime;
    }

    public void PlayBGM(SoundKey key, float fadeSec = 0.5f) {
        var se = library.Get(key);
        if (se == null || se.clip == null) return;

        StopAllCoroutines(); // 古いフェードを殺す（必要ならBGM専用コルーチン管理）
        StartCoroutine(FadeInBGM(se, fadeSec));
    }

    public void StopBGM(float fadeSec = 0.5f) {
        StartCoroutine(FadeOutBGM(fadeSec));
    }

    // --- Internal helpers ---
    private void ApplyToSource(AudioSource src, SoundEntry se) {
        src.outputAudioMixerGroup = se.output;
        src.clip = se.clip;
        src.volume = se.volume;
        src.pitch = se.pitch;
    }

    private IEnumerator PlayAndRecycle(AudioSource src, SoundKey key) {
        _playingCount[key] = _playingCount.TryGetValue(key, out var c) ? c+1 : 1;

        src.Play();
        yield return new WaitWhile(() => src.isPlaying);
        src.clip = null;
        _pool.Enqueue(src);

        _playingCount[key]--;
    }

    private IEnumerator FadeInBGM(SoundEntry se, float sec) {
        if (bgmSource.isPlaying) yield return FadeOutBGM(sec);
        ApplyToSource(bgmSource, se);
        bgmSource.loop = true;
        float t = 0f;
        float target = se.volume;
        bgmSource.volume = 0f;
        bgmSource.Play();
        while (t < sec) {
            t += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(0f, target, t / sec);
            yield return null;
        }
        bgmSource.volume = target;
    }

    private IEnumerator FadeOutBGM(float sec) {
        if (!bgmSource.isPlaying) yield break;
        float t = 0f;
        float start = bgmSource.volume;
        while (t < sec) {
            t += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(start, 0f, t / sec);
            yield return null;
        }
        bgmSource.Stop();
    }
}
