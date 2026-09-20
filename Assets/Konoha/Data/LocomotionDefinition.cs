using UnityEngine;

namespace Konoha.Data
{
    [CreateAssetMenu(menuName = "Konoha/Spike Locomotion")]
    public sealed class LocomotionDefinition : ScriptableObject
    {
        // Greybox tuning only; not a final hero balance definition.
        [Min(0.1f)] public float speed = 6f;
        [Min(0.1f)] public float turnDegreesPerSecond = 720f;
        public float gravity = -20f;
        public float groundStickSpeed = -2f;
    }
}
