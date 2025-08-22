using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Peridot;

public struct RaycastHit
{
    public float Distance;
    public Vector2 Normal;
    public Entity EntityHit;

    public RaycastHit(float distance, Vector2 normal, Entity entityHit = null)
    {
        Distance = distance;
        Normal = normal;
        EntityHit = entityHit;
    }   
}

public class Physics
{
    public static float BoxCast(AABB source, Vector2 direction, float distance, AABB dest)
    {
        var hit = BoxCastWithNormal(source, direction, distance, dest);
        return hit.Distance;
    }

    public static RaycastHit BoxCastWithNormal(AABB source, Vector2 direction, float distance, AABB dest)
    {
        // Handle edge cases
        if (distance <= 0f) return new RaycastHit(0f, Vector2.Zero);
        if (direction == Vector2.Zero) return new RaycastHit(0f, Vector2.Zero);

        // Check if AABBs are already overlapping
        if (source.Intersects(dest))
        {
            // If already overlapping, return immediate collision with appropriate normal
            var collision = ResolveCollisionWithNormal(source, dest);
            return new RaycastHit(0f, collision.Normal);
        }

        /// calculate the size of both aabbs
        var sourceSize = source.Max - source.Min;
        var destSize = dest.Max - dest.Min;

        var combinedSize = sourceSize + destSize;

        // create a new aabb centered at dest's center
        var combinedMin = dest.Center - (combinedSize / 2);
        var combinedMax = dest.Center + (combinedSize / 2);
        var combinedAABB = new AABB(combinedMin, combinedMax);

        var hit = RayIntersectsAABBWithNormal(source.Center, direction, combinedAABB, distance);

        return hit;
    }

    // Modified to return a float from 0 to 1 representing the fraction of maxDistance at which intersection occurs
    public static float RayIntersectsAABB(Vector2 origin, Vector2 direction, AABB aabb, float maxDistance = float.MaxValue)
    {
        var hit = RayIntersectsAABBWithNormal(origin, direction, aabb, maxDistance);
        return hit.Distance;
    }

    // Modified to return a RaycastHit with both distance and normal
    public static RaycastHit RayIntersectsAABBWithNormal(Vector2 origin, Vector2 direction, AABB aabb, float maxDistance = float.MaxValue)
    {
        int NUMDIM = 2;

        bool inside = true;
        int[] quadrant = new int[NUMDIM];
        int whichPlane;
        float[] maxT = new float[NUMDIM];
        float[] candidatePlane = new float[NUMDIM];

        int LEFT = 0;
        int RIGHT = 1;
        int MIDDLE = 2;

        /* Find candidate planes; this loop can be avoided if
        rays cast all from the eye(assume perpsective view) */

        if (origin.X < aabb.Min.X)
        {
            quadrant[0] = LEFT;
            candidatePlane[0] = aabb.Min.X;
            inside = false;
        }
        else if (origin.X > aabb.Max.X)
        {
            quadrant[0] = RIGHT;
            candidatePlane[0] = aabb.Max.X;
            inside = false;
        }
        else
        {
            quadrant[0] = MIDDLE;
        }


        if (origin.Y < aabb.Min.Y)
        {
            quadrant[1] = LEFT;
            candidatePlane[1] = aabb.Min.Y;
            inside = false;
        }
        else if (origin.Y > aabb.Max.Y)
        {
            quadrant[1] = RIGHT;
            candidatePlane[1] = aabb.Max.Y;
            inside = false;
        }
        else
        {
            quadrant[1] = MIDDLE;
        }

        /* Ray origin inside bounding box */
        if (inside)
        {
            return new RaycastHit(0f, Vector2.Zero);
        }

        /* Calculate T distances to candidate planes */
        if (quadrant[0] != MIDDLE && direction.X != 0.0f)
            maxT[0] = (candidatePlane[0] - origin.X) / direction.X;
        else
            maxT[0] = -1.0f;

        if (quadrant[1] != MIDDLE && direction.Y != 0.0f)
            maxT[1] = (candidatePlane[1] - origin.Y) / direction.Y;
        else
            maxT[1] = -1.0f;

        /* Get largest of the maxT's for final choice of intersection */
        whichPlane = 0;
        if (maxT[whichPlane] < maxT[0])
            whichPlane = 0;
        if (maxT[whichPlane] < maxT[1])
            whichPlane = 1;

        /* Check final candidate actually inside box */
        if (maxT[whichPlane] < 0.0f) return new RaycastHit(1f, Vector2.Zero);

        /* Check if intersection is within the maximum distance */
        if (maxT[whichPlane] > maxDistance) return new RaycastHit(1f, Vector2.Zero);

        Vector2 intersectionPoint = Vector2.Zero;

        if (whichPlane != 0)
        {
            intersectionPoint.X = origin.X + maxT[whichPlane] * direction.X;
            if (intersectionPoint.X < aabb.Min.X || intersectionPoint.X > aabb.Max.X)
                return new RaycastHit(1f, Vector2.Zero);
        }
        else
        {
            intersectionPoint.X = candidatePlane[0];
        }

        if (whichPlane != 1)
        {
            intersectionPoint.Y = origin.Y + maxT[whichPlane] * direction.Y;
            if (intersectionPoint.Y < aabb.Min.Y || intersectionPoint.Y > aabb.Max.Y)
                return new RaycastHit(1f, Vector2.Zero);
        }
        else
        {
            intersectionPoint.Y = candidatePlane[1];
        }

        // Calculate the normal based on which plane we hit
        Vector2 normal = Vector2.Zero;
        if (whichPlane == 0) // Hit vertical plane (left or right)
        {
            normal.X = (quadrant[0] == LEFT) ? -1f : 1f;
            normal.Y = 0f;
        }
        else if (whichPlane == 1) // Hit horizontal plane (top or bottom)
        {
            normal.X = 0f;
            normal.Y = (quadrant[1] == LEFT) ? -1f : 1f;
        }

        var intersectionDistance = maxT[whichPlane] / maxDistance;

        // Return the fraction of maxDistance at which intersection occurs and the normal
        return new RaycastHit(intersectionDistance, normal);
    }

    /// <summary>
    /// Resolves collision between two overlapping AABBs by calculating the minimum translation vector (MTV)
    /// to separate them. The MTV represents the shortest distance and direction to move the first AABB
    /// to resolve the collision.
    /// 
    /// Example usage:
    /// var separation = Physics.ResolveCollision(playerAABB, wallAABB);
    /// if (separation != Vector2.Zero)
    /// {
    ///     playerEntity.Position += separation; // Move player out of collision
    /// }
    /// </summary>
    /// <param name="aabb1">The first AABB (the one that will be moved)</param>
    /// <param name="aabb2">The second AABB (stationary reference)</param>
    /// <returns>A Vector2 representing the direction and distance to move aabb1 to separate from aabb2. 
    /// Returns Vector2.Zero if AABBs are not overlapping.</returns>
    public static Vector2 ResolveCollision(AABB aabb1, AABB aabb2)
    {
        // Check if AABBs are actually overlapping or touching
        if (!aabb1.Intersects(aabb2) && !aabb1.IsTouching(aabb2))
            return Vector2.Zero;

        // Calculate overlap on both axes
        float overlapX = Math.Min(aabb1.Max.X, aabb2.Max.X) - Math.Max(aabb1.Min.X, aabb2.Min.X);
        float overlapY = Math.Min(aabb1.Max.Y, aabb2.Max.Y) - Math.Max(aabb1.Min.Y, aabb2.Min.Y);

        // For touching AABBs, the overlap might be 0 or very small - add a small separation
        const float minSeparation = 0.001f;
        if (overlapX <= 0) overlapX = minSeparation;
        if (overlapY <= 0) overlapY = minSeparation;

        // Find the axis with minimum overlap (this is our separation direction)
        if (overlapX < overlapY)
        {
            // Horizontal separation is shorter
            float direction = (aabb1.Center.X < aabb2.Center.X) ? -1f : 1f;
            return new Vector2(direction * overlapX, 0f);
        }
        else
        {
            // Vertical separation is shorter
            float direction = (aabb1.Center.Y < aabb2.Center.Y) ? -1f : 1f;
            return new Vector2(0f, direction * overlapY);
        }
    }

    /// <summary>
    /// Resolves collision between two overlapping AABBs and returns detailed separation information.
    /// </summary>
    /// <param name="aabb1">The first AABB (the one that will be moved)</param>
    /// <param name="aabb2">The second AABB (stationary reference)</param>
    /// <returns>A RaycastHit where Distance is the separation distance and Normal is the separation direction.
    /// Returns distance 0 and zero normal if AABBs are not overlapping.</returns>
    public static RaycastHit ResolveCollisionWithNormal(AABB aabb1, AABB aabb2)
    {
        // Check if AABBs are actually overlapping or touching
        if (!aabb1.Intersects(aabb2) && !aabb1.IsTouching(aabb2))
            return new RaycastHit(0f, Vector2.Zero);

        // Calculate overlap on both axes
        float overlapX = Math.Min(aabb1.Max.X, aabb2.Max.X) - Math.Max(aabb1.Min.X, aabb2.Min.X);
        float overlapY = Math.Min(aabb1.Max.Y, aabb2.Max.Y) - Math.Max(aabb1.Min.Y, aabb2.Min.Y);

        // For touching AABBs, the overlap might be 0 or very small - add a small separation
        const float minSeparation = 0.001f;
        if (overlapX <= 0) overlapX = minSeparation;
        if (overlapY <= 0) overlapY = minSeparation;

        // Find the axis with minimum overlap (this is our separation direction)
        if (overlapX < overlapY)
        {
            // Horizontal separation is shorter
            Vector2 normal = (aabb1.Center.X < aabb2.Center.X) ? new Vector2(-1f, 0f) : new Vector2(1f, 0f);
            return new RaycastHit(overlapX, normal);
        }
        else
        {
            // Vertical separation is shorter
            Vector2 normal = (aabb1.Center.Y < aabb2.Center.Y) ? new Vector2(0f, -1f) : new Vector2(0f, 1f);
            return new RaycastHit(overlapY, normal);
        }
    }
}