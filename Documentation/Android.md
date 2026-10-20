# Android

## Build

- Unity 6000.0.32f1 with Android Build Support, SDK, NDK and OpenJDK.
- Run **Tools > Android > Prepare project**. This sets IL2CPP, ARM64, OpenGL ES 3,
  landscape orientation and Addressables rebuilding with the player.
- Activate the existing Android build profile and build an **APK**. Keep **Split
  Application Binary** disabled. The data installer currently expects assets in
  the base APK; Play Asset Delivery/OBB are not implemented.
- `AndroidStreamingFiles.json` is regenerated before Android builds. Startup copies
  its assets into `Application.persistentDataPath`, preserving existing JSON saves
  and JPEG previews according to the desktop installation rules.
- Desktop build behavior and the editor's keyboard/mouse controls are preserved.

## Touch controls

`MobileControls` is created automatically on Android only. No scene wiring is required.
The overlay uses the screen safe area and independent pointer IDs for multitouch.

- Left thumb: left/right.
- Right index finger: gas/brake at the upper right.
- Right thumb: tap NITRO to toggle it; hold SHIFT for tricks. Direction buttons
  supply the trick direction too.
- ESC: top center, pauses a race or returns from a menu.
- CAM: beside ESC during a race, cycles the follow camera views.
- Tap the continue prompt on the race results overlay to proceed without Enter.
- Menus: tap existing buttons and selector images. Tapping an already selected
  car/part again confirms a championship purchase.
- Menu arrows navigate selectors; UI SHIFT / UI CTRL / UI TAB are sticky modifiers
  for options such as lap count. Tap again to release a modifier.
- EnterNameView opens Android's native keyboard; finish typing and tap the usual OK button.
- Track editor entry requires an actual keyboard and mouse. The virtual menu
  keyboard does not satisfy this requirement.

Runtime and editor C# compilation completed with zero errors. Device validation
is still required: APK installation, rendering/materials, native
keyboard, simultaneous touches, menu purchases, external keyboard/mouse, audio and
online services. No Android player build or device test was run as part of this change.
