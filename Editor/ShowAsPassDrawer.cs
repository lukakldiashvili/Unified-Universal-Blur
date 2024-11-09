using System;
using System.Collections.Generic;
using System.Reflection;
using Unified.UniversalBlur.Runtime;
using UnityEditor;
using UnityEngine;

namespace Unified.UniversalBlur.Editor
{
    [CustomPropertyDrawer(typeof(ShowAsPass))]
    public class ShowAsPassDrawer : PropertyDrawer
    {
        private Type _targetType;
        private FieldInfo _targetField;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ShowAsPass passAttribute = (ShowAsPass) attribute;

            var target = property.serializedObject.targetObject;
            
            _targetType ??= target.GetType();
            _targetField ??= _targetType.GetField(passAttribute.TargetMaterial, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (_targetField != null)
            {
                var fieldValue = _targetField.GetValue(target);
            
                Material material = fieldValue as Material;
            
                var selectablePasses = GetPassIndexStringEntries(material);

                EditorGUI.BeginProperty(position, label, property);
                var choiceIndex = EditorGUI.Popup(position, "Pass Index", property.intValue, selectablePasses.ToArray());

                property.intValue = choiceIndex;
            }
            else
            {
                EditorGUI.HelpBox(position, $"Field {passAttribute.TargetMaterial} not found in {_targetType.Name}", MessageType.Error);
            }

            EditorGUI.EndProperty();
        }
        
        private List<string> GetPassIndexStringEntries(Material material)
        {
            List<string> passIndexEntries = new List<string>();
            for (int i = 0; i < material.passCount; ++i)
            {
                string entry = $"{material.GetPassName(i)} ({i})";
                passIndexEntries.Add(entry);
            }

            return passIndexEntries;
        }
    }
}