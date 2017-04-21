using System.IO;

namespace Ets2Map
{
    public class Ets2RoadLook
    {
        public bool IsHighway { get; set; }
        public bool IsLocal { get; set; }
        public bool IsExpress { get; set; }
        public bool IsNoVehicles { get; set; }
        public string LookID { get; private set; }

        public float Offset;
        public float SizeLeft;
        public float SizeRight;
        public float ShoulderLeft;
        public float ShoulderRight;

        public int LanesLeft;
        public int LanesRight;

        public Ets2RoadLook(string look)
        {
            LookID = look;
        }

        public float GetTotalWidth()
        {
            return Offset + 4.5f*LanesLeft + 4.5f*LanesRight;
        }
    }
}