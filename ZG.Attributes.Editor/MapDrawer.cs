using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ZG
{
    [CustomPropertyDrawer(typeof(MapAttribute))]
    public class DictionaryDrawer : PropertyDrawer
    {
        private struct Property
        {
            public int index;
            public SerializedProperty keys;
            public SerializedProperty value;

            public Property(SerializedProperty keys)
            {
                index = -1;
                this.keys = keys;
                value = null;
            }
        }

        private string[] __names;
        private string[] __buffer;

        private static string __path;

        public static GUIContent keyContent = new GUIContent("Key");
        public static GUIContent valueContent = new GUIContent("Value");

        private static void __DuplicateMapElement(object data)
        {
            Property property = (Property)data;
            if (property.value != null)
                property.value.DuplicateCommand();

            if (property.keys != null)
            {
                property.keys.InsertArrayElementAtIndex(property.index);

                SerializedObject serializedObject = property.keys.serializedObject;
                if (serializedObject != null)
                    serializedObject.ApplyModifiedProperties();
            }
        }

        private static void __DeleteMapElement(object data)
        {
            Property property = (Property)data;
            if (property.value != null)
                property.value.DeleteCommand();

            if (property.keys != null)
            {
                property.keys.DeleteArrayElementAtIndex(property.index);

                SerializedObject serializedObject = property.keys.serializedObject;
                if (serializedObject != null)
                    serializedObject.ApplyModifiedProperties();
            }
        }

        public static float GetHeight(SerializedProperty property, GUIContent label)
        {
            if (property == null)
                return 0.0f;

            float singleLineHeight = EditorGUIUtility.singleLineHeight, height = singleLineHeight;
            if (property.isExpanded)
            {
                SerializedProperty keysProperty = property.FindPropertyRelative("_keys"), valuesProperty = property.FindPropertyRelative("_values");
                if (keysProperty != null && valuesProperty != null)
                {
                    height += singleLineHeight;

                    SerializedProperty temp;//, end;
                    int size = Mathf.Min(keysProperty.arraySize, valuesProperty.arraySize);
                    for (int i = 0; i < size; ++i)
                    {
                        temp = keysProperty.GetArrayElementAtIndex(i);
                        height += singleLineHeight;
                        if (temp.isExpanded)
                        {
                            /*if (temp.hasVisibleChildren)
                            {
                                end = temp.GetEndProperty();
                                temp.NextVisible(true);
                                while (!SerializedProperty.EqualContents(temp, end))
                                {
                                    height += EditorGUI.GetPropertyHeight(temp, null, true);
                                    if (!temp.NextVisible(false))
                                        break;
                                }
                            }
                            else*/
                            height += EditorGUI.GetPropertyHeight(temp, keyContent);

                            temp = valuesProperty.GetArrayElementAtIndex(i);
                            /*if (temp.hasVisibleChildren)
                            {
                                end = temp.GetEndProperty();
                                temp.NextVisible(true);
                                while (!SerializedProperty.EqualContents(temp, end))
                                {
                                    height += EditorGUI.GetPropertyHeight(temp, null, true);
                                    if (!temp.NextVisible(false))
                                        break;
                                }
                            }
                            else*/
                            height += EditorGUI.GetPropertyHeight(temp, valueContent);
                        }
                    }
                }
            }

            return height + singleLineHeight * 2.0f;
        }

        public static void Draw(
            Rect position,
            SerializedProperty serializedProperty,
            GUIContent label,
            MapAttribute mapAttribute,
            FieldInfo fieldInfo,
            ref string[] names,
            ref string[] buffer)
        {
            if (serializedProperty == null)
                return;

            float singleLineHeight = EditorGUIUtility.singleLineHeight;

            SerializedProperty keysProperty = serializedProperty.FindPropertyRelative("_keys");
            if (keysProperty == null)
                return;

            SerializedProperty valuesProperty = serializedProperty.FindPropertyRelative("_values");
            if (valuesProperty == null)
                return;

            if (names == null)
            {
                IEnumerable<string> temp = mapAttribute == null ? null : keysProperty.GetNames(mapAttribute.nameKey, mapAttribute.path, mapAttribute.pathLevel);

                names = temp == null ? null : new List<string>(temp).ToArray();
            }

            position.height = EditorGUIUtility.singleLineHeight;
            if (serializedProperty.isExpanded = EditorGUI.Foldout(position, serializedProperty.isExpanded, label))
            {
                bool isDirty = false;
                int size = Mathf.Min(keysProperty.arraySize, valuesProperty.arraySize);
                position.y += position.height;
                ++EditorGUI.indentLevel;
                EditorGUI.BeginChangeCheck();
                size = EditorGUI.DelayedIntField(position, new GUIContent("Size"), size);
                if (EditorGUI.EndChangeCheck())
                {
                    keysProperty.arraySize = size;
                    valuesProperty.arraySize = size;

                    isDirty = true;
                }

                bool isBuffer = buffer == null || buffer.Length < size, isContains;
                float labelWidth = EditorGUIUtility.labelWidth, width, height;
                Property property = new Property(keysProperty);
                Event currentEvent = Event.current;
                GenericMenu genericMenu = null;
                SerializedProperty temp;//, end;

                if (isBuffer)
                    buffer = new string[size];

                for (int i = 0; i < size; ++i)
                {
                    temp = keysProperty.GetArrayElementAtIndex(i);
                    if (temp == null)
                        continue;

                    property.value = valuesProperty.GetArrayElementAtIndex(i);
                    property.index = i;

                    height = singleLineHeight;

                    position.y += position.height;
                    position.height = singleLineHeight;
                    if (currentEvent.type == EventType.ContextClick && position.Contains(currentEvent.mousePosition))
                    {
                        genericMenu = new GenericMenu();

                        currentEvent.Use();
                    }

                    if (isBuffer)
                        buffer[i] = temp.GetName(names);

                    if (temp.isExpanded = EditorGUI.Foldout(
                        position,
                        temp.isExpanded,
                        buffer[i]))
                    {
                        ++EditorGUI.indentLevel;
                        /*if (temp.hasVisibleChildren)
                        {
                            end = temp.GetEndProperty();
                            temp.NextVisible(true);
                            while (!SerializedProperty.EqualContents(temp, end))
                            {
                                position.y += position.height;
                                position.height = EditorGUI.GetPropertyHeight(temp, null, true);

                                width = position.width;
                                position.width = labelWidth;

                                isContains = position.Contains(currentEvent.mousePosition);

                                position.width = width;

                                EditorGUI.PropertyField(position, temp, true);

                                if (isContains && currentEvent.type == EventType.ContextClick)
                                {
                                    genericMenu = new GenericMenu();

                                    currentEvent.Use();
                                }

                                height += position.height;

                                if (!temp.NextVisible(false))
                                    break;
                            }
                        }
                        else*/
                        {
                            position.y += position.height;

                            position.height = EditorGUI.GetPropertyHeight(temp, keyContent);

                            width = position.width;
                            position.width = labelWidth;

                            isContains = position.Contains(currentEvent.mousePosition);

                            position.width = width;

                            if (mapAttribute == null || string.IsNullOrEmpty(mapAttribute.path))
                            {
                                EditorGUI.BeginChangeCheck();
                                EditorGUI.PropertyField(position, temp, keyContent, true);
                                isDirty |= EditorGUI.EndChangeCheck();
                            }
                            else
                                isDirty |= IndexDrawer.Draw(
                                    position,
                                    temp,
                                    keyContent,
                                    mapAttribute.path,
                                    //mapAttribute.relativePropertyPath,
                                    mapAttribute.emptyName,
                                    mapAttribute.emptyValue,
                                    mapAttribute.nameKey,
                                    mapAttribute.pathLevel,
                                    mapAttribute.uniqueLevel,
                                    fieldInfo);

                            if (isContains && currentEvent.type == EventType.ContextClick)
                            {
                                genericMenu = new GenericMenu();

                                currentEvent.Use();
                            }

                            height += position.height;
                        }

                        if (property.value != null)
                        {
                            /*if (property.value.hasVisibleChildren)
                            {
                                temp = property.value.Copy();
                                end = temp.GetEndProperty();
                                temp.NextVisible(true);
                                while (!SerializedProperty.EqualContents(temp, end))
                                {
                                    position.y += position.height;
                                    position.height = EditorGUI.GetPropertyHeight(temp, null, true);

                                    width = position.width;
                                    position.width = labelWidth;

                                    isContains = position.Contains(currentEvent.mousePosition);

                                    position.width = width;

                                    EditorGUI.PropertyField(position, temp, true);

                                    if (isContains && currentEvent.type == EventType.ContextClick)
                                    {
                                        genericMenu = new GenericMenu();

                                        currentEvent.Use();
                                    }

                                    height += position.height;

                                    if (!temp.NextVisible(false))
                                        break;
                                }
                            }
                            else*/
                            {
                                position.y += position.height;

                                position.height = EditorGUI.GetPropertyHeight(property.value, valueContent);

                                width = position.width;
                                position.width = labelWidth;

                                isContains = position.Contains(currentEvent.mousePosition);

                                position.width = width;

                                EditorGUI.BeginChangeCheck();
                                EditorGUI.PropertyField(position, property.value, valueContent, true);
                                isDirty |= EditorGUI.EndChangeCheck();

                                if (isContains && currentEvent.type == EventType.ContextClick)
                                {
                                    genericMenu = new GenericMenu();

                                    currentEvent.Use();
                                }

                                height += position.height;
                            }
                        }

                        --EditorGUI.indentLevel;
                    }

                    if (genericMenu != null)
                    {
                        genericMenu.AddItem(new GUIContent("Duplicate Dictionary Element"), false, __DuplicateMapElement, property);
                        genericMenu.AddItem(new GUIContent("Delete Dictionary Element"), false, __DeleteMapElement, property);
                        genericMenu.ShowAsContext();

                        genericMenu = null;
                    }
                }

                if (isDirty)
                {
                    keysProperty.serializedObject.ApplyModifiedProperties();
                    valuesProperty.serializedObject.ApplyModifiedProperties();
                }

                --EditorGUI.indentLevel;
            }

            position.y += position.height;
            position.height = singleLineHeight;
            if (GUI.Button(position, "Load"))
            {
                int keyGuidIndex, keyNameIndex, valueGuidIndex, valueNameIndex;
                if (mapAttribute == null)
                {
                    keyGuidIndex = 0;
                    keyNameIndex = 0;
                    valueGuidIndex = 0;
                    valueNameIndex = 0;
                }
                else
                {
                    keyGuidIndex = mapAttribute.keyGuidIndex;
                    keyNameIndex = mapAttribute.keyNameIndex;
                    valueGuidIndex = mapAttribute.valueGuidIndex;
                    valueNameIndex = mapAttribute.valueNameIndex;
                }

                string propertyPath = EditorHelper.GetPropertyPath(serializedProperty.propertyPath);

                string[] lines = CSVDrawer.Load(ref __path);

                AssetDatabase.StartAssetEditing();

                CSVDrawer.Load(
                    keysProperty,
                    keyGuidIndex,
                    keyNameIndex,
                    CSVDrawer.GetGuids(keysProperty, mapAttribute.path, mapAttribute.pathLevel),
                    names,
                    lines,
                    (target, indices, count) =>
                    {
                        if (indices == null)
                            return;

                        string path = propertyPath + "._values";
                        Array source = target.Get(ref path, out fieldInfo, out object parent) as Array;
                        if (fieldInfo == null)
                            return;

                        Type type = source == null ? null : source.GetType();
                        type = type == null ? null : type.GetElementType();
                        Array destination = type == null ? null : Array.CreateInstance(type, count);
                        if (destination == null)
                            return;

                        foreach (KeyValuePair<int, int> pair in indices)
                            ((IList)destination)[pair.Value] = ((IList)source)[pair.Key];

                        fieldInfo.SetValue(parent, destination);
                    });

                CSVDrawer.Load(valuesProperty, valueGuidIndex, valueNameIndex, /*names*/null, null, lines, null);

                SerializedObject serializedObject = serializedProperty.serializedObject;
                UnityEngine.Object[] targets = serializedObject == null ? null : serializedObject.targetObjects;
                if (targets != null)
                {
                    ISerializationCallbackReceiver receiver;
                    foreach (UnityEngine.Object target in targets)
                    {
                        receiver = target.Get(propertyPath) as ISerializationCallbackReceiver;
                        if (receiver != null)
                            receiver.OnAfterDeserialize();
                    }
                }

                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
            }

            position.y += singleLineHeight;
            position.height = singleLineHeight;
            if (GUI.Button(position, "Save"))
            {
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return GetHeight(property, label);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property == null)
                return;

            //label = EditorGUI.BeginProperty(position, label, property);
            Draw(position, property, label, attribute as MapAttribute, fieldInfo, ref __names, ref __buffer);
            //EditorGUI.EndProperty();
        }
    }
}