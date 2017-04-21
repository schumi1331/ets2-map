## Euro Truck Simulator 2 / American Truck Simulator Map

This repository is middle-ware for software developers to integrate ETS2/ATS maps into their widgets. It will also help with building a route advisor.

At the current state this library is ALPHA, meaning that it can read the map but with some quirks and limitations. There is work required to make it easy to use, fast to load or adaptable to mods.

Please see the ticket system to review any known issues or work that is left open.

The demo application can be used to check if exported files from ETS2/ATS are set up correctly, and how the map can be rendered onto a widget.

### Assets

ETS2/ATS maps are built basically by 2 main assets:

- Roads
    - These of course is the stuff you drive on.
- Prefabs
    - These are "prebuilt packages" that can be companies, garages, but most importantly junctions and other "road glue" for map editors to create wicked roads.

All other objects in the game are auxiliary to navigation maps, and not necessarily needed for generating maps.
However, the following objects may be of interest:
- Services, like rest stops and fuel stations
- Garage & Company prefabs
- Toll gates
- Docks + dock routes

### Supported maps / DLC
- ETS2
    - Base
    - Going East!
    - Scandinavia
    - Vive la France !
- ATS
    - Base
    - Nevada
    - Arizona

### Setting up

In order to set-up for a demo, you need to manually extract the following:

Use the [SCS extractor](http://modding.scssoft.com/wiki/Documentation/Tools/Game_Archive_Extractor) to extract .SCS files.

Easy way to copy all \*.ppd files (command-line) `Robocopy c:\source\ c:\destination\ *.ppd /E`

- Base map
    - Raw map information
        - This is located in base.scs at base/map/europe/ or base/map/usa/
        - Put all \*.base files in europe/SCS/map/ or usa/SCS/map/
    - Prefab information
        - Also located in base.scs, copy all \*.ppd files (Folders required) from base/prefab/ and base/prefab2/ to europe/SCS/prefab/
    - Sii road look files
        - Located in def.scs at def/world/
        - Copy road_look.sii and road_look.template.sii to europe/SCS/LUT/road/


- DLC (Vive la France ! as an example)
    - Raw map information
        - This is located in dlc_fr.scs at dlc_fr/map/europe/
        - Put all \*.base files in europe/SCS/map/
    - Prefab information
        - Also located in dlc_fr.scs, copy all \*.ppd files (with folders) from dlc_fr/prefab/ and dlc_fr/prefab2/ to europe/SCS/prefab/
    - Sii road look files
        - Also Located in dlc_fr.scs at dlc_fr/def/world/
        - Copy road_look.sii and road_look.template.sii to europe/SCS/LUT/road/ (some DLC won't have these)

During initialisation you need to point to these 3 directories. In the demo this is done at Ets2MapDemo.cs. Relocate the project map to where you will be using this GIT repository.

#### Folder stucture
```
projectFolder
├───europe
│   ├───LUT
│   │       cities.json
│   │       prefabs.json
│   │       roads.json
│   └───SCS
│       ├───LUT
│       │   └───road
│       │           road_look.sii
│       │           road_look.template.sii
│       ├───map
│       │       all .base files
│       └───prefab
│           ├───cross
│           │       hw2-2_x_city2-1_small.ppd
│           └───eurotunnel
│                   platform.ppd
│
└───usa
│   ├───LUT
│   │       cities.json
│   │       prefabs.json
│   │       roads.json
│   └───SCS
│       ├───LUT
│       │   └───road
│       ├───map
│       └───prefab
```

The LUT files are shipped in this repository. These files are translation tables to convert the game ID's to prefab names.

The loading of SCS Europe map requires some time. The mapper loads all sectors at once and keeps objects in memory. The map parser also uses a fail-safe method of searching for items which is rather CPU intensive. Therefor the loading process is paralleled to all threads your machine has, and will keep them busy at 100% for a brief time. On a Intel i5 3570 machine it takes 10 seconds to load up the map in Debug mode, and about 7 seconds in Release mode.
