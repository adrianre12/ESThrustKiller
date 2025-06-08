using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using VRageMath;

namespace ESThrustKiller.Notification
{
    public class NotificationConfig
    {
        const string configFilename = "Config-ESZoneNotification.xml";

        [XmlIgnore]
        public bool ConfigLoaded;

        public struct GPS
        {
            public string UniqueName;
            public Vector3D Position;
            public double AlertRadius;
            public string AlertMessage;
            public int AlertTimeMs;

            public GPS(string name, Vector3D position, double alertRadius = 0, string alertMessage = "Alert", int alertTimeMs = 2000)
            {
                UniqueName = name;
                Position = position;
                AlertRadius = alertRadius;
                AlertMessage = alertMessage;
                AlertTimeMs = alertTimeMs;
            }
        }

        public struct PlanetInfo
        {
            public double AlertRadius;
            public string AlertMessage;
            public int AlertTimeMs;

            public PlanetInfo(double alertRadius = 0, string alertMessage = "Alert", int alertTimeMs = 2000)
            {

                AlertRadius = alertRadius;
                AlertMessage = alertMessage;
                AlertTimeMs = alertTimeMs;
            }
        }


        public List<GPS> GPSlocations;

        public Dictionary<string, PlanetInfo> PlanetLocations;

        public NotificationConfig()
        {
            GPSlocations = new List<GPS>();
            PlanetLocations = new Dictionary<string, PlanetInfo>();
        }


        public NotificationConfig LoadSettings()
        {
            if (MyAPIGateway.Utilities.FileExistsInWorldStorage(configFilename, typeof(NotificationConfig)) == true)
            {
                try
                {
                    NotificationConfig config = null;
                    var reader = MyAPIGateway.Utilities.ReadFileInWorldStorage(configFilename, typeof(NotificationConfig));
                    string configcontents = reader.ReadToEnd();
                    config = MyAPIGateway.Utilities.SerializeFromXML<NotificationConfig>(configcontents);
                    config.ConfigLoaded = true;
                    Log.Msg($"Loaded Existing Settings From {configFilename}");
                    return config;
                }
                catch (Exception exc)
                {
                    Log.Msg(exc.ToString());
                    Log.Msg($"ERROR: Could Not Load Settings From {configFilename}. Using Empty Configuration.");
                    return new NotificationConfig();
                }

            }

            Log.Msg($"{configFilename} Doesn't Exist. Creating Default Configuration. ");

            var defaultSettings = new NotificationConfig();
            defaultSettings.GPSlocations.Add(new GPS("Example1", Vector3D.Zero));
            defaultSettings.PlanetLocations.Add("EarthLike-12345d120000", new PlanetInfo());

            try
            {
                using (var writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(configFilename, typeof(NotificationConfig)))
                {
                    writer.Write(MyAPIGateway.Utilities.SerializeToXML<NotificationConfig>(defaultSettings));
                }

            }
            catch (Exception exc)
            {
                Log.Msg(exc.ToString());
                Log.Msg($"ERROR: Could Not Create {configFilename}. Default Settings Will Be Used.");
            }

            return defaultSettings;
        }
    }
}
