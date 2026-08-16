using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Player
{
    /// <summary>
    /// Кинематографический оверлей: затемнение + леттербокс. Строит свой Canvas и картинки
    /// В КОДЕ — вешать/настраивать не нужно, контроллер создаёт его сам. Ничего ручного.
    /// </summary>
    public class ExamineOverlay : MonoBehaviour
    {
        private CanvasGroup _fade;
        private RectTransform _barTop, _barBottom;

        public static ExamineOverlay Create()
        {
            var go = new GameObject("ExamineOverlay");
            var ov = go.AddComponent<ExamineOverlay>();
            ov.Build();
            return ov;
        }

        private void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;

            var fadeRT = NewRect("Fade", transform);
            Stretch(fadeRT);
            _fade = fadeRT.gameObject.AddComponent<CanvasGroup>();
            _fade.alpha = 0f;
            _fade.blocksRaycasts = false;

            _barTop = NewRect("BarTop", transform);
            AnchorBar(_barTop, true);
            _barBottom = NewRect("BarBottom", transform);
            AnchorBar(_barBottom, false);

            SetLetterbox(0f);
        }

        // RawImage без текстуры рисует сплошной цвет — надёжнее Image (которому нужен спрайт).
        private RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<RawImage>();
            img.color = Color.black;
            img.raycastTarget = false;
            return go.GetComponent<RectTransform>();
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        private static void AnchorBar(RectTransform rt, bool top)
        {
            rt.anchorMin = new Vector2(0f, top ? 1f : 0f);
            rt.anchorMax = new Vector2(1f, top ? 1f : 0f);
            rt.pivot = new Vector2(0.5f, top ? 1f : 0f);
            rt.sizeDelta = Vector2.zero;
        }

        public void SetAlpha(float a)
        {
            _fade.alpha = Mathf.Clamp01(a);
            _fade.blocksRaycasts = _fade.alpha > 0.01f;
        }

        public IEnumerator FadeTo(float target, float time)
        {
            if (time <= 0f) { SetAlpha(target); yield break; }
            float start = _fade.alpha, t = 0f;
            while (t < time)
            {
                t += Time.unscaledDeltaTime;
                SetAlpha(Mathf.SmoothStep(start, target, t / time));
                yield return null;
            }
            SetAlpha(target);
        }

        public void SetLetterbox(float h01)
        {
            float h = Mathf.Clamp01(h01) * Screen.height * 0.12f;
            _barTop.sizeDelta = new Vector2(0f, h);
            _barBottom.sizeDelta = new Vector2(0f, h);
        }

        public IEnumerator LetterboxTo(float target01, float time)
        {
            float max = Mathf.Max(1f, Screen.height * 0.12f);
            float start = _barTop.sizeDelta.y / max, t = 0f;
            if (time <= 0f) { SetLetterbox(target01); yield break; }
            while (t < time)
            {
                t += Time.unscaledDeltaTime;
                SetLetterbox(Mathf.SmoothStep(start, target01, t / time));
                yield return null;
            }
            SetLetterbox(target01);
        }
    }
}