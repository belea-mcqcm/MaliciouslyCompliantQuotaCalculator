# Maliciously Compliant Quota Calculator Mod

### Please report any issues [here](https://github.com/belea-mcqcm/MaliciouslyCompliantQuotaCalculator/issues).

This is a terminal command mod for Lethal Company that calculates and outputs the optimal list of scrap to sell to fulfill the quota/certain credits target with the smallest surplus and some other things like: how much value is on the ship currently or how much is needed to meet the threshold required.

## The `mcqc` command

>Can be found in the 'other' section.

Running `mcqc` outputs what ship-scrap needs to be sold to fulfill the remaining quota at 100% buy rate (final day's buy rate).

Running `mcqc <target>` outputs what ship-scrap needs to be sold to have at least `<target>` credits _and_ fulfill the quota at 100% buy rate (final day's buy rate).

Running `mcqc today` outputs what ship-scrap needs to be sold to fulfill the remaining quota at that day's buy rate.

Running `mcqc today <target>` outputs what ship-scrap needs to be sold to have at least `<target>` credits _and_ fulfill the quota at that day's buy rate.

>NOTE: When calculating for a `<target>`, overtime is taken into consideration. Subtotals might appear smaller than needed at first glance.

>For example, if the input is `mcqc 550`, the command will calculate how much and what scrap to sell to get to at least 550 total credits with what you already have, **not** to get an _extra_ 550. 

The commands output some other relevant information:
* How much was needed to meet the threshold required: `Quota/Target left`;
* If today is entered, then what that threshold actually is at that day's buy rate: `In today's rate`;
* Total scrap value on ship: `Total value on ship`;
* Total scrap value on ship after selling everything on the list: `Total value after sale`
* Total value needed to meet threshold, if current scrap isn't enough: `Total value needed`.

## Config file options

`OutputSpacing` controls how much space there is between the scrap name column and the scrap value column, 15 being the smallest and 30 being the biggest.

`IncreasingOrder` decides what happens to two items of the same type when displayed in the list.

## Installation

### Thunderstore
This mod can be automatically installed through the Thunderstore mod manager.

### Manual
1. Install [BepinEx](https://docs.bepinex.dev/articles/user_guide/installation/index.html);
2. Run Lethal Company at least once with BepinEx installed to generate the necessary folders;
3. Manually install [TerminalApi](https://thunderstore.io/c/lethal-company/p/NotAtomicBomb/TerminalApi/);
4. Unzip this mod into the `LethalCompany/BepinEx` folder.
