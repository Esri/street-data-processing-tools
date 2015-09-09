# Street Data Processing Tools
When you purchase commercial street data with the intent of using it in ArcGIS Network Analyst, you’ll need to create a network dataset if one isn’t already provided. Creating a good network dataset takes time and requires a fairly thorough understanding of the process. For this reason, we have tried to simplify the process for many ArcGIS users by writing a set of tools that automatically creates a network dataset from two popular street data products: HERE™ NAVSTREETS™ and TomTom® MultiNet®.

![Street Data Processing Tools](street-data-processing-tools.png)

The tools process shapefile data from TomTom MultiNet or HERE NAVSTREETS into a file geodatabase network dataset. The tools import the street feature classes into the file geodatabase and add the appropriate fields to these feature classes for modeling overpasses/underpasses, one-way streets, travel times, hierarchy, and driving directions. They also create feature classes and tables for modeling turn restrictions and signpost guidance. The tools can also processes advanced logistics attributes from data vendors and model them as restrictions and attribute parameters in the network dataset. If historical traffic data, such as TomTom Speed Profiles® or HERE Traffic Patterns™, is also provided, these tools can carry over this information to the network dataset so you can generate the best routes based on a particular time and day of the week. The historical traffic data can be based on TMC codes or LINK_IDs.


## Software Requirements
* ArcGIS 10.3.1 or later
* ArcView (Basic) license or higher
* ArcGIS Network Analyst Extension
* If you have installed ArcGIS for Desktop Background Geoprocessing (64 Bit) update, the tools will run only if background geoprocessing is disabled.


## Installation
To install the tools

1. Download the latest version of [StreetDataProcessingTools_v*.zip](releases/latest) file.
2. Extract the contents of the archive to a folder.
3. Right-click `GPProcessVendorDataFunctions.dll` and select Properties.
4. In the Properties dialog, click the General tab and then click Unblock button in the Security section. Click Ok to save the changes and close the properties dialog.
5. Right-click the `InstallTools.bat` file and select Run as administrator.

To use the tools, from the Catalog Window in ArcMap or the Catalog Tree in ArcCatalog, browse to the folder where you extracted the archive and run the tools within the Street Data Processing Tools toolbox. The tools require ArcGIS 10.3.1, however the tools can be used to create network datasets that can be used with ArcGIS 10.1 or later.


## Issues


Find a bug or want to request a new feature?  Please let us know by submitting an issue.


## Contributing


Esri welcomes contributions from anyone and everyone. Please see our [guidelines for contributing](https://github.com/esri/contributing).


## Licensing
Copyright 2015 Esri


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


 







































