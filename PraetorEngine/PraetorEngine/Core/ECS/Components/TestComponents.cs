using Microsoft.Xna.Framework;

namespace PraetorEngine.Core.ECS.Components
{
    /// <summary>
    /// Position component for 2D transform.
    /// </summary>
    public struct PositionComponent
    {
        public Vector2 Position;
        public float Rotation;

        public PositionComponent(float x, float y, float rotation = 0f)
        {
            Position = new Vector2(x, y);
            Rotation = rotation;
        }
    }

    /// <summary>
    /// Velocity component for movement.
    /// </summary>
    public struct VelocityComponent
    {
        public Vector2 Velocity;

        public VelocityComponent(float x, float y)
        {
            Velocity = new Vector2(x, y);
        }
    }

    /// <summary>
    /// Health component for entities.
    /// </summary>
    public struct HealthComponent
    {
        public int Current;
        public int Maximum;

        public HealthComponent(int current, int maximum)
        {
            Current = current;
            Maximum = maximum;
        }

        public bool IsAlive => Current > 0;
        public float HealthPercent => Maximum > 0 ? (float)Current / Maximum : 0f;
    }
}
