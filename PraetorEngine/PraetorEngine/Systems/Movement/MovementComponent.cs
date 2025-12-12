using PraetorEngine.World;

namespace PraetorEngine.Systems.Movement
{
    /// <summary>
    /// Component storing movement state for entities.
    /// Must be unmanaged for ECS compatibility.
    /// </summary>
    public struct MovementComponent
    {
        public int MovementPoints;
        public int MaxMovementPoints;
        public HexCoord CurrentHex;
        public HexCoord TargetHex;
        public bool IsMoving;

        public MovementComponent(int maxMovementPoints, HexCoord startHex)
        {
            MaxMovementPoints = maxMovementPoints;
            MovementPoints = maxMovementPoints;
            CurrentHex = startHex;
            TargetHex = startHex;
            IsMoving = false;
        }

        /// <summary>
        /// Resets movement points to maximum (called on new turn).
        /// </summary>
        public void ResetMovementPoints()
        {
            MovementPoints = MaxMovementPoints;
        }

        /// <summary>
        /// Checks if entity has enough movement points for a cost.
        /// </summary>
        public readonly bool CanAffordMovement(int cost)
        {
            return MovementPoints >= cost;
        }

        /// <summary>
        /// Consumes movement points.
        /// </summary>
        public void ConsumeMovement(int cost)
        {
            MovementPoints -= cost;
            if (MovementPoints < 0) MovementPoints = 0;
        }
    }
}
