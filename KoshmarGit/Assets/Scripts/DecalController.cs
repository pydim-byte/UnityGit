using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class DecalController : MonoBehaviour
{
    public float pulseScale = 1.15f;
    public AnimationCurve scaleOverTime = AnimationCurve.EaseInOut(0, 0.8f, 1, 1f);
    public string colorProperty = "_Color"; // change if your shader uses different property
    private Material mat;
    private Renderer rend;
    Coroutine running;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        if (rend) mat = rend.material; // creates instance
    }

    public void Play(float duration)
    {
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(PlayRoutine(duration));
    }

    IEnumerator PlayRoutine(float duration)
    {
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;
        while (elapsed < duration)
        {
            float t = Mathf.Clamp01(elapsed / duration);

            // alpha fade in then out (smoothstep)
            float alpha = Mathf.SmoothStep(0f, 1f, t < 0.5f ? t * 2f : (1f - t) * 2f);
            SetAlpha(alpha);

            // scale/pulse
            float s = scaleOverTime.Evaluate(t) * pulseScale;
            transform.localScale = startScale * s;

            elapsed += Time.deltaTime;
            yield return null;
        }

        SetAlpha(0f);
        // return to pool or destroy (the spawner handles returning)
    }

    void SetAlpha(float a)
    {
        if (mat == null) return;
        if (mat.HasProperty(colorProperty))
        {
            Color c = mat.GetColor(colorProperty);
            c.a = a;
            mat.SetColor(colorProperty, c);
        }
    }

    private void OnDisable()
    {
        // reset alpha so when re-used it is visible
        SetAlpha(0f);
        transform.localScale = Vector3.one;
    }
}
