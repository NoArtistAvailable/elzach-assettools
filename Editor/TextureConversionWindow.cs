using elZach.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using elZach.EditorHelper;

namespace elZach.AssetConversion
{
    public class TextureConversionWindow : EditorWindow
    {
        public Texture2D single;
        public int jpgQuality = 75;
        public bool scaling = true;
        public Vector2Int dimension = new Vector2Int(2048, 2048);
        public enum FilterMode { Point, Bilinear}
        public FilterMode filterMode = FilterMode.Bilinear;
        //public TextureFormat resizeFormat = TextureFormat.ARGB32;
        public RenderTextureFormat conversionFormat = RenderTextureFormat.Default;

        public enum FileFormat { PNG, JPG, EXR, TGA }
        public FileFormat fileFormat;
        public bool deleteOld = false;
        public string folderPath;

        [MenuItem("Window/Asset Conversion Tools/Texture Conversion")]
        static void Init()
        {
            TextureConversionWindow window = (TextureConversionWindow)EditorWindow.GetWindow(typeof(TextureConversionWindow));
            window.titleContent = new GUIContent("Texture Conversion");
            window.minSize = new Vector2(350, 200);
            window.Show();
        }

        private void OnGUI()
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
            if (Event.current.type == EventType.DragExited)
            {
                for (int i = 0; i < DragAndDrop.objectReferences.Length; i++)
                {
                    if (AssetDatabase.IsValidFolder(DragAndDrop.paths[0]))
                        folderPath = DragAndDrop.paths[0];
                    else if (DragAndDrop.objectReferences[i].GetType() == typeof(Texture2D))
                        single = DragAndDrop.objectReferences[i] as Texture2D;
                }
            }
            //-----------------------
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(EditorGUIUtility.IconContent("Texture Icon"), GUILayout.Width(22));
            EditorGUILayout.LabelField("Single", GUILayout.Width(60));
            single = (Texture2D)EditorGUILayout.ObjectField(single, typeof(Texture2D), false);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(EditorGUIUtility.IconContent("Folder Icon"), GUILayout.Width(22), GUILayout.Height(18)))
                folderPath = EditorUtility.OpenFolderPanel("Convert Images Folder", "Assets/", "Folder");
            EditorGUILayout.LabelField("Folder", GUILayout.Width(60));
            folderPath = EditorGUILayout.TextField(folderPath);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if(scaling = EditorGUILayout.Foldout(scaling,"Scale"))
                dimension = EditorGUILayout.Vector2IntField("",dimension);
            EditorGUILayout.EndHorizontal();
            fileFormat = (FileFormat)EditorGUILayout.EnumPopup("File Format",fileFormat);
            if (fileFormat == FileFormat.JPG) jpgQuality = EditorGUILayout.IntSlider("Quality",jpgQuality, 1, 100);
            filterMode = (FilterMode)EditorGUILayout.EnumPopup("Filter", filterMode);
            conversionFormat = (RenderTextureFormat)EditorGUILayout.EnumPopup("Conversion Format",conversionFormat);
            deleteOld = EditorGUILayout.Toggle("Replace Files", deleteOld);

            EditorGUILayout.BeginHorizontal();
            if(single!=null)
                if(GUILayout.Button("Convert Texture"))
                {
                    ConvertImage(single);
                    AssetDatabase.Refresh();
                }
            if(folderPath!=null && folderPath.Length>0)
                if (GUILayout.Button("Convert Folder"))
                {
                    ConvertImageFolder(folderPath);
                }
            EditorGUILayout.EndHorizontal();
        }

        public void ConvertImage(Texture2D tex)
        {
            string original_path = AssetDatabase.GetAssetPath(tex);
            Debug.Log(original_path);
            TextureImporter importer = TextureImporter.GetAtPath(original_path) as TextureImporter; //AssetDatabase.LoadAssetAtPath<TextureImporter>(original_path);
            bool wasNormal = importer.textureType == TextureImporterType.NormalMap;
            if (wasNormal)
            {
                //importer.isReadable
                importer.textureType = TextureImporterType.Default;
                importer.SaveAndReimport();
            }
            if (!deleteOld) original_path = AssetDatabase.GenerateUniqueAssetPath(original_path);
            string fileName = Path.GetFileNameWithoutExtension(original_path);
            string[] hierachy = original_path.Split(Char.Parse("/"));
            string path = "";
            for (int i = 0; i < hierachy.Length - 1; i++)
            {
                path += hierachy[i] + "/";
            }
            Texture2D newTex = tex.GetCopy(conversionFormat, RenderTextureReadWrite.sRGB);
            if (deleteOld) AssetDatabase.DeleteAsset(original_path);
            //Debug.Log(newTex.Resize(dimension.x, dimension.y, resizeFormat, false));
            if (newTex.width != dimension.x || newTex.height != dimension.y)
                switch (filterMode) {
                    case FilterMode.Bilinear:
                        TextureScale.Bilinear(newTex, dimension.x, dimension.y);
                        break;
                    case FilterMode.Point:
                        TextureScale.Point(newTex, dimension.x, dimension.y);
                        break;
                }
            //newTex.Apply();
            byte[] bytes = new byte[0];
            switch (fileFormat)
            {
                case FileFormat.JPG:
                    bytes = newTex.EncodeToJPG(jpgQuality);
                    path += fileName + ".jpg";
                    break;
                case FileFormat.PNG:
                    bytes = newTex.EncodeToPNG();
                    path += fileName + ".png";
                    break;
                case FileFormat.EXR:
                    bytes = newTex.EncodeToEXR();
                    path += fileName + ".exr";
                    break;
                case FileFormat.TGA:
                    bytes = newTex.EncodeToTGA();
                    path += fileName + ".tga";
                    break;
            }
            File.WriteAllBytes(path, bytes);
            if (wasNormal && !deleteOld)
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.SaveAndReimport();
            }
        }

        public void ConvertImageFolder(string path)
        {
            var info = new DirectoryInfo(path);
            var fileInfo = info.GetFiles();
            foreach (var file in fileInfo)
            {
                string ext = file.Extension.ToUpper();
                if (ext == ".TGA" || ext == ".PNG" || ext == ".JPG" || ext == ".EXR")
                {
                    var assetDataPath = Datahandling.EnsureAssetDataPath(path);
                    string relativePath = assetDataPath + "/" + file.Name;
                    Debug.Log(relativePath);
                    Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(relativePath);
                    Debug.Log(tex.name);
                    ConvertImage(tex);
                }
            }
            AssetDatabase.Refresh();
        }
    }
}