#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Forces Men-at-Arms geography bonuses to reference Terrain Catalog entries via dropdown (subtypeId).
    /// This works even when the owning inspector uses DrawDefaultInspector().
    /// </summary>
    [CustomPropertyDrawer(typeof(GeographyBonus))]
    public sealed class GeographyBonusDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property == null)
            {
                EditorGUI.LabelField(position, label, new GUIContent("null"));
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty subtypeProp = property.FindPropertyRelative("subtypeId");
            SerializedProperty bonusProp = property.FindPropertyRelative("bonus");

            Rect line = position;
            line.height = EditorGUIUtility.singleLineHeight;

            // Prefix label
            line = EditorGUI.PrefixLabel(line, label);

            float spacing = 6f;
            float bonusWidth = Mathf.Min(80f, line.width * 0.25f);
            Rect subtypeRect = new Rect(line.x, line.y, Mathf.Max(0, line.width - bonusWidth - spacing), line.height);
            Rect bonusRect = new Rect(subtypeRect.xMax + spacing, line.y, Mathf.Max(0, bonusWidth), line.height);

            // subtypeId dropdown (Terrain Catalog)
            if (subtypeProp != null && subtypeProp.propertyType == SerializedPropertyType.String)
            {
                var terrains = WorldDataChoicesCache.GetTerrainDefinitions();
                if (terrains != null && terrains.Count > 0)
                {
                    string current = subtypeProp.stringValue ?? string.Empty;

                    string[] ids = new string[terrains.Count + 1];
                    string[] names = new string[terrains.Count + 1];
                    ids[0] = string.Empty;
                    names[0] = "(none)";

                    for (int i = 0; i < terrains.Count; i++)
                    {
                        ids[i + 1] = terrains[i].id ?? string.Empty;
                        names[i + 1] = string.IsNullOrEmpty(terrains[i].displayName) ? ids[i + 1] : terrains[i].displayName;
                    }

                    int curIndex = 0;
                    if (!string.IsNullOrEmpty(current))
                    {
                        for (int i = 1; i < ids.Length; i++)
                        {
                            if (ids[i] == current)
                            {
                                curIndex = i;
                                break;
                            }
                        }
                    }

                    int nextIndex = EditorGUI.Popup(subtypeRect, curIndex, names);
                    if (nextIndex != curIndex && nextIndex >= 0 && nextIndex < ids.Length)
                        subtypeProp.stringValue = ids[nextIndex];
                }
                else
                {
                    subtypeProp.stringValue = EditorGUI.TextField(subtypeRect, subtypeProp.stringValue);
                }
            }
            else if (subtypeProp != null)
            {
                EditorGUI.PropertyField(subtypeRect, subtypeProp, GUIContent.none);
            }

            // bonus field
            if (bonusProp != null)
                EditorGUI.PropertyField(bonusRect, bonusProp, GUIContent.none);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => EditorGUIUtility.singleLineHeight;
    }
}
#endif
