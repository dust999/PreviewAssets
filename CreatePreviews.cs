using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Threading;

class CreatePreviews : EditorWindow
{    
    private Object _sourcePath;
    private string _outPath = "";
    private string _folder = "Previews";
    private string _filterPrefab = "prefab";

    private List<string> _prefabsPaths = new List<string>();
    private List<string> _imagesPaths;

    [MenuItem("Tools/Create Previews")]
    static void Init()
    {
        CreatePreviews window = GetWindow<CreatePreviews>("Create Previews");
        window.position = new Rect(0, 0, 400, 200);
        window.Show();
    }

    void OnGUI()
    {
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(_outPath);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
       
        string newFolder = (_sourcePath == null) ? _folder : _sourcePath.name; 
        _folder = EditorGUILayout.TextField("Output Folder", newFolder);
        
        EditorGUILayout.EndHorizontal();

        _outPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop), _folder);

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        _sourcePath = EditorGUILayout.ObjectField("Path", _sourcePath, typeof(Object), false);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        if (!GUILayout.Button("Make Previews", GUILayout.Height(100))) return;

        if (_sourcePath == null)
        {
            ShowNotification(new GUIContent("Select folder first"));
            return;
        }

        string folderWithPrefabs = AssetDatabase.GetAssetPath(_sourcePath);

        GetAllAssets( folderWithPrefabs, ref _prefabsPaths); 

        if (_prefabsPaths.Count == 0)
        {
            ShowNotification(new GUIContent("No Assets was founded"));
            return;
        }

        Debug.Log("Total assets: " + _prefabsPaths.Count);
        Directory.CreateDirectory(_outPath);

        _imagesPaths = new List<string>();

        for (int i = 0; i < _prefabsPaths.Count; i++)
        {
            string progresBar = string.Concat("Prefab: ", i.ToString() , " / ", _prefabsPaths.Count.ToString() );
            float progress = (float)i / _prefabsPaths.Count;

            if(EditorUtility.DisplayCancelableProgressBar("Creating previews ...", progresBar, progress))
            {
                EditorUtility.ClearProgressBar();
                return;
            }

            MakePreview(_prefabsPaths[i]);
        }

        EditorUtility.ClearProgressBar();

        CreateInfoHtml();

        AssetDatabase.Refresh();

        ShowNotification(new GUIContent("Done"));
    }

    void GetAllAssets(string folder, ref List<string> prefabs)
    {
        string[] assetsPaths = AssetDatabase.GetAllAssetPaths();

        prefabs = new List<string>();

        foreach (string assetPath in assetsPaths)
        { 
            if (!assetPath.Contains(folder) || 
                !assetPath.Contains(_filterPrefab))
            {
                continue;
            }

            prefabs.Add(assetPath);
        }
    }

    void MakePreview(string assetPath)
    {
        Object asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath) as Object;

        Texture2D texture = AssetPreview.GetAssetPreview(asset);

        while (texture == null)
        {
            Thread.Sleep(10);
            texture = AssetPreview.GetAssetPreview(asset);
        }

        if (texture == null)
        {
            Debug.Log("Error" + assetPath);
        }

        string[] folders = assetPath.Split('/');
        string prefix = folders[folders.Length - 2];

        string imageName = string.Concat(prefix, "_@_", asset.name);

        string outFileName = Path.Combine(_outPath, imageName);

        outFileName = string.Concat(outFileName, ".png");

        _imagesPaths.Add(outFileName);

        SaveTextureToFile (texture , outFileName);
    }

    void SaveTextureToFile(Texture2D texture, string filename)
    {
        try
        {
            File.WriteAllBytes(filename, texture.EncodeToPNG());
        }
        catch
        {
            EditorUtility.ClearProgressBar();
        }
    }

    void CreateInfoHtml()
    {
        string html = CreateHTMLTable();

        if (string.IsNullOrEmpty(html))
        {
            AssetDatabase.Refresh();
            return;
        }

        string htmlPath = Path.Combine(_outPath, "!info.html");

        WriteHTML(htmlPath, html);
    }

    string CreateHTMLTable()
    {

        if(_imagesPaths.Count != _prefabsPaths.Count)
        {
            ShowNotification(new GUIContent("Prefabs and images not equals"));
            return "";
        }

        string table    = "<table>\n";
        string endTable = "\n</table>";
        string tr       = "\t<tr>\n";
        string endTr    = "\t</tr>\n";
        string th       = "\t\t<th>";
        string endTh    = "</th>\n";
        string td       = "\t\t<td>";
        string endTd    = "</td>\n";
        string footerTd = "<td colspan=\"3\">";
        string img      = "<img src=\"";
        string endImg   = "\">";

        string html = table;

        html = string.Concat(html, tr, th, "Image", endTh, th, "Folder", endTh, th, "Name", endTh, endTr);

        for(int i = 0; i < _imagesPaths.Count; i++)
        {
            html = string.Concat(html, tr, td , img, GetRelativePath(_imagesPaths[i]), endImg, endTd, td,  GetImageName(_imagesPaths[i], true), endTd, td, GetImageName(_imagesPaths[i]), endTd, endTr);
        }

        html = string.Concat(html, tr, footerTd, "Total: ", _imagesPaths.Count.ToString(), endTd, endTd, endTr);

        html = string.Concat(html, endTable);

        return html;
    }

    string GetRelativePath(string path)
    {
        char separator = Path.DirectorySeparatorChar;
        string[] parts = path.Split(separator);

       return parts[parts.Length - 1];
       
    }

    string GetImageName(string path, bool isFolder = false)
    {
        string prefabName = GetRelativePath(path);
        prefabName = prefabName.Replace(".png", "");
        string[] prefabNameParts = prefabName.Split(new string []{ "_@_"}, System.StringSplitOptions.None);

        return (isFolder) ? prefabNameParts[0] : prefabNameParts[1];
    }

    void WriteHTML(string path, string html)
    {
        File.WriteAllText(path, html);
    }
}