using UnityEngine;
using UnityEditor;
using System.IO;

public class AssetPostProcessor : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (assetPath.Contains("Resources/Maps"))
        {
            TextureImporter importer = assetImporter as TextureImporter;

            string[] parser = Path.GetFileNameWithoutExtension(assetPath).Split('@');
            importer.isReadable = true;
            importer.filterMode = FilterMode.Point;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = int.Parse (parser[1]);
            importer.compressionQuality = 0;

            Object asset = AssetDatabase.LoadAssetAtPath(importer.assetPath, typeof(Texture2D));
            if (asset)
            {
                EditorUtility.SetDirty(asset);
            }
            else
            {
                importer.textureType = TextureImporterType.Default;
            }
        }
    }
}