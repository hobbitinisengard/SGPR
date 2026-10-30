# Baked night environment lighting

The country prefabs in `Assets/Resources/envirs` use baked lights. Daytime still
uses the existing sun/ambient setup. Night lightmaps are loaded only for the
current country and detached when switching to daytime or unloading the environment.
The global sun, vehicle lights and sky prefabs are not converted by this tool.

## Generate the lighting

In Unity, outside Play Mode, use:

**Tools > Environment lighting > Bake all night environments**

This saves modified scenes through Unity's usual save prompt, prepares all eight
country prefabs, generates missing secondary UVs on their imported models, marks
static meshes/terrain as GI contributors and bakes each environment in turn.
Animated objects are excluded from static GI preparation. Settings use
non-directional 2048px lightmaps, 2 texels/unit and reduced mesh scale for mobile.
Cancel through Unity's lighting progress UI if needed.

The tool temporarily opens isolated night scenes in `Assets/EnvironmentLightingBakeScenes`
and restores the previous scene setup when finished. Keep these scenes: they hold
Unity's generated lightmap textures. Runtime data in
`Assets/Resources/envirs/Lighting/<country>_Night.asset` references those textures and
stores per-renderer/terrain UV offsets. Commit the generated textures, data, scene,
lighting settings and metadata. Re-bake after changing geometry, lamps or terrain.
Do not move environment children between baking and runtime: bindings use sibling paths.

**Prepare night prefabs** performs preparation alone, without baking. Merely setting
lights to Baked does not generate lighting; missing night lightmaps are reported
in the Console. Build only after the bake has finished. Graphics Settings retain
non-directional lightmap variants because the baked scenes are not in the build list.

## Scope

These maps cover fixed environment geometry and its original terrain. Dynamically
placed track tiles and user-edited terrain are not part of the environment bake;
lights cannot bake illumination onto geometry which did not exist during baking.
Vehicles continue to use the existing ambient/sun and their own lights. The tool
does not bake or install light probes for vehicles. A per-track bake would be needed
to include arbitrary track layouts in the lamp lighting.
