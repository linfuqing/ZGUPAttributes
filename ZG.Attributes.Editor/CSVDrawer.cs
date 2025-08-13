using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEditor;

namespace ZG
{
    [CustomPropertyDrawer(typeof(CSVAttribute))]
    public class CSVDrawer : PropertyDrawer
    {
        private struct Instance
        {
            public GameObject root;
            public HashSet<GameObject> parents;
        }

        public static GameObject FindCorrespondingObjectFromInstanceRoot(GameObject gameObject, GameObject value)
        {
            if (PrefabUtility.GetCorrespondingObjectFromSource(gameObject) == value)
                return gameObject;

            GameObject result;
            foreach (Transform child in gameObject.transform)
            {
                result = FindCorrespondingObjectFromInstanceRoot(child.gameObject, value);
                if (result != null)
                    return result;
            }

            return null;
        }

        public static T FindCorrespondingObjectFromInstanceRoot<T>(GameObject gameObject, T value) where T : UnityEngine.Object
        {
            T result = null;
            Type type = value.GetType();
            if (type == typeof(GameObject))
                result = (T)(UnityEngine.Object)FindCorrespondingObjectFromInstanceRoot(gameObject, (GameObject)(UnityEngine.Object)value);
            else if (type.IsSubclassOf(typeof(Component)))
            {
                T[] components = gameObject.GetComponentsInChildren<T>(true);
                foreach (T component in components)
                {
                    if (PrefabUtility.GetCorrespondingObjectFromSource(component) == value)
                    {
                        result = component;

                        break;
                    }
                }
            }
            else
            {
                Component[] components = gameObject.GetComponentsInChildren<Component>(true);
                foreach (Component component in components)
                {
                    if (PrefabUtility.GetCorrespondingObjectFromSource(component) == value)
                    {
                        result = (T)(UnityEngine.Object)component;

                        break;
                    }
                }
            }

            if (result == null)
                Debug.LogError(gameObject.name + " FindCorrespondingObject " + value.name + " Fail.");

            return result;
        }

        public static IEnumerable<Guid> GetGuids(
            SerializedProperty property,
            string path,
            int pathLevel)
        {
            if (property == null)
                return null;

            SerializedObject serializedObject = property.serializedObject;
            UnityEngine.Object targetObject = serializedObject.targetObject;
            if (targetObject == null)
                return null;

            string targetPath = EditorHelper.GetPropertyPath(property.propertyPath), propertyPath = targetPath;
            if (propertyPath == null)
                return null;

            int length = propertyPath.Length, index = length;
            for (int i = 0; i <= pathLevel; ++i)
            {
                index = propertyPath.LastIndexOf('.', index - 1);
                if (index == -1)
                    break;
            }

            propertyPath = propertyPath.Remove(index + 1);
            propertyPath += path;

            string temp = propertyPath;

            return (targetObject.Get(ref temp) as IEnumerable).GetGuids();
        }

        public static string[] Load(ref string path)
        {
            path = EditorUtility.OpenFilePanelWithFilters("CSV File For Load", path, new string[] { "CSV File", "csv" });
            if (!File.Exists(path))
                return null;

            return File.ReadAllLines(path);
        }

        public static bool Load(
            GameObject gameObject,
            string line,
            string[] fields,
            IEnumerable<string> names)
        {
            if (string.IsNullOrEmpty(line))
                return false;

            Component[] components = gameObject == null ? null : gameObject.GetComponents<Component>();
            if (components == null)
                return false;

            int index, i, j;
            Type componmentType;
            MethodInfo methodInfo;
            FieldInfo[] fieldInfos;
            PropertyInfo[] propertyInfos;
            object[] parameters = null;
            foreach (Component instance in components)
            {
                componmentType = instance == null ? null : instance.GetType();
                if (componmentType == null)
                    continue;

                fieldInfos = componmentType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetField);
                if (fieldInfos != null)
                {
                    foreach (FieldInfo child in fieldInfos)
                    {
                        if (child != null && child.IsDefined(typeof(CSVFieldAttribute), true))
                        {
                            index = Array.IndexOf(fields, child.Name);
                            if (index != -1)
                            {
                                i = 0;
                                for (j = 0; j < index; ++j)
                                {
                                    i = line.IndexOf(',', i);
                                    if (i == -1)
                                        break;

                                    ++i;
                                }

                                if (i != -1)
                                {
                                    index = line.IndexOf(',', i);
                                    if (index == -1)
                                        index = line.Length;

                                    try
                                    {
                                        child.SetValue(instance, line.Substring(i, index - i).As(child.FieldType, names));
                                    }
                                    catch (Exception e)
                                    {
                                        if (e != null)
                                            Debug.LogException(e.InnerException ?? e);
                                    }
                                }
                            }
                        }
                    }
                }

                propertyInfos = componmentType.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty);
                if (propertyInfos != null)
                {
                    foreach (PropertyInfo child in propertyInfos)
                    {
                        if (child != null && child.IsDefined(typeof(CSVFieldAttribute), true))
                        {
                            index = Array.IndexOf(fields, child.Name);
                            if (index != -1)
                            {
                                i = 0;
                                for (j = 0; j < index; ++j)
                                {
                                    i = line.IndexOf(',', i);
                                    if (i == -1)
                                        break;

                                    ++i;
                                }

                                if (i != -1)
                                {
                                    index = line.IndexOf(',', i);
                                    if (index == -1)
                                        index = line.Length;

                                    methodInfo = child.GetSetMethod();
                                    if (methodInfo != null)
                                    {
                                        if (parameters == null || parameters.Length < 1)
                                            parameters = new object[1];

                                        try
                                        {
                                            parameters[0] = line.Substring(i, index - i).As(child.PropertyType, names);

                                            methodInfo.Invoke(instance, parameters);
                                        }
                                        catch (Exception e)
                                        {
                                            if (e != null)
                                                Debug.LogException(e.InnerException ?? e);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return true;
        }

        public static string[] Load(
            SerializedProperty property,
            int guidIndex,
            int nameIndex,
            IEnumerable<Guid> guids,
            IEnumerable<string> names,
            string[] lines,
            Action<UnityEngine.Object, Dictionary<int, int>, int> handler)
        {
            if (lines == null || property == null || !property.isArray)
                return null;

            SerializedObject serializedObject = property == null ? null : property.serializedObject;
            UnityEngine.Object[] targets = serializedObject == null ? null : serializedObject.targetObjects;
            if (targets == null || targets.Length < 1)
            {
                AssetDatabase.StopAssetEditing();

                return null;
            }

            string propertyPath = EditorHelper.GetPropertyPath(property.propertyPath), path;

            bool isGameObjectOrComponent, isAttribute;
            int i, j, k, numLines, count, index;
            Guid guid;
            string name, line;
            object parent, temp;
            UnityEngine.Object instance, prefab;
            GameObject gameObject;
            Type type, tempType;
            IList source, destination;
            CSVFieldAttribute fieldAttribute;
            CSVMethodAttribute methodAttribute;
            MethodInfo methodInfo;
            FieldInfo fieldInfo;
            Component component;
            Component[] components;
            ParameterInfo parameterInfo;
            ParameterInfo[] parameterInfos;
            MethodInfo[] methodInfos;
            FieldInfo[] fieldInfos;
            PropertyInfo[] propertyInfos;
            object[] attributes;
            object[] parameters = null;
            string[] sources = lines, fields;
            List<string> result = null;
            Stack<(object, FieldInfo)> objectFieldInfos = null;
            Dictionary<int, int> indices = null;
            Dictionary<GameObject, Instance> instances = null;
            Dictionary<UnityEngine.Object, UnityEngine.Object> prefabs = null;
            Action<object, FieldInfo> objectFieldInfoWrapper = null;
            foreach (UnityEngine.Object target in targets)
            {
                type = target == null ? null : target.GetType();
                if (type == null)
                    continue;

                CSVShared.target = target;

                lines = sources;
                methodInfos = type == null ? null : type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                if (methodInfos != null)
                {
                    foreach (MethodInfo method in methodInfos)
                    {
                        isAttribute = false;
                        attributes = method == null ? null : method.GetCustomAttributes(typeof(CSVMethodAttribute), true);
                        if (attributes != null)
                        {
                            foreach (Attribute attribute in attributes)
                            {
                                methodAttribute = attribute as CSVMethodAttribute;
                                if (methodAttribute != null && methodAttribute.type == CSVMethodType.Filter && (string.IsNullOrEmpty(methodAttribute.path) || methodAttribute.path == propertyPath))
                                {
                                    isAttribute = true;

                                    break;
                                }
                            }
                        }

                        if (isAttribute && method.ReturnType == typeof(string[]))
                        {
                            parameterInfos = method.GetParameters();
                            if (parameterInfos != null && parameterInfos.Length == 1)
                            {
                                parameterInfo = parameterInfos[0];
                                if (parameterInfo != null && parameterInfo.ParameterType == typeof(string[]))
                                {
                                    if (parameters == null)
                                        parameters = new object[1];

                                    parameters[0] = sources;

                                    try
                                    {
                                        lines = method.Invoke(method.IsStatic ? null : target, parameters) as string[];
                                    }
                                    catch (Exception e)
                                    {
                                        if (e != null)
                                            Debug.LogException(e.InnerException ?? e);
                                    }
                                }
                            }
                        }
                    }
                }

                numLines = lines == null ? 0 : lines.Length;
                if (numLines < 2)
                {
                    Debug.LogError(propertyPath + ": CSV Load Fail By Custom Method.");

                    continue;
                }

                if (guidIndex >= 0)
                {
                    if (result != null)
                        result.Clear();

                    for (i = 1; i < numLines; ++i)
                    {
                        line = lines[i];

                        k = 0;
                        for (j = 0; j < guidIndex; ++j)
                        {
                            k = line.IndexOf(',', k);
                            if (k == -1)
                                break;

                            ++k;
                        }

                        if (k != -1)
                        {
                            index = line.IndexOf(',', k);
                            if (index > k && Guid.TryParse(line.Substring(k, index), out guid))
                            {
                                if (result == null)
                                    result = new List<string>();

                                result.Add(line);
                            }
                        }
                    }

                    numLines = result == null ? 0 : result.Count;
                    if (numLines > 0)
                    {
                        line = lines[0];
                        lines = new string[numLines + 1];
                        lines[0] = line;

                        result.CopyTo(lines, 1);
                    }
                    else
                        lines = null;
                }

                numLines = lines == null ? 0 : lines.Length;
                if (numLines < 2)
                {
                    Debug.LogError(propertyPath + ": CSV Load Fail By Guid Index.");

                    continue;
                }

                if (nameIndex >= 0)
                {
                    if (result != null)
                        result.Clear();

                    for (i = 1; i < numLines; ++i)
                    {
                        line = lines[i];

                        k = 0;
                        for (j = 0; j < nameIndex; ++j)
                        {
                            k = line.IndexOf(',', k);
                            if (k == -1)
                                break;

                            ++k;
                        }

                        if (k != -1 && line.IndexOf(',', k) > k)
                        {
                            if (result == null)
                                result = new List<string>();

                            result.Add(line);
                        }
                    }

                    numLines = result == null ? 0 : result.Count;
                    if (numLines > 0)
                    {
                        line = lines[0];
                        lines = new string[numLines + 1];
                        lines[0] = line;

                        result.CopyTo(lines, 1);
                    }
                    else
                        lines = null;
                }

                numLines = lines == null ? 0 : lines.Length;
                if (numLines < 2)
                {
                    Debug.LogError(propertyPath + ": CSV Load Fail By Name Index.");

                    continue;
                }

                line = lines[0];
                fields = string.IsNullOrEmpty(line) ? null : line.Split(',');
                if (fields == null)
                    continue;

                if (objectFieldInfos == null)
                    objectFieldInfos = new Stack<(object, FieldInfo)>();
                else
                    objectFieldInfos.Clear();

                if (objectFieldInfoWrapper == null)
                    objectFieldInfoWrapper = (target, fieldInfo) =>
                    {
                        objectFieldInfos.Push((target, fieldInfo));
                    };
                
                path = propertyPath;
                source = target.Get(objectFieldInfoWrapper, ref path, out fieldInfo, out parent) as IList;
                if (fieldInfo != null)
                {
                    type = fieldInfo.FieldType;
                    type = type.IsArray ? type.GetElementType() : null;
                    if (type != null)
                    {
                        isGameObjectOrComponent = type == typeof(GameObject) || type.IsSubclassOf(typeof(Component));

                        numLines = lines.Length - 1;

                        destination = Array.CreateInstance(type, numLines);
                        if (source != null)
                        {
                            if (indices != null)
                                indices.Clear();

                            count = source.Count;
                            if (isGameObjectOrComponent)
                            {
                                for (i = 0; i < count; ++i)
                                    source[i] = __GetPrefabInstance(source[i] as UnityEngine.Object, ref instances, ref prefabs);
                            }

                            for (i = 0; i < numLines; ++i)
                            {
                                line = lines[i + 1];

                                if (EditorUtility.DisplayCancelableProgressBar("CSV Loading..", "Reset Objects", i * 1.0f / numLines))
                                {
                                    EditorUtility.ClearProgressBar();

                                    return null;
                                }

                                if (string.IsNullOrEmpty(line))
                                    continue;

                                index = -1;

                                if (guidIndex >= 0)
                                {
                                    k = 0;
                                    for (j = 0; j < guidIndex; ++j)
                                    {
                                        k = line.IndexOf(',', k);
                                        if (k == -1)
                                            break;

                                        ++k;
                                    }

                                    if (k != -1)
                                    {
                                        index = line.IndexOf(',', k);
                                        if (index == -1)
                                            index = line.Length;

                                        name = line.Substring(k, index - k);

                                        if (Guid.TryParse(name, out guid))
                                        {
                                            if (guids != null && type.IsIndex())
                                            {
                                                index = guids.IndexOf(guid);
                                                for (k = 0; k < count; ++k)
                                                {
                                                    if ((int)Convert.ChangeType(source[k], typeof(int)) == index)
                                                        break;
                                                }

                                                index = k < count ? k : -1;
                                            }
                                            else
                                                index = source.GetGuids().IndexOf(guid);
                                        }
                                    }
                                }

                                if (nameIndex >= 0 && (index < 0 || index >= count))
                                {
                                    k = 0;
                                    for (j = 0; j < nameIndex; ++j)
                                    {
                                        k = line.IndexOf(',', k);
                                        if (k == -1)
                                            break;

                                        ++k;
                                    }

                                    if (k != -1)
                                    {
                                        index = line.IndexOf(',', k);
                                        if (index == -1)
                                            index = line.Length;

                                        name = line.Substring(k, index - k);

                                        if (names != null && type.IsIndex())
                                        {
                                            index = names.IndexOf(name);
                                            for (k = 0; k < count; ++k)
                                            {
                                                if ((int)Convert.ChangeType(source[k], typeof(int)) == index)
                                                    break;
                                            }

                                            index = k < count ? k : -1;
                                        }
                                        else
                                            //Force Use IndexOf Instead By IList.IndexOf
                                            index = ((IEnumerable)source).IndexOf(name);
                                    }
                                }

                                if (index >= 0 && index < count)
                                {
                                    if (indices == null)
                                        indices = new Dictionary<int, int>();

                                    if (!indices.ContainsKey(index))
                                    {
                                        indices[index] = i;
                                        destination[i] = source[index];
                                    }
                                }
                            }

                            for (i = 0; i < numLines; ++i)
                            {
                                if (indices != null && indices.ContainsValue(i))
                                    continue;

                                for (j = 0; j < count; ++j)
                                {
                                    if (indices != null && indices.ContainsKey(j))
                                        continue;

                                    if (indices == null)
                                        indices = new Dictionary<int, int>();

                                    indices[j] = i;

                                    destination[i] = source[j];

                                    break;
                                }
                            }

                            if (handler != null)
                                handler(target, indices, numLines);
                        }

                        isAttribute = false;
                        if (methodInfos != null)
                        {
                            foreach (MethodInfo method in methodInfos)
                            {
                                attributes = method == null ? null : method.GetCustomAttributes(typeof(CSVMethodAttribute), true);
                                if (attributes != null)
                                {
                                    foreach (Attribute attribute in attributes)
                                    {
                                        methodAttribute = attribute as CSVMethodAttribute;
                                        if (methodAttribute != null &&
                                            methodAttribute.type == CSVMethodType.Create &&
                                            (string.IsNullOrEmpty(methodAttribute.path) || methodAttribute.path == propertyPath) &&
                                            method.ReturnType != null)
                                        {
                                            parameterInfos = method.GetParameters();
                                            if (parameterInfos != null && parameterInfos.Length == 1)
                                            {
                                                parameterInfo = parameterInfos[0];
                                                if (parameterInfo != null && parameterInfo.ParameterType == typeof(string))
                                                {
                                                    for (i = 0; i < numLines; ++i)
                                                    {
                                                        line = lines[i + 1];
                                                        if (string.IsNullOrEmpty(line))
                                                            continue;

                                                        k = 0;
                                                        for (j = 0; j < nameIndex; ++j)
                                                        {
                                                            k = line.IndexOf(',', k);
                                                            if (k == -1)
                                                                break;

                                                            ++k;
                                                        }

                                                        if (k != -1)
                                                        {
                                                            index = line.IndexOf(',', k);
                                                            if (index == -1)
                                                                index = line.Length;

                                                            if (parameters == null)
                                                                parameters = new object[1];

                                                            parameters[0] = line.Substring(k, index - k);

                                                            try
                                                            {
                                                                if (isGameObjectOrComponent)
                                                                    destination[i] = __GetPrefabInstance(method.Invoke(method.IsStatic ? null : target, parameters) as UnityEngine.Object, ref instances, ref prefabs);
                                                                else
                                                                    destination[i] = method.Invoke(method.IsStatic ? null : target, parameters);

                                                            }
                                                            catch (Exception e)
                                                            {
                                                                if (e != null)
                                                                    Debug.LogException(e.InnerException ?? e);
                                                            }
                                                        }
                                                    }
                                                }
                                            }

                                            isAttribute = true;

                                            break;
                                        }
                                    }

                                    if (isAttribute)
                                        break;
                                }
                            }
                        }

                        if (type.IsPrimitive || type == typeof(string))
                        {
                            if (isAttribute)
                                continue;

                            for (i = 0; i < numLines; ++i)
                            {
                                if (EditorUtility.DisplayCancelableProgressBar("CSV Loading..", "Set Names", i * 1.0f / numLines))
                                {
                                    EditorUtility.ClearProgressBar();

                                    return null;
                                }

                                line = lines[i + 1];
                                if (string.IsNullOrEmpty(line))
                                    continue;

                                temp = null;
                                if (guidIndex >= 0)
                                {
                                    k = 0;
                                    for (j = 0; j < guidIndex; ++j)
                                    {
                                        k = line.IndexOf(',', k);
                                        if (k == -1)
                                            break;

                                        ++k;
                                    }

                                    if (k != -1)
                                    {
                                        index = line.IndexOf(',', k);
                                        if (index == -1)
                                            index = line.Length;

                                        try
                                        {
                                            temp = line.Substring(k, index - k).As(type, guids);
                                        }
                                        catch (Exception e)
                                        {
                                            if (e != null)
                                                Debug.LogException(e.InnerException ?? e);
                                        }
                                    }
                                }

                                if (nameIndex >= 0 && temp == null)
                                {
                                    k = 0;
                                    for (j = 0; j < nameIndex; ++j)
                                    {
                                        k = line.IndexOf(',', k);
                                        if (k == -1)
                                            break;

                                        ++k;
                                    }

                                    if (k != -1)
                                    {
                                        index = line.IndexOf(',', k);
                                        if (index == -1)
                                            index = line.Length;

                                        try
                                        {
                                            temp = line.Substring(k, index - k).As(type, names);
                                        }
                                        catch (Exception e)
                                        {
                                            if (e != null)
                                                Debug.LogException(e.InnerException ?? e);
                                        }
                                    }
                                }

                                if (temp == null)
                                    Debug.LogError("Fail to cast value on " + i);
                                else
                                    destination[i] = temp;
                            }
                        }
                        else
                        {
                            fieldInfos = type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetField);

                            propertyInfos = type.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty);

                            if (isGameObjectOrComponent)
                            {
                                for (i = 0; i < numLines; ++i)
                                {
                                    line = lines[i + 1];
                                    if (string.IsNullOrEmpty(line))
                                        continue;

                                    instance = destination[i] as UnityEngine.Object;
                                    if (instance == null)
                                        continue;

                                    if (EditorUtility.DisplayCancelableProgressBar("CSV Loading..", "Update Components", i * 1.0f / numLines))
                                    {
                                        EditorUtility.ClearProgressBar();

                                        return null;
                                    }

                                    gameObject = instance as GameObject;
                                    if (gameObject == null)
                                    {
                                        component = instance as Component;
                                        gameObject = component == null ? null : component.gameObject;
                                    }

                                    components = gameObject == null ? null : gameObject.GetComponentsInChildren<Component>(true);
                                    if (components == null)
                                        continue;

                                    if (prefabs == null || !prefabs.TryGetValue(instance, out prefab))
                                        prefab = null;

                                    foreach (Component tempComponent in components)
                                    {
                                        tempType = tempComponent == null ? null : tempComponent.GetType();
                                        if (tempType == null)
                                            continue;

                                        fieldInfos = tempType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetField);

                                        foreach (FieldInfo child in fieldInfos)
                                        {
                                            fieldAttribute = child?.GetCustomAttribute<CSVFieldAttribute>(true);
                                            if (fieldAttribute != null)
                                            {
                                                index = Array.IndexOf(fields, child.Name);
                                                if (index != -1)
                                                {
                                                    k = 0;
                                                    for (j = 0; j < index; ++j)
                                                    {
                                                        k = line.IndexOf(',', k);
                                                        if (k == -1)
                                                            break;

                                                        ++k;
                                                    }

                                                    if (k != -1)
                                                    {
                                                        index = line.IndexOf(',', k);
                                                        if (index == -1)
                                                            index = line.Length;

                                                        if (index > k)
                                                        {
                                                            try
                                                            {
                                                                if ((fieldAttribute.flag & CSVFieldFlag.OverrideNearestPrefab) == CSVFieldFlag.OverrideNearestPrefab && prefab != null)
                                                                    component = __GetCorrespondingObject(tempComponent, prefab, instances, prefabs);
                                                                else
                                                                    component = tempComponent;

                                                                child.SetValue(component, line.Substring(k, index - k).As(child.FieldType, names));

                                                                if (prefab != null)
                                                                    PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                                                            }
                                                            catch (Exception e)
                                                            {
                                                                if (e != null)
                                                                    Debug.LogException(e.InnerException ?? e);
                                                            }

                                                            continue;
                                                        }
                                                    }
                                                }

                                                if ((fieldAttribute.flag & CSVFieldFlag.SetDefaultIfNoField) == CSVFieldFlag.SetDefaultIfNoField)
                                                {
                                                    tempType = child.FieldType;
                                                    try
                                                    {
                                                        if ((fieldAttribute.flag & CSVFieldFlag.OverrideNearestPrefab) == CSVFieldFlag.OverrideNearestPrefab && prefab != null)
                                                            component = __GetCorrespondingObject(tempComponent, prefab, instances, prefabs);
                                                        else
                                                            component = tempComponent;

                                                        child.SetValue(component, tempType == null || tempType.IsClass ? null : Activator.CreateInstance(tempType));

                                                        if (prefab != null)
                                                            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        if (e != null)
                                                            Debug.LogException(e.InnerException ?? e);
                                                    }
                                                }
                                            }
                                        }

                                        propertyInfos = tempType.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty);

                                        foreach (PropertyInfo child in propertyInfos)
                                        {
                                            fieldAttribute = child?.GetCustomAttribute<CSVFieldAttribute>(true);
                                            if (fieldAttribute != null)
                                            {
                                                index = Array.IndexOf(fields, child.Name);
                                                if (index != -1)
                                                {
                                                    k = 0;
                                                    for (j = 0; j < index; ++j)
                                                    {
                                                        k = line.IndexOf(',', k);
                                                        if (k == -1)
                                                            break;

                                                        ++k;
                                                    }

                                                    if (k != -1)
                                                    {
                                                        index = line.IndexOf(',', k);
                                                        if (index == -1)
                                                            index = line.Length;

                                                        if (index > k)
                                                        {
                                                            methodInfo = child.GetSetMethod();
                                                            if (methodInfo != null)
                                                            {
                                                                if (parameters == null || parameters.Length < 1)
                                                                    parameters = new object[1];

                                                                try
                                                                {
                                                                    parameters[0] = line.Substring(k, index - k).As(child.PropertyType, names);

                                                                    if ((fieldAttribute.flag & CSVFieldFlag.OverrideNearestPrefab) == CSVFieldFlag.OverrideNearestPrefab && prefab != null)
                                                                        component = __GetCorrespondingObject(tempComponent, prefab, instances, prefabs);
                                                                    else
                                                                        component = tempComponent;

                                                                    methodInfo.Invoke(component, parameters);

                                                                    if (prefab != null)
                                                                        PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                                                                }
                                                                catch (Exception e)
                                                                {
                                                                    if (e != null)
                                                                        Debug.LogException(e.InnerException ?? e);
                                                                }
                                                            }

                                                            continue;
                                                        }
                                                    }
                                                }

                                                if ((fieldAttribute.flag & CSVFieldFlag.SetDefaultIfNoField) == CSVFieldFlag.SetDefaultIfNoField)
                                                {
                                                    methodInfo = child.GetSetMethod();
                                                    if (methodInfo != null)
                                                    {
                                                        if (parameters == null || parameters.Length < 1)
                                                            parameters = new object[1];

                                                        tempType = child.PropertyType;
                                                        try
                                                        {
                                                            parameters[0] = tempType == null || tempType.IsClass ? null : Activator.CreateInstance(tempType);

                                                            if ((fieldAttribute.flag & CSVFieldFlag.OverrideNearestPrefab) == CSVFieldFlag.OverrideNearestPrefab && prefab != null)
                                                                component = __GetCorrespondingObject(tempComponent, prefab, instances, prefabs);
                                                            else
                                                                component = tempComponent;

                                                            methodInfo.Invoke(component, parameters);

                                                            if (prefab != null)
                                                                PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                                                        }
                                                        catch (Exception e)
                                                        {
                                                            if (e != null)
                                                                Debug.LogException(e.InnerException ?? e);
                                                        }
                                                    }
                                                }
                                            }
                                        }

                                    }
                                }

                                if (prefabs != null)
                                {
                                    count = destination == null ? 0 : destination.Count;
                                    for (i = 0; i < count; ++i)
                                    {
                                        if (EditorUtility.DisplayCancelableProgressBar("CSV Loading..", "Replace Instances To Prefabs(" + i + '/' + count + ')', i * 1.0f / count))
                                        {
                                            EditorUtility.ClearProgressBar();

                                            return null;
                                        }

                                        instance = destination[i] as UnityEngine.Object;
                                        if (instance != null && prefabs.TryGetValue(instance, out prefab))
                                        {
                                            /*PrefabUtility.ApplyPrefabInstance(
                                                PrefabUtility.GetNearestPrefabInstanceRoot(instance),
                                                InteractionMode.AutomatedAction);*/

                                            destination[i] = prefab;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                for (i = 0; i < numLines; ++i)
                                {
                                    if (EditorUtility.DisplayCancelableProgressBar("CSV Loading..", "Load Data", i * 1.0f / numLines))
                                    {
                                        EditorUtility.ClearProgressBar();

                                        return null;
                                    }

                                    line = lines[i + 1];
                                    if (string.IsNullOrEmpty(line))
                                        continue;

                                    temp = destination[i];
                                    if (temp == null)
                                        temp = Activator.CreateInstance(type);

                                    foreach (FieldInfo child in fieldInfos)
                                    {
                                        fieldAttribute = child.GetCustomAttribute<CSVFieldAttribute>(true);
                                        if (fieldAttribute != null)
                                        {
                                            index = Array.IndexOf(fields, child.Name);
                                            if (index != -1)
                                            {
                                                k = 0;
                                                for (j = 0; j < index; ++j)
                                                {
                                                    k = line.IndexOf(',', k);
                                                    if (k == -1)
                                                        break;

                                                    ++k;
                                                }

                                                if (k != -1)
                                                {
                                                    index = line.IndexOf(',', k);
                                                    if (index == -1)
                                                        index = line.Length;

                                                    if (index > k)
                                                    {
                                                        try
                                                        {
                                                            child.SetValue(temp, line.Substring(k, index - k).As(child.FieldType, names));
                                                        }
                                                        catch (Exception e)
                                                        {
                                                            if (e != null)
                                                                Debug.LogException(e.InnerException ?? e);
                                                        }

                                                        continue;
                                                    }
                                                }
                                            }

                                            if ((fieldAttribute.flag & CSVFieldFlag.SetDefaultIfNoField) == CSVFieldFlag.SetDefaultIfNoField)
                                            {
                                                tempType = child.FieldType;
                                                try
                                                {
                                                    child.SetValue(temp, tempType == null || tempType.IsClass ? null : Activator.CreateInstance(tempType));
                                                }
                                                catch (Exception e)
                                                {
                                                    if (e != null)
                                                        Debug.LogException(e.InnerException ?? e);
                                                }
                                            }
                                        }
                                    }

                                    foreach (PropertyInfo child in propertyInfos)
                                    {
                                        fieldAttribute = child.GetCustomAttribute<CSVFieldAttribute>(true);
                                        if (fieldAttribute != null)
                                        {
                                            index = Array.IndexOf(fields, child.Name);
                                            if (index != -1)
                                            {
                                                k = 0;
                                                for (j = 0; j < index; ++j)
                                                {
                                                    k = line.IndexOf(',', k);
                                                    if (k == -1)
                                                        break;

                                                    ++k;
                                                }

                                                if (k != -1)
                                                {
                                                    index = line.IndexOf(',', k);
                                                    if (index == -1)
                                                        index = line.Length;

                                                    if (index > k)
                                                    {
                                                        methodInfo = child.GetSetMethod();
                                                        if (methodInfo != null)
                                                        {
                                                            if (parameters == null || parameters.Length < 1)
                                                                parameters = new object[1];

                                                            try
                                                            {
                                                                parameters[0] = line.Substring(k, index - k).As(child.PropertyType, names);

                                                                methodInfo.Invoke(temp, parameters);
                                                            }
                                                            catch (Exception e)
                                                            {
                                                                if (e != null)
                                                                    Debug.LogException(e.InnerException ?? e);
                                                            }
                                                        }

                                                        continue;
                                                    }
                                                }
                                            }

                                            if ((fieldAttribute.flag & CSVFieldFlag.SetDefaultIfNoField) == CSVFieldFlag.SetDefaultIfNoField)
                                            {
                                                methodInfo = child.GetSetMethod();
                                                if (methodInfo != null)
                                                {
                                                    if (parameters == null || parameters.Length < 1)
                                                        parameters = new object[1];

                                                    tempType = child.PropertyType;
                                                    try
                                                    {
                                                        parameters[0] = tempType == null || tempType.IsClass ? null : Activator.CreateInstance(tempType);

                                                        methodInfo.Invoke(temp, parameters);
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        if (e != null)
                                                            Debug.LogException(e.InnerException ?? e);
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    destination[i] = temp;
                                }
                            }
                        }

                        try
                        {
                            object child = destination;
                            while (objectFieldInfos.TryPop(out var objectFieldInfo))
                            {
                                parent = objectFieldInfo.Item1;
                                
                                objectFieldInfo.Item2.SetValue(parent, child);

                                child = parent;
                            }
                            
                            //fieldInfo.SetValue(parent, destination);
                        }
                        catch (Exception e)
                        {
                            if (e != null)
                                Debug.LogException(e.InnerException ?? e);
                        }
                    }
                }

                if (methodInfos != null)
                {
                    foreach (MethodInfo method in methodInfos)
                    {
                        isAttribute = false;
                        attributes = method == null ? null : method.GetCustomAttributes(typeof(CSVMethodAttribute), true);
                        if (attributes != null)
                        {
                            foreach (Attribute attribute in attributes)
                            {
                                methodAttribute = attribute as CSVMethodAttribute;
                                if (methodAttribute != null && methodAttribute.type == CSVMethodType.Update && (string.IsNullOrEmpty(methodAttribute.path) || methodAttribute.path == propertyPath))
                                {
                                    isAttribute = true;

                                    break;
                                }
                            }
                        }

                        if (isAttribute)
                        {
                            parameterInfos = method.GetParameters();
                            if (parameterInfos == null || parameterInfos.Length < 1)
                            {
                                try
                                {
                                    method.Invoke(method.IsStatic ? null : target, null);
                                }
                                catch (Exception e)
                                {
                                    if (e != null)
                                        Debug.LogException(e.InnerException ?? e);
                                }
                            }
                        }
                    }
                }

                EditorUtility.SetDirty(target);
            }

            if (instances != null)
            {
                i = 0;
                count = prefabs.Count;
                HashSet<GameObject> gameObjects = new HashSet<GameObject>();
                foreach (var prefabTemp in instances.Keys)
                {
                    if (EditorUtility.DisplayCancelableProgressBar("CSV Loading..", "Apply Prefab Instances", i++ * 1.0f / count))
                    {
                        EditorUtility.ClearProgressBar();

                        return null;
                    }

                    __Apply(prefabTemp, gameObjects, instances);
                }

                i = 0;
                count = gameObjects.Count;
                foreach (var gameObjectTemp in gameObjects)
                {
                    if (EditorUtility.DisplayCancelableProgressBar("CSV Loading..", "Destroy Prefab Instances", i++ * 1.0f / count))
                    {
                        EditorUtility.ClearProgressBar();

                        return null;
                    }

                    UnityEngine.Object.DestroyImmediate(gameObjectTemp);
                }
            }

            EditorUtility.ClearProgressBar();

            return lines;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;// * 2.0f;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            CSVAttribute attribute = base.attribute as CSVAttribute;
            if (attribute == null)
                return;

            SerializedObject serializedObject = property == null ? null : property.serializedObject;
            if (serializedObject == null)
                return;

            float singleLineHeight = EditorGUIUtility.singleLineHeight;

            position.height = singleLineHeight;
            if (GUI.Button(position, "Load"))
            {
                string path = property.stringValue;

                AssetDatabase.StartAssetEditing();

                string propertyPath = property.propertyPath, 
                    parentPath = EditorHelper.GetParentPath(propertyPath), 
                    attributePath = string.IsNullOrEmpty(parentPath) || parentPath == propertyPath ? attribute.path : $"{parentPath}.{attribute.path}";

                Load(serializedObject.FindProperty(attributePath), attribute.guidIndex, attribute.nameIndex, null, null, Load(ref path), null);

                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();

                serializedObject.Update();
                property.stringValue = path;
            }

            /*position.y += singleLineHeight;
            position.height = singleLineHeight;
            if (GUI.Button(position, "Save"))
            {

            }*/
        }

        private static GameObject __GetPrefabRoot(UnityEngine.Object target, out bool isComponent)
        {
            GameObject gameObject = target as GameObject;
            if (gameObject == null)
            {
                isComponent = true;
                gameObject = ((Component)target).transform.root.gameObject;
            }
            else
            {
                isComponent = false;
                gameObject = gameObject.transform.root.gameObject;
            }

            return gameObject;
        }

        private static UnityEngine.Object __GetPrefabInstance(
            UnityEngine.Object source,
            ref Dictionary<GameObject, Instance> instances,
            ref Dictionary<UnityEngine.Object, UnityEngine.Object> prefabs)
        {
            if (source == null)
                return null;

            if (PrefabUtility.GetPrefabAssetType(source) == PrefabAssetType.NotAPrefab)
                return source;

            if (instances == null)
                instances = new Dictionary<GameObject, Instance>();
            
            GameObject gameObject = __GetPrefabRoot(source, out bool isComponent);

            UnityEngine.Object destination;
            if (instances.TryGetValue(gameObject, out var instance))
                destination = FindCorrespondingObjectFromInstanceRoot(instance.root, source);
            else
            {
                destination = PrefabUtility.InstantiatePrefab(source);

                instance.root = isComponent ? ((Component)destination).transform.root.gameObject : ((GameObject)destination).transform.root.gameObject;

                instance.parents = null;

                instances[gameObject] = instance;

                if (prefabs == null)
                    prefabs = new Dictionary<UnityEngine.Object, UnityEngine.Object>();

                prefabs[destination] = source;
            }

            return destination;
        }

        private static Component __GetCorrespondingObject(
            Component source,
            UnityEngine.Object prefab,
            Dictionary<GameObject, Instance> instances,
            Dictionary<UnityEngine.Object, UnityEngine.Object> prefabs)
        {
            string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(source);
            source = PrefabUtility.GetCorrespondingObjectFromSourceAtPath(source, path);

            GameObject gameObject = source.transform.root.gameObject;
            Component destination;
            if (instances.TryGetValue(gameObject, out var instance))
                destination = FindCorrespondingObjectFromInstanceRoot(instance.root, source);
            else
            {
                destination = (Component)PrefabUtility.InstantiatePrefab(source);

                instance.root = destination.transform.root.gameObject;
                
                prefabs[destination] = source;
            }

            if (instance.parents == null)
                instance.parents = new HashSet<GameObject>();

            instance.parents.Add(__GetPrefabRoot(prefab, out _));

            instances[gameObject] = instance;

            return destination;
        }

        private static void __Apply(
            GameObject prefab,
            HashSet<GameObject> gameObjects,
            Dictionary<GameObject, Instance> instances)
        {
            var instance = instances[prefab];
            if (!gameObjects.Add(instance.root))
                return;

            if (instance.parents != null)
            {
                foreach(var parent in instance.parents)
                    __Apply(parent, gameObjects, instances);
            }

            try
            {
                PrefabUtility.ApplyPrefabInstance(instance.root, InteractionMode.AutomatedAction);
            }
            catch (Exception e)
            {
                if (e != null)
                    Debug.LogException(e.InnerException ?? e);
            }
        }
    }
}