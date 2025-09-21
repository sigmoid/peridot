using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Peridot.Graphics;

namespace Peridot.Components;

public class AnimatedSpriteComponent : Component
{
    private AnimatedSprite _animatedSprite;

    public string DefaultAnimation { get; set; }
    public float LayerDepth { get; set; } = 0.5f;

    public AnimatedSpriteComponent()
    {
        // Default constructor for reflection
    }

    public AnimatedSpriteComponent(string defaultAnimation)
    {
        DefaultAnimation = defaultAnimation;

        SetAnimation(Core.TextureAtlas.GetAnimation(defaultAnimation));
    }

    public void SetIsFlipped(bool horizontal, bool vertical)
    {
        if (_animatedSprite != null)
        {
            _animatedSprite.SetIsFlipped(horizontal, vertical);
        }
    }

    public void SetLayerDepth(float layerDepth)
    {
        if (_animatedSprite != null)
        {
            _animatedSprite.SetLayerDepth(layerDepth);
        }
    }

    public override void Initialize()
    {
        if (!string.IsNullOrEmpty(DefaultAnimation))
        {
            try
            {
                if (Core.TextureAtlas != null)
                {
                    var animation = Core.TextureAtlas.GetAnimation(DefaultAnimation);
                    _animatedSprite = new AnimatedSprite(animation);
                }
                else
                {
                    Logger.Error($"TextureAtlas not loaded, cannot load animation '{DefaultAnimation}'");
                    _animatedSprite = new AnimatedSprite();
                }
            }
            catch (System.Exception ex)
            {
                Logger.Error($"Failed to load animation '{DefaultAnimation}': {ex.Message}");
                _animatedSprite = new AnimatedSprite();
            }
        }
        else
        {
            _animatedSprite = new AnimatedSprite();
        }


        SetLayerDepth(LayerDepth);
        _animatedSprite.CenterOrigin();
    }

    public override void Update(GameTime gameTime)
    {
        if (_animatedSprite != null)
        {
            _animatedSprite.Update(gameTime);
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (_animatedSprite != null && Entity != null)
        {
            _animatedSprite.Draw(spriteBatch, Entity.Position, Entity.Rotation);
        }
    }

    public void SetAnimation(Animation animation)
    {
        if (_animatedSprite == null)
        {
            _animatedSprite = new AnimatedSprite(animation);
        }
        else
        {
            _animatedSprite.Animation = animation;
        }
    }

    public void SetAnimation(string animationName)
    {
        if (string.IsNullOrEmpty(animationName))
            return;

        try
        {
            if (Core.TextureAtlas != null)
            {
                var animation = Core.TextureAtlas.GetAnimation(animationName);
                SetAnimation(animation);
            }
            else
            {
                Logger.Error($"TextureAtlas not loaded, cannot set animation '{animationName}'");
            }
        }
        catch (System.Exception ex)
        {
            Logger.Error($"Failed to set animation '{animationName}': {ex.Message}");
        }
    }

    public AnimatedSprite AnimatedSprite
    {
        get => _animatedSprite;
        set => _animatedSprite = value;
    }
}