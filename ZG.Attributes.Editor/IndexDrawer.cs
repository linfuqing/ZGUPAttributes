using UnityEditor;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZG
{
    [CustomPropertyDrawer(typeof(IndexAttribute))]
    public class IndexDrawer : PropertyDrawer
    {
        public static bool Draw(
            Rect position,
            SerializedProperty property,
            GUIContent label,
            string path,
            //string relativePropertyPath,
            string emptyName,
            string emptyValue, 
            string nameKey,
            int pathLevel,
            int uniqueLevel,
            System.Reflection.FieldInfo fieldInfo)
        {
            SerializedObject serializedObject = property == null ? null : property.serializedObject;
            if (serializedObject == null)
                return false;

            string targetPath = EditorHelper.GetPropertyPath(property.propertyPath), propertyPath = EditorNameHelper.GetPath(pathLevel, path, targetPath);
            if (propertyPath == null)
            {
                if (position.Contains(Event.current.mousePosition))
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUI.PropertyField(position, property);
                    return EditorGUI.EndChangeCheck();
                }

                EditorHelper.HelpBox(position, label, "Error Path", MessageType.Error);

                return false;
            }

            int index, nameIndex;
            string temp = propertyPath;
            SerializedPropertyType type = property.propertyType;
            object targetObject = serializedObject.targetObject;
            IEnumerable<string> names = targetObject.Get(ref temp).GetNames(nameKey, type == SerializedPropertyType.Integer);
            Dictionary<string, int> source = null, destination = null;
            if (names == null)
            {
                if (position.Contains(Event.current.mousePosition))
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUI.PropertyField(position, property);
                    return EditorGUI.EndChangeCheck();
                }
                else
                {
                    temp = propertyPath.Remove(propertyPath.IndexOf(temp));
                    if (string.IsNullOrEmpty(temp))
                        EditorHelper.HelpBox(position, label, targetObject.GetName() + "." + propertyPath + " is not a IList", MessageType.Error);
                    else
                        EditorHelper.HelpBox(position, label, targetObject.GetName() + "." + temp + " is null", MessageType.Warning);
                }

                return false;
            }
            else
            {
                index = 0;
                foreach (string targetName in names)
                {
                    if (source == null)
                        source = new Dictionary<string, int>();

                    try
                    {
                        source.Add(targetName, index);
                    }
                    catch
                    {
                        if (type == SerializedPropertyType.String)
                        {
                            if (position.Contains(Event.current.mousePosition))
                            {
                                EditorGUI.BeginChangeCheck();
                                EditorGUI.PropertyField(position, property);
                                return EditorGUI.EndChangeCheck();
                            }

                            EditorHelper.HelpBox(position, label, targetObject.GetName() + "." + propertyPath + " is have the same name", MessageType.Error);

                            return false;
                        }
                        else
                            source.Add(NameHelper.MakeUnique(targetName, source.Keys), index);
                    }

                    ++index;
                }
            }

            UnityEngine.Object[] targets = serializedObject == null ? null : serializedObject.targetObjects;
            if (targets != null)
            {
                foreach (UnityEngine.Object target in targets)
                {
                    if (target == (UnityEngine.Object)targetObject)
                        continue;

                    temp = propertyPath;
                    names = target.Get(ref temp).GetNames(nameKey, type == SerializedPropertyType.Integer);
                    if (names == null)
                        source = null;
                    else if (source != null)
                    {
                        index = 0;
                        foreach (string targetName in names)
                        {
                            if (source.TryGetValue(targetName, out nameIndex) && (type != SerializedPropertyType.Integer || nameIndex == index))
                            {
                                if (destination == null)
                                    destination = new Dictionary<string, int>();

                                try
                                {
                                    destination.Add(targetName, index);
                                }
                                catch
                                {
                                    if (type == SerializedPropertyType.String)
                                    {
                                        if (position.Contains(Event.current.mousePosition))
                                        {
                                            EditorGUI.BeginChangeCheck();
                                            EditorGUI.PropertyField(position, property);
                                            return EditorGUI.EndChangeCheck();
                                        }

                                        EditorHelper.HelpBox(position, label, target.name + "." + propertyPath + " is have same name", MessageType.Error);

                                        return false;
                                    }
                                    else
                                        destination.Add(NameHelper.MakeUnique(targetName, destination.Keys), index);
                                }
                            }

                            ++index;
                        }

                        source = destination;
                        destination = null;
                    }

                    if (source == null)
                        break;
                }
            }

            bool isDirty = false;
            if (source == null || source.Count < 1)
            {
                if (position.Contains(Event.current.mousePosition))
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUI.PropertyField(position, property);
                    isDirty = EditorGUI.EndChangeCheck();
                }
                else
                    EditorHelper.HelpBox(position, label, "No Element", MessageType.Warning);
            }
            else
            {
                bool isShowMixedValue = EditorGUI.showMixedValue;
                EditorGUI.showMixedValue = property.hasMultipleDifferentValues;

                if (type == SerializedPropertyType.Integer)
                {
                    ICollection<string> keyCollection = source.Keys;
                    if (keyCollection == null)
                    {
                        if (position.Contains(Event.current.mousePosition))
                            return EditorGUI.PropertyField(position, property);

                        EditorHelper.HelpBox(position, label, "No keys", MessageType.Error);

                        return false;
                    }

                    bool isEmptyName = !string.IsNullOrEmpty(emptyName);
                    int count = keyCollection.Count;
                    string[] options;
                    if (uniqueLevel > 0)
                    {
                        options = new string[count];
                        keyCollection.CopyTo(options, 0);

                        bool isRemove = false;
                        foreach (SerializedProperty sibling in property.GetSiblings(uniqueLevel))
                        {
                            if (sibling != null)
                            {
                                index = sibling.intValue;

                                if (index >= 0 && index < count)
                                    isRemove = source.Remove(options[index]) || isRemove;
                            }
                        }

                        if (isRemove)
                        {
                            keyCollection = source.Keys;
                            count = keyCollection == null ? 0 : keyCollection.Count;

                            options = null;
                        }
                        else if (isEmptyName)
                            options = null;
                    }
                    else
                        options = null;

                    index = -1;
                    Dictionary<string, int>.ValueCollection values = source.Values;
                    if (values != null)
                    {
                        int result = property.intValue;
                        foreach (int value in values)
                        {
                            if (value == result)
                                break;

                            ++index;
                        }

                        if (index < count - 1)
                            ++index;
                        else
                            index = -1;
                    }

                    if (options == null)
                    {
                        if (isEmptyName)
                        {
                            options = new string[++count];
                            options[0] = emptyName;
                            keyCollection.CopyTo(options, 1);

                            ++index;
                        }
                        else
                        {
                            options = new string[count];
                            keyCollection.CopyTo(options, 0);
                        }
                    }

                    EditorGUI.BeginChangeCheck();
                    index = EditorGUI.Popup(
                        position,
                        label == null ? null : label.text,
                        index,
                        options);

                    if (EditorGUI.EndChangeCheck())
                    {
                        string name = index >= 0 && index < count ? options[index] : string.Empty;
                        index = !isEmptyName || index > 0 ? source[name] : -1;
                        property.intValue = index;

                        isDirty = true;
                        /*property = property.GetParent();
                        property = property == null ? null : property.FindPropertyRelative(relativePropertyPath);
                        if (property != null)
                        {
                            switch (property.propertyType)
                            {
                                case SerializedPropertyType.Integer:
                                    property.intValue = index;

                                    isDirty = true;
                                    break;
                                case SerializedPropertyType.String:
                                    property.stringValue = name;

                                    isDirty = true;
                                    break;
                            }
                        }*/
                    }
                    else if (fieldInfo != null)
                    {
                        Event current = Event.current;
                        if (current != null && current.type == EventType.ContextClick && position.Contains(current.mousePosition))
                        {
                            GenericMenu genericMenu = new GenericMenu();
                            genericMenu.AddItem(new GUIContent("Reset"), false, delegate ()
                            {
                                int numOptions = Mathf.Min(options == null ? 0 : options.Length);//, nameLength, count, i;
                                Type targetType;
                                object instance;
                                //string name;
                                foreach (UnityEngine.Object target in targets)
                                {
                                    temp = targetPath;
                                    instance = target.Get(ref temp);
                                    targetType = instance == null ? null : instance.GetType();
                                    if (targetType != null)
                                    {
                                        index = target.name.Approximately(options);
                                        if (index >= 0 && index < numOptions)
                                        {
                                            fieldInfo.SetValue(target, Convert.ChangeType(isEmptyName ? index - 1 : index, targetType));

                                            EditorUtility.SetDirty(target);
                                        }

                                        /*name = name == null ? null : Regex.Replace(PinYinConverter.Get(name).ToLower(), @"[\s|_\d]+", string.Empty);
                                        if (name != null)
                                        {
                                            nameLength = name.Length;

                                            length = 0;
                                            for (i = 0; i < numOptions; ++i)
                                            {
                                                option = options[i];
                                                option = option == null ? null : Regex.Replace(PinYinConverter.Get(options[i]).ToLower(), @"[\s|_\d]+", string.Empty);
                                                if (option != null)
                                                {
                                                    count = Mathf.Max(option.Contains(name) ? nameLength : 0, name.Contains(option) ? option.Length : 0);
                                                    if (count > length)
                                                    {
                                                        length = count;

                                                        index = values[i];
                                                    }
                                                }
                                            }
                                            
                                            if (length > 0)
                                            {
                                                if (targetType == typeof(int))
                                                    fieldInfo.SetValue(target, index);
                                                else if (targetType == typeof(short))
                                                    fieldInfo.SetValue(target, (short)index);
                                                else if (targetType == typeof(byte))
                                                    fieldInfo.SetValue(target, (byte)index);
                                                else if (targetType == typeof(sbyte))
                                                    fieldInfo.SetValue(target, (sbyte)index);
                                                else if (targetType == typeof(ushort))
                                                    fieldInfo.SetValue(target, (ushort)index);
                                                else if (targetType == typeof(uint))
                                                    fieldInfo.SetValue(target, (uint)index);
                                                else if (targetType == typeof(ulong))
                                                    fieldInfo.SetValue(target, (ulong)index);
                                                else if (targetType == typeof(long))
                                                    fieldInfo.SetValue(target, (long)index);
                                                break;
                                            }
                                        }*/
                                    }
                                }
                            });

                            genericMenu.ShowAsContext();

                            current.Use();
                        }
                    }
                }
                else if (type == SerializedPropertyType.String)
                {
                    if (uniqueLevel > 0)
                    {
                        foreach (SerializedProperty sibling in property.GetSiblings(uniqueLevel))
                        {
                            if (sibling != null)
                                source.Remove(sibling.stringValue);
                        }
                    }

                    bool isEmptyName = !string.IsNullOrEmpty(emptyName);
                    string[] options;
                    ICollection<string> keyCollection = source == null ? null : source.Keys;
                    if (isEmptyName)
                    {
                        int count = keyCollection == null ? 0 : keyCollection.Count;
                        options = new string[count + 1];
                        options[0] = emptyName;

                        if (count > 0)
                            keyCollection.CopyTo(options, 1);
                    }
                    else if (keyCollection == null)
                        options = null;
                    else
                    {
                        options = new string[keyCollection.Count];
                        keyCollection.CopyTo(options, 0);
                    }

                    index = keyCollection.IndexOf(property.stringValue);
                    EditorGUI.BeginChangeCheck();
                    index = EditorGUI.Popup(position, label == null ? null : label.text, (isEmptyName ? 1 : 0) + index, options);
                    if (EditorGUI.EndChangeCheck() && index >= 0 && index < options.Length)
                    {
                        string name = options[index];
                        bool isEmptyValue = isEmptyName && index < 1;
                        index = isEmptyValue ? -1 : source[name];
                        property.stringValue = isEmptyValue ? emptyValue : name;

                        isDirty = true;
                        /*property = property.GetParent();
                        property = property == null ? null : property.FindPropertyRelative(relativePropertyPath);
                        if (property != null)
                        {
                            switch (property.propertyType)
                            {
                                case SerializedPropertyType.Integer:
                                    property.intValue = index;

                                    isDirty = true;
                                    break;
                                case SerializedPropertyType.String:
                                    property.stringValue = name;

                                    isDirty = true;
                                    break;
                            }
                        }*/
                    }
                }
                else if (position.Contains(Event.current.mousePosition))
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUI.PropertyField(position, property);
                    isDirty = EditorGUI.EndChangeCheck();
                }
                else
                    EditorHelper.HelpBox(position, label, "Error type(Must be int Or string)", MessageType.Error);

                EditorGUI.showMixedValue = isShowMixedValue;
            }

            return isDirty;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            IndexAttribute attribute = this.attribute as IndexAttribute;
            label = EditorGUI.BeginProperty(position, label, property);
            if (attribute == null)
                Draw(
                    position, 
                    property, 
                    label, 
                    null, 
                    null, 
                    null, 
                    null, 
                    0, 
                    0, 
                    fieldInfo);
            else
                Draw(
                    position, 
                    property, 
                    label, 
                    attribute.path, 
                    //attribute.relativePropertyPath, 
                    attribute.emptyName,
                    attribute.emptyValue,
                    attribute.nameKey, 
                    attribute.pathLevel, 
                    attribute.uniqueLevel, 
                    fieldInfo);
            EditorGUI.EndProperty();
        }
    }
}
