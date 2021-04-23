using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using elZach.Common;
using System.IO;

namespace elZach.Tools2D
{
    public class ImageSequencePackerWindow : EditorWindow
    {
        [MenuItem("Window/Tools/Sequence Packer")]
        static void Init()
        {
            var window = (ImageSequencePackerWindow)EditorWindow.GetWindow(typeof(ImageSequencePackerWindow));
            window.titleContent = new GUIContent("Image Sequence Packer");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        List<Texture2D> imageSequence = new List<Texture2D>();
        Vector2 imageSequenceScroll = Vector2.zero;

        public enum PivotMode { Center, TopeLeft, TopCenter, TopRight, LeftCenter, RightCenter, BottomLeft, BottomCenter, BottomRight }
        PivotMode pivotMode;

        float rowsColumnsWeigth = 0.5f;

        Vector2 rowsColumns = Vector2.one;

        Texture2D targetTexture;
        Vector2 targetTextureScroll = Vector2.zero;
        int cellWidth = 2, cellHeight = 2;

        float zoom = 1f;
        string lastOpenedPath = "Assets/";
        bool imageSequenceChanged = false;

        private void OnGUI()
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
            if (Event.current.type == EventType.DragExited)
            {
                for (int i = 0; i < DragAndDrop.objectReferences.Length; i++)
                {
                    if (DragAndDrop.objectReferences[i].GetType() == typeof(Texture2D))
                        imageSequence.Add(DragAndDrop.objectReferences[i] as Texture2D);
                    imageSequenceChanged = true;
                }
            }
            //------------------------------------------------------------------------------
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Images", GUILayout.Width(60));
            int targetCount = EditorGUILayout.DelayedIntField(imageSequence.Count);
            if (targetCount != imageSequence.Count)
            {
                if (targetCount > imageSequence.Count)
                {
                    int count = targetCount - imageSequence.Count;
                    for (int i = 0; i < count; i++)
                        imageSequence.Add(null);
                }
                else
                    imageSequence.RemoveRange(targetCount, imageSequence.Count - targetCount);
                imageSequenceChanged = true;
            }
            string addedFilePath = "", addedFolderPath = "";
            if (GUILayout.Button("Folder", GUILayout.Width(60)))
                addedFolderPath = EditorUtility.OpenFolderPanel("Add Folder to Sequence", lastOpenedPath, "opened folder");

            if (addedFolderPath.Length > 1)
            {
                addedFolderPath = addedFolderPath.Substring(Application.dataPath.Length);
                lastOpenedPath = "Assets" + addedFolderPath;
                foreach (var obj in GetAtPath<Texture2D>(addedFolderPath))
                {
                    imageSequence.Add((Texture2D)obj);
                }
                imageSequenceChanged = true;
            }

            if (GUILayout.Button("File", GUILayout.Width(60)))
                addedFilePath = EditorUtility.OpenFilePanel("Add File to Sequence", lastOpenedPath, "png,jpg,jpeg");
            if (addedFilePath.Length > 1)
            {
                addedFilePath = "Assets" + addedFilePath.Substring(Application.dataPath.Length);
                lastOpenedPath = addedFilePath;
                imageSequence.Add(AssetDatabase.LoadAssetAtPath<Texture2D>(addedFilePath));
                imageSequenceChanged = true;
            }

            if (GUILayout.Button("+", GUILayout.Width(60)))
            {
                imageSequence.Add(null);
                imageSequenceChanged = true;
            }
            if (GUILayout.Button("-", GUILayout.Width(60)))
            {
                if (imageSequence.Count > 0)
                    imageSequence.RemoveAt(imageSequence.Count - 1);
                imageSequenceChanged = true;
            }
            EditorGUILayout.EndHorizontal();
            //------------------------------------------------------------------------------
            if (imageSequence.Count > 0)
            {
                int? toDelete = null;
                if (imageSequence.Count > 7) imageSequenceScroll = EditorGUILayout.BeginScrollView(imageSequenceScroll, GUILayout.Height(20 * 8));
                for (int i = 0; i < imageSequence.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    imageSequence[i] = (Texture2D)EditorGUILayout.ObjectField(imageSequence[i], typeof(Texture2D), false);
                    if (GUILayout.Button("-", GUILayout.Width(20)))
                        toDelete = i;
                    EditorGUILayout.EndHorizontal();
                }
                if (imageSequence.Count > 7) EditorGUILayout.EndScrollView();
                if (toDelete != null)
                    imageSequence.RemoveAt((int)toDelete);

                EditorGUILayout.BeginHorizontal();
                rowsColumnsWeigth = EditorGUILayout.Slider("Rows & Columns", rowsColumnsWeigth, 0.01f, 0.99f);
                rowsColumns.x = Mathf.CeilToInt(imageSequence.Count * rowsColumnsWeigth);
                rowsColumns.y = Mathf.CeilToInt(imageSequence.Count / rowsColumns.x);
                EditorGUILayout.Vector2Field("", rowsColumns, GUILayout.Width(100));
                EditorGUILayout.EndHorizontal();

                if (GUI.changed || imageSequenceChanged)
                {
                    Convert();
                }

                if (targetTexture)
                {
                    EditorGUILayout.Separator();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Zoom", GUILayout.Width(40));
                    zoom = EditorGUILayout.Slider(zoom, 0.1f, 10f);
                    pivotMode = (PivotMode)EditorGUILayout.EnumPopup(pivotMode);
                    if (GUILayout.Button("Save SpriteSheet"))
                        SaveTexture();
                    EditorGUILayout.EndHorizontal();
                    float space = 10 + 18.5f * 5;
                    space += Mathf.Min(imageSequence.Count, 7) * 20f;
                    targetTextureScroll = EditorGUILayout.BeginScrollView(targetTextureScroll,
                        GUILayout.Width(position.width),
                        GUILayout.Height(Mathf.Min(position.height - space, targetTexture.height * zoom + 20)));
                    Rect rect = EditorGUILayout.GetControlRect(GUILayout.Width(targetTexture.width * zoom), GUILayout.Height(targetTexture.height * zoom));
                    EditorGUI.DrawTextureTransparent(rect, targetTexture);
                    EditorGUILayout.EndScrollView();
                }
                imageSequenceChanged = false;
            }
        }

        public void Convert()
        {
            cellWidth = 2;
            cellHeight = 2;
            foreach (var img in imageSequence)
            {
                if (img == null) continue;
                if (img.width > cellWidth) cellWidth = img.width;
                if (img.height > cellHeight) cellHeight = img.height;
            }

            targetTexture = new Texture2D(cellWidth * Mathf.CeilToInt(rowsColumns.x), cellHeight * Mathf.CeilToInt(rowsColumns.y));
            targetTexture.filterMode = FilterMode.Point;

            Color[] blank = new Color[targetTexture.width * targetTexture.height];
            Color transpBlack = new Color(0, 0, 0, 0);
            for (int i = 0; i < blank.Length; i++)
                blank[i] = transpBlack;

            targetTexture.SetPixels(blank);
            int index = 0;
            int centerX, centerY;
            centerX = Mathf.RoundToInt((float)cellWidth / 2f);
            centerY = Mathf.RoundToInt((float)cellWidth / 2f);

            RenderTexture previous = RenderTexture.active;

            for (int col = (int)rowsColumns.y - 1; col >= 0; col--)
            {
                for (int row = 0; row < rowsColumns.x; row++)
                {
                    DrawSpritesToTexture(ref index, col, row);
                }
            }
            targetTexture.Apply();
            RenderTexture.active = previous;
        }

        void DrawSpritesToTexture(ref int index, int col, int row)
        {
            var img = index < imageSequence.Count ? imageSequence[index] : null;
            if (img == null)
            {
                img = new Texture2D(1, 1);
                img.SetPixel(0, 0, new Color(0, 0, 0, 0));
            }
            Color[] pixels = img.GetCopy().GetPixels();

            int offsetX, offsetY;
            offsetX = (cellWidth - img.width) / 2;
            offsetY = (cellHeight - img.height) / 2;
            targetTexture.SetPixels(row * cellWidth + offsetX, col * cellHeight + offsetY, img.width, img.height, pixels);

            index++;
        }

        void SaveTexture()
        {
            string path = AssetDatabase.GetAssetPath(imageSequence[0]);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            path = System.IO.Path.GetDirectoryName(path) + "/" + name + "-sheet.png";

            byte[] _bytes = targetTexture.EncodeToPNG();
            System.IO.File.WriteAllBytes(path, _bytes);
            AssetDatabase.Refresh();
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;

            ti.isReadable = true;
            ti.spriteImportMode = SpriteImportMode.Multiple;

            TextureImporterSettings settings = new TextureImporterSettings();
            ti.ReadTextureSettings(settings);
            settings.textureType = TextureImporterType.Sprite;
            ti.SetTextureSettings(settings);

            var sheet = new List<SpriteMetaData>();
            int index = 0;
            Vector2 size = new Vector2(cellWidth, cellHeight);
            Debug.Log(ti.qualifiesForSpritePacking);
            for (int r = 0; r < rowsColumns.x; r++)
            {
                for (int c = 0; c < rowsColumns.y; c++)
                {
                    var meta = new SpriteMetaData();
                    meta.name = name + " " + index.ToString();
                    meta.alignment = (int)pivotMode;
                    meta.border = Vector4.zero;
                    meta.rect = new Rect(new Vector2(r * cellWidth, c * cellHeight), size);
                    sheet.Add(meta);
                    index++;
                }
            }
            ti.spritesheet = sheet.ToArray();
            UnityEditor.AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            Debug.Log("name: " + ti.spritesheet[0].name);
            //---
            Texture2D newFile = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            EditorGUIUtility.PingObject(newFile);
            Debug.Log("[Sequence Packker] Saved at " + path, newFile);
        }


        public static T[] GetAtPath<T>(string path)
        {

            ArrayList al = new ArrayList();
            string[] fileEntries = Directory.GetFiles(Application.dataPath + "/" + path);
            foreach (string fileName in fileEntries)
            {
                string localPath = "Assets" + fileName.Substring(Application.dataPath.Length);
                Object t = AssetDatabase.LoadAssetAtPath(localPath, typeof(T));

                if (t != null)
                    al.Add(t);
            }
            T[] result = new T[al.Count];
            for (int i = 0; i < al.Count; i++)
                result[i] = (T)al[i];

            return result;
        }
    }
}
