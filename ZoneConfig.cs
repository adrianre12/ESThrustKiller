using Sandbox.ModAPI;
using System;
using System.Xml.Serialization;

namespace ESThrustKiller.Configuration
{

    public class ZoneConfig
    {
        const string configFilename = "Config-ESThrustKiller.xml";

        public string Test;

        [XmlIgnore]
        public bool ConfigLoaded;

        public ZoneConfig()
        {
            Test = "Testing.";
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

                    Log.Msg($"ERROR: Could Not Load Settings From {configFilename}. Using Default Configuration.");
                    var defaultSettings = new ZoneConfig();
                    return defaultSettings;

                }

            }
            else
            {

                Log.Msg("Config-ESThrustKiller Doesn't Exist. Creating Default Configuration. ");

            }

            var settings = new ZoneConfig();

            try
            {

                using (var writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(configFilename, typeof(ZoneConfig)))
                {

                    writer.Write(MyAPIGateway.Utilities.SerializeToXML<ZoneConfig>(settings));

                }

            }
            catch (Exception exc)
            {

                Log.Msg($"ERROR: Could Not Create {configFilename}. Default Settings Will Be Used.");

            }

            return settings;
        }
    }
}
