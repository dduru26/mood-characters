using UnityEngine;

namespace MoodCharacters
{
    [RequireComponent(typeof(Animator))]
    public class MoodCharacter : MonoBehaviour
    {
        private static readonly int MoodHash = Animator.StringToHash("Mood");
        private static readonly int IsActiveHash = Animator.StringToHash("IsActive");
        private static readonly int ReactHash = Animator.StringToHash("React");
        private static readonly string[] StateNames = { "Idle", "Happy", "Angry", "Sad", "React" };

        [SerializeField] private string displayName = "Character";
        [SerializeField] private GameObject selectionRing;

        [Header("Voice / action sounds")]
        [SerializeField] private AudioClip neutralClip;
        [SerializeField] private AudioClip happyClip;
        [SerializeField] private AudioClip angryClip;
        [SerializeField] private AudioClip sadClip;
        [SerializeField] private AudioClip reactClip;

        private Animator animator;

        public string DisplayName => displayName;
        public Mood CurrentMood { get; private set; } = Mood.Neutral;
        public bool IsActive { get; private set; }
        public AudioClip ReactClip => reactClip;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            if (selectionRing != null)
            {
                selectionRing.SetActive(false);
            }
        }

        public void SetActive(bool active)
        {
            IsActive = active;
            animator.SetBool(IsActiveHash, active);

            if (selectionRing != null)
            {
                selectionRing.SetActive(active);
            }

            if (!active)
            {
                animator.ResetTrigger(ReactHash);
                ApplyMood(Mood.Neutral);
            }
        }

        public void ApplyMood(Mood mood)
        {
            CurrentMood = mood;
            animator.SetInteger(MoodHash, (int)mood);
        }

        public void React()
        {
            animator.SetTrigger(ReactHash);
        }

        public AudioClip GetMoodClip(Mood mood)
        {
            switch (mood)
            {
                case Mood.Happy: return happyClip;
                case Mood.Angry: return angryClip;
                case Mood.Sad: return sadClip;
                default: return neutralClip;
            }
        }

        public string GetAnimatorStateLabel()
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return "No Animator";
            }

            if (animator.IsInTransition(0))
            {
                return "Blending to " + FindStateName(animator.GetNextAnimatorStateInfo(0));
            }

            return FindStateName(animator.GetCurrentAnimatorStateInfo(0));
        }

        private static string FindStateName(AnimatorStateInfo info)
        {
            foreach (string stateName in StateNames)
            {
                if (info.IsName(stateName))
                {
                    return stateName;
                }
            }
            return "Unknown";
        }
    }
}
