# Unity Taiso
3D animation of radio calisthenics(ラジオ体操第一) made with Unity and Blender.

[WebGL](https://vcraft.jp/unity/taiso/)<br>
[Youtube](https://youtu.be/MvqD-PNGG7o)

![Taiso](https://user-images.githubusercontent.com/46808493/161442401-73b3386b-1738-42cf-9121-268f0682f00e.jpg)

## Overview
Using 3D models created with Blender, animate a radio calisthenics exercise. **SKINNY** and **FAT**  children are represented by **Shape Keys**, and **UNMOTIVATED** animations are blended in addition to normal animations. There is also a variety of hairstyles and clothing.

## Tools
- Unity 2020.3 (LTS)
- Blender 2.92
- Inkscape 1.0.2
- Domino 1.44 (MIDI sequencer)
- Sound Font SGM-V2.01

## Operation
- mouse dragging: change the camera angle
- mouse wheel: zoom in/out the camera
- mouse click: move the camera to the center of the character
- [Space] : pause/play
- [Tab]: open/close UI Panels

## Notification
### How to import FBX
- Open 'chibi3.blend' in Blender
- Export>FBX dialog
	- Object Types: Armature|Mesh
	- Apply Scalings: FBX Unit Scale
	- Apply Transform: ON
- Import 'chibi3.fbx' in Unity 'Assets/Chibi/Models'<br>
  (Import settings are overwritten by 'FBXImporter.cs')
- Drag 'chibi3' in Scene Hierarchy, and Re-Drag into Project to create an original prefab
- Select the 'chibi3.prefab' and click [Tools>Modify 'Chibi'] menu

## Music composer
ラジオ体操第一 作曲：服部 正
