# VertexHeightOblateAdvanced
VertexHeightOblateAdvanced is a custom PQS Mod intended for use by planet modders to enable easy creation of oblate bodies via a simple PQS Mod, rather than via a heightmap 

## Installation and Use
* Install ALL listed dependencies, following the links below
* Download and extract the VertexHeightOblateAdvanced zip file
* Place the GameData folder into your KSP directory
* Once installed, simply add a VertexHeightOblateAdvanced node to the Mods node in your body's PQS node
* Using the following [Google Sheet](https://docs.google.com/spreadsheets/d/1QSUjAmyAIACKAFSL_C8qv5GxYc4YTB1eDBRP4TTUl5A/edit?usp=sharing), find appropriate periods for your body to get the desired oblateness, based on body surface gravity and radius.

## Parameters and expected values:
* oblateMode: overall controller for type of generation used
  * Values: (PointEquipotential, UniformEquipotential, Blend, CustomEllipsoid)
    * PointEquipotential: Generates a point mass equipotential
    * UniformEquipotential: Generates a uniform density equipotential, either a Maclaurin spheroid or Jacobi ellipsoid deppending on energyMode and period
    * Blend: Multiply a PointEquipotential by a CustomEllipsoid
    * CustomEllipsoid: Generate an ellipsoid based on a, b, and c axis values
* energyMode :
  * Used with: oblateMode (UniformEquipotential)
  * Values: (Low, High)
    * Low: For the given period, generate using the low oblateness branch.  Always generates a Maclaurin spheroid
    * High: For the given period, generate using the high oblateness branch. Generates either a Maclaurin spheroid or Jacobi ellipsoid based on period
* mass: Mass of the body.  Optional if both radius and geeASL are provided.
  * Used with: oblateMode (PointEquipotential, UniformEquipotential, Blend)
  * Value: (greater than 0)
* radius: Mass of the body.  Optional if both mass and geeASL are provided.
  * Used with: oblateMode (PointEquipotential, UniformEquipotential, Blend)
  * Value: (greater than 0)
* geeASL: Surface gravity of the body.  Optional if both mass and radius are provided.
  * Used with: oblateMode (PointEquipotential, UniformEquipotential, Blend)
  * Value: (greater than 0)
* period: Rotational period of the body. Used with PointEquipotential, UniformEquipotential, and Blend oblateMode.
  * Used with: oblateMode (PointEquipotential, UniformEquipotential, Blend)
  * Value: (greater than 0)
* a: The primariy equatorial axis as a ratio of provided radius.
  * Used with: oblateMode (Blend, CustomEllipsoid)
  * Value: (1 to infinity)
* b: The secondary equatorial axis as a ratio of provided radius.
  * Used with: oblateMode (Blend, CustomEllipsoid)
  * Value: (1 to infinity)
* c: The polar axis as a ratio of provided radius.
  * Used with: oblateMode (Blend, CustomEllipsoid)
  * Value: (1 to infinity)

## Examples
* 

## Requirements
* [ModuleManager](https://forum.kerbalspaceprogram.com/index.php?/topic/50533-18x-112x-module-manager-422-june-18th-2022-the-heatwave-edition/)
* [Kopernicus](https://forum.kerbalspaceprogram.com/index.php?/topic/200143-180-1123-kopernicus-stable-branch-last-updated-august-12th-2022/)

## FAQ
* Q. I'm not a planet modder? Do I need this?
* A. You do not need to install it manually yourself, but if you found this in you GameData, it is because a planet pack you have/had needs/needed it and so it was either included with the mod or auto installed through CKAN

## Licensing
* VertexHeightOblateAdvanced is licensed under the MIT License