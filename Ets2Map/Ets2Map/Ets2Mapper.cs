using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ets2Map
{
    public class Ets2Mapper
    {
        public bool Loading { get; private set; }

        public string SectorFolder { get; private set; }
        public string[] SectorFiles { get; private set; }

        public string PrefabFolder { get; private set; }

        public string LutSiiFolder { get; private set; }
        public string LutJsonFolder { get; private set; }

        public List<Ets2Sector> Sectors { get; private set; }

        public ConcurrentDictionary<ulong, Ets2Node> Nodes = new ConcurrentDictionary<ulong, Ets2Node>();
        public ConcurrentDictionary<ulong, Ets2Item> Items = new ConcurrentDictionary<ulong, Ets2Item>();

        public Dictionary<string, Ets2Item> Cities = new Dictionary<string, Ets2Item>();
        public Dictionary<Tuple<string, string>, Ets2Item> Companies = new Dictionary<Tuple<string, string>, Ets2Item>();

        /***  SOME ITEMS CROSS MULTIPLE SECTORS; PENDING SEARCH REQUESTS ***/
        private List<Ets2ItemSearchRequest> ItemSearchRequests { get; set; }

        /*** VARIOUS LOOK UP TABLES (LUTs) TO FIND CERTAIN GAME ITEMS ***/ 
        internal List<Ets2Prefab> PrefabsLookup = new List<Ets2Prefab>();
        private List<Ets2Company> CompaniesLookup = new List<Ets2Company>();
        private List<Ets2RoadLook> RoadLookLookup = new List<Ets2RoadLook>();

        private Dictionary<ulong, Ets2Prefab> PrefabLookup = new Dictionary<ulong, Ets2Prefab>();
        private Dictionary<ulong, string> CitiesLookup = new Dictionary<ulong, string>();
        private Dictionary<ulong, Ets2RoadLook> RoadLookup = new Dictionary<ulong, Ets2RoadLook>(); 

        public Ets2Mapper(string sectorFolder, string prefabFolder,string lutSii, string lutJson)
        {
            SectorFolder = sectorFolder;
            PrefabFolder = prefabFolder;

            SectorFiles = Directory.GetFiles(sectorFolder, "*.base");

            LutSiiFolder = lutSii;
            LutJsonFolder = lutJson;
        }

        public Ets2Item FindClosestRoadPrefab(Ets2Point location)
        {
            // Find road or prefab closest by
            var closestPrefab =
                Items.Values.Where(x => x.HideUI==false && x.Type == Ets2ItemType.Prefab && x.Prefab != null && x.Prefab.Curves.Any())
                    .OrderBy(x => Math.Sqrt(Math.Pow(location.X - x.PrefabNode.X, 2) + Math.Pow(location.Z - x.PrefabNode.Z, 2)))
                    .FirstOrDefault();
            return closestPrefab;
        }
        
        /// <summary>
        /// Navigate from X/Y to X/Y coordinates
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        public Ets2NavigationRoute NavigateTo(Ets2Point from, Ets2Point to)
        {
            var start = FindClosestRoadPrefab(from);
            var end = FindClosestRoadPrefab(to);

            Console.WriteLine("Navigating from " + start.ItemUID.ToString("X16") + " to " + end.ItemUID.ToString("X16"));
            // Look up pre-fab closest by these 2 points
            return new Ets2NavigationRoute(start,end, from, to, this);
        }

        /// <summary>
        /// Navigate to city from X/Y point
        /// </summary>
        /// <param name="from"></param>
        /// <param name="city"></param>
        public Ets2NavigationRoute NavigateTo(Ets2Point from, string city)
        {
            if (Cities.ContainsKey(city) == false)
                return null;

            var cityPoint = Cities[city].StartNode.Point;

            var start = FindClosestRoadPrefab(from);
            var end = FindClosestRoadPrefab(cityPoint);

            return new Ets2NavigationRoute(start, end, from, null, this);
        }
        /// <summary>
        /// Navigate to city company from X/Y point
        /// </summary>
        /// <param name="from"></param>
        /// <param name="city"></param>
        /// <param name="company"></param>
        public Ets2NavigationRoute NavigateTo(Ets2Point from, string city, string company)
        {
            throw new NotImplementedException();
        }

        private void ParseRoadLookFiles()
        {
            var roadLookFiles = Directory.GetFiles(LutSiiFolder + @"road\", "*.sii");

            roadLookFiles.ToList().ForEach(file =>
            {
                string road = String.Empty;
                var fileData = File.ReadLines(file);
                Ets2RoadLook look = null;
                foreach (var k in fileData)
                {

                    //value:
                    if (k.Contains(":") && !road.Equals(String.Empty) && look != null)
                    {
                        var key = k;
                        var data = key.Substring(key.IndexOf(':') + 1).Trim();
                        key = key.Substring(0, key.IndexOf(':')).Trim();

                        switch (key)
                        {
                            case "road_size_left":
                                float.TryParse(data, out look.SizeLeft);
                                break;

                            case "road_size_right":
                                float.TryParse(data, out look.SizeRight);
                                break;

                            case "shoulder_size_right":
                                float.TryParse(data, out look.ShoulderLeft);
                                break;

                            case "shoulder_size_left":
                                float.TryParse(data, out look.ShoulderRight);
                                break;

                            case "road_offset":
                                float.TryParse(data, out look.Offset);
                                break;
                            case "lanes_left[]":
                                look.LanesLeft++;
                                look.IsLocal = (data.Equals("traffic_lane.road.local") || data.Equals("traffic_lane.road.local.tram"));
                                look.IsExpress = (data.Equals("traffic_lane.road.expressway") || data.Equals("traffic_lane.road.divided"));
                                look.IsHighway = (data.Equals("traffic_lane.road.motorway") || data.Equals("traffic_lane.road.motorway.low_density") ||
                                                    data.Equals("traffic_lane.road.freeway") || data.Equals("traffic_lane.road.freeway.low_density") ||
                                                    data.Equals("traffic_lane.road.divided"));
                                look.IsNoVehicles = (data.Equals("traffic_lane.no_vehicles"));
                                break;

                            case "lanes_right[]":
                                look.LanesRight++;
                                look.IsLocal = (data.Equals("traffic_lane.road.local") || data.Equals("traffic_lane.road.local.tram"));
                                look.IsExpress = (data.Equals("traffic_lane.road.expressway") || data.Equals("traffic_lane.road.divided"));
                                look.IsHighway = (data.Equals("traffic_lane.road.motorway") || data.Equals("traffic_lane.road.motorway.low_density") ||
                                                    data.Equals("traffic_lane.road.freeway") || data.Equals("traffic_lane.road.freeway.low_density") ||
                                                    data.Equals("traffic_lane.road.divided"));
                                look.IsNoVehicles = (data.Equals("traffic_lane.no_vehicles"));
                                break;
                        }
                    }
                    if (k.StartsWith("road_look"))
                    {
                        var d = k.Split(':');
                        d[1] = d[1].Trim();
                        if (d[1].Length > 3)
                        {
                            road = d[1].Substring(0, d[1].Length - 1).Trim();
                            look = new Ets2RoadLook(road);
                        }
                    }
                    if (k.Trim() == "}")
                    {
                        if (look != null && !RoadLookLookup.Contains(look))
                        {
                            RoadLookLookup.Add(look);
                        }
                        road = String.Empty;
                    }
                }
            });
        }

        private void LoadLUT()
        {
            // PREFABS
            var prefabsJson = LutJsonFolder + "prefabs.json";
            if (File.Exists(prefabsJson))
            {
                var lines = File.ReadAllLines(prefabsJson);
                string filePath = "";
                ulong token = 0;
                foreach (var line in lines)
                {
                    if (line.Trim() == "]")
                    {
                        break;
                    }
                    if (line.Contains(':'))
                    {
                        string k = line.Split(':')[0].Split('"')[1];
                        string v = line.Split(':')[1].Split('"')[1];

                        if (k == "token")
                        {
                            token = ulong.Parse(v, NumberStyles.HexNumber);
                        }
                        if (k == "prefab_desc")
                        {
                            filePath = v;
                        }
                    }

                    if (line.Contains("}"))
                    {
                        if (token != 0 && filePath != "")
                        {
                            filePath = filePath.Substring(filePath.IndexOf('/', 1) + 1);
                            PrefabLookup.Add(token, new Ets2Prefab(this, PrefabFolder + filePath));
                        }

                        filePath = "";
                        token = 0;
                    }
                }

            }
            else
            {
                Console.WriteLine($"Cannot find file: {prefabsJson}");
            }

            // COMPANIES
            var companiesJson = LutJsonFolder + "companies.json";
            if (File.Exists(companiesJson))
            {
                CompaniesLookup =
                    File.ReadAllLines(companiesJson).Select(x => new Ets2Company(x, this)).ToList();
            }
            else
            {
                Console.WriteLine($"Cannot find file: {companiesJson}");
            }

            // CITIES
            var citiesJson = LutJsonFolder + "cities.json";
            if (File.Exists(citiesJson))
            {
                var lines = File.ReadAllLines(citiesJson);
                string cityName = "";
                ulong token = 0;
                foreach (var line in lines)
                {
                    if (line.Trim() == "]")
                    {
                        break;
                    }
                    if (line.Contains(':'))
                    {
                        string k = line.Split(':')[0].Split('"')[1];
                        string v = line.Split(':')[1].Split('"')[1];

                        if (k == "token")
                        {
                            token = ulong.Parse(v, NumberStyles.HexNumber);
                        }
                        if (k == "fullName")
                        {
                            cityName = v;
                        }
                    }
                    if (line.Contains("}"))
                    {
                        if (token != 0 && cityName != "")
                        {
                            CitiesLookup.Add(token, cityName);
                        }

                        cityName = "";
                        token = 0;
                    }
                }
            }
            else
            {
                Console.WriteLine($"Cannot find file: {citiesJson}");
            }

            // ROAD LOOKS
            var roadsJson = LutJsonFolder + "roads.json";
            if (File.Exists(roadsJson))
            {
                var lines = File.ReadAllLines(roadsJson);
                string idName = "";
                ulong token = 0;
                foreach (var line in lines)
                {
                    if (line.Trim() == "]")
                    {
                        break;
                    }
                    if (line.Contains(':'))
                    {
                        string k = line.Split(':')[0].Split('"')[1];
                        string v = line.Split(':')[1].Split('"')[1];

                        if (k == "token")
                        {
                            token = ulong.Parse(v, NumberStyles.HexNumber);
                        }
                        else if (k == "idName")
                        {
                            idName = v;
                        }
                    }

                    if (line.Contains("}"))
                    {
                        if (token != 0 && idName != "")
                        {
                            var obj = RoadLookLookup.FirstOrDefault(x => x.LookID == idName);
                            if (obj != null)
                            {
                                RoadLookup.Add(token, obj);
                            }
                        }

                        idName = "";
                        token = 0;
                    }
                }
            }
            else
            {
                Console.WriteLine($"Cannot find file: {roadsJson}");
            }
        }

        public void Parse(bool skipMultiSectors)
        {
            Loading = true;

            ParseRoadLookFiles();

            // Load all LUTs
            LoadLUT();

            ItemSearchRequests = new List<Ets2ItemSearchRequest>();
            Sectors = SectorFiles.Select(x => new Ets2Sector(this, x)).ToList();

            // 2-stage process so we can validate node UID's at item stage
            ThreadPool.SetMaxThreads(1, 1);
            Parallel.ForEach(Sectors, (sec) => sec.ParseNodes());
            Parallel.ForEach(Sectors, (sec) => sec.ParseItems());

            Loading = false;

            // Some nodes may refer to items in other sectors.
            // We can search all sectors for those, but this is relatively slow process.
            if (!skipMultiSectors)
            {
                // Now find all that were not found
                Console.WriteLine(ItemSearchRequests.Count +
                                  " were not found; attempting to search them through all sectors");
                foreach (var req in ItemSearchRequests)
                {
                    Ets2Item item = Sectors.Select(sec => sec.FindItem(req.ItemUID)).FirstOrDefault(tmp => tmp != null);

                    if (item == null)
                    {
                        Console.WriteLine("Still couldn't find node " + req.ItemUID.ToString("X16"));
                    }
                    else
                    {
                        if (req.IsBackward)
                        {
                            item.Apply(req.Node);
                            req.Node.BackwardItem = item;
                        }
                        if (req.IsForward)
                        {
                            item.Apply(req.Node);
                            req.Node.ForwardItem = item;
                        }

                        if (item.StartNode == null && item.StartNodeUID != null)
                        {
                            Ets2Node startNode;
                            if (Nodes.TryGetValue(item.StartNodeUID, out startNode))
                                item.Apply(startNode);
                        }
                        if (item.EndNode == null && item.EndNodeUID != null)
                        {
                            Ets2Node endNode;
                            if (Nodes.TryGetValue(item.EndNodeUID, out endNode))
                                item.Apply(endNode);
                        }

                        Console.Write(".");
                    }
                }
            }

            // Navigation cache
            BuildNavigationCache();

            // Lookup all cities
            Cities = Items.Values.Where(x => x.Type == Ets2ItemType.City).GroupBy(x=>x.City).Select(x=>x.FirstOrDefault()).ToDictionary(x => x.City, x => x);

            Console.WriteLine(Items.Values.Count(x => x.Type == Ets2ItemType.Building) + " buildings were found");
            Console.WriteLine(Items.Values.Count(x => x.Type == Ets2ItemType.Road) + " roads were found");
            Console.WriteLine(Items.Values.Count(x => x.Type == Ets2ItemType.Prefab) + " prefabs were found");
            Console.WriteLine(Items.Values.Count(x => x.Type == Ets2ItemType.Prefab && x.Prefab != null && x.Prefab.Curves.Any()) + " road prefabs were found");
            Console.WriteLine(Items.Values.Count(x => x.Type == Ets2ItemType.Service) + " service points were found");
            Console.WriteLine(Items.Values.Count(x => x.Type == Ets2ItemType.Company) + " companies were found");
            Console.WriteLine(Items.Values.Count(x => x.Type == Ets2ItemType.City) + " cities were found");
        }

        private void BuildNavigationCache()
        {
            // The idea of navigation cache is that we calculate distances between nodes
            // The nodes we identify as prefabs (cross points etc.)
            // Distance between them are the roads
            // This way we don't have to walk through each road segment (which can be hundreds or thousands) each time we want to know the node-node length
            // This is a reduction of approximately 6x for the current Europe map.
            foreach (var prefab in Items.Values.Where(x => x.HideUI == false && x.Type == Ets2ItemType.Prefab))
            {
                foreach (var node in prefab.NodesList.Values)
                {
                    var endNode = default(Ets2Item);

                    var fw = node.ForwardItem != null && node.ForwardItem.Type == Ets2ItemType.Road;
                    var road = node.ForwardItem != null && node.ForwardItem.Type == Ets2ItemType.Prefab
                    ? node.BackwardItem
                    : node.ForwardItem;
                    var totalLength = 0.0f;
                    var weight = 0.0f;
                    var roadList = new List<Ets2Item>();
                    while (road != null)
                    {
                        if (road.StartNode == null || road.EndNode == null)
                            break;
                        var length =
                            (float)Math.Sqrt(Math.Pow(road.StartNode.X - road.EndNode.X, 2) +
                                      Math.Pow(road.StartNode.Z - road.EndNode.Z, 2));
                        var spd = 1;
                        if (road.RoadLook != null)
                        {
                            if (road.RoadLook.IsExpress) spd = 25;
                            if (road.RoadLook.IsLocal) spd = 45;
                            if (road.RoadLook.IsHighway) spd = 70;
                        }

                        totalLength += length;
                        weight += length / spd;
                        roadList.Add(road);

                        if (fw)
                        {
                            road = road.EndNode == null?null: road.EndNode.ForwardItem;
                            if (road != null && road.Type == Ets2ItemType.Prefab)
                            {
                                endNode = road;
                                break;
                            }
                        }
                        else
                        {
                            road = road.StartNode == null ? null : road.StartNode.BackwardItem;
                            if (road != null && road.Type == Ets2ItemType.Prefab)
                            {
                                endNode = road;
                                break;
                            }
                        }
                    }

                    // If there is no end-node found, it is a dead-end road.
                    if (endNode != null && prefab != endNode)
                    {
                        if (prefab.Navigation.ContainsKey(endNode) == false)
                        {
                            prefab.Navigation.Add(endNode,
                                new Tuple<float, float, IEnumerable<Ets2Item>>(weight, totalLength, roadList));
                        }
                        if (endNode.Navigation.ContainsKey(prefab) == false)
                        {
                            var reversedRoadList = new List<Ets2Item>(roadList);
                            reversedRoadList.Reverse();
                            endNode.Navigation.Add(prefab,
                                new Tuple<float, float, IEnumerable<Ets2Item>>(weight, totalLength, reversedRoadList));
                        }
                    }
                }
            }
        }

        public void Find(Ets2Node node, ulong item, bool isBackward)
        {
            var req = new Ets2ItemSearchRequest
            {
                ItemUID = item,
                Node = node,
                IsBackward = isBackward,
                IsForward = !isBackward
            };

            ItemSearchRequests.Add(req);
        }

        public string LookupCityID(ulong id)
        {
            return !CitiesLookup.ContainsKey(id) ? string.Empty : CitiesLookup[id];
        }

        public Ets2Prefab LookupPrefab(ulong prefabId)
        {
            if (PrefabLookup.ContainsKey(prefabId))
                return PrefabLookup[prefabId];
            else
                return null;
        }

        public Ets2RoadLook LookupRoadLookID(ulong lookId)
        {
            if (RoadLookup.ContainsKey(lookId))
                return RoadLookup[lookId];
            else
                return null;
        }
    }
}