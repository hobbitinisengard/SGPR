# Por?wnanie kamery i opon: Unity ? SGP Reversed

## Unity

W CameraControl zaznacz `Measure Simulation Timing` i `Write Comparison Csv`.
Zapis startuje przy ?ledzeniu auta i domy?lnie trwa 60 sekund czasu rzeczywistego.
Konsola `[PhysicsComparison]` podaje pe?n? ?cie?k? do katalogu:
`Application.persistentDataPath/PhysicsComparison/unity-...`.
Powstaj? `camera.csv`, `tyres.csv` i `capture.txt`. Po zako?czeniu pomiaru lub
wyj?ciu z Play Mode pliki s? zamykane. Wy??cz i w??cz `Write Comparison Csv`,
?eby rozpocz?? kolejny zapis. Console `[SimulationTiming]` ma te? ?rednie b??dy,
wsp??czynnik doganiania oraz ?redni/maksymalny obr?t kamery na krok.

Pomiar nie zmienia cz?stotliwo?ci symulacji ani wsp??czynnik?w fizyki. CSV jest
buforowane i nie wypisuje ka?dego kroku do konsoli; jednak diagnostyka ma koszt
CPU/GC i nie powinna s?u?y? do benchmarku wydajno?ci. Dzia?a w Editor/Development Build.

## Oryginalny runtime

?r?d?a: `C:/Users/bernz/Desktop/SGP Reversed`.
Dodano opcjonalny zapis aktywowany zmienn? ?rodowiskow? `SGPR_COMPARISON_DIR`.
Bez tej zmiennej CSV nie powstaje. Instrumentacja kamery obejmuje zar?wno
`retail_camera.cpp`, jak i rzeczywisty dispatch w `source_camera_dispatch.cpp`.
Dane opon s? pobierane wewn?trz `vehicle.cpp::tyre_friction`, w kontek?cie
`vehicle_dynamics_tick`, tylko dla slotu 0. S? to warto?ci z dzia?aj?cego runtime,
a nie wynik niezale?nego przepisania wzor?w.

Wymagania: Visual Studio 2022 **Desktop development with C++**, MSVC x64,
Windows SDK i CMake tools for Windows. Tych narz?dzi C++ nie wykryto na bie??cej
maszynie, wi?c orygina? z pomiarem nie zosta? tutaj skompilowany.

Uruchom w PowerShell z katalogu `D:/source/repos/SGPR`:

```powershell
& .\Tools\PhysicsComparison\Build-Original.ps1
& .\Tools\PhysicsComparison\Run-Original.ps1
```

Budowany jest tylko target gry `StuntGP_NEW`, bez uruchamiania test?w.
Nowy EXE trafia do `SGP Reversed/tools/physics-comparison/out/RelWithDebInfo`;
istniej?cy `StuntGP_NEW-R1/StuntGP_NEW.exe` nie jest nadpisywany i nie ma tych sond.
Skrypt uruchamia nowy EXE z danymi `StuntGP_NEW-R1/data/retail`. Gdy dane s? gdzie
indziej, podaj `-AssetsRoot`, czyli katalog zawieraj?cy `data/retail/wads`.

Zagraj wybranym autem, a potem zamknij gr?. Pliki pojawi? si? w:
`SGP Reversed/tools/physics-comparison/captures/original-.../`.
Parametry `-Executable`, `-OutputDirectory` i `-SourceRoot` pozwalaj? zmieni? ?cie?ki.
Kopie plik?w C++ sprzed dodania sond s? w
`SGP Reversed/tools/physics-comparison/before-instrumentation/`.

## Przebieg pomiaru

W obu grach wybierz to samo auto, te same cz??ci, tor i zwyk?y tryb Race.
Unity `car0` odpowiada oryginalnemu `car1.cfg`. U?yj najbli?szej kamery (preset 1).
Najpierw kilka sekund postoju, nast?pnie jazda prosto i spokojne zakr?ty,
a potem ostrzejsze zakr?ty. Nie u?ywaj dopalacza, trickstartu, hamulca r?cznego
ani ewolucji podczas por?wnywania przyczepno?ci. Dodatkowy wyskok mo?na zmierzy?
osobno. Diagnostyka Unity ?ledzi aktualny cel kamery, a oryginalna slot 0.

## Znaczenie danych

`camera.csv`:

- `error_before_m/error_after_m`: dystans do **tej samej** docelowej pozycji
  przed i po kroku; osobno od ruchu auta w ?wiecie.
- `response_ratio`: `1 - error_after/error_before`. Bez ograniczenia pr?dko?ci
  i kolizji powinien zbli?a? si? do 0.5 w presecie 1 lub 0.125 w presetach 2/3.
- `target_error_*`: op??nienie punktu, na kt?ry patrzy kamera.
- `view_turn_deg`: ca?kowita zmiana orientacji widoku na krok, wraz z przechy?em.
- `coasting=1`: specjalne zachowanie w locie; b??d pozycji i odpowied? maj? NaN,
  bo w tej ga??zi orygina? nie dogania zwyk?ej pozycji docelowej.
- `limit_m`: limit przesuni?cia na krok; `lens`: oryginalny parametr projekcji.

`tyres.csv`:

- Ko?a w kolejno?ci ?r?d?owej: 0=RL, 1=RR, 2=FL, 3=FR.
- Pr?dko?ci i korekty maj? m/s; `mass_source`, `load_source`, `combined_slip_source`,
  `available_source` i `threshold_source` celowo zachowuj? jednostki oryginalnych wzor?w.
- `normalized_load`, `grip`, `static_coeff/kinetic_coeff`, `curve_value` pozwalaj?
  oddzieli? b??d wsp??czynnik?w od b??du obci??enia/po?lizgu.
- `static_coeff/kinetic_coeff` zawieraj? ju? mno?nik grip nawierzchni;
  `config_static/config_kinetic` to podstawowe wsp??czynniki cz??ci.
- `sliding=1`: ga??? tarcia kinetycznego, `grip_usage > 1` oznacza przekroczenie progu.
- `drive_delta_m_s`: korekta pr?dko?ci ko?a przed sumowaniem si? osi, NIE si?a w N.
  Orygina? operuje tu korektami pr?dko?ci na krok. `lat_after_m_s` to boczna
  pr?dko?? pozosta?a po zastosowaniu tarcia.
- Ko?a bez wystarczaj?cego obci??enia (load <= 8.515625) nie maj? wiersza tarcia.

## Analiza

```powershell
python .\Tools\PhysicsComparison\compare.py 'KATALOG_UNITY' 'KATALOG_ORIGINAL' --min-speed 80 --max-speed 120
```

Opcjonalnie `--wheel 2` ogranicza opony do przedniego lewego ko?a.
`--contact-class 0` (domy?lne) ogranicza kamer? do pe?nego kontaktu.
Skrypt podaje ?redni?, median? i p95, oraz procent przej?cia opon do tarcia kinetycznego.
Dodatkowo odtwarza **sam wz?r tarcia** z zarejestrowanych wej??, aby wskaza?
r??nic? mi?dzy r?wnaniem ?r?d?owym a zapisanym wynikiem. Ten rachunek Pythona
nie jest wykonaniem oryginalnego EXE ani ca?ej fizyki; drobne r??nice wynikaj?
z zaokr?gle? CSV/float32, a tu? przy progu tak?e wyb?r ga??zi mo?e si? r??ni?.
Tryb Drift ma celowo zmieniony bud?et przyspieszania i nie s?u?y do tej pr?by.

Identyfikatory tick?w i pozycje z dw?ch niezale?nych przejazd?w NIE oznaczaj?
identycznego stanu wej?ciowego. Por?wnuj te same cz??ci, zakresy pr?dko?ci,
sterowanie, nawierzchnie i obci??enia. Same r??nice ?rednich nie dowodz? b??du.

python .\Tools\PhysicsComparison\compare.py 'C:/Users/bernz/AppData/LocalLow/viatrufka/Stunt GPR\PhysicsComparison\unity-20261009-214546-458' 'C:\Users\bernz\Desktop\SGP Reversed\tools\physics-comparison\captures\original-1791579601265' --min-speed 80 --max-speed 120

## Dodatkowy pomiar po pustym camera.csv

Nowy zapis orygina?u zawiera te? `camera_modes.csv` z aktywnymi trybami/slotami.
Dla tryb?w mounted/spline/authored zapisujemy ruch i obr?t widoku, ale b??d zwyk?ej
pozycji chase pozostaje NaN. Przebuduj EXE przez Build-Original.ps1.
Por?wnanie `--grip 1` ogranicza opony do tej samej przyczepno?ci nawierzchni.
`--camera-slot N` wybiera cel kamery; domy?lne 0.
