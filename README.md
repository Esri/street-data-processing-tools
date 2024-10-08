﻿# Street Data Processing Tools
When you purchase commercial street data with the intent of using it in ArcGIS Network Analyst, you'll need to create a network dataset if one isn't already provided. Creating a good network dataset takes time and requires a fairly thorough understanding of the process. For this reason, we have tried to simplify the process for many ArcGIS users by writing a set of tools that automatically creates a network dataset from two popular street data products: HERE™ NAVSTREETS™ and TomTom® MultiNet®.

The tools process shapefile data from TomTom® MultiNet® or HERE™ NAVSTREETS™ into a file geodatabase network dataset. The tools import the street feature classes into the file geodatabase and add the appropriate fields to these feature classes for modeling overpasses/underpasses, one-way streets, travel times, hierarchy, and driving directions. They also create feature classes and tables for modeling turn restrictions and signpost guidance. The tools can also processes advanced logistics attributes from data vendors and model them as restrictions and attribute parameters in the network dataset. If historical traffic data, such as TomTom Speed Profiles® or HERE Traffic Patterns™, is also provided, these tools can carry over this information to the network dataset so you can generate the best routes based on a particular time and day of the week. The historical traffic data can be based on TMC codes or LINK_IDs.

## Software Requirements

Separate versions of the toolbox are provided for use in ArcMap and ArcGIS Pro. All users are encouraged to use the ArcGIS Pro version, as the ArcMap version is deprecated and no longer updated.

### ArcGIS Pro version

Software requirements:
* ArcGIS Pro 2.9 or later (The tool may run successfully on earlier versions of ArcGIS Pro but has not been tested.)
* Basic license or higher
* ArcGIS Network Analyst Extension

### ArcMap version

DEPRECATION NOTICE: This version of the tool is deprecated because a version of this tool for ArcGIS Pro is now available and ArcMap is in the mature support phase and will be retired in 2026.  No further updates will be made to this tool, and only minimal support will be provided.  Please migrate your workflows to ArcGIS Pro.

Software requirements:
* ArcGIS 10.8 or later
* ArcView (Basic) license or higher
* ArcGIS Network Analyst Extension
* If you have installed ArcGIS for Desktop Background Geoprocessing (64 Bit) update, the tools will run only if background geoprocessing is disabled.

(Note that network datasets created using the ArcMap version of this tool can still be used in ArcGIS Pro.  The tool must be run in ArcMap, but the resulting network is compatible with both products.)

## Instructions

Please see the README.md files in the [for-ArcMap](./for-ArcMap/README.md) and [for-ArcGIS-Pro](./for-ArcGIS-Pro/README.md) folders for instructions specific to the tool you plan to use.

## Issues

Find a bug or want to request a new feature?  Please let us know by submitting an issue.

## Contributing

Esri welcomes contributions from anyone and everyone. Please see our [guidelines for contributing](https://github.com/esri/contributing).

## Licensing
Copyright 2024 Esri

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at


   http://www.apache.org/licenses/LICENSE-2.0


Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.


A copy of the license is available in the repository's [license.txt](license.txt) file.


[](Esri Tags: ArcGIS Geoprocessing Toolbox)
[](Esri Language: C-Sharp)​​​​​​​​​​​​​​​
