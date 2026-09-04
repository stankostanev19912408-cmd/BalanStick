using System;

public interface IEffectPlayback
{
    event Action EffectStarted;
    event Action EffectCompleted;

    bool IsPlaying { get; }

    void Play(float durationSeconds);
    void Stop();
}
