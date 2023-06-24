using HarmonyLib;
using Il2CppSystem.Collections.Generic;

namespace MoreLanguages
{
    [HarmonyPatch(typeof (ResourceBundle))]
    [HarmonyPatch("LoadFromText")]
    internal static class Patch_ResourceBundle_LoadFromText
    {
        [HarmonyPriority(Priority.First)]
        private static void Postfix(string path, Dictionary<string, string> __result, string text)
        {
            LanguageController.ResetTranslations(GameContext.Instance.MessageDirector);

            foreach (var keyValuePair in LanguageController.TRANSLATIONS[path])
            {
                if (__result.ContainsKey(keyValuePair.Key))
                    __result[keyValuePair.Key] = keyValuePair.Value;
                else
                    __result.Add(keyValuePair.Key, keyValuePair.Value);
            }
            
        }
    }
    
    
}