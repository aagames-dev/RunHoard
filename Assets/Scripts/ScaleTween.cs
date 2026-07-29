using System;
using System.Collections;
using UnityEngine;

public static class ScaleTween
{
    // Shared scale-pulse/punch coroutine: drives target.localScale = baseScale * factorCurve(t) over
    // duration, where factorCurve maps normalized time [0,1] to a scale multiplier. Callers supply their
    // own easing shape (pulse, bounce, pop-and-shrink, etc.) instead of each hand-rolling the same
    // elapsed-time loop.
    public static IEnumerator Run(Transform target, Vector3 baseScale, Func<float, float> factorCurve, float duration, bool unscaledTime, Action onComplete = null)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            target.localScale = baseScale * factorCurve(t);
            yield return null;
        }

        target.localScale = baseScale * factorCurve(1f);
        onComplete?.Invoke();
    }
}
