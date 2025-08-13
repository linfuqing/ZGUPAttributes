using System;
using UnityEngine;

namespace ZG
{
    public enum CSVMethodType
    {
        Filter, 
        Create, 
        Update
    }

    [Flags]
    public enum CSVFieldFlag
    {
        SetDefaultIfNoField = 0x01, 
        OverrideNearestPrefab = 0x02
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class CSVFieldAttribute : Attribute
    {
        public CSVFieldFlag flag;

        public CSVFieldAttribute(CSVFieldFlag flag = CSVFieldFlag.SetDefaultIfNoField)
        {
            this.flag = flag;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class CSVMethodAttribute : Attribute
    {
        public string path;

        private CSVMethodType __type;

        public CSVMethodType type
        {
            get
            {
                return __type;
            }
        }

        public CSVMethodAttribute(CSVMethodType type)
        {
            __type = type;
        }
    }

    public class CSVAttribute : PropertyAttribute
    {
        public int guidIndex = 0;
        public int nameIndex = 1;

        private string __path;
        public string path
        {
            get
            {
                return __path;
            }
        }

        public CSVAttribute(string path)
        {
            __path = path;
        }
    }

    public static class CSVShared
    {
        public static UnityEngine.Object target;
    }
}