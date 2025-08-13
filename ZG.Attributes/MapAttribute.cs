using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ZG
{
    public class MapAttribute : IndexAttribute
    {
        public int keyGuidIndex = 0;
        public int keyNameIndex = 1;

        public int valueGuidIndex = 0;
        public int valueNameIndex = 1;

        public MapAttribute() : base(null)
        {

        }

        public MapAttribute(string path) : base(path)
        {

        }
    }
}