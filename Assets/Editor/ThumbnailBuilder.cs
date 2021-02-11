using System;
using System.Linq;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using Newtonsoft.Json;

public class CaptureObject : MonoBehaviour
{
}
public class ThumbnailBuilder : MonoBehaviour
{
    private static readonly int TextureSize = 512;
    private static readonly RenderTextureFormat RenderTextureFormat = RenderTextureFormat.ARGB32;
    private static readonly TextureFormat TextureFormat = TextureFormat.ARGB32;

    [MenuItem("Util/TakeShot")]
    public static void TakeShot(string path = "a.png")
    {
        var rt = new RenderTexture(TextureSize, TextureSize, 24, RenderTextureFormat);
        var tex2d = new Texture2D(rt.width, rt.height, TextureFormat, false);

        var cam = Camera.main;
        cam.targetTexture = rt;
        cam.Render();
        cam.targetTexture = null;

        RenderTexture.active = rt;
        tex2d.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex2d.Apply();
        RenderTexture.active = null;

        File.WriteAllBytes(path, tex2d.EncodeToPNG());
    }

    [MenuItem("Util/BuildThumbnails_Entity")]
    public static void BuildThumbnails_Field()
    {
        BuildThumbnails("Entity", "entity");
    }

    private static void BuildThumbnails(string basepath, string dbname)
    {
        var s = string.Join(", ", AssetDatabase.GetAllAssetPaths());
        Debug.Log(s);

        var paths = Directory.GetFiles($"Assets\\Resources\\{basepath}", "*.prefab", SearchOption.AllDirectories)
            .ToArray();

        var dic = new Dictionary<string, EntityData>();
        var cam = Camera.main;
        foreach (var path in paths)
        {
            var p = path.Substring("Assets\\Resources\\".Length);
            p = p.Substring(0, p.Length - 7);

            Debug.Log(p);

            var prefab = Resources.Load<GameObject>(p);
            //var prefab = AssetDatabase.LoadAssetAtPath(path, typeof(GameObject));
            var obj = (GameObject)Instantiate(prefab, cam.transform);
            obj.transform.localPosition = new Vector3(0, -0.4f, 3);
            obj.transform.localEulerAngles = new Vector3(-15, 45, -15);

            var dir = Path.GetDirectoryName(path);
            var fn = Path.GetFileNameWithoutExtension(path);

            Debug.Log(dir);

            var thumbnailPath = $"{dir}/{fn}_thumbnail.png";
            TakeShot(thumbnailPath);

            DestroyImmediate(obj);

            AssetDatabase.ImportAsset(thumbnailPath);

            var importer = (TextureImporter)AssetImporter.GetAtPath(thumbnailPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.SaveAndReimport();

            var splited = p.Split('\\');

            dic.Add(fn, new EntityData()
            {
                name = fn,
                category = splited.Length >= 3 ? splited[1] : "None",
                prefabPath = p.Replace("\\", "/")
            });
        }

        var json = JsonConvert.SerializeObject(dic, Formatting.Indented);
        File.WriteAllText($"Assets/Resources/DB/{dbname}.json", json);
    }
}
