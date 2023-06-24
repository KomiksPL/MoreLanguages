using System.Collections.Generic;
using System.IO;
using System.Linq;
using Il2CppTMPro;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace MoreLanguages
{
	public static class LanguageController
	{
		private static TMP_FontAsset RusselType;
		private static TMP_FontAsset Nunito;
		private static TMP_FontAsset HumanSans;
		private static TMP_FontAsset NunitoLight;
		internal static List<UnityEngine.Localization.Locale> addedLocales = new List<UnityEngine.Localization.Locale>();
		public static Dictionary<string, Dictionary<long, string>> moddedTranslations = new Dictionary<string, Dictionary<long, string>>();
		internal static List<StringTable> cachedStringTables = new List<StringTable>();
		internal static void Setup()
		{
			var manifestResourceStream = Melon<EntryPoint>.Instance.MelonAssembly.Assembly.GetManifestResourceStream("MoreLanguages.morelanguages");
			if (manifestResourceStream != null)
			{
				byte[] ba = new byte[manifestResourceStream.Length];
				_  = manifestResourceStream.Read(ba);
				var assetBundle = AssetBundle.LoadFromMemory(ba);
				RusselType = TMP_FontAsset.CreateFontAsset(assetBundle.LoadAsset("KatahdinRound").Cast<Font>());
				RusselType.hideFlags |= HideFlags.HideAndDontSave;
				RusselType.name = "KatahdinRound";
				Nunito = TMP_FontAsset.CreateFontAsset(assetBundle.LoadAsset("Nunito-Bold").Cast<Font>());
				Nunito.hideFlags |= HideFlags.HideAndDontSave;
				Nunito.name = "Nunito-Bold";
				HumanSans = TMP_FontAsset.CreateFontAsset(assetBundle.LoadAsset("HumanSans-Regular").Cast<Font>());
				HumanSans.hideFlags |= HideFlags.HideAndDontSave;
				HumanSans.name = "HumanSans-Regular";

				NunitoLight = TMP_FontAsset.CreateFontAsset(assetBundle.LoadAsset("NunitoSans-Light").Cast<Font>());
			}

			NunitoLight.hideFlags |= HideFlags.HideAndDontSave;
			NunitoLight.name = "NunitoSans-Light";

		}
		internal static void InstallHemispheres(TextMeshProUGUI textMeshPro)
		{
			if (RusselType == null) return;
			/*
			TMP_FontAsset cyrillicFontToDelete = null;
			foreach (var VARIABLE in textMeshPro.font.m_FallbackFontAssetTable)
			{
				if (VARIABLE.name.Contains("Cyrillic"))
					cyrillicFontToDelete = VARIABLE;

			}
			textMeshPro.font.m_FallbackFontAssetTable.Remove(cyrillicFontToDelete);
			List<TMP_FontAsset> toDelete = new List<TMP_FontAsset>();
			foreach (var fontAsset in textMeshPro.font.m_FallbackFontAssetTable)
			{
				if (fontAsset.HasCharacter('У'))
				{
					toDelete.Add(fontAsset);
				}
			}

			foreach (var VARIABLE in toDelete)
			{
				textMeshPro.font.m_FallbackFontAssetTable.Remove(VARIABLE);
			}
			*/
			textMeshPro.font.m_FallbackFontAssetTable.Add(RusselType);
			//textMeshPro.font.m_FallbackFontAssetTable.Add(Nunito);
			//MelonLogger.Msg($"Installed Successful: {RusselType.name}, {Nunito.name}");
			RusselType = null;

		}
		internal static void InstallHumanstSDF(TextMeshProUGUI textMeshPro)
		{
			if (HumanSans == null) return;
			textMeshPro.font.m_FallbackFontAssetTable.Add(HumanSans);
			//textMeshPro.font.m_FallbackFontAssetTable.Add(NunitoLight);
			//MelonLogger.Msg($"Installed Successful: {HumanSans.name}, {NunitoLight.name}");
			HumanSans = null;
		}
		
		public static void InstallLocale(UnityEngine.Localization.Locale locale)
		{
			locale.name = locale.Identifier.ToString();
			locale.hideFlags |= HideFlags.HideAndDontSave;
			
			addedLocales.Add(locale);
		}

		private static StringTableEntry GetEntryOrAddEntry(this StringTable stringTable, long keyId, string key)
		{
			StringTableEntry stringTableEntry = null;
			try
			{
				if (keyId != 0)
				{
					stringTableEntry = stringTable.GetEntry(keyId);
				}

				else if (!string.IsNullOrWhiteSpace(key))
				{
					stringTableEntry = stringTable.GetEntry(key);
				}
			}
			catch
			{
				// ignored
			}
			if (stringTableEntry != null)
			{
				return stringTableEntry;
			}
			if (moddedTranslations.TryGetValue(stringTable.TableCollectionName, out var moddedTranslation))
			{
				if (moddedTranslation.TryGetValue(keyId, out var value))
					return stringTable.AddEntry(keyId, value);
			}
			return null;
		}

		internal static (StringTable, StringTableEntry) ResetTranslations(TableReference tableReference, TableEntryReference tableEntryReference, UnityEngine.Localization.Locale locale)
		{
			var identifierCode = locale.Identifier.Code;
			var moreLanguagesPath = Path.Combine(MelonEnvironment.MelonBaseDirectory, "MoreLanguages", identifierCode);
			if (!Directory.Exists(moreLanguagesPath))
			{
				MelonLogger.Msg($"The language '{locale.name}' doesn't have a related folder for the language. LangCode: {identifierCode}");
				return default;
			}
			StringTable cachedStringTable = cachedStringTables.FirstOrDefault(x => x.name.Contains(tableReference.TableCollectionName) && x.LocaleIdentifier == locale.Identifier);
			if (cachedStringTable == null)
				return default;
			var stringTableEntry = cachedStringTable.GetEntryOrAddEntry(tableEntryReference.KeyId, tableEntryReference.Key);
			return (cachedStringTable, stringTableEntry);
		}
	}
}