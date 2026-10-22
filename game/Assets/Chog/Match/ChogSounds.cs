using System.Collections.Generic;
using Chog.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Chog.Match
{
    /// <summary>The sounds of the game, each a folder of variants under Resources/ChogAudio.</summary>
    public enum ChogSound
    {
        Hit,
        StructureHit,
        StructureFall,
        Death,
        Respawn,
        Click,
        Reward
    }

    /// <summary>The two music contexts: one loop for the menus and one selected loop for each match.</summary>
    public enum ChogMusic
    {
        None,
        Menu,
        Match
    }

    /// <summary>
    /// The rules of the sounds, plain numbers so they are tested without an engine: how often one clip may
    /// play, the spread of the pitch, how loud each sound is, and the pitch of each step of a reward.
    /// </summary>
    public static class ChogSoundRules
    {
        /// <summary>At most this many plays of one clip ...</summary>
        public const int MaxPlaysInWindow = 2;
        /// <summary>... within this many seconds; more would only make the mix louder and muddier.</summary>
        public const float Window = 0.05f;
        public const float LowestPitch = 0.92f;
        public const float HighestPitch = 1.08f;

        /// <summary>
        /// Whether a clip may play at "now", given the times it last started ("recent", newest first,
        /// below 0 for never).
        /// </summary>
        public static bool MayPlay(float now, float[] recent)
        {
            var inWindow = 0;
            foreach (var started in recent)
                if (started >= 0f && now - started < Window)
                    inWindow++;
            return inWindow < MaxPlaysInWindow;
        }

        /// <summary>The pitch of a play from a random number between 0 and 1.</summary>
        public static float Pitch(float random01) => Mathf.Lerp(LowestPitch, HighestPitch, Mathf.Clamp01(random01));

        /// <summary>
        /// How loud a sound plays before the player's setting: louder when the local hero is involved (it
        /// is hit, it hits, it falls), so the player hears their own fight over the others'.
        /// </summary>
        public static float Volume(ChogSound sound, bool local)
        {
            switch (sound)
            {
                case ChogSound.Hit: return local ? 0.9f : 0.45f;
                case ChogSound.StructureHit: return local ? 0.7f : 0.4f;
                case ChogSound.StructureFall: return 0.95f;
                case ChogSound.Death: return local ? 1f : 0.6f;
                case ChogSound.Respawn: return local ? 0.75f : 0.35f;
                case ChogSound.Click: return 0.55f;
                case ChogSound.Reward: return 0.8f;
                default: return 0.6f;
            }
        }

        /// <summary>
        /// The pitch of step "step" (0 for the first) of a reward: a major scale upwards, one note a step,
        /// back to the start after an octave.
        /// </summary>
        public static float RewardPitch(int step)
        {
            int[] semitones = { 0, 2, 4, 5, 7, 9, 11, 12 };
            var note = semitones[Mathf.Abs(step) % semitones.Length];
            return Mathf.Pow(2f, note / 12f);
        }

        /// <summary>A volume setting read back: between 0 and 1, the default when nothing is stored.</summary>
        public static float Setting(float stored, float fallback) =>
            float.IsNaN(stored) || stored < 0f ? fallback : Mathf.Clamp01(stored);
    }

    /// <summary>
    /// Plays the sounds and the music of the game, from one object kept across scenes: a few audio sources
    /// taken in turn for the sounds (no object made per sound), and two for the music, which cross-fade
    /// when the player goes from the menu to a match and back. Every sound has a few variants and a pitch
    /// that varies a little, so a fight never repeats the same note. The music and sound volumes are the
    /// player's settings, stored on the device. All sounds are flat (2D): the camera is high above a small
    /// arena, and the volume says what matters (the local hero) better than the distance would.
    /// </summary>
    public sealed class ChogSounds : MonoBehaviour
    {
        /// <summary>The keys of the settings on the device (PlayerPrefs).</summary>
        public const string MusicVolumeKey = "chog.music-volume";
        public const string SoundVolumeKey = "chog.sound-volume";
        public const float DefaultMusicVolume = 0.5f;
        public const float DefaultSoundVolume = 1f;

        private const string Folder = "ChogAudio/";
        private const int Voices = 8;
        private const float FadeSeconds = 1.2f;
        // The music under the sounds, before the player's setting.
        private const float MusicLevel = 0.55f;

        private static ChogSounds s_instance;
        private static float s_musicVolume = -1f;
        private static float s_soundVolume = -1f;
        private static int s_played;
        private static int s_throttled;

        private readonly Dictionary<ChogSound, AudioClip[]> _clips = new Dictionary<ChogSound, AudioClip[]>();
        private readonly Dictionary<AudioClip, float[]> _recent = new Dictionary<AudioClip, float[]>();
        private readonly AudioSource[] _voices = new AudioSource[Voices];
        private readonly float[] _startedAt = new float[Voices];
        private AudioSource _musicNow;
        private AudioSource _musicBefore;
        private AudioListener _listener;
        private ChogMusic _music = ChogMusic.None;
        // A sample scene can still offer its clip when no Resources match playlist is installed.
        private AudioClip _matchClip;
        private AudioClip _selectedMatchMusic;
        private int _previousMatchMusic = -1;
        private bool _wasInMatch;
        private float _fade = 1f;
        private float _nextListenerCheck;

        /// <summary>Muted for this session (the sample's mute key); not stored.</summary>
        public static bool Muted { get; set; }

        /// <summary>The music volume the player chose, 0 to 1, stored on the device.</summary>
        public static float MusicVolume
        {
            get
            {
                if (s_musicVolume < 0f)
                    s_musicVolume = ChogSoundRules.Setting(PlayerPrefs.GetFloat(MusicVolumeKey, -1f), DefaultMusicVolume);
                return s_musicVolume;
            }
            set
            {
                s_musicVolume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(MusicVolumeKey, s_musicVolume);
                PlayerPrefs.Save();
            }
        }

        /// <summary>The volume of the sounds (hits, falls, buttons) the player chose, 0 to 1, stored on the device.</summary>
        public static float SoundVolume
        {
            get
            {
                if (s_soundVolume < 0f)
                    s_soundVolume = ChogSoundRules.Setting(PlayerPrefs.GetFloat(SoundVolumeKey, -1f), DefaultSoundVolume);
                return s_soundVolume;
            }
            set
            {
                s_soundVolume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(SoundVolumeKey, s_soundVolume);
                PlayerPrefs.Save();
            }
        }

        /// <summary>One line for the performance overlay.</summary>
        public static string Status => "sound played " + s_played + "  throttled " + s_throttled + "  music " +
                                       (s_instance == null ? "-" : s_instance._music.ToString());

        /// <summary>A hero was hit; "local" when the local hero is hit or hits.</summary>
        public static void Hit(bool local) => Play(ChogSound.Hit, local);

        /// <summary>A tower or a core was hit.</summary>
        public static void StructureHit(bool local) => Play(ChogSound.StructureHit, local);

        /// <summary>A tower or a core fell.</summary>
        public static void StructureFall() => Play(ChogSound.StructureFall, true);

        /// <summary>A hero fell; "local" for the local hero.</summary>
        public static void Death(bool local) => Play(ChogSound.Death, local);

        /// <summary>A hero came back; "local" for the local hero.</summary>
        public static void Respawn(bool local) => Play(ChogSound.Respawn, local);

        /// <summary>A button was pressed.</summary>
        public static void Click() => Play(ChogSound.Click, true);

        /// <summary>
        /// One step of a reward counted up on a screen (0 for the first): the same chime a note higher at
        /// each step, so a row of rewards climbs.
        /// </summary>
        public static void Reward(int step) => Play(ChogSound.Reward, true, ChogSoundRules.RewardPitch(step));

        /// <summary>The local hero's special is charged: the reward's chime, a fifth higher.</summary>
        public static void SpecialReady() => Play(ChogSound.Reward, true, ChogSoundRules.RewardPitch(4));

        /// <summary>Any clip of the game (the sample's attack and skill sounds) through the same voices.</summary>
        public static void PlayClip(AudioClip clip, float volume)
        {
            if (clip == null || Muted || !Ready())
                return;
            if (!s_instance.MayPlay(clip))
            {
                s_throttled++;
                return;
            }
            s_instance.Voice(clip, volume * SoundVolume, ChogSoundRules.Pitch(Random.value));
        }

        /// <summary>
        /// The clip a match plays when Resources/ChogAudio/MusicMatch has none: the music of the sample's
        /// arena, handed over by its audio manager.
        /// </summary>
        public static void OfferMatchMusic(AudioClip clip)
        {
            if (clip == null || !Ready())
                return;
            s_instance._matchClip = clip;
            if (s_instance._music == ChogMusic.Match && s_instance._musicNow.clip == null)
                s_instance.Switch(ChogMusic.Match, true);
        }

        /// <summary>Plays that loop from now on (the menu's or a match's), cross-fading from the one before.</summary>
        public static void PlayMusic(ChogMusic music)
        {
            if (Ready())
                s_instance.Switch(music, false);
        }

        private static void Play(ChogSound sound, bool local, float pitch = 0f)
        {
            if (Muted || !Ready())
                return;
            var clips = s_instance.Clips(sound);
            if (clips.Length == 0)
                return;

            // A variant at random that has not played too often in the last instant.
            var first = Random.Range(0, clips.Length);
            for (var i = 0; i < clips.Length; i++)
            {
                var clip = clips[(first + i) % clips.Length];
                if (!s_instance.MayPlay(clip))
                    continue;
                var volume = ChogSoundRules.Volume(sound, local) * SoundVolume;
                s_instance.Voice(clip, volume, pitch > 0f ? pitch : ChogSoundRules.Pitch(Random.value));
                return;
            }

            s_throttled++;
        }

        private static bool Ready()
        {
            if (s_instance != null)
                return true;
            if (!Application.isPlaying)
                return false;
            var host = new GameObject("Chog Sounds");
            DontDestroyOnLoad(host);
            s_instance = host.AddComponent<ChogSounds>();
            s_instance.Build();
            return true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            s_musicVolume = -1f;
            s_soundVolume = -1f;
            PerformanceHud.StatusLines.Add(() => Status);
            Ready();
        }

        private void Build()
        {
            for (var i = 0; i < Voices; i++)
            {
                _voices[i] = Source(false);
                _startedAt[i] = -1f;
            }
            _musicNow = Source(true);
            _musicBefore = Source(true);
            _listener = gameObject.AddComponent<AudioListener>();
            SceneManager.sceneLoaded += (_, _) => OnScenes();
            SceneManager.sceneUnloaded += _ => OnScenes();
            OnScenes();
        }

        private AudioSource Source(bool music)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.loop = music;
            source.priority = music ? 64 : 128;
            return source;
        }

        // The menu's music plays in menus. Entering a Gameplay scene picks one playlist entry and
        // keeps it for that whole match. The next match chooses another entry when one is available.
        private void OnScenes()
        {
            var inMatch = false;
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                inMatch |= scene.isLoaded && ArenaScenes.IsArena(scene.name);
            }
            if (inMatch && !_wasInMatch)
                _selectedMatchMusic = PickMatchMusic();
            else if (!inMatch && _wasInMatch)
                _selectedMatchMusic = null;
            _wasInMatch = inMatch;
            Switch(inMatch ? ChogMusic.Match : ChogMusic.Menu, false);
            KeepOneListener();
        }

        // The sounds are heard from this object: another listener (the arena's camera) is turned off, so the
        // menu, which has none, is heard too and there are never two.
        private void KeepOneListener()
        {
            foreach (var other in FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (other != _listener && other.enabled)
                    other.enabled = false;
            _listener.enabled = true;
        }

        private AudioClip[] Clips(ChogSound sound)
        {
            if (!_clips.TryGetValue(sound, out var clips))
            {
                clips = Resources.LoadAll<AudioClip>(Folder + sound);
                _clips[sound] = clips;
            }
            return clips;
        }

        private bool MayPlay(AudioClip clip)
        {
            if (!_recent.TryGetValue(clip, out var recent))
            {
                recent = new float[ChogSoundRules.MaxPlaysInWindow];
                for (var i = 0; i < recent.Length; i++)
                    recent[i] = -1f;
                _recent[clip] = recent;
            }
            if (!ChogSoundRules.MayPlay(Time.unscaledTime, recent))
                return false;
            for (var i = recent.Length - 1; i > 0; i--)
                recent[i] = recent[i - 1];
            recent[0] = Time.unscaledTime;
            return true;
        }

        // A free voice, or the one that started first, plays the clip.
        private void Voice(AudioClip clip, float volume, float pitch)
        {
            var chosen = 0;
            for (var i = 0; i < Voices; i++)
            {
                if (!_voices[i].isPlaying)
                {
                    chosen = i;
                    break;
                }
                if (_startedAt[i] < _startedAt[chosen])
                    chosen = i;
            }

            var voice = _voices[chosen];
            voice.Stop();
            voice.clip = clip;
            voice.volume = Mathf.Clamp01(volume);
            voice.pitch = pitch;
            voice.Play();
            _startedAt[chosen] = Time.unscaledTime;
            s_played++;
        }

        private void Switch(ChogMusic music, bool force)
        {
            if (music == _music && !force)
                return;
            _music = music;
            var clip = MusicClip(music);
            if (!force && clip != null && _musicNow.clip == clip && _musicNow.isPlaying)
                return;

            // The loop playing now fades out while the new one fades in.
            (_musicBefore, _musicNow) = (_musicNow, _musicBefore);
            _musicNow.Stop();
            _musicNow.clip = clip;
            _musicNow.volume = 0f;
            if (clip != null)
                _musicNow.Play();
            _fade = 0f;
        }

        private AudioClip MusicClip(ChogMusic music)
        {
            if (music == ChogMusic.None)
                return null;
            var clips = Resources.LoadAll<AudioClip>(Folder + (music == ChogMusic.Menu ? "MusicMenu" : "MusicMatch"));
            if (music == ChogMusic.Menu && clips.Length > 0)
                return clips[0];
            if (music != ChogMusic.Match)
                return null;
            if (_selectedMatchMusic == null)
                _selectedMatchMusic = PickMatchMusic(clips);
            return _selectedMatchMusic != null ? _selectedMatchMusic : _matchClip;
        }

        private AudioClip PickMatchMusic()
        {
            return PickMatchMusic(Resources.LoadAll<AudioClip>(Folder + "MusicMatch"));
        }

        private AudioClip PickMatchMusic(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0)
                return _matchClip;
            var index = Random.Range(0, clips.Length);
            if (clips.Length > 1 && index == _previousMatchMusic)
                index = (index + 1) % clips.Length;
            _previousMatchMusic = index;
            return clips[index];
        }

        private void Update()
        {
            var level = Muted ? 0f : MusicVolume * MusicLevel;
            if (_fade < 1f)
                _fade = Mathf.Min(1f, _fade + Time.unscaledDeltaTime / FadeSeconds);
            _musicNow.volume = level * _fade;
            _musicBefore.volume = level * (1f - _fade);
            if (_fade >= 1f && _musicBefore.isPlaying)
                _musicBefore.Stop();

            // A camera with its own listener can appear after its scene loaded (the arena's essentials).
            if (Time.unscaledTime >= _nextListenerCheck)
            {
                _nextListenerCheck = Time.unscaledTime + 1f;
                KeepOneListener();
            }
        }
    }
}
