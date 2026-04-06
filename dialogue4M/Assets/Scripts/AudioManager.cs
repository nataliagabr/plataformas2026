using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    #region Singleton
    public static AudioManager Instance { get; private set; }
    [System.Obsolete("Use Instance instead", false)]
    public static AudioManager instance => Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        EnsureSystemSource();
        if (activeSources == null) activeSources = new List<AudioSource>();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
    #endregion

    [Header("Audio References")]
    [SerializeField]
    // single AudioSource used for 2D sounds and one-shots
    private AudioSource systemSource;

    [SerializeField]
    // parent transform for created 3D sources
    private Transform activeSourceParent;

    // list of active 3D looping AudioSources
    private List<AudioSource> activeSources = new List<AudioSource>();

    void EnsureSystemSource()
    {
        if (systemSource == null)
        {
            GameObject go = new GameObject("SystemAudioSource");
            go.transform.SetParent(transform);
            systemSource = go.AddComponent<AudioSource>();
            systemSource.playOnAwake = false;
            systemSource.spatialBlend = 0f; // 2D
        }

        if (activeSourceParent == null) activeSourceParent = transform;
    }

    // ------------------ 2D Sound API (systemSource) ------------------
    public void Play2D(AudioClip clip, float volume = 1f, bool loop = false)
    {
        if (clip == null) return;
        EnsureSystemSource();

        if (loop)
        {
            systemSource.clip = clip;
            systemSource.loop = true;
            systemSource.volume = volume;
            systemSource.Play();
        }
        else
        {
            systemSource.PlayOneShot(clip, volume);
        }
    }

    public void Play2DOneShot(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        EnsureSystemSource();
        systemSource.PlayOneShot(clip, volume);
    }

    public void Stop2D()
    {
        if (systemSource == null) return;
        systemSource.Stop();
        systemSource.clip = null;
        systemSource.loop = false;
    }

    public void Pause2D()
    {
        if (systemSource == null) return;
        systemSource.Pause();
    }

    public void Resume2D()
    {
        if (systemSource == null) return;
        systemSource.UnPause();
    }

    // ------------------ 3D Sound API (activeSources) ------------------
    // Plays a 3D clip at the given position. If loop==true the created AudioSource is tracked in activeSources and must be stopped explicitly.
    public AudioSource Play3D(AudioClip clip, Vector3 position, float volume = 1f, bool loop = false, float minDistance = 1f, float maxDistance = 500f)
    {
        if (clip == null) return null;

        GameObject go = new GameObject("3DAudio_" + clip.name);
        go.transform.position = position;
        go.transform.SetParent(activeSourceParent, true);

        AudioSource src = go.AddComponent<AudioSource>();
        src.spatialBlend = 1f; // full 3D
        src.playOnAwake = false;
        src.clip = clip;
        src.volume = volume;
        src.loop = loop;
        src.minDistance = minDistance;
        src.maxDistance = maxDistance;
        src.rolloffMode = AudioRolloffMode.Logarithmic;
        src.Play();

        if (loop)
        {
            if (activeSources == null) activeSources = new List<AudioSource>();
            activeSources.Add(src);
        }
        else
        {
            // schedule destruction when finished
            Destroy(go, clip.length + 0.1f);
        }

        return src;
    }

    // Play a 3D one-shot (does not create a tracked AudioSource)
    public void Play3DOneShot(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null) return;
        AudioSource.PlayClipAtPoint(clip, position, volume);
    }

    public void Stop3D(AudioSource source, bool destroy = true)
    {
        if (source == null) return;
        source.Stop();
        if (activeSources != null) activeSources.Remove(source);
        if (destroy) Destroy(source.gameObject);
        else
        {
            source.clip = null;
            source.loop = false;
        }
    }

    public void Pause3D(AudioSource source)
    {
        if (source == null) return;
        source.Pause();
    }

    public void Resume3D(AudioSource source)
    {
        if (source == null) return;
        source.UnPause();
    }

    public void StopAll3D()
    {
        if (activeSources == null) return;
        // make a copy to avoid modification during iteration
        var copy = new List<AudioSource>(activeSources);
        foreach (var s in copy)
        {
            if (s != null) Stop3D(s, true);
        }
        activeSources.Clear();
    }

    public void PauseAll3D()
    {
        if (activeSources == null) return;
        foreach (var s in activeSources)
        {
            if (s != null) s.Pause();
        }
    }

    public void ResumeAll3D()
    {
        if (activeSources == null) return;
        foreach (var s in activeSources)
        {
            if (s != null) s.UnPause();
        }
    }

    // Optional static convenience wrappers
    public static void Play2DStatic(AudioClip clip, float volume = 1f, bool loop = false) { if (Instance != null) Instance.Play2D(clip, volume, loop); }
    public static void Play2DOneShotStatic(AudioClip clip, float volume = 1f) { if (Instance != null) Instance.Play2DOneShot(clip, volume); }
    public static AudioSource Play3DStatic(AudioClip clip, Vector3 pos, float volume = 1f, bool loop = false) { return Instance != null ? Instance.Play3D(clip, pos, volume, loop) : null; }
    public static void Play3DOneShotStatic(AudioClip clip, Vector3 pos, float volume = 1f) { if (Instance != null) Instance.Play3DOneShot(clip, pos, volume); }
}