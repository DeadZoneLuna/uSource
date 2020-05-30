# Unity Source Engine Importer
 a small universal resource loader the "Source Engine" BSP / MDL / VTF formats in Unity (It isn't perfect and need improves :D)
![Screenshot](Pic1.png)

# How to use?
1. Download this repository and extract it to the project folder.
2. Open this project in Unity3D (It was tested in 2017.4.36f1)
3. Configure the folder paths in "ConfigLoader.cs", where you have a mods folder with game resources (Unpacked from VPK, reading VPK not yet implemented!). 
(Example: GamePath = @"F:\Games\Source Engine" ModFolders = { "cstrike", "hl2" })
4. Done! Open the "Loader" scene in Unity & click what you want :)

# Supported formats
(The list will be updated as far as possible.)

BSP - Maps (It was tested with 19-21)

VMT - Materials

VTF - Textures (May require more RAM while reading, it needs to be fixed.)

MDL - Models (It was tested with 44-48, improvement needed)

VVD - Models Vertices

VTX - Model Indices

The "angles" on the entities may be wrong, need to find a solution for fix problem.

You can fork a this repository and make your fixes / improves to the importer & push commits. It will even be better! :^)
