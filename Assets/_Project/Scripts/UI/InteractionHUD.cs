using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MoodCharacters
{
    public class InteractionHUD : MonoBehaviour
    {
        private static readonly Color[] MoodColors =
        {
            new Color(0.75f, 0.75f, 0.78f),
            new Color(1f, 0.8f, 0.25f),
            new Color(0.92f, 0.3f, 0.28f),
            new Color(0.35f, 0.55f, 0.95f)
        };

        [SerializeField] private TMP_Text characterNameText;
        [SerializeField] private TMP_Text moodText;
        [SerializeField] private TMP_Text animationStateText;
        [SerializeField] private Image moodBadge;
        [SerializeField] private TMP_Dropdown characterDropdown;

        [Tooltip("Mood button backgrounds in order: Neutral, Happy, Angry, Sad.")]
        [SerializeField] private Image[] moodButtonImages = new Image[4];
        [SerializeField] private Color unselectedButtonColor = new Color(1f, 1f, 1f, 0.35f);

        private string lastStateLabel;

        private void Start()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null)
            {
                return;
            }

            gm.ActiveCharacterChanged += OnActiveCharacterChanged;
            gm.MoodChanged += OnMoodChanged;

            if (characterDropdown != null)
            {
                List<string> names = new List<string>();
                foreach (MoodCharacter character in gm.Characters)
                {
                    names.Add(character.DisplayName);
                }
                characterDropdown.ClearOptions();
                characterDropdown.AddOptions(names);
                characterDropdown.onValueChanged.AddListener(gm.SelectCharacterByIndex);
            }

            Refresh();
        }

        private void OnDestroy()
        {
            GameManager gm = GameManager.Instance;
            if (gm != null)
            {
                gm.ActiveCharacterChanged -= OnActiveCharacterChanged;
                gm.MoodChanged -= OnMoodChanged;
            }
        }

        private void Update()
        {
            MoodCharacter active = GameManager.Instance != null ? GameManager.Instance.ActiveCharacter : null;
            if (active == null || animationStateText == null)
            {
                return;
            }

            string label = active.GetAnimatorStateLabel();
            if (label != lastStateLabel)
            {
                lastStateLabel = label;
                animationStateText.text = "Animation: " + label;
            }
        }

        private void OnActiveCharacterChanged(MoodCharacter character)
        {
            Refresh();
        }

        private void OnMoodChanged(MoodCharacter character, Mood mood)
        {
            Refresh();
        }

        private void Refresh()
        {
            GameManager gm = GameManager.Instance;
            MoodCharacter active = gm != null ? gm.ActiveCharacter : null;
            if (active == null)
            {
                return;
            }

            int moodIndex = (int)active.CurrentMood;
            Color moodColor = MoodColors[moodIndex];

            if (characterNameText != null)
            {
                characterNameText.text = active.DisplayName;
            }

            if (moodText != null)
            {
                moodText.text = "Mood: " + active.CurrentMood;
                moodText.color = moodColor;
            }

            if (moodBadge != null)
            {
                moodBadge.color = moodColor;
            }

            if (characterDropdown != null)
            {
                characterDropdown.SetValueWithoutNotify(gm.ActiveIndex);
            }

            for (int i = 0; i < moodButtonImages.Length; i++)
            {
                if (moodButtonImages[i] != null)
                {
                    moodButtonImages[i].color = i == moodIndex ? MoodColors[i] : unselectedButtonColor;
                }
            }
        }
    }
}
