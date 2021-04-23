using UnityEngine;
using System;

public class RenameAnimationClips : MonoBehaviour
{
    public string path;
    public string prefix;
    public bool forceLooping;

    [Header("Packing")]
    public GameObject meshObject;

    class Container
    {
        public Type typeOf;
        public string path;
        public AnimationCurve curve;

        public Container(Type typeOf, string path )
        {
            this.typeOf = typeOf;
            this.path = path;
            curve = new AnimationCurve();
        }
    }

    //[Button("Pack Animation")]
    


    //[Button("Rename Clips To Filename")]
    

    //[Button("Make Clips Loopable")]
    //public void MakeClipsLoopable()
    //{
    //    MakeClipsLoopable(meshObject);
    //}

    

    //[Button("Add Prefix to Clipnames")]
    //public void AddPrefix()
    //{
    //    AddPrefix(meshObject, prefix);
    //}

    
    
}
