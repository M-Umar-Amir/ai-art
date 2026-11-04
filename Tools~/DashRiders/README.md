# Crew replacement pass

Built-in imagegen references: `ace.png`, `nova.png`, `bolt.png`.
These are modeling inputs, not finished 3D models or evidence of in-game fidelity.

Bolt follows the PDF image per the user's explicit 2026-10-30 correction: helmeted courier,
large single mono-wheel, magenta/silver fairings, cyan visor and rear delivery cargo boxes.
The previous silver/yellow two-legged Blender robot is rejected as a visual match.
Do not substitute the written silver/yellow description for the client's chosen image again.

Ace and Nova use clean front-facing neutral A poses, full bodies and separated hands/feet.
Nova's ground smoke and handheld scarf were removed from the modeling input.
The original source images remain in the parent models directory.

Private Kaggle dataset: `muhammadumaramir/dash-riders-clean-crew-v2` (version 2 includes Bolt).
Private kernel: `muhammadumaramir/dash-riders-crew-model-workshop-v2`.
Completed and downloaded 2026-10-31. All three textured models generated successfully.
Hunyuan3D-2 recipe: 50 inference steps, octree resolution 512, up to 80k faces before
local optimization. The inspected live GLBs retain native UVs and use 44k/44k/52k faces for
Ace/Nova/Bolt. They are shipped in Assets/_Project/Art/Models/DashRiders.

In the GitHub source backup the reference PNGs are under Tools~/DashRiders/Reference. The
generation scripts retain paths for the local staging workspace; adapt those paths before a new
generation run. Opening/building the shipped Unity scene does not require regenerating the meshes.

Commands use the scoped parent `kaggle_pipeline.py` with `status --v2`, `logs --v2`,
and `download --v2`. `prepare --v2` builds the private submission metadata.
`push --v2` submits the kernel without uploading another dataset version.
Do not repeatedly submit jobs while the existing one is running.

Prompt set used with the built-in tool:

- Ace: preserve the reference face, tan skin, black swept hair and goggles, orange bomber
  utility jacket, charcoal scarf, black cargos, dark orange sneakers and fingerless gloves;
  polished animated 3D finish, clean surfaces, neutral symmetrical front A pose, complete
  hands and feet, plain gray studio background, neutral light, no text or backpack hardware.
- Nova: preserve the reference face, purple ponytail, green hair/wrist accents, purple athletic
  jacket, dark violet cargo trousers and purple high-tech shoes; polished animated 3D finish,
  neutral symmetrical front A pose, separated limbs, complete hands and feet, gray studio
  background, neutral light, remove ground smoke, held scarf, text, watermark and scenery.
- Bolt: preserve the entire PDF courier-plus-monowheel silhouette as one coherent asset;
  magenta helmet and cropped jacket, black visor with cyan LED eyes, silver chassis, thick
  single black tire, magenta fairings, cyan windscreen, handlebars with gloved hands,
  graphite trousers, lime shoe details, large rear cargo trunk and two side panniers;
  centered complete asset on gray studio background, neutral light, slight front three-quarter
  view, blank panels without third-party logos, no standing robot, extra wheels or scenery.

Concurrent project work was detected from an OpenCode session while this pass ran. Preserve its
unrelated edits. The tested GitHub snapshot uses the reference-based GLBs for the live crew.
