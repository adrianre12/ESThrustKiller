using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace ESThrustKiller.Configuration
{

    public class ZoneConfig
    {
        const string configFilename = "Config-ESThrustKiller.xml";

        public List<PlanetInfo> Planets;
        public bool DebugLog;

        [XmlIgnore]
        public bool ConfigLoaded;

        public struct PlanetInfo
        {
            public string Name;
            public int Altitude;

            public PlanetInfo(string name, int altitude)
            {
                Name = name;
                Altitude = altitude;
            }
        }


        public ZoneConfig()
        {
            Planets = new List<PlanetInfo>();
            DebugLog = false;
        }

        public bool TryGetPlanet(string name, out PlanetInfo planetInfo)
        {
            foreach (PlanetInfo info in Planets)
            {
                if (info.Name == name)
                {
                    planetInfo = info;
                    return true;
                }
            }
            planetInfo = new PlanetInfo();
            return false;
        }

        public ZoneConfig LoadSettings()
        {
            if (MyAPIGateway.Utilities.FileExistsInWorldStorage(configFilename, typeof(ZoneConfig)) == true)
            {

                try
                {

                    ZoneConfig config = null;
                    var reader = MyAPIGateway.Utilities.ReadFileInWorldStorage(configFilename, typeof(ZoneConfig));
                    string configcontents = reader.ReadToEnd();
                    config = MyAPIGateway.Utilities.SerializeFromXML<ZoneConfig>(configcontents);
                    config.ConfigLoaded = true;
                    Log.Msg($"Loaded Existing Settings From {configFilename}");
                    return config;

                }
                catch (Exception exc)
                {

                    Log.Msg($"ERROR: Could Not Load Settings From {configFilename}. Using Empty Configuration.");
                    return new ZoneConfig();

                }

            }

            Log.Msg($"{configFilename} Doesn't Exist. Creating Default Configuration. ");

            var defaultSettings = new ZoneConfig();
            defaultSettings.Planets.Add(new PlanetInfo("PlanetA", 10000));
            defaultSettings.Planets.Add(new PlanetInfo("PlanetB", 20000));

            try
            {

                using (var writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(configFilename, typeof(ZoneConfig)))
                {

                    writer.Write(MyAPIGateway.Utilities.SerializeToXML<ZoneConfig>(defaultSettings));

                }

            }
            catch (Exception exc)
            {

                Log.Msg($"ERROR: Could Not Create {configFilename}. Default Settings Will Be Used.");

            }

            return defaultSettings;
        }
    }
}
