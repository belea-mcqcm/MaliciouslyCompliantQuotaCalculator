# Maliciously Compliant Quota Calculator Mod

This is a terminal command mod for Lethal Company that calculates and outputs the optimal list of scrap to sell to fulfill the quota with the smallest surplus (and some other things).

## The `mcqc` command

>Can be found in the 'other' section.

Running `mcqc` outputs what ship-scrap needs to be sold to fulfill the remaining quota at 100% buy rate (final day's buy rate).

Running `mcqc today` outputs what ship-crap needs to be sold to fulfill the remaining quota at that day's buy rate.

Running `mcqc <num>` outputs what ship-scrap needs to be sold to reach `num` credits at 100% buy rate.

Running `mcqc today <num>` outputs what ship-scrap needs to be sold to reach `num` credits at that day's buy rate.

>If you have 500 credits and you input `mcqc [today] 700`, for example, the command will calculate what scrap you need to sell to get 200 credits, not 700.

## Config file options

`OutputSpacing` controls how much space there is between the scrap name column and the scrap value column, 15 being the smallest and 30 being the biggest.

`Verbosity` refers to how much/how detailed the outputted information is.

`IncreasingOrder` decides what happens to two items of the same type when displayed in the list.

## Installation

### Thunderstore
This mod can be automatically installed through the Thunderstore mod manager.

### Manual
1. Install BepinEx;
2. Run Lethal Company at least once with BepinEx installed to generate the necessary folders;
3. Manually install TerminalApi;
4. Unzip this mod into the `LethalCompany/BepinEx` folder.
