#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

/// <summary>
/// Keeps Android signing details out of the committed ProjectSettings.asset.
///
/// Before an Android build this sets the keystore path (~/Documents/aagames.keystore),
/// the alias and the passwords; it clears them again when the editor quits (and on load),
/// so Unity never serializes a machine-specific path or secret into ProjectSettings.asset.
///
/// Passwords are read from the macOS Keychain (generic passwords below) or, as a fallback,
/// from the AA_KEYSTORE_PASS / AA_KEY_PASS environment variables. They are never stored in the repo.
///
/// One-time Keychain setup (Terminal; "-w" with no value prompts for the password):
///   security add-generic-password -a aagames -s aagames-keystore -w
///   security add-generic-password -a aagames -s aagames-key -w
/// </summary>
public class AndroidSigningSetup : IPreprocessBuildWithReport, IPostprocessBuildWithReport
{
    const string KeystoreFileName = "aagames.keystore";
    const string KeyAlias = "aagames";
    const string KeychainAccount = "aagames";
    const string KeystoreService = "aagames-keystore";
    const string KeyService = "aagames-key";

    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.Android) return;

        // Note: on macOS, Mono's SpecialFolder.MyDocuments is the home folder, not ~/Documents.
        string path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documents", KeystoreFileName);
        if (!File.Exists(path))
        {
            ClearSigning();
            throw new BuildFailedException("Android keystore not found at: " + path);
        }

        string storePass = Lookup(KeystoreService, "AA_KEYSTORE_PASS");
        string keyPass = Lookup(KeyService, "AA_KEY_PASS");
        if (string.IsNullOrEmpty(storePass) || string.IsNullOrEmpty(keyPass))
        {
            ClearSigning();
            throw new BuildFailedException(
                "Keystore passwords not found. Add them to the macOS Keychain (see the comment at the top of AndroidSigningSetup.cs) or set AA_KEYSTORE_PASS / AA_KEY_PASS.");
        }

        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = path;
        PlayerSettings.Android.keystorePass = storePass;
        PlayerSettings.Android.keyaliasName = KeyAlias;
        PlayerSettings.Android.keyaliasPass = keyPass;
    }

    // Don't clear after the build: "Build And Run" still needs the signing settings afterwards
    // (bundletool signs the .apks for install). Clear when the editor quits / loads instead.
    public void OnPostprocessBuild(BuildReport report) { }

    [InitializeOnLoadMethod]
    static void RegisterCleanup()
    {
        ClearSigning(); // in case a previous session left the path behind
        EditorApplication.quitting -= ClearSigning;
        EditorApplication.quitting += ClearSigning;
    }

    static void ClearSigning()
    {
        if (!string.IsNullOrEmpty(PlayerSettings.Android.keystoreName))
            PlayerSettings.Android.keystoreName = "";
        if (!string.IsNullOrEmpty(PlayerSettings.Android.keystorePass))
            PlayerSettings.Android.keystorePass = "";
        if (!string.IsNullOrEmpty(PlayerSettings.Android.keyaliasPass))
            PlayerSettings.Android.keyaliasPass = "";
    }

    static string Lookup(string keychainService, string envVar)
    {
        string env = Environment.GetEnvironmentVariable(envVar);
        if (!string.IsNullOrEmpty(env)) return env;

        try
        {
            var psi = new ProcessStartInfo("/usr/bin/security",
                $"find-generic-password -a {KeychainAccount} -s {keychainService} -w")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using (var p = Process.Start(psi))
            {
                string output = p.StandardOutput.ReadToEnd().TrimEnd('\r', '\n');
                p.WaitForExit();
                return p.ExitCode == 0 ? output : null;
            }
        }
        catch (Exception)
        {
            return null;
        }
    }
}
#endif
