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
            EditorGUI.BeginProperty(position, label, property);
            
            ShowAsPass passAttribute = (ShowAsPass) attribute;

            var target = property.serializedObject.targetObject;
            var targetMaterialField = passAttribute.TargetMaterialField;
            
            _targetType ??= target.GetType();
            _targetField ??= _targetType.GetField(targetMaterialField, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (_targetField != null)
            {
                var fieldValue = _targetField.GetValue(target);
            
                Material material = fieldValue as Material;

                if (material != null)
                {
                    var selectablePasses = GetPassIndexStringEntries(material);
                    var choiceIndex = EditorGUI.Popup(position, label, property.intValue, selectablePasses.ToArray());
                    
                    property.intValue = choiceIndex;
                }
                else
                {
                    EditorGUI.HelpBox(position, $"Incorrect target field or Material not set.", MessageType.Error);
                }
            }
            else
            {
                EditorGUI.HelpBox(position, $"Field {targetMaterialField} not found on {_targetType.Name}.", MessageType.Error);
            }
            
            EditorGUI.EndProperty();
        }
        
        private List<GUIContent> GetPassIndexStringEntries(Material material)
        {
            var passIndexEntries = new List<GUIContent>();
            for (int i = 0; i < material.passCount; ++i)
            {
                string entry = $"{material.GetPassName(i)} ({i})";
                passIndexEntries.Add(new GUIContent(entry));
            }

            return passIndexEntries;
        }
    }
}