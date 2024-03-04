
- SHURIKEN TO SPRITESHEET

Bake your particle system(s) into spritesheets!
> Go to Window -> Mirza Beig -> Shuriken to Spritesheet to use this editor utility.

Reduce overdraw and particle counts instantly!
Use this tool to bake complex particle effects into spritesheets. 

You can playback and preview the entire particle system(s) animation through the editor window. 

QUICKSTART:

1. With the window open, simply select any game object which has at least one active
Shuriken particle system component anywhere in its hierarchy (either on the game object itself, or nested inside).

2. Set your per-frame texture size and final spritesheet texture sizes. The total spritesheet texture size must be
greater than or equal to the frame texture size. To only render a single texture sprite rather than a whole spritesheet,
make the frame texture size the same as the final output size.

Examples: 

-> a frame texture size of 128 and a total spritesheet texture size of 1024 = 1024 / 128 = 64 frames in an 8 x 8 spritesheet.
-> a frame texture size of 512 and a total spritesheet texture size of 4096 = 4096 / 512 = 64 frames in an 8 x 8 spritesheet.

3. You can automatically calculate the approximate duration of the entire selection of 
particle systems using the "Calculate Duration" button or assign this value manually.

4. Select a camera to render with. The spritesheet will render exactly what you see in the game view + transparency and
with any image effects / post-processing applied on top. You may want to create a separate camera for rendering spritesheets.

5. Select where you'd like to save your spritesheet, and give it a name (don't include the extension as it will automatically be .png).

That's it! Hit "CREATE SPRITESHEET" and wait for the render to finish.

** MAKE SURE TO READ THE TOOLTIPS!

XX -- CHANGE LOG -- XX

v1.1.3

- Updated for Unity 5.5.

v1.1.2: 

- Fixed render progress bar freezing in Unity 5.4.0f3. 
  May be due to a bug with coroutines in Unity itself.
  
- Instead of a progress bar, there will be a temporary hang immediately
  after clicking, "Create Spritesheet" with a duration equal to however long
  the progress bar would otherwise be there for while the texture is rendered.  

v1.1.0:
 
- Added more example prefabs.
- Modified folder structure and naming scheme.

- Fixed editor window not saving key parameters (save directory, texture sizes etc.).
- Fixed render script not being cleared on selected camera after render completion.
- Added more transparency options + improved render time:

-- None -> render on black.

-- Difference

-- Luminance
-- Luminance 2
-- Luminance 3

- Added pre-alpha and post-alpha colour multiplier sliders.

- Added an additional check to make sure target folder exists.

- When rendering a single sprite frame (frame texture size = spritesheet texture size),
  the frame rendered will be the one currently being previewed using the playback position time slider.
  
v1.1.1: 

- Fixed particles being cleared even if prewarm was on for that system.

v1.0.0: 

- Initial release.
  
- ADDITIONAL:

Questions, comments, requests? Send me an email: 
> mirza.realms@live.ca

If you have any cool ideas for more effects/content, let me know!

CC0 (public domain) assets by Kenney!
- http://www.kenney.nl/
