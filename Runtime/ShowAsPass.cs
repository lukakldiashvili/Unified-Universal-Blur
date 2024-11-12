using System;
using UnityEngine;

namespace Unified.UniversalBlur.Runtime
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ShowAsPass : PropertyAttribute
    {
        public string TargetMaterialField { get; private set; }
        
        public ShowAsPass(string targetMaterialField)
        {
            TargetMaterialField = targetMaterialField;
        }
    }
}