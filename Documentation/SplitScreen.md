# Local split screen

## Scene UI wiring

`SplitScreenSelector` calls `base.OnEnable()` and uses the existing TrackSelector
track-loading pipeline and assigned `trackImageTemplate`. Create the shared layout
in the editor and assign its public fields. Missing optional
references are skipped. The component adds runtime listeners while enabled and
removes only its own listeners on disable, preserving editor events. Assigning a
button field is sufficient: do not also add the same handler to its OnClick list.

- Assign `thisView` to the existing MainMenuView.
- Populate `playerRows` with up to ten entries, in player order (index 0 = player 1).
  Each entry exposes its existing `root`, `nameInput`, `scoreText`, `carText`,
  `controllerText`, `keyboardProfileText`, and `carButton`.
  Device/profile information is read-only and comes from registration.
  Unused row roots are hidden. Configure layout/scrolling/navigation in the editor.
- Assign the desired race buttons: `playersButton`, `raceButton`, `lapsButton`,
  `raceTypeButton`, `rivalsButton`, `timeOfDayButton`, `randomCarsButton`,
  `randomTracksButton`, `scoringButton`, `cpuLevelButton`, `catchupButton`,
  `roadTypeButton`, `backButton`. Assign the existing corresponding text fields
  (including those inherited from TrackSelector) and optional `statusText`.
- Configure the inherited TrackSelector fields: `trackImageTemplate`, `trackContent`,
  `scrollx`, `scrolly`, `startButton`, `trackDescText`, `trackAuthorText`,
  `tilesContainer`, `recordsContainer` and optional `radial`/`sortButton`.
  Track loading, sorting, grid navigation, description, records and scrolling use
  the shared implementation. Split screen excludes skate parks and locked/invalid
  tracks. Its virtual reset hook updates the local race controls.
- Assign `garageBtn` and `garageView` for the shared garage. `carSelector` can be
  assigned explicitly; otherwise it is resolved within the assigned garage view.
  In Demo, `garageView` points at the existing GarageView.
- All key rebinding is performed in RemapView. SplitScreenSelector has no key
  editor, key capture buttons, device cycling or profile reassignment controls.

## Controls and race

Select 2-10 local players. Hold the menu Shift modifier while clicking a cycling
option to decrease it. Names must be unique. Gamepads/joysticks are assigned to
one player each; keyboards can be shared using different keyboard profiles.
The total number of local players and CPU rivals cannot exceed ten.

Profiles 1-4 default to arrows, WASD, IJKL and the numeric keypad. Profiles 5-10
start unassigned. Use RemapView to edit each set; Escape cancels capture. Four driving keys are required before starting.
Changes persist immediately in PlayerPrefs (`SplitScreen.Keyboard.<index>`),
without changing menu or single-player controls. Assign distinct keys across
profiles; hardware keyboard rollover limits still apply.

Menus stay shared. At race start, `Info.playerInputs[0..9]` contains independent
input instances, with one camera and HUD per active player. Two players use
horizontal halves, three a full-width top view and two bottom views, four a 2x2
grid. Five/six use two rows and seven through ten use three rows, filling each row.
Only the primary camera has an audio listener. Shared Escape toggles pause once.

The first finisher starts the finish timeout. Shared results appear after all
local players finish or the timeout expires. Session scores remain in the lobby.
Leaving/restarting removes race inputs, extra cameras and HUDs and restores the
original camera/UI hierarchy. Race-time viewports are independent of lobby UI.

## RemapView input sets

Assign the existing keyboard/controller button parents to `RemapView.buttonContainers`.
Their `RebindActionUI` components retain the action/binding IDs assigned in the Inspector.
`ToggleSet(-1)` and `ToggleSet(1)` switch between input sets 1-10 and update the player icon.
The same existing buttons display/edit the selected set; no UI is created.

All ten sets edit private copies. When the view closes, set 1 is also applied to
the normal input asset and saved as `rebinds`. Every set is persisted under `SplitScreen.InputSet.<index>`; keyboard bindings
are synchronized with the corresponding split-screen keyboard profile. At race start,
each local player receives the saved set selected by their profile index (by default,
player 1 uses set 1, player 2 uses set 2, etc.). Switching sets cancels a pending capture.

`RemapView.keyboardMode` follows the last keyboard/controller input and selects
only the device used when starting the next capture. Mouse input does not change
it. Keyboard/controller navigation never changes BindingText or the displayed
binding group. The device displayed by each set is saved separately under
`SplitScreen.InputSetDevice.<index>` after a successful rebind. Cancelling a rebind
restores the previous display. Switching sets restores its saved device and keys.

`ShowSet()` assigns each RebindActionUI an atomic input-set context (private asset,
set index, binding ID). Display updates and interactive captures use that context.
BindingText reads the selected set's effective binding path, independently of other
players sharing the same physical keyboard.

The four built-in keyboard presets have no shared assigned keys, including pause:
Escape, Tab, Backquote and Num Lock respectively. Profiles 5-10 are completely
unassigned. Only unchanged legacy presets are automatically upgraded; customized
profiles are preserved.

## Joining from PreSplitScreenView

The existing `SplitScreenEntryb` OnClick can continue to call
`MainMenuView.GoToView(enterNameView)`. The existing PreSplitScreenView component
supplies its `splitScreenSelector` reference. `splitScreenEntryBtn` is disabled
until at least two players are selected (the count can be reduced to zero).

The selector uses `enterNameView`, `Icon1P2P`, and `PlayerIcons1P2P` to register one
player at a time. Optionally assign `registrationNameInput`; if omitted it uses
the existing EnterNameInputField under the assigned name view. No buttons are
created. The first icon is index 0, then index 1, etc.

Type a name and press the UI Submit control saved in an unused input set. The
physical device and that set index become the player's assignment. A gamepad
cannot join twice. One keyboard can join through different Submit keys/sets.
Default keyboard Submit keys are Enter for set 1 and digits 1-9 for sets 2-10;
these can be changed with the existing Submit rebind button. Previously saved
explicit Submit overrides are imported. Mouse clicks on OK do not assign players.
Names must be nonempty and unique.

Each successful confirmation advances the icon and resets/restores the next name.
After the required number of confirmations the existing SplitScreenView opens.
Back/Escape restores the previous name and releases that player's device/set;
Back from the first name returns to PreSplitScreenView. Name registration does
not overwrite the main single-player name. Registered race PlayerInputs are
still created only at race start.

## Active menu player and garage

While in SplitScreenView, each registered player's navigation/Submit actions are
read from a private copy of their registered input set, restricted to their device.
Keyboard menu navigation defaults to that set's driving directions unless an
explicit UI Move override was saved in RemapView. The player icon follows the
player navigating or submitting. Mouse input retains the most recent player.

Submitting the selected Garage button opens the garage for that player. The
owner remains fixed until the garage closes; other players cannot confirm their
purchase. Keyboard Submit keys beyond the main Enter also work. Only the owner's
input controls car browsing. Confirming returns directly to SplitScreenView,
without sponsor selection. Back cancels a preview without changing the car.

For Championship scoring, each player has a separate spendable `cash` balance;
`score` remains the cumulative result. Rewards increase both. Affordable cars are
filtered using that player's funds plus trade-in (the same fraction as
Championships), and the current car is always available. Funds and Price are shown
in the garage description. A purchase changes only its owner's car and cash.
Other scoring modes use unlocked cars without charging. Old sessions initialize
cash from their prior score; newly added players start with zero cash.

In RemapView, Shift + Delete clears the bindings represented by currently active
RebindActionUI buttons in `buttonContainers`, for the selected player set and its
displayed device. Hidden containers and other player sets are unaffected. Composite
buttons clear their component bindings. The operation cancels any pending capture,
saves immediately, and refreshes the labels. Plain Delete during capture clears
only that capture's binding.

## Local leaderboard and activity

Assign `leaderboard` and `ShortLeaderboardRowPrefab` on SplitScreenSelector. The
existing table keeps its title and reuses short prefab rows for local drivers.
Rows show player number/name, localized car name (or Random), and score; Championship
scoring shows current funds. Rows sort by the displayed value, with player order
breaking ties. Active-player text is white; other rows are grey.

Refreshing lobby data updates the table without recreating every row. Input events
from registered controllers and the registered keyboard navigation/Submit/driving
keys update the active player icon and table highlight. Releases, small axis drift,
mouse activity and unregistered devices do not claim the active-player indicator.
Garage controls remain locked to the garage owner.
