# Original track surface material values

`OriginalTrackSurfaceMaterials.csv` contains the material records referenced by collision polygons in the 29 original `track*.PMD` files.

## CSV columns

- `pmd_file`: source PMD file.
- `material_index`: zero-based index into that PMD's collision material table.
- `polygon_references`: number of collision polygons that use this material index.
- `flags_hex`, `flags_byte`: original flags byte. The low nibble is the contact type; the remaining bits are retained as authored.
- `contact_type_low_nibble`: `flags_byte & 15` before the original track-mode overrides.
- `grip_byte`: original grip byte.
- `grip_scalar_raw`: grip as read by the game, `grip_byte / 64`.
- `grip_scalar_after_type10_override`: effective grip after the game's special type-10 rule. For type 10, the game changes the contact type to 1 and grip to 4.
- `rolling_byte_raw`, `roughness_byte_raw`: original bytes. They are not normalized in the PMD loader.

The original loader reads a four-byte material record in this order: flags, grip, rolling, roughness. Collision polygons are eight-byte records; their final 16-bit field is the material-table index. The CSV includes only material records actually referenced by at least one collision polygon.

The grip conversion and type-10 behavior were checked against `C:\Users\bernz\Desktop\SGP Reversed\src\track_contact.cpp` (`apply_material`). The PMD record layout and section references were checked against the same file's track collision decoder (`decode_sections` and `load_track_collision`). In `apply_material`, track modes 1 and 6 also change contact type 1 to 3 and 4 respectively; that affects contact behavior, not the three raw surface coefficients listed here.

These are exact source material values keyed by PMD material index. They do not yet identify which Unity `GroundSurfaceMaster` entry or merged-collider submesh corresponds to each PMD index. The CSV is an offline reference and does not add PMD files to runtime track contact handling.
