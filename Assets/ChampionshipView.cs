using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using RVP;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

[Serializable]
public class ChampionshipSave
{
	public int version = 1;
	public string driverName;
	public string season;
	public int cash = 25000;
	public int carIndex;
	public Livery sponsor;
	public string[] tracks = Array.Empty<string>();
	public int nextRace;
	public List<string> wonSeasons = new();
	public List<ChampionshipStanding> standings = new();
	public Dictionary<int, CarConfig> cars = new();
	public bool Completed => tracks.Length > 0 && nextRace >= tracks.Length;
}
[Serializable]
public class ChampionshipStanding
{
	public Livery sponsor;
	public int carIndex;
	public int points;
	public int wins;
	public int lastPosition = 6;
}

public static class Championships
{
	public static readonly string[] Countries = { "GER", "SPN", "JAP", "ENG", "FRA", "USA", "ITA" };
	public static readonly Livery[] Sponsors = { Livery.TGR, Livery.Rline, Livery.Itex, Livery.Caltex, Livery.Titan, Livery.Mysuko };
	public static ChampionshipSave Current { get; private set; }
	public static int Slot { get; private set; } = -1;
	public static bool Active => F.I && F.I.gameMode == GameMode.Championships && Current != null;
	public static bool RacePending { get; private set; }
	static bool savingEnabled;
	static bool finalRankingRecorded;
	public static int StartingCash = 25000;
	public static int[] Prizes = { 15000, 10000, 7500, 5000, 2500, 1000 };
	public static float TradeInFraction = 0.5f;
	static CarConfig[] previousConfigs;
	static int previousCar;
	static string previousName;
	static Livery previousSponsor;
	static string previousTrack;
	static RaceType previousRaceType;
	static ScoringType previousScoring;
	static PavementType previousRoadType;
	static int previousLaps, previousRivals;
	static bool previousTeams, previousSpectator, previousEditor, previousRandomCars, previousRandomTracks;
	static string DirectoryPath => Path.Combine(F.I.documentsSGPRpath, "championships");
	static string SlotPath(int slot) => Path.Combine(DirectoryPath, $"slot{slot + 1}.json");

	public static ChampionshipSave ReadSlot(int slot)
	{
		try
		{
			if (!File.Exists(SlotPath(slot))) return null;
			var save = JsonConvert.DeserializeObject<ChampionshipSave>(File.ReadAllText(SlotPath(slot)));
			if (save == null || save.version != 1 || string.IsNullOrWhiteSpace(save.driverName)) throw new InvalidDataException("Invalid championship save.");
			// Earlier versions wrote name-only drafts. These are still empty slots.
			return IsReady(save) ? save : null;
		}
		catch (Exception error) { Debug.LogError($"Championship slot {slot + 1}: {error.Message}"); return null; }
	}
	static bool IsReady(ChampionshipSave save) => save != null &&
		(Countries.Contains(save.season) || save.season == "SGP" || save.season == "SURVIVAL") &&
		save.tracks != null && save.tracks.Length > 0 && save.tracks.All(t => !string.IsNullOrWhiteSpace(t)) &&
		save.nextRace >= 0 && save.nextRace <= save.tracks.Length &&
		save.wonSeasons != null && save.cars != null &&
		save.carIndex >= 0 && save.carIndex < F.I.cars.Length &&
		save.cars.TryGetValue(save.carIndex, out var car) && car?.originalPhysics != null && car.originalParts != null &&
		save.cars.All(c => c.Value != null) && save.standings != null && save.standings.Count == 6 &&
		save.standings.All(s => s != null && Sponsors.Contains(s.sponsor) && s.carIndex >= 0 && s.carIndex < F.I.cars.Length) &&
		save.standings.Select(s => s.sponsor).Distinct().Count() == 6 && Sponsors.Contains(save.sponsor);

	// Called by ChampionshipsView only after a full season has been prepared.
	public static void EnterChampionshipView()
	{
		if (!Active || !IsReady(Current) || Current.Completed) return;
		savingEnabled = true;
		Save();
	}
	public static void OpenSlot(int slot)
	{
		if (slot < 0 || slot >= 8) return;
		if (previousConfigs == null)
		{
			previousConfigs = F.I.cars.Select(c => new CarConfig(c.config)).ToArray();
			previousCar = F.I.s_playerCarIdx;
			previousName = F.I.playerData.playerName;
			previousSponsor = F.I.s_PlayerCarSponsor;
			previousTrack = F.I.s_trackName; previousRaceType = F.I.s_raceType;
			previousScoring = F.I.scoringType; previousRoadType = F.I.s_roadType;
			previousLaps = F.I.s_laps; previousRivals = F.I.s_cpuRivals;
			previousTeams = F.I.teams; previousSpectator = F.I.s_spectator; previousEditor = F.I.s_inEditor;
			previousRandomCars = F.I.randomCars; previousRandomTracks = F.I.randomTracks;
		}
		for (int i = 0; i < previousConfigs.Length; i++) F.I.cars[i].config = new CarConfig(previousConfigs[i]);
		Slot = slot;
		Current = ReadSlot(slot);
		savingEnabled = Current != null;
		RacePending = false;
		finalRankingRecorded = false;
		F.I.gameMode = GameMode.Championships;
		if (Current != null)
		{
			foreach (string season in Current.wonSeasons) F.I.RememberChampionshipWin(season);
			Restore();
		}
	}
	public static void Create(string name)
	{
		Current = new ChampionshipSave { driverName = name.Trim(), cash = StartingCash };
		savingEnabled = false;
	}
	static void Restore()
	{
		F.I.playerData.playerName = Current.driverName;
		F.I.s_playerCarIdx = Current.carIndex;
		F.I.s_PlayerCarSponsor = Current.sponsor;
		foreach (var entry in Current.cars)
			if (entry.Key >= 0 && entry.Key < F.I.cars.Length)
			{
				var config = new CarConfig(entry.Value);
				// CarConfig.name is excluded from JSON; restore it from the car's identity.
				config.name = previousConfigs[entry.Key].name;
				F.I.cars[entry.Key].config = config;
			}
	}
	public static void Save()
	{
		if (!savingEnabled || Slot < 0 || !IsReady(Current)) return;
		Directory.CreateDirectory(DirectoryPath);
		string path = SlotPath(Slot), temp = path + ".tmp";
		File.WriteAllText(temp, JsonConvert.SerializeObject(Current, Formatting.Indented));
		if (File.Exists(path)) File.Replace(temp, path, null); else File.Move(temp, path);
	}
	public static void CompleteSeason()
	{
		if (!Active || !Current.Completed || !savingEnabled) return;
		foreach (string season in Current.wonSeasons) F.I.RememberChampionshipWin(season);
		if (Position == 1) F.I.RememberChampionshipWin(Current.season);
		// Keep the final standings in memory for the podium and ranking screens.
		// The completed run must never be written back into its freed slot.
		savingEnabled = false;
		File.Delete(SlotPath(Slot));
		File.Delete(SlotPath(Slot) + ".tmp");
	}
	public static RankingRowData TakeFinalRankingEntry()
	{
		if (!Active || !Current.Completed || finalRankingRecorded) return null;
		finalRankingRecorded = true;
		return new RankingRowData(Current.driverName, DateTime.Now.ToString(), (byte)Current.tracks.Length, Current.cash);
	}
	public static void Leave()
	{
		Save();
		RacePending = false;
		if (previousConfigs != null)
		{
			for (int i = 0; i < previousConfigs.Length; i++) F.I.cars[i].config = previousConfigs[i];
			F.I.s_playerCarIdx = previousCar;
			F.I.playerData.playerName = previousName;
			F.I.s_PlayerCarSponsor = previousSponsor;
			F.I.s_trackName = previousTrack; F.I.s_raceType = previousRaceType;
			F.I.scoringType = previousScoring; F.I.s_roadType = previousRoadType;
			F.I.s_laps = previousLaps; F.I.s_cpuRivals = previousRivals;
			F.I.teams = previousTeams; F.I.s_spectator = previousSpectator; F.I.s_inEditor = previousEditor;
			F.I.randomCars = previousRandomCars; F.I.randomTracks = previousRandomTracks;
		}
		previousConfigs = null;
		savingEnabled = false;
		F.I.gameMode = GameMode.Exhibition;
		Current = null;
		Slot = -1;
	}
	public static Livery SponsorFor(string season) => season switch
	{
		"GER" or "ITA" => Livery.Titan,
		"SPN" or "FRA" => Livery.TGR,
		"JAP" => Livery.Itex,
		"ENG" => Livery.Mysuko,
		"USA" => Livery.Rline,
		_ => Livery.Random
	};
	public static string NormalizeSeason(string value) => value == "JPN" ? "JAP" : value == "UK" ? "ENG" : value;
	public static bool Unlocked(string season) => Countries.Contains(season) ||
		Countries.All(country => F.I.HasWonChampionship(country) || (Current != null && Current.wonSeasons.Contains(country)));
	static List<string> Tracks(string country = null) => F.I.tracks.Where(t => t.Value.valid && t.Value.IsOriginal &&
		t.Key.Length > 3 && (country == null || t.Value.envir.ToString() == country)).Select(t => t.Key).ToList();
	static void Shuffle<T>(IList<T> list)
	{
		for (int i = list.Count - 1; i > 0; i--) { int j = UnityEngine.Random.Range(0, i + 1); (list[i], list[j]) = (list[j], list[i]); }
	}
	public static bool StartSeason(string season)
	{
		if (!Active || !Unlocked(season)) return false;
		var tracks = new List<string>();
		if (season == "SGP")
		{
			foreach (string country in Countries)
			{
				var countryTracks = Tracks(country); Shuffle(countryTracks);
				if (countryTracks.Count < 3) { Debug.LogError($"Championship: {country} needs at least three tracks."); return false; }
				tracks.AddRange(countryTracks.Take(3));
			}
			Shuffle(tracks);
			if (!F.I.tracks.ContainsKey("AZTEC")) { Debug.LogError("Championship: AZTEC is missing."); return false; }
			tracks.Add("AZTEC");
		}
		else if (season == "SURVIVAL") { tracks = Tracks(); Shuffle(tracks); tracks = tracks.Take(5).ToList(); if (tracks.Count < 5) return false; }
		else
		{
			tracks = Tracks(season); Shuffle(tracks);
		}
		if (tracks.Count == 0) return false;
		Current.season = season; Current.tracks = tracks.ToArray(); Current.nextRace = 0;
		Current.sponsor = SponsorFor(season);
		if (Current.sponsor == Livery.Random) Current.sponsor = Sponsors[UnityEngine.Random.Range(0, Sponsors.Length)];
		CarGroup group = F.I.tracks[tracks[0]].preferredCarClass;
		Current.carIndex = group == CarGroup.Wild ? 9 : group == CarGroup.Aero ? 14 : 8;
		Current.standings.Clear();
		foreach (Livery sponsor in Sponsors)
		{
			var candidates = Enumerable.Range(0, F.I.cars.Length).Where(i => F.I.cars[i].category == group).ToArray();
			Current.standings.Add(new ChampionshipStanding { sponsor = sponsor, carIndex = sponsor == Current.sponsor ? Current.carIndex : candidates[UnityEngine.Random.Range(0, candidates.Length)] });
		}
		if (!Current.cars.ContainsKey(Current.carIndex))
		{
			var config = new CarConfig(previousConfigs[Current.carIndex]);
			config.originalParts = F.I.GetDefaultOriginalVehicleSetup(Current.carIndex);
			Current.cars[Current.carIndex] = config;
		}
		Restore(); return true;
	}
	public static List<ChampionshipStanding> Standings => Current.standings.OrderByDescending(s => s.points).ThenByDescending(s => s.wins).ThenBy(s => s.lastPosition).ToList();
	public static int Position => Standings.FindIndex(s => s.sponsor == Current.sponsor) + 1;
	public static string NextTrack => Active && !Current.Completed && Current.tracks.Length > 0 ? Current.tracks[Current.nextRace] : null;
	public static bool PrepareRace()
	{
		if (NextTrack == null) return false;
		Restore(); F.I.s_trackName = NextTrack; F.I.s_laps = 10; F.I.s_cpuRivals = 5;
		F.I.s_raceType = Current.season == "SURVIVAL" ? RaceType.Survival : RaceType.Race;
		F.I.teams = false; F.I.s_spectator = false; F.I.randomCars = F.I.randomTracks = false;
		F.I.s_inEditor = false; F.I.scoringType = ScoringType.Championship;
		ResultsView.Clear(); Save(); RacePending = true; return true;
	}
	public static CarConfig OpponentConfig(int carIndex)
	{
		var config = new CarConfig(previousConfigs[carIndex]);
		config.originalParts = F.I.GetDefaultOriginalVehicleSetup(carIndex);
		return config;
	}

	public static string DriverName(ChampionshipStanding standing)
	{
		if (standing.sponsor == Current.sponsor) return Current.driverName;
		int opponentIndex = Current.standings.Where(s => s.sponsor != Current.sponsor).ToList().IndexOf(standing);
		return "CP" + (opponentIndex + 1);
	}
	public static CarPlacement[] Placements()
	{
		var entries = Current.standings.Where(s => s.sponsor != Current.sponsor).ToList();
		var player = Current.standings.Find(s => s.sponsor == Current.sponsor);
		// Retain each opponent's identity; only its starting position changes.
		var grid = entries.Append(player).OrderBy(s => s.points).ThenByDescending(s => s.lastPosition).ToList();
		var placements = new CarPlacement[6];
		for (int i = 0; i < entries.Count; i++)
			placements[i] = new CarPlacement { position = grid.IndexOf(entries[i]), carIdx = entries[i].carIndex,
				livery = entries[i].sponsor, name = DriverName(entries[i]) };
		placements[5] = CarPlacement.LocalPlayer();
		placements[5].position = grid.IndexOf(player);
		return placements;
	}
	public static void CancelRace() { RacePending = false; Save(); }
	public static void RecordRace()
	{
		if (!Active || !RacePending || !RaceManager.I.playerCar || RaceManager.I.playerCar.raceBox.enabled) return;
		// Cars still racing are placed by progress when the player returns to the menu.
		var results = ResultsView.SortedResultsByFinishPos
			.OrderBy(r => r.disqualified)
			.ThenByDescending(r => r.finished && !r.disqualified)
			.ThenBy(r => r.finished && !r.disqualified ? r.raceTime.TotalSeconds : 0)
			.ThenByDescending(r => r.progress).ToList();
		int[] points = { 10, 6, 4, 3, 2, 1 };
		for (int i = 0; i < results.Count && i < 6; i++)
		{
			var standing = Current.standings.Find(s => s.sponsor == results[i].sponsor);
			if (standing == null) continue;
			standing.lastPosition = i + 1;
			if (!results[i].disqualified) { standing.points += points[i]; if (i == 0) standing.wins++; }
			if (standing.sponsor == Current.sponsor && !results[i].disqualified) Current.cash += Prizes[i];
		}
		RacePending = false; Current.nextRace++;
		if (Current.Completed && Position == 1 && !Current.wonSeasons.Contains(Current.season)) Current.wonSeasons.Add(Current.season);
		// ChampionshipsView saves the awarded progress after the results are acknowledged.
	}
	public static int PartPrice(OriginalVehiclePartDefinition part) => Mathf.Max(0, part?.price ?? 0);
	public static int TradeIn(OriginalVehiclePartDefinition part) => PartPrice(part) / 2;
	public static int PartCost(OriginalVehiclePartDefinition part)
	{
		var installed = F.I.originalVehiclePartCatalog.GetSelectedPart(F.I.cars[Current.carIndex].config.originalParts, part.type);
		return installed?.id == part.id ? 0 : PartPrice(part) - TradeIn(installed);
	}
	public static bool BuyPart(OriginalVehiclePartDefinition part)
	{
		if (!Active || part == null) return false;
		var config = F.I.cars[Current.carIndex].config;
		var installed = F.I.originalVehiclePartCatalog.GetSelectedPart(config.originalParts, part.type);
		if (installed?.id == part.id) return false;
		int cost = PartCost(part);
		if (cost > Current.cash || !config.originalParts.TrySelectPart(part)) return false;
		// A negative exchange cost credits the difference to the player's balance.
		Current.cash -= cost; config.MarkModified(); Current.cars[Current.carIndex] = new CarConfig(config);
		Save(); F.I.UnlockOriginalVehiclePart(part); return true;
	}
	public static int CarPrice(int index) => F.I.cars[index].price;
	public static int CarTradeIn(int index) => Mathf.FloorToInt(Mathf.Max(0, CarPrice(index)) * TradeInFraction);
	public static int CarCost(int index) => index == Current.carIndex ? 0 : CarPrice(index) - CarTradeIn(Current.carIndex);
	public static bool BuyCar(int index)
	{
		if (!Active || index < 0 || index >= F.I.cars.Length) return false;
		if (index == Current.carIndex) return true;
		int price = CarCost(index);
		if (CarPrice(index) < 0 || price > Current.cash) return false;
		Current.cash -= price; Current.carIndex = index;
		Current.standings.Find(s => s.sponsor == Current.sponsor).carIndex = index;
		if (!Current.cars.ContainsKey(index))
		{
			var config = new CarConfig(previousConfigs[index]); config.originalParts = F.I.GetDefaultOriginalVehicleSetup(index); Current.cars[index] = config;
		}
		Restore(); Save(); return true;
	}
}

public static class ChampionshipUI
{
	public static MainMenuView View(string name) => Resources.FindObjectsOfTypeAll<MainMenuView>().FirstOrDefault(v => v.gameObject.scene.IsValid() && v.name == name);
	public static void SetText(GameObject target, string text)
	{
		if (!target) return;
		foreach (var localization in target.GetComponentsInChildren<LocalizeStringEvent>(true)) localization.enabled = false;
		var tmp = target.GetComponent<TMP_Text>() ?? target.GetComponentInChildren<TMP_Text>(true);
		if (tmp) { tmp.text = text; return; }
		var legacy = target.GetComponent<Text>() ?? target.GetComponentInChildren<Text>(true);
		if (legacy) legacy.text = text;
	}
	public static Transform Child(Transform root, string name) => root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
	public static Button Bind(Transform root, string name, UnityAction action)
	{
		var button = root.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name.Equals(name, StringComparison.OrdinalIgnoreCase));
		if (!button) return null;
		button.onClick = new Button.ButtonClickedEvent(); button.onClick.AddListener(action); return button;
	}
	public static Button PurchaseButton(Transform root, string name, UnityAction action)
	{
		Transform existing = Child(root, name);
		if (existing) return Bind(root, name, action);
		var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
		go.transform.SetParent(root, false);
		var rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = new Vector2(1, 0);
		rect.pivot = new Vector2(1, 0); rect.anchoredPosition = new Vector2(-50, 40); rect.sizeDelta = new Vector2(340, 55);
		go.GetComponent<Image>().color = new Color(0.35f, 0.06f, 0.01f, 0.95f);
		var label = new GameObject("Label", typeof(RectTransform), typeof(Text)); label.transform.SetParent(go.transform, false);
		var lr = (RectTransform)label.transform; lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one; lr.offsetMin = lr.offsetMax = Vector2.zero;
		var text = label.GetComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = 22; text.alignment = TextAnchor.MiddleCenter;
		return Bind(root, name, action);
	}
}

public class ChampionshipView : MainMenuView
{
	protected override void OnEnable()
	{
		base.OnEnable();
		prevView = ChampionshipUI.View("SPView");
		if (!Championships.Active) return;
		Championships.EnterChampionshipView();
		Refresh();
	}
	protected override void OnDisable() { if (Championships.Active) Championships.Save(); base.OnDisable(); }
	public void Refresh()
	{
		var state = Championships.Current;
		int round = Mathf.Min(state.nextRace + 1, state.tracks.Length);
		ChampionshipUI.SetText(ChampionshipUI.Child(transform, "Description")?.gameObject,
			string.Format(F.I.LocStr("Champ.CurrentStandings"), round, state.tracks.Length));
		ChampionshipUI.SetText(ChampionshipUI.Child(transform, "status")?.gameObject, "");
		ChampionshipUI.SetText(ChampionshipUI.Child(transform, "status_1")?.gameObject,
			$"{F.I.LocStr("Credits")}:\t$ {state.cash.ToString("000,000", CultureInfo.InvariantCulture)}\n" +
			$"{F.I.LocStr("Vehicle")}:\t$ {Championships.CarTradeIn(state.carIndex).ToString("000,000", CultureInfo.InvariantCulture)}");
		var grid = ChampionshipUI.Child(transform, "GridLayoutView");
		if (grid)
		{
			var standings = Championships.Standings;
			string[] headers = { "Name", "Sponsor", "Points", "Wins" };
			for (int i = 0; i < grid.childCount; i++)
			{
				var cell = grid.GetChild(i); cell.gameObject.SetActive(i < 28);
				if (i < 4) ChampionshipUI.SetText(cell.gameObject, F.I.LocStr(headers[i]));
				else if (i < 28)
				{
					var row = standings[(i - 4) / 4]; int col = (i - 4) % 4;
					ChampionshipUI.SetText(cell.gameObject, col == 0 ? Championships.DriverName(row) : col == 1 ? row.sponsor.ToString() : col == 2 ? row.points.ToString() : row.wins.ToString());
					Color color = row.sponsor == state.sponsor ? Color.white : Color.gray;
					foreach (var text in cell.GetComponentsInChildren<TMP_Text>(true)) text.color = color;
					foreach (var text in cell.GetComponentsInChildren<Text>(true)) text.color = color;
				}
			}
		}
	}
	public void NextRace() => GoToView(ChampionshipUI.View(Championships.Current.Completed ? "ChampSeasonSelection" : "ChampNextRace"));
	public void ModifyVehicle()
	{
		var view = ChampionshipUI.View("TuningView"); view.prevView = this; GoToView(view);
	}
	public void ChangeVehicle()
	{
		F.I.carSelector.SetType(GarageType.Earned, Championships.Current.cash);
		var view = ChampionshipUI.View("GarageView"); view.prevView = this; GoToView(view);
	}
	public void ExitChampionship() => GoToView(ChampionshipUI.View("SPView"));
}
