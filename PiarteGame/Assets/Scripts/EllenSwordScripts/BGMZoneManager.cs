using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BGMZoneManager : MonoBehaviour
{
    [System.Serializable]
    public class BGMZone
    {
        public string zoneName;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
    }

    [Header("BGM Zones")]
    public List<BGMZone> zones = new List<BGMZone>();

    [Header("Fade Settings")]
    public float fadeDuration = 1.0f;

    private AudioSource _audioSource;
    private Coroutine _fadeRoutine;
    private string _currentZone = "";

    // =======================
    // PLAYER LIFECYCLE STATE
    // =======================
    private string _lastPlayedZone = "";

    private void Awake()
    {
        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.loop = true;
        _audioSource.playOnAwake = false;
    }

    // =======================
    // ZONE CONTROL
    // =======================

    public void PlayZoneByName(string zoneName)
    {
        if (_currentZone == zoneName) return;

        BGMZone zone = zones.Find(z => z.zoneName == zoneName);
        if (zone == null)
        {
            Debug.LogWarning($"[BGMZoneManager] Zone not found: {zoneName}");
            return;
        }

        if (_fadeRoutine != null)
            StopCoroutine(_fadeRoutine);

        _fadeRoutine = StartCoroutine(FadeTo(zone));
    }

    private IEnumerator FadeTo(BGMZone zone)
    {
        // Fade OUT current
        if (_audioSource.isPlaying)
        {
            float startVol = _audioSource.volume;
            float t = 0f;

            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                _audioSource.volume = Mathf.Lerp(startVol, 0f, t / fadeDuration);
                yield return null;
            }

            _audioSource.Stop();
        }

        // Switch clip
        _audioSource.clip = zone.clip;
        _audioSource.volume = 0f;
        _audioSource.Play();

        _currentZone = zone.zoneName;

        // Fade IN new
        float tIn = 0f;
        while (tIn < fadeDuration)
        {
            tIn += Time.deltaTime;
            _audioSource.volume = Mathf.Lerp(0f, zone.volume, tIn / fadeDuration);
            yield return null;
        }

        _audioSource.volume = zone.volume;
    }

    public void StopMusic(float fadeOut = 1f)
    {
        if (_fadeRoutine != null)
            StopCoroutine(_fadeRoutine);

        _fadeRoutine = StartCoroutine(FadeOutAndStop(fadeOut));
    }

    private IEnumerator FadeOutAndStop(float duration)
    {
        float startVol = _audioSource.volume;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            _audioSource.volume = Mathf.Lerp(startVol, 0f, t / duration);
            yield return null;
        }

        _audioSource.Stop();
        _currentZone = "";
    }

    // =======================
    // PLAYER LIFECYCLE HOOKS
    // =======================

    public void OnPlayerDied_StopMusic()
    {
        _lastPlayedZone = _currentZone;
        StopMusic(0.6f);

        Debug.Log("[BGMZoneManager] Player died → music stopped");
    }

    public void OnPlayerRestarted_PlayLastMusic()
    {
        if (string.IsNullOrEmpty(_lastPlayedZone))
        {
            Debug.LogWarning("[BGMZoneManager] No last music to restore");
            return;
        }

        PlayZoneByName(_lastPlayedZone);

        Debug.Log($"[BGMZoneManager] Player restarted → music restored: {_lastPlayedZone}");
    }
}
