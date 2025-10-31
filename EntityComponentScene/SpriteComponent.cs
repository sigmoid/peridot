using System.Diagnostics.Contracts;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Peridot.Graphics;

namespace Peridot.Components;

public class SpriteComponent : Component
{
    private Sprite _sprite;
    public string Texture { get; set; }
    public Sprite Sprite { get { return _sprite; } }
    public float LayerDepth { get; set; } = 0f;

    public SpriteComponent()
    {
    }

    public override XElement Serialize()
    {
        var element = base.Serialize();
        element.Add(new XAttribute("Texture", Texture));
        return element;
    }

    public SpriteComponent(string defaultTexture)
    {
        Texture = defaultTexture;

        try
        {
            // First try texture atlas
            if (Core.TextureAtlas != null)
            {
                SetTexture(Core.TextureAtlas.GetRegion(defaultTexture));
            }
            else
            {
                throw new System.Exception("TextureAtlas not loaded");
            }
        }
        catch (System.Exception)
        {
            // If texture atlas fails, try loading from file
            try
            {
                if (Core.Content != null)
                {
                    var texture2D = Core.Content.Load<Texture2D>(defaultTexture);
                    _sprite = new Sprite(texture2D);
                }
            }
            catch (System.Exception ex)
            {
                Logger.Error($"Failed to load texture '{defaultTexture}' from both texture atlas and file: {ex.Message}");
                _sprite = new Sprite();
            }
        }
    }

    public SpriteComponent(Sprite sprite)
    {
        _sprite = sprite;
    }

    public override void Initialize()
    {
        if (!string.IsNullOrEmpty(Texture))
        {
            try
            {
                // First, try to load from texture atlas (existing behavior)
                if (Core.TextureAtlas != null)
                {
                    var textureRegion = Core.TextureAtlas.GetRegion(Texture);
                    _sprite = new Sprite(textureRegion);
                }
                else
                {
                    throw new System.Exception("TextureAtlas not loaded");
                }
            }
            catch (System.Exception)
            {
                // If texture atlas loading fails, try loading from file
                try
                {
                    if (Core.Content != null)
                    {
                        var texture2D = Core.Content.Load<Texture2D>(Texture);
                        _sprite = new Sprite(texture2D);
                    }
                    else
                    {
                        Logger.Error($"Neither TextureAtlas nor ContentManager available, cannot load texture '{Texture}'");
                        _sprite = new Sprite();
                    }
                }
                catch (System.Exception ex)
                {
                    Logger.Error($"Failed to load texture '{Texture}' from both texture atlas and file: {ex.Message}");
                    _sprite = new Sprite();
                }
            }
        }
        else if(_sprite == null)
        {
            _sprite = new Sprite();
        }

        _sprite.CenterOrigin();
        _sprite.SetLayerDepth(LayerDepth);
    }

    public override void Update(GameTime gameTime)
    {
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (_sprite != null && Entity != null)
        {
            _sprite.Draw(spriteBatch, Entity.Position, Entity.Rotation);
        }
    }

    public void SetTexture(TextureRegion sprite)
    {
        if (_sprite == null)
        {
            _sprite = new Sprite(sprite);
        }
        else
        {
            _sprite.Region = sprite;
        }
    }
}