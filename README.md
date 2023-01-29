# Zeus
Zeus is the successor of Thunderman. This plugin allows minis to be installed from thunderstore midgame. The minis are limited to packs that contain a zeus file in their mod. The backend database updates every 30 minutes. Currently the proccess is synchronise so it will be little slow when loading new asset packs. Aysnc will be in a future day.

## Install
Currently you need to either follow the build guide down below or use the R2ModMan. 

## Usage
Once an asset is loaded onto the board, the plugin will check if that asset is available on thunderstore. Upon doing so the user will be prompted asking if they want to opt in with auto-mod downloading. Upon so the plugin will synchronously download and inject the assets into TaleSpire via CAL. Upon succesfull injection, the missing minis will load in correctly.

## Changelog
- 1.3.0: Upgrade to net48 and pipeline deployment.
- 1.2.1: Force never rebuild for CALP during pack building.
- 1.2.0: Updated to be async for downloads to increase performance 
- 1.1.0: uses zeus file to register asset packs instead
- 1.0.1: Update Icon (from flaticon)
- 1.0.0: Initial release

## Shoutouts
Shoutout to my Patreons on https://www.patreon.com/HolloFox recognising your
mighty contribution to my caffeine addiciton:
- John Fuller
- [Tales Tavern](https://talestavern.com/) - MadWizard