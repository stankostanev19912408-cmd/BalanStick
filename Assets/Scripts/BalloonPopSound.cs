using System;
using System.Collections;
using UnityEngine;

public sealed class BalloonPopSound : MonoBehaviour
{
    private const float MinimumAllowedPitch = 0.1f;
    private const float MaximumAllowedPitch = 3f;

    [SerializeField] private AudioSource audioSource;
    [SerializeField, Range(MinimumAllowedPitch, MaximumAllowedPitch)] private float minPitch = 0.9f;
    [SerializeField, Range(MinimumAllowedPitch, MaximumAllowedPitch)] private float maxPitch = 1.1f;

    private Coroutine playbackCoroutine;

    private void Start()
    {
        if (audioSource == null)
        {
            Debug.LogWarning("BalloonPopSound: audioSource is not assigned.", this);
            return;
        }

        if (audioSource.clip == null)
        {
            Debug.LogWarning("BalloonPopSound: AudioSource clip is not assigned.", this);
        }
    }

    private void OnDisable()
    {
        if (playbackCoroutine != null)
        {
            StopCoroutine(playbackCoroutine);
            playbackCoroutine = null;
        }

        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

    private void OnValidate()
    {
        minPitch = Mathf.Clamp(minPitch, MinimumAllowedPitch, MaximumAllowedPitch);
        maxPitch = Mathf.Clamp(maxPitch, minPitch, MaximumAllowedPitch);
    }

    public bool TryPlay(Action onCompleted)
    {
        if (playbackCoroutine != null)
        {
            return true;
        }

        if (audioSource == null || audioSource.clip == null)
        {
            return false;
        }

        playbackCoroutine = StartCoroutine(PlayRoutine(onCompleted));
        return true;
    }

    private IEnumerator PlayRoutine(Action onCompleted)
    {
        audioSource.pitch = UnityEngine.Random.Range(minPitch, maxPitch);
        audioSource.Play();
        yield return null;

        while (audioSource != null && audioSource.isPlaying)
        {
            yield return null;
        }

        playbackCoroutine = null;
        onCompleted?.Invoke();
    }
}
