using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using VRageMath;


/*Font color string can be one of the following;
    Debug
    Red
    Green
    Blue
    White
    DarkBlue
    UrlNormal
    UrlHighlight
    ErrorMessageBoxCaption
    ErrorMessageBoxText
    InfoMessageBoxCaption
    InfoMessageBoxText
    ScreenCaption
    GameCredits
    LoadingScreen
    BuildInfo
    BuildInfoHighlight
*/

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
            public string AlertMessageEnter;
            public string AlertMessageLeave;
            public int AlertTimeMs;

            public GPS(string name, Vector3D position, double alertRadius = 0, string alertMessageEnter = "Alert", string alertMessageLeave = "Safe", int alertTimeMs = 2000)
            {
                UniqueName = name;
                Position = position;
                AlertRadius = alertRadius;
                AlertMessageEnter = alertMessageEnter;
                AlertMessageLeave = alertMessageLeave;
                AlertTimeMs = alertTimeMs;
            }
        }

        public struct PlanetInfo
        {
            public string PlanetName;
            public double AlertRadius;
            public string AlertMessageEnter;
            public string AlertMessageLeave;
            public int AlertTimeMs;

            public PlanetInfo(string planetName, double alertRadius = 0, string alertMessageEnter = "Alert", string alertMessageLeave = "Safe", int alertTimeMs = 2000)
            {
                PlanetName = planetName;
                AlertRadius = alertRadius;
                AlertMessageEnter = alertMessageEnter;
                AlertMessageLeave = alertMessageLeave;
                AlertTimeMs = alertTimeMs;
            }
        }


        public List<GPS> GPSlocations;
        public List<PlanetInfo> PlanetLocations;

        public NotificationConfig()
        {
            GPSlocations = new List<GPS>();
            PlanetLocations = new List<PlanetInfo>();
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
            defaultSettings.PlanetLocations.Add(new PlanetInfo("EarthLike-12345d120000"));

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
