using Newtonsoft.Json;
using PCBuddy.Models.ComputerInfo;
using PCBuddy.Models.Enums;
using System;
using System.CodeDom.Compiler;
using System.IO;

namespace PCBuddy.Models.Settings
{
    public static class JsonSettingsManager
    {
        public static void SaveComputerSettingsToFile(UserProfile profile, Computer computer)
        {
            var jsonSettingsObject =
                new JsonSettingsObject()
                {
                    Profile =  profile,
                    Computer = computer
                };


            var settingsSerialized = JsonConvert.SerializeObject(jsonSettingsObject);

            var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            var appFolder = Path.Combine(folder, "PCBuddy");
            Directory.CreateDirectory(appFolder);

            var savePath = Path.Combine(appFolder, "settings.json");

            File.WriteAllText(savePath, settingsSerialized);
        }
    }
}


