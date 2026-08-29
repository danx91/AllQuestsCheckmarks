# All Quests Checkmarks
A mod for SPT that overhauls quest checkmarks for items by checking for future quests and slightly changing how default logic checks for quest items. It also adds detailed description to tooltips that show: how many items are currently in stash, total required items for all quests, active, future quests and other players who need this item (FIKA users only). This mod works only for quest types of: handover item, find item and leave item at Location. Markers, Wi-Fi Cameras and Jammers are completely excluded. This mod also works with FIKA and marks items required for quests of your squad members. This mod completely overrides default logic of checkmarks so other mods that also alter checkmarks will most likely not work!

__More detailed description and screenshots are available on [SPT Forge](https://sp-mod.com/mod/2025/all-quests-checkmarks).__

## Looking for translators
I'm looking for people who are willing to provide translation for this mod. If you want to contribute to this mod by traslating it, please head to the [locales](/AllQuestsCheckmarksClient/locales/) directory in `AllQuestsCheckmarksClient` and follow instructions inside `README.md` file.

## Installation
1. Make sure that both SPT Client and SPT Server are not running
2. Head to [releases page](https://github.com/danx91/AllQuestsCheckmarks/releases)
3. Download correct version for your SPT
4. Open zip file
5. Drag and drop `BepInEx` and `SPT_Runtime` folders to your SPT directory
6. Start server and client and make sure that mod is working

## Building from source
To build the project from source, follow these steps:
1. Clone or download this repository
```
git clone https://github.com/danx91/AllQuestsCheckmarks.git
```
*(Alternatively, download the ZIP and extract it.)*

2. Download or clone the required [Common Library](https://github.com/danx91/SPT-ZGFueDkxCommonLibrary)
```
git clone https://github.com/danx91/SPT-ZGFueDkxCommonLibrary.git
```
Place it in a directory of your choice.

3. Adjust project references
Update the .csproj files to ensure that project and/or assembly references correctly point to the Common Library and SPT binaries location on your system.

4. Build the solution
Open the solution in Visual Studio and build it, or run:
```
dotnet build
```

## Config
You can access config while in-game by pressing `F12` key and then selecting `AllQuestsCheckmarks` tab

### General
* **Include Collector quest (Fence)** - Whether or not to include items needed for Collector quest
* **Include non-FiR quests** - Whether or not to include quests that don't require found in raid items
* **Include loyalty regain quests** - Whether or not to include quests for regaining loyalty (Compensation for Damage (Fence), Make Amends (Lightkeeper) & Chemical questline finale)
* **Include unreachable quests** - Whether or not to include quests that are unreachable (event quests and quests for other account types)
* **Hide checkmark if have enough (in raid)** - Whether or not to hide checkmark in raid on items that you have enough for all active and future quests. Be careful when using with 'Include items in PMC inventory (in raid)', as this combo may hide checkmarks while still in raid!
* **Show only active quests** - Whether or not to show only active quests (no future quests)
* **Include items in PMC inventory (in raid)** - Whether or not to include items in PMC inventory while in raid in 'In Stash' count
* **Mark squad members quests** - Whether or not to mark items currently needed for players in your squad (**FIKA only**)

### Colors
* **Checkmark color** - Color of checkmark if item is not currently needed but is required for future quests<sup>*</sup>
* **Checkmark color (non-FIR)** - "Color of checkmark if non-FiR item is not currently needed but is required for future quests<sup>*</sup>
* **Collector color** - Color of checkmark for collector quest<sup>*</sup>
* **Use different color if have enough** - Whether or not to use different checkmark color if you have enough items for all quests. 'Hide checkmark if have enough' option will hide this checkmark while in raid
* **Have enough color** - Color of checkmark if you have enough items for all quests<sup>*</sup>
* **Use custom quest checkmark color** - Whether or not to use custom checkmark color for active quests
* **Custom quest color** - Custom color of default quest checkmark<sup>*</sup>
* **Checkmark color (squad members)** - Wether or not to mark items currently needed for players in your squad<sup>*</sup> (**FIKA only**)

### Text
* **Use bullet points** - Whether or not to use bullet points in quests list
* **Use custom text colors** - Whether or not to use custom text colors
* **Custom text color - active quests** - Custom color of active quests text<sup>*</sup>
* **Custom text color - future quests** - Custom color of future quests text<sup>*</sup>
* **Custom text color - squad quests** - Custom color of squad quests text<sup>*</sup>

<sup>*</sup> - accepted color formats are: HEX `#RRGGBB` or RGB (0-255 range) `RRR,GGG,BBB` (e.g. #FF00FF or 255,0,255)

## Credits
* Server side part is based on examples provided by [Single Player Tarkov](https://github.com/sp-tarkov/mod-examples/tree/master).
* Basic ideas about code and where to start inspired by [TommySoucy's MoreCheckmarks mod](https://github.com/TommySoucy/MoreCheckmarks).

## License
Copyright © 2026 danx91 (aka ZGFueDkx)

This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License along with this program. If not, see https://www.gnu.org/licenses/.

If you believe that this software infringes yours or someone else's copyrights, please contact me via Discord to resolve this issue: **danx91**.