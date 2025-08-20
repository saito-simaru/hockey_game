using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName="Audio/AudioLibrary")]
public class AudioLibrary : ScriptableObject {
    public List<SoundEntry> entries;
    private Dictionary<SoundKey, SoundEntry> _map;
    public SoundEntry Get(SoundKey key) {
        _map ??= Build();
        return _map.TryGetValue(key, out var e) ? e : null;
    }
    private Dictionary<SoundKey, SoundEntry> Build() {
        var dict = new Dictionary<SoundKey, SoundEntry>();
        foreach (var e in entries) dict[e.key] = e;
        return dict;
    }
}
