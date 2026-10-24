using System.IO;
using System.Linq;
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
        ConfigureLauncherIcons();
        WriteManifest();
        AssetDatabase.SaveAssets();
        Debug.Log("Android prepared: IL2CPP, ARM64, OpenGL ES 3, landscape. Build the existing Android profile with APK expansion disabled.");
    }
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
        ConfigureLauncherIcons();
        WriteManifest();
    }
}
