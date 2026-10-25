using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class AndroidBuildPreparation : IPreprocessBuildWithReport
{
    public int callbackOrder => -100;
    [MenuItem("Tools/Android/Prepare project")]
    public static void Prepare()
    {
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.splitApplicationBinary = false;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
        var addressables = AddressableAssetSettingsDefaultObject.Settings;
        if (addressables)
        {
            addressables.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.BuildWithPlayer;
            EditorUtility.SetDirty(addressables);
        }
        LoadSigningKey();
        ConfigureLauncherIcons();
        WriteManifest();
        AssetDatabase.SaveAssets();
        Debug.Log("Android prepared: IL2CPP, ARM64, OpenGL ES 3, landscape. Build the existing Android profile with APK expansion disabled.");
    }
    [System.Serializable]
    sealed class SigningSettings
    {
        public string keystorePath;
        public string alias;
        public string protectedPassword;
    }
    [MenuItem("Tools/Android/Load signing key")]
    public static void LoadSigningKey()
    {
#if UNITY_EDITOR_WIN
        string configPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
            ".sgpr", "android-signing", "signing.json");
        // Other developers can configure their own keystore in Publishing Settings.
        if (!File.Exists(configPath)) return;
        try
        {
            var settings = JsonUtility.FromJson<SigningSettings>(File.ReadAllText(configPath));
            if (settings == null || string.IsNullOrEmpty(settings.keystorePath) ||
                !File.Exists(settings.keystorePath) || string.IsNullOrEmpty(settings.alias) ||
                string.IsNullOrEmpty(settings.protectedPassword))
                throw new BuildFailedException("Android signing configuration or keystore is missing: " + configPath);
            byte[] passwordBytes = UnprotectPassword(System.Convert.FromBase64String(settings.protectedPassword));
            try
            {
                string password = System.Text.Encoding.UTF8.GetString(passwordBytes);
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = settings.keystorePath;
                PlayerSettings.Android.keyaliasName = settings.alias;
                PlayerSettings.Android.keystorePass = password;
                PlayerSettings.Android.keyaliasPass = password;
            }
            finally
            {
                System.Array.Clear(passwordBytes, 0, passwordBytes.Length);
            }
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            throw new BuildFailedException("Cannot decrypt Android signing credentials. Load them using the Windows account that created them, or configure the original key manually in Publishing Settings.");
        }
#endif
    }
#if UNITY_EDITOR_WIN
    [StructLayout(LayoutKind.Sequential)]
    struct DataBlob
    {
        public int length;
        public System.IntPtr data;
    }
    [DllImport("crypt32.dll", SetLastError = true, ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool CryptUnprotectData(ref DataBlob input, System.IntPtr description,
        System.IntPtr entropy, System.IntPtr reserved, System.IntPtr prompt, uint flags,
        out DataBlob output);

    [DllImport("kernel32.dll", ExactSpelling = true)]
    static extern System.IntPtr LocalFree(System.IntPtr memory);

    // Unity's API profile does not expose ProtectedData. Use the same Windows
    // DPAPI format directly, preserving the existing encrypted signing.json.
    static byte[] UnprotectPassword(byte[] encrypted)
    {
        var pinned = GCHandle.Alloc(encrypted, GCHandleType.Pinned);
        var output = new DataBlob();
        try
        {
            var input = new DataBlob { length = encrypted.Length, data = pinned.AddrOfPinnedObject() };
            const uint forbidUI = 1;
            if (!CryptUnprotectData(ref input, System.IntPtr.Zero, System.IntPtr.Zero,
                    System.IntPtr.Zero, System.IntPtr.Zero, forbidUI, out output))
                throw new System.Security.Cryptography.CryptographicException(Marshal.GetLastWin32Error());
            var password = new byte[output.length];
            Marshal.Copy(output.data, password, 0, password.Length);
            return password;
        }
        finally
        {
            if (output.data != System.IntPtr.Zero)
            {
                for (int i = 0; i < output.length; i++) Marshal.WriteByte(output.data, i, 0);
                LocalFree(output.data);
            }
            pinned.Free();
        }
    }
#endif
    static void ConfigureLauncherIcons()
    {
        var logo = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Unver/logos/ico2.png");
        if (!logo) throw new BuildFailedException("Android launcher logo is missing: Assets/Unver/logos/ico2.png");
        var target = NamedBuildTarget.Android;
        foreach (var kind in PlayerSettings.GetSupportedIconKinds(target))
        {
            var icons = PlayerSettings.GetPlatformIcons(target, kind);
            foreach (var icon in icons)
            {
                var layers = new Texture2D[icon.maxLayerCount];
                // The logo includes its opaque black background. Fill the required
                // adaptive layers; leave optional monochrome layers unset.
                for (int layer = 0; layer < icon.minLayerCount; layer++) layers[layer] = logo;
                icon.SetTextures(layers);
            }
            PlayerSettings.SetPlatformIcons(target, kind, icons);
        }
    }
    static void WriteManifest()
    {
        string root = Application.streamingAssetsPath;
        var files = Directory.GetFiles(root, "*", SearchOption.AllDirectories)
            .Where(path => !path.EndsWith(".meta") && !path.EndsWith(".rec"))
            .Select(path => path.Substring(root.Length + 1).Replace('\\', '/')).OrderBy(path => path).ToArray();
        const string output = "Assets/Resources/AndroidStreamingFiles.json";
        File.WriteAllText(output, JsonUtility.ToJson(new AndroidDataFiles.Manifest { files = files }, true));
        AssetDatabase.ImportAsset(output);
    }
    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.Android) return;
        if (PlayerSettings.Android.splitApplicationBinary)
            throw new BuildFailedException("Android data installation requires a single APK; disable Split Application Binary.");
        LoadSigningKey();
        ConfigureLauncherIcons();
        WriteManifest();
    }
}
