using System;
using System.Collections;
using UnityEngine;

public sealed class BalloonPopAnimation : MonoBehaviour
{
    private static readonly int BaseMapPropertyId = Shader.PropertyToID("_BaseMap");
    private static readonly int MainTexturePropertyId = Shader.PropertyToID("_MainTex");

    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Texture2D[] frames;
    [SerializeField, Min(0.01f)] private float frameDurationSeconds = 0.05f;

    private MaterialPropertyBlock properties;
    private Coroutine playbackCoroutine;

    private void Awake()
    {
        properties = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        ApplyFrame(0);
    }

    private void OnDisable()
    {
        if (playbackCoroutine == null)
        {
            return;
        }

        StopCoroutine(playbackCoroutine);
        playbackCoroutine = null;
    }

    private void Start()
    {
        if (targetRenderer == null)
        {
            Debug.LogWarning("BalloonPopAnimation: targetRenderer is not assigned.", this);
        }

        if (frames == null || frames.Length < 2)
        {
            Debug.LogWarning("BalloonPopAnimation: at least two animation frames are required.", this);
        }
    }

    private void OnValidate()
    {
        frameDurationSeconds = Mathf.Max(0.01f, frameDurationSeconds);
    }

    public bool TryPlay(Action onCompleted)
    {
        if (playbackCoroutine != null)
        {
            return true;
        }

        if (targetRenderer == null || frames == null || frames.Length < 2)
        {
            return false;
        }

        playbackCoroutine = StartCoroutine(PlayRoutine(onCompleted));
        return true;
    }

    private IEnumerator PlayRoutine(Action onCompleted)
    {
        WaitForSeconds frameDelay = new WaitForSeconds(frameDurationSeconds);

        for (int frameIndex = 1; frameIndex < frames.Length; frameIndex++)
        {
            ApplyFrame(frameIndex);
            yield return frameDelay;
        }

        playbackCoroutine = null;
        onCompleted?.Invoke();
    }

    private void ApplyFrame(int frameIndex)
    {
        if (targetRenderer == null || frames == null || frameIndex < 0 || frameIndex >= frames.Length)
        {
            return;
        }

        Texture2D frame = frames[frameIndex];
        if (frame == null)
        {
            return;
        }

        if (properties == null)
        {
            properties = new MaterialPropertyBlock();
        }

        targetRenderer.GetPropertyBlock(properties);
        properties.SetTexture(BaseMapPropertyId, frame);
        properties.SetTexture(MainTexturePropertyId, frame);
        targetRenderer.SetPropertyBlock(properties);
    }
}
