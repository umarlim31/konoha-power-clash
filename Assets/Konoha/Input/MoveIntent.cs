using UnityEngine;

namespace Konoha.Input
{
    public readonly struct MoveIntent
    {
        public readonly Vector2 Axis;
        public MoveIntent(Vector2 axis) { Axis = Vector2.ClampMagnitude(axis, 1f); }
        public Vector3 WorldDirection => new Vector3(Axis.x, 0f, Axis.y);
    }
}
