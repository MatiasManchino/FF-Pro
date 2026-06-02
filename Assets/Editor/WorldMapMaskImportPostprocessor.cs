#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

internal class WorldMapMaskImportPostprocessor : AssetPostprocessor
{
    private static readonly string[] MaskNames = { "mask-water-land", "mask-ice" };

// Se ejecuta antes de procesar la textura.
    void OnPreprocessTexture()
    {
        if (!assetPath.Contains("/Map/Textures/"))
            return;

        string fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        if (System.Array.IndexOf(MaskNames, fileName) < 0)
            return;

        var importer = (TextureImporter)assetImporter;
        importer.isReadable = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
    }
}
#endif