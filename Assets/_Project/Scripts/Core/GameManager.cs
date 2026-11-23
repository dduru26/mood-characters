using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoodCharacters
{
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Tooltip("Drag the 3 characters here in the order you want them. Left empty, the scene is searched automatically.")]
        [SerializeField] private List<MoodCharacter> characters = new List<MoodCharacter>();
        [SerializeField] private int startIndex = 0;

        public event Action<MoodCharacter> ActiveCharacterChanged;
        public event Action<MoodCharacter, Mood> MoodChanged;
        public event Action<MoodCharacter> CharacterReacted;
        public event Action<AppState> StateChanged;

        public IReadOnlyList<MoodCharacter> Characters => characters;
        public MoodCharacter ActiveCharacter { get; private set; }
        public int ActiveIndex { get; private set; } = -1;
        public AppState State { get; private set; } = AppState.Menu;
        public bool CanInteract => State == AppState.Interacting && ActiveCharacter != null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            characters.RemoveAll(c => c == null);
            if (characters.Count == 0)
            {
                characters.AddRange(FindObjectsByType<MoodCharacter>());
                characters.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
            }
        }

        private void Start()
        {
            foreach (MoodCharacter character in characters)
            {
                character.SetActive(false);
            }
            SelectCharacterByIndex(startIndex);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void SelectCharacterByIndex(int index)
        {
            int count = characters.Count;
            if (count == 0)
            {
                return;
            }

            index = ((index % count) + count) % count;
            if (index == ActiveIndex)
            {
                return;
            }

            if (ActiveCharacter != null)
            {
                ActiveCharacter.SetActive(false);
            }

            ActiveIndex = index;
            ActiveCharacter = characters[index];
            ActiveCharacter.SetActive(true);
            ActiveCharacterChanged?.Invoke(ActiveCharacter);
        }

        public void SelectCharacter(MoodCharacter character)
        {
            int index = characters.IndexOf(character);
            if (index >= 0)
            {
                SelectCharacterByIndex(index);
            }
        }

        public void NextCharacter()
        {
            SelectCharacterByIndex(ActiveIndex + 1);
        }

        public void PreviousCharacter()
        {
            SelectCharacterByIndex(ActiveIndex - 1);
        }

        public void SetMoodIndex(int moodIndex)
        {
            SetMood((Mood)Mathf.Clamp(moodIndex, 0, 3));
        }

        public void SetMood(Mood mood)
        {
            if (!CanInteract || ActiveCharacter.CurrentMood == mood)
            {
                return;
            }

            ActiveCharacter.ApplyMood(mood);
            MoodChanged?.Invoke(ActiveCharacter, mood);
        }

        public void TriggerReaction()
        {
            if (!CanInteract)
            {
                return;
            }

            ActiveCharacter.React();
            CharacterReacted?.Invoke(ActiveCharacter);
        }

        public void SetState(AppState newState)
        {
            if (State == newState)
            {
                return;
            }

            State = newState;
            StateChanged?.Invoke(newState);
        }
    }
}
