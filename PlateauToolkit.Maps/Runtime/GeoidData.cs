using System;
using UnityEngine;

namespace PlateauToolkit.Maps
{

    [Serializable]
    public class GeoidData
    {
        public float latitude;
        public float longitude;
        public float geoidHeight;
    }

    [Serializable]
    public class RootObject
    {
        public GeoidData OutputData;
    }
}