# Cat art

Python scripts that draw the ten designed cats (Bean, Wisp, Puff, Pebble and Sprout, cartoon and realistic) the nine dogs (Mocha, Juniper, Domino, Pip, Toast, Clover, Waffles, Hazel and Peaches) and the ten birds (Ember, Blueberry, Acorn, Sunny, Kiwi, Skye, Inky, Slate, Nugget and Rosehip) in every pose, and write them into `src/DesktopBuddy.Core/CatBreedArt.cs`.

- `posekit.py`: every pose, drawn from simple shapes plus hand-drawn heads.
- `drafts.py`: the cartoon cats (flat colours, dark outline).
- `final.py`: the realistic cats (fur shading, outline in the fur's own colour).
- `dogkit.py`: the dogs on the same pose kit: dog heads and tails, panting, and the dog-only bone poses (carry, chew, toss, catch, play bow).
- `dogs.py`: the dogs' coats, and the bone drawn over the dog.
- `birdkit.py`: the birds' own body on the same canvas and pose names: hopping for walking, a wingbeat for running, preening for grooming, plus the bird-only poses (peck, sing, flap; glide and perch are drawn but not used yet).
- `birds.py`: the birds' plumage, and the seeds and music notes drawn over them.
- `export_cs.py`: writes the C# file.

Needs Python 3 and Pillow. After changing a drawing, run `python3 export_cs.py` from this folder and commit the regenerated C# file.
To see the cats, `python3 final.py <folder>`, `python3 drafts.py <folder>`, `python3 dogs.py <folder>` and `python3 birds.py <folder>` write pose sheets and animated GIFs.
`site_art.py` renders the GIFs for the project website (`site/assets`) from the shipped C# art; run it after changing any cat.
