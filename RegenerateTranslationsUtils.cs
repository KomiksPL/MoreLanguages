using System.Collections.Generic;
using System.IO;
using MelonLoader;
using Newtonsoft.Json;
using UnityEngine.Localization.Tables;

namespace MoreLanguages
{
    internal static class RegenerateTranslationsUtils
    {
        internal static bool ifRegenerate = false;
        private static List<string> catchedLists = new List<string>();

        private static System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>> translations = new();
        internal static void AddItToList(StringTable table)
        {
            if (catchedLists.Contains(table.name))
                return;
            catchedLists.Add(table.name);
            foreach (var valueMTableEntry in table.m_TableEntries)
            {
                if (string.IsNullOrEmpty(valueMTableEntry.Value.Key)) continue;
                var replace = table.SharedData.name.Replace(" Shared Data", string.Empty);
                if (!translations.ContainsKey(replace))
                {
                    translations.Add(replace, new Dictionary<string, string>());
                } 
                translations[replace].Add($"{valueMTableEntry.Value.Key}", valueMTableEntry.Value.Value);
            }
        }

        internal static void ConvertDirectoryIntoFiles()
        {
            foreach (var VARIABLE in translations)
            {
                File.WriteAllText(@"E:\SteamLibrary\steamapps\common\Slime Rancher 2\MoreLanguages\en\" + VARIABLE.Key + ".json",JsonConvert.SerializeObject(translations[VARIABLE.Key], Formatting.Indented) );
            }
            
        }

    }
}
