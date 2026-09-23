using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Ushimitsu.Audio;

namespace Ushimitsu.UI
{
    public class JumpscareController : MonoBehaviour
    {
        public CanvasGroup group;
        public Image ghostImage;
        public StingPlayer sting;

        Coroutine running;

        void Awake()
        {
            if (group != null)
            {
                group.alpha = 0f;
                group.blocksRaycasts = false;
            }

            var tex = Resources.Load<Texture2D>("ghost");
            if (tex != null && ghostImage != null)
            {
                ghostImage.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
        }

        public void Play()
        {
            sting?.PlaySting();
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(FlashRoutine());
        }

        IEnumerator FlashRoutine()
        {
            if (group == null) yield break;

            float t = 0f;
            const float inDur = 0.08f;
            const float holdDur = 0.28f;
            const float outDur = 0.18f;

            while (t < inDur)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Clamp01(t / inDur);
                yield return null;
            }
            group.alpha = 1f;

            yield return new WaitForSecondsRealtime(holdDur);

            t = 0f;
            while (t < outDur)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = 1f - Mathf.Clamp01(t / outDur);
                yield return null;
            }
            group.alpha = 0f;
        }
    }
}
