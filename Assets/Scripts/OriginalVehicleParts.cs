using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Newtonsoft.Json;

public enum OriginalVehiclePartType
{
	Engine,
	Battery,
	Tyres,
	Brakes,
	Suspension,
	Chassis,
	Drive,
	Horn,
	StuntBoost,
	Bms,
	Gears
}

[Serializable]
public class OriginalVehiclePartDefinition
{
	public string id;
	public OriginalVehiclePartType type;
	public int index;
	public int price;
	public Dictionary<string, float> physicsOverrides;
	public int presentationIndex;
	[JsonIgnore] public string sourcePath;
	[JsonIgnore] public bool IsUserPart => physicsOverrides != null;
	public string[] parameterNames = Array.Empty<string>();
	public float[] parameters = Array.Empty<float>();
	public string[] localizedNames = Array.Empty<string>();
	public string[] localizedDescriptions = Array.Empty<string>();

	public float GetParameter(string parameterName, float fallback = 0)
	{
		if (parameterNames == null || parameters == null)
			return fallback;

		for (int i = 0; i < parameterNames.Length && i < parameters.Length; i++)
		{
			if (string.Equals(parameterNames[i], parameterName, StringComparison.OrdinalIgnoreCase))
				return parameters[i];
		}
		return fallback;
	}

	public string GetName(Language language = Language.English)
	{
		// The remake currently has English and Polish UI; the original data has no Polish entries.
		return GetLocalizedName(language == Language.English ? 0 : -1);
	}

	public string GetDescription(Language language = Language.English)
	{
		return GetLocalizedDescription(language == Language.English ? 0 : -1);
	}

	/// <summary>Returns a source languagepc.csv translation (0 = English, 1 = Dutch, ...).</summary>
	public string GetLocalizedName(int sourceLanguageIndex) => GetLocalized(localizedNames, sourceLanguageIndex, id);

	/// <summary>Returns a source languagepc.csv translation (0 = English, 1 = Dutch, ...).</summary>
	public string GetLocalizedDescription(int sourceLanguageIndex) => GetLocalized(localizedDescriptions, sourceLanguageIndex, string.Empty);

	static string GetLocalized(string[] values, int index, string fallback)
	{
		if (values == null || values.Length == 0)
			return fallback;
		if (index < 0 || index >= values.Length || string.IsNullOrWhiteSpace(values[index]))
			index = 0;
		return index < values.Length && !string.IsNullOrWhiteSpace(values[index]) ? values[index] : fallback;
	}
}

[Serializable]
public class OriginalVehiclePartSlot
{
	public OriginalVehiclePartType type;
	public int defaultIndex;
	public int minimumIndex;
	public int maximumIndex;
	public int selectedIndex;
	public string defaultPartId;
	public string selectedUserPartId;

	[JsonIgnore]
	public string SelectedPartId => !string.IsNullOrEmpty(selectedUserPartId) ? selectedUserPartId :
		selectedIndex > 0 ? OriginalVehiclePartCatalog.MakePartId(type, selectedIndex) : null;

	public OriginalVehiclePartSlot Clone()
	{
		return new OriginalVehiclePartSlot
		{
			type = type,
			defaultIndex = defaultIndex,
			minimumIndex = minimumIndex,
			maximumIndex = maximumIndex,
			selectedIndex = selectedIndex,
			defaultPartId = defaultPartId,
			selectedUserPartId = selectedUserPartId
		};
	}
}

[Serializable]
public class OriginalVehicleCarSetup
{
	public string sourceCarId;
	public int category;
	public int livery;
	public OriginalVehiclePartSlot[] slots = Array.Empty<OriginalVehiclePartSlot>();

	public OriginalVehiclePartSlot GetSlot(OriginalVehiclePartType type)
	{
		if (slots == null)
			return null;
		for (int i = 0; i < slots.Length; i++)
			if (slots[i] != null && slots[i].type == type)
				return slots[i];
		return null;
	}

	public bool TrySelectPart(OriginalVehiclePartType type, int index)
	{
		OriginalVehiclePartSlot slot = GetSlot(type);
		if (slot == null || index < slot.minimumIndex || index > slot.maximumIndex)
			return false;
		slot.selectedIndex = index;
		slot.selectedUserPartId = null;
		return true;
	}

	public bool TrySelectPart(OriginalVehiclePartDefinition part)
	{
		if (part == null) return false;
		if (!part.IsUserPart) return TrySelectPart(part.type, part.index);
		OriginalVehiclePartSlot slot = GetSlot(part.type);
		if (slot == null) return false;
		slot.selectedUserPartId = part.id;
		return true;
	}
	public OriginalVehicleCarSetup Clone()
	{
		var clonedSlots = new OriginalVehiclePartSlot[slots?.Length ?? 0];
		for (int i = 0; i < clonedSlots.Length; i++)
			clonedSlots[i] = slots[i]?.Clone();
		return new OriginalVehicleCarSetup
		{
			sourceCarId = sourceCarId,
			category = category,
			livery = livery,
			slots = clonedSlots
		};
	}
}

/// <summary>
/// Runtime copy of the original game's dynamics, allowed/default car part setups,
/// and the corresponding names and descriptions from languagepc.csv.
/// </summary>
public class OriginalVehiclePartCatalog
{
	static readonly OriginalVehiclePartType[] PartTypes =
	{
		OriginalVehiclePartType.Engine,
		OriginalVehiclePartType.Battery,
		OriginalVehiclePartType.Tyres,
		OriginalVehiclePartType.Brakes,
		OriginalVehiclePartType.Suspension,
		OriginalVehiclePartType.Chassis,
		OriginalVehiclePartType.Drive,
		OriginalVehiclePartType.Horn,
		OriginalVehiclePartType.StuntBoost,
		OriginalVehiclePartType.Bms,
		OriginalVehiclePartType.Gears
	};

	static readonly string[] PartPrefixes =
	{
		"Engine", "Battery", "Tyres", "Brakes", "Suspension", "Chassis", "Drive", "Horns", "StuntBoost", "Bms", "Gears"
	};

	static readonly string[] LanguagePrefixes =
	{
		"Engine", "Battery", "Tyres", "Brakes", "Shocks", "Chassis", "Drive", "Horn", "Boost", "BMS", "Gears"
	};

	static readonly string[] SetupColumnNames =
	{
		"Engine", "Battery", "Tyres", "Brakes", "Suspension", "Chassis", "Drive", "Horn", "Stunt Boost", "BMS", "Gears"
	};

	readonly Dictionary<string, OriginalVehiclePartDefinition> partsById = new(StringComparer.OrdinalIgnoreCase);
	readonly Dictionary<string, OriginalVehicleCarSetup> setupsBySourceId = new(StringComparer.OrdinalIgnoreCase);
	readonly Dictionary<string, int> carPricesById = new(StringComparer.OrdinalIgnoreCase);
	readonly Dictionary<string, float[]> carDynamicsById = new(StringComparer.OrdinalIgnoreCase);
	readonly Dictionary<string, int> typeOrderBySetupName = new(StringComparer.OrdinalIgnoreCase);
	readonly Dictionary<string, string[]> languageNamesByKey = new(StringComparer.OrdinalIgnoreCase);
	readonly Dictionary<string, string[]> languageDescriptionsByKey = new(StringComparer.OrdinalIgnoreCase);
	public readonly List<OriginalVehiclePartDefinition> parts = new();
	public readonly List<OriginalVehicleCarSetup> carSetups = new();
	public readonly string[] parameterNames;

	OriginalVehiclePartCatalog(string[] dynamicsHeader)
	{
		parameterNames = new string[Math.Max(0, dynamicsHeader.Length - 2)];
		for (int i = 2; i < dynamicsHeader.Length; i++)
			parameterNames[i - 2] = string.IsNullOrWhiteSpace(dynamicsHeader[i]) ? "Parameter" + (i - 1) : dynamicsHeader[i].Trim();

		for (int i = 0; i < PartTypes.Length; i++)
		{
			typeOrderBySetupName[SetupColumnNames[i]] = i;
		}
	}

	public static OriginalVehiclePartCatalog Load(string dynamicsCsv, string carSetupCsv, string languageCsv)
	{
		List<string[]> dynamicsRows = ParseCsv(dynamicsCsv);
		List<string[]> setupRows = ParseCsv(carSetupCsv);
		List<string[]> languageRows = ParseCsv(languageCsv);
		if (dynamicsRows.Count < 2 || setupRows.Count < 2)
			throw new InvalidDataException("Original vehicle part CSV data is missing a header or data rows.");

		var catalog = new OriginalVehiclePartCatalog(dynamicsRows[0]);
		catalog.LoadLanguageRows(languageRows);
		catalog.LoadCarDynamicsRows(dynamicsRows);
		catalog.LoadPartRows(dynamicsRows);
		catalog.LoadCarSetups(setupRows);
		return catalog;
	}

	void LoadCarDynamicsRows(List<string[]> rows)
	{
		for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
		{
			string[] row = rows[rowIndex];
			if (row.Length == 0 || !row[0].StartsWith("Car", StringComparison.OrdinalIgnoreCase))
				continue;

			var values = new float[parameterNames.Length];
			for (int column = 2; column < row.Length && column - 2 < values.Length; column++)
				values[column - 2] = ParseFloat(row[column]);
			carDynamicsById[row[0].Trim()] = values;
			carPricesById[row[0].Trim()] = row.Length > 1 ? (int)ParseFloat(row[1]) : 0;
		}
	}

	/// <summary>
	/// Resolves one value from the car's dynamics.csv row combined with its selected
	/// parts. Parts replace populated fields; Additive Mass is summed across slots,
	/// matching the original race setup's tuning block.
	/// </summary>
	public bool TryGetEffectiveParameter(OriginalVehicleCarSetup setup, string parameterName, out float value)
	{
		int parameterIndex = Array.FindIndex(parameterNames,
			name => string.Equals(name, parameterName, StringComparison.OrdinalIgnoreCase));
		return TryGetEffectiveParameter(setup, parameterIndex, out value);
	}

	public bool TryGetEffectiveParameter(OriginalVehicleCarSetup setup, string parameterName, int occurrence,
		out float value)
	{
		int parameterIndex = -1;
		for (int i = 0; i < parameterNames.Length; i++)
		{
			if (!string.Equals(parameterNames[i], parameterName, StringComparison.OrdinalIgnoreCase))
				continue;
			if (occurrence-- == 0)
			{
				parameterIndex = i;
				break;
			}
		}
		return TryGetEffectiveParameter(setup, parameterIndex, out value);
	}

	public bool TryGetEffectiveParameter(OriginalVehicleCarSetup setup, int parameterIndex, out float value)
	{
		value = 0;
		if (setup == null || string.IsNullOrWhiteSpace(setup.sourceCarId) ||
			!carDynamicsById.TryGetValue(setup.sourceCarId, out float[] baseValues) ||
			parameterIndex < 0 || parameterIndex >= baseValues.Length)
			return false;

		const float notSpecified = -99999;
		value = baseValues[parameterIndex];
		if (string.Equals(parameterNames[parameterIndex], "Additive Mass", StringComparison.OrdinalIgnoreCase))
		{
			for (int i = 0; i < (setup.slots?.Length ?? 0); i++)
			{
				OriginalVehiclePartSlot slot = setup.slots[i];
				OriginalVehiclePartDefinition part = slot == null ? null : GetPresentationPart(GetPart(slot.SelectedPartId));
				if (part == null)
					continue;
				float additiveMass = parameterIndex < part.parameters.Length
					? part.parameters[parameterIndex] : notSpecified;
				if (additiveMass != notSpecified)
					value += additiveMass;
			}
			return value != notSpecified;
		}

		for (int i = 0; i < (setup.slots?.Length ?? 0); i++)
		{
			OriginalVehiclePartSlot slot = setup.slots[i];
			OriginalVehiclePartDefinition part = slot == null ? null : GetPresentationPart(GetPart(slot.SelectedPartId));
			if (part == null)
				continue;
			float partValue = parameterIndex < part.parameters.Length
				? part.parameters[parameterIndex] : notSpecified;
			if (partValue != notSpecified)
				value = partValue;
		}
		return value != notSpecified;
	}

	void LoadLanguageRows(List<string[]> rows)
	{
		for (int i = 0; i < rows.Count; i++)
		{
			string[] row = rows[i];
			if (row.Length < 2 || string.IsNullOrWhiteSpace(row[0]))
				continue;
			if (row[0].StartsWith("feText_", StringComparison.OrdinalIgnoreCase) && row[0].IndexOf("Name", StringComparison.OrdinalIgnoreCase) >= 0)
				languageNamesByKey[row[0]] = SliceLanguages(row);
			else if (row[0].StartsWith("feText_", StringComparison.OrdinalIgnoreCase) && row[0].IndexOf("Info", StringComparison.OrdinalIgnoreCase) >= 0)
				languageDescriptionsByKey[row[0]] = SliceLanguages(row);
		}
	}

	void LoadPartRows(List<string[]> rows)
	{
		for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
		{
			string[] row = rows[rowIndex];
			if (row.Length < 2)
				continue;
			string id = row[0].Trim();
			if (string.IsNullOrEmpty(id) || !TryGetPartType(id, out OriginalVehiclePartType type, out int partIndex))
				continue;

			var definition = new OriginalVehiclePartDefinition
			{
				id = id,
				type = type,
				index = partIndex,
				price = ParseInt(row[1]),
				parameterNames = parameterNames,
				parameters = new float[parameterNames.Length]
			};
			for (int column = 2; column < row.Length && column - 2 < definition.parameters.Length; column++)
				definition.parameters[column - 2] = ParseFloat(row[column]);

			int typeIndex = Array.IndexOf(PartTypes, type);
			string languagePrefix = LanguagePrefixes[typeIndex];
			string languageSuffix = partIndex.ToString("00", CultureInfo.InvariantCulture);
			string nameKey = "feText_" + languagePrefix + "Name" + languageSuffix;
			string infoKey = "feText_" + languagePrefix + "Info" + languageSuffix;
			languageNamesByKey.TryGetValue(nameKey, out definition.localizedNames);
			languageDescriptionsByKey.TryGetValue(infoKey, out string[] localizedInfo);
			definition.localizedDescriptions = GetDescriptionLanguages(localizedInfo);

			parts.Add(definition);
			partsById[definition.id] = definition;
		}
	}

	void LoadCarSetups(List<string[]> rows)
	{
		string[] header = rows[0];
		var setupColumnStarts = new List<(OriginalVehiclePartType type, int start)>();
		for (int i = 0; i < header.Length; i++)
		{
			if (typeOrderBySetupName.TryGetValue(header[i].Trim(), out int typeOrder))
				setupColumnStarts.Add((PartTypes[typeOrder], i));
		}

		for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
		{
			string[] row = rows[rowIndex];
			if (row.Length == 0 || string.IsNullOrWhiteSpace(row[0]))
				continue;
			var setup = new OriginalVehicleCarSetup
			{
				sourceCarId = row[0].Trim(),
				category = ParseInt(GetCell(row, 1)),
				livery = ParseInt(GetCell(row, 2)),
				slots = new OriginalVehiclePartSlot[setupColumnStarts.Count]
			};

			for (int slotIndex = 0; slotIndex < setupColumnStarts.Count; slotIndex++)
			{
				(OriginalVehiclePartType type, int start) = setupColumnStarts[slotIndex];
				int defaultIndex = ParseInt(GetCell(row, start));
				int minIndex = ParseInt(GetCell(row, start + 1));
				int maxIndex = ParseInt(GetCell(row, start + 2));
				setup.slots[slotIndex] = new OriginalVehiclePartSlot
				{
					type = type,
					defaultIndex = defaultIndex,
					minimumIndex = minIndex,
					maximumIndex = maxIndex,
					selectedIndex = defaultIndex,
					defaultPartId = defaultIndex > 0 ? MakePartId(type, defaultIndex) : null
				};
			}

			carSetups.Add(setup);
			setupsBySourceId[setup.sourceCarId] = setup;
		}
	}

	public const string UserPartExtension = "sgppart";
	public int UserPartsVersion { get; private set; }
	public OriginalVehiclePartDefinition GetSelectedPart(OriginalVehicleCarSetup setup, OriginalVehiclePartType type) =>
		GetPart(setup?.GetSlot(type)?.SelectedPartId);

	public OriginalVehiclePartDefinition GetPresentationPart(OriginalVehiclePartDefinition part) =>
		part != null && part.IsUserPart ? GetPart(part.type, part.presentationIndex) : part;
	public void LoadUserParts(string directory)
	{
		Directory.CreateDirectory(directory);
		foreach (OriginalVehiclePartDefinition part in parts)
			if (part.IsUserPart) partsById.Remove(part.id);
		parts.RemoveAll(part => part.IsUserPart);
		string[] files = Directory.GetFiles(directory, "*." + UserPartExtension);
		Array.Sort(files, StringComparer.OrdinalIgnoreCase);
		foreach (string path in files)
		{
			try
			{
				OriginalVehicleUserPartFile file = JsonConvert.DeserializeObject<OriginalVehicleUserPartFile>(File.ReadAllText(path));
				if (file == null || file.formatVersion != 1 || string.IsNullOrWhiteSpace(file.id) ||
					!file.id.StartsWith("user-", StringComparison.Ordinal) || !Enum.IsDefined(typeof(OriginalVehiclePartType), file.type) ||
					file.physics == null || file.physics.Count == 0 || partsById.ContainsKey(file.id))
					throw new InvalidDataException("Invalid or duplicate user part.");
				foreach (var entry in file.physics)
					if (!OriginalVehicleUserPartFile.IsValidValue(entry.Key, entry.Value))
						throw new InvalidDataException("Invalid part parameter: " + entry.Key);
				float[] values = new float[parameterNames.Length];
				Array.Fill(values, -99999f);
				var part = new OriginalVehiclePartDefinition
				{
					id = file.id, type = file.type, index = 0,
					parameterNames = parameterNames, parameters = values,
					localizedNames = new[] { string.IsNullOrWhiteSpace(file.name) ? Path.GetFileNameWithoutExtension(path) : file.name },
					localizedDescriptions = new[] { file.description ?? "User-created part" },
					physicsOverrides = file.physics, presentationIndex = file.presentationIndex, sourcePath = Path.GetFullPath(path)
				};
				parts.Add(part);
				partsById.Add(part.id, part);
			}
			catch (Exception exception)
			{
				UnityEngine.Debug.LogWarning("Could not load user part " + path + ": " + exception.Message);
			}
		}
		UserPartsVersion++;
	}
	public OriginalVehiclePartDefinition GetPart(string id)
	{
		if (string.IsNullOrWhiteSpace(id))
			return null;
		partsById.TryGetValue(id, out OriginalVehiclePartDefinition part);
		return part;
	}

	public OriginalVehiclePartDefinition GetPart(OriginalVehiclePartType type, int index) => GetPart(MakePartId(type, index));

	public List<OriginalVehiclePartDefinition> GetAvailableParts(OriginalVehicleCarSetup setup, OriginalVehiclePartType type)
	{
		var available = new List<OriginalVehiclePartDefinition>();
		OriginalVehiclePartSlot slot = setup?.GetSlot(type);
		if (slot == null) return available;

		for (int index = System.Math.Max(1, slot.minimumIndex); index <= slot.maximumIndex; index++)
		{
			OriginalVehiclePartDefinition part = GetPart(type, index);
			if (part != null)
				available.Add(part);
		}
		foreach (OriginalVehiclePartDefinition part in parts)
			if (part.IsUserPart && part.type == type) available.Add(part);
		return available;
	}

	public int GetSourceCarPrice(int remakeCarIndex)
	{
		string id = "Car" + (remakeCarIndex + 1).ToString("00", CultureInfo.InvariantCulture);
		return carPricesById.TryGetValue(id, out int price) ? price : -1;
	}

	public OriginalVehicleCarSetup GetDefaultSetup(int remakeCarIndex)
	{
		string sourceCarId = "Car" + (remakeCarIndex + 1).ToString("00", CultureInfo.InvariantCulture);
		return GetDefaultSetup(sourceCarId);
	}

	public OriginalVehicleCarSetup GetDefaultSetup(string sourceCarId)
	{
		if (string.IsNullOrWhiteSpace(sourceCarId))
			return null;
		setupsBySourceId.TryGetValue(sourceCarId, out OriginalVehicleCarSetup setup);
		return setup?.Clone();
	}

	public static string MakePartId(OriginalVehiclePartType type, int index)
	{
		if (index <= 0)
			return null;
		int typeIndex = Array.IndexOf(PartTypes, type);
		return typeIndex < 0 ? null : PartPrefixes[typeIndex] + index.ToString("00", CultureInfo.InvariantCulture);
	}

	static bool TryGetPartType(string id, out OriginalVehiclePartType type, out int partIndex)
	{
		for (int i = 0; i < PartPrefixes.Length; i++)
		{
			string prefix = PartPrefixes[i];
			if (!id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
				continue;
			string suffix = id.Substring(prefix.Length);
			if (int.TryParse(suffix, NumberStyles.Integer, CultureInfo.InvariantCulture, out partIndex) && partIndex > 0)
			{
				type = PartTypes[i];
				return true;
			}
		}
		type = default;
		partIndex = 0;
		return false;
	}

	static string[] SliceLanguages(string[] row)
	{
		int count = Math.Max(0, row.Length - 1);
		var values = new string[count];
		for (int i = 0; i < count; i++)
			values[i] = row[i + 1].Trim();
		return values;
	}

	static string[] GetDescriptionLanguages(string[] localizedInfo)
	{
		if (localizedInfo == null)
			return Array.Empty<string>();
		var descriptions = new string[localizedInfo.Length];
		for (int i = 0; i < localizedInfo.Length; i++)
		{
			string info = localizedInfo[i] ?? string.Empty;
			int separator = info.IndexOf("||", StringComparison.Ordinal);
			descriptions[i] = separator >= 0 ? info.Substring(separator + 2).Trim() : info.Trim();
		}
		return descriptions;
	}

	static string GetCell(string[] row, int index) => index >= 0 && index < row.Length ? row[index] : string.Empty;

	static int ParseInt(string value)
	{
		return int.TryParse(value?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) ? result : 0;
	}

	static float ParseFloat(string value)
	{
		return float.TryParse(value?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float result) ? result : 0;
	}

	static List<string[]> ParseCsv(string text)
	{
		var rows = new List<string[]>();
		if (string.IsNullOrEmpty(text))
			return rows;

		var row = new List<string>();
		var field = new StringBuilder();
		bool quoted = false;
		for (int i = 0; i < text.Length; i++)
		{
			char current = text[i];
			if (quoted)
			{
				if (current == '"' && i + 1 < text.Length && text[i + 1] == '"')
				{
					field.Append('"');
					i++;
				}
				else if (current == '"')
					quoted = false;
				else
					field.Append(current);
				continue;
			}

			if (current == '"')
				quoted = true;
			else if (current == ',')
			{
				row.Add(field.ToString());
				field.Clear();
			}
			else if (current == '\r' || current == '\n')
			{
				if (current == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
					i++;
				row.Add(field.ToString());
				field.Clear();
				rows.Add(row.ToArray());
				row.Clear();
			}
			else
				field.Append(current);
		}

		if (field.Length > 0 || row.Count > 0)
		{
			row.Add(field.ToString());
			rows.Add(row.ToArray());
		}
		return rows;
	}
}

[Serializable]
public class OriginalVehicleUserPartFile
{
	public int formatVersion = 1;
	public string id;
	public string name;
	public string description = "User-created part";
	[JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
	public OriginalVehiclePartType type;
	public int presentationIndex;
	public Dictionary<string, float> physics = new();

	public static bool IsValidValue(string key, float value)
	{
		if (float.IsNaN(value) || float.IsInfinity(value)) return false;
		return TryGetRatioIndex(key, out _) || ResolveField(null, key, out _) != null;
	}
	static bool TryGetRatioIndex(string key, out int index)
	{
		index = -1;
		return key != null && key.StartsWith("ratios.", StringComparison.Ordinal) &&
			int.TryParse(key.Substring(7), out index) && index >= 0 && index < 8;
	}
	static System.Reflection.FieldInfo ResolveField(RVP.OriginalVehiclePhysicsConfig config, string key, out object target)
	{
		target = config;
		if (string.IsNullOrEmpty(key)) return null;
		string[] segments = key.Split('.');
		Type type = typeof(RVP.OriginalVehiclePhysicsConfig);
		string fieldName = key;
		int wheel = segments.Length == 3 && segments[1] == "rear" ? 0 :
			segments.Length == 3 && segments[1] == "front" ? 2 : -1;
		if (segments.Length == 3 && segments[0] == "tyres" &&
			(wheel >= 0 || int.TryParse(segments[1], out wheel)) && wheel >= 0 && wheel < 4)
		{
			type = typeof(RVP.OriginalTyrePhysicsConfig);
			target = config?.tyres != null && wheel < config.tyres.Length ? config.tyres[wheel] : null;
			fieldName = segments[2];
		}
		var field = type.GetField(fieldName);
		return field != null && field.Name != "formatVersion" && (field.FieldType == typeof(float) || field.FieldType == typeof(int)) ? field : null;
	}
	public static void Apply(RVP.OriginalVehiclePhysicsConfig config, Dictionary<string, float> values)
	{
		if (config == null || values == null) return;
		foreach (var entry in values)
		{
			if (!IsValidValue(entry.Key, entry.Value)) continue;
			if (TryGetRatioIndex(entry.Key, out int ratio))
			{
				if (config.ratios != null && ratio < config.ratios.Length) config.ratios[ratio] = entry.Value;
				continue;
			}
			var field = ResolveField(config, entry.Key, out object target);
			if (field == null || target == null) continue;
			if (field.FieldType == typeof(int)) field.SetValue(target, UnityEngine.Mathf.RoundToInt(entry.Value));
			else field.SetValue(target, entry.Value);
			// Axle keys apply the same value to both sides; numeric keys from older parts still work.
			string[] segments = entry.Key.Split('.');
			if (segments.Length == 3 && segments[0] == "tyres" && (segments[1] == "rear" || segments[1] == "front"))
			{
				int right = segments[1] == "rear" ? 1 : 3;
				if (config.tyres != null && right < config.tyres.Length && config.tyres[right] != null)
					field.SetValue(config.tyres[right], entry.Value);
			}
		}
	}
}
