namespace Chess.UI.SplashKit;

/// <summary>
/// Makes the SplashKit native library loadable on Windows without changing system settings.
///
/// SplashKit.dll from `skm` is built with MSYS2's UCRT64 toolchain, so its dependencies (SDL2,
/// libstdc++ and others) must come from C:\msys64\ucrt64\bin. If C:\msys64\mingw64\bin is earlier
/// on PATH, Windows loads an incompatible libstdc++ and the first SplashKit call fails with
/// "DllNotFoundException ... The specified procedure could not be found".
///
/// Windows resolves the library lazily, on the first SplashKit call, using this process's PATH at
/// that moment. So putting the right folders first here, before any SplashKit type is touched,
/// fixes `dotnet run` for this process only.
/// </summary>
internal static class NativeLibrarySetup
{
    public static void Configure()
    {
        if (!OperatingSystem.IsWindows()) return;

        string msys = Environment.GetEnvironmentVariable("MSYS2_ROOT") ?? @"C:\msys64";
        string[] preferred =
        {
            Path.Combine(msys, "ucrt64", "bin"),   // UCRT64 SDL2, libstdc++, ...
            Path.Combine(msys, "mingw64", "lib"),  // where `skm global install` puts SplashKit.dll
        };

        var existing = preferred.Where(Directory.Exists).ToArray();
        if (existing.Length == 0) return; // SplashKit installed elsewhere: rely on the user's PATH

        string current = Environment.GetEnvironmentVariable("PATH") ?? "";
        Environment.SetEnvironmentVariable("PATH", string.Join(';', existing) + ";" + current);
    }
}
