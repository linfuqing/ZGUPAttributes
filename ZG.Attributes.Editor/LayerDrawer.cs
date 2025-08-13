using UnityEngine;
using UnityEditor;

namespace ZG
{
    [CustomPropertyDrawer(typeof(LayerAttribute))]
    public class LayerDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.Integer)
                property.intValue = EditorGUI.LayerField(position, property.displayName, property.intValue);
            else
                EditorGUI.LabelField(position, "Need Interger.");
        }
    }
}
