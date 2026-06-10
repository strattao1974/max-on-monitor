using Microsoft.Win32;

namespace MaxOnMonitor;

internal static class Settings
{
    private const string KeyPath = @"Software\MaxOnMonitor";
    private const string AnimationValueName = "SnapAnimationMs";

    public static int LoadAnimationMs()
    {
        using var key = Registry.CurrentUser.OpenSubKey(KeyPath);
        return key?.GetValue(AnimationValueName) as int? ?? 0;
    }

    public static void SaveAnimationMs(int ms)
    {
        using var key = Registry.CurrentUser.CreateSubKey(KeyPath);
        key.SetValue(AnimationValueName, ms, RegistryValueKind.DWord);
    }
}
