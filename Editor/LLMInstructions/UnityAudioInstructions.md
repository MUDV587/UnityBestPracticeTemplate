
# GitHub Copilot Instructions: Unity Audio

Use this cheat sheet for LLM completions. See the readme.md for the general rationale behind these guidelines.

Table of contents:
- [Unity Version-Specific Instructions](#unity-version-specific-instructions)
- [AudioMixer and AudioMixerGroup](#audiomixer-and-audiomixergroup)
- [AudioMixerSnapshot Transitions](#audiomixersnapshot-transitions)
- [Volume Control via Exposed Parameters](#volume-control-via-exposed-parameters)
- [AudioSource Pooling](#audiosource-pooling)
- [Avoid PlayClipAtPoint for Frequent SFX](#avoid-playclipatpoint-for-frequent-sfx)
- [AudioClip Memory Management](#audioclip-memory-management)
- [Spatial Audio](#spatial-audio)
- [AudioListener](#audiolistener)
- [Pooled Audio Service Pattern](#pooled-audio-service-pattern)
- [Troubleshooting](#troubleshooting)

# Unity Version-Specific Instructions

- ℹ️ This project uses Unity 2022.3 LTS. The Audio system is unchanged from Unity 2022 LTS.
- ℹ️ Unity Audio Random Containers and the native (non-FMOD) audio backend are Unity 6 features — not available on 2022.3. Randomise SFX in code (pick a random clip from an array, or randomise `AudioSource.pitch`) instead.
- ℹ️ Use `AudioSource.outputAudioMixerGroup` to route all audio through the mixer — direct volume control on `AudioSource.volume` bypasses the mixer hierarchy.
- ℹ️ `AudioMixer.SetFloat` parameter names must match **Exposed Parameters** names in the Audio Mixer window exactly (case-sensitive).

# AudioMixer and AudioMixerGroup

- ✅ Create a single `AudioMixer` asset for the project with a hierarchical group structure.
- ✅ Route every `AudioSource` to an `AudioMixerGroup` via `AudioSource.outputAudioMixerGroup`.
- ✅ Typical group hierarchy: `Master` > `Music`, `SFX`, `Voice`, `Ambience` — `SFX` > `Footsteps`, `Weapons`, `UI`.
- ❌ Never set `AudioSource.volume` directly for player-controlled volume — use mixer parameters.
- ❌ Never create multiple `AudioMixer` assets for a single game — one mixer with multiple groups is correct.

```csharp
public class AudioSourceSetup : MonoBehaviour
{
    [SerializeField] private AudioMixerGroup _sFXGroup;

    private AudioSource _source;

    private void Awake()
    {
        _source = GetComponent<AudioSource>();

        // Route to the SFX group — not setting volume directly
        _source.outputAudioMixerGroup = _sFXGroup;
        _source.playOnAwake = false;
    }
}
```

# AudioMixerSnapshot Transitions

- ✅ Use `AudioMixerSnapshot` to define different audio states (Combat, Exploration, UI, Pause) with different volume/effect settings.
- ✅ Transition between snapshots with `snapshot.TransitionTo(timeToReach)` for a smooth blend.
- ✅ Use `AudioMixer.TransitionToSnapshots(snapshots, weights, timeToReach)` to blend between multiple snapshots simultaneously.
- ❌ Do not manually lerp mixer parameters across frames to simulate snapshot behaviour — use `TransitionTo` instead.

```csharp
public class AudioStateManager : MonoBehaviour
{
    [SerializeField] private AudioMixerSnapshot _explorationSnapshot;
    [SerializeField] private AudioMixerSnapshot _combatSnapshot;
    [SerializeField] private AudioMixerSnapshot _pauseSnapshot;

    [SerializeField] private float _transitionTime = 0.5f;

    public void EnterCombat()
    {
        _combatSnapshot.TransitionTo(_transitionTime);
    }

    public void ExitCombat()
    {
        _explorationSnapshot.TransitionTo(_transitionTime);
    }

    public void PauseGame()
    {
        // Instant pause — no transition time
        _pauseSnapshot.TransitionTo(0f);
    }
}
```

# Volume Control via Exposed Parameters

- ✅ Expose mixer group volume parameters in the **Audio Mixer** window (**right-click the volume knob > Expose**).
- ✅ Name exposed parameters descriptively: `MasterVolume`, `MusicVolume`, `SFXVolume`.
- ✅ Convert linear volume (0–1 from a UI slider) to decibels before passing to `AudioMixer.SetFloat`.
- ❌ Never set volume as a linear value directly in `SetFloat` — the mixer expects decibels (dB).
- ❌ Never pass `0f` to the dB formula — it produces `-Infinity`; clamp the input to a minimum (e.g. `0.0001f`).

```csharp
public class VolumeController : MonoBehaviour
{
    [SerializeField] private AudioMixer _mixer;

    private const string MasterVolumeParam = "MasterVolume";
    private const string MusicVolumeParam = "MusicVolume";
    private const string SFXVolumeParam = "SFXVolume";

    // Call this from a UI Slider's onValueChanged event
    // sliderValue: 0.0 (silent) to 1.0 (full volume)
    public void SetMasterVolume(float sliderValue)
    {
        float db = LinearToDecibels(sliderValue);
        _mixer.SetFloat(MasterVolumeParam, db);
        PlayerPrefs.SetFloat(MasterVolumeParam, sliderValue);
    }

    public void SetMusicVolume(float sliderValue)
    {
        _mixer.SetFloat(MusicVolumeParam, LinearToDecibels(sliderValue));
        PlayerPrefs.SetFloat(MusicVolumeParam, sliderValue);
    }

    public void SetSFXVolume(float sliderValue)
    {
        _mixer.SetFloat(SFXVolumeParam, LinearToDecibels(sliderValue));
        PlayerPrefs.SetFloat(SFXVolumeParam, sliderValue);
    }

    private void Start()
    {
        // Restore saved volumes on startup
        SetMasterVolume(PlayerPrefs.GetFloat(MasterVolumeParam, 1f));
        SetMusicVolume(PlayerPrefs.GetFloat(MusicVolumeParam, 1f));
        SetSFXVolume(PlayerPrefs.GetFloat(SFXVolumeParam, 1f));
    }

    private static float LinearToDecibels(float linear)
    {
        // Clamp to avoid log(0) = -Infinity
        return Mathf.Log10(Mathf.Max(linear, 0.0001f)) * 20f;
    }
}
```

# AudioSource Pooling

- ✅ Pre-create a pool of `AudioSource` components on a persistent `GameObject` in the scene.
- ✅ Return `AudioSource` instances to the pool when `!audioSource.isPlaying`.
- ✅ Use `UnityEngine.Pool.ObjectPool<AudioSource>` to manage the pool lifecycle.
- ❌ Never create and destroy `AudioSource` GameObjects at runtime for frequent SFX — it causes GC allocations and audio glitches.
- ❌ Never use `AudioSource.PlayClipAtPoint` for sounds that play frequently (footsteps, bullets, impacts) — it allocates a new `GameObject` every call.

```csharp
public class AudioPool : MonoBehaviour
{
    [SerializeField] private int _initialPoolSize = 20;
    [SerializeField] private int _maxPoolSize = 40;
    [SerializeField] private AudioMixerGroup _defaultSFXGroup;

    private ObjectPool<AudioSource> _pool;

    private void Awake()
    {
        _pool = new ObjectPool<AudioSource>(
            createFunc: CreateAudioSource,
            actionOnGet: source => source.gameObject.SetActive(true),
            actionOnRelease: source => source.gameObject.SetActive(false),
            actionOnDestroy: source => Destroy(source.gameObject),
            collectionCheck: false,
            defaultCapacity: _initialPoolSize,
            maxSize: _maxPoolSize
        );
    }

    private AudioSource CreateAudioSource()
    {
        var go = new GameObject("PooledAudioSource");
        go.transform.SetParent(transform);
        var source = go.AddComponent<AudioSource>();
        source.outputAudioMixerGroup = _defaultSFXGroup;
        source.playOnAwake = false;
        return source;
    }

    public void PlaySFX(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f)
    {
        AudioSource source = _pool.Get();
        source.transform.position = position;
        source.clip = clip;
        source.volume = volume;
        source.pitch = pitch;
        source.Play();
        StartCoroutine(ReturnWhenDone(source, clip.length / pitch));
    }

    private System.Collections.IEnumerator ReturnWhenDone(AudioSource source, float duration)
    {
        yield return new WaitForSeconds(duration);
        _pool.Release(source);
    }
}
```

# Avoid PlayClipAtPoint for Frequent SFX

```csharp
// AVOID for frequent SFX (footsteps, bullets, impacts)
AudioSource.PlayClipAtPoint(clip, transform.position);
// Allocates a new GameObject + AudioSource every call, then destroys it after playback

// PREFER — use a pooled audio service
[SerializeField] private AudioPool _audioPool;

private void OnFootstep()
{
    _audioPool.PlaySFX(_footstepClip, transform.position);
}
```

- ✅ `AudioSource.PlayClipAtPoint` is acceptable for rare events (explosion, level complete) where allocation cost is negligible.
- ❌ Never use `PlayClipAtPoint` inside `Update`, on collision events with many simultaneous colliders, or for any sound that plays more than a few times per second.

# AudioClip Memory Management

- ✅ Set `AudioClip.loadType` in the Inspector:
  - `Decompress On Load` — for short SFX clips (< 200 KB compressed). Fast access, higher memory.
  - `Compressed In Memory` — for medium-length clips (200 KB–1 MB). Balanced.
  - `Streaming` — for long music tracks (> 1 MB). Low memory, slight latency.
- ✅ Enable `Load In Background` on large clips to avoid a hitch when the clip first loads.
- ✅ Call `audioClip.UnloadAudioData()` on clips that are no longer needed to free memory.
- ✅ Call `audioClip.LoadAudioData()` before playing a clip that was previously unloaded.
- ❌ Never stream short SFX — streaming adds latency that causes perceptible delays.
- ❌ Never decompress-on-load long music tracks — they consume hundreds of MB of RAM uncompressed.

```csharp
public class AudioMemoryManager : MonoBehaviour
{
    [SerializeField] private AudioClip _bossMusic;

    public void PrepareForBossFight()
    {
        // Ensure the clip is loaded before the fight starts
        if (!_bossMusic.loadState.Equals(AudioDataLoadState.Loaded))
            _bossMusic.LoadAudioData();
    }

    public void ExitBossFight()
    {
        // Unload boss music from memory when no longer needed
        _bossMusic.UnloadAudioData();
    }
}
```

# Spatial Audio

- ✅ Set `AudioSource.spatialBlend = 1f` for fully 3D sounds (footsteps, weapon fire, ambient world sounds).
- ✅ Set `AudioSource.spatialBlend = 0f` for fully 2D sounds (music, UI feedback, voice-over).
- ✅ Configure `AudioSource.minDistance` and `maxDistance` to match the world scale of your scene.
- ✅ Use `AudioSource.rolloffMode = AudioRolloffMode.Custom` with a hand-tuned curve for realistic attenuation.
- ❌ Never leave `spatialBlend` at 0 for world-space SFX — sounds will be heard at equal volume everywhere.

```csharp
public class WorldSFXSource : MonoBehaviour
{
    private AudioSource _source;

    [SerializeField] private float _minDistance = 2f;
    [SerializeField] private float _maxDistance = 25f;

    private void Awake()
    {
        _source = GetComponent<AudioSource>();
        _source.spatialBlend = 1f;         // Fully 3D
        _source.minDistance = _minDistance;
        _source.maxDistance = _maxDistance;
        _source.rolloffMode = AudioRolloffMode.Logarithmic;
    }
}
```

# AudioListener

- ✅ Ensure exactly one `AudioListener` is active in the scene at all times — Unity warns about 0 or 2+ listeners.
- ✅ Place the `AudioListener` on the main camera or on the player character's head bone for first-person games.
- ✅ For additive multi-scene setups, disable extra `AudioListener` components when loading additive scenes that contain a camera with a listener.
- ❌ Never have two active `AudioListener` components simultaneously — the audio output will be undefined.

# Pooled Audio Service Pattern

The complete minimal pattern for a game-wide audio service:

```csharp
public class AudioService : MonoBehaviour
{
    public static AudioService Instance { get; private set; }

    [Header("Mixer Groups")]
    [SerializeField] private AudioMixerGroup _sFXGroup;
    [SerializeField] private AudioMixerGroup _musicGroup;

    [Header("Pool Settings")]
    [SerializeField] private int _sFXPoolSize = 20;

    private ObjectPool<AudioSource> _sFXPool;
    private AudioSource _musicSource;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitialisePools();
    }

    private void InitialisePools()
    {
        _sFXPool = new ObjectPool<AudioSource>(
            createFunc: CreateSFXSource,
            actionOnGet: s => s.gameObject.SetActive(true),
            actionOnRelease: s => { s.Stop(); s.gameObject.SetActive(false); },
            actionOnDestroy: s => Destroy(s.gameObject),
            defaultCapacity: _sFXPoolSize,
            maxSize: _sFXPoolSize * 2
        );

        var musicGo = new GameObject("MusicSource");
        musicGo.transform.SetParent(transform);
        _musicSource = musicGo.AddComponent<AudioSource>();
        _musicSource.outputAudioMixerGroup = _musicGroup;
        _musicSource.loop = true;
    }

    private AudioSource CreateSFXSource()
    {
        var go = new GameObject("SFX");
        go.transform.SetParent(transform);
        var src = go.AddComponent<AudioSource>();
        src.outputAudioMixerGroup = _sFXGroup;
        src.playOnAwake = false;
        return src;
    }

    public void PlaySFX(AudioClip clip, Vector3 worldPosition)
    {
        if (clip == null) return;
        AudioSource src = _sFXPool.Get();
        src.transform.position = worldPosition;
        src.spatialBlend = 1f;
        src.clip = clip;
        src.Play();
        StartCoroutine(ReturnToPool(src, clip.length));
    }

    public void PlayMusic(AudioClip musicClip)
    {
        _musicSource.clip = musicClip;
        _musicSource.Play();
    }

    private System.Collections.IEnumerator ReturnToPool(AudioSource src, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (src != null && src.gameObject.activeSelf)
            _sFXPool.Release(src);
    }
}

---

# Troubleshooting

## AudioSource Issues

```csharp
// Issue: Sound not playing — log full AudioSource state
private void DebugAudioSource(AudioSource source)
{
    Debug.Log($"AudioSource on {source.gameObject.name}:");
    Debug.Log($"  Clip: {source.clip?.name ?? "None"}");
    Debug.Log($"  Volume: {source.volume}");
    Debug.Log($"  Mute: {source.mute}");
    Debug.Log($"  IsPlaying: {source.isPlaying}");
    Debug.Log($"  Enabled: {source.enabled}");
    Debug.Log($"  GameObject Active: {source.gameObject.activeInHierarchy}");
}

// Common checklist:
// 1. Is AudioClip assigned?
// 2. Is volume > 0?
// 3. Is exactly one AudioListener active in the scene?
// 4. Is AudioSource not muted?
// 5. Is the GameObject active and enabled?
```

## Spatial Audio Issues

```csharp
// Issue: 3D sound not attenuating — Spatial Blend must be 1 for fully 3D audio
Debug.Log($"Spatial Blend: {_source.spatialBlend}");  // 0 = 2D, 1 = 3D

// Debug: Check listener distance vs max distance
var listener = FindObjectOfType<AudioListener>();
if (listener != null)
{
    float distance = Vector3.Distance(transform.position, listener.transform.position);
    Debug.Log($"Distance to listener: {distance:F1}, Max Distance: {_source.maxDistance}");
}
```

## AudioMixer Issues

```csharp
// Issue: Mixer parameter not changing
// Check: Parameter must be Exposed in the Audio Mixer window
// (right-click the volume knob → Expose '<name>' to script)

float value;
if (_mixer.GetFloat("MasterVolume", out value))
{
    Debug.Log($"MasterVolume: {value} dB");
}
else
{
    Debug.LogError("Parameter 'MasterVolume' not found or not exposed in the Audio Mixer");
}

// Note: Mixer volume uses decibels (-80 to 0), not linear (0 to 1)
// Always convert: float db = Mathf.Log10(Mathf.Max(linear, 0.0001f)) * 20f;
```

## Common Audio Issues

| Symptom | Check | Solution |
|---------|-------|----------|
| No sound at all | Is AudioListener in scene? | Add AudioListener to the main camera |
| Sound plays once only | Is `loop` disabled and `Play()` not called again? | Enable loop or call `Play()` again |
| 3D sound always same volume | Is Spatial Blend set to 3D? | Set `spatialBlend = 1` |
| Sound cuts off early | Max Distance too small for scene scale? | Increase `maxDistance` |
| Mixer not affecting sound | Is `AudioSource.outputAudioMixerGroup` set? | Assign the correct mixer group |
| Volume slider has no effect | Using linear value instead of dB? | Convert to dB before `SetFloat` |
```
