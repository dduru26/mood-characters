using System.Collections;
using UnityEngine;

namespace MoodCharacters
{
    [RequireComponent(typeof(CanvasGroup))]
    public class UIScreen : MonoBehaviour
    {
        [SerializeField] private AppState stateWhileOpen = AppState.Menu;
        [SerializeField] private float duration = 0.35f;
        [SerializeField] private Vector2 slideOffset = new Vector2(0f, -60f);
        [SerializeField] private float startScale = 0.96f;

        private CanvasGroup group;
        private RectTransform rect;
        private Vector2 restPosition;
        private Coroutine routine;
        private bool initialised;

        public AppState StateWhileOpen => stateWhileOpen;

        private void Awake()
        {
            Init();
        }

        private void Init()
        {
            if (initialised)
            {
                return;
            }
            group = GetComponent<CanvasGroup>();
            rect = GetComponent<RectTransform>();
            restPosition = rect.anchoredPosition;
            initialised = true;
        }

        public void HideInstant()
        {
            Init();
            StopRoutine();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        public void Show()
        {
            Init();
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            StopRoutine();
            routine = StartCoroutine(Animate(true));
        }

        public void Hide()
        {
            Init();
            if (!gameObject.activeInHierarchy)
            {
                return;
            }
            StopRoutine();
            routine = StartCoroutine(Animate(false));
        }

        private void StopRoutine()
        {
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }
        }

        private IEnumerator Animate(bool show)
        {
            group.interactable = false;
            group.blocksRaycasts = show;

            float fromAlpha = group.alpha;
            float toAlpha = show ? 1f : 0f;
            Vector2 fromPos = rect.anchoredPosition;
            Vector2 toPos = show ? restPosition : restPosition + slideOffset;
            float fromScale = rect.localScale.x;
            float toScale = show ? 1f : startScale;

            if (show && fromAlpha <= 0.01f)
            {
                fromPos = restPosition + slideOffset;
                fromScale = startScale;
            }

            float time = 0f;
            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                group.alpha = Mathf.Lerp(fromAlpha, toAlpha, eased);
                rect.anchoredPosition = Vector2.LerpUnclamped(fromPos, toPos, eased);
                float s = Mathf.Lerp(fromScale, toScale, eased);
                rect.localScale = new Vector3(s, s, 1f);
                yield return null;
            }

            group.alpha = toAlpha;
            rect.anchoredPosition = toPos;
            rect.localScale = new Vector3(toScale, toScale, 1f);
            routine = null;

            if (show)
            {
                group.interactable = true;
            }
            else
            {
                rect.anchoredPosition = restPosition;
                rect.localScale = Vector3.one;
                gameObject.SetActive(false);
            }
        }
    }
}
