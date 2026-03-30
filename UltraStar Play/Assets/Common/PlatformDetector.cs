using System;
using System.IO;
using UnityEngine;

/// <remarks>
/// ## PlatformDetector
///
/// Detects the runtime platform and execution environment, distinguishing between
/// native Linux, Steam Deck hardware, and Proton (Wine) layers.
///
/// ---
///
/// ### Reliability Ranking of Detection Signals
///
/// | Rank | Signal | Reliability | Notes |
/// |------|--------|-------------|-------|
/// | 1 | `SteamDeck=1` env var | ★★★★★ | Canonical Valve signal. Set by Steam on all Deck hardware — Gaming Mode, Desktop Mode, and under Proton. Primary indicator regardless of execution layer. |
/// | 2 | `/sys/class/dmi/id/product_name` = `Jupiter` / `Galileo` | ★★★★ | Hardware DMI name. `Jupiter` = Deck Gen 1, `Galileo` = Deck OLED. Accessible under Proton because Wine forwards `/sys` from the host kernel. Silently skipped on non-Linux hosts. |
/// | 3 | `STEAM_COMPAT_DATA_PATH` env var | ★★★★ | Most consistently present Proton variable across all versions. Set on both Steam Deck and regular Linux desktops running via Proton. |
/// | 4 | `PROTON_VERSION` env var | ★★★★ | Explicit Proton version string. May be absent in older Proton builds — use alongside `STEAM_COMPAT_DATA_PATH`. |
/// | 5 | `WINEPREFIX` / `WINE_DISTRIB` env vars | ★★★ | Set by Wine and Proton (which is Wine-based). May also appear in non-Steam Wine setups — treat as last resort only. |
///
/// ---
///
/// ### Key Notes
///
/// - **Caching** — Results are cached after the first call to avoid repeated environment
///   variable lookups on subsequent frames.
/// - **Proton reports as Windows** — When a Unity Windows build runs under Proton,
///   `Application.platform` returns `WindowsPlayer`. Hardware and env-var checks are
///   therefore essential to distinguish Proton-on-Deck from a real Windows machine.
/// - **DMI accessible under Proton** — The `/sys` and `/proc` virtual filesystems are
///   forwarded into the Wine environment transparently by the host Linux kernel.
/// - **`SteamDeck=1` is Steam-owned** — Set by Steam itself, not by Proton or the game,
///   making it resilient to differences between Proton versions.
/// </remarks>
public static class PlatformDetector
{
    public enum RunEnvironment
    {
        Windows,
        MacOS,
        NativeLinux,
        SteamDeckNativeLinux,
        SteamDeckProton,    // Steam Deck running a Windows build via Proton
        ProtonOnLinux,      // Regular Linux PC running a Windows build via Proton
    }

    private static RunEnvironment? _cached;

    public static RunEnvironment Detect()
    {
        if (_cached.HasValue) return _cached.Value;
        _cached = DetectInternal();
        return _cached.Value;
    }

    private static RunEnvironment DetectInternal()
    {
        bool isSteamDeck   = IsSteamDeck();
        bool isProton      = IsRunningUnderProton();
        bool isNativeLinux = Application.platform == RuntimePlatform.LinuxPlayer;

        if (isSteamDeck && isProton)    return RunEnvironment.SteamDeckProton;
        if (isSteamDeck)                return RunEnvironment.SteamDeckNativeLinux;
        if (isProton && !isNativeLinux) return RunEnvironment.ProtonOnLinux;
        if (isNativeLinux)              return RunEnvironment.NativeLinux;
        if (Application.platform == RuntimePlatform.OSXPlayer) return RunEnvironment.MacOS;
        return RunEnvironment.Windows;
    }

    // ── Steam Deck hardware detection ────────────────────────────────────────

    private static bool IsSteamDeck()
    {
        // 1. Valve's canonical env var — set by Steam on all Deck hardware,
        //    including Gaming Mode, Desktop Mode, and under Proton.
        if (Environment.GetEnvironmentVariable("SteamDeck") == "1") return true;

        // 2. Hardware DMI product name. "Jupiter" = Deck Gen 1, "Galileo" = Deck OLED.
        //    Accessible under Proton because Wine forwards /sys from the host kernel.
        try
        {
            string productName = File.ReadAllText("/sys/class/dmi/id/product_name").Trim();
            if (productName is "Jupiter" or "Galileo") return true;
        }
        catch { /* Non-Linux host or insufficient permissions — skip silently. */ }

        // 3. Secondary Valve env var present in some Steam/SteamOS configurations.
        if (Environment.GetEnvironmentVariable("STEAM_DECK") == "1") return true;

        return false;
    }

    // ── Proton layer detection ────────────────────────────────────────────────

    private static bool IsRunningUnderProton()
    {
        // Most consistently present across all Proton versions.
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("STEAM_COMPAT_DATA_PATH")))
            return true;

        // Explicit Proton version string; may be absent in older Proton builds.
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROTON_VERSION")))
            return true;

        // Wine-level variables; Proton is Wine-based so these are set as well,
        // but may also appear in non-Steam Wine setups — use as a last resort.
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WINEPREFIX")) ||
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WINE_DISTRIB")))
            return true;

        return false;
    }
}
