using System.IO;

namespace Ets2Map
{
    public class Ets2RoadLook
    {
        public bool IsHighway { get; private set; }
        public bool IsLocal { get; private set; }
        public bool IsExpress { get; private set; }
        public bool IsNoVehicles { get; private set; }
        public string LookID { get; private set; }
        private Ets2Mapper Mapper;

        public float Offset;
        public float SizeLeft;
        public float SizeRight;
        public float ShoulderLeft;
        public float ShoulderRight;

        public int LanesLeft;
        public int LanesRight;

        public Ets2RoadLook(string look, Ets2Mapper mapper)
        {
            LookID = look;
            Mapper = mapper;

            var roadLookData = mapper.LUTSIIFolder + "-road_look.sii";
            var roadLookTemplateData = mapper.LUTSIIFolder + "-road_look.template.sii";
            var roadLookTemplateFrData = mapper.LUTSIIFolder + "-road_look.template.dlc_fr.sii";

            if (!FindInFiles(roadLookData))
                if (!FindInFiles(roadLookTemplateData))
                    FindInFiles(roadLookTemplateFrData);
        }

        private bool FindInFiles(string path)
        {
            bool found = false;
            if (!File.Exists(path)) return found;
            var fileData = File.ReadLines(path);
            foreach (var k in fileData)
            {
                if (!found)
                {
                    if (k.StartsWith("road_look") && k.Contains(LookID))
                    {
                        found = true;
                    }
                }
                else
                {
                    //value:
                    if (k.Contains(":"))
                    {
                        var key = k;
                        var data = key.Substring(key.IndexOf(":") + 1).Trim();
                        key = key.Substring(0, key.IndexOf(":")).Trim();

                        switch (key)
                        {
                            case "road_size_left":
                                float.TryParse(data, out SizeLeft);
                                break;

                            case "road_size_right":
                                float.TryParse(data, out SizeRight);
                                break;

                            case "shoulder_size_right":
                                float.TryParse(data, out ShoulderLeft);
                                break;

                            case "shoulder_size_left":
                                float.TryParse(data, out ShoulderRight);
                                break;

                            case "road_offset":
                                float.TryParse(data, out Offset);
                                break;
                            case "lanes_left[]":
                                LanesLeft++;
                                IsLocal = (data == "traffic_lane.road.local");
                                IsExpress = (data == "traffic_lane.road.expressway");
                                IsHighway = (data == "traffic_lane.road.motorway" || data == "traffic_lane.road.motorway.low_density");
                                IsNoVehicles = (data == "traffic_lane.no_vehicles");
                                break;

                            case "lanes_right[]":
                                LanesRight++;
                                IsLocal = (data == "traffic_lane.road.local");
                                IsExpress = (data == "traffic_lane.road.expressway");
                                IsHighway = (data == "traffic_lane.road.motorway" || data == "traffic_lane.road.motorway.low_density");
                                IsNoVehicles = (data == "traffic_lane.no_vehicles");
                                break;
                        }
                    }
                    if (k.Trim() == "}")
                        break;
                }
            }
            return found;
        }

        public float GetTotalWidth()
        {
            return Offset + 4.5f*LanesLeft + 4.5f*LanesRight;
        }
    }
}