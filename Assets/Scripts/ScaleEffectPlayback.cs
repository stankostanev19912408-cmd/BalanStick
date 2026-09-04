using System;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-200)]
public sealed class ScaleEffectPlayback : MonoBehaviour, IEffectPlayback
{
    [Header("References")]
    [SerializeField] private Transform target;

    [Header("Scale")]
    [SerializeField] private Vector3 scale1 = Vector3.one;
    [SerializeField] private Vector3 scale2 = Vector3.one * 1.2f;

    [Header("Phase weights")]
    [Tooltip("Base duration and relative weight of the Scale 1 to Scale 2 phase.")]
    [SerializeField, Min(0f)] private float timeToScale2 = 0.2f;
    [Tooltip("Base duration and relative weight of the Scale 2 to Scale 1 phase.")]
    [SerializeField, Min(0f)] private float timeToScale1 = 0.2f;

    [Header("Playback")]
    [SerializeField] private bool playOnEnable = true;

    private float playbackDuration;
    private float elapsedTime;

    public event Action EffectStarted;
    public event Action EffectCompleted;

    public bool IsPlaying { get; private set; }

    private float BaseDuration => timeToScale2 + timeToScale1;

    private void Awake()
    {
        if (target == null)
        {
            Debug.LogWarning("ScaleEffectPlayback: target is not assigned.", this);
        }
    }

    private void OnEnable()
    {
        if (playOnEnable)
        {
            Play(BaseDuration);
        }
    }

    private void OnDisable()
    {
        Stop();
    }

    private void OnValidate()
    {
        timeToScale2 = Mathf.Max(0f, timeToScale2);
        timeToScale1 = Mathf.Max(0f, timeToScale1);
    }

    private void Update()
    {
        if (!IsPlaying)
        {
            return;
        }

        elapsedTime = Mathf.Min(elapsedTime + Time.deltaTime, playbackDuration);
        ApplyScale(elapsedTime / playbackDuration);

        if (elapsedTime >= playbackDuration)
        {
            CompletePlayback();
        }
    }

    public void Play(float durationSeconds)
    {
        Stop();

        if (target == null)
        {
            Debug.LogWarning("ScaleEffectPlayback: cannot play without a target.", this);
            return;
        }

        playbackDuration = Mathf.Max(0f, durationSeconds);
        elapsedTime = 0f;
        IsPlaying = true;
        ApplyScale(0f);
        EffectStarted?.Invoke();

        if (playbackDuration <= 0f || BaseDuration <= 0f)
        {
            CompletePlayback();
        }
    }

    public void Stop()
    {
        IsPlaying = false;
        elapsedTime = 0f;

        if (target != null)
        {
            target.localScale = scale1;
        }
    }

    private void ApplyScale(float normalizedTime)
    {
        float totalWeight = BaseDuration;
        if (target == null || totalWeight <= 0f)
        {
            return;
        }

        float firstPhaseRatio = timeToScale2 / totalWeight;
        if (firstPhaseRatio <= 0f)
        {
            target.localScale = Vector3.LerpUnclamped(scale2, scale1, normalizedTime);
            return;
        }

        if (firstPhaseRatio >= 1f)
        {
            target.localScale = normalizedTime < 1f
                ? Vector3.LerpUnclamped(scale1, scale2, normalizedTime)
                : scale1;
            return;
        }

        if (normalizedTime < firstPhaseRatio)
        {
            target.localScale = Vector3.LerpUnclamped(
                scale1,
                scale2,
                normalizedTime / firstPhaseRatio);
            return;
        }

        target.localScale = Vector3.LerpUnclamped(
            scale2,
            scale1,
            (normalizedTime - firstPhaseRatio) / (1f - firstPhaseRatio));
    }

    private void CompletePlayback()
    {
        target.localScale = scale1;
        IsPlaying = false;
        elapsedTime = playbackDuration;
        EffectCompleted?.Invoke();
    }
}
