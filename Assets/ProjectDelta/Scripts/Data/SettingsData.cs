using System;
using System.Collections.Generic;

namespace ProjectDelta.Data
{
    // Independent of any run. Saved immediately after each change, not tied
    // to the auto-save cadence used for RunData (기획서 9.1).
    [Serializable]
    public sealed class SettingsData
    {
        public DisplaySettings Display = new DisplaySettings();
        public GraphicsSettingsData Graphics = new GraphicsSettingsData();
        public UiSettings Ui = new UiSettings();
        public TextSettings Text = new TextSettings();
        public AudioSettingsData Audio = new AudioSettingsData();
        public AccessibilitySettings Accessibility = new AccessibilitySettings();
        public List<KeyBindingEntry> KeyBindings = new List<KeyBindingEntry>();
        public string Language = "ko";
        public bool StreamingModeEnabled;
    }

    [Serializable]
    public sealed class DisplaySettings
    {
        public int ResolutionWidth = 1920;
        public int ResolutionHeight = 1080;
        public int WindowMode;
        public bool VSync = true;
        public int FrameRateLimit = 60;
    }

    [Serializable]
    public sealed class GraphicsSettingsData
    {
        public int QualityLevel;
        public int ShadowQuality;
        public int EffectQuality;
    }

    [Serializable]
    public sealed class UiSettings
    {
        public float UiScale = 1f;
        public bool ShowDamageNumbers = true;
        public int HudLayout;
    }

    [Serializable]
    public sealed class TextSettings
    {
        public float TextSpeed = 1f;
        public bool AutoAdvance;
        public int FontSize;
    }

    [Serializable]
    public sealed class AudioSettingsData
    {
        public float Master = 1f;
        public float Bgm = 1f;
        public float Sfx = 1f;
        public float Ambience = 1f;
        public float UiSound = 1f;
        public float Voice = 1f;
    }

    [Serializable]
    public sealed class AccessibilitySettings
    {
        public bool ReduceFlashing;
        public bool ReduceScreenShake;
        public bool MonoAudio;
        public bool SfxSubtitles;
    }

    [Serializable]
    public sealed class KeyBindingEntry
    {
        public string ActionId;
        public string KeyboardBinding;
        public string GamepadBinding;
    }
}
