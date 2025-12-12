namespace PraetorEngine.Core.ECS
{
    /// <summary>
    /// Marker interface for ECS components.
    /// Components must be unmanaged (value types with no references) for performance.
    /// This ensures components can be stored in contiguous arrays for cache efficiency.
    /// Note: This is a marker interface. The unmanaged constraint is enforced at usage sites.
    /// </summary>
    public interface IComponent
    {
    }
}
