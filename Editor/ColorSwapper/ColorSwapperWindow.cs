using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using elZach.Common;

namespace elZach.Tools2D
{
    public class ColorSwapperWindow : EditorWindow
    {
        public enum SwapMode { HueChange, SingleColors }
        SwapMode mode;
        SwapMode previousMode;

        Texture2D source;
        Texture2D previousSource;
        Texture2D recoloredImage;
        Vector2 recoloredImageScroll;
        float zoom = 1f;
        float previousZoom;

        float hueChange = 0f, saturationChange = 0f, valueChange = 0f;

        List<Color> sourceColors = new List<Color>();
        List<Color> targetColors = new List<Color>();
        Vector2 colorScrollPosition;

        Dictionary<Color, List<int>> colorMap;

        [MenuItem("Window/Tools/Color Swapper [PixelArt]")]
        static void Init()
        {
            ColorSwapperWindow window = (ColorSwapperWindow)EditorWindow.GetWindow(typeof(ColorSwapperWindow));
            window.titleContent = new GUIContent("Color Swapper");
            window.minSize = new Vector2(350, 350);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Source: ", EditorStyles.boldLabel);
            previousSource = source;
            source = (Texture2D)EditorGUILayout.ObjectField(source, typeof(Texture2D), false, GUILayout.Width(220));
            GUILayout.EndHorizontal();

            //float imageDistance = 0f;

            GUILayout.Space(10f);
            if (source)
            {
                //imageDistance = source.height * zoom;
                //GUILayout.Space(imageDistance + 25f);
                previousMode = mode;
                mode = (ColorSwapperWindow.SwapMode)EditorGUILayout.EnumPopup(mode);
                if (mode == SwapMode.HueChange)
                {
                    hueChange = EditorGUILayout.Slider("Hue", hueChange, -1f, 1f);
                    saturationChange = EditorGUILayout.Slider("Saturation", saturationChange, -1f, 1f);
                    valueChange = EditorGUILayout.Slider("Value", valueChange, -1f, 1f);
                }
                if (mode == SwapMode.SingleColors)
                {
                    if (targetColors.Count > 8)
                        colorScrollPosition = EditorGUILayout.BeginScrollView(colorScrollPosition, GUILayout.Height(160));
                    for (int i = 0; i < targetColors.Count; i++)
                    {
                        targetColors[i] = EditorGUILayout.ColorField(targetColors[i]);
                    }
                    if (targetColors.Count > 8)
                        EditorGUILayout.EndScrollView();
                }
                //------swap
                if (GUI.changed == true || recoloredImage == null)
                {
                    if (sourceColors?.Count == 0 || colorMap == null || recoloredImage == null || source != previousSource) GetColors();
                    // if (zoom != previousZoom || source != previousSource || mode != previousMode) ResizeToContent();
                    SwapColors();
                }
                EditorGUILayout.Separator();
                previousZoom = zoom;
                zoom = EditorGUILayout.Slider("Zoom", zoom, 0.1f, 10f);
                GUILayout.Space(10f);
                if (GUILayout.Button("Save Duplicate"))
                {
                    //Debug.Log("Implement Save");
                    SaveTex(source, recoloredImage);
                }
                recoloredImageScroll = EditorGUILayout.BeginScrollView(recoloredImageScroll, 
                    GUILayout.MaxWidth(position.width), 
                    GUILayout.MaxHeight(position.height - 60f));
                Rect rect = EditorGUILayout.GetControlRect(GUILayout.Width(recoloredImage.width * zoom), GUILayout.Height(recoloredImage.height * zoom));
                EditorGUI.DrawTextureTransparent(rect, recoloredImage);
                EditorGUILayout.EndScrollView();
                //GUILayout.EndHorizontal();
            }

        }

        void ResizeToContent()
        {
            var window = EditorWindow.GetWindow<ColorSwapperWindow>();
            int extraHeigth = 30;
            if (mode == SwapMode.SingleColors)
                extraHeigth = 17 * Mathf.Min(targetColors.Count, 8);
            Vector2 size = new Vector2(
                Mathf.Max(source.width * zoom + 20, 300),
                source.height * zoom + 170 + extraHeigth);
            window.minSize = size;
            window.maxSize = new Vector2(1920, size.y);
        }

        public void GetColors()
        {
            sourceColors.Clear();
            targetColors.Clear();

            hueChange = 0f;
            saturationChange = 0f;
            valueChange = 0f;

            colorMap = new Dictionary<Color, List<int>>();

            Color[] pixels = source.GetCopy().GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a == 0) continue;
                if (!colorMap.ContainsKey(pixels[i]))
                {
                    colorMap.Add(pixels[i], new List<int>());
                    sourceColors.Add(pixels[i]);
                    targetColors.Add(pixels[i]);
                }
                colorMap[pixels[i]].Add(i);
            }
        }

        void SwapColors()
        {
            int width = source.width;
            int height = source.height;
            recoloredImage = new Texture2D(width, height);
            recoloredImage.filterMode = FilterMode.Point;
            //---blank
            Color[] blank = new Color[width * height];
            Color transpBlack = new Color(0, 0, 0, 0);
            for (int i = 0; i < blank.Length; i++)
                blank[i] = transpBlack;
            recoloredImage.SetPixels(blank);
            //---
            foreach (var kvp in colorMap)
            {
                Color c = kvp.Key;
                int index = sourceColors.FindIndex(x => x == kvp.Key);
                if (mode == SwapMode.HueChange)
                {
                    float h, s, v;
                    Color.RGBToHSV(c, out h, out s, out v);
                    //Debug.Log("h: " + h + "; s:" + s + "; v:" + v);
                    h += hueChange;
                    h = h > 0f ? h : 1f + h;
                    h %= 1f;
                    s += saturationChange;
                    v += valueChange;
                    c = Color.HSVToRGB(h, s, v);
                    targetColors[index] = c;
                }
                else if (mode == SwapMode.SingleColors)
                    c = targetColors[sourceColors.FindIndex(x => x == kvp.Key)];

                foreach (int i in kvp.Value)
                {
                    var pos = IndexToColumnRow(i, width, height);
                    recoloredImage.SetPixel((int)pos.x, (int)pos.y, c);
                }
            }
            recoloredImage.Apply();
        }

        public static void SaveTex(Texture2D source, Texture2D newImage)
        {
            var bytes = newImage.EncodeToPNG();
            string source_path = UnityEditor.AssetDatabase.GetAssetPath(source);
            string path = //System.IO.Path.GetFileNameWithoutExtension(path);
                UnityEditor.AssetDatabase.GenerateUniqueAssetPath(source_path);
            System.IO.File.WriteAllBytes(path, bytes);

            UnityEditor.AssetDatabase.Refresh();
            //---
            string copyFromPath = UnityEditor.AssetDatabase.GetAssetPath(source);
            UnityEditor.TextureImporter ti1 = UnityEditor.AssetImporter.GetAtPath(copyFromPath) as UnityEditor.TextureImporter;
            if (ti1.spriteImportMode == UnityEditor.SpriteImportMode.Multiple)
            {
                bool previousReadable = ti1.isReadable;
                ti1.isReadable = true;

                string copyToPath = path;
                UnityEditor.TextureImporter ti2 = UnityEditor.AssetImporter.GetAtPath(copyToPath) as UnityEditor.TextureImporter;
                ti2.isReadable = true;

                ti2.spriteImportMode = UnityEditor.SpriteImportMode.Multiple;
                ti2.spritePixelsPerUnit = ti1.spritePixelsPerUnit;

                string previousName = System.IO.Path.GetFileNameWithoutExtension(source_path);
                string newName = System.IO.Path.GetFileNameWithoutExtension(path);
                List<UnityEditor.SpriteMetaData> newData = new List<UnityEditor.SpriteMetaData>();
                for (int i = 0; i < ti1.spritesheet.Length; i++)
                {
                    UnityEditor.SpriteMetaData d = ti1.spritesheet[i];
                    d.name = d.name.Replace(previousName, newName);
                    newData.Add(d);
                }
                ti2.spritesheet = newData.ToArray();

                ti1.isReadable = previousReadable;
                ti2.isReadable = previousReadable;

                UnityEditor.AssetDatabase.ImportAsset(copyToPath, UnityEditor.ImportAssetOptions.ForceUpdate);
            }
            //---
            Texture2D newFile = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            EditorGUIUtility.PingObject(newFile);
            Debug.Log("[Color Swapper] Saved at " + path, newFile);
        }
        public static Vector2 IndexToColumnRow(int value, int width, int height)
        {
            int y = Mathf.FloorToInt((float)value / (float)width);
            int x = value - y * width;
            return new Vector2(x, y);
        }
    }
}
