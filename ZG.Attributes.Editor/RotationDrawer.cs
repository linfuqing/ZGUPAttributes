using UnityEngine;
using UnityEditor;

namespace ZG
{
    [CustomPropertyDrawer(typeof(RotationAttribute))]
    public class RotationDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            float singleLineHeight = EditorGUIUtility.singleLineHeight;
            position.height = singleLineHeight;

            bool isExpanded = EditorGUI.Foldout(position, property.isExpanded, property.displayName);
            property.isExpanded = isExpanded;
            if (isExpanded)
            {
                ++EditorGUI.indentLevel;

                position.y += singleLineHeight;

                bool isEmpty = true, isDirty = false;

                var type = ((RotationAttribute)attribute).type;

                switch (type)
                {
                    case RotationType.Normal:
                        isDirty = EditorGUI.PropertyField(position, property, true);

                        if (property.propertyType == SerializedPropertyType.Quaternion)
                            isEmpty = default == property.quaternionValue;
                        break;
                    case RotationType.Direction:
                        if (property.propertyType == SerializedPropertyType.Vector3)
                        {
                            var value = property.vector3Value;
                            isEmpty = Vector3.zero == value;
                            value = isEmpty ? Vector3.zero : Quaternion.FromToRotation(Vector3.forward, value).eulerAngles;

                            EditorGUI.BeginChangeCheck();
                            var rotation = EditorGUI.Vector3Field(position, property.displayName, value);
                            isDirty = EditorGUI.EndChangeCheck();

                            if (isDirty)
                                property.vector3Value = Quaternion.Euler(rotation) * Vector3.forward;
                        }
                        else
                        {
                            EditorGUI.HelpBox(position, "Need Vector3.", MessageType.Error);

                            return;
                        }
                        break;
                    default:
                        break;
                }


                position.y += singleLineHeight;

                if (isEmpty && !isDirty)
                    GUI.Box(position, "Empty");
                else
                {
                    if (GUI.Button(position, "Clear"))
                    {
                        switch (type)
                        {
                            case RotationType.Normal:

                                property.quaternionValue = default;

                                break;

                            case RotationType.Direction:

                                property.vector3Value = Vector3.zero;
                                break;
                        }
                    }
                }

                --EditorGUI.indentLevel;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return property.isExpanded ? EditorGUIUtility.singleLineHeight * 3.0f : EditorGUIUtility.singleLineHeight;
        }
    }
}