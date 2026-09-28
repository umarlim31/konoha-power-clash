using UnityEngine;

namespace Konoha.Character
{
    // Sits on a hero visual root created from a rigged model (HeroVisualCatalog). Its
    // presence marks the visual as a 3D model, so presenters hide the capsule body.
    // Speed is measured from the root's own horizontal movement, which works for the
    // solo motor, the PvP owner and interpolated remote players alike. Every call is a
    // no-op when the Animator, its controller or a parameter is missing.
    public sealed class HeroAnimatorDriver : MonoBehaviour
    {
        public const string SpeedParameter = "Speed";
        public const string AttackTrigger = "Attack";
        public const string SkillTrigger = "Skill";
        public const string HitTrigger = "Hit";
        public const string JumpTrigger = "Jump";
        public const string RuntuhBool = "Runtuh";

        private static readonly int SpeedHash = Animator.StringToHash(SpeedParameter);
        private static readonly int AttackHash = Animator.StringToHash(AttackTrigger);
        private static readonly int SkillHash = Animator.StringToHash(SkillTrigger);
        private static readonly int HitHash = Animator.StringToHash(HitTrigger);
        private static readonly int JumpHash = Animator.StringToHash(JumpTrigger);
        private static readonly int RuntuhHash = Animator.StringToHash(RuntuhBool);

        public Animator animator;
        [Min(0f)] public float speedSmoothing = 12f;

        private Vector3 lastPosition;
        private float speed;
        private bool hasLastPosition;
        private RuntimeAnimatorController cachedController;
        private AnimatorControllerParameter[] cachedParameters;

        public float CurrentSpeed => speed;

        public static bool UsesModel(GameObject visual) =>
            visual != null && visual.GetComponent<HeroAnimatorDriver>() != null;

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
        }

        private void OnEnable() => hasLastPosition = false;

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            Vector3 position = transform.position;
            if (!hasLastPosition || dt <= 0f)
            {
                lastPosition = position;
                hasLastPosition = true;
                return;
            }
            Vector3 delta = position - lastPosition;
            delta.y = 0f;
            lastPosition = position;
            // Teleports (respawn, restart) would read as a sprint for one frame.
            float measured = delta.magnitude > 3f ? 0f : delta.magnitude / dt;
            speed = Mathf.Lerp(speed, measured, 1f - Mathf.Exp(-speedSmoothing * dt));
            if (Ready(SpeedHash, AnimatorControllerParameterType.Float))
                animator.SetFloat(SpeedHash, speed);
        }

        public void PlayAttack() => Trigger(AttackHash);
        public void PlaySkill() => Trigger(SkillHash);
        public void PlayHit() => Trigger(HitHash);
        public void PlayJump() => Trigger(JumpHash);

        public void SetRuntuh(bool value)
        {
            if (Ready(RuntuhHash, AnimatorControllerParameterType.Bool))
                animator.SetBool(RuntuhHash, value);
        }

        private void Trigger(int hash)
        {
            if (Ready(hash, AnimatorControllerParameterType.Trigger))
                animator.SetTrigger(hash);
        }

        private bool Ready(int hash, AnimatorControllerParameterType type)
        {
            if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null)
                return false;
            // Avoid "parameter does not exist" warnings for clips the owner has not supplied.
            // Animator.parameters allocates, so cache it per controller.
            if (cachedController != animator.runtimeAnimatorController || cachedParameters == null)
            {
                cachedController = animator.runtimeAnimatorController;
                cachedParameters = animator.parameters;
            }
            foreach (var parameter in cachedParameters)
                if (parameter.nameHash == hash && parameter.type == type) return true;
            return false;
        }
    }
}
