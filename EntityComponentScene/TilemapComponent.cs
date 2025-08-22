using System;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Peridot;
using Peridot.Components;

public class TilemapComponent : Component
{
    private string[,] _tiles;

    [Peridot.EntityComponentScene.Serialization.ComponentProperty]
    public int Width
    {
        get => _width;
        private set
        {
            _width = value;
        }
    }
    
    [Peridot.EntityComponentScene.Serialization.ComponentProperty]
    public int Height 
    { 
        get => _height; 
        set 
        {
            _height = value;
        }
    }

    private int _width;
    private int _height;

    private int _tileSize;

    [Peridot.EntityComponentScene.Serialization.ComponentProperty]
    public int TileSize 
    { 
        get => _tileSize;  
        set => _tileSize = value;
    }

    public TilemapComponent()
    {
        // Default constructor for serialization or other purposes
    }

    public TilemapComponent(int width, int height, int tileSize, string defaultTile)
    {
        _width = width;
        _height = height;
        _tileSize = tileSize;
        _tiles = new string[width, height];

        FillTilemap(defaultTile);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        Vector4 worldBounds = Core.Camera.GetWorldSpaceBounds();

        // The map is drawn such that the entity position is the center of the tilemap.
        Vector2 centerPosition = Entity.Position;
        int startX = (int)((worldBounds.X - centerPosition.X) / _tileSize);
        int startY = (int)((worldBounds.Y - centerPosition.Y) / _tileSize);

        startX = Math.Max(0, startX);
        startY = Math.Max(0, startY);
        startX = Math.Min(startX, _width - 1);
        startY = Math.Min(startY, _height - 1);

        var endX = Math.Min(startX + (int)(worldBounds.Z - worldBounds.X) / _tileSize + 2, _width);
        var endY = Math.Min(startY + (int)(worldBounds.W - worldBounds.Y) / _tileSize + 2, _height);

        for (int x = startX; x < endX; x++)
        {
            for (int y = startY; y < endY; y++)
            {
                string tile = _tiles[x, y];
                Core.TextureAtlas.GetRegion(tile).Draw(spriteBatch, new Vector2(x * _tileSize, y * _tileSize), Color.White);
            }
        }
    }

    public override void Initialize()
    {
    }

    public override void Update(GameTime gameTime)
    {
    }

    public void SetTile(int x, int y, string tileName)
    {
        if (x < 0 || x >= _width || y < 0 || y >= _height)
            throw new ArgumentOutOfRangeException("Coordinates are out of bounds.");

        _tiles[x, y] = tileName;
    }

    private void FillTilemap(string tileName)
    {
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                _tiles[x, y] = tileName;
            }
        }
    }


}
