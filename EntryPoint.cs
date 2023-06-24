using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime;
using Il2CppMonomiPark.SlimeRancher.UI.Localization;
using Il2CppTMPro;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.SmartFormat.Core.Extensions;
using UnityEngine.Localization.SmartFormat.Core.Formatting;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;
using Assembly = System.Reflection.Assembly;
using File = System.IO.File;
using IEnumerator = System.Collections.IEnumerator;
using Locale = UnityEngine.Localization.Locale;
using MethodInfo = System.Reflection.MethodInfo;
using Object = Il2CppSystem.Object;

namespace MoreLanguages
{
    [HarmonyPatch]
    internal static class Il2cppDetourMethodPatcherReportExceptionPatch
    {
        public static MethodInfo TargetMethod() => AccessTools.Method(((IEnumerable<Type>) AccessTools.AllAssemblies().FirstOrDefault<Assembly>((Func<Assembly, bool>) (x => x.GetName().Name.Equals("Il2CppInterop.HarmonySupport"))).GetTypes()).FirstOrDefault<Type>((Func<Type, bool>) (x => x.Name == "Il2CppDetourMethodPatcher")), "ReportException");


        public static bool Prefix(System.Exception ex)
        {
            MelonLogger.Error("During invoking native->managed trampoline", ex);
            return false;
        }
    }

    [HarmonyPatch(typeof(TextMeshProUGUI), nameof(TextMeshProUGUI.Awake))]
    internal static class TextMeshProUGUIAwakePatch
    {
        public static void Prefix(TextMeshProUGUI __instance)
        {
            if (__instance.font.name.Contains("- HemispheresCaps2 SDF"))
                LanguageController.InstallHemispheres(__instance);
            if (__instance.font.name.Contains("humanst_ SDF")) 
                LanguageController.InstallHumanstSDF(__instance);
        }
    }
  
    

    [HarmonyPatch(typeof(LocalizationDirector), nameof(LocalizationDirector.Awake))]
    internal static class LocalizationDirectorAwakePatch
    {

        public static void Postfix(LocalizationDirector __instance)
        {
            LanguageController.addedLocales.ForEach(x =>
            {
                x.CustomFormatterCode = "";
                x.SortOrder = 0;
                __instance.Locales.Add(x);
            });

        }
    }

    [HarmonyPatch(typeof(LocalizedString), nameof(LocalizedString.RefreshString))]
    internal static class LocalizedStringRefreshString
    {
        internal static LocalizedString Instance;

        public static void Prefix(LocalizedString __instance)
        {
            Instance = __instance;
        }
    }

    [HarmonyPatch(typeof(LocalizedString), nameof(LocalizedString.GetSourceValue))]
    internal static class LocalizedStringGetSourceValue
    {
        public static bool Prefix(LocalizedString __instance, ISelectorInfo selector, ref object __result)
        {
            if (__instance.IsEmpty)
            {
                __result = string.Empty;
                return true;
            }
            Locale locale = __instance.LocaleOverride;
            if (locale == null && selector.FormatDetails.FormatCache != null) 
                locale = LocalizationSettings.AvailableLocales.GetLocale(selector.FormatDetails.FormatCache.Table.LocaleIdentifier);
            if (locale == null && LocalizationSettings.SelectedLocaleAsync.IsDone) 
                locale = LocalizationSettings.SelectedLocaleAsync.Result;
            if (locale == null)
            {
                __result = "<No Available Locale>";
                return true;

            }
            if (LanguageController.addedLocales.FirstOrDefault(x => x.Identifier.Code == locale.Identifier.Code) == null)
                return true;
            var stringTable = LanguageController.cachedStringTables.Find(x => x.TableCollectionName == __instance.TableReference.TableCollectionName);

            StringTableEntry stringTableEntry = stringTable.GetEntry(__instance.TableEntryReference.KeyId) ??
                                                stringTable.GetEntry(__instance.TableEntryReference.Key);

            LocalizedStringDatabase stringDatabase = LocalizationSettings.StringDatabase;
            TableReference tableReference = __instance.TableReference;
            TableEntryReference tableEntryReference = __instance.TableEntryReference;
            if (!stringTableEntry.IsSmart)
            {
                __result = Il2CppSystem.Activator.CreateInstance(Il2CppType.Of<LocalizedString.StringTableEntryVariable>(), stringDatabase.GenerateLocalizedString(stringTable, stringTableEntry, tableReference, tableEntryReference, locale,   __instance.Arguments), stringTableEntry);
                return false;
            }
            FormatCache formatCache = stringTableEntry?.GetOrCreateFormatCache();
            if (formatCache != null)
            {
                formatCache.VariableTriggers.Clear();
                formatCache.LocalVariables = __instance.m_VariableLookup.Count <= 0 ? selector.FormatDetails.FormatCache.LocalVariables : new LocalizedString.ChainedLocalVariablesGroup(__instance.Cast<IVariableGroup>(), selector.FormatDetails.FormatCache.LocalVariables).Cast<IVariableGroup>();
            }

            Il2CppSystem.Collections.Generic.List<Object> objectList = new Il2CppSystem.Collections.Generic.List<Object>();
            if (selector.CurrentValue != null)
                objectList.Add(selector.CurrentValue);
            if (__instance.Arguments != null)
                objectList.AddRange(__instance.Arguments.Cast<Il2CppSystem.Collections.Generic.IEnumerable<Object>>());
            string localizedString = stringDatabase.GenerateLocalizedString(stringTable, stringTableEntry,
                tableReference, tableEntryReference, locale, objectList.Cast<Il2CppSystem.Collections.Generic.IList<Object>>());
            if (formatCache != null)
            {
                formatCache.LocalVariables = null;
                __instance.UpdateVariableListeners(formatCache.VariableTriggers);
            }

            __result = Il2CppSystem.Activator.CreateInstance(Il2CppType.Of<LocalizedString.StringTableEntryVariable>(), localizedString, stringTableEntry);


            return false;
        }

    }
    
    
    [HarmonyPatch(typeof(LocalizationDirector), nameof(LocalizationDirector.ReloadTables))]
    [HarmonyPriority(Priority.First)]
    internal static class LocalizationDirectorSetLocale
    {
        public static bool Prefix(LocalizationDirector __instance)
        {
            var identifierCode = LocalizationSettings.SelectedLocale.Identifier.Code;
            if (LanguageController.addedLocales.FirstOrDefault(x => x.Identifier.Code == identifierCode) == null) 
                return true;
            foreach (var cachedStringTable in LanguageController.cachedStringTables)
                UnityEngine.Object.Destroy(cachedStringTable);
            
            LanguageController.cachedStringTables.Clear();
            var directoryInfo = new DirectoryInfo(Path.Combine(MelonEnvironment.MelonBaseDirectory, "MoreLanguages", identifierCode));
            if (!directoryInfo.Exists)
            {
                MelonLogger.Msg($"This Language named {LocalizationSettings.SelectedLocale.name} doesn't have any folder related to language. LangCode: {identifierCode}");
                return false;
            }
            var instanceTables = __instance.Tables;
            if (!instanceTables.ContainsKey("BootUp"))
                instanceTables.Add("BootUp", null);
            if (!instanceTables.ContainsKey("Options"))
                instanceTables.Add("Options", null);
            foreach (var stringKeys in instanceTables)
            {
                var combine = new FileInfo(Path.Combine(directoryInfo.FullName, stringKeys.Key + ".json"));
                if (!combine.Exists)
                {
                    MelonLogger.Msg($"This Language named {LocalizationSettings.SelectedLocale.name} doesn't have bundle named {stringKeys.Key}. LangCode: {identifierCode}");
                    continue;
                }
                var firstOrDefault = EntryPoint.copyTables.FirstOrDefault(x => x.name.Contains(stringKeys.Key));
                var stringTable = UnityEngine.Object.Instantiate(firstOrDefault);
                if (firstOrDefault != null)
                    stringTable.name = firstOrDefault.name.Replace("_en(Clone)", "_" + identifierCode);
                stringTable.LocaleIdentifier = LocalizationSettings.SelectedLocale.Identifier;
                stringTable.hideFlags |= HideFlags.HideAndDontSave;
                var deserializeObject = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(combine.FullName));
                foreach (var stringTableMTableEntry in stringTable.m_TableEntries)
                {
                    if (string.IsNullOrEmpty(stringTableMTableEntry.Value.Key)) continue;
                    if (deserializeObject.TryGetValue(stringTableMTableEntry.Value.Key, out var value))
                        stringTableMTableEntry.Value.Value = value;
                }
                LanguageController.cachedStringTables.Add(stringTable);
            }
            foreach (var stringTable in LanguageController.cachedStringTables)
                __instance.Tables[stringTable.SharedData.TableCollectionName] = stringTable;
            __instance.Tables.Remove("BootUp");
            __instance.Tables.Remove("Options");

            return false;
        }
    }
    [HarmonyPatch(typeof(LocalizedStringDatabase), nameof(LocalizedStringDatabase.GenerateLocalizedString))] 
    internal static class LocalizedStringDatabaseGenerateLocalizedString
    {
        public static void Prefix(ref StringTable table, ref StringTableEntry entry, TableReference tableReference, TableEntryReference tableEntryReference, Locale locale, Il2CppSystem.Collections.Generic.IList<Object> arguments)
        {
            if (table != null) return;
            if (entry != null) return;
            var (stringTable, stringTableEntry) = LanguageController.ResetTranslations(tableReference, tableEntryReference, locale);
            if (stringTableEntry == null || stringTable == null) return;
            table = stringTable;
            entry = stringTableEntry;
            if (entry.m_FormatCache != null) return;
            entry.m_FormatCache = entry.GetOrCreateFormatCache() ?? new FormatCache();
            entry.m_FormatCache.LocalVariables = LocalizedStringRefreshString.Instance.TryCast<IVariableGroup>();
            entry.m_FormatCache.VariableTriggers.Clear();
            LocalizedStringRefreshString.Instance = null;
        }
    }

    [HarmonyPatch]
    public static class StringTableAddEntryPatch
    {
        public static MethodBase TargetMethod()
        {
            return typeof(DetailedLocalizationTable<>).MakeGenericType(typeof(StringTableEntry)).GetMethod("AddEntry", new []
            {
                 typeof(string), typeof(string)
            });
        }
        public static void Postfix(DetailedLocalizationTable<StringTableEntry> __instance, string key, string localized)
        {
            var keyId = __instance.FindKeyId(key, false);
            if (keyId == 0) return;
            if (!LanguageController.moddedTranslations.TryGetValue(__instance.TableCollectionName, out var value))
            {
                var dictionary = new Dictionary<long, string>();
                value = dictionary;
                LanguageController.moddedTranslations.TryAdd(__instance.TableCollectionName, dictionary);
            }
            value.TryAdd(keyId, localized);
        }
    }
    
    
    
   
    
    
    
    internal class EntryPoint : MelonMod
    {

        public static List<StringTable> copyTables = new List<StringTable>();
        public static IEnumerator GetAllTables(Locale locale)
        {
            AsyncOperationHandle<Il2CppSystem.Collections.Generic.IList<StringTable>> asyncOperationHandle = LocalizationSettings.StringDatabase.GetAllTables(locale);
            yield return asyncOperationHandle;

            Il2CppSystem.Collections.Generic.List<StringTable> list = new(asyncOperationHandle.Result.Cast<Il2CppSystem.Collections.Generic.IEnumerable<StringTable>>());
                
            foreach (var stringTable in list)
            {
                if (RegenerateTranslationsUtils.ifRegenerate)
                {
                    RegenerateTranslationsUtils.AddItToList(stringTable);
                }

                var instantiate = UnityEngine.Object.Instantiate(stringTable);
                instantiate.hideFlags |= HideFlags.HideAndDontSave;
               instantiate.SharedData = UnityEngine.Object.Instantiate(instantiate.SharedData); 

                copyTables.Add(instantiate);
                
            }
            if (RegenerateTranslationsUtils.ifRegenerate)
            {
                RegenerateTranslationsUtils.ConvertDirectoryIntoFiles();
            }
        }
        
      
        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
          
            switch (sceneName)
            {
                case "SystemCore":
                {
                    var english = SRSingleton<SystemContext>.Instance.LocalizationDirector.Locales.ToArray().FirstOrDefault(x => x.Identifier.Code.Equals("en"));
                    MelonCoroutines.Start(GetAllTables(english));
                    break;
                    
                }
                case "MainMenuEnvironment":
                {
                    break;
                }
            }
            
        }
        
        public override void OnInitializeMelon()
        {
            LanguageController.Setup();
            LanguageController.InstallLocale(UnityEngine.Localization.Locale.CreateLocale(SystemLanguage.Polish));
        }
        

    }
}

    

