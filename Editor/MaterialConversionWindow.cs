using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using elZach.Common;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

[Serializable]
public class MaterialConversionWindow : EditorWindow
{
    [MenuItem("Window/Asset Conversion Tools/Material Conversion")]
    static void Init()
    {
        MaterialConversionWindow window = (MaterialConversionWindow)EditorWindow.GetWindow(typeof(MaterialConversionWindow));
        window.titleContent = new GUIContent("Material Conversion");
        window.minSize = new Vector2(250, 380);
        window.Show();
    }

    [Folder]
    [SerializeField] private string _path = "Assets/";

    public Material targetMaterial;
    [SerializeField] List<Entry> entries = new List<Entry>();
    private SerializedObject so;

    private Vector2 entriesScroll;

    void OnEnable()
    {
        so = new SerializedObject(this);
    }
    
    private void OnGUI()
    {
        EditorGUILayout.PropertyField(so.FindProperty(nameof(_path)));
        targetMaterial = EditorGUILayout.ObjectField(targetMaterial, typeof(Material), false) as Material;
        if (GUILayout.Button("Get Shaders From Materials"))
        {
            string[] allMats = targetMaterial ? new string[]{AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(targetMaterial))} 
                : AssetDatabase.FindAssets("t:material", new string[] {_path});
            foreach (var mat in allMats)
            {
                var someShader = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(mat)).shader;
                // Debug.Log(someShader.name);
                if (entries.All(x => x.From != someShader))
                {
                    entries.Add(new Entry(){From = someShader});
                }
                so.Update();
            }
        }

        if (GUILayout.Button("Get Missing Properties"))
        {
            foreach (var x in entries)
            {
                if (!x.From || !x.To) continue;
                var toProperties = x.To.GetPropertyNames();
                foreach (var fromProp in x.From.GetPropertyNames())
                    if (!toProperties.Contains(fromProp) && x.materialProperties.All(y => y.From != fromProp))
                        x.materialProperties.Add(new Entry.StringPair() {From = fromProp});
            }
            so.Update();
        }

        if (GUILayout.Button("Convert Materials"))
        {
            string[] allMats = targetMaterial ? new string[]{AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(targetMaterial))} 
                : AssetDatabase.FindAssets("t:material", new string[] {_path});
            foreach (var mat in allMats)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(mat));
                var replacementEntry = entries.FirstOrDefault(x => x.From == material.shader && x.To != null);
                if (replacementEntry == null) continue;

                foreach (var prop in replacementEntry.materialProperties.Where(x=>!string.IsNullOrEmpty(x.From) && !string.IsNullOrEmpty(x.To)))
                {
                    var index = replacementEntry.From.FindPropertyIndex(prop.From);
                    // var nameID = replacementEntry.From.GetPropertyNameId(index);
                    var propType = replacementEntry.From.GetPropertyType(index);
                    switch (propType)
                    {
                        case ShaderPropertyType.Color:
                            material.SetColor(prop.To,material.GetColor(prop.From));
                            break;
                        case ShaderPropertyType.Float:
                            material.SetFloat(prop.To, material.GetFloat(prop.From));
                            break;
                        case ShaderPropertyType.Int:
                            material.SetInt(prop.To, material.GetInt(prop.From));
                            break;
                        case ShaderPropertyType.Texture:
                            material.SetTexture(prop.To, material.GetTexture(prop.From));
                            break;
                        case ShaderPropertyType.Vector:
                            material.SetVector(prop.To, material.GetVector(prop.From));
                            break;
                    }
                }
                material.shader = replacementEntry.To;
                Debug.Log($"Converted Material {material.name} from {replacementEntry.From.name} to {replacementEntry.To.name}", material);
            }
        }

        entriesScroll = EditorGUILayout.BeginScrollView(entriesScroll);
        EditorGUILayout.PropertyField(so.FindProperty(nameof(entries)));
        EditorGUILayout.EndScrollView();
        
        so.ApplyModifiedProperties();
    }

    [Serializable]
    public class Entry
    {
        public Shader From, To;
        public List<StringPair> materialProperties = new List<StringPair>();

        [Serializable]
        public struct StringPair
        {
            public string From, To;
        }
    }
}
