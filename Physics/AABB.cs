using System;
using Microsoft.Xna.Framework;

public class AABB
{
    public Vector2 Min { get; set; }
    public Vector2 Max { get; set; }

    public Vector2 Center => (Min + Max) / 2;

    public AABB(Vector2 min, Vector2 max)
    {
        Min = min;
        Max = max;
    }

    public bool Intersects(AABB other)
    {
        return Min.X < other.Max.X && Max.X > other.Min.X &&
               Min.Y < other.Max.Y && Max.Y > other.Min.Y;
    }

    /// <summary>
    /// Checks if this AABB intersects with another AABB, with tolerance for floating-point precision.
    /// This is more robust for detecting flush (touching) collisions.
    /// </summary>
    /// <param name="other">The other AABB to check against</param>
    /// <param name="tolerance">The tolerance for floating-point precision (default: 0.0001f)</param>
    /// <returns>True if the AABBs intersect or are within tolerance of touching</returns>
    public bool IntersectsWithTolerance(AABB other, float tolerance = 0.0001f)
    {
        return Min.X <= other.Max.X + tolerance && Max.X >= other.Min.X - tolerance &&
               Min.Y <= other.Max.Y + tolerance && Max.Y >= other.Min.Y - tolerance;
    }

    /// <summary>
    /// Checks if this AABB is touching (flush against) another AABB without overlapping.
    /// </summary>
    /// <param name="other">The other AABB to check against</param>
    /// <param name="tolerance">The tolerance for floating-point precision (default: 0.0001f)</param>
    /// <returns>True if the AABBs are touching but not overlapping</returns>
    public bool IsTouching(AABB other, float tolerance = 0.0001f)
    {
        // First check if they're actually overlapping - if so, they're not just touching
        if (Intersects(other)) return false;
        
        // Check if they're flush on any edge
        bool touchingLeft = MathF.Abs(Max.X - other.Min.X) <= tolerance;
        bool touchingRight = MathF.Abs(Min.X - other.Max.X) <= tolerance;
        bool touchingTop = MathF.Abs(Max.Y - other.Min.Y) <= tolerance;
        bool touchingBottom = MathF.Abs(Min.Y - other.Max.Y) <= tolerance;
        
        // Check if they have overlapping ranges on the perpendicular axis
        bool overlapX = Min.X < other.Max.X + tolerance && Max.X > other.Min.X - tolerance;
        bool overlapY = Min.Y < other.Max.Y + tolerance && Max.Y > other.Min.Y - tolerance;
        
        // They're touching if they're flush on one axis and have overlapping ranges on the other
        return ((touchingLeft || touchingRight) && overlapY) || 
               ((touchingTop || touchingBottom) && overlapX);
    }

    public override bool Equals(object obj)
    {
        if (obj is AABB other)
        {
            return Min.Equals(other.Min) && Max.Equals(other.Max);
        }
        return false;
    }

    public override int GetHashCode()
    {
        return Min.GetHashCode() ^ Max.GetHashCode();
    }

    public static bool operator ==(AABB left, AABB right)
    {
        if (ReferenceEquals(left, right))
            return true;
        if (left is null || right is null)
            return false;
        return left.Equals(right);
    }

    public static bool operator !=(AABB left, AABB right)
    {
        return !(left == right);
    }

}