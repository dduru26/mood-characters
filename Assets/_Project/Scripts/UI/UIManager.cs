using System.Collections.Generic;
using UnityEngine;

namespace MoodCharacters
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [SerializeField] private UIScreen firstScreen;
        [SerializeField] private List<UIScreen> screens = new List<UIScreen>();

        private readonly Stack<UIScreen> history = new Stack<UIScreen>();

        public UIScreen CurrentScreen { get; private set; }
        public bool CanGoBack => history.Count > 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (screens.Count == 0)
            {
                screens.AddRange(GetComponentsInChildren<UIScreen>(true));
            }
        }

        private void Start()
        {
            foreach (UIScreen screen in screens)
            {
                screen.HideInstant();
            }

            if (firstScreen != null)
            {
                CurrentScreen = firstScreen;
                firstScreen.Show();
                ApplyState(firstScreen);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Open(UIScreen screen)
        {
            if (screen == null || screen == CurrentScreen)
            {
                return;
            }

            if (CurrentScreen != null)
            {
                history.Push(CurrentScreen);
                CurrentScreen.Hide();
            }

            SwitchTo(screen);
        }

        public void Back()
        {
            if (!CanGoBack)
            {
                return;
            }

            CurrentScreen.Hide();
            SwitchTo(history.Pop());
        }

        public void GoHome()
        {
            if (firstScreen == null || CurrentScreen == firstScreen)
            {
                return;
            }

            history.Clear();
            CurrentScreen.Hide();
            SwitchTo(firstScreen);
        }

        public void QuitApp()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void SwitchTo(UIScreen screen)
        {
            CurrentScreen = screen;
            screen.Show();
            ApplyState(screen);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayScreenChange();
            }
        }

        private void ApplyState(UIScreen screen)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(screen.StateWhileOpen);
            }
        }
    }
}
