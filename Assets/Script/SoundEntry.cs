using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(menuName="Audio/SoundEntry")]
public class SoundEntry : ScriptableObject {
    public SoundKey key;
    public AudioClip clip;
    public AudioMixerGroup output;   // SFX or BGM
    [Range(0f, 1f)] public float volume = 1f;
    [Range(-3f, 3f)] public float pitch = 1f;
    public bool loop;
    public float cooldownSec = 0f;   // 連打抑制に使用
    public int maxSimultaneous = 4;  // 同時発音上限（SFX用）
    public bool spatialize2D = false; // 2DならfalseでOK。位置再生したければtrue
    public float spatialBlend = 0f;   // 0=2D, 1=3D
}
