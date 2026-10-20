using UnityEngine;
using UnityEngine.InputSystem;

public class RebindsLoad : MonoBehaviour
{
	public InputActionAsset actions;
	public void OnEnable()
	{
		var rebinds = PlayerPrefs.GetString("rebinds");
		if (!string.IsNullOrEmpty(rebinds))
		{
			actions.LoadBindingOverridesFromJson(rebinds);
			bool changed = false;
			foreach (var map in actions.actionMaps)
				foreach (var action in map.actions)
				{
					for (int i = 0; i < action.bindings.Count; i++)
						changed |= NormalizeDriveBinding(action, i);
					changed |= RepairDriveAxisPairs(action);
				}
			if (changed) PlayerPrefs.SetString("rebinds", actions.SaveBindingOverridesAsJson());
		}
	}
	public static bool RepairDriveAxisPairs(InputAction action)
	{
		if (action.name != "Drive") return false;
		bool changed = false;
		for (int upIndex = 0; upIndex < action.bindings.Count; upIndex++)
		{
			var up = action.bindings[upIndex];
			if (!up.isPartOfComposite ||
				(!up.name.Equals("up", System.StringComparison.OrdinalIgnoreCase) && !up.name.Equals("right", System.StringComparison.OrdinalIgnoreCase)) ||
				string.IsNullOrEmpty(up.overridePath) || string.IsNullOrEmpty(up.overrideProcessors)) continue;
			var control = InputSystem.FindControl(up.overridePath) as UnityEngine.InputSystem.Controls.AxisControl;
			if (control == null || control is UnityEngine.InputSystem.Controls.ButtonControl) continue;
			int compositeIndex = upIndex - 1;
			string opposite = up.name.Equals("right", System.StringComparison.OrdinalIgnoreCase) ? "left" : "down";
			while (compositeIndex >= 0 && action.bindings[compositeIndex].isPartOfComposite) compositeIndex--;
			for (int downIndex = compositeIndex + 1; downIndex < action.bindings.Count && action.bindings[downIndex].isPartOfComposite; downIndex++)
			{
				var down = action.bindings[downIndex];
				if (!down.name.Equals(opposite, System.StringComparison.OrdinalIgnoreCase) ||
					down.overridePath != up.overridePath || down.overrideProcessors != up.overrideProcessors) continue;
				if (up.overrideProcessors != "Clamp(min=0,max=1)" && up.overrideProcessors != "Invert,Clamp(min=0,max=1)") continue;
				action.ApplyBindingOverride(downIndex, new InputBinding
				{
					overridePath = down.overridePath,
					overrideProcessors = up.overrideProcessors == "Clamp(min=0,max=1)"
						? "Invert,Clamp(min=0,max=1)" : "Clamp(min=0,max=1)"
				});
				changed = true;
			}
		}
		return changed;
	}
	public static bool NormalizeDriveBinding(InputAction action, int bindingIndex)
	{
		if (action.name != "Drive") return false;
		var binding = action.bindings[bindingIndex];
		if (!binding.isPartOfComposite ||
			(!binding.name.Equals("up", System.StringComparison.OrdinalIgnoreCase) &&
			 !binding.name.Equals("down", System.StringComparison.OrdinalIgnoreCase) &&
			 !binding.name.Equals("left", System.StringComparison.OrdinalIgnoreCase) &&
			 !binding.name.Equals("right", System.StringComparison.OrdinalIgnoreCase))) return false;
		string path = binding.overridePath;
		// New directional axis overrides already contain their captured polarity.
		if (!string.IsNullOrEmpty(binding.overrideProcessors)) return false;
		bool horizontal = binding.name.Equals("left", System.StringComparison.OrdinalIgnoreCase) || binding.name.Equals("right", System.StringComparison.OrdinalIgnoreCase);
		if (string.IsNullOrEmpty(path) || !path.EndsWith(horizontal ? "/x" : "/y", System.StringComparison.OrdinalIgnoreCase) ||
			path.IndexOf("stick/", System.StringComparison.OrdinalIgnoreCase) < 0) return false;
		// Preserve the selected stick and device, changing only the full axis
		// into the directional half-axis corresponding to this composite part.
		action.ApplyBindingOverride(bindingIndex, path.Substring(0, path.Length - 1) + binding.name.ToLowerInvariant());
		return true;
	}
}
