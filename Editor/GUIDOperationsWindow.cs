using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace elZach.AssetConversion
{
    public class GUIDOperationsWindow : EditorWindow
    {
        [MenuItem("Window/Asset Conversion Tools/GUID Operations")]
        static void Init()
        {
            GUIDOperationsWindow window = (GUIDOperationsWindow) EditorWindow.GetWindow(typeof(GUIDOperationsWindow));
            window.titleContent = new GUIContent("GUID Replacement");
            window.minSize = new Vector2(550, 380);
            window.Show();
        }

        string _path = "Assets/";
        private bool limitReferenceSearch = true;

        private Dictionary<string, (string, List<string>)> guidReferences =
            new Dictionary<string, (string, List<string>)>();

        private Vector2 scrollPos;

        void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(EditorGUIUtility.IconContent("Folder Icon"), GUILayout.Width(22),
                GUILayout.Height(18)))
            {
                _path = EditorUtility.OpenFolderPanel("Folder for GUID Operations", "Assets/", "Folder");
                _path = _path.Substring(Application.dataPath.Length);
                _path = "Assets" + _path;
            }

            EditorGUILayout.LabelField("Path:", GUILayout.Width(40));
            _path = EditorGUILayout.TextField(_path);
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Get Assets"))
            {
                guidReferences = new Dictionary<string, (string, List<string>)>();
                guidReferences.Add(AssetDatabase.AssetPathToGUID(_path), (_path, null));
                var guids = AssetDatabase.FindAssets("", new string[] {_path});
                foreach (var guid in guids)
                {
                    //Debug.Log($" {AssetDatabase.GUIDToAssetPath(guid)} : {guid}" );
                    guidReferences.Add(guid, (AssetDatabase.GUIDToAssetPath(guid), null));
                }
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Find All References"))
            {
                List<KeyValuePair<string, (string, List<string>)>> newEntries =
                    new List<KeyValuePair<string, (string, List<string>)>>();
                foreach (var kvp in guidReferences)
                {
                    newEntries.Add(new KeyValuePair<string, (string, List<string>)>(kvp.Key,
                        (kvp.Value.Item1, ReferenceFind(kvp.Key, limitReferenceSearch ? _path : null))));
                }

                guidReferences.Clear();
                foreach (var kvp in newEntries)
                    guidReferences.Add(kvp.Key, kvp.Value);
            }

            if (GUILayout.Button("Generate new GUIDs"))
            {
                List<KeyValuePair<string, (string, List<string>)>> newEntries =
                    new List<KeyValuePair<string, (string, List<string>)>>();
                foreach (var kvp in guidReferences)
                {
                    var newGuid = GUID.Generate().ToString();
                    ReplaceGUID(kvp.Key, newGuid, kvp.Value.Item2);
                    newEntries.Add(new KeyValuePair<string, (string, List<string>)>(newGuid, kvp.Value));
                }

                guidReferences.Clear();
                foreach (var kvp in newEntries)
                    guidReferences.Add(kvp.Key, kvp.Value);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            limitReferenceSearch = EditorGUILayout.Toggle(limitReferenceSearch, GUILayout.Width(25));
            EditorGUILayout.LabelField("Limit Reference Search To Selected Folder",GUILayout.Width(400));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            KeyValuePair<string, (string, List<string>)> toReplace = new KeyValuePair<string, (string, List<string>)>();
            string deleteKey = null;
            foreach (var guidRef in guidReferences)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent($"{guidRef.Value.Item1} : {guidRef.Key}"), "Label",
                    GUILayout.Width(position.width - 230)))
                {
                    var assetObject = AssetDatabase.LoadAssetAtPath<Object>(guidRef.Value.Item1);
                    EditorGUIUtility.PingObject(assetObject);
                }

                //UnityEditor.EditorGUILayout.LabelField($"{guidRef.Value.Item1} : {guidRef.Key}");
                string buttonText = guidRef.Value.Item2?.Count.ToString() ?? "Get References";
                if (GUILayout.Button(buttonText, GUILayout.Width(100)))
                {
                    toReplace = new KeyValuePair<string, (string, List<string>)>(guidRef.Key, (guidRef.Value.Item1,
                        ReferenceFind(guidRef.Key, limitReferenceSearch ? _path : null)));
                }

                if (GUILayout.Button("Replace GUID", GUILayout.Width(100)))
                {
                    var newGUID = GUID.Generate().ToString();
                    Debug.Log(newGUID);
                    deleteKey = guidRef.Key;
                    if (guidRef.Value.Item2 != null) ReplaceGUID(guidRef.Key, newGUID, guidRef.Value.Item2);
                    toReplace = new KeyValuePair<string, (string, List<string>)>(newGUID, guidRef.Value);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }

                EditorGUILayout.EndHorizontal();
            }

            if (!string.IsNullOrEmpty(deleteKey))
            {
                guidReferences.Remove(deleteKey);
                guidReferences.Add(toReplace.Key, toReplace.Value);
            }
            else if (!string.IsNullOrEmpty(toReplace.Key)) guidReferences[toReplace.Key] = toReplace.Value;

            EditorGUILayout.EndScrollView();
        }

        //Reference Finder Code from https://gist.github.com/ffyhlkain/2111681c1df404108837ffa5f71e0f68
        private Stopwatch searchTimer = new Stopwatch();
        private Object searchedObject;

        List<string> ReferenceFind(string guidToFind, string pathToSearchIn = null)
        {
            searchTimer.Reset();
            searchTimer.Start();
            if (string.IsNullOrEmpty(pathToSearchIn)) pathToSearchIn = Application.dataPath;
            else
                pathToSearchIn = Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length) +
                                 pathToSearchIn;

            List<string> referenceObjects = new List<string>();
            var pathToAsset = AssetDatabase.GUIDToAssetPath(guidToFind);
            if (!string.IsNullOrEmpty(pathToAsset))
            {
                searchedObject = AssetDatabase.LoadAssetAtPath<Object>(pathToAsset);

                var allPathToAssetsList = new List<string>();
                var allPrefabs = Directory.GetFiles(pathToSearchIn, "*.prefab",
                    SearchOption.AllDirectories);
                allPathToAssetsList.AddRange(allPrefabs);
                var allMaterials =
                    Directory.GetFiles(pathToSearchIn, "*.mat", SearchOption.AllDirectories);
                allPathToAssetsList.AddRange(allMaterials);
                var allScenes =
                    Directory.GetFiles(pathToSearchIn, "*.unity", SearchOption.AllDirectories);
                allPathToAssetsList.AddRange(allScenes);
                var allControllers = Directory.GetFiles(pathToSearchIn, "*.controller",
                    SearchOption.AllDirectories);
                allPathToAssetsList.AddRange(allControllers);
                var allVfxGraphs =
                    Directory.GetFiles(pathToSearchIn, "*.vfx", SearchOption.AllDirectories);
                allPathToAssetsList.AddRange(allVfxGraphs);
                var allShaderGraphs = Directory.GetFiles(pathToSearchIn, "*.shadergraph",
                    SearchOption.AllDirectories);
                allPathToAssetsList.AddRange(allShaderGraphs);

                string assetPath;
                for (int i = 0; i < allPathToAssetsList.Count; i++)
                {
                    assetPath = allPathToAssetsList[i];
                    var text = File.ReadAllText(assetPath);
                    if (text.Contains($"guid: {guidToFind}"))
                        referenceObjects.Add(assetPath);
                }

                searchTimer.Stop();
                return referenceObjects;
            }

            return null;
        }

        void ReplaceGUID(string originalGUID, string newGUID, IEnumerable<string> metas)
        {
            var myPath = AssetDatabase.GUIDToAssetPath(originalGUID);
            var myMetaPath = ToAbsolutePath(myPath) + ".meta";
            var myMeta = File.ReadAllText(myMetaPath);
            var newMeta = myMeta.Replace(originalGUID, newGUID);
            File.WriteAllText(myMetaPath, newMeta);
            AssetDatabase.ImportAsset(myPath);
            foreach (var assetPath in metas)
            {
                var text = File.ReadAllText(assetPath);
                var replace = text.Replace(originalGUID, newGUID);
                File.WriteAllText(assetPath, replace);
                Debug.Log($"Replaced guid in {assetPath}");
                AssetDatabase.ImportAsset(ToRelative(assetPath));
            }
        }

        static string ToAbsolutePath(string relative)
        {
            return Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length) + relative;
        }

        static string ToRelative(string absolute)
        {
            return "Assets" + absolute.Replace(Application.dataPath, "");
        }
    }
}
