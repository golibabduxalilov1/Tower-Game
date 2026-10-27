using Microsoft.Xna.Framework.Audio;

namespace TowerGame.Audio;

/// <summary>All audio is synthesized in memory at startup. No recorded audio assets.</summary>
public sealed class SynthAudio : IDisposable
{
    const int Rate = 22050;
    const int MaxVoices = 14, MaxPerSound = 3;
    readonly Dictionary<string, SoundEffect> sounds = [];
    readonly List<(string Name, SoundEffectInstance Voice)> active = [];
    readonly Dictionary<string, double> cooldowns = [];
    SoundEffectInstance? music;
    public float Master = .65f, Effects = .65f, Music = .35f;
    public bool Muted, Suspended;
    public bool Available => sounds.Count > 0;

    public SynthAudio()
    {
        try
        {
            sounds["click"] = Tone(660, .05, Shape.Sine);
            sounds["build"] = Chirp(380, 620, .18);
            sounds["upgrade"] = Arpeggio([523, 659, 784], .09);
            sounds["sell"] = Chirp(620, 380, .16);
            sounds["error"] = Tone(130, .2, Shape.Square);
            sounds["wave"] = Arpeggio([294, 392, 294, 440], .12);
            sounds["boss"] = Arpeggio([147, 139, 147, 131], .2, Shape.Square);
            sounds["victory"] = Arpeggio([523, 659, 784, 1047, 784, 1047], .14);
            sounds["defeat"] = Arpeggio([392, 330, 262, 196], .22, Shape.Square);
            sounds["explosion"] = Noise(.35, 2.5);
            sounds["Gatling"] = Noise(.05, 7);
            sounds["Missile"] = Chirp(240, 120, .17, .5);
            sounds["Mortar"] = Chirp(90, 50, .22, .6);
            sounds["Tesla"] = Zap(.12);
            sounds["Laser"] = Chirp(1500, 700, .12);
            sounds["Flame"] = Noise(.14, 3);
            sounds["Goo"] = Chirp(300, 200, .1);
            sounds["Ice"] = Chirp(1400, 1900, .15);
            sounds["music"] = MusicLoop();
            music = sounds["music"].CreateInstance(); music.IsLooped = true;
        }
        catch (Exception) { Dispose(); } // Audio device absence must not prevent offline play.
    }

    enum Shape { Sine, Square }
    static SoundEffect Build(double seconds, Func<double, double> sample)
    {
        var data = new byte[(int)(Rate * seconds) * 2];
        for (int i = 0; i < data.Length / 2; i++)
        {
            double t = (double)i / Rate, envelope = Math.Min(t * 200, 1) * Math.Pow(1 - t / seconds, 2);
            short value = (short)(Math.Clamp(sample(t), -1, 1) * envelope * 12000);
            data[i * 2] = (byte)value; data[i * 2 + 1] = (byte)(value >> 8);
        }
        return new SoundEffect(data, Rate, AudioChannels.Mono);
    }
    static double Wave(Shape shape, double phase) => shape == Shape.Square ? Math.Sign(Math.Sin(phase)) * .5 : Math.Sin(phase);
    static SoundEffect Tone(double f, double s, Shape shape) => Build(s, t => Wave(shape, Math.Tau * f * t));
    static SoundEffect Chirp(double from, double to, double s, double noise = 0)
    {
        var rng = new Random((int)(from * 7 + to));
        return Build(s, t => { var f = from + (to - from) * t / s; return Math.Sin(Math.Tau * (from * t + (f - from) * t / 2)) * (1 - noise) + (rng.NextDouble() * 2 - 1) * noise; });
    }
    static SoundEffect Noise(double s, double decay)
    {
        var rng = new Random(17); double last = 0;
        return Build(s, t => { last = last * .6 + (rng.NextDouble() * 2 - 1) * .4; return last * 2 * Math.Exp(-t * decay); });
    }
    static SoundEffect Zap(double s)
    {
        var rng = new Random(5);
        return Build(s, t => Math.Sign(Math.Sin(Math.Tau * (700 + rng.Next(600)) * t)) * .5);
    }
    static SoundEffect Arpeggio(double[] notes, double each, Shape shape = Shape.Sine) =>
        Build(notes.Length * each, t => { var i = Math.Min(notes.Length - 1, (int)(t / each)); var local = t - i * each; return Wave(shape, Math.Tau * notes[i] * t) * Math.Min(1, (each - local) * 30); });
    static SoundEffect MusicLoop()
    {
        const int seconds = 16;
        var data = new byte[Rate * seconds * 2];
        double[] notes = [130.81, 164.81, 196, 164.81, 110, 130.81, 164.81, 196];
        for (int i = 0; i < Rate * seconds; i++)
        {
            double t = (double)i / Rate, local = t % 2;
            double env = Math.Sin(Math.PI * local / 2) * .35;
            double n = notes[(int)(t / 2)];
            double pulse = (t * 4 % 1) < .08 ? .25 : 0;
            short value = (short)((Math.Sin(Math.Tau * n * t) + .35 * Math.Sin(Math.Tau * n * 2 * t) + pulse * Math.Sin(Math.Tau * n * 4 * t)) * env * 5000);
            data[i * 2] = (byte)value; data[i * 2 + 1] = (byte)(value >> 8);
        }
        return new SoundEffect(data, Rate, AudioChannels.Mono);
    }

    public void Update(double dt)
    {
        foreach (var key in cooldowns.Keys.ToArray()) cooldowns[key] = Math.Max(0, cooldowns[key] - dt);
        for (int i = active.Count - 1; i >= 0; i--)
            if (active[i].Voice.State == SoundState.Stopped) { active[i].Voice.Dispose(); active.RemoveAt(i); }
        if (music is null) return;
        try
        {
            music.Volume = Math.Clamp(Master * Music, 0, 1);
            if (Muted || Suspended || Music <= 0) { if (music.State == SoundState.Playing) music.Pause(); }
            else if (music.State == SoundState.Paused) music.Resume();
            else if (music.State == SoundState.Stopped) music.Play();
        }
        catch (Exception) { } // Output device changes must never crash the game.
    }

    /// <summary>Plays a short effect. Combat sounds are rate limited per name so rapid fire does not pile up.</summary>
    public void Play(string name, bool combat = false)
    {
        if (Muted || Suspended || active.Count >= MaxVoices || !sounds.TryGetValue(name, out var effect)) return;
        if (cooldowns.GetValueOrDefault(name) > 0) return;
        var same = 0; foreach (var a in active) if (a.Name == name) same++;
        if (same >= MaxPerSound) return;
        try
        {
            var instance = effect.CreateInstance(); instance.Volume = Math.Clamp(Master * Effects * (combat ? .35f : .7f), 0, 1);
            instance.Play(); active.Add((name, instance));
            if (combat) cooldowns[name] = name == "Gatling" ? .09 : .06;
        }
        catch (Exception) { }
    }
    public void Suspend()
    {
        Suspended = true;
        try { foreach (var sound in active) sound.Voice.Stop(); music?.Pause(); } catch (Exception) { }
    }
    public void Resume() => Suspended = false;
    public void Dispose()
    {
        foreach (var a in active) a.Voice.Dispose(); active.Clear();
        music?.Dispose(); music = null;
        foreach (var sound in sounds.Values) sound.Dispose(); sounds.Clear();
    }
}
