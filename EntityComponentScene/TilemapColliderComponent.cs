using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Peridot;
using Peridot.Components;

public class TilemapColliderComponent : Component
{
    private int _width;
    private int _height;

    private int _tileSize;

    private bool[,] _collisions;

    public TilemapColliderComponent()
    {
        // Default constructor for serialization or other purposes
    }

    public TilemapColliderComponent(int width, int height, int tileSize)
    {
        _tileSize = tileSize;
        _width = width;
        _height = height;
        _collisions = new bool[width, height];
    }

    public override XElement Serialize()
    {
        var element = new XElement("Component",
            new XAttribute("Type", "TilemapColliderComponent"));

        element.Add(new XElement("Property",
            new XAttribute("Name", "Width"),
            new XAttribute("Value", _width.ToString()),
            new XAttribute("Type", "int")));

        element.Add(new XElement("Property",
            new XAttribute("Name", "Height"),
            new XAttribute("Value", _height.ToString()),
            new XAttribute("Type", "int")));

        element.Add(new XElement("Property",
            new XAttribute("Name", "TileSize"),
            new XAttribute("Value", _tileSize.ToString()),
            new XAttribute("Type", "int")));

        return element;
    }

    public override void Initialize()
    {
    }

    public override void Update(GameTime gameTime)
    {
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
    }

    public void SetCollision(int x, int y, bool value)
    {
        if (x < 0 || x >= _width || y < 0 || y >= _height)
            throw new System.ArgumentOutOfRangeException("Coordinates are out of bounds.");

        _collisions[x, y] = value;
    }

    public bool GetCollision(int x, int y)
    {
        if (x < 0 || x >= _width || y < 0 || y >= _height)
            throw new System.ArgumentOutOfRangeException("Coordinates are out of bounds.");

        return _collisions[x, y];
    }

    public List<AABB> GetAABBs(float x, float y, float radius)
    {
        List<AABB> aabbs = new List<AABB>();

        int startX = (int)((x - radius) / _tileSize);
        int startY = (int)((y - radius) / _tileSize);
        int endX = (int)((x + radius) / _tileSize);
        int endY = (int)((y + radius) / _tileSize);

        for (int i = Math.Max(0, startX); i <= Math.Min(_width - 1, endX); i++)
        {
            for (int j = Math.Max(0, startY); j <= Math.Min(_height - 1, endY); j++)
            {
                if (_collisions[i, j])
                {
                    Vector2 min = new Vector2(i * _tileSize, j * _tileSize);
                    Vector2 max = new Vector2(min.X + _tileSize, min.Y + _tileSize);
                    aabbs.Add(new AABB(min, max));
                }
            }
        }

        return aabbs;
    }

    public float Width
    {
        get => _width;
    }

    public float Height
    {
        get => _height;
    }

    public float TileSize
    {
        get => _tileSize;
    }

}