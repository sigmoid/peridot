using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

public class AudioLibrary
{
    private Dictionary<string, SoundEffect> _soundEffects;

    public AudioLibrary()
    {
        _soundEffects = new Dictionary<string, SoundEffect>();
    }

    public void Load(ContentManager content, string dictionaryName)
    {
        if (!File.Exists(dictionaryName))
        {
            return;
        }
        var soundEffectList = File.ReadAllLines(dictionaryName);
        foreach (var soundEffectName in soundEffectList)
        {
            var soundEffect = content.Load<SoundEffect>(soundEffectName);
            _soundEffects[soundEffectName] = soundEffect;
        }
    }

    public SoundEffect Get(string soundEffectName)
    {
        if (_soundEffects.TryGetValue(soundEffectName, out var soundEffect))
        {
            return soundEffect;
        }
        throw new KeyNotFoundException($"Sound effect '{soundEffectName}' not found in the audio library.");
    }
}