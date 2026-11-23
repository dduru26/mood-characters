using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MoodCharacters
{
    public class SettingsScreen : MonoBehaviour
    {
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Toggle muteToggle;
        [SerializeField] private TMP_Text statusText;

        private void Start()
        {
            AudioManager audioManager = AudioManager.Instance;
            if (audioManager == null)
            {
                return;
            }

            if (masterSlider != null)
            {
                masterSlider.onValueChanged.AddListener(audioManager.SetMasterVolume);
            }
            if (musicSlider != null)
            {
                musicSlider.onValueChanged.AddListener(audioManager.SetMusicVolume);
            }
            if (sfxSlider != null)
            {
                sfxSlider.onValueChanged.AddListener(audioManager.SetSfxVolume);
            }
            if (muteToggle != null)
            {
                muteToggle.onValueChanged.AddListener(audioManager.SetMuted);
            }

            audioManager.SettingsChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SettingsChanged -= Refresh;
            }
        }

        private void Refresh()
        {
            AudioManager audioManager = AudioManager.Instance;
            if (audioManager == null)
            {
                return;
            }

            if (masterSlider != null)
            {
                masterSlider.SetValueWithoutNotify(audioManager.MasterVolume);
            }
            if (musicSlider != null)
            {
                musicSlider.SetValueWithoutNotify(audioManager.MusicVolume);
            }
            if (sfxSlider != null)
            {
                sfxSlider.SetValueWithoutNotify(audioManager.SfxVolume);
            }
            if (muteToggle != null)
            {
                muteToggle.SetIsOnWithoutNotify(audioManager.IsMuted);
            }
            if (statusText != null)
            {
                statusText.text = audioManager.IsMuted
                    ? "SOUND IS MUTED"
                    : "MASTER " + Mathf.RoundToInt(audioManager.MasterVolume * 100f) + "%";
            }
        }
    }
}
