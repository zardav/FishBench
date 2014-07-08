using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FishBench
{
    class FishSettings
    {
        readonly string settingsFile;
        Dictionary<string, string> settings;
        public FishSettings(string settingsFile)
        {
            this.settingsFile = settingsFile;
            string[] lines = File.Exists(settingsFile) ? File.ReadAllLines(settingsFile) : new string[]{};
            settings = new Dictionary<string, string>(lines.Length);
            char[] sep = {' ', ':'};
            for (int i = 0; i < lines.Length; i++)
            {
                string[] tup = lines[i].Split(sep, 2, StringSplitOptions.RemoveEmptyEntries);
                settings.Add(tup[0], tup[1]);
            }
        }
        public string this[string key]
        {
            get
            {
                return settings.ContainsKey(key) ? settings[key] : "";
            }
            set
            {
                settings[key] = value;
                string[] lines = new string[settings.Count];
                int i = 0;
                foreach (var pair in settings)
                {
                    lines[i++] = (pair.Key + ": " + pair.Value);
                }
                File.WriteAllLines(settingsFile, lines.ToArray());
            }
        }

    }
}
