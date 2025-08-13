using UnityEngine;
using UnityEditor;

namespace ZG
{
    [CustomPropertyDrawer(typeof(TagAttribute))]
    public class TagDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.String)
                property.stringValue = EditorGUI.TagField(position, property.displayName, property.stringValue);
            else
                EditorGUI.LabelField(position, "Need String.");
        }
    }
}