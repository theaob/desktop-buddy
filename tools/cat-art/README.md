# Cat art

Python scripts that draw the ten designed cats (Bean, Wisp, Puff, Pebble and Sprout, cartoon and realistic) in every pose, and write them into `src/DesktopBuddy.Core/CatBreedArt.cs`.

- `posekit.py`: every pose, drawn from simple shapes plus hand-drawn heads.
- `drafts.py`: the cartoon cats (flat colours, dark outline).
- `final.py`: the realistic cats (fur shading, outline in the fur's own colour).
- `export_cs.py`: writes the C# file.

Needs Python 3 and Pillow. After changing a drawing, run `python3 export_cs.py` from this folder and commit the regenerated C# file.
To see the cats, `python3 final.py <folder>` and `python3 drafts.py <folder>` write pose sheets and animated GIFs.
