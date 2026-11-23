using UnityEngine;

namespace MoodCharacters
{
    public class CameraFocus : MonoBehaviour
    {
        [SerializeField] private Vector3 offset = new Vector3(0f, 1.6f, -3.2f);
        [SerializeField] private float lookHeight = 1.1f;
        [SerializeField] private float smoothTime = 0.35f;

        private Transform target;
        private Vector3 velocity;

        private void Start()
        {
            if (GameManager.Instance == null)
            {
                return;
            }

            GameManager.Instance.ActiveCharacterChanged += OnActiveCharacterChanged;
            if (GameManager.Instance.ActiveCharacter != null)
            {
                target = GameManager.Instance.ActiveCharacter.transform;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ActiveCharacterChanged -= OnActiveCharacterChanged;
            }
        }

        private void OnActiveCharacterChanged(MoodCharacter character)
        {
            target = character.transform;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 desired = target.position + offset;
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
            transform.LookAt(target.position + Vector3.up * lookHeight);
        }
    }
}
