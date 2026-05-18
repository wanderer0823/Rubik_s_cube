using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AudioEntry
{
    public string key;
    public AudioClip clip;
}

public class MusicAudioManager : MonoBehaviour
{
    public static MusicAudioManager Instance;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource loopSfxSource;

    [Header("Audio Clips")]
    [SerializeField] private List<AudioEntry> bgmClips = new List<AudioEntry>();
    [SerializeField] private List<AudioEntry> sfxClips = new List<AudioEntry>();

    [Header("Volume")]
    [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float bgmVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

    [Header("Default")]
    [SerializeField] private string defaultBgmKey;
    [SerializeField] private bool playDefaultBgmOnStart = true;

    private readonly Dictionary<string, AudioClip> bgmClipMap = new Dictionary<string, AudioClip>();
    private readonly Dictionary<string, AudioClip> sfxClipMap = new Dictionary<string, AudioClip>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        EnsureAudioSources();
        RebuildClipCache();
        ApplyVolumes();
    }

    private void Start()
    {
        if (playDefaultBgmOnStart && !string.IsNullOrEmpty(defaultBgmKey))
            PlayBgm(defaultBgmKey);
    }

    private void OnValidate()
    {
        ApplyVolumes();
    }

    public void RebuildClipCache()
    {
        bgmClipMap.Clear();
        sfxClipMap.Clear();

        CacheEntries(bgmClips, bgmClipMap);
        CacheEntries(sfxClips, sfxClipMap);
    }

    public void PlayBgm(string key, bool loop = true)
    {
        if (bgmSource == null)
            return;

        AudioClip clip = GetClip(bgmClipMap, key);
        if (clip == null)
            return;

        if (bgmSource.clip == clip && bgmSource.isPlaying)
            return;

        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.Play();
    }

    public void StopBgm()
    {
        if (bgmSource == null)
            return;

        bgmSource.Stop();
        bgmSource.clip = null;
    }

    public void PauseBgm()
    {
        if (bgmSource == null || !bgmSource.isPlaying)
            return;

        bgmSource.Pause();
    }

    public void ResumeBgm()
    {
        if (bgmSource == null)
            return;

        bgmSource.UnPause();
    }

    public void PlaySfx(string key)
    {
        if (sfxSource == null)
            return;

        AudioClip clip = GetClip(sfxClipMap, key);
        if (clip == null)
            return;

        sfxSource.PlayOneShot(clip, masterVolume * sfxVolume);
    }

    public void PlayLoopSfx(string key)
    {
        if (loopSfxSource == null)
            return;

        AudioClip clip = GetClip(sfxClipMap, key);
        if (clip == null)
            return;

        if (loopSfxSource.clip == clip && loopSfxSource.isPlaying)
            return;

        loopSfxSource.clip = clip;
        loopSfxSource.loop = true;
        loopSfxSource.Play();
    }

    public void StopLoopSfx()
    {
        if (loopSfxSource == null)
            return;

        loopSfxSource.Stop();
        loopSfxSource.clip = null;
    }

    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        ApplyVolumes();
    }

    public void SetBgmVolume(float value)
    {
        bgmVolume = Mathf.Clamp01(value);
        ApplyVolumes();
    }

    public void SetSfxVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        ApplyVolumes();
    }

    public bool HasBgm(string key)
    {
        return bgmClipMap.ContainsKey(key);
    }

    public bool HasSfx(string key)
    {
        return sfxClipMap.ContainsKey(key);
    }

    private void EnsureAudioSources()
    {
        if (bgmSource == null)
            bgmSource = CreateChildSource("BGM AudioSource");

        if (sfxSource == null)
            sfxSource = CreateChildSource("SFX AudioSource");

        if (loopSfxSource == null)
            loopSfxSource = CreateChildSource("Loop SFX AudioSource");

        bgmSource.playOnAwake = false;
        bgmSource.loop = true;

        sfxSource.playOnAwake = false;
        sfxSource.loop = false;

        loopSfxSource.playOnAwake = false;
        loopSfxSource.loop = true;
    }

    private AudioSource CreateChildSource(string childName)
    {
        Transform child = transform.Find(childName);
        GameObject childObject;

        if (child != null)
        {
            childObject = child.gameObject;
        }
        else
        {
            childObject = new GameObject(childName);
            childObject.transform.SetParent(transform, false);
        }

        AudioSource source = childObject.GetComponent<AudioSource>();
        if (source == null)
            source = childObject.AddComponent<AudioSource>();

        return source;
    }

    private void CacheEntries(List<AudioEntry> entries, Dictionary<string, AudioClip> targetMap)
    {
        foreach (AudioEntry entry in entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.key) || entry.clip == null)
                continue;

            targetMap[entry.key] = entry.clip;
        }
    }

    private AudioClip GetClip(Dictionary<string, AudioClip> clipMap, string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        if (clipMap.TryGetValue(key, out AudioClip clip))
            return clip;

        Debug.LogWarning($"MusicAudioManager: 未找到音频 key -> {key}");
        return null;
    }

    private void ApplyVolumes()
    {
        if (bgmSource != null)
            bgmSource.volume = masterVolume * bgmVolume;

        if (sfxSource != null)
            sfxSource.volume = masterVolume * sfxVolume;

        if (loopSfxSource != null)
            loopSfxSource.volume = masterVolume * sfxVolume;
    }
}
