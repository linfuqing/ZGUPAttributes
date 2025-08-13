using UnityEngine;

namespace ZG
{
    public enum RotationType
    {
        Normal, 
        Direction
    }

    public class RotationAttribute : PropertyAttribute
    {
        public RotationType type;

        public RotationAttribute(RotationType type = RotationType.Normal)
        {
            this.type = type;
        }
    }
}