using System;
using UnityEngine;

namespace Unified.UniversalBlur.Runtime
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ShowAsPass : PropertyAttribute
    {
        public string TargetMaterial { get; private set; }
        
        public ShowAsPass(string targetMaterial)
        {
            TargetMaterial = targetMaterial;
        }
    }
}