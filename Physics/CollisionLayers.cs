using System;

namespace Peridot
{
    /// <summary>
    /// Defines collision layers for entities. Each layer is a power of 2 to allow bitwise operations.
    /// </summary>
    [Flags]
    public enum CollisionLayer : uint
    {
        None = 0,
        Default = 1 << 0,       // General objects
        Player = 1 << 1,        // Player entities
        Enemy = 1 << 2,         // Enemy entities
        Projectile = 1 << 3,    // Bullets, missiles, etc.
        Environment = 1 << 4,   // Walls, platforms, etc.
        Pickup = 1 << 5,        // Items, power-ups, etc.
        Trigger = 1 << 6,       // Trigger zones, sensors
        UI = 1 << 7,            // UI elements (if needed for physics)
        Debris = 1 << 8,        // Destructible objects, particles
        Vehicle = 1 << 9,       // Cars, ships, etc.
        // Add more layers as needed (up to 32 total)
        
        // Convenience combinations
        All = uint.MaxValue,
        Solid = Default | Environment,
        Characters = Player | Enemy,
        Collectibles = Pickup | Debris
    }

    /// <summary>
    /// Utility class for working with collision layers
    /// </summary>
    public static class CollisionLayerExtensions
    {
        /// <summary>
        /// Check if two layers can collide with each other
        /// </summary>
        public static bool CanCollideWith(this CollisionLayer layer1, CollisionLayer layer2)
        {
            return (layer1 & layer2) != 0;
        }

        /// <summary>
        /// Check if a layer contains another layer
        /// </summary>
        public static bool Contains(this CollisionLayer mask, CollisionLayer layer)
        {
            return (mask & layer) == layer;
        }

        /// <summary>
        /// Add a layer to a mask
        /// </summary>
        public static CollisionLayer Add(this CollisionLayer mask, CollisionLayer layer)
        {
            return mask | layer;
        }

        /// <summary>
        /// Remove a layer from a mask
        /// </summary>
        public static CollisionLayer Remove(this CollisionLayer mask, CollisionLayer layer)
        {
            return mask & ~layer;
        }
    }
}
