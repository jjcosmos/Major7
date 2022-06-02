# Major7
Addressable audio tooling for Unity

Major7 is a sort of audio middleware, meant to move most of the audio work into code.
The goal is for it to feel similar to using SDKs like FMOD or WWISE, where creating an audio event is something that happens apart from normal engine components.
This is very much a work in progress, and is currently much less powerful than either the built-in audio tool or traditional middleware.
However, for simple use cases, it offers a lightweight and relatively un-intrusive way to play sound effects by name.

Major7 uses the addressables system for efficient async loading of audio clips. See how this is used in practice in the example.
Because of this, the user is currently required to run a menu item (Major7 > Generate Clip Definitions) in order to generate the code that allows assets to be referenced as AsyncOperationHandles.
This will be made automatic in the future.

The 2 "gotchas" that currently exists are that, after import, if a sound file is moved, the user must save (or GenerateClipDefinitions) to update the addressable paths. 
If a folder name is changed (harder to detect, so WIP), just run a "Major7 > Force Addressable Paths Reload".
Also, audio clips must have unique names - a unique path is not sufficient.

Audio assets /do/ have to be marked as "addressable" to be included by Major7
