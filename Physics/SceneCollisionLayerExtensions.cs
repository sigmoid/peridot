using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Peridot
{
    /// <summary>
    /// Extension methods for Scene class to handle collision layers
    /// </summary>
    public static class SceneCollisionLayerExtensions
    {
        /// <summary>
        /// Performs a raycast against entities matching specific collision layers
        /// </summary>
        public static RaycastHit RaycastLayers(this Scene scene, Vector2 origin, Vector2 direction, CollisionLayer layerMask, float maxDistance = float.MaxValue)
        {
            float minIntersectionDistance = 1.0f;
            Vector2 hitNormal = Vector2.Zero;
            Entity hitEntity = null;

            foreach (var entity in scene.GetEntities())
            {
                var boxCollider = entity.GetComponent<BoxColliderComponent>();
                if (boxCollider != null && layerMask.Contains(boxCollider.Layer))
                {
                    var hit = Physics.RayIntersectsAABBWithNormal(origin, direction, boxCollider.GetBoundingBox(), maxDistance);
                    if (hit.Distance < minIntersectionDistance && hit.Distance < 1.0f)
                    {
                        minIntersectionDistance = hit.Distance;
                        hitNormal = hit.Normal;
                        hitEntity = entity;
                    }
                }

                var tilemapCollider = entity.GetComponent<TilemapColliderComponent>();
                if (tilemapCollider != null && layerMask.Contains(CollisionLayer.Environment)) // Assume tilemaps are environment
                {
                    var searchRadius = maxDistance + 100;
                    var tilemapAABBs = tilemapCollider.GetAABBs(origin.X, origin.Y, searchRadius);

                    foreach (var aabb in tilemapAABBs)
                    {
                        var hit = Physics.RayIntersectsAABBWithNormal(origin, direction, aabb, maxDistance);
                        if (hit.Distance < minIntersectionDistance && hit.Distance < 1.0f)
                        {
                            minIntersectionDistance = hit.Distance;
                            hitNormal = hit.Normal;
                            hitEntity = entity;
                        }
                    }
                }
            }

            return new RaycastHit(minIntersectionDistance, hitNormal, hitEntity);
        }

        /// <summary>
        /// Performs a boxcast against entities matching specific collision layers
        /// </summary>
        public static RaycastHit BoxCastLayers(this Scene scene, AABB source, Vector2 direction, float distance, CollisionLayer layerMask)
        {
            float minIntersectionDistance = 1.0f;
            Vector2 hitNormal = Vector2.Zero;
            Entity hitEntity = null;

            foreach (var entity in scene.GetEntities())
            {
                var boxCollider = entity.GetComponent<BoxColliderComponent>();
                if (boxCollider != null && boxCollider.GetBoundingBox() != source && layerMask.Contains(boxCollider.Layer))
                {
                    var hit = Physics.BoxCastWithNormal(source, direction, distance, boxCollider.GetBoundingBox());
                    if (hit.Distance < minIntersectionDistance)
                    {
                        minIntersectionDistance = hit.Distance;
                        hitNormal = hit.Normal;
                        hitEntity = entity;
                    }
                }

                var tilemapCollider = entity.GetComponent<TilemapColliderComponent>();
                if (tilemapCollider != null && layerMask.Contains(CollisionLayer.Environment))
                {
                    var sourceBox = source;
                    var searchRadius = distance + System.Math.Max(sourceBox.Max.X - sourceBox.Min.X, sourceBox.Max.Y - sourceBox.Min.Y);
                    var centerX = (sourceBox.Min.X + sourceBox.Max.X) / 2;
                    var centerY = (sourceBox.Min.Y + sourceBox.Max.Y) / 2;

                    var tilemapAABBs = tilemapCollider.GetAABBs(centerX, centerY, searchRadius);

                    foreach (var aabb in tilemapAABBs)
                    {
                        if (aabb == source) continue;

                        var hit = Physics.BoxCastWithNormal(source, direction, distance, aabb);
                        if (hit.Distance < minIntersectionDistance)
                        {
                            minIntersectionDistance = hit.Distance;
                            hitNormal = hit.Normal;
                            hitEntity = entity;
                        }
                    }
                }
            }

            return new RaycastHit(minIntersectionDistance, hitNormal, hitEntity);
        }

        /// <summary>
        /// Resolves collisions with entities matching specific collision layers
        /// </summary>
        public static RaycastHit ResolveCollisionsLayers(this Scene scene, AABB sourceAABB, CollisionLayer layerMask, Entity excludeEntity = null)
        {
            Vector2 totalSeparation = Vector2.Zero;
            Vector2 finalNormal = Vector2.Zero;
            Entity hitEntity = null;
            bool hasCollisions = true;
            int maxIterations = 10;
            int iteration = 0;

            while (hasCollisions && iteration < maxIterations)
            {
                hasCollisions = false;
                iteration++;

                foreach (var entity in scene.GetEntities())
                {
                    if (entity == excludeEntity) continue;

                    var boxCollider = entity.GetComponent<BoxColliderComponent>();
                    if (boxCollider != null && layerMask.Contains(boxCollider.Layer) && boxCollider.IsSolid)
                    {
                        var collision = Physics.ResolveCollisionWithNormal(sourceAABB, boxCollider.GetBoundingBox());
                        if (collision.Distance > 0)
                        {
                            var epsilonDistance = collision.Distance + 0.001f;
                            var separation = collision.Normal * epsilonDistance;

                            totalSeparation += separation;
                            finalNormal = collision.Normal;
                            hitEntity = entity;
                            sourceAABB = new AABB(
                                sourceAABB.Min + separation,
                                sourceAABB.Max + separation
                            );
                            hasCollisions = true;
                        }
                    }

                    var tilemapCollider = entity.GetComponent<TilemapColliderComponent>();
                    if (tilemapCollider != null && layerMask.Contains(CollisionLayer.Environment))
                    {
                        var searchRadius = System.Math.Max(sourceAABB.Max.X - sourceAABB.Min.X, sourceAABB.Max.Y - sourceAABB.Min.Y) * 2;
                        var centerX = (sourceAABB.Min.X + sourceAABB.Max.X) / 2;
                        var centerY = (sourceAABB.Min.Y + sourceAABB.Max.Y) / 2;

                        var tilemapAABBs = tilemapCollider.GetAABBs(centerX, centerY, searchRadius);

                        foreach (var aabb in tilemapAABBs)
                        {
                            var collision = Physics.ResolveCollisionWithNormal(sourceAABB, aabb);
                            if (collision.Distance > 0)
                            {
                                var epsilonDistance = collision.Distance + 0.001f;
                                var separation = collision.Normal * epsilonDistance;

                                totalSeparation += separation;
                                finalNormal = collision.Normal;
                                hitEntity = entity;
                                sourceAABB = new AABB(
                                    sourceAABB.Min + separation,
                                    sourceAABB.Max + separation
                                );
                                hasCollisions = true;
                                break;
                            }
                        }
                    }
                }
            }

            var totalDistance = totalSeparation.Length();
            var totalNormal = totalDistance > 0 ? Vector2.Normalize(totalSeparation) : finalNormal;

            return new RaycastHit(totalDistance, totalNormal, hitEntity);
        }

        /// <summary>
        /// Get all entities in a specific layer
        /// </summary>
        public static List<Entity> GetEntitiesInLayer(this Scene scene, CollisionLayer layer)
        {
            var result = new List<Entity>();
            
            foreach (var entity in scene.GetEntities())
            {
                var boxCollider = entity.GetComponent<BoxColliderComponent>();
                if (boxCollider != null && boxCollider.Layer == layer)
                {
                    result.Add(entity);
                }
            }
            
            return result;
        }

        /// <summary>
        /// Get all entities that match any of the specified layers
        /// </summary>
        public static List<Entity> GetEntitiesInLayers(this Scene scene, CollisionLayer layerMask)
        {
            var result = new List<Entity>();
            
            foreach (var entity in scene.GetEntities())
            {
                var boxCollider = entity.GetComponent<BoxColliderComponent>();
                if (boxCollider != null && layerMask.Contains(boxCollider.Layer))
                {
                    result.Add(entity);
                }
            }
            
            return result;
        }
    }
}
