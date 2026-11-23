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
            new Color(0.85f, 0.85f, 0.88f),
            new Color(1f, 0.8f, 0.25f),
            new Color(1f, 0.23f, 0.07f),
            new Color(0.35f, 0.55f, 1f)
        };

        [SerializeField] private TMP_Text characterNameText;
        [SerializeField] private TMP_Text indexText;
        [SerializeField] private TMP_Text moodText;
        [SerializeField] private TMP_Text animationStateText;
        [SerializeField] private Image moodBadge;
        [SerializeField] private TMP_Dropdown characterDropdown;

        [Tooltip("Mood button backgrounds in order: Neutral, Happy, Angry, Sad.")]
        [SerializeField] private Image[] moodButtonImages = new Image[4];
        [SerializeField] private TMP_Text[] moodButtonLabels = new TMP_Text[4];
        [SerializeField] private Color unselectedButtonColor = new Color(1f, 1f, 1f, 0.35f);
        [SerializeField] private Color selectedLabelColor = new Color(0.06f, 0.06f, 0.08f);
        [SerializeField] private Color unselectedLabelColor = Color.white;

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
                    names.Add(character.DisplayName.ToUpperInvariant());
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
                animationStateText.text = "STATE / " + label.ToUpperInvariant();
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
                characterNameText.text = active.DisplayName.ToUpperInvariant();
            }

            if (indexText != null)
            {
                indexText.text = "[" + (gm.ActiveIndex + 1).ToString("00") + "/" + gm.Characters.Count.ToString("00") + "]";
            }

            if (moodText != null)
            {
                moodText.text = "MOOD / " + active.CurrentMood.ToString().ToUpperInvariant();
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
                bool selected = i == moodIndex;
                if (moodButtonImages[i] != null)
                {
                    moodButtonImages[i].color = selected ? MoodColors[i] : unselectedButtonColor;
                }
                if (i < moodButtonLabels.Length && moodButtonLabels[i] != null)
                {
                    moodButtonLabels[i].color = selected ? selectedLabelColor : unselectedLabelColor;
                }
            }
        }
    }
}
