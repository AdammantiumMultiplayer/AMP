#if AMP
using AMP.Overlay;
using ThunderRoad;
#if STEAM
using Steamworks;
using Netamite.Steam.Server;
using Netamite.Steam.Integration;
using SteamClient = Netamite.Steam.Client.SteamClient;
#endif
using AMP.Data;
using AMP.Extension;
#endif
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Linq;
using AMP.SupportFunctions;

using AMP.Logging;
using ThunderRoadVRKBSharedData;
using AMP.Network;
using AMP.Threading;
using AMP.Network.Data.Sync;
using AMP.Resources;
using AMP.GameInteraction;
using AMP.Network.Packets.Implementation;

namespace AMP.UI {
	// Mod Compatibility Checks
	public partial class IngameModUI {
		RectTransform modCompatibilityPanel;
		ScrollRect modList;
		RectTransform modInfo;
		RectTransform modInfoContent;

		private ModInfo[] loadedMods;

		private static ModInfo currentModInfo;
		private static Button currentModButton;
		private static readonly Color modRowColor = Color.clear;
		private static readonly Color modRowSelectedColor = new Color(0.85f, 0.56f, 0.2f, 0.35f);

		private void CreateModCompatibiltyPanel() {
			GameObject gobj = CreateObject("ModCompatibilityPanel");
			gobj.transform.SetParent(transform);
			modCompatibilityPanel = gobj.AddComponent<RectTransform>();
			modCompatibilityPanel.anchorMin = Vector2.zero;
			modCompatibilityPanel.anchorMax = Vector2.one;
			modCompatibilityPanel.sizeDelta = Vector2.zero;
			modCompatibilityPanel.localPosition = Vector3.zero;

			modList = CreateScrollRect("Modlist", false);
			modList.transform.SetParent(modCompatibilityPanel, false);
			RectTransform rect = modList.gameObject.GetComponent<RectTransform>();
			rect.anchorMin = Vector2.zero;
			rect.anchorMax = new Vector2(0.5f, 1f);
			rect.offsetMin = new Vector2(32, 24);
			rect.offsetMax = new Vector2(-12, -100);

			VerticalLayoutGroup listLayout = modList.content.GetComponent<VerticalLayoutGroup>();
			listLayout.padding = new RectOffset(8, 8, 8, 8);
			listLayout.spacing = 6;
			listLayout.childForceExpandWidth = true;

			gobj = CreateObject("Modinfo");
			gobj.transform.SetParent(modCompatibilityPanel, false);
			modInfo = gobj.AddComponent<RectTransform>();
			modInfo.anchorMin = new Vector2(0.5f, 0f);
			modInfo.anchorMax = Vector2.one;
			modInfo.offsetMin = new Vector2(0, 24);
			modInfo.offsetMax = new Vector2(-32, -100);

			GameObject contentObject = new GameObject("ModInfoContent", typeof(RectTransform));
			contentObject.transform.SetParent(modInfo, false);
			modInfoContent = contentObject.GetComponent<RectTransform>();
			modInfoContent.anchorMin = Vector2.zero;
			modInfoContent.anchorMax = Vector2.one;
			modInfoContent.offsetMin = new Vector2(8, 20);
			modInfoContent.offsetMax = new Vector2(-22, -20);
		}

		private class ModCompatVotes {
			public enum CompatibilityLevel {
				NotChecked = -2,
				Unknown = -1,
				WillBreakMod = 0,
				NotSynced = 1,
				PartiallySynced = 2,
				FullySynced = 3
			}

			public bool hasData = false;
			public int vote_willbreakmod = 0;
			public int vote_notsynced = 0;
			public int vote_partiallysynced = 0;
			public int vote_fullysynced = 0;
			public bool votable = true;

			public ModCompatVotes() { }

			public CompatibilityLevel CalculateCompatLevel() {
				if(hasData) {
					int[] votes = { vote_willbreakmod, vote_notsynced, vote_partiallysynced, vote_fullysynced };
					int maxVotes = votes.Max();
					if(maxVotes == 0) return CompatibilityLevel.Unknown;

					return (CompatibilityLevel)Array.IndexOf(votes, maxVotes);
				}
				return CompatibilityLevel.NotChecked;
			}

			public String GetCompatibilityString() {
				switch(CalculateCompatLevel()) {
					case CompatibilityLevel.NotChecked:
						return "Not Checked";
					case CompatibilityLevel.Unknown:
						return "No votes yet";
					case CompatibilityLevel.WillBreakMod:
						return "Will Break Multiplayer";
					case CompatibilityLevel.NotSynced:
						return "Not Synced";
					case CompatibilityLevel.PartiallySynced:
						return "Works Mostly";
					case CompatibilityLevel.FullySynced:
						return "Works";
					default:
						return "Unknown";
				}
			}

			public Color GetCompatibilityColor() {
				switch(CalculateCompatLevel()) {
					case CompatibilityLevel.NotChecked:
						return new Color(0.35f, 0.35f, 0.35f, 1f);
					case CompatibilityLevel.Unknown:
						return new Color(0.42f, 0.42f, 0.42f, 1f);
					case CompatibilityLevel.WillBreakMod:
						return new Color(0.68f, 0.12f, 0.12f, 1f);
					case CompatibilityLevel.NotSynced:
						return new Color(0.68f, 0.3f, 0.05f, 1f);
					case CompatibilityLevel.PartiallySynced:
						return new Color(0.12f, 0.24f, 0.58f, 1f);
					case CompatibilityLevel.FullySynced:
						return new Color(0f, 0.28f, 0f, 1f);
					default:
						return new Color(0.3f, 0.3f, 0.3f, 1f);
				}
			}
		}

		private class ModInfo {
			public string Name = "";
			public string ModVersion = "";
			public string Author = "";
			public string Description = "";

			public ModCompatVotes compatibilityVotes = new ModCompatVotes();

			public ModInfo(string Name, string ModVersion, string Author, string Description) {
				this.Name = Name;
				this.ModVersion = ModVersion;
				this.Author = Author;
				this.Description = Description;
			}

			public GameObject GetPrefab() {
				GameObject gobj = new GameObject(Name);
				RectTransform rect = gobj.AddComponent<RectTransform>();
				rect.anchorMin = new Vector2(0, 1);
				rect.anchorMax = Vector2.one;
				rect.pivot = new Vector2(0.5f, 1);
				rect.sizeDelta = new Vector2(0, 64);

				Image background = gobj.AddComponent<Image>();
				background.color = modRowColor;
				Button btn = gobj.AddComponent<Button>();
				btn.targetGraphic = background;
				btn.transition = Selectable.Transition.None;
				btn.onClick.AddListener(() => currentUI.SelectMod(this, btn));

				CreateModText(rect, "Name", Name, 0, 64, 18, 16, TextAlignmentOptions.MidlineLeft, FontStyles.Bold, 0, 0.62f, 16, -4);
				TextMeshProUGUI text = CreateModText(rect, "Compatibility", compatibilityVotes.GetCompatibilityString(), 0, 64, 16, 12, TextAlignmentOptions.MidlineRight, FontStyles.Bold, 0.62f, 1, 4, -14);
				text.color = compatibilityVotes.GetCompatibilityColor();
				return gobj;
			}
		}

		private void LoadMods() {
			// Get loaded mods and sort them alphabetically by name
			loadedMods = ThunderRoad.ModManager.loadedMods.OrderBy(m => m.Name).Select(m => new ModInfo(m.Name, m.ModVersion, m.Author, m.Description)).ToArray();
			
			StartCoroutine(CheckModsCompatibility());
		}

		private void BuildModList() {
			if(loadedMods == null) LoadMods();

			foreach(Transform c in modList.content) Destroy(c.gameObject);
			currentModInfo = null;
			currentModButton = null;

			foreach(ModInfo mod in loadedMods) {
				GameObject obj = mod.GetPrefab();
				obj.transform.SetParent(modList.content, false);
			}
			if(loadedMods.Length == 0) {
				CreateModText(modList.content, "NoModsMessage", "No mods are currently loaded.", 0, 64, 18, 12, TextAlignmentOptions.Center, FontStyles.Normal);
			}
			UpdateModInfo();
			modList.verticalNormalizedPosition = 1f;
			FixSize();
		}

		private void SelectMod(ModInfo mod, Button button) {
			if(currentModButton != null) currentModButton.targetGraphic.color = modRowColor;
			currentModInfo = mod;
			currentModButton = button;
			currentModButton.targetGraphic.color = modRowSelectedColor;
			UpdateModInfo();
		}

		private void UpdateModInfo() {
			foreach(Transform c in modInfoContent) Destroy(c.gameObject);

			if(currentModInfo != null) {
				CreateModText(modInfoContent, "ModName", currentModInfo.Name, 0, 42, 28, 22, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
				CreateModText(modInfoContent, "ModVersion", "Version  " + currentModInfo.ModVersion, 54, 28, 18, 16, TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
				CreateModText(modInfoContent, "ModAuthor", "Author  " + currentModInfo.Author, 88, 28, 18, 16, TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
				CreateModDescription(currentModInfo.Description, currentModInfo.compatibilityVotes.votable);

				if(currentModInfo.compatibilityVotes.votable) {
					CreateVoteHeading();
					ModCompatVotes.CompatibilityLevel[] levels = {
						ModCompatVotes.CompatibilityLevel.WillBreakMod,
						ModCompatVotes.CompatibilityLevel.NotSynced,
						ModCompatVotes.CompatibilityLevel.PartiallySynced,
						ModCompatVotes.CompatibilityLevel.FullySynced
					};
					string[] labels = { "It Breaks MP", "Not Synced", "Works mostly", "Works fully" };
					for(int i = 0; i < levels.Length; i++) {
						ModCompatVotes.CompatibilityLevel level = levels[i];
						int column = i % 2;
						int row = i / 2;
						float bottom = 12 + (1 - row) * 54;
						Button btn = CreateButton(labels[i], Color.black, new Vector2(220, 44));
						btn.transform.SetParent(modInfoContent, false);
						TextMeshProUGUI buttonText = btn.GetComponentInChildren<TextMeshProUGUI>();
						buttonText.color = Color.black;
						buttonText.fontSize = 18;
						buttonText.enableAutoSizing = true;
						buttonText.fontSizeMin = 14;
						buttonText.fontSizeMax = 20;
						buttonText.textWrappingMode = TextWrappingModes.NoWrap;
						RectTransform rect = btn.GetComponent<RectTransform>();
						rect.anchorMin = new Vector2(column * 0.5f, 0);
						rect.anchorMax = new Vector2((column + 1) * 0.5f, 0);
						rect.pivot = new Vector2(0.5f, 0);
						rect.sizeDelta = Vector2.zero;
						rect.offsetMin = new Vector2(column == 0 ? 8 : 6, bottom);
						rect.offsetMax = new Vector2(column == 0 ? -6 : -8, bottom + 44);

						btn.onClick.AddListener(() => StartCoroutine(SubmitModCompatibilityVote(currentModInfo, level)));
					}
				}
			} else {
				CreateModText(modInfoContent, "ModInfoPlaceholder", "Select a mod to view its details.", 0, 64, 18, 14, TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
			}
		}

		private void CreateVoteHeading() {
			GameObject gobj = new GameObject("VoteHeading", typeof(RectTransform));
			gobj.transform.SetParent(modInfoContent, false);
			RectTransform rect = gobj.GetComponent<RectTransform>();
			rect.anchorMin = new Vector2(0, 0);
			rect.anchorMax = new Vector2(1, 0);
			rect.pivot = new Vector2(0.5f, 0);
			rect.offsetMin = new Vector2(8, 118);
			rect.offsetMax = new Vector2(-8, 146);

			TextMeshProUGUI text = gobj.AddComponent<TextMeshProUGUI>();
			text.text = "Vote:";
			text.color = Color.black;
			text.fontSize = 18;
			text.fontStyle = FontStyles.Bold;
			text.alignment = TextAlignmentOptions.MidlineLeft;
			text.textWrappingMode = TextWrappingModes.NoWrap;
		}

		private static TextMeshProUGUI CreateModText(Transform parent, string objectName, string text, float top, float height, float fontSize, float minFontSize, TextAlignmentOptions alignment, FontStyles style, float anchorMinX = 0, float anchorMaxX = 1, float leftInset = 0, float rightInset = 0) {
			GameObject gobj = new GameObject(objectName, typeof(RectTransform));
			gobj.transform.SetParent(parent, false);
			RectTransform rect = gobj.GetComponent<RectTransform>();
			rect.anchorMin = new Vector2(anchorMinX, 1);
			rect.anchorMax = new Vector2(anchorMaxX, 1);
			rect.pivot = new Vector2(0.5f, 1);
			rect.offsetMin = new Vector2(leftInset, -(top + height));
			rect.offsetMax = new Vector2(rightInset, -top);

			TextMeshProUGUI tmp = gobj.AddComponent<TextMeshProUGUI>();
			tmp.text = text;
			tmp.alignment = alignment;
			tmp.fontStyle = style;
			tmp.color = Color.black;
			tmp.enableAutoSizing = true;
			tmp.fontSizeMin = minFontSize;
			tmp.fontSizeMax = fontSize;
			tmp.textWrappingMode = TextWrappingModes.NoWrap;
			return tmp;
		}

		private void CreateModDescription(string description, bool reserveVoteSpace) {
			GameObject gobj = new GameObject("ModDescription", typeof(RectTransform));
			gobj.transform.SetParent(modInfoContent, false);
			RectTransform rect = gobj.GetComponent<RectTransform>();
			rect.anchorMin = Vector2.zero;
			rect.anchorMax = Vector2.one;
			rect.offsetMin = new Vector2(0, reserveVoteSpace ? 150 : 0);
			rect.offsetMax = new Vector2(0, -134);

			TextMeshProUGUI tmp = gobj.AddComponent<TextMeshProUGUI>();
			tmp.text = description;
			tmp.fontSize = 17;
			tmp.color = Color.black;
			tmp.alignment = TextAlignmentOptions.TopLeft;
			tmp.textWrappingMode = TextWrappingModes.Normal;
		}

		public IEnumerator CheckModsCompatibility() {
			foreach(ModInfo mod in loadedMods) {
				yield return CheckModCompatibility(mod);
			}
		}

		private IEnumerator CheckModCompatibility(ModInfo mod) {
			if(mod.compatibilityVotes.CalculateCompatLevel() != ModCompatVotes.CompatibilityLevel.NotChecked) yield break;

			Log.Debug(Defines.AMP, $"Checking Mod Compat {ModManager.safeFile.hostingSettings.masterServerUrl}/api/modcompatcheck.php?name={mod.Name}");
			using (UnityWebRequest webRequest = UnityWebRequest.Get($"https://{ModManager.safeFile.hostingSettings.masterServerUrl}/api/modcompatcheck.php?name={mod.Name}")) {
				yield return webRequest.SendWebRequest();

				if(webRequest.result == UnityWebRequest.Result.Success) {
					try {
						ModCompatVotes votes = JsonConvert.DeserializeObject<ModCompatVotes>(webRequest.downloadHandler.text);
						votes.hasData = true;
						mod.compatibilityVotes = votes;
					}
					catch(Exception e) {
						Log.Err($"Failed to parse compatibility level for mod {mod.Name}: {e.Message}");
					}
				}
				else {
					Log.Err($"Failed to check compatibility for mod {mod.Name}: {webRequest.error}");
				}
				BuildModList();
			};
		}

		private IEnumerator SubmitModCompatibilityVote(ModInfo mod, ModCompatVotes.CompatibilityLevel vote) {
			if(!mod.compatibilityVotes.votable) yield break;

			string vote_text;
			switch(vote) {
				case ModCompatVotes.CompatibilityLevel.WillBreakMod:
					vote_text = "willbreakmod";
					break;
				case ModCompatVotes.CompatibilityLevel.NotSynced:
					vote_text = "notsynced";
					break;
				case ModCompatVotes.CompatibilityLevel.PartiallySynced:
					vote_text = "partiallysynced";
					break;
				case ModCompatVotes.CompatibilityLevel.FullySynced:
					vote_text = "fullysynced";
					break;
				default:
					Log.Err($"Invalid vote value for mod {mod.Name}: {vote}");
					yield break;
			}

			Log.Debug(Defines.AMP, $"Submitting Mod Compat Vote {ModManager.safeFile.hostingSettings.masterServerUrl}/api/modcompatvote.php?name={mod.Name}&username={ModManager.safeFile.username}&vote_type={vote_text}");
			using(UnityWebRequest webRequest = UnityWebRequest.Get($"https://{ModManager.safeFile.hostingSettings.masterServerUrl}/api/modcompatvote.php?name={mod.Name}&username={ModManager.safeFile.username}&vote_type={vote_text}")) {
				yield return webRequest.SendWebRequest();
				if(webRequest.result == UnityWebRequest.Result.Success) {
					mod.compatibilityVotes.votable = false;
				} else {
					Log.Err($"Failed to submit compatibility vote for mod {mod.Name}: {webRequest.error}");
				}
				BuildModList();
			}
		}
	}
}
