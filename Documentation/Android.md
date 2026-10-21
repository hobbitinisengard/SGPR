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

- Right thumb: left/right below gas/brake, with direct multitouch steering.
- Gas/brake: upper right. Sliding off GAS releases the throttle.
- Left thumb: hold NITRO and SHIFT. Nitro requires throttle and no brake.
- Options > controls opens the existing RemapAndroidView on Android. Drag the race
  buttons to reposition them, then use Save and Back (or leave the view normally).
  Positions are stored in PlayerPrefs relative to the screen safe area and restored
  on the next launch. Reset Positions restores the default layout.
- The same view switches NITRO between hold and toggle modes; the choice is saved.
  Toggle mode is armed by tapping, but boost still requires throttle and no brake.
- ESC/CAM: upper left; R below them resets the car using the normal cooldown.
- Tap anywhere on the race results overlay to proceed without Enter.
- Menus: tap existing buttons and selector images. Tapping an already selected
  car/part again confirms a championship purchase.
- Menu ENTER sits above UP at the right edge and submits the selected menu item.
- Menu arrows navigate selectors; UI SHIFT / UI CTRL / UI TAB are sticky modifiers
  for options such as lap count. Tap again to release a modifier.
- EnterNameView opens the native keyboard only after tapping the name field.
- Android Arcade and Championships races spawn at most three CPU rivals. Championship
  saves retain the full roster for compatibility with desktop; only CP1-CP3 race on Android.
- Track editor entry is available regardless of connected input devices. Editing
  controls still use a physical keyboard and mouse.

Runtime and editor C# compilation completed with zero errors. Device validation
is still required: APK installation, rendering/materials, native
keyboard, simultaneous touches, menu purchases, external keyboard/mouse, audio and
online services. No Android player build or device test was run as part of this change.
