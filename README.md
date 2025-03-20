# All Quests Checkmarks
A mod for SPT that overhauls quest checkmarks for items by checking for future quests and slightly changing how default logic checks for quest items. It also adds detailed description to tooltips that show: how many items are currently in stash, total required items for all quests, active, future quests and other players who need this item (FIKA users only). This mod works only for quest types of: handover item, find item and leave item at Location. Markers, Wi-Fi Cameras and Jammers are completely excluded. This mod also works with FIKA and marks items required for quests of your squad members. This mod completely overrides default logic of checkmarks so other mods that also alter checkmarks will most likely not work!

More detailed description and screenshots are available on [SPT Hub](https://hub.sp-tarkov.com/files/file/2705-all-quests-checkmarks/).

## Changed Logic
* If selected in config, this mod will also mark non-FiR items that are required for active quests with default yellow checkmark.
* Items required for upcoming quests are marked with checkmark of different color (also includes non-FiR items if selected in config).

## Default Colors
* **Magenta** - Item found in raid and is required for future quests
* **Dark purple** - Item **not** found in raid but is required for future quests
* **Light red** - Neither of above, but is required for at least one active quest of your squad members (**FIKA only**)

## Installation
1. Make sure that both SPT Client and SPT Server are not running
2. Head to [releases page](https://github.com/danx91/AllQuestsCheckmarks/releases)
3. Download correct version for your SPT
4. Open zip file
5. Drag and drop `BepInEx` and `user` folders to your SPT directory
6. Start server and client and make sure that mod is working

## Config
You can access config while in-game by pressing `F12` key and then selecting `AllQuestsCheckmarks` tab

### General
* **Include Collector quest (Fence)** - Whether or not to include items needed for Collector quest
* **Include loyalty regain quests** - Whether or not to include quests for regaining loyalty (Compensation for Damage (Fence), Make Amends (Lightkeeper) & Chemical questline finale)
* **Include non-FiR quest** - Whether or not to include quests that don't require found in raid items
* **Hide checkmark if have enough (in raid)** - Whether or not to hide checkmark in raid on items that you have enough for all active and future quests. Be careful when using with 'Include items in PMC inventory (in raid)', as this combo may hide checkmarks while still in raid!
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

## Translating
Detailed info about translating can be found [here](AllQuestsCheckmarks-Core/locales/).

## Building from source
* Client Core:
	* Open [solution](AllQuestsCheckmarks-Core/AllQuestsCheckmarks.sln) in Visual Studio
	* Make sure you are using `Release` mode (only if you are going to create zip bundle)
	* Build binaries: `Build > Build Solution` or press `F7`
* Client Fika Module:
	* Open [solution](AllQuestsCheckmarks-Fika/AllQuestsCheckmarks-Fika.sln) in Visual Studio
	* Make sure you are using `Release` mode (only if you are going to create zip bundle)
	* Build binaries: `Build > Build Solution` or press `F7`
* Server:
	* No need to build
* Bundling everything into zip:
	* Adjust settings in [build.json](build.json)
	* Open `cmd` in root directory
	* Run command `node .\build.mjs`
	* Your zip bundle will appear in `dist` folder

## Credits
* Server side part is based on examples provided by [Single Player Tarkov](https://github.com/sp-tarkov/mod-examples/tree/master).
* Basic ideas about code and where to start inspired by [TommySoucy's MoreCheckmarks mod](https://github.com/TommySoucy/MoreCheckmarks).
* Special thanks to Jehree for great [tutorial how to setup Visual Studio](https://hub.sp-tarkov.com/doc/entry/89-client-modding-quick-start-guide/).

## License
Copyright © 2025 danx91 (aka ZGFueDkx)
This software is distributed under GNU GPLv3 license. Full license text can be found [here](LICENSE).

If you believe that this software infringes yours or someone else's copyrights, please contact me via Discord to resolve this issue: **danx91**.