using UnityEngine;
using UnityEditor;

public class EnableMeshReadWrite
{
    [MenuItem("Tools/Enable Read-Write on All Meshes")]
    static void EnableAll()
    {
        string[] guids = AssetDatabase.FindAssets("t:Model");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer != null && !importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }
        }
        Debug.Log("Tous les meshes sont Read/Write !");
    }
}