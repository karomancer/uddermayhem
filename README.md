# Udder Mayhem

A rhythm game you play by milking a cow. Squeeze the silicone udder in time with the music to fill coffee cups for the customers of a cow-run café.

Udder Mayhem started as the midterm for *The New Arcade* at NYU ITP in spring 2023, went on to the ITP Showcase and the Coney Island Maker Faire, and was written up in Make Magazine. The full story, with photos and videos, is on my portfolio: https://www.karinachowtime.com/portfolio/uddermayhem

## What's in this repo

| Folder | What it is |
|---|---|
| `teensy/` | Firmware for the udder controller. A Teensy reads four force-sensing resistors, one per teat, and shows up to the computer as a keyboard. |
| `CAD/` | Fusion 360 and STL files for the controller body and the silicone udder mold. |
| `MakeMagazine/` | Files that go with the Make Magazine article, including the one-piece mold box. |
| `BovineBaristaGame/` | The original Unity project from the 2023 class, under the game's working title. This is the hand-written version described in the portfolio write-up. |

## Playing it

The controller presses plain keyboard keys, so anything that runs the game works with either the udder or a keyboard:

| Teat | Key |
|---|---|
| Back left | Q |
| Back right | W |
| Front left | A |
| Front right | S |

Squeeze when a cup arrives under a teat and let go when it's full.

## The current game

The version people play at events today is a much bigger arcade build that runs on a Raspberry Pi inside the cabinet, made together with Kevin Mitchell. Its source isn't in this repo. The Unity project here is the 2023 class project, kept as the record of how it was built.

## Building the controller

Start with the Make Magazine files and the Teensy sketch. The sketch expects four FSRs on the analog pins named at the top of `teensy/fsr_udder/fsr_udder.ino`, and the press and release thresholds are constants there if your sensors read differently.
