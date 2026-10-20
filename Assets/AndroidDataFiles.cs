using System;
using System.IO;
using System.IO.Compression;
using UnityEngine;

public static class AndroidDataFiles
{
    [Serializable] public class Manifest { public string[] files; }
    // StreamingAssets cannot be enumerated as a filesystem directory inside an APK.
    public static void Install(string destination)
    {
        var manifestAsset = Resources.Load<TextAsset>("AndroidStreamingFiles");
        if (!manifestAsset) throw new InvalidOperationException("Android StreamingAssets manifest is missing. Use Tools/Android/Prepare project before building.");
        var manifest = JsonUtility.FromJson<Manifest>(manifestAsset.text);
        using (var stream = File.OpenRead(Application.dataPath))
        using (var apk = new ZipArchive(stream, ZipArchiveMode.Read))
            foreach (string relative in manifest.files)
            {
                string target = Path.Combine(destination, relative);
                string extension = Path.GetExtension(relative).ToLowerInvariant();
                if ((extension == ".jpg" || extension == ".json") && File.Exists(target)) continue;
                var entry = apk.GetEntry("assets/" + relative);
                if (entry == null) throw new FileNotFoundException("Missing APK asset: " + relative);
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                using (var input = entry.Open())
                using (var output = File.Create(target)) input.CopyTo(output);
            }
    }
}
