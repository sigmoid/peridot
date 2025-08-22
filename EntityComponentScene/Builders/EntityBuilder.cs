using System.Numerics;
using Peridot;
using Peridot.EntityComponentScene.Serialization;

public class EntityBuilderComponentProperty
{
    public string Type { get; set; }
    public string Name { get; set; }
    public string Value { get; set; }
}

public class EntityBuilder
{
    private Vector2 _position;
    private string _components;
    private string _name;

    public EntityBuilder()
    {
        _components = string.Empty;
    }

    public EntityBuilder WithComponent(string componentType, params EntityBuilderComponentProperty[] properties)
    {

        _components += $"<Component Type=\"{componentType}\">";

        foreach (var property in properties)
        {
            _components += $"<Property Name=\"{property.Name}\" Type=\"{property.Type}\" Value=\"{property.Value}\" />";
        }

        _components += "</Component>";

        return this;
    }
    public EntityBuilder WithName(string name)
    {
        _name = name;
        return this;
    }
    public EntityBuilder WithPosition(float x, float y)
    {
        _position = new Vector2(x, y);
        return this;
    }

    public Entity Build()
    {
        var text = "<?xml version=\"1.0\" encoding=\"utf-8\"?><Entity Name=\"" + _name + "\">" + "<Position><X>" + _position.X + "</X><Y>" + _position.Y + "</Y></Position>" + _components + "<Children></Children></Entity>";
        return EntityFactory.FromString(text);
    }
}
