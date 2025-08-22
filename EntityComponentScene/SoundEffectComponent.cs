using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Peridot;
using Peridot.Components;

public class SoundEffectComponent : Component
{
    public string SoundEffectName { get; set; }

    public bool IsLooping { get; set; }

    public bool PlayOnAwake { get; set; }

    public float Volume { get; set; }

    public bool IsOneShot { get; set; }

    public float Lifetime { get; set; }


    private SoundEffectInstance _soundEffectInstance;

    private float _timer;

    private bool _isPlaying;

    public SoundEffectComponent()
    {

    }

    public SoundEffectComponent(string soundEffectName, bool isLooping, bool playOnAwake, float volume, float lifetime, bool isOneShot)
    {
        SoundEffectName = soundEffectName;
        IsLooping = isLooping;
        PlayOnAwake = playOnAwake;
        Volume = volume;
        Lifetime = lifetime;
        IsOneShot = isOneShot;
        _timer = 0f;
    }

    public override void Initialize()
    {
        if (PlayOnAwake)
            Play();

    }

    public override void Update(GameTime gameTime)
    {
        if (_isPlaying && Lifetime != 0f)
        {
            _timer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (IsOneShot && _timer >= Lifetime)
            {
                _soundEffectInstance.Stop();
                _isPlaying = false;
                _timer = 0f;
            }
        }
    }

    private void Play()
    {
        _soundEffectInstance = Core.AudioLibrary.Get(SoundEffectName).CreateInstance();
        _soundEffectInstance.IsLooped = IsLooping;
        _soundEffectInstance.Volume = Volume;
        _soundEffectInstance.Play();
        _isPlaying = true;
    }
}