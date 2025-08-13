using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ZG
{
    [CustomPropertyDrawer(typeof(TypeAttribute))]
    public class TypeDrawer : PropertyDrawer
    {
        private static Dictionary<Type, List<Type>> __types;

        private string[] __options;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            List<Type> types;

            if (__types == null)
            {
                __types = new Dictionary<Type, List<Type>>();

                foreach (var assemble in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach(var type in assemble.GetTypes())
                    {
                        foreach(var attribute in type.GetCustomAttributesData())
                        {
                            if(!__types.TryGetValue(attribute.AttributeType, out types))
                            {
                                types = new List<Type>();

                                __types[attribute.AttributeType] = types;
                            }

                            types.Add(type);
                        }

                        foreach(var interfaceType in  type.GetInterfaces())
                        {
                            if (!__types.TryGetValue(interfaceType, out types))
                            {
                                types = new List<Type>();

                                __types[interfaceType] = types;
                            }

                            types.Add(type);
                        }
                    }
                }
            }

            if (property.propertyType == SerializedPropertyType.String)
            {
                var attribute = base.attribute as TypeAttribute;

                if (__options == null)
                {
                    var options = new List<string>();
                    foreach (var interfaceOrAttributeType in attribute.interfaceOrAttributeTypes)
                    {
                        if (__types.TryGetValue(interfaceOrAttributeType, out types))
                        {
                            foreach (var type in types)
                                options.Add(type.AssemblyQualifiedName);
                        }
                    }

                    __options = options.ToArray();
                }

                int selectedIndex = EditorGUI.Popup(position, property.displayName, Array.IndexOf(__options, property.stringValue), __options);
                property.stringValue = selectedIndex == -1 ? string.Empty : __options[selectedIndex];
            }
            else
                EditorGUI.LabelField(position, "Need String.");
        }
    }
}