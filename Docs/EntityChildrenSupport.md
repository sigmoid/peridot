# Entity Children and File Loading Support

The EntityFactory now supports loading entities with children and referencing entities from external files. This allows for more complex entity hierarchies with **relative positioning** and better organization of entity definitions.

## Features Added

### 1. Children Entities with Relative Positioning

Entities can now have child entities defined directly in their XML or loaded from external files. **Children positions are relative to their parent**, making it easy to create complex hierarchical structures that move together as a unit.

### 2. File-Based Entity References

You can reference entities defined in other XML files using the `<EntityFromFile>` element, promoting reusability and modular design.

### 3. Relative Path Resolution

When loading entities from files, relative paths are resolved relative to the parent entity's directory, making it easy to organize related entities in folders.

### 4. Hierarchical Position Management

- **LocalPosition**: Position relative to parent entity
- **Position**: World position (calculated from parent hierarchy)
- **Parent**: Reference to parent entity (null if root entity)

## Key Benefits of Relative Positioning

- **Coherent Movement**: When a parent moves, all children move with it automatically
- **Easier Design**: Define child positions relative to parent without worrying about world coordinates
- **Nested Hierarchies**: Support for grandchildren and deeper nesting levels
- **Modular Components**: Attach sub-components that maintain their relative positions

## XML Structure

### Basic Children with Relative Positioning
```xml
<Entity>
  <Position>
    <X>100</X>
    <Y>200</Y>
  </Position>
  <Component Type="SomeComponent"/>
  <Children>
    <!-- Child at position (10, 0) relative to parent -->
    <Entity>
      <Position>
        <X>10</X>
        <Y>0</Y>
      </Position>
      <Component Type="ChildComponent"/>
    </Entity>
    
    <!-- Child loaded from file with relative position override -->
    <EntityFromFile Path="child_entity.xml">
      <Position>
        <X>-10</X>
        <Y>0</Y>
      </Position>
    </EntityFromFile>
  </Children>
</Entity>
```

### Nested Hierarchies with Relative Positioning
```xml
<Entity>
  <Position><X>100</X><Y>200</Y></Position>
  <Children>
    <!-- Gun attached to vehicle -->
    <Entity>
      <Position><X>25</X><Y>0</Y></Position>
      <Component Type="GunComponent"/>
      <Children>
        <!-- Muzzle flash relative to gun -->
        <Entity>
          <Position><X>5</X><Y>0</Y></Position>
          <Component Type="MuzzleFlashComponent"/>
        </Entity>
      </Children>
    </Entity>
  </Children>
</Entity>
```

In this example:
- Vehicle is at world position (100, 200)
- Gun is at world position (125, 200) - parent + local (100+25, 200+0)
- Muzzle flash is at world position (130, 200) - grandparent + parent local + local (100+25+5, 200+0+0)

### EntityFromFile Element

The `<EntityFromFile>` element allows you to reference an entity defined in another XML file:

- `Path` attribute: Specifies the path to the XML file containing the entity definition
- Optional `<Position>` element: Overrides the position defined in the referenced file

## Code Changes

### Entity.cs
- Added `Parent` property to track parent-child relationships
- Added `LocalPosition` property for relative positioning
- Modified `Position` property to calculate world position from parent hierarchy
- Enhanced `AddChild` and `RemoveChild` methods to manage parent references
- Added utility methods: `GetWorldPosition()`, `SetWorldPosition()`, `Move()`
- Enhanced `Initialize`, `Update`, and `Draw` methods to handle children

### EntityDefinition.cs
- Added `Children` property to support child entities
- Added `EntityFromFileDefinition` class for file references

### EntityFactory.cs
- Modified entity creation to use `LocalPosition` for children
- Enhanced `FromDefinition()` method to handle children recursively
- Added relative path resolution for file-based entities
- Improved error handling and documentation

## Usage Examples

### Loading Entity with Relative Positioning
```csharp
// Load entity that may have children
var entity = EntityFactory.FromContentFile(content, "entities/vehicle_entity.xml");

// Access children and their positions
var children = entity.GetChildren();
foreach (var child in children)
{
    Console.WriteLine($"Child world position: {child.Position}");
    Console.WriteLine($"Child local position: {child.LocalPosition}");
}
```

### Working with Relative Positions
```csharp
// Create parent and child entities
var parent = new Entity();
parent.LocalPosition = new Vector2(100, 200);

var child = new Entity();
child.LocalPosition = new Vector2(10, 5);

// Add child to parent - child's world position becomes (110, 205)
parent.AddChild(child);

// Moving parent moves all children
parent.Move(new Vector2(50, 100)); // Parent now at (150, 300)
// Child world position automatically becomes (160, 305)

// Moving child locally
child.LocalPosition = new Vector2(20, 10);
// Child world position becomes (170, 310)
```

### Utility Methods
```csharp
// Get world position
Vector2 worldPos = entity.GetWorldPosition();

// Set world position (automatically calculates local position if has parent)
entity.SetWorldPosition(new Vector2(200, 300));

// Move by offset (affects local position)
entity.Move(new Vector2(10, 20));
```

### Lifecycle Management
```csharp
// Children are automatically managed in lifecycle methods
entity.Initialize(); // Initializes entity and all children
entity.Update(gameTime); // Updates entity and all children
entity.Draw(spriteBatch); // Draws entity and all children
```

## Benefits

1. **Hierarchical Organization**: Build complex entities from simpler components with relative positioning
2. **Coherent Movement**: Parent and children move together as a unified system
3. **Reusability**: Share common sub-entities across multiple parent entities
4. **Maintainability**: Keep related entities in separate files for better organization
5. **Modularity**: Mix inline and file-based children as needed
6. **Intuitive Design**: Define child positions relative to parent without complex calculations
7. **Nested Hierarchies**: Support for grandchildren and deeper nesting levels

## Position System Details

### Properties
- `LocalPosition`: Position relative to parent (or world if no parent)
- `Position`: Calculated world position (parent.Position + LocalPosition)
- `Parent`: Reference to parent entity (null for root entities)

### Behavior
- When a parent moves, all children automatically move with it
- Children can be repositioned locally without affecting siblings
- Removing a child resets its Parent to null
- World position is calculated on-demand from the hierarchy

### Example Hierarchy
```
Vehicle (100, 200)
├── Gun (125, 200)        [Local: (25, 0)]
│   └── Muzzle (130, 200) [Local: (5, 0)]
└── Thruster (85, 190)    [Local: (-15, -10)]
```

When Vehicle moves to (300, 400):
```
Vehicle (300, 400)
├── Gun (325, 400)        [Local: (25, 0)] - unchanged
│   └── Muzzle (330, 400) [Local: (5, 0)]  - unchanged
└── Thruster (285, 390)   [Local: (-15, -10)] - unchanged
```

## File Organization Example

```
Content/
  entities/
    player_entity.xml      (references weapon_entity.xml)
    vehicle_entity.xml     (references weapon_entity.xml)
    weapon_entity.xml      (reusable weapon definition)
    effects/
      explosion.xml
      smoke.xml
```

This structure allows for easy sharing of common entities while maintaining clear organization of related components.
