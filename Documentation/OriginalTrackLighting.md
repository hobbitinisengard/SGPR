# Oświetlenie otoczenia oryginalnych tras Stunt GP

Źródło wartości: `C:\Users\bernz\Desktop\SGP Reversed\assets\config\levels\trackN.cfg`, sekcje `-Ambience[]` i `-FogColour[]`.

Intensywność ambientu = I / 255 (zaokrąglona do sześciu miejsc). Ambient RGB we wszystkich poniższych konfiguracjach wynosi (255, 255, 255). FogColour podano jako RGB w zakresie 0–255 i HEX; sam kolor nie określa gęstości mgły.

Nazwy tras i kraje powiązano z identyfikatorami trackN na podstawie wpisów w Assets/Info.cs.

| Konfiguracja | Trasa | Kraj | I | Intensywność I/255 | FogColour RGB | FogColour HEX |
|---|---|---|---:|---:|---|---|
| track1.cfg | CRAZY STRAIGHTS | Francja | 75 | 0.294118 | (255, 128, 0) | #FF8000 |
| track2.cfg | BANK JOB | Japonia | 127 | 0.498039 | (255, 128, 0) | #FF8000 |
| track3.cfg | TUBULAR HELL | Japonia | 127 | 0.498039 | (255, 128, 0) | #FF8000 |
| track4.cfg | CURBED HEIGHTS | Francja | 127 | 0.498039 | (255, 128, 0) | #FF8000 |
| track5.cfg | FLYING FINISH | Włochy | 104 | 0.407843 | (255, 128, 0) | #FF8000 |
| track6.cfg | SECRET SIX | Hiszpania | 127 | 0.498039 | (255, 128, 0) | #FF8000 |
| track7.cfg | THE SANDWINDER | Hiszpania | 134 | 0.525490 | (255, 128, 0) | #FF8000 |
| track8.cfg | ROUGHDUST FLATS | Hiszpania | 127 | 0.498039 | (255, 128, 0) | #FF8000 |
| track9.cfg | THE LOOPBACK | Wielka Brytania | 127 | 0.498039 | (255, 128, 0) | #FF8000 |
| track10.cfg | INTERSECTOR | Wielka Brytania | 121 | 0.474510 | (255, 128, 0) | #FF8000 |
| track11.cfg | RISE'N'FALL | Hiszpania | 127 | 0.498039 | (255, 128, 0) | #FF8000 |
| track12.cfg | WIDE WALL CHASE | Hiszpania | 127 | 0.498039 | (128, 128, 255) | #8080FF |
| track13.cfg | CURB CITY CIRCUIT | USA | 132 | 0.517647 | (255, 128, 0) | #FF8000 |
| track14.cfg | WATERFRONT DASH | Włochy | 127 | 0.498039 | (255, 128, 0) | #FF8000 |
| track15.cfg | FREEFALL FREEWAY | Hiszpania | 127 | 0.498039 | (128, 128, 255) | #8080FF |
| track16.cfg | HIGH ROLLER | Niemcy | 132 | 0.517647 | (255, 128, 0) | #FF8000 |
| track17.cfg | HIGHFLY CLEARWAY | Japonia | 127 | 0.498039 | (255, 128, 0) | #FF8000 |
| track18.cfg | HELIPAD HEIGHTS | USA | 129 | 0.505882 | (255, 128, 0) | #FF8000 |
| track19.cfg | SUNKEN SIGHTS | Hiszpania | 127 | 0.498039 | (128, 128, 255) | #8080FF |
| track20.cfg | DUST BUSTER | Japonia | 96 | 0.376471 | (255, 128, 0) | #FF8000 |
| track21.cfg | TWINLOOP CIRCUIT | Niemcy | 127 | 0.498039 | (255, 128, 0) | #FF8000 |
| track22.cfg | SNAKESTORM | Niemcy | 127 | 0.498039 | (255, 128, 0) | #FF8000 |
| track23.cfg | SKYTOP SPEED CIRCUIT | USA | 127 | 0.498039 | (255, 128, 0) | #FF8000 |
| track24.cfg | THE CHRONOZONE | Francja | 127 | 0.498039 | (255, 128, 0) | #FF8000 |

## Konfiguracja domyślna i zakres raportu

`default.cfg`: I = 127, intensywność = 0.498039, ambient RGB = (255, 255, 255), FogColour RGB = (255, 128, 0), HEX = #FF8000.

Katalog zawiera konfiguracje track1.cfg–track24.cfg. setup/tracks.csv ma również wpis 25, ale brak osobnego track25.cfg; nie przypisano mu wartości bez potwierdzenia używanej konfiguracji.

## Sposób zastosowania w oryginale

src/race_session.cpp najpierw inicjalizuje oświetlenie z PMD, a następnie stosuje konfigurację poziomu. src/retail_level_lighting.cpp:37 odczytuje I oraz RGB ambientu i FogColour; intensywność jest mnożona przez stałą float 0x3b808081 (około 1/255). src/retail_lighting.cpp:88 mnoży RGB ambientu przez intensywność. Wartości w tabeli są wartościami z konfiguracji, a nie próbkowaniem obrazu ani parametrami oświetlenia Unity.
