//Credits to: https://github.com/Unity-Technologies/Graphics

using System.Collections.Generic;
using Unified.UniversalBlur.Runtime;
using UnityEditor;
using UnityEditor.Rendering;

namespace Unified.UniversalBlur.Editor
{
    /// <summary>
    /// Custom editor for FullScreenPassRendererFeature class responsible for drawing unavailable by default properties
    /// such as custom drop down items and additional properties.
    /// </summary>
    [CustomEditor(typeof(UniversalBlurFeature))]
    public class UniversalBlurFeatureEditor : UnityEditor.Editor
    {
        private UniversalBlurFeature m_AffectedFeature;
        private EditorPrefBool m_ShowAdditionalProperties;
        private int m_PassIndexToUse = 0;

        /// <summary>
        /// A toggle that is responsible whether additional properties are shown.
        /// This toggle also sets pass index to 0 when toggle's value changes.
        /// </summary>
        public bool showAdditionalProperties
        {
            get => m_ShowAdditionalProperties.value;
            set
            {
                if (value != m_ShowAdditionalProperties.value)
                {
                    m_PassIndexToUse = 0;
                }
                m_ShowAdditionalProperties.value = value;
            }
        }

        /// <summary>
        /// Implementation for a custom inspector
        /// </summary>
        public override void OnInspectorGUI()
        {
            DrawPropertiesExcluding(serializedObject, "m_Script");
            m_AffectedFeature = target as UniversalBlurFeature;

            if (showAdditionalProperties)
            {
                DrawAdditionalProperties();
            }

            m_AffectedFeature.PassIndex = m_PassIndexToUse;

            EditorUtility.SetDirty(target);
        }

        private void DrawAdditionalProperties()
        {
            List<string> selectablePasses;
            bool isMaterialValid = m_AffectedFeature.PassMaterial != null;
            selectablePasses = isMaterialValid ? GetPassIndexStringEntries(m_AffectedFeature) : new List<string>() { "No material" };

            // If material is invalid 0'th index is selected automatically, so it stays on "No material" entry
            // It is invalid index, but FullScreenPassRendererFeature wont execute until material is valid
            var choiceIndex = EditorGUILayout.Popup("Pass Index", m_AffectedFeature.PassIndex, selectablePasses.ToArray());

            m_PassIndexToUse = choiceIndex;

        }

        private List<string> GetPassIndexStringEntries(UniversalBlurFeature component)
        {
            List<string> passIndexEntries = new List<string>();
            for (int i = 0; i < component.PassMaterial.passCount; ++i)
            {
                // "Name of a pass (index)" - "PassAlpha (1)"
                string entry = $"{component.PassMaterial.GetPassName(i)} ({i})";
                passIndexEntries.Add(entry);
            }

            return passIndexEntries;
        }
    }

}