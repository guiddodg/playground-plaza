using System.Collections;
using UnityEngine;

/// <summary>
/// One row in the <see cref="NotificationFeed"/>. Stays solid for its lifetime,
/// then fades out via its <see cref="CanvasGroup"/> and destroys itself.
/// </summary>
public class NotificationRow : MonoBehaviour
{
    private CanvasGroup group;

    public void Play(CanvasGroup canvasGroup, float lifetime, float fadeDuration)
    {
        group = canvasGroup;
        if (group != null) group.alpha = 1f;
        StartCoroutine(Life(lifetime, fadeDuration));
    }

    private IEnumerator Life(float lifetime, float fadeDuration)
    {
        yield return new WaitForSeconds(lifetime);

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            if (group != null) group.alpha = Mathf.Clamp01(1f - t / fadeDuration);
            yield return null;
        }

        Destroy(gameObject);
    }
}
