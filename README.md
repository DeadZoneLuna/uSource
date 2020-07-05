# Unity Source Engine Importer
Importer of Source Engine resources in Unity3D. (BSP, MDL, VMT, VTF at the moment)
![Screenshot](Pic1.png)

# Requirements
1. Unity (This project was developed in 2017.4.36f1 version)
2. GCFScape (For extract Source Engine resources from "VPK" files. In the future, support for reading directly from VPK will be added!) & folder where the "Mod" folders with unpacked resources will be located.

# How to use?
1. Download the repository.
2. Unpack it.
3. Open project in Unity.
4. Configure the folder paths in "ConfigLoader.cs", where you have a mods folder with game resources (Unpacked from VPK, reading VPK not implemented yet!). 
(Example: GamePath = @"F:\Games\Source Engine" ModFolders = { "cstrike", "hl2" })
5. Done! Open the "Loader" scene in Unity & have fun :)

It can be used as import only in the editor or at runtime :^)

Feel free to suggest your fixes or improvements for this repository, only thanks to you this project can become better! :)
--

# About "ConfigLoader.cs".

* GamePath - Path to the folder where all the "Mod" folders are located.
* ModFolders - List of folders that will be found in the "GamePath" variable. **(First folder always has priority. This is necessary in order to the search maps from this folder)**
* LevelName - Map name for import
* VpkName - TODO
* VpkUse - TODO
* LoadMDL - Import only MDL from the "ModelName" variable.
* LoadLightmapsAsTextureShader - Load all lightmaps to materials. **(If false, then all lightmaps will be loaded from Unity Lighting System)**
* use3DSkybox - Creates two cameras, which one of them renders "3D Skybox" with depth render flags, and the another camera rendering everything but without depth render flags. (Hack :D)
* LoadMap - Import only BSP from the "LevelName" variable.
* LoadInfoDecals - Parsing & created the "info" decal entities. **(But it simple parsing. Need the real decal projection system :V)**
* DynamicLight - Using real-time shadows on the lights.
* useHDRLighting - Using HDR Lumps data.
* DrawArmature - Drawing models armature (Bones).
* ModelName - Model path for import **(if LoadMDL var is true)**.
* WorldScale - Constant for converting "Source Engine" units to "Unity" units.


# Supported formats
(The list will be updated)

BSP - Maps (It was tested with 19-21)

VMT - Materials

VTF - Textures (May require more RAM while parsing)

MDL - Models (It was tested with 44-48, improvement needed)

VVD - Model Vertices

VTX - Model Indices

# Limitations
- [ ] "angles" on the prop entities may be wrong. (But not for other entities  ¯\_(ツ)_/¯)
- [ ] Some MDL structure isn't fully parsed & some model parts may is gone after import.
- [ ] VTF Textures may require more RAM while parsing. (In the future I will try to fix it.)
- [ ] Older versions of BSP may not be loaded.

# TODO
- [ ] Load resources from the Unity. (models, materials).
- [ ] Optimize parsing.
- [ ] Create simple API for use this in other projects.
- [ ] Parsing Source Engine collision to Unity.
