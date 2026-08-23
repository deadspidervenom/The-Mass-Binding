#AI-Generated Tag recent addition Note

This will be a long section so i will include a TL;DR but i ask you, if you like this mod or dont want to crusify me unjustly please read all of it.

Before i go on, in this project i did not use Gen AI art, audio, or video in anything. I oppose the usage of such things. Now on to the TL;DR

TL;DR: 2 people have brought up to me concerns with the fact this document was generated with claude.ai and that i use LLM Assistance in my coding to overcome a mental disability that would ruin the health of both my mind and body. The one bringing up concerns about LLM code insisted i add in the "AI-Generated" tag even though claude.ai only assists me on the parts my disability would act up on as well as this bit in the doc. So that is why i am making this and adding the tag so people will leave me alone. For a more detailed reason for everything which i ask you to genuinely read, please check below.

So what disability do i have that warrants the use of claude.ai? Well the root name is "Autism" but its something that is a part of that, that is the issue. In my special blend of the spectrum i got a form of "Obsessive-Compulsive Disorder". So what does that look like and how does it relate to coding? Glad you asked, first lets go over what it looks like. When i get into an obsessive state i effectively change as a person. Nothing but my obsession matters, not me eating, drinking, using the bathroom, taking care of myself/my eviroment, and sleeping. Friends and family stop mattering, only my obsession matters. In coding this happens if i hit any sort of issue that i can't figure out the immediate solution. When it happens i tunnel vision on the method i am trying. This leads me to spend days sometimes weeks on one problem, refusing to compromise until i either finally accept the method is impossible or make the method possible. However afterwards i become extremely burnt out and abandon the project. Plus i get a whole host of other issues once i am out of an obsessive state, i feel the effects of all the 24 hours awake, multiple days not eating, the guilt of ignoring family and friends, and other such things.

Due to that when i tried using claude.ai to help me overcome the issues i have as a last ditch effort to do the thing i enjoy. I found that while i still get obsessive its far weaker then when i deal with those parts myself, this allowed me to enjoy coding without worrying that ill end up in the hospital. Which has made me the happiest i've been in almost 18 years and allowed me to not fear at all the suicidal thoughts i've struggled with for years.

I did not mark it at the start for multiple reasons.

- I did not want to have to explain my personal disability problems which is needed since the tag is misleading in my case.
- I did not want my genuine hardwork and effort for quality to be overshadowed by a label that only remotely applies.
- I did not think it was needed for LLM Assisted work and the fact i do not use Gen AI art, audio, or video.

Now that it has come to this i have a potentially sad announcement.

Going forward as of this update, i likely will stop working on White Knuckle mods. Between the constant people bothering me over my LLM usage now and likely in the future even though its to overcome my disability, and the fact that in the White Knuckle modding discord they were actively discussing ways to blacklist mods that have any sort of Gen AI touching them regardless of quality or reasoning. With a few even discussing seeing if the devs might add the blacklist baked into the game.

Since i do not like being hounded over my disability, dont want to be around when my work goes to waste as spiteful people who see "AI" and want it to burn regardless of any good it does, who also preceeds to mark all my works as "AI" and makes it so i get blamed for things i did not do because few people will actually read this, and will slander me over something i did to overcome my disability and be happy with my life.

If you want to reach me make and issue on github, or DM me on discord. Ill try to keep things updated and fix any major issues, but unless something changes my mind. I see it as too much emotional distress to keep working on white knuckle mods.


# The Mass Binding

A [White Knuckle](https://store.steampowered.com/app/2881650/White_Knuckle/) mod that adds **The Mass**, a new Binding: hunger only refills from eating denizens — or from "Denizen Meat" collected from them — instead of normal food. Eating enough denizens mutates you but your hunger grows each time.
[Thunderstore](https://thunderstore.io/c/white-knuckle/p/JEG_Development/The_Mass_Binding/)
[![Screenshot](Screenshot.jpg)](https://youtu.be/kkRDp-EnqT8)

## Features

- Adds a real, selectable Binding ("The Mass") to the Trinkets & Bindings picker, alongside the game's own bindings.
- Regular food barely sustains you; hunger is meant to be refilled by eating denizens instead.
- Grab a denizen with both hands, then: (NOTE: you do not NEED to grab them, simply holding both grab buttons and aiming at them should work.)
  - **Eat it directly** — kills it, restores hunger immediately, and counts toward the next perk reward.
  - **Turn it into Denizen Meat** — kills it and gives you a meat item you can eat later for hunger. Doesn't count toward the reward on its own; that's earned by the kill.
- Eating enough denizens grants a random perk. The number needed grows after each reward. You start off at 5, and it grows by 5 each perk gotten. You can obtain any perk including even perks (or debuff) outside of broken or shattered limbs.
- If another perk you pick up also brings its own hunger meter (e.g. Conditioned Polyphagia), it's kept in sync with The Mass instead of running as a separate, disconnected system.
- Fully configurable: hunger decay/restore rates, reward thresholds, keybinds, and more.
- Eating or turning into meat a Grub is considered forsaking you humanity and as such doing it once disgusts mother. (AKA eat or turn a grub into meat and you go to hotzone)

## Controls

Hold both grab buttons on a denizen, then press:

| Key | Action |
|-----|--------|
| `G` | Eat the denizen directly (restores hunger, counts toward the reward) |
| `X` | Turn the denizen into Denizen Meat (restores hunger when eaten later) |

Both keys are rebindable in the config.

## Installation

### Thunderstore / r2modman / Gale
Install through your mod manager of choice as normal — search for **The Mass Binding** and click install.

### Manual
1. Download the release zip.
2. Extract `TheMassBinding.dll` into your BepInEx plugins folder:
   `<profile>/BepInEx/plugins/TheMassBinding/TheMassBinding.dll`
3. Launch the game through your mod manager (or a BepInEx-patched install) so the plugin loads.

## Configuration

Settings are written to `BepInEx/config/com.vilcan.themassbinding.cfg` after the first run, or editable in-game via a mod config manager.

| Setting | Section | Default | Description |
|---|---|---|---|
| `HungerDecayMultiplier` | Hunger | `1.0` | Multiplier on the hunger decay rate. `1.0` = same pace as vanilla Survival Mode. |
| `NerfedFoodFraction` | Hunger | `0.1` | Fraction of normal food's hunger restore that still applies while The Mass is active. |
| `DenizenEatRestoreMultiplier` | Hunger | `1.0` | Hunger restored by eating a denizen directly, as a multiple of a normal meal. |
| `DenizenMeatEatMultiplier` | Hunger | `1.0` | Hunger restored by eating Denizen Meat, as a multiple of a normal meal. |
| `StartingMeatCount` | Hunger | `3` | Denizen Meat granted when you take the binding, in case you can't find a denizen right away. |
| `StartingThreshold` | Perks | `5` | Denizens eaten needed for the first random perk. |
| `ThresholdStep` | Perks | `5` | How much the required count increases after each perk reward. |
| `SyncForeignHungerModules` | Perks | `true` | Keeps a hunger module from another perk (e.g. Conditioned Polyphagia) tuned to match and fed by The Mass, instead of left as an unrelated system. (don't turn false it will break things) |
| `EatKey` | Controls | `G` | Key to eat a grabbed denizen directly. |
| `FoodMakerKey` | Controls | `X` | Key to turn a grabbed denizen into Denizen Meat. |
| `EatRange` | Controls | `3.0` | Max distance to a denizen for the eat prompt to register. |
| `VerboseLogging` | Debug | `false` | Per-frame eat-detection logging, for troubleshooting only. |

## Compatibility

Works alongside other perk/binding mods. If a granted perk introduces its own hunger meter, The Mass syncs with it rather than conflicting — see `SyncForeignHungerModules` above.

## Building from source

Requires the .NET SDK, BepInEx 5 core files, and a copy of the game's `Managed` folder.

```
dotnet build -p:BepInExDir="C:\...\BepInEx\core" -p:GameManagedDir="C:\...\White Knuckle_Data\Managed"
```

Default paths are pre-filled in `TheMassBinding.csproj` for a typical r2modman setup; override them with the flags above if yours differ.

## Known limitations

- Hunger-syncing with a foreign perk module only covers `PerkModule_HungerMeter`-based perks; unrelated hunger/food mods aren't accounted for.
- Denizen detection uses a raycast from the camera, so extremely thin or unusual denizen colliders may occasionally miss.

## License

MIT — see [LICENSE](LICENSE) if included, or adapt as needed for your release.
