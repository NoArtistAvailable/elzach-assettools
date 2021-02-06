using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using Autodesk.Fbx;
using UnityEditor.Formats.Fbx.Exporter;
using UnityEditor.Animations;
using elZach.Common;

namespace elZach.AssetConversion
{
    public class AnimationConversionWindow : EditorWindow
    {
        [MenuItem("Window/Asset Conversion Tools/Animation Conversion")]
        static void Init()
        {
            AnimationConversionWindow window = (AnimationConversionWindow)EditorWindow.GetWindow(typeof(AnimationConversionWindow));
            window.titleContent = new GUIContent("Animation Conversion");
            window.minSize = new Vector2(250, 200);
            window.Show();
        }

        string _path = "Assets/";
        string _prefix;
        bool _forceLooping;
        GameObject _meshObject;

        static bool alwaysFalse = false;

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(EditorGUIUtility.IconContent("Folder Icon"), GUILayout.Width(22), GUILayout.Height(18)))
            {
                _path = EditorUtility.OpenFolderPanel("Convert Animation Folder", "Assets/", "Folder");
                _path = _path.Substring(Application.dataPath.Length);
                _path = "Assets" + _path;
            }
            EditorGUILayout.LabelField("Path:", GUILayout.Width(40));
            _path = EditorGUILayout.TextField(_path);
            EditorGUILayout.EndHorizontal();
            _meshObject = (GameObject) EditorGUILayout.ObjectField("FBX",_meshObject, typeof(GameObject),false);
            _prefix = EditorGUILayout.TextField("Prefix:", _prefix);
            _forceLooping = EditorGUILayout.Toggle("Force Looping:",_forceLooping);

            EditorGUILayout.HelpBox("Animation and Rig need to be set to generic for packing process.", MessageType.Info);
            EditorGUILayout.HelpBox("Recommended to check FBX export configuration for binary instead of ASCII.", MessageType.Info);

            if (GUILayout.Button("Pack Animations of path into combined fbx"))
                PackAnimations(_path, _meshObject);

            if (GUILayout.Button("Rename Clips in Path like Filenames"))
                RenameAllAtPath(_path, _prefix, _forceLooping);
            if (GUILayout.Button("Rename Clips in FBX like Filename"))
                RenameClips(_meshObject, _prefix, _forceLooping);
            if (GUILayout.Button("Add Prefix To Clipnames in FBX"))
                AddPrefix(_meshObject, _prefix);
            if (GUILayout.Button("Make Clips in FBX loop"))
                MakeClipsLoopable(_meshObject);
        }

        class Container
        {
            public Type typeOf;
            public string path;
            public AnimationCurve curve;

            public Container(Type typeOf, string path)
            {
                this.typeOf = typeOf;
                this.path = path;
                curve = new AnimationCurve();
            }
        }

        public static void PackAnimations(string path, GameObject meshObject)
        {
            List<AnimationClip> clips = new List<AnimationClip>();
            List<ModelImporterClipAnimation> clipDatas = new List<ModelImporterClipAnimation>();

            var info = new DirectoryInfo(path);
            var fileInfo = info.GetFiles();
            foreach (var file in fileInfo)
            {
                if (file.Extension == ".fbx" || file.Extension == ".FBX")
                {
                    string relativePath = path + "/" + file.Name;
                    AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(relativePath);
                    //Debug.Log(relativePath);
                    if (clip != null)
                    {
                        clips.Add(clip);
                    }
                    if (alwaysFalse)
                    {
                        ModelImporter importer = AssetDatabase.LoadAssetAtPath<ModelImporter>(relativePath);
                        if (importer != null)
                        {
                            clipDatas.AddRange(importer.clipAnimations);
                        }
                    }
                }
            }
            Debug.Log(clips.Count);

            //---getting all curves/keyframes---
            if (alwaysFalse)
            {
                AnimationClip collectiveClip = new AnimationClip();
                Dictionary<string, Container> collectiveCurves = new Dictionary<string, Container>();
                float timeOffset = 0f;
                float maxTime = 0f;
                for (int i = 0; i < clips.Count; i++)
                {
                    var bindings = AnimationUtility.GetCurveBindings(clips[i]);
                    Debug.Log("Clip[i] Count of bindings: " + bindings.Length);
                    foreach (var binding in bindings)
                    {
                        Debug.Log(binding.propertyName + ": Path: " + binding.path);
                        if (!collectiveCurves.ContainsKey(binding.propertyName))
                        {
                            collectiveCurves.Add(binding.propertyName, new Container(binding.type, binding.path));
                        }
                        var curveToWriteTo = collectiveCurves[binding.propertyName];
                        var curveToReadFrom = AnimationUtility.GetEditorCurve(clips[i], binding);
                        //Debug.Log("Keys: " + curve.keys.Length);
                        maxTime = Mathf.Max(maxTime, curveToReadFrom.GetLastKey().time);
                        for (int frame = 0; frame < curveToReadFrom.length; frame++)
                        {
                            var keyFrame = curveToReadFrom.keys[frame];
                            keyFrame.time += timeOffset;
                            curveToWriteTo.curve.AddKey(keyFrame);
                        }
                        //collectiveClip.SetCurve()
                    }
                    timeOffset += maxTime;
                }

                foreach (var kvp in collectiveCurves)
                {
                    collectiveClip.SetCurve(kvp.Value.path, kvp.Value.typeOf, kvp.Key, kvp.Value.curve);
                }
            }

            //---instantiating go and adding animation component
            GameObject clone = Instantiate(meshObject);
            clone.name = meshObject.name;
            if (alwaysFalse)
            {
                var anim = clone.AddComponent<Animation>();
                AnimationUtility.SetAnimationClips(anim, clips.ToArray());
            }
            if (true)
            {
                var anim = clone.GetComponent<Animator>();
                if (!anim) anim = clone.AddComponent<Animator>();
                var controller = AnimatorController.CreateAnimatorControllerAtPath(path + "/temp.controller");
                foreach (var clip in clips)
                    controller.AddMotion(clip);

                anim.runtimeAnimatorController = controller;
            }

            if (alwaysFalse)
            {
                var fbxManager = FbxManager.Create();
                var settings = fbxManager.GetIOSettings();
                var exporter = FbxExporter.Create(fbxManager, "name");
                //exporter.
            }
            if (true)
                ModelExporter.ExportObject(path + "/combined.fbx", clone);

            if (alwaysFalse)
            {
                using (FbxManager fbxManager = FbxManager.Create())
                {
                    // configure IO settings.
                    var settings = FbxIOSettings.Create(fbxManager, Globals.IOSROOT);
                    fbxManager.SetIOSettings(settings);
                    // Export the scene
                    using (FbxExporter exporter = FbxExporter.Create(fbxManager, "myExporter"))
                    {

                        // Initialize the exporter.
                        bool status = exporter.Initialize("combindObjects", -1, fbxManager.GetIOSettings());

                        // Create a new scene to export
                        FbxScene scene = FbxScene.Create(fbxManager, "myScene");
                        //FbxObject obj = FbxObject.Create(fbxManager, "combinedThings");
                        // Export the scene to the file.
                        exporter.Export(scene);
                    }
                }
            }


            //AssetDatabase.CreateAsset(collectiveClip, path + "/combined.anim");
            //AssetDatabase.SaveAssets();

            //Animation newAnimation = GetComponent<Animation>();
            //Debug.Log(newAnimation.name);
            //AnimationUtility.SetAnimationClips(newAnimation, clips.ToArray());
            //AssetDatabase.CreateAsset(newAnimation, path + "/combined.fbx");
            //AssetDatabase.CreateAsset(clips, path + "/testasset.fbx");
        }

        public static void RenameAllAtPath(string path, string prefix, bool forceLooping = false)
        {
            var info = new DirectoryInfo(path);
            var fileInfo = info.GetFiles();
            foreach (var file in fileInfo)
            {
                if (file.Extension == ".fbx")
                {
                    string relativePath = path + "/" + file.Name;
                    Debug.Log(relativePath);
                    GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(relativePath);
                    Debug.Log(go.name);
                    RenameClips(go, prefix, forceLooping);
                }
            }
        }

        public static void RenameClips(GameObject asset, string prefix, bool forceLooping = false)
        {
            var path = AssetDatabase.GetAssetPath(asset);
            Debug.Log("Path: " + path);
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            Debug.Log("Clipcount: " + importer.defaultClipAnimations.Length);
            ModelImporterClipAnimation[] animationClips = new ModelImporterClipAnimation[importer.defaultClipAnimations.Length];
            //foreach(var nfo in importer.importedTakeInfos)
            //{
            //    Debug.Log(nfo.defaultClipName);
            //}
            for (int i = 0; i < importer.defaultClipAnimations.Length; i++)
            {

                animationClips[i] = importer.defaultClipAnimations[i];
                Debug.Log(animationClips[i].takeName);
                if (forceLooping)
                {
                    animationClips[i].loop = true;
                    animationClips[i].loopTime = true;
                }
                animationClips[i].name = asset.name;
            }
            importer.clipAnimations = animationClips;
            importer.SaveAndReimport();
            if (asset.name.Length < prefix.Length || asset.name.Substring(0, prefix.Length) != prefix)
            {
                AssetDatabase.RenameAsset(path, prefix + asset.name);
            }
        }

        public static void MakeClipsLoopable(GameObject asset)
        {
            var path = AssetDatabase.GetAssetPath(asset);
            Debug.Log("Path: " + path);
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            Debug.Log("Clipcount: " + importer.clipAnimations.Length);
            ModelImporterClipAnimation[] animationClips = new ModelImporterClipAnimation[importer.clipAnimations.Length];
            for (int i = 0; i < importer.clipAnimations.Length; i++)
            {
                animationClips[i] = importer.clipAnimations[i];
                Debug.Log(animationClips[i].takeName);
                animationClips[i].loop = true;
                animationClips[i].loopTime = true;

            }
            importer.clipAnimations = animationClips;
            importer.SaveAndReimport();
        }

        public static void AddPrefix(GameObject asset, string prefix)
        {
            var path = AssetDatabase.GetAssetPath(asset);
            Debug.Log("Path: " + path);
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            Debug.Log("Clipcount: " + importer.clipAnimations.Length);
            ModelImporterClipAnimation[] animationClips = new ModelImporterClipAnimation[importer.clipAnimations.Length];
            for (int i = 0; i < importer.clipAnimations.Length; i++)
            {
                animationClips[i] = importer.clipAnimations[i];
                if (animationClips[i].name.Length < prefix.Length || animationClips[i].name.Substring(0, prefix.Length).ToUpper() != prefix.ToUpper())
                    animationClips[i].name = prefix + animationClips[i].name;
            }
            importer.clipAnimations = animationClips;
            importer.SaveAndReimport();
        }
    }
}
