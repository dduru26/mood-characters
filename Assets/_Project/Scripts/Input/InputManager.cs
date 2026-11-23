using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MoodCharacters
{
    public class InputManager : MonoBehaviour
    {
        [SerializeField] private Camera raycastCamera;
        [SerializeField] private LayerMask characterLayers = ~0;
        [SerializeField] private float maxRayDistance = 100f;

        private void Start()
        {
            if (raycastCamera == null)
            {
                raycastCamera = Camera.main;
            }
        }

        private void Update()
        {
            GameManager gm = GameManager.Instance;
            UIManager ui = UIManager.Instance;
            if (gm == null)
            {
                return;
            }

            if (EscapePressed() && ui != null)
            {
                ui.Back();
            }

            if (!gm.CanInteract)
            {
                return;
            }

            if (DigitPressed(1)) gm.SetMood(Mood.Neutral);
            if (DigitPressed(2)) gm.SetMood(Mood.Happy);
            if (DigitPressed(3)) gm.SetMood(Mood.Angry);
            if (DigitPressed(4)) gm.SetMood(Mood.Sad);

            if (SpacePressed()) gm.TriggerReaction();
            if (NextPressed()) gm.NextCharacter();
            if (PreviousPressed()) gm.PreviousCharacter();

            if (ClickPressed(out Vector2 screenPosition) && !PointerOverUI())
            {
                TrySelectAt(screenPosition, gm);
            }
        }

        private void TrySelectAt(Vector2 screenPosition, GameManager gm)
        {
            if (raycastCamera == null)
            {
                return;
            }

            Ray ray = raycastCamera.ScreenPointToRay(screenPosition);
            if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, characterLayers))
            {
                MoodCharacter character = hit.collider.GetComponentInParent<MoodCharacter>();
                if (character == null)
                {
                    return;
                }

                if (character == gm.ActiveCharacter)
                {
                    gm.TriggerReaction();
                }
                else
                {
                    gm.SelectCharacter(character);
                }
            }
        }

        private static bool PointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

#if ENABLE_INPUT_SYSTEM
        private static bool EscapePressed()
        {
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        }

        private static bool SpacePressed()
        {
            return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        }

        private static bool NextPressed()
        {
            Keyboard k = Keyboard.current;
            return k != null && (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame);
        }

        private static bool PreviousPressed()
        {
            Keyboard k = Keyboard.current;
            return k != null && (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame);
        }

        private static bool DigitPressed(int digit)
        {
            Keyboard k = Keyboard.current;
            if (k == null)
            {
                return false;
            }

            switch (digit)
            {
                case 1: return k.digit1Key.wasPressedThisFrame || k.numpad1Key.wasPressedThisFrame;
                case 2: return k.digit2Key.wasPressedThisFrame || k.numpad2Key.wasPressedThisFrame;
                case 3: return k.digit3Key.wasPressedThisFrame || k.numpad3Key.wasPressedThisFrame;
                case 4: return k.digit4Key.wasPressedThisFrame || k.numpad4Key.wasPressedThisFrame;
                default: return false;
            }
        }

        private static bool ClickPressed(out Vector2 position)
        {
            position = Vector2.zero;
            Mouse m = Mouse.current;
            if (m == null || !m.leftButton.wasPressedThisFrame)
            {
                return false;
            }
            position = m.position.ReadValue();
            return true;
        }
#else
        private static bool EscapePressed()
        {
            return Input.GetKeyDown(KeyCode.Escape);
        }

        private static bool SpacePressed()
        {
            return Input.GetKeyDown(KeyCode.Space);
        }

        private static bool NextPressed()
        {
            return Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D);
        }

        private static bool PreviousPressed()
        {
            return Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A);
        }

        private static bool DigitPressed(int digit)
        {
            switch (digit)
            {
                case 1: return Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1);
                case 2: return Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2);
                case 3: return Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3);
                case 4: return Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4);
                default: return false;
            }
        }

        private static bool ClickPressed(out Vector2 position)
        {
            position = Input.mousePosition;
            return Input.GetMouseButtonDown(0);
        }
#endif
    }
}
