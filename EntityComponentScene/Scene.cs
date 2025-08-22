using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using Peridot.EntityComponentScene.Serialization;

namespace Peridot;

/// <summary>
/// A scene that contains entities and manages their lifecycle
/// </summary>
public class Scene
{
    private readonly List<Entity> _entities = new List<Entity>();
    private List<Entity> _entityQueue = new List<Entity>();
    private List<Entity> _removedEntitiesQueue = new List<Entity>();

    public static Scene FromFile(ContentManager content, string fileName)
    {
        var res = new Scene();

        string filePath = Path.Combine(content.RootDirectory, fileName);

        using (Stream stream = TitleContainer.OpenStream(filePath))
        {
            using (XmlReader reader = XmlReader.Create(stream))
            {
                XDocument doc = XDocument.Load(reader);
                XElement root = doc.Root;

                // Load entities from the XML
                var entityElements = root.Element("Entities")?.Elements("Entity");

                if (entityElements != null)
                {
                    foreach (var entityElement in entityElements)
                    {
                        try
                        {
                            // Use EntityFactory to handle both inline entities and file references
                            var entity = EntityFactory.FromXElement(entityElement, content.RootDirectory);
                            res._entities.Add(entity);
                        }
                        catch (Exception ex)
                        {
                            // Log the error and continue with other entities
                            Console.WriteLine($"Failed to load entity: {ex.Message}");
                        }
                    }
                }
            }
        }

        return res;
    }

    public void SerializeToFile(string filePath)
    {
        var root = new XElement("Scene");

        var entitiesElement = new XElement("Entities");
        foreach (var entity in _entities)
        {
            entitiesElement.Add(entity.Serialize());
        }
        root.Add(entitiesElement);

        XDocument doc = new XDocument(root);
        doc.Save(filePath);
    }

    public float BoxCastAll(AABB source, Vector2 direction, float distance, CollisionLayer layerMask = CollisionLayer.All)
    {
        var hit = BoxCastAllWithNormal(source, direction, distance, layerMask);
        return hit.Distance;
    }

    public RaycastHit BoxCastAllWithNormal(AABB source, Vector2 direction, float distance, CollisionLayer layerMask = CollisionLayer.All)
    {
        float minIntersectionDistance = 1.0f;
        Vector2 hitNormal = Vector2.Zero;
        Entity hitEntity = null;

        foreach (var entity in _entities)
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
    /// Performs a raycast against all entities in the scene
    /// </summary>
    /// <param name="origin">The starting point of the ray</param>
    /// <param name="direction">The direction of the ray (should be normalized)</param>
    /// <param name="maxDistance">The maximum distance to cast the ray</param>
    /// <param name="layerMask">Optional collision layer mask to filter which entities to check (default: all layers)</param>
    /// <returns>A RaycastHit containing the distance, normal, and entity hit (if any)</returns>
    public RaycastHit Raycast(Vector2 origin, Vector2 direction, float maxDistance = float.MaxValue, CollisionLayer layerMask = CollisionLayer.All)
    {
        float minIntersectionDistance = 1.0f; // Start with full distance possible
        Vector2 hitNormal = Vector2.Zero;
        Entity hitEntity = null;

        foreach (var entity in _entities)
        {
            // Check for regular box colliders
            var boxCollider = entity.GetComponent<BoxColliderComponent>();
            if (boxCollider != null && layerMask.Contains(boxCollider.Layer))
            {
                var hit = Physics.RayIntersectsAABBWithNormal(origin, direction, boxCollider.GetBoundingBox(), maxDistance);
                {
                    minIntersectionDistance = hit.Distance;
                    hitNormal = hit.Normal;
                    hitEntity = entity;
                }
            }

            // Check for tilemap colliders - assume they are Environment layer
            var tilemapCollider = entity.GetComponent<TilemapColliderComponent>();
            if (tilemapCollider != null && layerMask.Contains(CollisionLayer.Environment))
            {
                // Get nearby AABBs from the tilemap using the ray origin as reference
                var searchRadius = maxDistance + 100; // Add some buffer for tile size
                var tilemapAABBs = tilemapCollider.GetAABBs(origin.X, origin.Y, searchRadius);

                foreach (var aabb in tilemapAABBs)
                {
                    var hit = Physics.RayIntersectsAABBWithNormal(origin, direction, aabb, maxDistance);
                    if (hit.Distance < minIntersectionDistance && hit.Distance < 1.0f) // Only consider actual hits
                    {
                        minIntersectionDistance = hit.Distance;
                        hitNormal = hit.Normal;
                        hitEntity = entity; // The entity with the tilemap collider
                    }
                }
            }
        }

        return new RaycastHit(minIntersectionDistance, hitNormal, hitEntity);
    }

    /// <summary>
    /// Performs a raycast against all entities in the scene and returns just the distance
    /// </summary>
    /// <param name="origin">The starting point of the ray</param>
    /// <param name="direction">The direction of the ray (should be normalized)</param>
    /// <param name="maxDistance">The maximum distance to cast the ray</param>
    /// <param name="layerMask">Optional collision layer mask to filter which entities to check (default: all layers)</param>
    /// <returns>The distance to the first hit, or 1.0f if no hit</returns>
    public float RaycastDistance(Vector2 origin, Vector2 direction, float maxDistance = float.MaxValue, CollisionLayer layerMask = CollisionLayer.All)
    {
        var hit = Raycast(origin, direction, maxDistance, layerMask);
        return hit.Distance;
    }

    /// <summary>
    /// Resolves collision between a source AABB and all entities in the scene.
    /// Returns the minimum translation vector needed to separate the source from all overlapping entities.
    /// </summary>
    /// <param name="sourceAABB">The AABB to resolve collisions for</param>
    /// <param name="excludeEntity">Optional entity to exclude from collision resolution (e.g., the entity that owns the sourceAABB)</param>
    /// <param name="layerMask">Optional collision layer mask to filter which entities to check (default: all layers)</param>
    /// <returns>A Vector2 representing the minimum translation needed to resolve all collisions, or Vector2.Zero if no collisions</returns>
    public Vector2 ResolveCollisions(AABB sourceAABB, Entity excludeEntity = null, CollisionLayer layerMask = CollisionLayer.All)
    {
        Vector2 totalSeparation = Vector2.Zero;
        bool hasCollisions = true;
        int maxIterations = 10; // Prevent infinite loops
        int iteration = 0;

        // Keep resolving collisions until no more overlaps exist or max iterations reached
        while (hasCollisions && iteration < maxIterations)
        {
            hasCollisions = false;
            iteration++;

            foreach (var entity in _entities)
            {
                // Skip the excluded entity
                if (entity == excludeEntity) continue;

                // Check for regular box colliders
                var boxCollider = entity.GetComponent<BoxColliderComponent>();
                if (boxCollider != null && layerMask.Contains(boxCollider.Layer))
                {
                    var separation = Physics.ResolveCollision(sourceAABB, boxCollider.GetBoundingBox());
                    if (separation != Vector2.Zero)
                    {
                        // Add a small epsilon to move slightly past the collision point
                        var separationDirection = Vector2.Normalize(separation);
                        var epsilonSeparation = separation + separationDirection * 0.001f;

                        totalSeparation += epsilonSeparation;
                        sourceAABB = new AABB(
                            sourceAABB.Min + epsilonSeparation,
                            sourceAABB.Max + epsilonSeparation
                        );
                        hasCollisions = true;
                        entity.OnCollision(excludeEntity);
                        excludeEntity.OnCollision(entity);
                    }
                }

                // Check for tilemap colliders - assume they are Environment layer
                var tilemapCollider = entity.GetComponent<TilemapColliderComponent>();
                if (tilemapCollider != null && layerMask.Contains(CollisionLayer.Environment))
                {
                    // Get nearby AABBs from the tilemap
                    var searchRadius = Math.Max(sourceAABB.Max.X - sourceAABB.Min.X, sourceAABB.Max.Y - sourceAABB.Min.Y) * 2;
                    var centerX = (sourceAABB.Min.X + sourceAABB.Max.X) / 2;
                    var centerY = (sourceAABB.Min.Y + sourceAABB.Max.Y) / 2;

                    var tilemapAABBs = tilemapCollider.GetAABBs(centerX, centerY, searchRadius);

                    foreach (var aabb in tilemapAABBs)
                    {
                        var separation = Physics.ResolveCollision(sourceAABB, aabb);
                        if (separation != Vector2.Zero)
                        {
                            // Add a small epsilon to move slightly past the collision point
                            var separationDirection = Vector2.Normalize(separation);
                            var epsilonSeparation = separation + separationDirection * 0.001f;

                            totalSeparation += epsilonSeparation;
                            sourceAABB = new AABB(
                                sourceAABB.Min + epsilonSeparation,
                                sourceAABB.Max + epsilonSeparation
                            );
                            hasCollisions = true;
                            entity.OnCollision(excludeEntity);
                            excludeEntity.OnCollision(entity);
                            break; // Process one tilemap collision at a time
                        }
                    }
                }
            }
        }

        return totalSeparation;
    }

    /// <summary>
    /// Resolves collision between a source AABB and all entities in the scene, returning detailed collision information.
    /// </summary>
    /// <param name="sourceAABB">The AABB to resolve collisions for</param>
    /// <param name="excludeEntity">Optional entity to exclude from collision resolution</param>
    /// <param name="layerMask">Optional collision layer mask to filter which entities to check (default: all layers)</param>
    /// <returns>A RaycastHit with the total separation distance, final separation direction, and the entity that caused the collision</returns>
    public RaycastHit ResolveCollisionsWithNormal(AABB sourceAABB, Entity excludeEntity = null, CollisionLayer layerMask = CollisionLayer.All)
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

            foreach (var entity in _entities)
            {
                if (entity == excludeEntity) continue;

                // Check for regular box colliders
                var boxCollider = entity.GetComponent<BoxColliderComponent>();
                if (boxCollider != null && layerMask.Contains(boxCollider.Layer))
                {
                    var collision = Physics.ResolveCollisionWithNormal(sourceAABB, boxCollider.GetBoundingBox());
                    if (collision.Distance > 0)
                    {
                        // Add a small epsilon to move slightly past the collision point
                        var epsilonDistance = collision.Distance + 0.001f;
                        var separation = collision.Normal * epsilonDistance;

                        totalSeparation += separation;
                        finalNormal = collision.Normal; // Keep the most recent collision normal
                        hitEntity = entity; // Track the entity that caused the collision
                        sourceAABB = new AABB(
                            sourceAABB.Min + separation,
                            sourceAABB.Max + separation
                        );
                        hasCollisions = true;
                    }
                }

                // Check for tilemap colliders - assume they are Environment layer
                var tilemapCollider = entity.GetComponent<TilemapColliderComponent>();
                if (tilemapCollider != null && layerMask.Contains(CollisionLayer.Environment))
                {
                    var searchRadius = Math.Max(sourceAABB.Max.X - sourceAABB.Min.X, sourceAABB.Max.Y - sourceAABB.Min.Y) * 2;
                    var centerX = (sourceAABB.Min.X + sourceAABB.Max.X) / 2;
                    var centerY = (sourceAABB.Min.Y + sourceAABB.Max.Y) / 2;

                    var tilemapAABBs = tilemapCollider.GetAABBs(centerX, centerY, searchRadius);

                    foreach (var aabb in tilemapAABBs)
                    {
                        var collision = Physics.ResolveCollisionWithNormal(sourceAABB, aabb);
                        if (collision.Distance > 0)
                        {
                            // Add a small epsilon to move slightly past the collision point
                            var epsilonDistance = collision.Distance + 0.001f;
                            var separation = collision.Normal * epsilonDistance;

                            totalSeparation += separation;
                            finalNormal = collision.Normal;
                            hitEntity = entity; // The entity with the tilemap collider
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
    /// Add an entity to the scene
    /// </summary>
    /// <param name="entity">The entity to add</param>
    public void AddEntity(Entity entity)
    {
        _entityQueue.Add(entity);
    }

    /// <summary>
    /// Remove an entity from the scene
    /// </summary>
    /// <param name="entity">The entity to remove</param>
    public void RemoveEntity(Entity entity)
    {
        _removedEntitiesQueue.Add(entity);
    }

    /// <summary>
    /// Load an entity from an XML file and add it to the scene
    /// </summary>
    /// <param name="content">The content manager</param>
    /// <param name="assetName">The name of the XML asset (without .xml extension)</param>
    /// <returns>The loaded entity</returns>
    public Entity LoadEntity(ContentManager content, string assetName)
    {
        var entity = Entity.FromFile(content, assetName);
        AddEntity(entity);
        return entity;
    }

    /// <summary>
    /// Load an entity from an XML file using the full relative path and add it to the scene
    /// </summary>
    /// <param name="content">The content manager</param>
    /// <param name="assetPath">The full relative path to the XML asset</param>
    /// <returns>The loaded entity</returns>
    public Entity LoadEntityFromContent(ContentManager content, string assetPath)
    {
        var entity = Entity.FromContentFile(content, assetPath);
        AddEntity(entity);
        return entity;
    }

    /// <summary>
    /// Load an entity from an XML file path and add it to the scene
    /// </summary>
    /// <param name="xmlFilePath">The path to the XML file</param>
    /// <returns>The loaded entity</returns>
    public Entity LoadEntityFromFile(string xmlFilePath)
    {
        var entity = Entity.FromXmlFile(xmlFilePath);
        AddEntity(entity);
        return entity;
    }

    public Entity FindEntityByName(string name)
    {
        return _entities.Find(e => e.Name == name);
    }

    /// <summary>
    /// Load a scene from an XML file (instance method for existing scenes)
    /// </summary>
    /// <param name="filePath">The path to the XML file</param>
    public void LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Scene file not found: {filePath}");
        }

        // Clear existing entities
        Clear();

        // Load from XML
        using (var reader = new FileStream(filePath, FileMode.Open))
        {
            var doc = XDocument.Load(reader);
            var root = doc.Root;

            // Load entities from the XML
            var entityElements = root.Element("Entities")?.Elements("Entity");

            if (entityElements != null)
            {
                foreach (var entityElement in entityElements)
                {
                    try
                    {
                        // Use EntityFactory to handle both inline entities and file references
                        var entity = EntityFactory.FromXElement(entityElement, Path.GetDirectoryName(filePath));
                        _entities.Add(entity);
                        entity.Initialize();
                    }
                    catch (Exception ex)
                    {
                        // Log the error and continue with other entities
                        Logger.Warning($"Failed to load entity: {ex.Message}");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Update all entities in the scene
    /// </summary>
    /// <param name="gameTime">The game time</param>
    public void Update(GameTime gameTime)
    {
        PipeEntityQueue();
        foreach (var entity in _entities)
        {
            entity.Update(gameTime);
        }
        PipeEntityQueue();

        foreach (var entity in _entities)
        {
            var rigidbody = entity.GetComponent<RigidbodyComponent>();

            if (rigidbody != null && !rigidbody.IsStatic)
            {
                var boxCollider = entity.GetComponent<BoxColliderComponent>();
                if (boxCollider != null)
                {
                    // Use the entity's collision mask to determine what it should collide with
                    var resolve = ResolveCollisions(boxCollider.GetBoundingBox(), entity, boxCollider.CollisionMask);
                    if (resolve != Vector2.Zero)
                    {
                        entity.Position += resolve; // Move the player out of collision
                    }
                }
            }
        }
    }

    /// <summary>
    /// Draw all entities in the scene
    /// </summary>
    /// <param name="spriteBatch">The sprite batch to draw with</param>
    public void Draw(SpriteBatch spriteBatch)
    {
        foreach (var entity in _entities)
        {
            entity.Draw(spriteBatch);
        }
    }

    /// <summary>
    /// Get all entities in the scene
    /// </summary>
    /// <returns>A list of all entities</returns>
    public List<Entity> GetEntities()
    {
        return new List<Entity>(_entities);
    }

    /// <summary>
    /// Clear all entities from the scene
    /// </summary>
    public void Clear()
    {
        foreach (var entity in _entities)
        {
            entity.Cleanup();
        }
        _entities.Clear();
    }

    public void InitializeEntities()
    {
        foreach (var entity in _entities)
        {
            entity.Initialize();
        }
    }

    private void PipeEntityQueue()
    {
        while (_entityQueue.Count > 0)
        {
            var entity = _entityQueue[0];
            _entityQueue.RemoveAt(0);
            _entities.Add(entity);
            entity.Initialize();
        }

        while (_removedEntitiesQueue.Count > 0)
        {
            var entity = _removedEntitiesQueue[0];
            _removedEntitiesQueue.RemoveAt(0);
            if (_entities.Remove(entity))
            {
                entity.Cleanup();
            }
        }
    }
}
