/*
 * Copyright 2015 Esri
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
       http://www.apache.org/licenses/LICENSE-2.0
 * Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on
 * an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and limitations under the License.​
 */

using System;
using System.Runtime.InteropServices;

using ESRI.ArcGIS.ADF.CATIDs;           // for GPFunctionFactories.Register()

using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geoprocessing;

namespace GPProcessVendorDataFunctions
{
    /// <summary>
    /// Summary description for ProcessVendorDataGPFunctionFactory.
    /// </summary>
    /// 
    [Guid("9244CD44-866A-417b-B117-92D495A1DEBA")]
    [ClassInterface(ClassInterfaceType.None)]
    [ProgId("GPProcessVendorDataFunctions.ProcessVendorDataGPFunctionFactory")]

    public class ProcessVendorDataGPFunctionFactory : IGPFunctionFactory
    {

        #region COM Registration Function(s)
        [ComRegisterFunction()]
        [ComVisible(false)]
        private static void RegisterFunction(Type registerType)
        {
            string regKey = string.Format("HKEY_CLASSES_ROOT\\CLSID\\{{{0}}}", registerType.GUID);
            GPFunctionFactories.Register(regKey);
        }

        [ComUnregisterFunction()]
        [ComVisible(false)]
        private static void UnregisterFunction(Type registerType)
        {
            string regKey = string.Format("HKEY_CLASSES_ROOT\\CLSID\\{{{0}}}", registerType.GUID);
            GPFunctionFactories.Unregister(regKey);
        }
        #endregion

        public ProcessVendorDataGPFunctionFactory()
        {
        }

        #region IGPFunctionFactory Members

        public IGPFunction GetFunction(string name)
        {
            IGPFunction gpFunction = null;
            switch (name)
            {
                case "ProcessMultiNetData":
                    gpFunction = new ProcessMultiNetDataFunction() as IGPFunction;
                    break;
                case "ProcessMultiNetTimeZones":
                    gpFunction = new ProcessMultiNetTimeZonesFunction() as IGPFunction;
                    break;
                case "ProcessNavStreetsData":
                    gpFunction = new ProcessNavStreetsDataFunction() as IGPFunction;
                    break;
                case "ProcessNavStreetsTimeZones":
                    gpFunction = new ProcessNavStreetsTimeZonesFunction() as IGPFunction;
                    break;
            }
            return gpFunction;
        }

        public UID CLSID
        {
            get
            {
                UID pUID;
                pUID = new UIDClass();
                pUID.Value = "GPProcessVendorDataFunctions.ProcessVendorDataGPFunctionFactory";
                return pUID;
            }
        }

        public IEnumGPName GetFunctionNames()
        {
            IArray array = new EnumGPNameClass();
            array.Add(GetFunctionName("ProcessMultiNetData"));
            array.Add(GetFunctionName("ProcessMultiNetTimeZones"));
            array.Add(GetFunctionName("ProcessNavStreetsData"));
            array.Add(GetFunctionName("ProcessNavStreetsTimeZones"));

            return array as IEnumGPName;
        }

        public string Name
        {
            get
            {
                return "Network Analyst Sample Tools";
            }
        }

        public IEnumGPEnvironment GetFunctionEnvironments()
        {
            return null;
        }

        public string Alias
        {
            get
            {
                return "Network Analyst Sample Tools";
            }
        }

        public IGPName GetFunctionName(string name)
        {
            IGPFunctionFactory functionFactory = new ProcessVendorDataGPFunctionFactory();
            IGPFunctionName functionName = new GPFunctionNameClass();

            IGPName gpName;

            switch (name)
            {
                case "ProcessMultiNetData":
                    gpName = functionName as IGPName;
                    gpName.Name = name;
                    gpName.Category = "Street Data Processing";
                    gpName.Description = "This tool reads in Tele Atlas® MultiNet® data and Tele Atlas Speed Profiles® data (if provided) and creates a file geodatabase with a network dataset that can be analyzed with ArcGIS Network Analyst. The tool first creates the feature classes and tables needed for the network dataset, then creates and builds the network dataset.";
                    gpName.DisplayName = "Process MultiNet® Street Data";
                    gpName.Factory = functionFactory;
                    return gpName;
                case "ProcessMultiNetTimeZones":
                    gpName = functionName as IGPName;
                    gpName.Name = name;
                    gpName.Category = "Street Data Processing";
                    gpName.Description = "This tool reads in Tele Atlas® MultiNet® data and creates a file geodatabase with a polygon feature class of the time zones. If the Network Geometry (NW) feature class is provided, then the output file geodatabase will also contain a Streets feature class and a TimeZones table for use in creating a network dataset with time zone information by using the ProcessMultiNetData tool.";
                    gpName.DisplayName = "Process MultiNet® Time Zone Data";
                    gpName.Factory = functionFactory;
                    return gpName;
                case "ProcessNavStreetsData":
                    gpName = functionName as IGPName;
                    gpName.Name = name;
                    gpName.Category = "Street Data Processing";
                    gpName.Description = "This tool reads in NAVTEQ™ NAVSTREETS™ data and NAVTEQ Traffic Patterns™ data (if provided) and creates a file geodatabase with a network dataset that can be analyzed with ArcGIS Network Analyst. The tool first creates the feature classes and tables needed for the network dataset, then creates and builds the network dataset.";
                    gpName.DisplayName = "Process NAVSTREETS™ Street Data";
                    gpName.Factory = functionFactory;
                    return gpName;
                case "ProcessNavStreetsTimeZones":
                    gpName = functionName as IGPName;
                    gpName.Name = name;
                    gpName.Category = "Street Data Processing";
                    gpName.Description = "This tool reads in NAVTEQ™ NAVSTREETS™ data and creates a file geodatabase with a polygon feature class of the time zones. If the Streets feature class is provided, then the output file geodatabase will also contain a Streets feature class and a TimeZones table for use in creating a network dataset with time zone information by using the ProcessNavStreetsData tool.";
                    gpName.DisplayName = "Process NAVSTREETS™ Time Zone Data";
                    gpName.Factory = functionFactory;
                    return gpName;
            }

            return null;
        }

        #endregion
    }
}
