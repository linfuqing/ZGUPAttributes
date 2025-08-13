using System;
using UnityEngine;

namespace ZG
{
    public class TypeAttribute : PropertyAttribute
    {
        public Type[] interfaceOrAttributeTypes;

        public TypeAttribute(params Type[] interfaceOrAttributeTypes)
        {
            this.interfaceOrAttributeTypes = interfaceOrAttributeTypes;
        }
    }
}