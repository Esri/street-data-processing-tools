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
using System.Collections;

using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geoprocessing;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.GISClient;

using ESRI.ArcGIS.Geoprocessor;
using ESRI.ArcGIS.AnalysisTools;
using ESRI.ArcGIS.ConversionTools;
using ESRI.ArcGIS.DataManagementTools;
using ESRI.ArcGIS.NetworkAnalystTools;

namespace GPProcessVendorDataFunctions
{
    /// <summary>
    /// Summary description for ProcessMultiNetDataFunction.
    /// </summary>
    ///

    [Guid("33351078-7D27-454e-85EB-65146C1B23DD")]
    [ClassInterface(ClassInterfaceType.None)]
    [ProgId("GPProcessVendorDataFunctions.ProcessMultiNetDataFunction")]

    class ProcessMultiNetDataFunction : IGPFunction2
    {
        #region Constants
        // names
        private const string DefaultFdsName = "Routing";
        private const string DefaultNdsName = "Routing_ND";
        private const string StreetsFCName = "Streets";
        private const string ProfilesTableName = "DailyProfiles";
        private const string HistTrafficJoinTableName = "Streets_DailyProfiles";
        private const string TMCJoinTableName = "Streets_TMC";
        private const string TurnFCName = "RestrictedTurns";
        private const string RoadSplitsTableName = "Streets_RoadSplits";
        private const string SignpostFCName = "Signposts";
        private const string SignpostJoinTableName = "Signposts_Streets";
        
        // restriction usage (prefer/avoid) factor constants
        private const double PreferHighFactor = 0.2;
        private const double PreferMediumFactor = 0.5;
        private const double PreferLowFactor = 0.8;
        private const double AvoidLowFactor = 1.3;
        private const double AvoidMediumFactor = 2.0;
        private const double AvoidHighFactor = 5.0;

        // parameter index constants
        private const int InputNWFeatureClass = 0;
        private const int InputMNFeatureClass = 1;
        private const int InputMPTable = 2;
        private const int InputSITable = 3;
        private const int InputSPTable = 4;
        private const int OutputFileGDB = 5;
        private const int OutputFileGDBVersion = 6;
        private const int InputFeatureDatasetName = 7;
        private const int InputNetworkDatasetName = 8;
        private const int InputCreateNetworkAttributesInMetric = 9;
        private const int InputCreateTwoDistanceAttributes = 10;
        private const int InputRSTable = 11;
        private const int InputTimeZoneIDBaseFieldName = 12;
        private const int InputTimeZoneTable = 13;
        private const int InputCommonTimeZoneForTheEntireDataset = 14;
        private const int InputHSNPTable = 15;
        private const int InputHSPRTable = 16;
        private const int InputRDTable = 17;
        private const int InputLiveTrafficFeedFolder = 18;
        private const int InputLiveTrafficFeedArcGISServerConnection = 19;
        private const int InputLiveTrafficFeedGeoprocessingServiceName = 20;
        private const int InputLiveTrafficFeedGeoprocessingTaskName = 21;
        private const int InputLTRTable = 22;
        private const int InputLRSTable = 23;
        private const int InputLVCTable = 24;
        private const int OutputNetworkDataset = 25;

        // field names and types
        private static readonly string[] NWFieldNames = new string[]
                                        { "ID", "FEATTYP", "F_JNCTID", "F_JNCTTYP", "T_JNCTID", "T_JNCTTYP",
                                          "PJ", "METERS", "FRC", "NET2CLASS", "NAME", "FOW", "FREEWAY",
                                          "BACKRD", "TOLLRD", "RDCOND", "PRIVATERD", "CONSTATUS",
                                          "ONEWAY", "F_ELEV", "T_ELEV", "KPH", "MINUTES",
                                          "NTHRUTRAF", "ROUGHRD" };
        private static readonly esriFieldType[] NWFieldTypes = new esriFieldType[]
                                        { esriFieldType.esriFieldTypeDouble,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeDouble,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeDouble,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeDouble,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeSingle,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeSmallInteger };
        private static readonly string[] MNFieldNames = new string[]
                                        { "ID", "FEATTYP", "BIFTYP", "PROMANTYP", "JNCTID" };
        private static readonly esriFieldType[] MNFieldTypes = new esriFieldType[]
                                        { esriFieldType.esriFieldTypeDouble,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeDouble };
        private static readonly string[] MPFieldNames = new string[]
                                        { "ID", "SEQNR", "TRPELID", "TRPELTYP" };
        private static readonly esriFieldType[] MPFieldTypes = new esriFieldType[]
                                        { esriFieldType.esriFieldTypeDouble,
                                          esriFieldType.esriFieldTypeInteger,
                                          esriFieldType.esriFieldTypeDouble,
                                          esriFieldType.esriFieldTypeSmallInteger };
        private static readonly string[] SIFieldNames = new string[] 
                                        { "ID", "SEQNR", "DESTSEQ", "INFOTYP", "RNPART",
                                          "TXTCONT", "TXTCONTLC", "CONTYP", "AMBIG" };
        private static readonly esriFieldType[] SIFieldTypes = new esriFieldType[]
                                        { esriFieldType.esriFieldTypeDouble,
                                          esriFieldType.esriFieldTypeInteger,
                                          esriFieldType.esriFieldTypeInteger,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeSmallInteger};
        private static readonly string[] SPFieldNames = new string[]
                                        { "ID", "SEQNR", "TRPELID", "TRPELTYP" };
        private static readonly esriFieldType[] SPFieldTypes = new esriFieldType[]
                                        { esriFieldType.esriFieldTypeDouble,
                                          esriFieldType.esriFieldTypeInteger,
                                          esriFieldType.esriFieldTypeDouble,
                                          esriFieldType.esriFieldTypeSmallInteger};
        private static readonly string[] RSFieldNames = new string[]
                                        { "ID", "SEQNR", "FEATTYP", "DIR_POS", "RESTRTYP", "RESTRVAL", "VT" };
        private static readonly esriFieldType[] RSFieldTypes = new esriFieldType[]
                                        { esriFieldType.esriFieldTypeDouble,
                                          esriFieldType.esriFieldTypeInteger,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeSmallInteger};
        private static readonly string[] TimeZoneFieldNames = new string[]
                                        { "MSTIMEZONE" };
        private static readonly esriFieldType[] TimeZoneFieldTypes = new esriFieldType[]
                                        { esriFieldType.esriFieldTypeString };
        private static readonly string[] HSNPFieldNames = new string[]
                                        { "NETWORK_ID", "VAL_DIR", "SPFREEFLOW",
                                          "SPWEEKDAY", "SPWEEKEND", "SPWEEK", "PROFILE_1",
                                          "PROFILE_2", "PROFILE_3", "PROFILE_4",
                                          "PROFILE_5", "PROFILE_6", "PROFILE_7" };
        private static readonly esriFieldType[] HSNPFieldTypes = new esriFieldType[]
                                        { esriFieldType.esriFieldTypeDouble,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeSmallInteger };
        private static readonly string[] HSPRFieldNames = new string[]
                                        { "PROFILE_ID", "TIME_SLOT", "REL_SP" };
        private static readonly esriFieldType[] HSPRFieldTypes = new esriFieldType[]
                                        { esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeInteger,
                                          esriFieldType.esriFieldTypeSingle };
        private static readonly string[] RDFieldNames = new string[]
                                        { "ID", "RDSTMC", "TMCPATHID", "TMCMPATHID" };
        private static readonly esriFieldType[] RDFieldTypes = new esriFieldType[]
                                        { esriFieldType.esriFieldTypeDouble,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeDouble,
                                          esriFieldType.esriFieldTypeString};
        private static readonly string[] LTRFieldNames = new string[]
                                        { "ID", "SEQNR", "FEATTYP", "PREFERRED", "RESTRICTED" };
        private static readonly esriFieldType[] LTRFieldTypes = new esriFieldType[]
                                        { esriFieldType.esriFieldTypeDouble,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeSmallInteger };
        private static readonly string[] LRSFieldNames = new string[]
                                        { "ID", "SEQNR", "FEATTYP", "RESTRTYP", "VT", "RESTRVAL",
                                          "LIMIT", "UNIT_MEAS", "LANE_VALID", "VALDIRPOS", "VERIFIED" };
        private static readonly esriFieldType[] LRSFieldTypes = new esriFieldType[]
                                        { esriFieldType.esriFieldTypeDouble,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeDouble,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeSmallInteger };
        private static readonly string[] LVCFieldNames = new string[]
                                        { "ID", "SEQNR", "SUBSEQNR", "FEATTYP",
                                          "VT_CLASS", "VALUE", "UNIT_MEAS", "TYPE" };
        private static readonly esriFieldType[] LVCFieldTypes = new esriFieldType[]
                                        { esriFieldType.esriFieldTypeDouble,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeDouble,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeString };
        #endregion

        private IArray m_parameters;
        private IGPUtilities m_gpUtils;

        public ProcessMultiNetDataFunction()
        {
            // Create the GPUtilities Object
            m_gpUtils = new GPUtilitiesClass();
        }

        #region IGPFunction2 Members

        public IArray ParameterInfo
        {
            // create and return the parameters for this function:
            //  0 - Input Network Geometry (NW) Feature Class
            //  1 - Input Maneuvers Geometry (MN) Feature Class
            //  2 - Input Maneuver Path Index (MP) Table
            //  3 - Input Sign Information (SI) Table
            //  4 - Input Sign Path (SP) Table
            //  5 - Output File Geodatabase
            //  6 - Output File Geodatabase Version
            //  7 - Feature Dataset Name
            //  8 - Network Dataset Name
            //  9 - Create Network Attributes in Metric (optional)
            // 10 - Create Two Distance Attributes (optional)
            // 11 - Input Restrictions (RS) Table (optional)
            // 12 - Input Time Zone ID Base Field Name (optional)
            // 13 - Input Time Zone Table (optional)
            // 14 - Input Common Time Zone for the Entire Dataset (optional)
            // 15 - Input Network Profile Link (HSNP) Table (optional)
            // 16 - Input Historical Speed Profiles (HSPR) Table (optional)
            // 17 - Input RDS-TMC Information (RD) Table (optional)
            // 18 - Input Live Traffic Feed Folder (optional)
            // 19 - Input Live Traffic Feed ArcGIS Server Connection (optional)
            // 20 - Input Live Traffic Feed Geoprocessing Service Name (optional)
            // 21 - Input Live Traffic Feed Geoprocessing Task Name (optional)
            // 22 - Input Logistics Truck Routes (LTR) Table (optional)
            // 23 - Input Logistics Restrictions (LRS) Table (optional)
            // 24 - Input Logistics Vehicle Characteristics (LVC) Table (optional)
            // 25 - Output Network Dataset (derived parameter)

            get
            {
                IArray paramArray = new ArrayClass();

                // 0 - input_nw_feature_class

                IGPParameterEdit paramEdit = new GPParameterClass();
                paramEdit.DataType = new DEFeatureClassTypeClass() as IGPDataType;
                paramEdit.Value = new DEFeatureClass() as IGPValue;
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Input Network Geometry (NW) Feature Class";
                paramEdit.DisplayOrder = InputNWFeatureClass;
                paramEdit.Enabled = true;
                paramEdit.Name = "input_nw_feature_class";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeRequired;

                IGPFeatureClassDomain lineFeatureClassDomain = new GPFeatureClassDomainClass();
                lineFeatureClassDomain.AddType(esriGeometryType.esriGeometryLine);
                lineFeatureClassDomain.AddType(esriGeometryType.esriGeometryPolyline);
                paramEdit.Domain = lineFeatureClassDomain as IGPDomain;

                paramArray.Add(paramEdit as object);

                // 1 - input_mn_feature_class

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new DEFeatureClassTypeClass() as IGPDataType;
                paramEdit.Value = new DEFeatureClass() as IGPValue;
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Input Maneuvers Geometry (MN) Feature Class";
                paramEdit.DisplayOrder = InputMNFeatureClass;
                paramEdit.Enabled = true;
                paramEdit.Name = "input_mn_feature_class";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeRequired;

                IGPFeatureClassDomain pointFeatureClassDomain = new GPFeatureClassDomainClass();
                pointFeatureClassDomain.AddType(esriGeometryType.esriGeometryPoint);
                paramEdit.Domain = pointFeatureClassDomain as IGPDomain;

                paramArray.Add(paramEdit as object);

                // 2 - input_mp_table

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new DETableTypeClass() as IGPDataType;
                paramEdit.Value = new DETableClass() as IGPValue;
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Input Maneuver Path Index (MP) Table";
                paramEdit.DisplayOrder = InputMPTable;
                paramEdit.Enabled = true;
                paramEdit.Name = "input_mp_table";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeRequired;

                paramArray.Add(paramEdit as object);

                // 3 - input_si_table

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new DETableTypeClass() as IGPDataType;
                paramEdit.Value = new DETableClass() as IGPValue;
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Input Sign Information (SI) Table";
                paramEdit.DisplayOrder = InputSITable;
                paramEdit.Enabled = true;
                paramEdit.Name = "input_si_table";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeRequired;

                paramArray.Add(paramEdit as object);

                // 4 - input_sp_table

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new DETableTypeClass() as IGPDataType;
                paramEdit.Value = new DETableClass() as IGPValue;
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Input Sign Path (SP) Table";
                paramEdit.DisplayOrder = InputSPTable;
                paramEdit.Enabled = true;
                paramEdit.Name = "input_sp_table";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeRequired;

                paramArray.Add(paramEdit as object);

                // 5 - output_file_geodatabase

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new DEWorkspaceTypeClass() as IGPDataType;
                paramEdit.Value = new DEWorkspaceClass() as IGPValue;
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionOutput;
                paramEdit.DisplayName = "Output File Geodatabase";
                paramEdit.DisplayOrder = OutputFileGDB;
                paramEdit.Enabled = true;
                paramEdit.Name = "output_file_geodatabase";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeRequired;

                IGPWorkspaceDomain fileGDBDomain = new GPWorkspaceDomainClass();
                fileGDBDomain.AddType(esriWorkspaceType.esriLocalDatabaseWorkspace);
                paramEdit.Domain = fileGDBDomain as IGPDomain;

                paramArray.Add(paramEdit as object);

                // 6 - output_file_geodatabase_version

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new GPStringTypeClass() as IGPDataType;
                IGPString gpString = new GPStringClass();
                gpString.Value = "10.1";
                paramEdit.Value = gpString as IGPValue;
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Output File Geodatabase Version";
                paramEdit.DisplayOrder = OutputFileGDBVersion;
                paramEdit.Enabled = true;
                paramEdit.Name = "output_file_geodatabase_version";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeRequired;

                IGPCodedValueDomain codedValueDomain = new GPCodedValueDomainClass();
                codedValueDomain.AddStringCode("10.1", "10.1");
                codedValueDomain.AddStringCode("10.0", "10.0");
                codedValueDomain.AddStringCode("9.3", "9.3");
                paramEdit.Domain = codedValueDomain as IGPDomain;

                paramArray.Add(paramEdit as object);

                // 7 - feature_dataset_name
                
                paramEdit = new GPParameterClass();
                paramEdit.DataType = new GPStringTypeClass() as IGPDataType;
                gpString = new GPStringClass();
                gpString.Value = DefaultFdsName;
                paramEdit.Value = gpString as IGPValue;
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Feature Dataset Name";
                paramEdit.DisplayOrder = InputFeatureDatasetName;
                paramEdit.Enabled = true;
                paramEdit.Name = "feature_dataset_name";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeRequired;

                paramArray.Add(paramEdit as object);

                // 8 - network_dataset_name

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new GPStringTypeClass() as IGPDataType;
                gpString = new GPStringClass();
                gpString.Value = DefaultNdsName;
                paramEdit.Value = gpString as IGPValue;
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Network Dataset Name";
                paramEdit.DisplayOrder = InputNetworkDatasetName;
                paramEdit.Enabled = true;
                paramEdit.Name = "network_dataset_name";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeRequired;

                paramArray.Add(paramEdit as object);

                // 9 - input_create_network_attributes_in_metric

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new GPBooleanTypeClass() as IGPDataType;
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Create Network Attributes in Metric";
                paramEdit.DisplayOrder = InputCreateNetworkAttributesInMetric;
                paramEdit.Enabled = true;
                paramEdit.Name = "input_create_network_attributes_in_metric";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeOptional;
                IGPBoolean gpBool = new GPBooleanClass();
                gpBool.Value = false;
                paramEdit.Value = gpBool as IGPValue;

                paramArray.Add(paramEdit as object);

                // 10 - input_create_two_distance_attributes

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new GPBooleanTypeClass() as IGPDataType;
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Create Two Distance Attributes";
                paramEdit.DisplayOrder = InputCreateTwoDistanceAttributes;
                paramEdit.Enabled = true;
                paramEdit.Name = "input_create_two_distance_attributes";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeOptional;
                gpBool = new GPBooleanClass();
                gpBool.Value = false;
                paramEdit.Value = gpBool as IGPValue;

                paramArray.Add(paramEdit as object);

                // 11 - input_rs_table

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new DETableTypeClass() as IGPDataType;
                paramEdit.Value = new DETableClass() as IGPValue;
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Input Restrictions (RS) Table";
                paramEdit.DisplayOrder = InputRSTable;
                paramEdit.Enabled = true;
                paramEdit.Name = "input_rs_table";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeOptional;

                paramArray.Add(paramEdit as object);

                // 12 - input_time_zone_id_base_field_name

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new GPStringTypeClass() as IGPDataType;
                paramEdit.Value = new GPStringClass() as IGPValue;
                paramEdit.Category = "Time Zones";
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Input Time Zone ID Base Field Name";
                paramEdit.DisplayOrder = InputTimeZoneIDBaseFieldName;
                paramEdit.Enabled = true;
                paramEdit.Name = "input_time_zone_id_base_field_name";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeOptional;

                paramArray.Add(paramEdit as object);

                // 13 - input_time_zone_table

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new DETableTypeClass() as IGPDataType;
                paramEdit.Value = new DETableClass() as IGPValue;
                paramEdit.Category = "Time Zones";
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Input Time Zone Table";
                paramEdit.DisplayOrder = InputTimeZoneTable;
                paramEdit.Enabled = true;
                paramEdit.Name = "input_time_zone_table";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeOptional;

                paramArray.Add(paramEdit as object);

                // 14 - input_common_time_zone_for_the_entire_dataset

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new GPStringTypeClass() as IGPDataType;
                paramEdit.Value = new GPStringClass() as IGPValue;
                paramEdit.Category = "Time Zones";
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Input Time Zone for the Entire Dataset";
                paramEdit.DisplayOrder = InputCommonTimeZoneForTheEntireDataset;
                paramEdit.Enabled = true;
                paramEdit.Name = "input_common_time_zone_for_the_entire_dataset";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeOptional;

                paramArray.Add(paramEdit as object);

                // 15 - input_hsnp_table

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new DETableTypeClass() as IGPDataType;
                paramEdit.Value = new DETableClass() as IGPValue;
                paramEdit.Category = "Historical Traffic";
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Input Network Profile Link (HSNP) Table";
                paramEdit.DisplayOrder = InputHSNPTable;
                paramEdit.Enabled = true;
                paramEdit.Name = "input_hsnp_table";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeOptional;

                paramArray.Add(paramEdit as object);

                // 16 - input_hspr_table

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new DETableTypeClass() as IGPDataType;
                paramEdit.Value = new DETableClass() as IGPValue;
                paramEdit.Category = "Historical Traffic";
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Input Historical Speed Profiles (HSPR) Table";
                paramEdit.DisplayOrder = InputHSPRTable;
                paramEdit.Enabled = true;
                paramEdit.Name = "input_hspr_table";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeOptional;

                paramArray.Add(paramEdit as object);

                // 17 - input_rd_table

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new DETableTypeClass() as IGPDataType;
                paramEdit.Value = new DETableClass() as IGPValue;
                paramEdit.Category = "Live Traffic";
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Input RDS-TMC Information (RD) Table";
                paramEdit.DisplayOrder = InputRDTable;
                paramEdit.Enabled = true;
                paramEdit.Name = "input_rd_table";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeOptional;

                paramArray.Add(paramEdit as object);

                // 18 - input_live_traffic_feed_folder

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new GPStringTypeClass() as IGPDataType;
                paramEdit.Value = new GPStringClass() as IGPValue;
                paramEdit.Category = "Live Traffic";
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Input Live Traffic Feed Folder";
                paramEdit.DisplayOrder = InputLiveTrafficFeedFolder;
                paramEdit.Enabled = true;
                paramEdit.Name = "input_live_traffic_feed_folder";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeOptional;

                paramArray.Add(paramEdit as object);

                // 19 - input_live_traffic_feed_arcgis_server_connection

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new DEServerConnectionTypeClass() as IGPDataType;
                paramEdit.Value = new DEServerConnectionClass() as IGPValue;
                paramEdit.Category = "Live Traffic";
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Input Live Traffic Feed ArcGIS Server Connection";
                paramEdit.DisplayOrder = InputLiveTrafficFeedArcGISServerConnection;
                paramEdit.Enabled = true;
                paramEdit.Name = "input_live_traffic_feed_arcgis_server_connection";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeOptional;

                paramArray.Add(paramEdit as object);

                // 20 - input_live_traffic_feed_geoprocessing_service_name

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new GPStringTypeClass() as IGPDataType;
                paramEdit.Value = new GPStringClass() as IGPValue;
                paramEdit.Category = "Live Traffic";
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Input Live Traffic Feed Geoprocessing Service Name";
                paramEdit.DisplayOrder = InputLiveTrafficFeedGeoprocessingServiceName;
                paramEdit.Enabled = true;
                paramEdit.Name = "input_live_traffic_feed_geoprocessing_service_name";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeOptional;

                paramArray.Add(paramEdit as object);

                // 21 - input_live_traffic_feed_geoprocessing_task_name

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new GPStringTypeClass() as IGPDataType;
                paramEdit.Value = new GPStringClass() as IGPValue;
                paramEdit.Category = "Live Traffic";
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Input Live Traffic Feed Geoprocessing Task Name";
                paramEdit.DisplayOrder = InputLiveTrafficFeedGeoprocessingTaskName;
                paramEdit.Enabled = true;
                paramEdit.Name = "input_live_traffic_feed_geoprocessing_task_name";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeOptional;

                paramArray.Add(paramEdit as object);

                // 22 - input_ltr_table

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new DETableTypeClass() as IGPDataType;
                paramEdit.Value = new DETableClass() as IGPValue;
                paramEdit.Category = "Logistics (North America only)";
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Input Logistics Truck Routes (LTR) Table";
                paramEdit.DisplayOrder = InputLTRTable;
                paramEdit.Enabled = true;
                paramEdit.Name = "input_ltr_table";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeOptional;

                paramArray.Add(paramEdit as object);

                // 23 - input_lrs_table

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new DETableTypeClass() as IGPDataType;
                paramEdit.Value = new DETableClass() as IGPValue;
                paramEdit.Category = "Logistics (North America only)";
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Input Logistics Restrictions (LRS) Table";
                paramEdit.DisplayOrder = InputLRSTable;
                paramEdit.Enabled = true;
                paramEdit.Name = "input_lrs_table";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeOptional;

                paramArray.Add(paramEdit as object);

                // 24 - input_lvc_table

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new DETableTypeClass() as IGPDataType;
                paramEdit.Value = new DETableClass() as IGPValue;
                paramEdit.Category = "Logistics (North America only)";
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Input Logistics Vehicle Characteristics (LVC) Table";
                paramEdit.DisplayOrder = InputLVCTable;
                paramEdit.Enabled = true;
                paramEdit.Name = "input_lvc_table";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeOptional;

                paramArray.Add(paramEdit as object);

                // 25 - output_network_dataset

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new DENetworkDatasetTypeClass() as IGPDataType;
                IDENetworkDataset dend = new DENetworkDatasetClass();
                dend.NetworkType = esriNetworkDatasetType.esriNDTGeodatabase;
                paramEdit.Value = dend as IGPValue;
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionOutput;
                paramEdit.DisplayName = "Output Network Dataset";
                paramEdit.DisplayOrder = OutputNetworkDataset;
                paramEdit.Enabled = true;
                paramEdit.Name = "output_network_dataset";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeDerived;

                paramArray.Add(paramEdit as object);

                return paramArray;
            }
        }

        public IGPMessages Validate(IArray paramvalues, bool updateValues, IGPEnvironmentManager envMgr)
        {
            // Initialize a copy of our parameters

            if (m_parameters == null) m_parameters = ParameterInfo;
            
            // Call UpdateParameters() (only call if updateValues is true)

            if (updateValues)
                UpdateParameters(paramvalues, envMgr);

            // Call InternalValidate (basic validation):
            // - Are all the required parameters supplied?
            // - Are the Values to the parameters the correct data type?

            IGPMessages validateMsgs = m_gpUtils.InternalValidate(m_parameters, paramvalues, updateValues, true, envMgr);

            // Call UpdateMessages()

            UpdateMessages(paramvalues, envMgr, validateMsgs);

            // Return the messages

            return validateMsgs;
        }

        public void UpdateParameters(IArray paramvalues, IGPEnvironmentManager envMgr)
        {
            m_parameters = paramvalues;

            // Disable parameters that are not specific to the selected output FileGDB version

            var gpParam = m_parameters.get_Element(OutputFileGDBVersion) as IGPParameter;
            IGPValue outputFileGDBVersionValue = m_gpUtils.UnpackGPValue(gpParam);
            if (!(outputFileGDBVersionValue.IsEmpty()))
            {
                switch (outputFileGDBVersionValue.GetAsText())
                {
                    case "9.3":
                        // Disable time zones, historical traffic, and live traffic
                        DisableParameter(m_parameters.get_Element(InputTimeZoneIDBaseFieldName) as IGPParameterEdit,
                                         new GPStringClass() as IGPValue);
                        DisableParameter(m_parameters.get_Element(InputTimeZoneTable) as IGPParameterEdit,
                                         new DETableClass() as IGPValue);
                        DisableParameter(m_parameters.get_Element(InputCommonTimeZoneForTheEntireDataset) as IGPParameterEdit,
                                         new GPStringClass() as IGPValue);
                        DisableParameter(m_parameters.get_Element(InputHSNPTable) as IGPParameterEdit,
                                         new DETableClass() as IGPValue);
                        DisableParameter(m_parameters.get_Element(InputHSPRTable) as IGPParameterEdit,
                                         new DETableClass() as IGPValue);
                        DisableParameter(m_parameters.get_Element(InputRDTable) as IGPParameterEdit,
                                         new DETableClass() as IGPValue);
                        DisableParameter(m_parameters.get_Element(InputLiveTrafficFeedFolder) as IGPParameterEdit,
                                         new GPStringClass() as IGPValue);
                        DisableParameter(m_parameters.get_Element(InputLiveTrafficFeedArcGISServerConnection) as IGPParameterEdit,
                                         new DEServerConnectionClass() as IGPValue);
                        DisableParameter(m_parameters.get_Element(InputLiveTrafficFeedGeoprocessingServiceName) as IGPParameterEdit,
                                         new GPStringClass() as IGPValue);
                        DisableParameter(m_parameters.get_Element(InputLiveTrafficFeedGeoprocessingTaskName) as IGPParameterEdit,
                                         new GPStringClass() as IGPValue);
                        break;

                    case "10.0":
                        // Enable time zones and historical traffic; Disable live traffic
                        EnableParameter(m_parameters.get_Element(InputTimeZoneIDBaseFieldName) as IGPParameterEdit);
                        EnableParameter(m_parameters.get_Element(InputTimeZoneTable) as IGPParameterEdit);
                        EnableParameter(m_parameters.get_Element(InputCommonTimeZoneForTheEntireDataset) as IGPParameterEdit);
                        EnableParameter(m_parameters.get_Element(InputHSNPTable) as IGPParameterEdit);
                        EnableParameter(m_parameters.get_Element(InputHSPRTable) as IGPParameterEdit);
                        DisableParameter(m_parameters.get_Element(InputRDTable) as IGPParameterEdit,
                                         new DETableClass() as IGPValue);
                        DisableParameter(m_parameters.get_Element(InputLiveTrafficFeedFolder) as IGPParameterEdit,
                                         new GPStringClass() as IGPValue);
                        DisableParameter(m_parameters.get_Element(InputLiveTrafficFeedArcGISServerConnection) as IGPParameterEdit,
                                         new DEServerConnectionClass() as IGPValue);
                        DisableParameter(m_parameters.get_Element(InputLiveTrafficFeedGeoprocessingServiceName) as IGPParameterEdit,
                                         new GPStringClass() as IGPValue);
                        DisableParameter(m_parameters.get_Element(InputLiveTrafficFeedGeoprocessingTaskName) as IGPParameterEdit,
                                         new GPStringClass() as IGPValue);
                        break;

                    default:
                        // Enable all parameters
                        EnableParameter(m_parameters.get_Element(InputTimeZoneIDBaseFieldName) as IGPParameterEdit);
                        EnableParameter(m_parameters.get_Element(InputTimeZoneTable) as IGPParameterEdit);
                        EnableParameter(m_parameters.get_Element(InputCommonTimeZoneForTheEntireDataset) as IGPParameterEdit);
                        EnableParameter(m_parameters.get_Element(InputHSNPTable) as IGPParameterEdit);
                        EnableParameter(m_parameters.get_Element(InputHSPRTable) as IGPParameterEdit);
                        EnableParameter(m_parameters.get_Element(InputRDTable) as IGPParameterEdit);
                        EnableParameter(m_parameters.get_Element(InputLiveTrafficFeedFolder) as IGPParameterEdit);
                        EnableParameter(m_parameters.get_Element(InputLiveTrafficFeedArcGISServerConnection) as IGPParameterEdit);
                        EnableParameter(m_parameters.get_Element(InputLiveTrafficFeedGeoprocessingServiceName) as IGPParameterEdit);
                        EnableParameter(m_parameters.get_Element(InputLiveTrafficFeedGeoprocessingTaskName) as IGPParameterEdit);
                        break;
                }
            }

            // Get the output geodatabase, feature dataset name, and network dataset name parameters

            gpParam = m_parameters.get_Element(OutputFileGDB) as IGPParameter;
            IGPValue outputFileGDBValue = m_gpUtils.UnpackGPValue(gpParam);
            gpParam = m_parameters.get_Element(InputFeatureDatasetName) as IGPParameter;
            IGPValue featureDatasetNameValue = m_gpUtils.UnpackGPValue(gpParam);
            gpParam = m_parameters.get_Element(InputNetworkDatasetName) as IGPParameter;
            IGPValue networkDatasetNameValue = m_gpUtils.UnpackGPValue(gpParam);

            // Get the output network dataset parameter and pack it based on the path to the output geodatabase

            if (!(outputFileGDBValue.IsEmpty() | featureDatasetNameValue.IsEmpty() | networkDatasetNameValue.IsEmpty()))
            {
                gpParam = paramvalues.get_Element(OutputNetworkDataset) as IGPParameter;
                var dendType = new DENetworkDatasetTypeClass() as IGPDataType;
                m_gpUtils.PackGPValue(dendType.CreateValue(outputFileGDBValue.GetAsText() + "\\" + featureDatasetNameValue.GetAsText() + "\\" + networkDatasetNameValue.GetAsText()), gpParam);
            }

            return;
        }

        public void UpdateMessages(IArray paramvalues, IGPEnvironmentManager pEnvMgr, IGPMessages messages)
        {
            // Check for error message

            IGPMessage msg = (IGPMessage)messages;
            if (msg.IsError())
                return;

            // Verify chosen output file geodatabase has a ".gdb" extension

            var gpParam = paramvalues.get_Element(OutputFileGDB) as IGPParameter;
            IGPValue outputFileGDBValue = m_gpUtils.UnpackGPValue(gpParam);
            if (!outputFileGDBValue.IsEmpty())
            {
                string outputFileGDBValueAsText = outputFileGDBValue.GetAsText();
                if (!(outputFileGDBValueAsText.EndsWith(".gdb")))
                {
                    IGPMessage gpMessage = messages.GetMessage(OutputFileGDB);
                    gpMessage.Type = esriGPMessageType.esriGPMessageTypeError;
                    gpMessage.Description = "Input value is not a valid file geodatabase path.";
                }
            }

            // Verify chosen output file geodatabase version is valid

            gpParam = paramvalues.get_Element(OutputFileGDBVersion) as IGPParameter;
            IGPValue outputFileGDBVersionValue = m_gpUtils.UnpackGPValue(gpParam);
            if (!outputFileGDBVersionValue.IsEmpty())
            {
                switch (outputFileGDBVersionValue.GetAsText())
                {
                    case "9.3":
                    case "10.0":
                    case "10.1":
                        // version is supported
                        break;
                    default:
                        IGPMessage gpMessage = messages.GetMessage(OutputFileGDBVersion);
                        gpMessage.Type = esriGPMessageType.esriGPMessageTypeError;
                        gpMessage.Description = "This version of the file geodatabase is not supported.";
                        break;
                }
            }

            // Verify chosen input NW feature class has the expected fields

            gpParam = paramvalues.get_Element(InputNWFeatureClass) as IGPParameter;
            IGPValue tableValue = m_gpUtils.UnpackGPValue(gpParam);
            if (!tableValue.IsEmpty())
            {
                IDETable inputTable = m_gpUtils.DecodeDETable(tableValue);
                CheckForTableFields(inputTable, NWFieldNames, NWFieldTypes, messages.GetMessage(InputNWFeatureClass));
            }

            // Verify chosen input MN feature class has the expected fields

            gpParam = paramvalues.get_Element(InputMNFeatureClass) as IGPParameter;
            tableValue = m_gpUtils.UnpackGPValue(gpParam);
            if (!tableValue.IsEmpty())
            {
                IDETable inputTable = m_gpUtils.DecodeDETable(tableValue);
                CheckForTableFields(inputTable, MNFieldNames, MNFieldTypes, messages.GetMessage(InputMNFeatureClass));
            }

            // Verify chosen input MP table has the expected fields

            gpParam = paramvalues.get_Element(InputMPTable) as IGPParameter;
            tableValue = m_gpUtils.UnpackGPValue(gpParam);
            if (!tableValue.IsEmpty())
            {
                IDETable inputTable = m_gpUtils.DecodeDETable(tableValue);
                CheckForTableFields(inputTable, MPFieldNames, MPFieldTypes, messages.GetMessage(InputMPTable));
            }

            // Verify chosen input SI table has the expected fields

            gpParam = paramvalues.get_Element(InputSITable) as IGPParameter;
            tableValue = m_gpUtils.UnpackGPValue(gpParam);
            if (!tableValue.IsEmpty())
            {
                IDETable inputTable = m_gpUtils.DecodeDETable(tableValue);
                CheckForTableFields(inputTable, SIFieldNames, SIFieldTypes, messages.GetMessage(InputSITable));
            }

            // Verify chosen input SP table has the expected fields

            gpParam = paramvalues.get_Element(InputSPTable) as IGPParameter;
            tableValue = m_gpUtils.UnpackGPValue(gpParam);
            if (!tableValue.IsEmpty())
            {
                IDETable inputTable = m_gpUtils.DecodeDETable(tableValue);
                CheckForTableFields(inputTable, SPFieldNames, SPFieldTypes, messages.GetMessage(InputSPTable));
            }

            // Verify chosen input RS table has the expected fields

            gpParam = paramvalues.get_Element(InputRSTable) as IGPParameter;
            tableValue = m_gpUtils.UnpackGPValue(gpParam);
            if (!tableValue.IsEmpty())
            {
                IDETable inputTable = m_gpUtils.DecodeDETable(tableValue);
                CheckForTableFields(inputTable, RSFieldNames, RSFieldTypes, messages.GetMessage(InputRSTable));
            }

            // Verify chosen input time zone table has the expected fields

            gpParam = paramvalues.get_Element(InputTimeZoneTable) as IGPParameter;
            tableValue = m_gpUtils.UnpackGPValue(gpParam);
            if (!tableValue.IsEmpty())
            {
                IDETable inputTable = m_gpUtils.DecodeDETable(tableValue);
                CheckForTableFields(inputTable, TimeZoneFieldNames, TimeZoneFieldTypes, messages.GetMessage(InputTimeZoneTable));
            }

            // Verify chosen input HSNP table has the expected fields

            gpParam = paramvalues.get_Element(InputHSNPTable) as IGPParameter;
            tableValue = m_gpUtils.UnpackGPValue(gpParam);
            if (!tableValue.IsEmpty())
            {
                IDETable inputTable = m_gpUtils.DecodeDETable(tableValue);
                CheckForTableFields(inputTable, HSNPFieldNames, HSNPFieldTypes, messages.GetMessage(InputHSNPTable));
            }

            // Verify chosen input HSPR table has the expected fields

            gpParam = paramvalues.get_Element(InputHSPRTable) as IGPParameter;
            tableValue = m_gpUtils.UnpackGPValue(gpParam);
            if (!tableValue.IsEmpty())
            {
                IDETable inputTable = m_gpUtils.DecodeDETable(tableValue);
                CheckForTableFields(inputTable, HSPRFieldNames, HSPRFieldTypes, messages.GetMessage(InputHSPRTable));
            }

            // Verify chosen input RD table has the expected fields

            gpParam = paramvalues.get_Element(InputRDTable) as IGPParameter;
            tableValue = m_gpUtils.UnpackGPValue(gpParam);
            if (!tableValue.IsEmpty())
            {
                IDETable inputTable = m_gpUtils.DecodeDETable(tableValue);
                CheckForTableFields(inputTable, RDFieldNames, RDFieldTypes, messages.GetMessage(InputRDTable));
            }

            // Verify chosen input LTR table has the expected fields

            gpParam = paramvalues.get_Element(InputLTRTable) as IGPParameter;
            tableValue = m_gpUtils.UnpackGPValue(gpParam);
            if (!tableValue.IsEmpty())
            {
                IDETable inputTable = m_gpUtils.DecodeDETable(tableValue);
                CheckForTableFields(inputTable, LTRFieldNames, LTRFieldTypes, messages.GetMessage(InputLTRTable));
            }

            // Verify chosen input LRS table has the expected fields

            gpParam = paramvalues.get_Element(InputLRSTable) as IGPParameter;
            tableValue = m_gpUtils.UnpackGPValue(gpParam);
            if (!tableValue.IsEmpty())
            {
                IDETable inputTable = m_gpUtils.DecodeDETable(tableValue);
                CheckForTableFields(inputTable, LRSFieldNames, LRSFieldTypes, messages.GetMessage(InputLRSTable));
            }

            // Verify chosen input LVC table has the expected fields

            gpParam = paramvalues.get_Element(InputLVCTable) as IGPParameter;
            tableValue = m_gpUtils.UnpackGPValue(gpParam);
            if (!tableValue.IsEmpty())
            {
                IDETable inputTable = m_gpUtils.DecodeDETable(tableValue);
                CheckForTableFields(inputTable, LVCFieldNames, LVCFieldTypes, messages.GetMessage(InputLVCTable));
            }

            return;
        }

        public void Execute(IArray paramvalues, ITrackCancel trackcancel,
                            IGPEnvironmentManager envMgr, IGPMessages messages)
        {
            // Remember the original GP environment settings and temporarily override these settings

            var gpSettings = envMgr as IGeoProcessorSettings;
            bool origAddOutputsToMapSetting = gpSettings.AddOutputsToMap;
            bool origLogHistorySetting = gpSettings.LogHistory;
            gpSettings.AddOutputsToMap = false;
            gpSettings.LogHistory = false;

            // Create the Geoprocessor

            Geoprocessor gp = new Geoprocessor();

            try
            {
                // Validate our values

                IGPMessages validateMessages = ((IGPFunction2)this).Validate(paramvalues, false, envMgr);
                if ((validateMessages as IGPMessage).IsError())
                {
                    messages.AddError(1, "Validate failed");
                    return;
                }

                // Unpack values

                IGPParameter gpParam = paramvalues.get_Element(InputNWFeatureClass) as IGPParameter;
                IGPValue inputNWFeatureClassValue = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(InputMNFeatureClass) as IGPParameter;
                IGPValue inputMNFeatureClassValue = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(InputMPTable) as IGPParameter;
                IGPValue inputMPTableValue = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(InputSITable) as IGPParameter;
                IGPValue inputSITableValue = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(InputSPTable) as IGPParameter;
                IGPValue inputSPTableValue = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(OutputFileGDB) as IGPParameter;
                IGPValue outputFileGDBValue = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(OutputFileGDBVersion) as IGPParameter;
                IGPValue outputFileGDBVersionValue = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(InputFeatureDatasetName) as IGPParameter;
                IGPValue inputFeatureDatasetNameValue = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(InputNetworkDatasetName) as IGPParameter;
                IGPValue inputNetworkDatasetNameValue = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(InputCreateNetworkAttributesInMetric) as IGPParameter;
                IGPValue inputCreateNetworkAttributeInMetricValue = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(InputCreateTwoDistanceAttributes) as IGPParameter;
                IGPValue inputCreateTwoDistanceAttributesValue = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(InputRSTable) as IGPParameter;
                IGPValue inputRSTableValue = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(InputTimeZoneIDBaseFieldName) as IGPParameter;
                IGPValue inputTimeZoneIDBaseFieldNameValue = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(InputTimeZoneTable) as IGPParameter;
                IGPValue inputTimeZoneTableValue = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(InputCommonTimeZoneForTheEntireDataset) as IGPParameter;
                IGPValue inputCommonTimeZoneForTheEntireDatasetValue = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(InputHSNPTable) as IGPParameter;
                IGPValue inputHSNPTableValue = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(InputHSPRTable) as IGPParameter;
                IGPValue inputHSPRTableValue = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(InputRDTable) as IGPParameter;
                IGPValue inputRDTableValue = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(InputLiveTrafficFeedFolder) as IGPParameter;
                IGPValue inputLiveTrafficFeedFolderValue = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(InputLiveTrafficFeedArcGISServerConnection) as IGPParameter;
                IGPValue inputLiveTrafficFeedArcGISServerConnectionValue = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(InputLiveTrafficFeedGeoprocessingServiceName) as IGPParameter;
                IGPValue inputLiveTrafficFeedGeoprocessingServiceNameValue = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(InputLiveTrafficFeedGeoprocessingTaskName) as IGPParameter;
                IGPValue inputLiveTrafficFeedGeoprocessingTaskNameValue = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(InputLTRTable) as IGPParameter;
                IGPValue inputLTRTableValue = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(InputLRSTable) as IGPParameter;
                IGPValue inputLRSTableValue = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(InputLVCTable) as IGPParameter;
                IGPValue inputLVCTableValue = m_gpUtils.UnpackGPValue(gpParam);

                double fgdbVersion = 0.0;
                if (!(outputFileGDBVersionValue.IsEmpty()))
                    fgdbVersion = Convert.ToDouble(outputFileGDBVersionValue.GetAsText(), System.Globalization.CultureInfo.InvariantCulture);
                
                bool createNetworkAttributesInMetric = false;
                if (!(inputCreateNetworkAttributeInMetricValue.IsEmpty()))
                    createNetworkAttributesInMetric = ((inputCreateNetworkAttributeInMetricValue.GetAsText()).ToUpper() == "TRUE");

                bool createTwoDistanceAttributes = false;
                if (!(inputCreateTwoDistanceAttributesValue.IsEmpty()))
                    createTwoDistanceAttributes = ((inputCreateTwoDistanceAttributesValue.GetAsText()).ToUpper() == "TRUE");

                if (inputTimeZoneIDBaseFieldNameValue.IsEmpty() ^ inputTimeZoneTableValue.IsEmpty())
                {
                    messages.AddError(1, "The Time Zone ID Base Field Name and the Time Zone Table must be specified together.");
                    return;
                }

                string timeZoneIDBaseFieldName = "";
                bool directedTimeZoneIDFields = false;
                if (!(inputTimeZoneIDBaseFieldNameValue.IsEmpty()))
                {
                    timeZoneIDBaseFieldName = inputTimeZoneIDBaseFieldNameValue.GetAsText();
                    IDETable inputTable = m_gpUtils.DecodeDETable(inputNWFeatureClassValue);
                    if (inputTable.Fields.FindField(timeZoneIDBaseFieldName) == -1)
                    {
                        directedTimeZoneIDFields = true;
                        if (((inputTable.Fields.FindField("FT_" + timeZoneIDBaseFieldName) == -1) || (inputTable.Fields.FindField("TF_" + timeZoneIDBaseFieldName) == -1)))
                        {
                            messages.AddError(1, "Field named " + timeZoneIDBaseFieldName + " does not exist, nor the pair of fields with FT_ and TF_ prefixing " + timeZoneIDBaseFieldName + " does not exist.");
                            return;
                        }
                    }
                }

                string commonTimeZone = "";
                if (!(inputCommonTimeZoneForTheEntireDatasetValue.IsEmpty()))
                    commonTimeZone = inputCommonTimeZoneForTheEntireDatasetValue.GetAsText();

                if (inputHSNPTableValue.IsEmpty() ^ inputHSPRTableValue.IsEmpty())
                {
                    messages.AddError(1, "The HSNP and HSPR tables must be specified together.");
                    return;
                }

                bool agsConnectionIsEmpty = inputLiveTrafficFeedArcGISServerConnectionValue.IsEmpty();
                bool gpServiceNameIsEmpty = inputLiveTrafficFeedGeoprocessingServiceNameValue.IsEmpty();
                bool gpTaskNameIsEmpty = inputLiveTrafficFeedGeoprocessingTaskNameValue.IsEmpty();
                if ((agsConnectionIsEmpty | gpServiceNameIsEmpty | gpTaskNameIsEmpty) ^
                    (agsConnectionIsEmpty & gpServiceNameIsEmpty & gpTaskNameIsEmpty))
                {
                    messages.AddError(1, "The ArcGIS Server Connection, Geoprocessing Service Name, and Geoprocessing Task Name must all be specified together.");
                    return;
                }

                bool feedFolderIsEmpty = inputLiveTrafficFeedFolderValue.IsEmpty();
                if (!(feedFolderIsEmpty | agsConnectionIsEmpty))
                {
                    messages.AddError(1, "The live traffic feed folder and live traffic feed connection cannot be specified together.");
                    return;
                }

                bool usesLiveTraffic = !(inputRDTableValue.IsEmpty());
                if (!usesLiveTraffic ^ (feedFolderIsEmpty & agsConnectionIsEmpty))
                {
                    messages.AddError(1, "The RD table and live traffic feed folder or connection must be specified together.");
                    return;
                }

                ITrafficFeedLocation trafficFeedLocation = null;
                if (usesLiveTraffic)
                {
                    if (feedFolderIsEmpty)
                    {
                        // We're using an ArcGIS Server Connection and Geoprocessing Service/Task

                        ITrafficFeedGPService tfgps = new TrafficFeedGPServiceClass();
                        IName trafficFeedGPServiceName = m_gpUtils.GetNameObject(inputLiveTrafficFeedArcGISServerConnectionValue as IDataElement);
                        tfgps.ConnectionProperties = ((IAGSServerConnectionName)trafficFeedGPServiceName).ConnectionProperties;
                        tfgps.ServiceName = inputLiveTrafficFeedGeoprocessingServiceNameValue.GetAsText();
                        tfgps.TaskName = inputLiveTrafficFeedGeoprocessingTaskNameValue.GetAsText();
                        trafficFeedLocation = tfgps as ITrafficFeedLocation;
                    }
                    else
                    {
                        // We're using a Traffic Feed Folder

                        ITrafficFeedDirectory tfd = new TrafficFeedDirectoryClass();
                        tfd.TrafficDirectory = inputLiveTrafficFeedFolderValue.GetAsText();
                        trafficFeedLocation = tfd as ITrafficFeedLocation;
                    }
                }

                if (inputLRSTableValue.IsEmpty() ^ inputLVCTableValue.IsEmpty())
                {
                    messages.AddError(1, "The LRS and LVC tables must be specified together.");
                    return;
                }

                // Get the path to the output file GDB, and feature dataset and network dataset names

                string outputFileGdbPath = outputFileGDBValue.GetAsText();
                string fdsName = inputFeatureDatasetNameValue.GetAsText();
                string ndsName = inputNetworkDatasetNameValue.GetAsText();

                // Create the new file geodatabase and feature dataset

                AddMessage("Creating the file geodatabase and feature dataset...", messages, trackcancel);

                int lastBackslash = outputFileGdbPath.LastIndexOf("\\");
                CreateFileGDB createFGDBTool = new CreateFileGDB();
                createFGDBTool.out_folder_path = outputFileGdbPath.Remove(lastBackslash);
                createFGDBTool.out_name = outputFileGdbPath.Substring(lastBackslash + 1);
                createFGDBTool.out_version = "9.3";
                gp.Execute(createFGDBTool, trackcancel);

                CreateFeatureDataset createFDSTool = new CreateFeatureDataset();
                createFDSTool.out_dataset_path = outputFileGdbPath;
                createFDSTool.out_name = fdsName;
                createFDSTool.spatial_reference = inputNWFeatureClassValue.GetAsText();
                gp.Execute(createFDSTool, trackcancel);

                // Import the NW feature class to the file geodatabase
                // If we're using ArcInfo, also sort the feature class

                MakeFeatureLayer makeFeatureLayerTool = new MakeFeatureLayer();
                makeFeatureLayerTool.in_features = inputNWFeatureClassValue.GetAsText();
                makeFeatureLayerTool.out_layer = "nw_Layer";
                makeFeatureLayerTool.where_clause = "FEATTYP <> 4165";
                gp.Execute(makeFeatureLayerTool, trackcancel);

                string pathToFds = outputFileGdbPath + "\\" + fdsName;
                string streetsFeatureClassPath = pathToFds + "\\" + StreetsFCName;
                FeatureClassToFeatureClass importFCTool = null;

                IAoInitialize aoi = new AoInitializeClass();
                if (aoi.InitializedProduct() == esriLicenseProductCode.esriLicenseProductCodeAdvanced)
                {
                    AddMessage("Importing and spatially sorting the Streets feature class...", messages, trackcancel);

                    Sort sortTool = new Sort();
                    sortTool.in_dataset = "nw_Layer";
                    sortTool.out_dataset = streetsFeatureClassPath;
                    sortTool.sort_field = "Shape";
                    sortTool.spatial_sort_method = "PEANO";
                    gp.Execute(sortTool, trackcancel);
                }
                else
                {
                    AddMessage("Importing the Streets feature class...", messages, trackcancel);

                    importFCTool = new FeatureClassToFeatureClass();
                    importFCTool.in_features = "nw_Layer";
                    importFCTool.out_path = pathToFds;
                    importFCTool.out_name = StreetsFCName;
                    gp.Execute(importFCTool, trackcancel);
                }

                Delete deleteTool = new Delete();
                deleteTool.in_data = "nw_Layer";
                gp.Execute(deleteTool, trackcancel);

                // Add an index to the Streets feature class's ID field

                AddMessage("Indexing the ID field...", messages, trackcancel);

                AddIndex addIndexTool = new AddIndex();
                addIndexTool.in_table = streetsFeatureClassPath;
                addIndexTool.fields = "ID";
                addIndexTool.index_name = "ID";
                gp.Execute(addIndexTool, trackcancel);

                // Copy the time zones table to the file geodatabase

                TableToTable importTableTool = null;
                if (timeZoneIDBaseFieldName != "")
                {
                    AddMessage("Copying the TimeZones table...", messages, trackcancel);

                    importTableTool = new TableToTable();
                    importTableTool.in_rows = inputTimeZoneTableValue.GetAsText();
                    importTableTool.out_path = outputFileGdbPath;
                    importTableTool.out_name = "TimeZones";
                    gp.Execute(importTableTool, trackcancel);
                }

                // Initialize common variables that will be used for both
                // processing historical traffic and maneuvers
                AddField addFieldTool = null;
                AddJoin addJoinTool = null;
                RemoveJoin removeJoinTool = null;
                CalculateField calcFieldTool = null;
                TableSelect tableSelectTool = null;
                MakeTableView makeTableViewTool = null;

                #region Process Historical Traffic Tables

                bool usesHistoricalTraffic = false;

                if (!(inputHSNPTableValue.IsEmpty()))
                {
                    usesHistoricalTraffic = true;

                    // Add fields for the weekday/weekend/all-week averages to the Streets feature class

                    AddMessage("Creating fields for the weekday/weekend/all-week averages...", messages, trackcancel);

                    addFieldTool = new AddField();
                    addFieldTool.in_table = streetsFeatureClassPath;

                    addFieldTool.field_type = "SHORT";
                    addFieldTool.field_name = "FT_Weekday";
                    gp.Execute(addFieldTool, trackcancel);
                    addFieldTool.field_name = "TF_Weekday";
                    gp.Execute(addFieldTool, trackcancel);
                    addFieldTool.field_name = "FT_Weekend";
                    gp.Execute(addFieldTool, trackcancel);
                    addFieldTool.field_name = "TF_Weekend";
                    gp.Execute(addFieldTool, trackcancel);
                    addFieldTool.field_name = "FT_AllWeek";
                    gp.Execute(addFieldTool, trackcancel);
                    addFieldTool.field_name = "TF_AllWeek";
                    gp.Execute(addFieldTool, trackcancel);

                    addFieldTool.field_type = "FLOAT";
                    addFieldTool.field_name = "FT_WeekdayMinutes";
                    gp.Execute(addFieldTool, trackcancel);
                    addFieldTool.field_name = "TF_WeekdayMinutes";
                    gp.Execute(addFieldTool, trackcancel);
                    addFieldTool.field_name = "FT_WeekendMinutes";
                    gp.Execute(addFieldTool, trackcancel);
                    addFieldTool.field_name = "TF_WeekendMinutes";
                    gp.Execute(addFieldTool, trackcancel);
                    addFieldTool.field_name = "FT_AllWeekMinutes";
                    gp.Execute(addFieldTool, trackcancel);
                    addFieldTool.field_name = "TF_AllWeekMinutes";
                    gp.Execute(addFieldTool, trackcancel);

                    // Separate out the FT and TF speeds into separate tables and index the NETWORK_ID fields

                    AddMessage("Extracting speed information...", messages, trackcancel);

                    string FTSpeedsTablePath = outputFileGdbPath + "\\FT_Speeds";
                    string TFSpeedsTablePath = outputFileGdbPath + "\\TF_Speeds";

                    tableSelectTool = new TableSelect();
                    tableSelectTool.in_table = inputHSNPTableValue.GetAsText();
                    tableSelectTool.out_table = FTSpeedsTablePath;
                    tableSelectTool.where_clause = "VAL_DIR = 2";
                    gp.Execute(tableSelectTool, trackcancel);

                    tableSelectTool.out_table = TFSpeedsTablePath;
                    tableSelectTool.where_clause = "VAL_DIR = 3";
                    gp.Execute(tableSelectTool, trackcancel);

                    addIndexTool = new AddIndex();
                    addIndexTool.in_table = FTSpeedsTablePath;
                    addIndexTool.fields = "NETWORK_ID";
                    addIndexTool.index_name = "NETWORK_ID";
                    gp.Execute(addIndexTool, trackcancel);
                    addIndexTool.in_table = TFSpeedsTablePath;
                    gp.Execute(addIndexTool, trackcancel);

                    // Calculate the speeds fields

                    makeFeatureLayerTool = new MakeFeatureLayer();
                    makeFeatureLayerTool.in_features = streetsFeatureClassPath;
                    makeFeatureLayerTool.out_layer = "Streets_Layer";
                    gp.Execute(makeFeatureLayerTool, trackcancel);

                    addJoinTool = new AddJoin();
                    addJoinTool.in_layer_or_view = "Streets_Layer";
                    addJoinTool.in_field = "ID";
                    addJoinTool.join_table = FTSpeedsTablePath;
                    addJoinTool.join_field = "NETWORK_ID";
                    gp.Execute(addJoinTool, trackcancel);

                    AddMessage("Copying over the FT weekday speeds...", messages, trackcancel);

                    calcFieldTool = new CalculateField();
                    calcFieldTool.in_table = "Streets_Layer";
                    calcFieldTool.field = StreetsFCName + ".FT_Weekday";
                    calcFieldTool.expression = "[FT_Speeds.SPWEEKDAY]";
                    calcFieldTool.expression_type = "VB";
                    gp.Execute(calcFieldTool, trackcancel);

                    AddMessage("Copying over the FT weekend speeds...", messages, trackcancel);

                    calcFieldTool.field = StreetsFCName + ".FT_Weekend";
                    calcFieldTool.expression = "[FT_Speeds.SPWEEKEND]";
                    calcFieldTool.expression_type = "VB";
                    gp.Execute(calcFieldTool, trackcancel);

                    AddMessage("Copying over the FT all-week speeds...", messages, trackcancel);

                    calcFieldTool.field = StreetsFCName + ".FT_AllWeek";
                    calcFieldTool.expression = "[FT_Speeds.SPWEEK]";
                    calcFieldTool.expression_type = "VB";
                    gp.Execute(calcFieldTool, trackcancel);

                    removeJoinTool = new RemoveJoin();
                    removeJoinTool.in_layer_or_view = "Streets_Layer";
                    removeJoinTool.join_name = "FT_Speeds";
                    gp.Execute(removeJoinTool, trackcancel);

                    addJoinTool = new AddJoin();
                    addJoinTool.in_layer_or_view = "Streets_Layer";
                    addJoinTool.in_field = "ID";
                    addJoinTool.join_table = TFSpeedsTablePath;
                    addJoinTool.join_field = "NETWORK_ID";
                    gp.Execute(addJoinTool, trackcancel);

                    AddMessage("Copying over the TF weekday speeds...", messages, trackcancel);

                    calcFieldTool = new CalculateField();
                    calcFieldTool.in_table = "Streets_Layer";
                    calcFieldTool.field = StreetsFCName + ".TF_Weekday";
                    calcFieldTool.expression = "[TF_Speeds.SPWEEKDAY]";
                    calcFieldTool.expression_type = "VB";
                    gp.Execute(calcFieldTool, trackcancel);

                    AddMessage("Copying over the TF weekend speeds...", messages, trackcancel);

                    calcFieldTool.field = StreetsFCName + ".TF_Weekend";
                    calcFieldTool.expression = "[TF_Speeds.SPWEEKEND]";
                    calcFieldTool.expression_type = "VB";
                    gp.Execute(calcFieldTool, trackcancel);

                    AddMessage("Copying over the TF all-week speeds...", messages, trackcancel);

                    calcFieldTool.field = StreetsFCName + ".TF_AllWeek";
                    calcFieldTool.expression = "[TF_Speeds.SPWEEK]";
                    calcFieldTool.expression_type = "VB";
                    gp.Execute(calcFieldTool, trackcancel);

                    removeJoinTool = new RemoveJoin();
                    removeJoinTool.in_layer_or_view = "Streets_Layer";
                    removeJoinTool.join_name = "TF_Speeds";
                    gp.Execute(removeJoinTool, trackcancel);

                    deleteTool = new Delete();
                    deleteTool.in_data = "Streets_Layer";
                    gp.Execute(deleteTool, trackcancel);

                    deleteTool.in_data = FTSpeedsTablePath;
                    gp.Execute(deleteTool, trackcancel);
                    deleteTool.in_data = TFSpeedsTablePath;
                    gp.Execute(deleteTool, trackcancel);

                    // Calculate the travel time fields

                    AddMessage("Calculating the FT weekday travel times...", messages, trackcancel);

                    calcFieldTool = new CalculateField();
                    calcFieldTool.in_table = streetsFeatureClassPath;

                    calcFieldTool.field = "FT_WeekdayMinutes";
                    calcFieldTool.expression = "[METERS] * 0.06 / s";
                    calcFieldTool.code_block = "s = [FT_Weekday]\nIf IsNull(s) Then s = [KPH]";
                    calcFieldTool.expression_type = "VB";
                    gp.Execute(calcFieldTool, trackcancel);

                    AddMessage("Calculating the FT weekend travel times...", messages, trackcancel);

                    calcFieldTool.field = "FT_WeekendMinutes";
                    calcFieldTool.expression = "[METERS] * 0.06 / s";
                    calcFieldTool.code_block = "s = [FT_Weekend]\nIf IsNull(s) Then s = [KPH]";
                    calcFieldTool.expression_type = "VB";
                    gp.Execute(calcFieldTool, trackcancel);

                    AddMessage("Calculating the FT all-week travel times...", messages, trackcancel);

                    calcFieldTool.field = "FT_AllWeekMinutes";
                    calcFieldTool.expression = "[METERS] * 0.06 / s";
                    calcFieldTool.code_block = "s = [FT_AllWeek]\nIf IsNull(s) Then s = [KPH]";
                    calcFieldTool.expression_type = "VB";
                    gp.Execute(calcFieldTool, trackcancel);

                    AddMessage("Calculating the TF weekday travel times...", messages, trackcancel);

                    calcFieldTool.field = "TF_WeekdayMinutes";
                    calcFieldTool.expression = "[METERS] * 0.06 / s";
                    calcFieldTool.code_block = "s = [TF_Weekday]\nIf IsNull(s) Then s = [KPH]";
                    calcFieldTool.expression_type = "VB";
                    gp.Execute(calcFieldTool, trackcancel);

                    AddMessage("Calculating the TF weekend travel times...", messages, trackcancel);

                    calcFieldTool.field = "TF_WeekendMinutes";
                    calcFieldTool.expression = "[METERS] * 0.06 / s";
                    calcFieldTool.code_block = "s = [TF_Weekend]\nIf IsNull(s) Then s = [KPH]";
                    calcFieldTool.expression_type = "VB";
                    gp.Execute(calcFieldTool, trackcancel);

                    AddMessage("Calculating the TF all-week travel times...", messages, trackcancel);

                    calcFieldTool.field = "TF_AllWeekMinutes";
                    calcFieldTool.expression = "[METERS] * 0.06 / s";
                    calcFieldTool.code_block = "s = [TF_AllWeek]\nIf IsNull(s) Then s = [KPH]";
                    calcFieldTool.expression_type = "VB";
                    gp.Execute(calcFieldTool, trackcancel);

                    // Create the DailyProfiles table

                    AddMessage("Creating the profiles table...", messages, trackcancel);

                    CreateDailyProfilesTable(m_gpUtils.OpenDataset(inputHSPRTableValue) as ITable, outputFileGdbPath, fgdbVersion);

                    // Copy over the historical traffic records of the HSNP table to the file geodatabase

                    AddMessage("Copying HSNP table to the file geodatabase...", messages, trackcancel);

                    string histTrafficJoinTablePath = outputFileGdbPath + "\\" + HistTrafficJoinTableName;

                    tableSelectTool.out_table = histTrafficJoinTablePath;
                    tableSelectTool.where_clause = "SPFREEFLOW > 0";
                    gp.Execute(tableSelectTool, trackcancel);

                    // Add FCID, FID, and position fields to the Streets_DailyProfiles table

                    AddMessage("Creating fields on the historical traffic join table...", messages, trackcancel);

                    addFieldTool = new AddField();
                    addFieldTool.in_table = histTrafficJoinTablePath;

                    addFieldTool.field_type = "LONG";
                    addFieldTool.field_name = "EdgeFCID";
                    gp.Execute(addFieldTool, trackcancel);
                    addFieldTool.field_name = "EdgeFID";
                    gp.Execute(addFieldTool, trackcancel);

                    addFieldTool.field_type = "DOUBLE";
                    addFieldTool.field_name = "EdgeFrmPos";
                    gp.Execute(addFieldTool, trackcancel);
                    addFieldTool.field_name = "EdgeToPos";
                    gp.Execute(addFieldTool, trackcancel);

                    // If we're creating 10.0, then also create the FreeflowMinutes field

                    if (fgdbVersion == 10.0)
                    {
                        addFieldTool.field_type = "DOUBLE";
                        addFieldTool.field_name = "FreeflowMinutes";
                        gp.Execute(addFieldTool, trackcancel);
                    }

                    // Calculate the fields

                    AddMessage("Calculating the EdgeFrmPos field for historical traffic...", messages, trackcancel);

                    calcFieldTool = new CalculateField();
                    calcFieldTool.in_table = histTrafficJoinTablePath;

                    calcFieldTool.field = "EdgeFrmPos";
                    calcFieldTool.expression = "x";
                    calcFieldTool.code_block = "Select Case [VAL_DIR]\n  Case 2: x = 0\n  Case 3: x = 1\nEnd Select";
                    calcFieldTool.expression_type = "VB";
                    gp.Execute(calcFieldTool, trackcancel);

                    AddMessage("Calculating the EdgeToPos field for historical traffic...", messages, trackcancel);

                    calcFieldTool.field = "EdgeToPos";
                    calcFieldTool.expression = "x";
                    calcFieldTool.code_block = "Select Case [VAL_DIR]\n  Case 2: x = 1\n  Case 3: x = 0\nEnd Select";
                    calcFieldTool.expression_type = "VB";
                    gp.Execute(calcFieldTool, trackcancel);

                    AddMessage("Calculating the EdgeFCID field for historical traffic...", messages, trackcancel);

                    calcFieldTool = new CalculateField();
                    calcFieldTool.in_table = histTrafficJoinTablePath;
                    calcFieldTool.field = "EdgeFCID";
                    calcFieldTool.expression = "1";
                    calcFieldTool.expression_type = "VB";
                    gp.Execute(calcFieldTool, trackcancel);

                    AddMessage("Calculating the EdgeFID field for historical traffic...", messages, trackcancel);

                    makeTableViewTool = new MakeTableView();
                    makeTableViewTool.in_table = histTrafficJoinTablePath;
                    makeTableViewTool.out_view = "Streets_DailyProfiles_View";
                    gp.Execute(makeTableViewTool, trackcancel);

                    addJoinTool = new AddJoin();
                    addJoinTool.in_layer_or_view = "Streets_DailyProfiles_View";
                    addJoinTool.in_field = "NETWORK_ID";
                    addJoinTool.join_table = streetsFeatureClassPath;
                    addJoinTool.join_field = "ID";
                    gp.Execute(addJoinTool, trackcancel);

                    calcFieldTool = new CalculateField();
                    calcFieldTool.in_table = "Streets_DailyProfiles_View";
                    calcFieldTool.field = HistTrafficJoinTableName + ".EdgeFID";
                    calcFieldTool.expression = "[" + StreetsFCName + ".OBJECTID]";
                    calcFieldTool.expression_type = "VB";
                    gp.Execute(calcFieldTool, trackcancel);

                    if (fgdbVersion == 10.0)
                    {
                        AddMessage("Calculating the FreeflowMinutes field...", messages, trackcancel);

                        calcFieldTool = new CalculateField();
                        calcFieldTool.in_table = "Streets_DailyProfiles_View";
                        calcFieldTool.field = HistTrafficJoinTableName + ".FreeflowMinutes";
                        calcFieldTool.expression = "[" + StreetsFCName + ".METERS] * 0.06 / [" + HistTrafficJoinTableName + ".SPFREEFLOW]";
                        calcFieldTool.expression_type = "VB";
                        gp.Execute(calcFieldTool, trackcancel);
                    }

                    removeJoinTool = new RemoveJoin();
                    removeJoinTool.in_layer_or_view = "Streets_DailyProfiles_View";
                    removeJoinTool.join_name = StreetsFCName;
                    gp.Execute(removeJoinTool, trackcancel);

                    deleteTool = new Delete();
                    deleteTool.in_data = "Streets_DailyProfiles_View";
                    gp.Execute(deleteTool, trackcancel);

                    // Add an index to the Streets feature class's NET2CLASS field

                    AddMessage("Indexing the NET2CLASS field...", messages, trackcancel);

                    addIndexTool = new AddIndex();
                    addIndexTool.in_table = streetsFeatureClassPath;
                    addIndexTool.fields = "NET2CLASS";
                    addIndexTool.index_name = "NET2CLASS";
                    gp.Execute(addIndexTool, trackcancel);
                }
                #endregion

                #region Process Live Traffic Table

                if (usesLiveTraffic)
                {
                    // Copy the RD table to the file geodatabase

                    AddMessage("Creating the live traffic join table...", messages, trackcancel);

                    importTableTool = new TableToTable();
                    importTableTool.in_rows = inputRDTableValue.GetAsText();
                    importTableTool.out_path = outputFileGdbPath;
                    importTableTool.out_name = TMCJoinTableName;
                    gp.Execute(importTableTool, trackcancel);

                    string TMCJoinTablePath = outputFileGdbPath + "\\" + TMCJoinTableName;

                    // Add FCID, FID, position, and TMC fields to the Streets_TMC table

                    AddMessage("Creating fields on the live traffic join table...", messages, trackcancel);

                    addFieldTool = new AddField();
                    addFieldTool.in_table = TMCJoinTablePath;

                    addFieldTool.field_type = "LONG";
                    addFieldTool.field_name = "EdgeFCID";
                    gp.Execute(addFieldTool, trackcancel);
                    addFieldTool.field_name = "EdgeFID";
                    gp.Execute(addFieldTool, trackcancel);

                    addFieldTool.field_type = "DOUBLE";
                    addFieldTool.field_name = "EdgeFrmPos";
                    gp.Execute(addFieldTool, trackcancel);
                    addFieldTool.field_name = "EdgeToPos";
                    gp.Execute(addFieldTool, trackcancel);

                    addFieldTool.field_type = "TEXT";
                    addFieldTool.field_length = 9;
                    addFieldTool.field_name = "TMC";
                    gp.Execute(addFieldTool, trackcancel);

                    // Calculate the fields

                    AddMessage("Calculating the TMC field for live traffic...", messages, trackcancel);

                    calcFieldTool = new CalculateField();
                    calcFieldTool.in_table = TMCJoinTablePath;
                    calcFieldTool.field = "TMC";
                    calcFieldTool.expression = "Right([RDSTMC], 9)";
                    calcFieldTool.expression_type = "VB";
                    gp.Execute(calcFieldTool, trackcancel);

                    AddMessage("Calculating the EdgeFrmPos field for live traffic...", messages, trackcancel);

                    calcFieldTool.field = "EdgeFrmPos";
                    calcFieldTool.expression = "x";
                    calcFieldTool.code_block = "Select Case Left([RDSTMC], 1)\n  Case \"+\": x = 0\n  Case \"-\": x = 1\nEnd Select";
                    calcFieldTool.expression_type = "VB";
                    gp.Execute(calcFieldTool, trackcancel);

                    AddMessage("Calculating the EdgeToPos field for live traffic...", messages, trackcancel);

                    calcFieldTool.field = "EdgeToPos";
                    calcFieldTool.expression = "x";
                    calcFieldTool.code_block = "Select Case Left([RDSTMC], 1)\n  Case \"-\": x = 0\n  Case \"+\": x = 1\nEnd Select";
                    calcFieldTool.expression_type = "VB";
                    gp.Execute(calcFieldTool, trackcancel);

                    AddMessage("Calculating the EdgeFCID field for live traffic...", messages, trackcancel);

                    calcFieldTool = new CalculateField();
                    calcFieldTool.in_table = TMCJoinTablePath;
                    calcFieldTool.field = "EdgeFCID";
                    calcFieldTool.expression = "1";
                    calcFieldTool.expression_type = "VB";
                    gp.Execute(calcFieldTool, trackcancel);

                    AddMessage("Calculating the EdgeFID field for live traffic...", messages, trackcancel);

                    makeTableViewTool = new MakeTableView();
                    makeTableViewTool.in_table = TMCJoinTablePath;
                    makeTableViewTool.out_view = "Streets_TMC_View";
                    gp.Execute(makeTableViewTool, trackcancel);

                    addJoinTool = new AddJoin();
                    addJoinTool.in_layer_or_view = "Streets_TMC_View";
                    addJoinTool.in_field = "ID";
                    addJoinTool.join_table = streetsFeatureClassPath;
                    addJoinTool.join_field = "ID";
                    gp.Execute(addJoinTool, trackcancel);

                    calcFieldTool = new CalculateField();
                    calcFieldTool.in_table = "Streets_TMC_View";
                    calcFieldTool.field = TMCJoinTableName + ".EdgeFID";
                    calcFieldTool.expression = "[" + StreetsFCName + ".OBJECTID]";
                    calcFieldTool.expression_type = "VB";
                    gp.Execute(calcFieldTool, trackcancel);

                    removeJoinTool = new RemoveJoin();
                    removeJoinTool.in_layer_or_view = "Streets_TMC_View";
                    removeJoinTool.join_name = StreetsFCName;
                    gp.Execute(removeJoinTool, trackcancel);

                    deleteTool = new Delete();
                    deleteTool.in_data = "Streets_TMC_View";
                    gp.Execute(deleteTool, trackcancel);

                    // If Historical Traffic is not being used, then we need to create placeholder historical traffic tables

                    if (!usesHistoricalTraffic)
                    {
                        // Create the Streets_DailyProfiles table by starting with a copy of the Streets_TMC table

                        AddMessage("Creating the historical traffic join table...", messages, trackcancel);

                        importTableTool = new TableToTable();
                        importTableTool.in_rows = TMCJoinTablePath;
                        importTableTool.out_path = outputFileGdbPath;
                        importTableTool.out_name = HistTrafficJoinTableName;
                        gp.Execute(importTableTool, trackcancel);

                        string histTrafficJoinTablePath = outputFileGdbPath + "\\" + HistTrafficJoinTableName;

                        AddMessage("Creating and calculating the KPH field on the historical traffic join table...", messages, trackcancel);

                        addFieldTool = new AddField();
                        addFieldTool.in_table = histTrafficJoinTablePath;
                        addFieldTool.field_type = "SHORT";
                        addFieldTool.field_name = "KPH";
                        gp.Execute(addFieldTool, trackcancel);

                        makeTableViewTool = new MakeTableView();
                        makeTableViewTool.in_table = histTrafficJoinTablePath;
                        makeTableViewTool.out_view = "Streets_DailyProfiles_View";
                        gp.Execute(makeTableViewTool, trackcancel);

                        addJoinTool = new AddJoin();
                        addJoinTool.in_layer_or_view = "Streets_DailyProfiles_View";
                        addJoinTool.in_field = "EdgeFID";
                        addJoinTool.join_table = streetsFeatureClassPath;
                        addJoinTool.join_field = "OBJECTID";
                        gp.Execute(addJoinTool, trackcancel);

                        calcFieldTool = new CalculateField();
                        calcFieldTool.in_table = "Streets_DailyProfiles_View";
                        calcFieldTool.field = HistTrafficJoinTableName + ".KPH";
                        calcFieldTool.expression = "[" + StreetsFCName + ".KPH]";
                        calcFieldTool.expression_type = "VB";
                        gp.Execute(calcFieldTool, trackcancel);

                        removeJoinTool = new RemoveJoin();
                        removeJoinTool.in_layer_or_view = "Streets_DailyProfiles_View";
                        removeJoinTool.join_name = StreetsFCName;
                        gp.Execute(removeJoinTool, trackcancel);

                        deleteTool = new Delete();
                        deleteTool.in_data = "Streets_DailyProfiles_View";
                        gp.Execute(deleteTool, trackcancel);

                        AddMessage("Creating and calculating the PROFILE fields on the historical traffic join table...", messages, trackcancel);

                        for (int i = 1; i <= 7; i++)
                        {
                            string fieldName = "PROFILE_" + Convert.ToString(i, System.Globalization.CultureInfo.InvariantCulture);

                            addFieldTool = new AddField();
                            addFieldTool.in_table = histTrafficJoinTablePath;
                            addFieldTool.field_type = "SHORT";
                            addFieldTool.field_name = fieldName;
                            gp.Execute(addFieldTool, trackcancel);

                            calcFieldTool = new CalculateField();
                            calcFieldTool.in_table = histTrafficJoinTablePath;
                            calcFieldTool.field = fieldName;
                            calcFieldTool.expression = "1";
                            calcFieldTool.expression_type = "VB";
                            gp.Execute(calcFieldTool, trackcancel);
                        }

                        // Create the Profiles table

                        CreateNonHistoricalDailyProfilesTable(outputFileGdbPath);
                    }
                }
                #endregion

                // Copy the MN feature class to the file geodatabase

                AddMessage("Copying the Maneuvers feature class and indexing...", messages, trackcancel);

                importFCTool = new FeatureClassToFeatureClass();
                importFCTool.in_features = inputMNFeatureClassValue.GetAsText();
                importFCTool.out_path = outputFileGdbPath;
                importFCTool.out_name = "mn";
                gp.Execute(importFCTool, trackcancel);

                string mnFeatureClassPath = outputFileGdbPath + "\\mn";

                addIndexTool = new AddIndex();
                addIndexTool.in_table = mnFeatureClassPath;
                addIndexTool.fields = "ID";
                addIndexTool.index_name = "ID";
                gp.Execute(addIndexTool, trackcancel);

                // Copy the MP table to the file geodatabase

                AddMessage("Copying the Maneuver Path table...", messages, trackcancel);

                importTableTool = new TableToTable();
                importTableTool.in_rows = inputMPTableValue.GetAsText();
                importTableTool.out_path = outputFileGdbPath;
                importTableTool.out_name = "mp";
                gp.Execute(importTableTool, trackcancel);

                string mpTablePath = outputFileGdbPath + "\\mp";

                // Add and calculate the at junction and feature type fields to the MP table

                AddMessage("Creating and calculating fields for the maneuver types and at junctions...", messages, trackcancel);

                addFieldTool = new AddField();
                addFieldTool.in_table = mpTablePath;
                addFieldTool.field_name = "JNCTID";
                addFieldTool.field_type = "DOUBLE";
                gp.Execute(addFieldTool, trackcancel);

                addFieldTool.field_name = "FEATTYP";
                addFieldTool.field_type = "SHORT";
                gp.Execute(addFieldTool, trackcancel);

                makeTableViewTool = new MakeTableView();
                makeTableViewTool.in_table = mpTablePath;
                makeTableViewTool.out_view = "mp_View";
                gp.Execute(makeTableViewTool, trackcancel);

                addJoinTool = new AddJoin();
                addJoinTool.in_layer_or_view = "mp_View";
                addJoinTool.in_field = "ID";
                addJoinTool.join_table = mnFeatureClassPath;
                addJoinTool.join_field = "ID";
                gp.Execute(addJoinTool, trackcancel);

                calcFieldTool = new CalculateField();
                calcFieldTool.in_table = "mp_View";
                calcFieldTool.field = "mp.JNCTID";
                calcFieldTool.expression = "[mn.JNCTID]";
                calcFieldTool.expression_type = "VB";
                gp.Execute(calcFieldTool, trackcancel);

                calcFieldTool.field = "mp.FEATTYP";
                calcFieldTool.expression = "[mn.FEATTYP]";
                calcFieldTool.expression_type = "VB";
                gp.Execute(calcFieldTool, trackcancel);

                removeJoinTool = new RemoveJoin();
                removeJoinTool.in_layer_or_view = "mp_View";
                removeJoinTool.join_name = "mn";
                gp.Execute(removeJoinTool, trackcancel);

                deleteTool = new Delete();
                deleteTool.in_data = "mp_View";
                gp.Execute(deleteTool, trackcancel);

                deleteTool.in_data = mnFeatureClassPath;
                gp.Execute(deleteTool, trackcancel);

                // Extract only the prohibited maneuvers (feature types 2103 and 2101)

                AddMessage("Extracting prohibited maneuvers...", messages, trackcancel);

                string prohibMPwJnctIDTablePath = outputFileGdbPath + "\\ProhibMPwJnctID";
                string tempTablePath = outputFileGdbPath + "\\TempTable";

                tableSelectTool = new TableSelect();
                tableSelectTool.in_table = mpTablePath;
                tableSelectTool.out_table = prohibMPwJnctIDTablePath;
                tableSelectTool.where_clause = "FEATTYP = 2103";
                gp.Execute(tableSelectTool, trackcancel);

                tableSelectTool.out_table = tempTablePath;
                tableSelectTool.where_clause = "FEATTYP = 2101";
                gp.Execute(tableSelectTool, trackcancel);

                Append appendTool = new Append();
                appendTool.inputs = tempTablePath;
                appendTool.target = prohibMPwJnctIDTablePath;
                appendTool.schema_type = "TEST";
                gp.Execute(appendTool, trackcancel);

                deleteTool = new Delete();
                deleteTool.in_data = tempTablePath;
                gp.Execute(deleteTool, trackcancel);

                AddMessage("Creating turn feature class...", messages, trackcancel);

                // Create the turn feature class

                string tempStatsTablePath = outputFileGdbPath + "\\tempStatsTable";

                Statistics statsTool = new Statistics();
                statsTool.in_table = prohibMPwJnctIDTablePath;
                statsTool.out_table = tempStatsTablePath;
                statsTool.statistics_fields = "SEQNR MAX";
                gp.Execute(statsTool, null);

                CreateAndPopulateTurnFeatureClass(outputFileGdbPath, fdsName, "ProhibMPwJnctID", "tempStatsTable",
                                                  messages, trackcancel);

                deleteTool = new Delete();
                deleteTool.in_data = tempStatsTablePath;
                gp.Execute(deleteTool, trackcancel);

                deleteTool.in_data = prohibMPwJnctIDTablePath;
                gp.Execute(deleteTool, trackcancel);

                GC.Collect();

                // Extract the bifurcations (feature type 9401)

                AddMessage("Extracting bifurcations...", messages, trackcancel);

                string bifurcationMPwJnctIDTablePath = outputFileGdbPath + "\\BifurcationMPwJnctID";

                tableSelectTool = new TableSelect();
                tableSelectTool.in_table = mpTablePath;
                tableSelectTool.out_table = bifurcationMPwJnctIDTablePath;
                tableSelectTool.where_clause = "FEATTYP = 9401";
                gp.Execute(tableSelectTool, trackcancel);

                deleteTool.in_data = mpTablePath;
                gp.Execute(deleteTool, trackcancel);

                // Add and calculate the StreetsOID and the from-/to-junction ID fields to the MP table

                AddMessage("Creating and calculating fields for the StreetsOID and the from-/to-junction IDs...", messages, trackcancel);

                addFieldTool = new AddField();
                addFieldTool.in_table = bifurcationMPwJnctIDTablePath;
                addFieldTool.field_type = "LONG";
                addFieldTool.field_name = "StreetsOID";
                gp.Execute(addFieldTool, trackcancel);

                addFieldTool.field_type = "DOUBLE";
                addFieldTool.field_name = "F_JNCTID";
                gp.Execute(addFieldTool, trackcancel);
                addFieldTool.field_name = "T_JNCTID";
                gp.Execute(addFieldTool, trackcancel);

                makeTableViewTool = new MakeTableView();
                makeTableViewTool.in_table = bifurcationMPwJnctIDTablePath;
                makeTableViewTool.out_view = "bifurcationMP_View";
                gp.Execute(makeTableViewTool, trackcancel);

                addJoinTool = new AddJoin();
                addJoinTool.in_layer_or_view = "bifurcationMP_View";
                addJoinTool.in_field = "TRPELID";
                addJoinTool.join_table = streetsFeatureClassPath;
                addJoinTool.join_field = "ID";
                gp.Execute(addJoinTool, trackcancel);

                calcFieldTool = new CalculateField();
                calcFieldTool.in_table = "bifurcationMP_View";
                calcFieldTool.field = "BifurcationMPwJnctID.StreetsOID";
                calcFieldTool.expression = "[Streets.OBJECTID]";
                calcFieldTool.expression_type = "VB";
                gp.Execute(calcFieldTool, trackcancel);

                calcFieldTool.field = "BifurcationMPwJnctID.F_JNCTID";
                calcFieldTool.expression = "[Streets.F_JNCTID]";
                calcFieldTool.expression_type = "VB";
                gp.Execute(calcFieldTool, trackcancel);

                calcFieldTool.field = "BifurcationMPwJnctID.T_JNCTID";
                calcFieldTool.expression = "[Streets.T_JNCTID]";
                calcFieldTool.expression_type = "VB";
                gp.Execute(calcFieldTool, trackcancel);

                removeJoinTool = new RemoveJoin();
                removeJoinTool.in_layer_or_view = "bifurcationMP_View";
                removeJoinTool.join_name = "Streets";
                gp.Execute(removeJoinTool, trackcancel);

                deleteTool = new Delete();
                deleteTool.in_data = "bifurcationMP_View";
                gp.Execute(deleteTool, trackcancel);

                AddMessage("Creating RoadSplits table...", messages, trackcancel);

                // Create the RoadSplits table

                CreateAndPopulateRoadSplitsTable(outputFileGdbPath, "BifurcationMPwJnctID", messages, trackcancel);

                deleteTool.in_data = bifurcationMPwJnctIDTablePath;
                gp.Execute(deleteTool, trackcancel);

                GC.Collect();

                #region Process Restrictions Table

                bool usesRSTable = false;

                if (!(inputRSTableValue.IsEmpty()))
                {
                    usesRSTable = true;

                    // Extract the information from the Restrictions table

                    AddMessage("Extracting information from the Restrictions table...", messages, trackcancel);

                    string rsTablePath = outputFileGdbPath + "\\rs";

                    tableSelectTool = new TableSelect();
                    tableSelectTool.in_table = inputRSTableValue.GetAsText();
                    tableSelectTool.out_table = rsTablePath;
                    tableSelectTool.where_clause = "RESTRTYP = 'DF' OR FEATTYP IN (2101, 2103)";
                    gp.Execute(tableSelectTool, trackcancel);

                    // Create and populate fields for the Streets

                    makeFeatureLayerTool = new MakeFeatureLayer();
                    makeFeatureLayerTool.in_features = streetsFeatureClassPath;
                    makeFeatureLayerTool.out_layer = "Streets_Layer";
                    gp.Execute(makeFeatureLayerTool, trackcancel);

                    CreateAndPopulateRSField(outputFileGdbPath, false, "FT_AllVehicles_Restricted",
                                             "DIR_POS IN (1, 2) AND VT = 0", gp, messages, trackcancel);
                    CreateAndPopulateRSField(outputFileGdbPath, false, "TF_AllVehicles_Restricted",
                                             "DIR_POS IN (1, 3) AND VT = 0", gp, messages, trackcancel);
                    CreateAndPopulateRSField(outputFileGdbPath, false, "FT_PassengerCars_Restricted",
                                             "DIR_POS IN (1, 2) AND VT = 11", gp, messages, trackcancel);
                    CreateAndPopulateRSField(outputFileGdbPath, false, "TF_PassengerCars_Restricted",
                                             "DIR_POS IN (1, 3) AND VT = 11", gp, messages, trackcancel);
                    CreateAndPopulateRSField(outputFileGdbPath, false, "FT_ResidentialVehicles_Restricted",
                                             "DIR_POS IN (1, 2) AND VT = 12", gp, messages, trackcancel);
                    CreateAndPopulateRSField(outputFileGdbPath, false, "TF_ResidentialVehicles_Restricted",
                                             "DIR_POS IN (1, 3) AND VT = 12", gp, messages, trackcancel);
                    CreateAndPopulateRSField(outputFileGdbPath, false, "FT_Taxis_Restricted",
                                             "DIR_POS IN (1, 2) AND VT = 16", gp, messages, trackcancel);
                    CreateAndPopulateRSField(outputFileGdbPath, false, "TF_Taxis_Restricted",
                                             "DIR_POS IN (1, 3) AND VT = 16", gp, messages, trackcancel);
                    CreateAndPopulateRSField(outputFileGdbPath, false, "FT_PublicBuses_Restricted",
                                             "DIR_POS IN (1, 2) AND VT = 17", gp, messages, trackcancel);
                    CreateAndPopulateRSField(outputFileGdbPath, false, "TF_PublicBuses_Restricted",
                                             "DIR_POS IN (1, 3) AND VT = 17", gp, messages, trackcancel);

                    deleteTool = new Delete();
                    deleteTool.in_data = "Streets_Layer";
                    gp.Execute(deleteTool, trackcancel);

                    // Create and populate fields for the RestrictedTurns

                    string pathToTurnFC = pathToFds + "\\" + TurnFCName;

                    makeFeatureLayerTool = new MakeFeatureLayer();
                    makeFeatureLayerTool.in_features = pathToTurnFC;
                    makeFeatureLayerTool.out_layer = "RestrictedTurns_Layer";
                    gp.Execute(makeFeatureLayerTool, trackcancel);

                    CreateAndPopulateRSField(outputFileGdbPath, true, "AllVehicles_Restricted", "VT = 0 OR RESTRTYP = '8I'", gp, messages, trackcancel);
                    CreateAndPopulateRSField(outputFileGdbPath, true, "PassengerCars_Restricted", "VT = 11", gp, messages, trackcancel);
                    CreateAndPopulateRSField(outputFileGdbPath, true, "ResidentialVehicles_Restricted", "VT = 12", gp, messages, trackcancel);
                    CreateAndPopulateRSField(outputFileGdbPath, true, "Taxis_Restricted", "VT = 16", gp, messages, trackcancel);
                    CreateAndPopulateRSField(outputFileGdbPath, true, "PublicBuses_Restricted", "VT = 17", gp, messages, trackcancel);

                    deleteTool = new Delete();
                    deleteTool.in_data = "RestrictedTurns_Layer";
                    gp.Execute(deleteTool, trackcancel);

                    deleteTool = new Delete();
                    deleteTool.in_data = rsTablePath;
                    gp.Execute(deleteTool, trackcancel);
                }
                #endregion

                #region Process Logistics Truck Routes Table

                bool usesLTRTable = false;

                if (!(inputLTRTableValue.IsEmpty()))
                {
                    usesLTRTable = true;

                    makeFeatureLayerTool = new MakeFeatureLayer();
                    makeFeatureLayerTool.in_features = streetsFeatureClassPath;
                    makeFeatureLayerTool.out_layer = "Streets_Layer";
                    gp.Execute(makeFeatureLayerTool, trackcancel);

                    if (fgdbVersion >= 10.1)
                    {
                        CreateAndPopulateLTRField(outputFileGdbPath, inputLTRTableValue.GetAsText(),
                                                  "NationalSTAARoute", "PREFERRED = 1", gp, messages, trackcancel);
                        CreateAndPopulateLTRField(outputFileGdbPath, inputLTRTableValue.GetAsText(),
                                                  "NationalRouteAccess", "PREFERRED = 2", gp, messages, trackcancel);
                        CreateAndPopulateLTRField(outputFileGdbPath, inputLTRTableValue.GetAsText(),
                                                  "DesignatedTruckRoute", "PREFERRED = 3", gp, messages, trackcancel);
                        CreateAndPopulateLTRField(outputFileGdbPath, inputLTRTableValue.GetAsText(),
                                                  "TruckBypassRoad", "PREFERRED = 4", gp, messages, trackcancel);
                    }

                    CreateAndPopulateLTRField(outputFileGdbPath, inputLTRTableValue.GetAsText(),
                                              "NoCommercialVehicles", "RESTRICTED = 1", gp, messages, trackcancel);
                    CreateAndPopulateLTRField(outputFileGdbPath, inputLTRTableValue.GetAsText(),
                                              "ImmediateAccessOnly", "RESTRICTED = 2", gp, messages, trackcancel);
                    CreateAndPopulateLTRField(outputFileGdbPath, inputLTRTableValue.GetAsText(),
                                              "TrucksRestricted", "RESTRICTED = 3", gp, messages, trackcancel);

                    deleteTool = new Delete();
                    deleteTool.in_data = "Streets_Layer";
                    gp.Execute(deleteTool, trackcancel);
                }
                #endregion

                #region Process Logistics Restrictions Table

                bool usesLRSTable = false;
                string lrsStatsTablePath = outputFileGdbPath + "\\lrs_Stats";

                if (!(inputLRSTableValue.IsEmpty()))
                {
                    usesLRSTable = true;

                    // Copy the LRS table to the file geodatabase

                    AddMessage("Copying the Logistics Restrictions (LRS) table...", messages, trackcancel);

                    importTableTool = new TableToTable();
                    importTableTool.in_rows = inputLRSTableValue.GetAsText();
                    importTableTool.out_path = outputFileGdbPath;
                    importTableTool.out_name = "lrs";
                    gp.Execute(importTableTool, trackcancel);

                    string lrsTablePath = outputFileGdbPath + "\\lrs";

                    // Copy the SUBSEQNR 1 rows of the LVC table to the file geodatabase

                    AddMessage("Copying the Logistics Vehicle Characteristics (LVC) table...", messages, trackcancel);

                    string lvcTablePath = outputFileGdbPath + "\\lvc";

                    tableSelectTool = new TableSelect();
                    tableSelectTool.in_table = inputLVCTableValue.GetAsText();
                    tableSelectTool.out_table = lvcTablePath;
                    tableSelectTool.where_clause = "SUBSEQNR = 1";
                    gp.Execute(tableSelectTool, trackcancel);

                    // Add and calculate join fields on the LRS and LVC tables

                    addFieldTool = new AddField();
                    addFieldTool.field_name = "ID_SEQNR";
                    addFieldTool.field_length = 19;
                    addFieldTool.field_type = "TEXT";
                    addFieldTool.in_table = lrsTablePath;
                    gp.Execute(addFieldTool, trackcancel);
                    addFieldTool.in_table = lvcTablePath;
                    gp.Execute(addFieldTool, trackcancel);

                    calcFieldTool = new CalculateField();
                    calcFieldTool.field = "ID_SEQNR";
                    calcFieldTool.expression = "[ID] & \".\" & [SEQNR]";
                    calcFieldTool.expression_type = "VB";

                    AddMessage("Calculating the join field on the LRS table...", messages, trackcancel);

                    calcFieldTool.in_table = lrsTablePath;
                    gp.Execute(calcFieldTool, trackcancel);

                    AddMessage("Calculating the join field on the LVC table...", messages, trackcancel);

                    calcFieldTool.in_table = lvcTablePath;
                    gp.Execute(calcFieldTool, trackcancel);

                    // Index the join field on the LVC table

                    AddMessage("Indexing the join field...", messages, trackcancel);

                    addIndexTool = new AddIndex();
                    addIndexTool.in_table = lvcTablePath;
                    addIndexTool.fields = "ID_SEQNR";
                    addIndexTool.index_name = "ID_SEQNR";
                    gp.Execute(addIndexTool, trackcancel);

                    // Join the LRS and LVC tables together, and only extract those rows from
                    // the LRS table that do not have an accompanying row in the LVC table

                    AddMessage("Simplifying the LRS table...", messages, trackcancel);

                    makeTableViewTool = new MakeTableView();
                    makeTableViewTool.in_table = lrsTablePath;
                    makeTableViewTool.out_view = "lrs_View";
                    gp.Execute(makeTableViewTool, trackcancel);

                    addJoinTool = new AddJoin();
                    addJoinTool.in_layer_or_view = "lrs_View";
                    addJoinTool.in_field = "ID_SEQNR";
                    addJoinTool.join_table = lvcTablePath;
                    addJoinTool.join_field = "ID_SEQNR";
                    gp.Execute(addJoinTool, trackcancel);

                    importTableTool = new TableToTable();
                    importTableTool.in_rows = "lrs_View";
                    importTableTool.out_path = outputFileGdbPath;
                    importTableTool.out_name = "lrs_simplified";
                    importTableTool.where_clause = "lvc.OBJECTID IS NULL";
                    importTableTool.field_mapping = "ID \"ID\" true true false 8 Double 0 0 ,First,#," + lrsTablePath + ",lrs.ID,-1,-1;" +
                                                    "SEQNR \"SEQNR\" true true false 2 Short 0 0 ,First,#," + lrsTablePath + ",lrs.SEQNR,-1,-1;" +
                                                    "FEATTYP \"FEATTYP\" true true false 2 Short 0 0 ,First,#," + lrsTablePath + ",lrs.FEATTYP,-1,-1;" +
                                                    "RESTRTYP \"RESTRTYP\" true true false 2 Text 0 0 ,First,#," + lrsTablePath + ",lrs.RESTRTYP,-1,-1;" +
                                                    "VT \"VT\" true true false 2 Short 0 0 ,First,#," + lrsTablePath + ",lrs.VT,-1,-1;" +
                                                    "RESTRVAL \"RESTRVAL\" true true false 2 Short 0 0 ,First,#," + lrsTablePath + ",lrs.RESTRVAL,-1,-1;" +
                                                    "LIMIT \"LIMIT\" true true false 8 Double 0 0 ,First,#," + lrsTablePath + ",lrs.LIMIT,-1,-1;" +
                                                    "UNIT_MEAS \"UNIT_MEAS\" true true false 2 Short 0 0 ,First,#," + lrsTablePath + ",lrs.UNIT_MEAS,-1,-1;" +
                                                    "LANE_VALID \"LANE_VALID\" true true false 20 Text 0 0 ,First,#," + lrsTablePath + ",lrs.LANE_VALID,-1,-1;" +
                                                    "VALDIRPOS \"VALDIRPOS\" true true false 2 Short 0 0 ,First,#," + lrsTablePath + ",lrs.VALDIRPOS,-1,-1;" +
                                                    "VERIFIED \"VERIFIED\" true true false 2 Short 0 0 ,First,#," + lrsTablePath + ",lrs.VERIFIED,-1,-1";
                    gp.Execute(importTableTool, trackcancel);

                    removeJoinTool = new RemoveJoin();
                    removeJoinTool.in_layer_or_view = "lrs_View";
                    removeJoinTool.join_name = "lvc";
                    gp.Execute(removeJoinTool, trackcancel);

                    string lrsSimplifiedTablePath = outputFileGdbPath + "\\lrs_simplified";

                    deleteTool = new Delete();
                    deleteTool.in_data = "lrs_View";
                    gp.Execute(deleteTool, trackcancel);

                    deleteTool.in_data = lrsTablePath;
                    gp.Execute(deleteTool, trackcancel);
                    deleteTool.in_data = lvcTablePath;
                    gp.Execute(deleteTool, trackcancel);

                    // Get statistics on the simplified LRS table

                    AddMessage("Analyzing the LRS table...", messages, trackcancel);

                    statsTool = new Statistics();
                    statsTool.in_table = lrsSimplifiedTablePath;
                    statsTool.out_table = lrsStatsTablePath;
                    statsTool.statistics_fields = "ID COUNT";
                    statsTool.case_field = "RESTRTYP;VT;RESTRVAL";
                    gp.Execute(statsTool, trackcancel);

                    // Create and populate the logistics restriction fields

                    CreateAndPopulateLogisticsRestrictionFields(outputFileGdbPath, "lrs_Stats", lrsSimplifiedTablePath, gp, messages, trackcancel);

                    deleteTool = new Delete();
                    deleteTool.in_data = lrsSimplifiedTablePath;
                    gp.Execute(deleteTool, trackcancel);
                }
                #endregion

                GC.Collect();

                // Create Signpost feature class and table

                AddMessage("Creating signpost feature class and table...", messages, trackcancel);

                CreateSignposts(inputSITableValue.GetAsText(), inputSPTableValue.GetAsText(), outputFileGdbPath,
                                messages, trackcancel);

                AddSpatialIndex addSpatialIndexTool = new AddSpatialIndex();
                addSpatialIndexTool.in_features = pathToFds + "\\" + SignpostFCName;
                gp.Execute(addSpatialIndexTool, trackcancel);

                addIndexTool = new AddIndex();
                addIndexTool.in_table = outputFileGdbPath + "\\" + SignpostJoinTableName;
                addIndexTool.fields = "SignpostID";
                addIndexTool.index_name = "SignpostID";
                gp.Execute(addIndexTool, trackcancel);

                addIndexTool.fields = "Sequence";
                addIndexTool.index_name = "Sequence";
                gp.Execute(addIndexTool, trackcancel);

                addIndexTool.fields = "EdgeFCID";
                addIndexTool.index_name = "EdgeFCID";
                gp.Execute(addIndexTool, trackcancel);

                addIndexTool.fields = "EdgeFID";
                addIndexTool.index_name = "EdgeFID";
                gp.Execute(addIndexTool, trackcancel);

                GC.Collect();

                // Upgrade the geodatabase (if not 9.3)

                if (fgdbVersion > 9.3)
                {
                    UpgradeGDB upgradeGdbTool = new UpgradeGDB();
                    upgradeGdbTool.input_workspace = outputFileGdbPath;
                    gp.Execute(upgradeGdbTool, trackcancel);
                }

                // Create and build the network dataset, then pack it in a GPValue

                AddMessage("Creating and building the network dataset...", messages, trackcancel);

                CreateAndBuildNetworkDataset(outputFileGdbPath, fgdbVersion, fdsName, ndsName, createNetworkAttributesInMetric,
                                             createTwoDistanceAttributes, timeZoneIDBaseFieldName, directedTimeZoneIDFields, commonTimeZone,
                                             usesHistoricalTraffic, trafficFeedLocation, usesRSTable, usesLTRTable, usesLRSTable, "lrs_Stats");

                // Once the network dataset is built, we can delete the stats table

                if (usesLRSTable)
                {
                    deleteTool = new Delete();
                    deleteTool.in_data = lrsStatsTablePath;
                    gp.Execute(deleteTool, trackcancel);
                }

                // Write the build errors to the turn feature class

                TurnGeometryUtilities.WriteBuildErrorsToTurnFC(outputFileGdbPath, fdsName, TurnFCName, messages, trackcancel);

                // Compact the output file geodatabase

                AddMessage("Compacting the output file geodatabase...", messages, trackcancel);

                Compact compactTool = new Compact();
                compactTool.in_workspace = outputFileGdbPath;
                gp.Execute(compactTool, trackcancel);
            }
            catch (Exception e)
            {
                if (gp.MaxSeverity == 2)
                {
                    object missing = System.Type.Missing;
                    messages.AddError(1, gp.GetMessages(ref missing));
                }
                messages.AddError(1, e.Message);
                messages.AddError(1, e.StackTrace);
            }
            finally
            {
                // Restore the original GP environment settings

                gpSettings.AddOutputsToMap = origAddOutputsToMapSetting;
                gpSettings.LogHistory = origLogHistorySetting;
            }
            GC.Collect();
            return;
        }

        public string DisplayName
        {
            get
            {
                return "Process MultiNet® Street Data";
            }
        }

        public string MetadataFile
        {
            get
            {
                string filePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                return System.IO.Path.Combine(filePath, "ProcessMultiNetData_nasample.xml");
            }
        }

        public IName FullName
        {
            get
            {
                IGPFunctionFactory functionFactory = new ProcessVendorDataGPFunctionFactory();
                return functionFactory.GetFunctionName(this.Name) as IName;
            }
        }

        public bool IsLicensed()
        {
            // Only allow this tool to run if Network extension is checked out

            IAoInitialize aoi = new AoInitializeClass();
            return aoi.IsExtensionCheckedOut(esriLicenseExtensionCode.esriLicenseExtensionCodeNetwork);
        }

        public UID DialogCLSID
        {
            get
            {
                return null;
            }
        }

        public string Name
        {
            get
            {
                return "ProcessMultiNetData";
            }
        }

        public int HelpContext
        {
            get
            {
                return 0;
            }
        }

        public string HelpFile
        {
            get
            {
                return null;
            }
        }

        public object GetRenderer(IGPParameter gpParam)
        {
            return null;
        }
        #endregion

        private void DisableParameter(IGPParameterEdit gpParamEdit, IGPValue emptyValue)
        {
            gpParamEdit.Value = emptyValue;
            gpParamEdit.Enabled = false;
            return;
        }

        private void EnableParameter(IGPParameterEdit gpParamEdit)
        {
            gpParamEdit.Enabled = true;
            return;
        }

        private bool CheckForTableFields(IDETable inputTable, string[] fieldNames, esriFieldType[] fieldTypes,
                                         IGPMessage gpMessage)
        {
            IFields fields = inputTable.Fields;
            int fieldIndex;

            for (int i = 0; i < fieldNames.Length; i++)
            {
                fieldIndex = fields.FindField(fieldNames[i]);
                if (fieldIndex == -1)
                {
                    gpMessage.Type = esriGPMessageType.esriGPMessageTypeError;
                    gpMessage.Description = "Field named " + fieldNames[i] + " not found.";
                    return false;
                }

                if (fields.get_Field(fieldIndex).Type != fieldTypes[i])
                {
                    gpMessage.Type = esriGPMessageType.esriGPMessageTypeError;
                    gpMessage.Description = "Field named " + fieldNames[i] + " is not the expected type.";
                    return false;
                }
            }
            return true;
        }

        private void AddMessage(string messageString, IGPMessages messages, ITrackCancel trackcancel)
        {
            messages.AddMessage(messageString);
            IStepProgressor sp = trackcancel as IStepProgressor;
            if (sp != null)
                sp.Message = messageString;
        }

        private void CreateDailyProfilesTable(ITable inputHSPRTable, string outputFileGdbPath, double fgdbVersion)
        {
            // Find the needed fields on the input HSPR table
            
            int profileIDField = inputHSPRTable.FindField("PROFILE_ID");
            int relSpField = inputHSPRTable.FindField("REL_SP");

            // Create the Profiles table in the output file geodatabase and open an InsertCursor on it

            Type factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.FileGDBWorkspaceFactory");
            var gdbWSF = Activator.CreateInstance(factoryType) as IWorkspaceFactory;
            var gdbFWS = gdbWSF.OpenFromFile(outputFileGdbPath, 0) as IFeatureWorkspace;
            var ocd = new ObjectClassDescriptionClass() as IObjectClassDescription;
            var tableFields = ocd.RequiredFields as IFieldsEdit;
            var newField = new FieldClass() as IFieldEdit;
            newField.Name_2 = "ProfileID";
            newField.Type_2 = esriFieldType.esriFieldTypeSmallInteger;
            tableFields.AddField(newField as IField);
            for (int i = 0; i < 288; i++)
            {
                newField = new FieldClass();
                newField.Type_2 = esriFieldType.esriFieldTypeSingle;
                newField.Name_2 = ((fgdbVersion == 10.0) ? "TimeFactor_" : "SpeedFactor_") + String.Format("{0:00}", i / 12) + String.Format("{0:00}", (i % 12) * 5);
                tableFields.AddField(newField as IField);
            }
            ITable newTable = gdbFWS.CreateTable(ProfilesTableName, tableFields as IFields, ocd.InstanceCLSID, ocd.ClassExtensionCLSID, "");
            int newTableProfileIDField = newTable.FindField("ProfileID");
            IRowBuffer buff = newTable.CreateRowBuffer();
            ICursor insertCursor = newTable.Insert(true);
            
            // Loop through the HSPR table and populate the newly-created Profiles table

            ITableSort ts = new TableSortClass();
            ts.Table = inputHSPRTable;
            ts.Fields = "PROFILE_ID, TIME_SLOT";
            ts.set_Ascending("PROFILE_ID", true);
            ts.set_Ascending("TIME_SLOT", true);
            ts.Sort(null);
            ICursor cur = ts.Rows;
            IRow r = cur.NextRow();
            while (r != null)
            {
                buff.set_Value(newTableProfileIDField, r.get_Value(profileIDField));
                for (int i = 1; i <= 288; i++)
                {
                    buff.set_Value(newTableProfileIDField + i,
                                   (fgdbVersion == 10.0) ? (100 / (float)(r.get_Value(relSpField)))
                                                         : ((float)(r.get_Value(relSpField)) / 100));
                    r = cur.NextRow();
                }
                insertCursor.InsertRow(buff);
            }

            // Flush any outstanding writes to the table
            insertCursor.Flush();
        }

        private void CreateNonHistoricalDailyProfilesTable(string outputFileGdbPath)
        {
            // Create the Profiles table in the output file geodatabase and open an InsertCursor on it

            Type factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.FileGDBWorkspaceFactory");
            var gdbWSF = Activator.CreateInstance(factoryType) as IWorkspaceFactory;
            var gdbFWS = gdbWSF.OpenFromFile(outputFileGdbPath, 0) as IFeatureWorkspace;
            var ocd = new ObjectClassDescriptionClass() as IObjectClassDescription;
            var tableFields = ocd.RequiredFields as IFieldsEdit;
            var newField = new FieldClass() as IFieldEdit;
            newField = new FieldClass();
            newField.Type_2 = esriFieldType.esriFieldTypeSingle;
            newField.Name_2 = "SpeedFactor_AM";
            tableFields.AddField(newField as IField);
            newField = new FieldClass();
            newField.Type_2 = esriFieldType.esriFieldTypeSingle;
            newField.Name_2 = "SpeedFactor_PM";
            tableFields.AddField(newField as IField);
            ITable newTable = gdbFWS.CreateTable(ProfilesTableName, tableFields as IFields, ocd.InstanceCLSID, ocd.ClassExtensionCLSID, "");
            IRowBuffer buff = newTable.CreateRowBuffer();
            ICursor insertCursor = newTable.Insert(true);

            int speedFactorAMField = newTable.FindField("SpeedFactor_AM");
            int speedFactorPMField = newTable.FindField("SpeedFactor_PM");
            buff.set_Value(speedFactorAMField, (float)1.0);
            buff.set_Value(speedFactorPMField, (float)1.0);
            insertCursor.InsertRow(buff);
            insertCursor.Flush();
        }

        private void CreateAndPopulateTurnFeatureClass(string outputFileGdbPath, string fdsName,
                                                       string ProhibMPTableName, string tempStatsTableName,
                                                       IGPMessages messages, ITrackCancel trackcancel)
        {
            // Determine the number of AltID fields we need (the same as the MAX_SEQNR value).

            Type factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.FileGDBWorkspaceFactory");
            var wsf = Activator.CreateInstance(factoryType) as IWorkspaceFactory;
            var fws = wsf.OpenFromFile(outputFileGdbPath, 0) as IFeatureWorkspace;
            ITable tempStatsTable = fws.OpenTable(tempStatsTableName);
            short numAltIDFields = 2;
            if (tempStatsTable.RowCount(null) == 1)
                numAltIDFields = (short)((double)(tempStatsTable.GetRow(1).get_Value(tempStatsTable.FindField("MAX_SEQNR"))));

            // Open the MP table and find the fields we need

            ITable mpTable = fws.OpenTable(ProhibMPTableName);
            int seqNrField = mpTable.FindField("SEQNR");
            int trpElIDField = mpTable.FindField("TRPELID");
            int idFieldOnMP = mpTable.FindField("ID");
            int mpJnctIDField = mpTable.FindField("JNCTID");

            // Create a temporary template feature class

            var fcd = new FeatureClassDescriptionClass() as IFeatureClassDescription;
            var ocd = fcd as IObjectClassDescription;
            var fieldsEdit = ocd.RequiredFields as IFieldsEdit;
            IField fieldOnMPTable = mpTable.Fields.get_Field(idFieldOnMP);  // use the ID field as a template for the AltID fields
            for (short i = 1; i <= numAltIDFields; i++)
            {
                IFieldEdit newField = new FieldClass();
                newField.Name_2 = "AltID" + i;
                newField.Precision_2 = fieldOnMPTable.Precision;
                newField.Scale_2 = fieldOnMPTable.Scale;
                newField.Type_2 = fieldOnMPTable.Type;
                fieldsEdit.AddField(newField as IField);
            }
            fieldsEdit.AddField(fieldOnMPTable);
            fieldOnMPTable = mpTable.Fields.get_Field(mpJnctIDField);
            fieldsEdit.AddField(fieldOnMPTable);
            var fieldChk = new FieldCheckerClass() as IFieldChecker;
            IEnumFieldError enumFieldErr = null;
            IFields validatedFields = null;
            fieldChk.ValidateWorkspace = fws as IWorkspace;
            fieldChk.Validate(fieldsEdit as IFields, out enumFieldErr, out validatedFields);
            var tempFC = fws.CreateFeatureClass("TheTemplate", validatedFields, ocd.InstanceCLSID, ocd.ClassExtensionCLSID,
                                                esriFeatureType.esriFTSimple, fcd.ShapeFieldName, "") as IDataset;

            // Create the turn feature class from the template, then delete the template

            Geoprocessor gp = new Geoprocessor();
            CreateTurnFeatureClass createTurnFCTool = new CreateTurnFeatureClass();
            string pathToFds = outputFileGdbPath + "\\" + fdsName;
            createTurnFCTool.out_location = pathToFds;
            createTurnFCTool.out_feature_class_name = TurnFCName;
            createTurnFCTool.maximum_edges = numAltIDFields;
            createTurnFCTool.in_template_feature_class = outputFileGdbPath + "\\TheTemplate";
            gp.Execute(createTurnFCTool, trackcancel);
            tempFC.Delete();

            // Open the new turn feature class and find all the fields on it

            IFeatureClass turnFC = fws.OpenFeatureClass(TurnFCName);
            int[] altIDFields = new int[numAltIDFields];
            int[] edgeFCIDFields = new int[numAltIDFields];
            int[] edgeFIDFields = new int[numAltIDFields];
            int[] edgePosFields = new int[numAltIDFields];
            for (short i = 0; i < numAltIDFields; i++)
            {
                altIDFields[i] = turnFC.FindField("AltID" + (i + 1));
                edgeFCIDFields[i] = turnFC.FindField("Edge" + (i + 1) + "FCID");
                edgeFIDFields[i] = turnFC.FindField("Edge" + (i + 1) + "FID");
                edgePosFields[i] = turnFC.FindField("Edge" + (i + 1) + "Pos");
            }
            int edge1endField = turnFC.FindField("Edge1End");
            int idFieldOnTurnFC = turnFC.FindField("ID");
            int turnFCJnctIDField = turnFC.FindField("JNCTID");

            // Look up the FCID of the Streets feature class

            IFeatureClass streetsFC = fws.OpenFeatureClass(StreetsFCName);
            int streetsFCID = streetsFC.FeatureClassID;

            // Set up queries

            var ts = new TableSortClass() as ITableSort;
            ts.Fields = "ID, SEQNR";
            ts.set_Ascending("ID", true);
            ts.set_Ascending("SEQNR", true);
            ts.QueryFilter = new QueryFilterClass();
            ts.Table = mpTable;
            ts.Sort(null);
            ICursor mpCursor = ts.Rows;
            IFeatureCursor turnFCCursor = turnFC.Insert(true);
            IFeatureBuffer turnBuffer = turnFC.CreateFeatureBuffer();

            // Write the field values to the turn feature class accordingly

            turnBuffer.set_Value(edge1endField, "?");  // dummy value; will be updated in a later calculation
            int numFeatures = 0;
            IRow mpRow = mpCursor.NextRow();
            while (mpRow != null)
            {
                // Transfer the non-edge identifying field values to the buffer
                turnBuffer.set_Value(idFieldOnTurnFC, mpRow.get_Value(idFieldOnMP));
                turnBuffer.set_Value(turnFCJnctIDField, mpRow.get_Value(mpJnctIDField));

                // Write the AltID values to the buffer
                int seq = (int)(mpRow.get_Value(seqNrField));
                int lastEntry;
                do
                {
                    lastEntry = seq;
                    turnBuffer.set_Value(altIDFields[lastEntry - 1], mpRow.get_Value(trpElIDField));
                    mpRow = mpCursor.NextRow();
                    if (mpRow == null) break;
                    seq = (int)(mpRow.get_Value(seqNrField));
                } while (seq != 1);

                // Zero-out the unused fields
                for (int i = lastEntry; i < numAltIDFields; i++)
                    turnBuffer.set_Value(altIDFields[i], 0);

                // Write the FCID and Pos field values to the buffer
                for (short i = 0; i < numAltIDFields; i++)
                {
                    double altID = (double)(turnBuffer.get_Value(altIDFields[i]));
                    if (altID != 0)
                    {
                        turnBuffer.set_Value(edgeFCIDFields[i], streetsFCID);
                        turnBuffer.set_Value(edgeFIDFields[i], 1);
                        turnBuffer.set_Value(edgePosFields[i], 0.5);
                    }
                    else
                    {
                        turnBuffer.set_Value(edgeFCIDFields[i], 0);
                        turnBuffer.set_Value(edgeFIDFields[i], 0);
                        turnBuffer.set_Value(edgePosFields[i], 0);
                    }
                }

                // Create the turn feature
                turnFCCursor.InsertFeature(turnBuffer);
                numFeatures++;

                if ((numFeatures % 100) == 0)
                {
                    // check for user cancel

                    if (trackcancel != null && !trackcancel.Continue())
                        throw (new COMException("Function cancelled."));
                }
            }

            // Flush any outstanding writes to the turn feature class
            turnFCCursor.Flush();

            // Update the Edge1End values

            AddMessage("Updating the Edge1End values...", messages, trackcancel);

            MakeFeatureLayer makeFeatureLayerTool = new MakeFeatureLayer();
            makeFeatureLayerTool.in_features = pathToFds + "\\" + TurnFCName;
            makeFeatureLayerTool.out_layer = "Turn_Layer";
            gp.Execute(makeFeatureLayerTool, trackcancel);

            AddJoin addJoinTool = new AddJoin();
            addJoinTool.in_layer_or_view = "Turn_Layer";
            addJoinTool.in_field = "AltID1";
            addJoinTool.join_table = pathToFds + "\\" + StreetsFCName;
            addJoinTool.join_field = "ID";
            gp.Execute(addJoinTool, trackcancel);

            CalculateField calcFieldTool = new CalculateField();
            calcFieldTool.in_table = "Turn_Layer";
            calcFieldTool.field = TurnFCName + ".Edge1End";
            calcFieldTool.expression = "x";
            calcFieldTool.code_block = "Select Case [" + TurnFCName + ".JNCTID]\n  Case [" + StreetsFCName + ".F_JNCTID]: x = \"N\"\n  Case [" + StreetsFCName + ".T_JNCTID]: x = \"Y\"\n  Case Else: x = \"?\"\nEnd Select";
            calcFieldTool.expression_type = "VB";
            gp.Execute(calcFieldTool, trackcancel);

            RemoveJoin removeJoinTool = new RemoveJoin();
            removeJoinTool.in_layer_or_view = "Turn_Layer";
            removeJoinTool.join_name = StreetsFCName;
            gp.Execute(removeJoinTool, trackcancel);

            Delete deleteTool = new Delete();
            deleteTool.in_data = "Turn_Layer";
            gp.Execute(deleteTool, trackcancel);

            AddMessage("Updating the EdgeFID values...", messages, trackcancel);

            // Create a temporary network dataset for updating the EdgeFID values

            IDENetworkDataset dends = new DENetworkDatasetClass();
            dends.SupportsTurns = true;
            dends.Buildable = true;
            (dends as IDataElement).Name = StreetsFCName;
            (dends as IDEGeoDataset).SpatialReference = (streetsFC as IGeoDataset).SpatialReference;
            IArray sourceArray = new ArrayClass();
            var efs = new EdgeFeatureSourceClass() as IEdgeFeatureSource;
            (efs as INetworkSource).Name = StreetsFCName;
            efs.UsesSubtypes = false;
            efs.ClassConnectivityGroup = 1;
            efs.ClassConnectivityPolicy = esriNetworkEdgeConnectivityPolicy.esriNECPEndVertex;
            sourceArray.Add(efs);
            var tfs = new TurnFeatureSourceClass() as INetworkSource;
            tfs.Name = TurnFCName;
            sourceArray.Add(tfs);
            dends.Sources = sourceArray;
            var fdxc = fws.OpenFeatureDataset(fdsName) as IFeatureDatasetExtensionContainer;
            var dsCont = fdxc.FindExtension(esriDatasetType.esriDTNetworkDataset) as IDatasetContainer2;
            var tempNDS = dsCont.CreateDataset(dends as IDEDataset) as IDataset;

            // Set the EdgeFID field values by running UpdateByAlternateIDFields

            UpdateByAlternateIDFields updateByAltIDTool = new UpdateByAlternateIDFields();
            updateByAltIDTool.in_network_dataset = pathToFds + "\\" + StreetsFCName;
            updateByAltIDTool.alternate_ID_field_name = "ID";
            gp.Execute(updateByAltIDTool, trackcancel);

            // Delete the temporary network dataset

            tempNDS.Delete();

            // Write the turn geometries

            TurnGeometryUtilities.WriteTurnGeometry(outputFileGdbPath, StreetsFCName, TurnFCName,
                                                    numAltIDFields, 0.3, messages, trackcancel);

            // Index the turn geometries

            AddMessage("Creating spatial index on the turn feature class...", messages, trackcancel);

            AddSpatialIndex addSpatialIndexTool = new AddSpatialIndex();
            addSpatialIndexTool.in_features = pathToFds + "\\" + TurnFCName;
            gp.Execute(addSpatialIndexTool, trackcancel);

            return;
        }

        private void CreateAndPopulateRoadSplitsTable(string outputFileGdbPath, string BifurcationMPTableName,
                                                      IGPMessages messages, ITrackCancel trackcancel)
        {
            // Open the Rdms table and find all the fields we need

            Type factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.FileGDBWorkspaceFactory");
            var wsf = Activator.CreateInstance(factoryType) as IWorkspaceFactory;
            var fws = wsf.OpenFromFile(outputFileGdbPath, 0) as IFeatureWorkspace;

            // Open the MP table and find the fields we need

            ITable mpTable = fws.OpenTable(BifurcationMPTableName);
            int seqNrField = mpTable.FindField("SEQNR");
            int trpElIDField = mpTable.FindField("TRPELID");
            int idFieldOnMP = mpTable.FindField("ID");
            int mpJnctIDField = mpTable.FindField("JNCTID");
            int FJnctIDField = mpTable.FindField("F_JNCTID");
            int TJnctIDField = mpTable.FindField("T_JNCTID");
            int StreetsOIDField = mpTable.FindField("StreetsOID");

            // Open the Streets feature class and get its feature class ID

            IFeatureClass inputLineFeatures = fws.OpenFeatureClass(StreetsFCName);
            int streetsFCID = inputLineFeatures.FeatureClassID;

            // Define the fields for the RoadSplits table

            var ocd = new ObjectClassDescriptionClass() as IObjectClassDescription;
            var fieldsEdit = ocd.RequiredFields as IFieldsEdit;
            IFieldEdit field;

            // Add the anchor edge fields to the table

            field = new FieldClass();
            field.Name_2 = "EdgeFCID";
            field.Type_2 = esriFieldType.esriFieldTypeInteger;
            fieldsEdit.AddField(field);

            field = new FieldClass();
            field.Name_2 = "EdgeFID";
            field.Type_2 = esriFieldType.esriFieldTypeInteger;
            fieldsEdit.AddField(field);

            field = new FieldClass();
            field.Name_2 = "EdgeFrmPos";
            field.Type_2 = esriFieldType.esriFieldTypeDouble;
            fieldsEdit.AddField(field);

            field = new FieldClass();
            field.Name_2 = "EdgeToPos";
            field.Type_2 = esriFieldType.esriFieldTypeDouble;
            fieldsEdit.AddField(field);

            // Add the branch edge fields to the table

            for (short i = 0; i < 3; i++)
            {
                field = new FieldClass();
                field.Name_2 = "Branch" + i + "FCID";
                field.Type_2 = esriFieldType.esriFieldTypeInteger;
                fieldsEdit.AddField(field);

                field = new FieldClass();
                field.Name_2 = "Branch" + i + "FID";
                field.Type_2 = esriFieldType.esriFieldTypeInteger;
                fieldsEdit.AddField(field);

                field = new FieldClass();
                field.Name_2 = "Branch" + i + "FrmPos";
                field.Type_2 = esriFieldType.esriFieldTypeDouble;
                fieldsEdit.AddField(field);

                field = new FieldClass();
                field.Name_2 = "Branch" + i + "ToPos";
                field.Type_2 = esriFieldType.esriFieldTypeDouble;
                fieldsEdit.AddField(field);
            }

            // Check the fields and create the RoadSplits table

            var fieldChk = new FieldCheckerClass() as IFieldChecker;
            IEnumFieldError enumFieldErr = null;
            IFields validatedFields = null;
            fieldChk.ValidateWorkspace = fws as IWorkspace;
            fieldChk.Validate(fieldsEdit as IFields, out enumFieldErr, out validatedFields);
            var roadSplitsTable = fws.CreateTable(RoadSplitsTableName, validatedFields, ocd.InstanceCLSID, ocd.ClassExtensionCLSID, "") as ITable;

            // Find all the fields
            int EdgeFCIDFI = roadSplitsTable.FindField("EdgeFCID");
            int EdgeFIDFI = roadSplitsTable.FindField("EdgeFID");
            int EdgeFrmPosFI = roadSplitsTable.FindField("EdgeFrmPos");
            int EdgeToPosFI = roadSplitsTable.FindField("EdgeToPos");
            int Branch0FCIDFI = roadSplitsTable.FindField("Branch0FCID");
            int Branch0FIDFI = roadSplitsTable.FindField("Branch0FID");
            int Branch0FrmPosFI = roadSplitsTable.FindField("Branch0FrmPos");
            int Branch0ToPosFI = roadSplitsTable.FindField("Branch0ToPos");
            int Branch1FCIDFI = roadSplitsTable.FindField("Branch1FCID");
            int Branch1FIDFI = roadSplitsTable.FindField("Branch1FID");
            int Branch1FrmPosFI = roadSplitsTable.FindField("Branch1FrmPos");
            int Branch1ToPosFI = roadSplitsTable.FindField("Branch1ToPos");
            int Branch2FCIDFI = roadSplitsTable.FindField("Branch2FCID");
            int Branch2FIDFI = roadSplitsTable.FindField("Branch2FID");
            int Branch2FrmPosFI = roadSplitsTable.FindField("Branch2FrmPos");
            int Branch2ToPosFI = roadSplitsTable.FindField("Branch2ToPos");

            // Create insert cursor and row buffer for output

            ICursor tableInsertCursor = roadSplitsTable.Insert(true);
            IRowBuffer tableBuffer = roadSplitsTable.CreateRowBuffer();
            IRow row = tableBuffer as IRow;
            tableBuffer.set_Value(EdgeFCIDFI, null);
            tableBuffer.set_Value(EdgeFIDFI, null);
            tableBuffer.set_Value(EdgeFrmPosFI, null);
            tableBuffer.set_Value(EdgeToPosFI, null);
            tableBuffer.set_Value(Branch0FCIDFI, null);
            tableBuffer.set_Value(Branch0FIDFI, null);
            tableBuffer.set_Value(Branch0FrmPosFI, null);
            tableBuffer.set_Value(Branch0ToPosFI, null);
            tableBuffer.set_Value(Branch1FCIDFI, null);
            tableBuffer.set_Value(Branch1FIDFI, null);
            tableBuffer.set_Value(Branch1FrmPosFI, null);
            tableBuffer.set_Value(Branch1ToPosFI, null);
            tableBuffer.set_Value(Branch2FCIDFI, null);
            tableBuffer.set_Value(Branch2FIDFI, null);
            tableBuffer.set_Value(Branch2FrmPosFI, null);
            tableBuffer.set_Value(Branch2ToPosFI, null);

            // Create input cursor for the MP table we are importing

            ITableSort tableSort = new TableSortClass();
            tableSort.Fields = "ID, SEQNR";
            tableSort.set_Ascending("ID", true);
            tableSort.set_Ascending("SEQNR", true);
            tableSort.QueryFilter = null;
            tableSort.Table = mpTable;
            tableSort.Sort(null);
            ICursor inputCursor = tableSort.Rows;

            IRow inputTableRow = inputCursor.NextRow();
            if (inputTableRow == null)
                return;     // if MP table is empty, there's nothing to do

            long currentID = Convert.ToInt64(inputTableRow.get_Value(idFieldOnMP));

            int streetsOID = Convert.ToInt32(inputTableRow.get_Value(StreetsOIDField));
            int seqNr = Convert.ToInt32(inputTableRow.get_Value(seqNrField));
            double frmPos = 0.0;
            double toPos = 1.0;
            if (seqNr == 1)
            {
                if (Convert.ToInt64(inputTableRow.get_Value(mpJnctIDField)) == Convert.ToInt64(inputTableRow.get_Value(FJnctIDField)))
                {
                    frmPos = 1.0;
                    toPos = 0.0;
                }
            }
            else
            {
                if (Convert.ToInt64(inputTableRow.get_Value(mpJnctIDField)) == Convert.ToInt64(inputTableRow.get_Value(TJnctIDField)))
                {
                    frmPos = 1.0;
                    toPos = 0.0;
                }
            }

            switch (seqNr)
            {
                case 1:
                    tableBuffer.set_Value(EdgeFCIDFI, streetsFCID);
                    tableBuffer.set_Value(EdgeFIDFI, streetsOID);
                    tableBuffer.set_Value(EdgeFrmPosFI, frmPos);
                    tableBuffer.set_Value(EdgeToPosFI, toPos);
                    break;
                case 2:
                    tableBuffer.set_Value(Branch0FCIDFI, streetsFCID);
                    tableBuffer.set_Value(Branch0FIDFI, streetsOID);
                    tableBuffer.set_Value(Branch0FrmPosFI, frmPos);
                    tableBuffer.set_Value(Branch0ToPosFI, toPos);
                    break;
                case 3:
                    tableBuffer.set_Value(Branch1FCIDFI, streetsFCID);
                    tableBuffer.set_Value(Branch1FIDFI, streetsOID);
                    tableBuffer.set_Value(Branch1FrmPosFI, frmPos);
                    tableBuffer.set_Value(Branch1ToPosFI, toPos);
                    break;
                case 4:
                    tableBuffer.set_Value(Branch2FCIDFI, streetsFCID);
                    tableBuffer.set_Value(Branch2FIDFI, streetsOID);
                    tableBuffer.set_Value(Branch2FrmPosFI, frmPos);
                    tableBuffer.set_Value(Branch2ToPosFI, toPos);
                    break;
            }

            double previousID = currentID;

            while ((inputTableRow = inputCursor.NextRow()) != null)
            {
                currentID = Convert.ToInt64(inputTableRow.get_Value(idFieldOnMP));

                if (currentID != previousID)
                {
                    // write out the previous buffered row and reinitialize the buffer
                    tableInsertCursor.InsertRow(tableBuffer);

                    tableBuffer.set_Value(EdgeFCIDFI, null);
                    tableBuffer.set_Value(EdgeFIDFI, null);
                    tableBuffer.set_Value(EdgeFrmPosFI, null);
                    tableBuffer.set_Value(EdgeToPosFI, null);
                    tableBuffer.set_Value(Branch0FCIDFI, null);
                    tableBuffer.set_Value(Branch0FIDFI, null);
                    tableBuffer.set_Value(Branch0FrmPosFI, null);
                    tableBuffer.set_Value(Branch0ToPosFI, null);
                    tableBuffer.set_Value(Branch1FCIDFI, null);
                    tableBuffer.set_Value(Branch1FIDFI, null);
                    tableBuffer.set_Value(Branch1FrmPosFI, null);
                    tableBuffer.set_Value(Branch1ToPosFI, null);
                    tableBuffer.set_Value(Branch2FCIDFI, null);
                    tableBuffer.set_Value(Branch2FIDFI, null);
                    tableBuffer.set_Value(Branch2FrmPosFI, null);
                    tableBuffer.set_Value(Branch2ToPosFI, null);
                }

                streetsOID = Convert.ToInt32(inputTableRow.get_Value(StreetsOIDField));
                seqNr = Convert.ToInt32(inputTableRow.get_Value(seqNrField));
                frmPos = 0.0;
                toPos = 1.0;
                if (seqNr == 1)
                {
                    if (Convert.ToInt64(inputTableRow.get_Value(mpJnctIDField)) == Convert.ToInt64(inputTableRow.get_Value(FJnctIDField)))
                    {
                        frmPos = 1.0;
                        toPos = 0.0;
                    }
                }
                else
                {
                    if (Convert.ToInt64(inputTableRow.get_Value(mpJnctIDField)) == Convert.ToInt64(inputTableRow.get_Value(TJnctIDField)))
                    {
                        frmPos = 1.0;
                        toPos = 0.0;
                    }
                }

                switch (seqNr)
                {
                    case 1:
                        tableBuffer.set_Value(EdgeFCIDFI, streetsFCID);
                        tableBuffer.set_Value(EdgeFIDFI, streetsOID);
                        tableBuffer.set_Value(EdgeFrmPosFI, frmPos);
                        tableBuffer.set_Value(EdgeToPosFI, toPos);
                        break;
                    case 2:
                        tableBuffer.set_Value(Branch0FCIDFI, streetsFCID);
                        tableBuffer.set_Value(Branch0FIDFI, streetsOID);
                        tableBuffer.set_Value(Branch0FrmPosFI, frmPos);
                        tableBuffer.set_Value(Branch0ToPosFI, toPos);
                        break;
                    case 3:
                        tableBuffer.set_Value(Branch1FCIDFI, streetsFCID);
                        tableBuffer.set_Value(Branch1FIDFI, streetsOID);
                        tableBuffer.set_Value(Branch1FrmPosFI, frmPos);
                        tableBuffer.set_Value(Branch1ToPosFI, toPos);
                        break;
                    case 4:
                        tableBuffer.set_Value(Branch2FCIDFI, streetsFCID);
                        tableBuffer.set_Value(Branch2FIDFI, streetsOID);
                        tableBuffer.set_Value(Branch2FrmPosFI, frmPos);
                        tableBuffer.set_Value(Branch2ToPosFI, toPos);
                        break;
                }

                previousID = currentID;
            }

            // Write out the final row and flush
            tableInsertCursor.InsertRow(tableBuffer);
            tableInsertCursor.Flush();
        }

        private void CreateAndPopulateRSField(string outputFileGdbPath, bool onTurnFC, string newFieldName, string queryExpression,
                                              Geoprocessor gp, IGPMessages messages, ITrackCancel trackcancel)
        {
            string fcName = StreetsFCName;
            string layerName = "Streets_Layer";
            if (onTurnFC)
            {
                fcName = TurnFCName;
                layerName = "RestrictedTurns_Layer";
            }

            // Add a new field to the feature class

            AddField addFieldTool = new AddField();
            addFieldTool.in_table = outputFileGdbPath + "\\" + fcName;
            addFieldTool.field_type = "TEXT";
            addFieldTool.field_length = 1;
            addFieldTool.field_name = newFieldName;
            gp.Execute(addFieldTool, trackcancel);

            // Extract the information needed for this field from the RS table

            string extractTablePath = outputFileGdbPath + "\\rsExtract";

            AddMessage("Extracting information for the " + newFieldName + " field...", messages, trackcancel);

            string rsTablePath = outputFileGdbPath + "\\rs";

            TableSelect tableSelectTool = new TableSelect();
            tableSelectTool.in_table = rsTablePath;
            tableSelectTool.out_table = extractTablePath;
            tableSelectTool.where_clause = queryExpression;
            gp.Execute(tableSelectTool, trackcancel);

            AddMessage("Indexing the ID field...", messages, trackcancel);

            AddIndex addIndexTool = new AddIndex();
            addIndexTool.fields = "ID";
            addIndexTool.index_name = "ID";
            addIndexTool.in_table = extractTablePath;
            gp.Execute(addIndexTool, trackcancel);

            // Calculate the turn restriction field

            AddJoin addJoinTool = new AddJoin();
            addJoinTool.in_layer_or_view = layerName;
            addJoinTool.in_field = "ID";
            addJoinTool.join_table = extractTablePath;
            addJoinTool.join_field = "ID";
            addJoinTool.join_type = "KEEP_COMMON";
            gp.Execute(addJoinTool, trackcancel);

            AddMessage("Calculating the " + newFieldName + " field...", messages, trackcancel);

            CalculateField calcFieldTool = new CalculateField();
            calcFieldTool.in_table = layerName;
            calcFieldTool.field = fcName + "." + newFieldName;
            calcFieldTool.expression = "\"Y\"";
            calcFieldTool.expression_type = "VB";
            gp.Execute(calcFieldTool, trackcancel);

            RemoveJoin removeJoinTool = new RemoveJoin();
            removeJoinTool.in_layer_or_view = layerName;
            removeJoinTool.join_name = "rsExtract";
            gp.Execute(removeJoinTool, trackcancel);

            Delete deleteTool = new Delete();
            deleteTool.in_data = extractTablePath;
            gp.Execute(deleteTool, trackcancel);
        }

        private void CreateAndPopulateLTRField(string outputFileGdbPath, string ltrTablePath,
                                               string newFieldName, string queryExpression,
                                               Geoprocessor gp, IGPMessages messages, ITrackCancel trackcancel)
        {
            // Add a new field to the Streets feature class

            AddField addFieldTool = new AddField();
            addFieldTool.in_table = outputFileGdbPath + "\\" + StreetsFCName;
            addFieldTool.field_name = newFieldName;
            addFieldTool.field_type = "TEXT";
            addFieldTool.field_length = 1;
            gp.Execute(addFieldTool, trackcancel);

            // Extract the information needed for this field from the LTR table

            string extractTablePath = outputFileGdbPath + "\\ltrExtract";

            AddMessage("Extracting information for the " + newFieldName + " field...", messages, trackcancel);

            TableSelect tableSelectTool = new TableSelect();
            tableSelectTool.in_table = ltrTablePath;
            tableSelectTool.out_table = extractTablePath;
            tableSelectTool.where_clause = queryExpression;
            gp.Execute(tableSelectTool, trackcancel);

            AddIndex addIndexTool = new AddIndex();
            addIndexTool.fields = "ID";
            addIndexTool.index_name = "ID";
            addIndexTool.in_table = extractTablePath;
            gp.Execute(addIndexTool, trackcancel);

            // Calculate the truck route/restriction field

            AddJoin addJoinTool = new AddJoin();
            addJoinTool.in_layer_or_view = "Streets_Layer";
            addJoinTool.in_field = "ID";
            addJoinTool.join_table = extractTablePath;
            addJoinTool.join_field = "ID";
            addJoinTool.join_type = "KEEP_COMMON";
            gp.Execute(addJoinTool, trackcancel);

            AddMessage("Writing the information to the " + newFieldName + " field...", messages, trackcancel);

            CalculateField calcFieldTool = new CalculateField();
            calcFieldTool.in_table = "Streets_Layer";
            calcFieldTool.field = StreetsFCName + "." + newFieldName;
            calcFieldTool.expression = "\"Y\"";
            calcFieldTool.expression_type = "VB";
            gp.Execute(calcFieldTool, trackcancel);

            RemoveJoin removeJoinTool = new RemoveJoin();
            removeJoinTool.in_layer_or_view = "Streets_Layer";
            removeJoinTool.join_name = "ltrExtract";
            gp.Execute(removeJoinTool, trackcancel);

            Delete deleteTool = new Delete();
            deleteTool.in_data = extractTablePath;
            gp.Execute(deleteTool, trackcancel);
        }
        
        private void CreateAndPopulateLogisticsRestrictionFields(string outputFileGdbPath, string lrsStatsTableName, string lrsTablePath,
                                                                 Geoprocessor gp, IGPMessages messages, ITrackCancel trackcancel)
        {
            // Make a feature layer for the Streets feature class

            MakeFeatureLayer makeFeatureLayerTool = new MakeFeatureLayer();
            makeFeatureLayerTool.in_features = outputFileGdbPath + "\\" + StreetsFCName;
            makeFeatureLayerTool.out_layer = "Streets_Layer";
            gp.Execute(makeFeatureLayerTool, trackcancel);

            // Create fields for the maximum and recommended speeds for all trucks

            AddField addFieldTool = new AddField();
            addFieldTool.in_table = "Streets_Layer";
            addFieldTool.field_type = "DOUBLE";
            addFieldTool.field_name = "MaximumSpeed_KPH_AllTrucks";
            gp.Execute(addFieldTool, trackcancel);
            addFieldTool.field_name = "RecommendedSpeed_KPH_AllTrucks";
            gp.Execute(addFieldTool, trackcancel);
            
            // Initialize the Delete tool for use later

            Delete deleteTool = new Delete();

            // Open the lrsStatsTable and find the fields we need

            Type factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.FileGDBWorkspaceFactory");
            var wsf = Activator.CreateInstance(factoryType) as IWorkspaceFactory;
            var fws = wsf.OpenFromFile(outputFileGdbPath, 0) as IFeatureWorkspace;
            ITable lrsStatsTable = fws.OpenTable(lrsStatsTableName);
            int restrTypField = lrsStatsTable.FindField("RESTRTYP");
            int vtField = lrsStatsTable.FindField("VT");
            int restrValField = lrsStatsTable.FindField("RESTRVAL");

            // Loop through the lrsStatsTable

            ICursor cur = lrsStatsTable.Search(null, true);
            IRow statsTableRow = null;
            while ((statsTableRow = cur.NextRow()) != null)
            {
                // Get the RESTRTYP, VT, and RESTRVAL field values and determine the field name

                string restrTyp = (string)(statsTableRow.get_Value(restrTypField));
                short vt = (short)(statsTableRow.get_Value(vtField));
                short restrVal = (short)(statsTableRow.get_Value(restrValField));
                string fieldName = "";

                // Create a new field for this restriction
                // (note for speed restrictions, fields were already created earlier)

                if (restrTyp == null || restrTyp.Trim().Length == 0)
                    continue;
                else if (restrTyp != "SP")
                {
                    fieldName = MakeLogisticsFieldName(restrTyp, vt, restrVal);
                    addFieldTool = new AddField();
                    addFieldTool.in_table = "Streets_Layer";
                    addFieldTool.field_name = fieldName;
                    switch (restrTyp.Remove(1))
                    {
                        case "!":
                            addFieldTool.field_type = "DOUBLE";
                            break;
                        case "@":
                            addFieldTool.field_type = "TEXT";
                            addFieldTool.field_length = 1;
                            break;
                        default:
                            continue;    // Only process dimensional (!_) and load (@_) restrictions
                    }
                    gp.Execute(addFieldTool, trackcancel);
                }
                else
                {
                    // for speed restrictions, only process for All Trucks (vt 50)
                    if (vt != 50)
                        continue;
                    else
                    {
                        // only process for Maximum Speeds (restrVal 1) and Recommended Speeds (restrVal 2)
                        switch (restrVal)
                        {
                            case 1:
                                fieldName = "MaximumSpeed_KPH_AllTrucks";
                                break;
                            case 2:
                                fieldName = "RecommendedSpeed_KPH_AllTrucks";
                                break;
                            default:
                                continue;
                        }
                    }
                }

                // Extract the LRS table rows for this restriction

                AddMessage("Extracting information for the " + fieldName + " field...", messages, trackcancel);

                string outputTablePath = outputFileGdbPath + "\\" + fieldName;

                TableSelect tableSelectTool = new TableSelect();
                tableSelectTool.in_table = lrsTablePath;
                tableSelectTool.out_table = outputTablePath;
                tableSelectTool.where_clause = "RESTRTYP = '" + restrTyp + "' AND VT = " + vt + " AND RESTRVAL = " + restrVal;
                gp.Execute(tableSelectTool, trackcancel);

                AddIndex addIndexTool = new AddIndex();
                addIndexTool.in_table = outputTablePath;
                addIndexTool.fields = "ID";
                addIndexTool.index_name = "ID";
                gp.Execute(addIndexTool, trackcancel);

                // Calculate the field for this restriction

                AddMessage("Calculating the " + fieldName + " field...", messages, trackcancel);

                AddJoin addJoinTool = new AddJoin();
                addJoinTool.in_layer_or_view = "Streets_Layer";
                addJoinTool.in_field = "ID";
                addJoinTool.join_table = outputTablePath;
                addJoinTool.join_field = "ID";
                addJoinTool.join_type = "KEEP_COMMON";
                gp.Execute(addJoinTool, trackcancel);

                CalculateField calcFieldTool = new CalculateField();
                calcFieldTool.in_table = "Streets_Layer";
                calcFieldTool.field = StreetsFCName + "." + fieldName;
                switch (restrTyp)
                {
                    case "SP":
                        // Speed restriction -- numeric field must be in km/h
                        calcFieldTool.code_block = "sp = [" + fieldName + ".LIMIT]\nIf [" + fieldName + ".UNIT_MEAS] = 2 Then\n  sp = sp * 1.609344\nEnd If";
                        calcFieldTool.expression = "sp";
                        break;
                    case "!A":
                    case "!B":
                    case "!C":
                    case "!D":
                    case "!E":
                    case "!F":
                        // Weight limits -- numeric field must be in tons
                        calcFieldTool.code_block = "wt = [" + fieldName + ".LIMIT]\nIf [" + fieldName + ".UNIT_MEAS] = 3 Then\n  wt = wt / 0.90718474\nEnd If";
                        calcFieldTool.expression = "wt";
                        break;
                    case "!G":
                    case "!H":
                    case "!I":
                    case "!J":
                    case "!K":
                    case "!L":
                    case "!M":
                    case "!N":
                    case "!O":
                    case "!P":
                        // Length/Width/Height limits -- numeric field must be in feet
                        calcFieldTool.code_block = "d = [" + fieldName + ".LIMIT]\nSelect Case [" + fieldName + ".UNIT_MEAS]\n" +
                                                   "  Case 4: d = d / 30.48\n  Case 5: d = d / 0.3048\n  Case 8: d = d / 12\nEnd Select";
                        calcFieldTool.expression = "d";
                        break;
                    default:
                        // Load restrictions -- string field is the letter "Y"
                        calcFieldTool.expression = "\"Y\"";
                        break;
                }
                calcFieldTool.expression_type = "VB";
                gp.Execute(calcFieldTool, trackcancel);

                RemoveJoin removeJoinTool = new RemoveJoin();
                removeJoinTool.in_layer_or_view = "Streets_Layer";
                removeJoinTool.join_name = fieldName;
                gp.Execute(removeJoinTool, trackcancel);

                // Clean up the extracted data before moving on to the next row

                deleteTool.in_data = outputTablePath;
                gp.Execute(deleteTool, trackcancel);
            }

            // Remove the streets layer before exiting

            deleteTool.in_data = "Streets_Layer";
            gp.Execute(deleteTool, trackcancel);
        }

        private string MakeLogisticsFieldName(string restrTyp, short vt, short restrVal)
        {
            string fieldName = "";

            switch (restrTyp)
            {
                case "!A":
                    fieldName = "GrossVehicleWeight_Tons_";
                    break;
                case "!B":
                    fieldName = "WeightPerAxle_Tons_";
                    break;
                case "!C":
                    fieldName = "TandemAxleWeight_Tons_";
                    break;
                case "!D":
                    fieldName = "TridemAxleWeight_Tons_";
                    break;
                case "!E":
                    fieldName = "OtherWeight_Tons_";
                    break;
                case "!F":
                    fieldName = "UnladenVehicleWeight_Tons_";
                    break;
                case "!G":
                    fieldName = "TotalVehicleLength_Feet_";
                    break;
                case "!H":
                    fieldName = "ExtremeAxleLength_Feet_";
                    break;
                case "!I":
                    fieldName = "TrailerLength_Feet_";
                    break;
                case "!J":
                    fieldName = "TractorLength_Feet_";
                    break;
                case "!K":
                    fieldName = "KingpinToLastAxle_Feet_";
                    break;
                case "!L":
                    fieldName = "KingpinToMiddleOfLastTandem_Feet_";
                    break;
                case "!M":
                    fieldName = "KingpinToEndOfTrailer_Feet_";
                    break;
                case "!N":
                    fieldName = "OtherLength_Feet_";
                    break;
                case "!O":
                    fieldName = "VehicleWidth_Feet_";
                    break;
                case "!P":
                    fieldName = "VehicleHeight_Feet_";
                    break;
                case "@A":
                    fieldName = "AgricultureLoad_";
                    break;
                case "@B":
                    fieldName = "Coal_";
                    break;
                case "@C":
                    fieldName = "BuildingMaterialsLoad_";
                    break;
                case "@D":
                    fieldName = "SanitaryWasteLoad_";
                    break;
                case "@E":
                    fieldName = "SandAndGravel_";
                    break;
                case "@F":
                    fieldName = "CommodityLoad_";
                    break;
                case "@G":
                    fieldName = "NaturalResources_";
                    break;
                case "@H":
                    fieldName = "HazMatClass1_";
                    break;
                case "@I":
                    fieldName = "HazMatClass2_";
                    break;
                case "@J":
                    fieldName = "HazMatClass3_";
                    break;
                case "@K":
                    fieldName = "HazMatClass4_";
                    break;
                case "@L":
                    fieldName = "HazMatClass5_";
                    break;
                case "@M":
                    fieldName = "HazMatClass6_";
                    break;
                case "@N":
                    fieldName = "HazMatClass7_";
                    break;
                case "@O":
                    fieldName = "HazMatClass8_";
                    break;
                case "@P":
                    fieldName = "HazMatClass9_";
                    break;
                case "@Q":
                    fieldName = "HazMatClassI_";
                    break;
                case "@S":
                    fieldName = "GeneralHazardousMaterials_";
                    break;
                case "@T":
                    fieldName = "ExplosiveMaterials_";
                    break;
                case "@U":
                    fieldName = "GoodsHarmfulToWater_";
                    break;
                case "@Z":
                    fieldName = "HazMatAllClasses_";
                    break;
                default:
                    break;
            }

            switch (vt)
            {
                case 50:
                    fieldName += "AllTrucks";
                    break;
                case 51:
                    fieldName += "StraightTrucks";
                    break;
                case 52:
                    fieldName += "TractorSemiTrailers";
                    break;
                case 53:
                    fieldName += "StandardDoubles";
                    break;
                case 54:
                    fieldName += "IntermediateDoubles";
                    break;
                case 55:
                    fieldName += "LongDoubles";
                    break;
                case 56:
                    fieldName += "Triples";
                    break;
                case 57:
                    fieldName += "OtherLongVehicles";
                    break;
                case 58:
                    fieldName += "PublicTrucks";
                    break;
                case 59:
                    fieldName += "ResidentialTrucks";
                    break;
                case 60:
                    fieldName += "DeliveryTrucks";
                    break;
                default:
                    break;
            }

            switch (restrVal)
            {
                case 1:
                    fieldName += "_HardRestr";
                    break;
                case 2:
                    fieldName += "_Immediate";
                    break;
                case 3:
                    fieldName += "_SoftRestr";
                    break;
                case 4:
                    fieldName += "_Preferred";
                    break;
                default:
                    break;
            }

            return fieldName;
        }

        private string MakeLogisticsRestrictionAttributeName(string restrTyp, short vt, short restrVal)
        {
            string attrName = "";

            switch (restrTyp)
            {
                case "!A":
                    attrName = "Weight: Gross Vehicle Weight Restriction for ";
                    break;
                case "!B":
                    attrName = "Weight: Weight Restriction Per Axle for ";
                    break;
                case "!C":
                    attrName = "Weight: Tandem Axle Weight Restriction for ";
                    break;
                case "!D":
                    attrName = "Weight: Tridem Axle Weight Restriction for ";
                    break;
                case "!E":
                    attrName = "Weight: Other Weight Restriction for ";
                    break;
                case "!F":
                    attrName = "Weight: Unladen Vehicle Weight Restriction for ";
                    break;
                case "!G":
                    attrName = "Length: Total Vehicle Length Restriction for ";
                    break;
                case "!H":
                    attrName = "Length: Extreme Axle Length Restriction for ";
                    break;
                case "!I":
                    attrName = "Length: Trailer Length Restriction for ";
                    break;
                case "!J":
                    attrName = "Length: Tractor Length Restriction for ";
                    break;
                case "!K":
                    attrName = "Length: Kingpin To Last Axle Restriction for ";
                    break;
                case "!L":
                    attrName = "Length: Kingpin To Middle Of Last Tandem Restriction for ";
                    break;
                case "!M":
                    attrName = "Length: Kingpin To End Of Trailer Restriction for ";
                    break;
                case "!N":
                    attrName = "Length: Other Length Restriction for ";
                    break;
                case "!O":
                    attrName = "Width: Vehicle Width Restriction for ";
                    break;
                case "!P":
                    attrName = "Height: Vehicle Height Restriction for ";
                    break;
                case "@A":
                    attrName = "Load: Agriculture Load on ";
                    break;
                case "@B":
                    attrName = "Load: Coal on ";
                    break;
                case "@C":
                    attrName = "Load: Building Materials Load on ";
                    break;
                case "@D":
                    attrName = "Load: Sanitary Waste Load on ";
                    break;
                case "@E":
                    attrName = "Load: Sand And Gravel on ";
                    break;
                case "@F":
                    attrName = "Load: Commodity Load on ";
                    break;
                case "@G":
                    attrName = "Load: Natural Resources on ";
                    break;
                case "@H":
                    attrName = "HazMat: Class 1: Explosives on ";
                    break;
                case "@I":
                    attrName = "HazMat: Class 2: Flammable/Compressed/Poisonous Gases on ";
                    break;
                case "@J":
                    attrName = "HazMat: Class 3: Flammable/Compressed Liquids on ";
                    break;
                case "@K":
                    attrName = "HazMat: Class 4: Flammable Solids/Spontaneously Combusible Materials on ";
                    break;
                case "@L":
                    attrName = "HazMat: Class 5: Oxidizers/Organic Peroxides on ";
                    break;
                case "@M":
                    attrName = "HazMat: Class 6: Poisonous/Toxic/Infectious Substances on ";
                    break;
                case "@N":
                    attrName = "HazMat: Class 7: Radioactive Materials on ";
                    break;
                case "@O":
                    attrName = "HazMat: Class 8: Corrosive Materials on ";
                    break;
                case "@P":
                    attrName = "HazMat: Class 9: Miscellaneous Hazardous Materials on ";
                    break;
                case "@Q":
                    attrName = "HazMat: Class I: Poisonous Inhalation Hazards on ";
                    break;
                case "@S":
                    attrName = "General Hazardous Materials on ";
                    break;
                case "@T":
                    attrName = "Explosive Materials on ";
                    break;
                case "@U":
                    attrName = "Goods Harmful To Water on ";
                    break;
                case "@Z":
                    attrName = "HazMat: All Classes on ";
                    break;
                default:
                    break;
            }

            switch (vt)
            {
                case 50:
                    attrName += "All Trucks";
                    break;
                case 51:
                    attrName += "Straight Trucks";
                    break;
                case 52:
                    attrName += "Tractor Semi Trailers";
                    break;
                case 53:
                    attrName += "Standard Doubles";
                    break;
                case 54:
                    attrName += "Intermediate Doubles";
                    break;
                case 55:
                    attrName += "Long Doubles";
                    break;
                case 56:
                    attrName += "Triples";
                    break;
                case 57:
                    attrName += "Other Long Vehicles";
                    break;
                case 58:
                    attrName += "Public Trucks";
                    break;
                case 59:
                    attrName += "Residential Trucks";
                    break;
                case 60:
                    attrName += "Delivery Trucks";
                    break;
                default:
                    break;
            }

            switch (restrVal)
            {
                case 1:
                    attrName += " (Hard Prohibited)";
                    break;
                case 2:
                    attrName += " (Immediate Access Only)";
                    break;
                case 3:
                    attrName += " (Soft Restricted)";
                    break;
                case 4:
                    attrName += " (Preferred)";
                    break;
                default:
                    break;
            }

            return attrName;
        }

        private string MakeLogisticsLimitAttributeName(string restrTyp, short vt, short restrVal, bool metricUnits)
        {
            string attrName = "";

            if (metricUnits)
            {
                switch (restrTyp)
                {
                    case "!A":
                        attrName = "Weight: Gross Vehicle Weight Limit (metric tons) for ";
                        break;
                    case "!B":
                        attrName = "Weight: Weight Limit Per Axle (metric tons) for ";
                        break;
                    case "!C":
                        attrName = "Weight: Tandem Axle Weight Limit (metric tons) for ";
                        break;
                    case "!D":
                        attrName = "Weight: Tridem Axle Weight Limit (metric tons) for ";
                        break;
                    case "!E":
                        attrName = "Weight: Other Weight Limit (metric tons) for ";
                        break;
                    case "!F":
                        attrName = "Weight: Unladen Vehicle Weight Limit (metric tons) for ";
                        break;
                    case "!G":
                        attrName = "Length: Total Vehicle Length Limit (meters) for ";
                        break;
                    case "!H":
                        attrName = "Length: Extreme Axle Length Limit (meters) for ";
                        break;
                    case "!I":
                        attrName = "Length: Trailer Length Limit (meters) for ";
                        break;
                    case "!J":
                        attrName = "Length: Tractor Length Limit (meters) for ";
                        break;
                    case "!K":
                        attrName = "Length: Kingpin To Last Axle Limit (meters) for ";
                        break;
                    case "!L":
                        attrName = "Length: Kingpin To Middle Of Last Tandem Limit (meters) for ";
                        break;
                    case "!M":
                        attrName = "Length: Kingpin To End Of Trailer Limit (meters) for ";
                        break;
                    case "!N":
                        attrName = "Length: Other Length Limit (meters) for ";
                        break;
                    case "!O":
                        attrName = "Width: Vehicle Width Limit (meters) for ";
                        break;
                    case "!P":
                        attrName = "Height: Vehicle Height Limit (meters) for ";
                        break;
                    default:
                        break;
                }
            }
            else
            {
                switch (restrTyp)
                {
                    case "!A":
                        attrName = "Weight: Gross Vehicle Weight Limit (tons) for ";
                        break;
                    case "!B":
                        attrName = "Weight: Weight Limit Per Axle (tons) for ";
                        break;
                    case "!C":
                        attrName = "Weight: Tandem Axle Weight Limit (tons) for ";
                        break;
                    case "!D":
                        attrName = "Weight: Tridem Axle Weight Limit (tons) for ";
                        break;
                    case "!E":
                        attrName = "Weight: Other Weight Limit (tons) for ";
                        break;
                    case "!F":
                        attrName = "Weight: Unladen Vehicle Weight Limit (tons) for ";
                        break;
                    case "!G":
                        attrName = "Length: Total Vehicle Length Limit (feet) for ";
                        break;
                    case "!H":
                        attrName = "Length: Extreme Axle Length Limit (feet) for ";
                        break;
                    case "!I":
                        attrName = "Length: Trailer Length Limit (feet) for ";
                        break;
                    case "!J":
                        attrName = "Length: Tractor Length Limit (feet) for ";
                        break;
                    case "!K":
                        attrName = "Length: Kingpin To Last Axle Limit (feet) for ";
                        break;
                    case "!L":
                        attrName = "Length: Kingpin To Middle Of Last Tandem Limit (feet) for ";
                        break;
                    case "!M":
                        attrName = "Length: Kingpin To End Of Trailer Limit (feet) for ";
                        break;
                    case "!N":
                        attrName = "Length: Other Length Limit (feet) for ";
                        break;
                    case "!O":
                        attrName = "Width: Vehicle Width Limit (feet) for ";
                        break;
                    case "!P":
                        attrName = "Height: Vehicle Height Limit (feet) for ";
                        break;
                    default:
                        break;
                }
            }

            switch (vt)
            {
                case 50:
                    attrName += "All Trucks";
                    break;
                case 51:
                    attrName += "Straight Trucks";
                    break;
                case 52:
                    attrName += "Tractor Semi Trailers";
                    break;
                case 53:
                    attrName += "Standard Doubles";
                    break;
                case 54:
                    attrName += "Intermediate Doubles";
                    break;
                case 55:
                    attrName += "Long Doubles";
                    break;
                case 56:
                    attrName += "Triples";
                    break;
                case 57:
                    attrName += "Other Long Vehicles";
                    break;
                case 58:
                    attrName += "Public Trucks";
                    break;
                case 59:
                    attrName += "Residential Trucks";
                    break;
                case 60:
                    attrName += "Delivery Trucks";
                    break;
                default:
                    break;
            }

            switch (restrVal)
            {
                case 1:
                    attrName += " (Hard Prohibited)";
                    break;
                case 2:
                    attrName += " (Immediate Access Only)";
                    break;
                case 3:
                    attrName += " (Soft Restricted)";
                    break;
                case 4:
                    attrName += " (Preferred)";
                    break;
                default:
                    break;
            }

            return attrName;
        }

        private string MakeLogisticsAttributeParameterName(string restrTyp, bool metricUnits)
        {
            string paramName = "";

            if (metricUnits)
            {
                switch (restrTyp)
                {
                    case "!A":
                        paramName = "Gross Vehicle Weight (metric tons)";
                        break;
                    case "!B":
                        paramName = "Weight Per Axle (metric tons)";
                        break;
                    case "!C":
                        paramName = "Tandem Axle Weight (metric tons)";
                        break;
                    case "!D":
                        paramName = "Tridem Axle Weight (metric tons)";
                        break;
                    case "!E":
                        paramName = "Other Weight (metric tons)";
                        break;
                    case "!F":
                        paramName = "Unladen Vehicle Weight (metric tons)";
                        break;
                    case "!G":
                        paramName = "Total Vehicle Length (meters)";
                        break;
                    case "!H":
                        paramName = "Extreme Axle Length (meters)";
                        break;
                    case "!I":
                        paramName = "Trailer Length (meters)";
                        break;
                    case "!J":
                        paramName = "Tractor Length (meters)";
                        break;
                    case "!K":
                        paramName = "Kingpin To Last Axle (meters)";
                        break;
                    case "!L":
                        paramName = "Kingpin To Middle Of Last Tandem (meters)";
                        break;
                    case "!M":
                        paramName = "Kingpin To End Of Trailer (meters)";
                        break;
                    case "!N":
                        paramName = "Other Length (meters)";
                        break;
                    case "!O":
                        paramName = "Vehicle Width (meters)";
                        break;
                    case "!P":
                        paramName = "Vehicle Height (meters)";
                        break;
                    default:
                        break;
                }
            }
            else
            {
                switch (restrTyp)
                {
                    case "!A":
                        paramName = "Gross Vehicle Weight (tons)";
                        break;
                    case "!B":
                        paramName = "Weight Per Axle (tons)";
                        break;
                    case "!C":
                        paramName = "Tandem Axle Weight (tons)";
                        break;
                    case "!D":
                        paramName = "Tridem Axle Weight (tons)";
                        break;
                    case "!E":
                        paramName = "Other Weight (tons)";
                        break;
                    case "!F":
                        paramName = "Unladen Vehicle Weight (tons)";
                        break;
                    case "!G":
                        paramName = "Total Vehicle Length (feet)";
                        break;
                    case "!H":
                        paramName = "Extreme Axle Length (feet)";
                        break;
                    case "!I":
                        paramName = "Trailer Length (feet)";
                        break;
                    case "!J":
                        paramName = "Tractor Length (feet)";
                        break;
                    case "!K":
                        paramName = "Kingpin To Last Axle (feet)";
                        break;
                    case "!L":
                        paramName = "Kingpin To Middle Of Last Tandem (feet)";
                        break;
                    case "!M":
                        paramName = "Kingpin To End Of Trailer (feet)";
                        break;
                    case "!N":
                        paramName = "Other Length (feet)";
                        break;
                    case "!O":
                        paramName = "Vehicle Width (feet)";
                        break;
                    case "!P":
                        paramName = "Vehicle Height (feet)";
                        break;
                    default:
                        break;
                }
            }

            return paramName;
        }

        private void CreateSignposts(string inputSITablePath, string inputSPTablePath, string outputFileGdbPath, 
                                     IGPMessages messages, ITrackCancel trackcancel)
        {
            // Open the input SI and SP tables

            ITable inputSignInformationTable = m_gpUtils.OpenTableFromString(inputSITablePath);
            ITable inputSignPathTable = m_gpUtils.OpenTableFromString(inputSPTablePath);

            // Open the Streets feature class

            Type gdbFactoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.FileGDBWorkspaceFactory");
            var gdbWSF = Activator.CreateInstance(gdbFactoryType) as IWorkspaceFactory;
            var gdbFWS = gdbWSF.OpenFromFile(outputFileGdbPath, 0) as IFeatureWorkspace;
            IFeatureClass inputLineFeatures = gdbFWS.OpenFeatureClass(StreetsFCName);

            // Create the signpost feature class and table

            IFeatureClass outputSignFeatures = SignpostUtilities.CreateSignsFeatureClass(inputLineFeatures, SignpostFCName);
            ITable outputSignDetailTable = SignpostUtilities.CreateSignsDetailTable(inputLineFeatures, SignpostJoinTableName);

            #region Find fields
            //(Validate checked that these exist)
            IFields inputSITableFields = inputSignInformationTable.Fields;
            int inSiIdFI = inputSITableFields.FindField("ID");
            int inSiInfoTypFI = inputSITableFields.FindField("INFOTYP");
            int inSiTxtContFI = inputSITableFields.FindField("TXTCONT");
            int inSiTxtContLCFI = inputSITableFields.FindField("TXTCONTLC");
            int inSiConTypFI = inputSITableFields.FindField("CONTYP");
            IFields inputSPTableFields = inputSignPathTable.Fields;
            int inSpIdFI = inputSPTableFields.FindField("ID");
            int inSpSeqNrFI = inputSPTableFields.FindField("SEQNR");
            int inSpTrpElIdFI = inputSPTableFields.FindField("TRPELID");
            int inSpTrpElTypFI = inputSPTableFields.FindField("TRPELTYP");

            // Find output fields (we just made these)

            IFields outputSignFeatureFields = outputSignFeatures.Fields;
            int outExitNameFI = outputSignFeatureFields.FindField("ExitName");

            int[] outBranchXFI = new int[SignpostUtilities.MaxBranchCount];
            int[] outBranchXDirFI = new int[SignpostUtilities.MaxBranchCount];
            int[] outBranchXLngFI = new int[SignpostUtilities.MaxBranchCount];
            int[] outTowardXFI = new int[SignpostUtilities.MaxBranchCount];
            int[] outTowardXLngFI = new int[SignpostUtilities.MaxBranchCount];

            string indexString;

            for (int i = 0; i < SignpostUtilities.MaxBranchCount; i++)
            {
                indexString = Convert.ToString(i, System.Globalization.CultureInfo.InvariantCulture);

                outBranchXFI[i] = outputSignFeatureFields.FindField("Branch" + indexString);
                outBranchXDirFI[i] = outputSignFeatureFields.FindField("Branch" + indexString + "Dir");
                outBranchXLngFI[i] = outputSignFeatureFields.FindField("Branch" + indexString + "Lng");
                outTowardXFI[i] = outputSignFeatureFields.FindField("Toward" + indexString);
                outTowardXLngFI[i] = outputSignFeatureFields.FindField("Toward" + indexString + "Lng");
            }

            IFields outputTableFields = outputSignDetailTable.Fields;
            int outTblSignpostIDFI = outputTableFields.FindField("SignpostID");
            int outTblSequenceFI = outputTableFields.FindField("Sequence");
            int outTblEdgeFCIDFI = outputTableFields.FindField("EdgeFCID");
            int outTblEdgeFIDFI = outputTableFields.FindField("EdgeFID");
            int outTblEdgeFrmPosFI = outputTableFields.FindField("EdgeFrmPos");
            int outTblEdgeToPosFI = outputTableFields.FindField("EdgeToPos");

            // Find ID fields on referenced lines

            int inLinesOIDFI = inputLineFeatures.FindField(inputLineFeatures.OIDFieldName);
            int inLinesUserIDFI = inputLineFeatures.FindField("ID");
            int inLinesShapeFI = inputLineFeatures.FindField(inputLineFeatures.ShapeFieldName);

            #endregion

            // Get the language lookup hash

            System.Collections.Hashtable langLookup = CreateLanguageLookup();
            
            // Fetch all line features referenced by the input signs table.  We do the
            // "join" this hard way to support all data sources in the sample. 
            // Also, for large numbers of sign records, this strategy of fetching all
            // related features and holding them in RAM could be a problem.  To fix
            // this, one could process the input sign records in batches.

            System.Collections.Hashtable lineFeaturesList = SignpostUtilities.FillFeatureCache(inputSignPathTable, inSpTrpElIdFI, -1, inputLineFeatures, "ID", trackcancel);

            // Create output feature/row buffers

            IFeatureBuffer featureBuffer = outputSignFeatures.CreateFeatureBuffer();
            IFeature feature = featureBuffer as IFeature;
            IRowBuffer featureRowBuffer = featureBuffer as IRowBuffer;

            IRowBuffer tableBuffer = outputSignDetailTable.CreateRowBuffer();
            IRow row = tableBuffer as IRow;
            IRowBuffer tableRowBuffer = tableBuffer as IRowBuffer;

            // Create insert cursors.

            IFeatureCursor featureInsertCursor = outputSignFeatures.Insert(true);
            ICursor tableInsertCursor = outputSignDetailTable.Insert(true);

            // Create input cursors for the sign tables we are importing

            ITableSort spTableSort = new TableSortClass();
            spTableSort.Fields = "ID, SEQNR";
            spTableSort.set_Ascending("ID", true);
            spTableSort.set_Ascending("SEQNR", true);
            spTableSort.QueryFilter = null;
            spTableSort.Table = inputSignPathTable;
            spTableSort.Sort(null);
            ICursor spInputCursor = spTableSort.Rows;

            ITableSort siTableSort = new TableSortClass();
            siTableSort.Fields = "ID, SEQNR, DESTSEQ, RNPART";
            siTableSort.set_Ascending("ID", true);
            siTableSort.set_Ascending("SEQNR", true);
            siTableSort.set_Ascending("DESTSEQ", true);
            siTableSort.set_Ascending("RNPART", true);
            siTableSort.QueryFilter = null;
            siTableSort.Table = inputSignInformationTable;
            siTableSort.Sort(null);
            ICursor siInputCursor = siTableSort.Rows;

            IRow inputSpTableRow;
            IRow inputSiTableRow;
            long currentID = -1, loopSpID, loopSiID;

            int numOutput = 0;
            int numInput = 0;
            bool fetchFeatureDataSucceeded;
            ArrayList idVals = new System.Collections.ArrayList(2);
            ArrayList edgesData = new System.Collections.ArrayList(2);
            ArrayList reverseEdge = new System.Collections.ArrayList(2);
            SignpostUtilities.FeatureData currentFeatureData = new SignpostUtilities.FeatureData(-1, null);
            ICurve earlierEdgeCurve, laterEdgeCurve;
            IPoint earlierEdgeStart, earlierEdgeEnd;
            IPoint laterEdgeStart, laterEdgeEnd;

            int nextBranchNum = -1, nextTowardNum = -1;
            string infoTypText, txtContText;
            string txtContTextLC, langVal;
            int conTypVal;

            int refLinesFCID = inputLineFeatures.ObjectClassID;
            IGeometry outputSignGeometry;
            object newOID;

            inputSpTableRow = spInputCursor.NextRow();
            inputSiTableRow = siInputCursor.NextRow();
            while (inputSpTableRow != null && inputSiTableRow != null)
            {
                currentID = Convert.ToInt64(inputSpTableRow.get_Value(inSpIdFI));

                // fetch the edge ID values from the SP table for the current sign ID

                idVals.Clear();
                while (true)
                {
                    idVals.Add(Convert.ToInt64(inputSpTableRow.get_Value(inSpTrpElIdFI)));

                    inputSpTableRow = spInputCursor.NextRow();
                    if (inputSpTableRow == null)
                        break;    // we've reached the end of the SP table

                    loopSpID = Convert.ToInt64(inputSpTableRow.get_Value(inSpIdFI));
                    if (loopSpID != currentID)
                        break;    // we're now on a new ID value                    
                }

                numInput++;

                // fetch the FeatureData for each of these edges

                edgesData.Clear();
                fetchFeatureDataSucceeded = true;
                foreach (long currentIDVal in idVals)
                {
                    try
                    {
                        currentFeatureData = (SignpostUtilities.FeatureData)lineFeaturesList[currentIDVal];
                        edgesData.Add(currentFeatureData);
                    }
                    catch
                    {
                        fetchFeatureDataSucceeded = false;
                        if (numInput - numOutput < 100)
                        {
                            messages.AddWarning("Line feature not found for signpost with ID: " +
                                Convert.ToString(currentIDVal, System.Globalization.CultureInfo.InvariantCulture));
                        }
                        break;
                    }
                }
                if (!fetchFeatureDataSucceeded)
                    continue;

                if (edgesData.Count == 1)
                {
                    messages.AddWarning("Signpost with ID " + Convert.ToString(currentID, System.Globalization.CultureInfo.InvariantCulture)
                        + " only has one transportation element.");
                    continue;
                }

                // determine the orientation for each of these edges

                reverseEdge.Clear();
                for (int i = 1; i < edgesData.Count; i++)
                {
                    // get the endpoints of the earlier curve
                    currentFeatureData = (SignpostUtilities.FeatureData)edgesData[i - 1];
                    earlierEdgeCurve = currentFeatureData.feature as ICurve;
                    earlierEdgeStart = earlierEdgeCurve.FromPoint;
                    earlierEdgeEnd = earlierEdgeCurve.ToPoint;

                    // get the endpoints of the later curve
                    currentFeatureData = (SignpostUtilities.FeatureData)edgesData[i];
                    laterEdgeCurve = currentFeatureData.feature as ICurve;
                    laterEdgeStart = laterEdgeCurve.FromPoint;
                    laterEdgeEnd = laterEdgeCurve.ToPoint;

                    // determine the orientation of the first edge
                    // (first edge is reversed if its Start point is coincident with either point of the second edge)
                    if (i == 1)
                        reverseEdge.Add(TurnGeometryUtilities.EqualPoints(earlierEdgeStart, laterEdgeStart) || TurnGeometryUtilities.EqualPoints(earlierEdgeStart, laterEdgeEnd));

                    // determine the orientation of the i'th edge
                    // (i'th edge is reversed if its End point is coincident with either point of the previous edge)
                    reverseEdge.Add(TurnGeometryUtilities.EqualPoints(laterEdgeEnd, earlierEdgeStart) || TurnGeometryUtilities.EqualPoints(laterEdgeEnd, earlierEdgeEnd));
                }

                // write out the sign geometry to the featureBuffer

                outputSignGeometry = MakeSignGeometry(edgesData, reverseEdge);
                featureBuffer.Shape = outputSignGeometry;

                // fetch the signpost information from the SI table for the current sign ID

                nextBranchNum = 0;
                nextTowardNum = 0;
                featureBuffer.set_Value(outExitNameFI, "");

                while (inputSiTableRow != null)
                {
                    loopSiID = Convert.ToInt64(inputSiTableRow.get_Value(inSiIdFI));
                    if (loopSiID < currentID)
                    {
                        inputSiTableRow = siInputCursor.NextRow();
                        continue;
                    }
                    else if (loopSiID > currentID)
                    {
                        break;    // we're now on a new ID value
                    }

                    infoTypText = inputSiTableRow.get_Value(inSiInfoTypFI) as string;
                    txtContText = inputSiTableRow.get_Value(inSiTxtContFI) as string;
                    txtContTextLC = inputSiTableRow.get_Value(inSiTxtContLCFI) as string;
                    langVal = SignpostUtilities.GetLanguageValue(txtContTextLC, langLookup);
                    conTypVal = Convert.ToInt32(inputSiTableRow.get_Value(inSiConTypFI));

                    switch (infoTypText)
                    {
                        case "4E":    // exit number

                            featureBuffer.set_Value(outExitNameFI, txtContText);

                            break;
                        case "9D":    // place name
                        case "4I":    // other destination

                            // check for schema overflow
                            if (nextTowardNum > SignpostUtilities.MaxBranchCount - 1)
                            {
                                inputSiTableRow = siInputCursor.NextRow();
                                continue;
                            }

                            // set values
                            featureBuffer.set_Value(outTowardXFI[nextTowardNum], txtContText);
                            featureBuffer.set_Value(outTowardXLngFI[nextTowardNum], langVal);

                            // get ready for next toward
                            nextTowardNum++;

                            break;
                        case "6T":    // street name
                        case "RN":    // route number

                            if (conTypVal == 2)    // toward
                            {
                                // check for schema overflow
                                if (nextTowardNum > SignpostUtilities.MaxBranchCount - 1)
                                {
                                    inputSiTableRow = siInputCursor.NextRow();
                                    continue;
                                }

                                // set values
                                featureBuffer.set_Value(outTowardXFI[nextTowardNum], txtContText);
                                featureBuffer.set_Value(outTowardXLngFI[nextTowardNum], langVal);

                                // get ready for next toward
                                nextTowardNum++;
                            }
                            else    // branch
                            {
                                // check for schema overflow
                                if (nextBranchNum > SignpostUtilities.MaxBranchCount - 1)
                                {
                                    inputSiTableRow = siInputCursor.NextRow();
                                    continue;
                                }

                                // set values
                                featureBuffer.set_Value(outBranchXFI[nextBranchNum], txtContText);
                                featureBuffer.set_Value(outBranchXDirFI[nextBranchNum], "");
                                featureBuffer.set_Value(outBranchXLngFI[nextBranchNum], "en");

                                // get ready for next branch
                                nextBranchNum++;
                            }

                            break;
                    }  // switch

                    inputSiTableRow = siInputCursor.NextRow();

                }  // each SI table record

                // clean up unused parts of the row and pack toward/branch items

                SignpostUtilities.CleanUpSignpostFeatureValues(featureBuffer, nextBranchNum - 1, nextTowardNum - 1,
                                                               outBranchXFI, outBranchXDirFI, outBranchXLngFI,
                                                               outTowardXFI, outTowardXLngFI);

                // insert sign feature record

                newOID = featureInsertCursor.InsertFeature(featureBuffer);

                // set streets table values

                tableRowBuffer.set_Value(outTblSignpostIDFI, newOID);
                tableRowBuffer.set_Value(outTblEdgeFCIDFI, refLinesFCID);
                for (int i = 0; i < edgesData.Count; i++)
                {
                    currentFeatureData = (SignpostUtilities.FeatureData)edgesData[i];
                    tableRowBuffer.set_Value(outTblSequenceFI, i + 1);
                    tableRowBuffer.set_Value(outTblEdgeFIDFI, currentFeatureData.OID);
                    if ((bool)reverseEdge[i])
                    {
                        tableRowBuffer.set_Value(outTblEdgeFrmPosFI, 1.0);
                        tableRowBuffer.set_Value(outTblEdgeToPosFI, 0.0);
                    }
                    else
                    {
                        tableRowBuffer.set_Value(outTblEdgeFrmPosFI, 0.0);
                        tableRowBuffer.set_Value(outTblEdgeToPosFI, 1.0);
                    }

                    // insert detail record

                    tableInsertCursor.InsertRow(tableRowBuffer);
                }

                numOutput++;
                if ((numOutput % 100) == 0)
                {
                    // check for user cancel

                    if (trackcancel != null && !trackcancel.Continue())
                        throw (new COMException("Function cancelled."));
                }

            }  // outer while

            // Flush any outstanding writes to the feature class and table
            featureInsertCursor.Flush();
            tableInsertCursor.Flush();

            // add a summary message

            messages.AddMessage(Convert.ToString(numOutput) + " of " + Convert.ToString(numInput) + " signposts added.");

            return;
        }

        private Hashtable CreateLanguageLookup()
        {
            Hashtable lookupHash = new System.Collections.Hashtable(93);
            lookupHash.Add("ALB", "sq");  // Albanian
            lookupHash.Add("ALS", "");  // Alsacian
            lookupHash.Add("ARA", "ar");  // Arabic
            lookupHash.Add("BAQ", "eu");  // Basque
            lookupHash.Add("BAT", "");  // Baltic (Other)
            lookupHash.Add("BEL", "be");  // Belarusian
            lookupHash.Add("BET", "be");  // Belarusian (Latin)
            lookupHash.Add("BOS", "bs");  // Bosnian
            lookupHash.Add("BRE", "br");  // Breton
            lookupHash.Add("BUL", "bg");  // Bulgarian
            lookupHash.Add("BUN", "bg");  // Bulgarian (Latin)
            lookupHash.Add("BUR", "my");  // Burmese
            lookupHash.Add("CAT", "ca");  // Catalan
            lookupHash.Add("CEL", "");  // Celtic (Other)
            lookupHash.Add("CHI", "zh");  // Chinese, Han Simplified
            lookupHash.Add("CHL", "zh");  // Chinese, Mandarin Pinyin
            lookupHash.Add("CHT", "zh");  // Chinese, Han Traditional
            lookupHash.Add("CTN", "zh");  // Chinese, Cantonese Pinyin
            lookupHash.Add("CZE", "cs");  // Czech
            lookupHash.Add("DAN", "da");  // Danish
            lookupHash.Add("DUT", "nl");  // Dutch
            lookupHash.Add("ENG", "en");  // English
            lookupHash.Add("EST", "et");  // Estonian
            lookupHash.Add("FAO", "fo");  // Faroese
            lookupHash.Add("FIL", "");  // Filipino
            lookupHash.Add("FIN", "fi");  // Finnish
            lookupHash.Add("FRE", "fr");  // French
            lookupHash.Add("FRY", "fy");  // Frisian
            lookupHash.Add("FUR", "");  // Friulian
            lookupHash.Add("GEM", "");  // Franco-Provencal
            lookupHash.Add("GER", "de");  // German
            lookupHash.Add("GLA", "gd");  // Gaelic (Scots)
            lookupHash.Add("GLE", "ga");  // Irish
            lookupHash.Add("GLG", "gl");  // Galician
            lookupHash.Add("GRE", "el");  // Greek (Modern)
            lookupHash.Add("GRL", "el");  // Greek (Latin Transcription)
            lookupHash.Add("HEB", "he");  // Hebrew
            lookupHash.Add("HIN", "hi");  // Hindi
            lookupHash.Add("HUN", "hu");  // Hungarian
            lookupHash.Add("ICE", "is");  // Icelandic
            lookupHash.Add("IND", "id");  // Indonesian
            lookupHash.Add("ITA", "it");  // Italian
            lookupHash.Add("KHM", "km");  // Khmer
            lookupHash.Add("KOL", "ko");  // Korean (Latin)
            lookupHash.Add("KOR", "ko");  // Korean
            lookupHash.Add("LAD", "");  // Ladin
            lookupHash.Add("LAO", "lo");  // Lao
            lookupHash.Add("LAT", "la");  // Latin
            lookupHash.Add("LAV", "lv");  // Latvian
            lookupHash.Add("LIT", "lt");  // Lithuanian
            lookupHash.Add("LTZ", "lb");  // Letzeburgesch
            lookupHash.Add("MAC", "mk");  // Macedonian
            lookupHash.Add("MAP", "");  // Austronesian (Other)
            lookupHash.Add("MAT", "mk");  // Macedonian (Latin Transcription)
            lookupHash.Add("MAY", "ms");  // Malaysian
            lookupHash.Add("MLT", "mt");  // Maltese
            lookupHash.Add("MOL", "mo");  // Moldavian
            lookupHash.Add("MYN", "");  // Mayan Languages
            lookupHash.Add("NOR", "no");  // Norwegian
            lookupHash.Add("OCI", "oc");  // Occitan
            lookupHash.Add("PAA", "");  // Papuan-Australian (Other)
            lookupHash.Add("POL", "pl");  // Polish
            lookupHash.Add("POR", "pt");  // Portuguese
            lookupHash.Add("PRO", "");  // Provencal
            lookupHash.Add("ROA", "");  // Romance (Other)
            lookupHash.Add("ROH", "rm");  // Raeto-Romance
            lookupHash.Add("ROM", "");  // Romani
            lookupHash.Add("RUL", "ru");  // Russian (Latin Transcription)
            lookupHash.Add("RUM", "ro");  // Romanian
            lookupHash.Add("RUS", "ru");  // Russian
            lookupHash.Add("SCC", "sh");  // Serbian (Latin)
            lookupHash.Add("SCO", "gd");  // Scots
            lookupHash.Add("SCR", "sh");  // Croatian
            lookupHash.Add("SCY", "sh");  // Serbian (Cyrillic)
            lookupHash.Add("SLA", "cu");  // Slavic
            lookupHash.Add("SLO", "sk");  // Slovak
            lookupHash.Add("SLV", "sv");  // Slovenian
            lookupHash.Add("SMC", "");  // Montenegrin (Cyrillic)
            lookupHash.Add("SMI", "se");  // Lapp (Sami)
            lookupHash.Add("SML", "");  // Montenegrin (Latin)
            lookupHash.Add("SPA", "es");  // Spanish
            lookupHash.Add("SRD", "sc");  // Sardinian
            lookupHash.Add("SWE", "sv");  // Swedish
            lookupHash.Add("THA", "th");  // Thai
            lookupHash.Add("THL", "th");  // Thai (Latin)
            lookupHash.Add("TUR", "tr");  // Turkish
            lookupHash.Add("UKL", "uk");  // Ukranian (Latin)
            lookupHash.Add("UKR", "uk");  // Ukranian
            lookupHash.Add("UND", "");  // Undefined
            lookupHash.Add("VAL", "ca");  // Valencian
            lookupHash.Add("VIE", "vi");  // Vietnamese
            lookupHash.Add("WEL", "cy");  // Welsh
            lookupHash.Add("WEN", "");  // Sorbian (Other)

            return lookupHash;
        }
        
        private IGeometry MakeSignGeometry(ArrayList edgesData, ArrayList reverseEdge)
        {
            ISegmentCollection resultSegments = new PolylineClass();
            SignpostUtilities.FeatureData currentFeatureData = new SignpostUtilities.FeatureData(-1, null);
            ICurve currentCurve, resultCurve;

            for (int i = 0; i < edgesData.Count; i++)
            {
                // fetch the curve and reverse it as needed

                currentFeatureData = (SignpostUtilities.FeatureData)edgesData[i];
                currentCurve = currentFeatureData.feature as ICurve;
                if ((bool)reverseEdge[i])
                    currentCurve.ReverseOrientation();

                // trim the first and last geometries so that they only cover 25% of the street feature

                if (i == 0)
                    currentCurve.GetSubcurve(0.75, 1.0, true, out resultCurve);
                else if (i == (edgesData.Count - 1))
                    currentCurve.GetSubcurve(0.0, 0.25, true, out resultCurve);
                else
                    resultCurve = currentCurve;

                // add the resulting geometry to the collection

                resultSegments.AddSegmentCollection(resultCurve as ISegmentCollection);
            }

            return resultSegments as IGeometry;
        }

        private void CreateAndBuildNetworkDataset(string outputFileGdbPath, double fgdbVersion, string fdsName, string ndsName,
                                                  bool createNetworkAttributesInMetric, bool createTwoDistanceAttributes,
                                                  string timeZoneIDBaseFieldName, bool directedTimeZoneIDFields, string commonTimeZone,
                                                  bool usesHistoricalTraffic, ITrafficFeedLocation trafficFeedLocation,
                                                  bool usesRSTable, bool usesLTRTable, bool usesLRSTable, string lrsStatsTableName)
        {
            // This code is modified from "How to create a network dataset" in the ArcObjects SDK.

            //
            // Create a network dataset data element
            //

            // Create an empty data element for a buildable network dataset.
            IDENetworkDataset3 deNetworkDataset = new DENetworkDatasetClass();
            deNetworkDataset.Buildable = true;

            // Open the feature dataset and cast to the IGeoDataset interface.
            Type factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.FileGDBWorkspaceFactory");
            var workspaceFactory = (IWorkspaceFactory)Activator.CreateInstance(factoryType);
            IWorkspace workspace = workspaceFactory.OpenFromFile(outputFileGdbPath, 0);
            var featureWorkspace = (IFeatureWorkspace)workspace;
            IFeatureDataset featureDataset = featureWorkspace.OpenFeatureDataset(fdsName);
            var geoDataset = (IGeoDataset)featureDataset;

            // Copy the feature dataset's extent and spatial reference to the network dataset data element.
            var deGeoDataset = (IDEGeoDataset)deNetworkDataset;
            deGeoDataset.Extent = geoDataset.Extent;
            deGeoDataset.SpatialReference = geoDataset.SpatialReference;

            // Specify the name of the network dataset.
            var dataElement = (IDataElement)deNetworkDataset;
            dataElement.Name = ndsName;

            //
            // Add network sources
            //

            // Specify the network dataset's elevation model.
            deNetworkDataset.ElevationModel = esriNetworkElevationModel.esriNEMElevationFields;

            // Create an EdgeFeatureSource object and point it to the Streets feature class.
            INetworkSource edgeNetworkSource = new EdgeFeatureSourceClass();
            edgeNetworkSource.Name = StreetsFCName;
            edgeNetworkSource.ElementType = esriNetworkElementType.esriNETEdge;

            // Set the edge feature source's connectivity settings.
            var edgeFeatureSource = (IEdgeFeatureSource)edgeNetworkSource;
            edgeFeatureSource.UsesSubtypes = false;
            edgeFeatureSource.ClassConnectivityGroup = 1;
            edgeFeatureSource.ClassConnectivityPolicy = esriNetworkEdgeConnectivityPolicy.esriNECPEndVertex;
            edgeFeatureSource.FromElevationFieldName = "F_ELEV";
            edgeFeatureSource.ToElevationFieldName = "T_ELEV";

            //
            // Specify directions settings for the edge source
            //

            // Create a StreetNameFields object and populate its settings.
            IStreetNameFields streetNameFields = new StreetNameFieldsClass();
            streetNameFields.Priority = 1; // Priority 1 indicates the primary street name.
            streetNameFields.StreetNameFieldName = "NAME";

            // Add the StreetNameFields object to a new NetworkSourceDirections object,
            // then add it to the EdgeFeatureSource created earlier.
            INetworkSourceDirections nsDirections = new NetworkSourceDirectionsClass();
            IArray nsdArray = new ArrayClass();
            nsdArray.Add(streetNameFields);
            nsDirections.StreetNameFields = nsdArray;
            edgeNetworkSource.NetworkSourceDirections = nsDirections;

            //
            // Specify the turn source
            //

            deNetworkDataset.SupportsTurns = true;

            // Create a TurnFeatureSource object and point it to the RestrictedTurns feature class.
            INetworkSource turnNetworkSource = new TurnFeatureSourceClass();
            turnNetworkSource.Name = TurnFCName;
            turnNetworkSource.ElementType = esriNetworkElementType.esriNETTurn;

            //
            // Add all sources to the data element
            //

            IArray sourceArray = new ArrayClass();
            sourceArray.Add(edgeNetworkSource);
            sourceArray.Add(turnNetworkSource);

            deNetworkDataset.Sources = sourceArray;

            //
            // Add the traffic data tables (if applicable)
            //

            if (usesHistoricalTraffic || (trafficFeedLocation != null))
            {
                // Create a new TrafficData object and populate its historical and live traffic settings.
                var traffData = new TrafficDataClass() as ITrafficData2;
                traffData.LengthAttributeName = (createNetworkAttributesInMetric ? "Kilometers" : "Miles");

                // Populate the speed profile table settings.
                var histTraff = traffData as IHistoricalTrafficData2;
                histTraff.ProfilesTableName = ProfilesTableName;
                if (usesHistoricalTraffic)
                {
                    if (fgdbVersion == 10.0)
                    {
                        histTraff.FirstTimeSliceFieldName = "TimeFactor_0000";
                        histTraff.LastTimeSliceFieldName = "TimeFactor_2355";
                    }
                    else
                    {
                        histTraff.FirstTimeSliceFieldName = "SpeedFactor_0000";
                        histTraff.LastTimeSliceFieldName = "SpeedFactor_2355";
                    }
                }
                else
                {
                    histTraff.FirstTimeSliceFieldName = "SpeedFactor_AM";
                    histTraff.LastTimeSliceFieldName = "SpeedFactor_PM";
                }
                histTraff.TimeSliceDurationInMinutes = usesHistoricalTraffic ? 5 : 720;
                histTraff.FirstTimeSliceStartTime = new DateTime(1, 1, 1, 0, 0, 0); // 12 AM
                // Note: the last time slice finish time is implied from the above settings and need not be specified.

                // Populate the street-speed profile join table settings.
                histTraff.JoinTableName = HistTrafficJoinTableName;
                if (usesHistoricalTraffic)
                {
                    if (fgdbVersion == 10.0)
                    {
                        histTraff.JoinTableBaseTravelTimeFieldName = "FreeflowMinutes";
                        histTraff.JoinTableBaseTravelTimeUnits = esriNetworkAttributeUnits.esriNAUMinutes;
                    }
                    else
                    {
                        histTraff.JoinTableBaseSpeedFieldName = "SPFREEFLOW";
                        histTraff.JoinTableBaseSpeedUnits = esriNetworkAttributeUnits.esriNAUKilometersPerHour;
                    }
                }
                else
                {
                    histTraff.JoinTableBaseSpeedFieldName = "KPH";
                    histTraff.JoinTableBaseSpeedUnits = esriNetworkAttributeUnits.esriNAUKilometersPerHour;
                }
                IStringArray fieldNames = new NamesClass();
                fieldNames.Add("PROFILE_1");
                fieldNames.Add("PROFILE_2");
                fieldNames.Add("PROFILE_3");
                fieldNames.Add("PROFILE_4");
                fieldNames.Add("PROFILE_5");
                fieldNames.Add("PROFILE_6");
                fieldNames.Add("PROFILE_7");
                histTraff.JoinTableProfileIDFieldNames = fieldNames;

                // If a traffic feed location was provided, populate the dynamic traffic settings.
                if (trafficFeedLocation != null)
                {
                    var dynTraff = traffData as IDynamicTrafficData;
                    dynTraff.DynamicTrafficTableName = TMCJoinTableName;
                    dynTraff.DynamicTrafficTMCFieldName = "TMC";
                    dynTraff.TrafficFeedLocation = trafficFeedLocation;
                }

                // Add the traffic data to the network dataset data element.
                deNetworkDataset.TrafficData = (ITrafficData)traffData;
            }

            //
            // Add network attributes
            //

            IArray attributeArray = new ArrayClass();

            // Initialize variables reused when creating attributes:
            IEvaluatedNetworkAttribute evalNetAttr;
            INetworkAttribute2 netAttr2;
            INetworkFieldEvaluator netFieldEval;
            INetworkConstantEvaluator netConstEval;

            if (usesRSTable)
            {
                // Add the vehicle-specific network attributes

                evalNetAttr = CreateRSRestrictionAttribute("All Vehicles Restricted", "AllVehicles_Restricted",
                                                           true, fgdbVersion, edgeNetworkSource, turnNetworkSource);
                attributeArray.Add(evalNetAttr);

                evalNetAttr = CreateRSRestrictionAttribute("Driving a Passenger Car", "PassengerCars_Restricted",
                                                           true, fgdbVersion, edgeNetworkSource, turnNetworkSource);
                attributeArray.Add(evalNetAttr);

                evalNetAttr = CreateRSRestrictionAttribute("Driving a Residential Vehicle", "ResidentialVehicles_Restricted",
                                                           false, fgdbVersion, edgeNetworkSource, turnNetworkSource);
                attributeArray.Add(evalNetAttr);

                evalNetAttr = CreateRSRestrictionAttribute("Driving a Taxi", "Taxis_Restricted",
                                                           false, fgdbVersion, edgeNetworkSource, turnNetworkSource);
                attributeArray.Add(evalNetAttr);

                evalNetAttr = CreateRSRestrictionAttribute("Driving a Public Bus", "PublicBuses_Restricted",
                                                           false, fgdbVersion, edgeNetworkSource, turnNetworkSource);
                attributeArray.Add(evalNetAttr);
            }
            else
            {
                // Otherwise, add the generic Oneway and RestrictedTurns restriction attributes

                //
                // Oneway network attribute
                //

                // Create an EvaluatedNetworkAttribute object and populate its settings.
                evalNetAttr = CreateRestrAttrNoEvals("Oneway", fgdbVersion, -1, true, "");

                // Create evaluator objects and set them on the EvaluatedNetworkAttribute object.
                netFieldEval = new NetworkFieldEvaluatorClass();
                netFieldEval.SetExpression("restricted", "restricted = False\n\r" +
                                           "Select Case UCase([ONEWAY])\n\r" +
                                           "  Case \"N\", \"TF\", \"T\": restricted = True\n\r" +
                                           "End Select");
                evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAlongDigitized, (INetworkEvaluator)netFieldEval);

                netFieldEval = new NetworkFieldEvaluatorClass();
                netFieldEval.SetExpression("restricted", "restricted = False\n\r" +
                                           "Select Case UCase([ONEWAY])\n\r" +
                                           "  Case \"N\", \"FT\", \"F\": restricted = True\n\r" +
                                           "End Select");
                evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAgainstDigitized, (INetworkEvaluator)netFieldEval);

                netConstEval = new NetworkConstantEvaluatorClass();
                netConstEval.ConstantValue = false; // False = traversable.
                evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETEdge, (INetworkEvaluator)netConstEval);

                netConstEval = new NetworkConstantEvaluatorClass();
                netConstEval.ConstantValue = false; // False = traversable.
                evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETJunction, (INetworkEvaluator)netConstEval);

                netConstEval = new NetworkConstantEvaluatorClass();
                netConstEval.ConstantValue = false; // False = traversable.
                evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETTurn, (INetworkEvaluator)netConstEval);

                // Add the attribute to the array.
                attributeArray.Add(evalNetAttr);

                //
                // RestrictedTurns network attribute
                //

                // Create an EvaluatedNetworkAttribute object and populate its settings.
                evalNetAttr = CreateRestrAttrNoEvals("RestrictedTurns", fgdbVersion, -1, true, "");

                netConstEval = new NetworkConstantEvaluatorClass();
                netConstEval.ConstantValue = true;
                evalNetAttr.set_Evaluator(turnNetworkSource, esriNetworkEdgeDirection.esriNEDNone, (INetworkEvaluator)netConstEval);

                netConstEval = new NetworkConstantEvaluatorClass();
                netConstEval.ConstantValue = false;
                evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETEdge, (INetworkEvaluator)netConstEval);

                netConstEval = new NetworkConstantEvaluatorClass();
                netConstEval.ConstantValue = false;
                evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETJunction, (INetworkEvaluator)netConstEval);

                netConstEval = new NetworkConstantEvaluatorClass();
                netConstEval.ConstantValue = false;
                evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETTurn, (INetworkEvaluator)netConstEval);

                // Add the attribute to the array.
                attributeArray.Add(evalNetAttr);
            }

            //
            // Minutes attribute
            //

            // Create an EvaluatedNetworkAttribute object and populate its settings.
            evalNetAttr = new EvaluatedNetworkAttributeClass();
            netAttr2 = (INetworkAttribute2)evalNetAttr;
            netAttr2.Name = "Minutes";
            netAttr2.UsageType = esriNetworkAttributeUsageType.esriNAUTCost;
            netAttr2.DataType = esriNetworkAttributeDataType.esriNADTDouble;
            netAttr2.Units = esriNetworkAttributeUnits.esriNAUMinutes;
            netAttr2.UseByDefault = !(usesHistoricalTraffic || (trafficFeedLocation != null));

            // Create evaluator objects and set them on the EvaluatedNetworkAttribute object.
            netFieldEval = new NetworkFieldEvaluatorClass();
            netFieldEval.SetExpression("[MINUTES]", "");
            evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAlongDigitized, (INetworkEvaluator)netFieldEval);

            netFieldEval = new NetworkFieldEvaluatorClass();
            netFieldEval.SetExpression("[MINUTES]", "");
            evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAgainstDigitized, (INetworkEvaluator)netFieldEval);

            netConstEval = new NetworkConstantEvaluatorClass();
            netConstEval.ConstantValue = 0;
            evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETEdge, (INetworkEvaluator)netConstEval);

            netConstEval = new NetworkConstantEvaluatorClass();
            netConstEval.ConstantValue = 0;
            evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETJunction, (INetworkEvaluator)netConstEval);

            netConstEval = new NetworkConstantEvaluatorClass();
            netConstEval.ConstantValue = 0;
            evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETTurn, (INetworkEvaluator)netConstEval);

            // Add the attribute to the array.
            attributeArray.Add(evalNetAttr);

            //
            // Length network attribute(s)
            //

            if (createTwoDistanceAttributes || createNetworkAttributesInMetric)
            {
                evalNetAttr = CreateLengthNetworkAttribute("Kilometers", esriNetworkAttributeUnits.esriNAUKilometers,
                                                           "[METERS] / 1000", edgeNetworkSource);
                // Add the attribute to the array.
                attributeArray.Add(evalNetAttr);
            }
            if (createTwoDistanceAttributes || !createNetworkAttributesInMetric)
            {
                evalNetAttr = CreateLengthNetworkAttribute("Miles", esriNetworkAttributeUnits.esriNAUMiles,
                                                           "[METERS] / 1609.344", edgeNetworkSource);
                // Add the attribute to the array.
                attributeArray.Add(evalNetAttr);
            }

            //
            // Avoid-type network attributes
            //

            evalNetAttr = CreateAvoidNetworkAttribute("Avoid Ferries", "[FEATTYP] = 4130",
                                                      false, fgdbVersion, AvoidMediumFactor, edgeNetworkSource);
            attributeArray.Add(evalNetAttr);

            evalNetAttr = CreateAvoidNetworkAttribute("Avoid Limited Access Roads", "[FOW] = 1 OR [FREEWAY] = 1",
                                                      false, fgdbVersion, AvoidMediumFactor, edgeNetworkSource);
            attributeArray.Add(evalNetAttr);

            evalNetAttr = CreateAvoidNetworkAttribute("Avoid Service Roads", "[FOW] = 11",
                                                      true, fgdbVersion, AvoidMediumFactor, edgeNetworkSource);
            attributeArray.Add(evalNetAttr);

            evalNetAttr = CreateAvoidNetworkAttribute("Avoid Pedestrian Zones", "[FOW] = 14",
                                                      true, fgdbVersion, -1, edgeNetworkSource);
            attributeArray.Add(evalNetAttr);

            evalNetAttr = CreateAvoidNetworkAttribute("Avoid Walkways", "[FOW] = 15",
                                                      true, fgdbVersion, -1, edgeNetworkSource);
            attributeArray.Add(evalNetAttr);

            evalNetAttr = CreateAvoidNetworkAttribute("Avoid Roads for Authorities", "[FOW] = 20",
                                                      true, fgdbVersion, -1, edgeNetworkSource);
            attributeArray.Add(evalNetAttr);

            evalNetAttr = CreateAvoidNetworkAttribute("Avoid Back Roads", "[BACKRD] = 1",
                                                      false, fgdbVersion, AvoidMediumFactor, edgeNetworkSource);
            attributeArray.Add(evalNetAttr);

            evalNetAttr = CreateAvoidTollRoadsNetworkAttribute("Avoid Toll Roads", "TOLLRD",
                                                               false, fgdbVersion, AvoidMediumFactor, edgeNetworkSource);
            attributeArray.Add(evalNetAttr);

            evalNetAttr = CreateAvoidNetworkAttribute("Avoid Unpaved Roads", "[RDCOND] = 2",
                                                      false, fgdbVersion, AvoidMediumFactor, edgeNetworkSource);
            attributeArray.Add(evalNetAttr);

            evalNetAttr = CreateAvoidNetworkAttribute("Avoid Roads in Poor Condition", "[RDCOND] = 3",
                                                      false, fgdbVersion, AvoidMediumFactor, edgeNetworkSource);
            attributeArray.Add(evalNetAttr);

            evalNetAttr = CreateAvoidNetworkAttribute("Avoid Private Roads", "[PRIVATERD] > 0",
                                                      true, fgdbVersion, AvoidMediumFactor, edgeNetworkSource);
            attributeArray.Add(evalNetAttr);

            evalNetAttr = CreateDirectionalAvoidNetworkAttribute("Avoid Roads Under Construction", "CONSTATUS",
                                                                 true, fgdbVersion, -1, edgeNetworkSource);
            attributeArray.Add(evalNetAttr);

            evalNetAttr = CreateAvoidNetworkAttribute("Through Traffic Prohibited", "[NTHRUTRAF] = 1",
                                                      (fgdbVersion >= 10.1), fgdbVersion, AvoidHighFactor, edgeNetworkSource);
            attributeArray.Add(evalNetAttr);

            evalNetAttr = CreateAvoidNetworkAttribute("Avoid Four Wheel Drive Only Roads", "[ROUGHRD] = 1",
                                                      false, fgdbVersion, AvoidMediumFactor, edgeNetworkSource);
            attributeArray.Add(evalNetAttr);

            //
            // RoadClass network attribute
            //

            // Create an EvaluatedNetworkAttribute object and populate its settings.
            evalNetAttr = new EvaluatedNetworkAttributeClass();
            netAttr2 = (INetworkAttribute2)evalNetAttr;
            netAttr2.Name = "RoadClass";
            netAttr2.UsageType = esriNetworkAttributeUsageType.esriNAUTDescriptor;
            netAttr2.DataType = esriNetworkAttributeDataType.esriNADTInteger;
            netAttr2.Units = esriNetworkAttributeUnits.esriNAUUnknown;
            netAttr2.UseByDefault = false;

            // Create evaluator objects and set them on the EvaluatedNetworkAttribute object.
            string roadClassExpression = "rc = 1          'Local road\n\r" +
                                         "If [FEATTYP] = 4130 Then\n\r" + 
                                         "  rc = 4          'Ferry\n\r" + 
                                         "Else\n\r" +
                                         "  Select Case [FOW]\n\r" + 
                                         "    Case 1: rc = 2          'Highway\n\r" +
                                         "    Case 10: rc = 3          'Ramp\n\r" +
                                         "    Case 4: rc = 5          'Roundabout\n\r" + 
                                         "  End Select\n\r" + 
                                         "End If";

            netFieldEval = new NetworkFieldEvaluatorClass();
            netFieldEval.SetExpression("rc", roadClassExpression);
            evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAlongDigitized, (INetworkEvaluator)netFieldEval);

            netFieldEval = new NetworkFieldEvaluatorClass();
            netFieldEval.SetExpression("rc", roadClassExpression);
            evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAgainstDigitized, (INetworkEvaluator)netFieldEval);

            netConstEval = new NetworkConstantEvaluatorClass();
            netConstEval.ConstantValue = 0;
            evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETEdge, (INetworkEvaluator)netConstEval);

            netConstEval = new NetworkConstantEvaluatorClass();
            netConstEval.ConstantValue = 0;
            evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETJunction, (INetworkEvaluator)netConstEval);

            netConstEval = new NetworkConstantEvaluatorClass();
            netConstEval.ConstantValue = 0;
            evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETTurn, (INetworkEvaluator)netConstEval);

            // Add the attribute to the array.
            attributeArray.Add(evalNetAttr);

            if (fgdbVersion >= 10.1)
            {
                //
                // ManeuverClass network attribute
                //

                // Create an EvaluatedNetworkAttribute object and populate its settings.
                evalNetAttr = new EvaluatedNetworkAttributeClass();
                netAttr2 = (INetworkAttribute2)evalNetAttr;
                netAttr2.Name = "ManeuverClass";
                netAttr2.UsageType = esriNetworkAttributeUsageType.esriNAUTDescriptor;
                netAttr2.DataType = esriNetworkAttributeDataType.esriNADTInteger;
                netAttr2.Units = esriNetworkAttributeUnits.esriNAUUnknown;
                netAttr2.UseByDefault = false;

                // Create evaluator objects and set them on the EvaluatedNetworkAttribute object.
                string maneuverClassExpression = "mc = 0          'Default\n\r" +
                                                 "Select Case [PJ]\n\r" +
                                                 "  Case 1: mc = 1          'Intersection Internal\n\r" +
                                                 "  Case 3: mc = 2          'Maneuver\n\r" +
                                                 "End Select";

                netFieldEval = new NetworkFieldEvaluatorClass();
                netFieldEval.SetExpression("mc", maneuverClassExpression);
                evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAlongDigitized,
                                          (INetworkEvaluator)netFieldEval);

                netFieldEval = new NetworkFieldEvaluatorClass();
                netFieldEval.SetExpression("mc", maneuverClassExpression);
                evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAgainstDigitized,
                                          (INetworkEvaluator)netFieldEval);

                netConstEval = new NetworkConstantEvaluatorClass();
                netConstEval.ConstantValue = 0;
                evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETEdge,
                                                 (INetworkEvaluator)netConstEval);

                netConstEval = new NetworkConstantEvaluatorClass();
                netConstEval.ConstantValue = 0;
                evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETJunction,
                                                 (INetworkEvaluator)netConstEval);

                netConstEval = new NetworkConstantEvaluatorClass();
                netConstEval.ConstantValue = 0;
                evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETTurn,
                                                 (INetworkEvaluator)netConstEval);

                // Add the attribute to the array.
                attributeArray.Add(evalNetAttr);
            }

            if (usesHistoricalTraffic)
            {
                //
                // WeekdayFallbackTravelTime network attribute
                //

                // Create an EvaluatedNetworkAttribute object and populate its settings.
                evalNetAttr = new EvaluatedNetworkAttributeClass();
                netAttr2 = (INetworkAttribute2)evalNetAttr;
                netAttr2.Name = "WeekdayFallbackTravelTime";
                netAttr2.UsageType = esriNetworkAttributeUsageType.esriNAUTCost;
                netAttr2.DataType = esriNetworkAttributeDataType.esriNADTDouble;
                netAttr2.Units = esriNetworkAttributeUnits.esriNAUMinutes;
                netAttr2.UseByDefault = false;

                // Create evaluator objects and set them on the EvaluatedNetworkAttribute object.
                netFieldEval = new NetworkFieldEvaluatorClass();
                netFieldEval.SetExpression("[FT_WeekdayMinutes]", "");
                evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAlongDigitized, (INetworkEvaluator)netFieldEval);

                netFieldEval = new NetworkFieldEvaluatorClass();
                netFieldEval.SetExpression("[TF_WeekdayMinutes]", "");
                evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAgainstDigitized, (INetworkEvaluator)netFieldEval);

                netConstEval = new NetworkConstantEvaluatorClass();
                netConstEval.ConstantValue = 0;
                evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETEdge, (INetworkEvaluator)netConstEval);

                netConstEval = new NetworkConstantEvaluatorClass();
                netConstEval.ConstantValue = 0;
                evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETJunction, (INetworkEvaluator)netConstEval);

                netConstEval = new NetworkConstantEvaluatorClass();
                netConstEval.ConstantValue = 0;
                evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETTurn, (INetworkEvaluator)netConstEval);

                // Add the attribute to the array.
                attributeArray.Add(evalNetAttr);

                //
                // WeekendFallbackTravelTime network attribute
                //

                // Create an EvaluatedNetworkAttribute object and populate its settings.
                evalNetAttr = new EvaluatedNetworkAttributeClass();
                netAttr2 = (INetworkAttribute2)evalNetAttr;
                netAttr2.Name = "WeekendFallbackTravelTime";
                netAttr2.UsageType = esriNetworkAttributeUsageType.esriNAUTCost;
                netAttr2.DataType = esriNetworkAttributeDataType.esriNADTDouble;
                netAttr2.Units = esriNetworkAttributeUnits.esriNAUMinutes;
                netAttr2.UseByDefault = false;

                // Create evaluator objects and set them on the EvaluatedNetworkAttribute object.
                netFieldEval = new NetworkFieldEvaluatorClass();
                netFieldEval.SetExpression("[FT_WeekendMinutes]", "");
                evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAlongDigitized, (INetworkEvaluator)netFieldEval);

                netFieldEval = new NetworkFieldEvaluatorClass();
                netFieldEval.SetExpression("[TF_WeekendMinutes]", "");
                evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAgainstDigitized, (INetworkEvaluator)netFieldEval);

                netConstEval = new NetworkConstantEvaluatorClass();
                netConstEval.ConstantValue = 0;
                evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETEdge, (INetworkEvaluator)netConstEval);

                netConstEval = new NetworkConstantEvaluatorClass();
                netConstEval.ConstantValue = 0;
                evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETJunction, (INetworkEvaluator)netConstEval);

                netConstEval = new NetworkConstantEvaluatorClass();
                netConstEval.ConstantValue = 0;
                evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETTurn, (INetworkEvaluator)netConstEval);

                // Add the attribute to the array.
                attributeArray.Add(evalNetAttr);

                //
                // AverageTravelTime network attribute
                //

                // Create an EvaluatedNetworkAttribute object and populate its settings.
                evalNetAttr = new EvaluatedNetworkAttributeClass();
                netAttr2 = (INetworkAttribute2)evalNetAttr;
                netAttr2.Name = "AverageTravelTime";
                netAttr2.UsageType = esriNetworkAttributeUsageType.esriNAUTCost;
                netAttr2.DataType = esriNetworkAttributeDataType.esriNADTDouble;
                netAttr2.Units = esriNetworkAttributeUnits.esriNAUMinutes;
                netAttr2.UseByDefault = false;

                // Create evaluator objects and set them on the EvaluatedNetworkAttribute object.
                netFieldEval = new NetworkFieldEvaluatorClass();
                netFieldEval.SetExpression("[FT_AllWeekMinutes]", "");
                evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAlongDigitized, (INetworkEvaluator)netFieldEval);

                netFieldEval = new NetworkFieldEvaluatorClass();
                netFieldEval.SetExpression("[TF_AllWeekMinutes]", "");
                evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAgainstDigitized, (INetworkEvaluator)netFieldEval);

                netConstEval = new NetworkConstantEvaluatorClass();
                netConstEval.ConstantValue = 0;
                evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETEdge, (INetworkEvaluator)netConstEval);

                netConstEval = new NetworkConstantEvaluatorClass();
                netConstEval.ConstantValue = 0;
                evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETJunction, (INetworkEvaluator)netConstEval);

                netConstEval = new NetworkConstantEvaluatorClass();
                netConstEval.ConstantValue = 0;
                evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETTurn, (INetworkEvaluator)netConstEval);

                // Add the attribute to the array.
                attributeArray.Add(evalNetAttr);

                //
                // TravelTime network attribute
                //

                // Create an EvaluatedNetworkAttribute object and populate its settings.
                evalNetAttr = new EvaluatedNetworkAttributeClass();
                netAttr2 = (INetworkAttribute2)evalNetAttr;
                netAttr2.Name = "TravelTime";
                netAttr2.UsageType = esriNetworkAttributeUsageType.esriNAUTCost;
                netAttr2.DataType = esriNetworkAttributeDataType.esriNADTDouble;
                netAttr2.Units = esriNetworkAttributeUnits.esriNAUMinutes;
                netAttr2.UseByDefault = true;

                // Create evaluator objects and set them on the EvaluatedNetworkAttribute object.
                IHistoricalTravelTimeEvaluator histTravelTimeEval = new NetworkEdgeTrafficEvaluatorClass();
                histTravelTimeEval.WeekdayFallbackAttributeName = "WeekdayFallbackTravelTime";
                histTravelTimeEval.WeekendFallbackAttributeName = "WeekendFallbackTravelTime";
                histTravelTimeEval.TimeNeutralAttributeName = "AverageTravelTime";
                evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAlongDigitized, (INetworkEvaluator)histTravelTimeEval);

                histTravelTimeEval = new NetworkEdgeTrafficEvaluatorClass();
                histTravelTimeEval.WeekdayFallbackAttributeName = "WeekdayFallbackTravelTime";
                histTravelTimeEval.WeekendFallbackAttributeName = "WeekendFallbackTravelTime";
                histTravelTimeEval.TimeNeutralAttributeName = "AverageTravelTime";
                evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAgainstDigitized, (INetworkEvaluator)histTravelTimeEval);

                netConstEval = new NetworkConstantEvaluatorClass();
                netConstEval.ConstantValue = 0;
                evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETEdge, (INetworkEvaluator)netConstEval);

                netConstEval = new NetworkConstantEvaluatorClass();
                netConstEval.ConstantValue = 0;
                evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETJunction, (INetworkEvaluator)netConstEval);

                netConstEval = new NetworkConstantEvaluatorClass();
                netConstEval.ConstantValue = 0;
                evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETTurn, (INetworkEvaluator)netConstEval);

                // Add the attribute to the array.
                attributeArray.Add(evalNetAttr);
            }
            else if (trafficFeedLocation != null)
            {
                //
                // TravelTime network attribute
                //

                // Create an EvaluatedNetworkAttribute object and populate its settings.
                evalNetAttr = new EvaluatedNetworkAttributeClass();
                netAttr2 = (INetworkAttribute2)evalNetAttr;
                netAttr2.Name = "TravelTime";
                netAttr2.UsageType = esriNetworkAttributeUsageType.esriNAUTCost;
                netAttr2.DataType = esriNetworkAttributeDataType.esriNADTDouble;
                netAttr2.Units = esriNetworkAttributeUnits.esriNAUMinutes;
                netAttr2.UseByDefault = true;

                // Create evaluator objects and set them on the EvaluatedNetworkAttribute object.
                IHistoricalTravelTimeEvaluator histTravelTimeEval = new NetworkEdgeTrafficEvaluatorClass();
                histTravelTimeEval.WeekdayFallbackAttributeName = "Minutes";
                histTravelTimeEval.WeekendFallbackAttributeName = "Minutes";
                histTravelTimeEval.TimeNeutralAttributeName = "Minutes";
                evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAlongDigitized, (INetworkEvaluator)histTravelTimeEval);

                histTravelTimeEval = new NetworkEdgeTrafficEvaluatorClass();
                histTravelTimeEval.WeekdayFallbackAttributeName = "Minutes";
                histTravelTimeEval.WeekendFallbackAttributeName = "Minutes";
                histTravelTimeEval.TimeNeutralAttributeName = "Minutes";
                evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAgainstDigitized, (INetworkEvaluator)histTravelTimeEval);

                netConstEval = new NetworkConstantEvaluatorClass();
                netConstEval.ConstantValue = 0;
                evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETEdge, (INetworkEvaluator)netConstEval);

                netConstEval = new NetworkConstantEvaluatorClass();
                netConstEval.ConstantValue = 0;
                evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETJunction, (INetworkEvaluator)netConstEval);

                netConstEval = new NetworkConstantEvaluatorClass();
                netConstEval.ConstantValue = 0;
                evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETTurn, (INetworkEvaluator)netConstEval);

                // Add the attribute to the array.
                attributeArray.Add(evalNetAttr);
            }

            //
            // Hierarchy network attribute
            //

            // Create an EvaluatedNetworkAttribute object and populate its settings.
            evalNetAttr = new EvaluatedNetworkAttributeClass();
            netAttr2 = (INetworkAttribute2)evalNetAttr;
            netAttr2.Name = "Hierarchy";
            netAttr2.UsageType = esriNetworkAttributeUsageType.esriNAUTHierarchy;
            netAttr2.DataType = esriNetworkAttributeDataType.esriNADTInteger;
            netAttr2.Units = esriNetworkAttributeUnits.esriNAUUnknown;
            netAttr2.UseByDefault = true;

            // Create evaluator objects and set them on the EvaluatedNetworkAttribute object.
            string hierarchyExpression = "h = [NET2CLASS] + 1\n\r" +
                                         "If h > 5 Then h = 5";

            netFieldEval = new NetworkFieldEvaluatorClass();
            netFieldEval.SetExpression("h", hierarchyExpression);
            evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAlongDigitized, (INetworkEvaluator)netFieldEval);

            netFieldEval = new NetworkFieldEvaluatorClass();
            netFieldEval.SetExpression("h", hierarchyExpression);
            evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAgainstDigitized, (INetworkEvaluator)netFieldEval);

            netConstEval = new NetworkConstantEvaluatorClass();
            netConstEval.ConstantValue = 0;
            evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETEdge, (INetworkEvaluator)netConstEval);

            netConstEval = new NetworkConstantEvaluatorClass();
            netConstEval.ConstantValue = 0;
            evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETJunction, (INetworkEvaluator)netConstEval);

            netConstEval = new NetworkConstantEvaluatorClass();
            netConstEval.ConstantValue = 0;
            evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETTurn, (INetworkEvaluator)netConstEval);

            // Add the attribute to the array.
            attributeArray.Add(evalNetAttr);

            // Since this is the hierarchy attribute, also set it as the hierarchy cluster attribute.
            deNetworkDataset.HierarchyClusterAttribute = (INetworkAttribute)evalNetAttr;

            // Specify the ranges for the hierarchy levels.
            deNetworkDataset.HierarchyLevelCount = 3;
            deNetworkDataset.set_MaxValueForHierarchy(1, 2); // level 1: up to 2
            deNetworkDataset.set_MaxValueForHierarchy(2, 4); // level 2: 3 - 4
            deNetworkDataset.set_MaxValueForHierarchy(3, 5); // level 3: 5 and higher (the values of h only go up to 5)

            //
            // Create and specify the time zone attribute (if applicable)
            //

            if (timeZoneIDBaseFieldName != "" || commonTimeZone != "")
            {
                string timeZoneAttrName = timeZoneIDBaseFieldName;
                if (timeZoneIDBaseFieldName == "")
                {
                    timeZoneAttrName = "TimeZoneID";

                    // Create the time zone table with the common time zone
                    TimeZoneUtilities.CreateTimeZoneTable(outputFileGdbPath, commonTimeZone);
                }

                //
                // TimeZoneID network attribute
                //

                // Create an EvaluatedNetworkAttribute object and populate its settings.
                evalNetAttr = new EvaluatedNetworkAttributeClass();
                netAttr2 = (INetworkAttribute2)evalNetAttr;
                netAttr2.Name = timeZoneAttrName;
                netAttr2.UsageType = esriNetworkAttributeUsageType.esriNAUTDescriptor;
                netAttr2.DataType = esriNetworkAttributeDataType.esriNADTInteger;
                netAttr2.Units = esriNetworkAttributeUnits.esriNAUUnknown;
                netAttr2.UseByDefault = false;

                // Create evaluator objects and set them on the EvaluatedNetworkAttribute object.
                if (timeZoneIDBaseFieldName == "")
                {
                    netConstEval = new NetworkConstantEvaluatorClass();
                    netConstEval.ConstantValue = 1;
                    evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAlongDigitized, (INetworkEvaluator)netConstEval);

                    netConstEval = new NetworkConstantEvaluatorClass();
                    netConstEval.ConstantValue = 1;
                    evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAgainstDigitized, (INetworkEvaluator)netConstEval);
                }
                else if (directedTimeZoneIDFields)
                {
                    netFieldEval = new NetworkFieldEvaluatorClass();
                    netFieldEval.SetExpression("[FT_" + timeZoneIDBaseFieldName + "]", "");
                    evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAlongDigitized, (INetworkEvaluator)netFieldEval);

                    netFieldEval = new NetworkFieldEvaluatorClass();
                    netFieldEval.SetExpression("[TF_" + timeZoneIDBaseFieldName + "]", "");
                    evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAgainstDigitized, (INetworkEvaluator)netFieldEval);
                }
                else
                {
                    netFieldEval = new NetworkFieldEvaluatorClass();
                    netFieldEval.SetExpression("[" + timeZoneIDBaseFieldName + "]", "");
                    evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAlongDigitized, (INetworkEvaluator)netFieldEval);

                    netFieldEval = new NetworkFieldEvaluatorClass();
                    netFieldEval.SetExpression("[" + timeZoneIDBaseFieldName + "]", "");
                    evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAgainstDigitized, (INetworkEvaluator)netFieldEval);
                }

                netConstEval = new NetworkConstantEvaluatorClass();
                netConstEval.ConstantValue = 0;
                evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETEdge, (INetworkEvaluator)netConstEval);

                netConstEval = new NetworkConstantEvaluatorClass();
                netConstEval.ConstantValue = 0;
                evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETJunction, (INetworkEvaluator)netConstEval);

                netConstEval = new NetworkConstantEvaluatorClass();
                netConstEval.ConstantValue = 0;
                evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETTurn, (INetworkEvaluator)netConstEval);

                // Set this as the time zone attribute, and specify the time zone table.
                deNetworkDataset.TimeZoneAttributeName = timeZoneAttrName;
                deNetworkDataset.TimeZoneTableName = "TimeZones";

                // Add the attribute to the array.
                attributeArray.Add(evalNetAttr);
            }

            #region Add Logistics Truck Route and Logistics Restrictions network attributes

            //
            // Add Logistics Truck Route network attributes
            //

            if (usesLTRTable)
            {
                if (fgdbVersion >= 10.1)
                {
                    evalNetAttr = CreateLTRRestrictionAttribute("Prefer National Route Access Roads", PreferLowFactor,
                                                                "NationalRouteAccess", fgdbVersion, edgeNetworkSource);
                    attributeArray.Add(evalNetAttr);

                    //
                    // Prefer National (STAA) Route Roads network attribute
                    //

                    // Create an EvaluatedNetworkAttribute object and populate its settings.
                    evalNetAttr = CreateRestrAttrNoEvals("Prefer National (STAA) Route Roads", fgdbVersion, PreferMediumFactor, false, "");

                    // Create evaluator objects and set them on the EvaluatedNetworkAttribute object.
                    netFieldEval = new NetworkFieldEvaluatorClass();
                    netFieldEval.SetExpression("[NationalSTAARoute] = \"Y\"", "");
                    evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAlongDigitized, (INetworkEvaluator)netFieldEval);

                    netFieldEval = new NetworkFieldEvaluatorClass();
                    netFieldEval.SetExpression("[NationalSTAARoute] = \"Y\"", "");
                    evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAgainstDigitized, (INetworkEvaluator)netFieldEval);

                    netConstEval = new NetworkConstantEvaluatorClass();
                    netConstEval.ConstantValue = false; // False = traversable.
                    evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETEdge, (INetworkEvaluator)netConstEval);

                    netConstEval = new NetworkConstantEvaluatorClass();
                    netConstEval.ConstantValue = false; // False = traversable.
                    evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETJunction, (INetworkEvaluator)netConstEval);

                    netConstEval = new NetworkConstantEvaluatorClass();
                    netConstEval.ConstantValue = false; // False = traversable.
                    evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETTurn, (INetworkEvaluator)netConstEval);

                    // Add the attribute to the array.
                    attributeArray.Add(evalNetAttr);

                    //
                    // Prefer National (STAA) Route and Locally Designated Truck Route Roads network attribute
                    //

                    // Create an EvaluatedNetworkAttribute object and populate its settings.
                    evalNetAttr = CreateRestrAttrNoEvals("Prefer National (STAA) Route and Locally Designated Truck Route Roads",
                                                         fgdbVersion, PreferMediumFactor, false, "");

                    // Create evaluator objects and set them on the EvaluatedNetworkAttribute object.
                    netFieldEval = new NetworkFieldEvaluatorClass();
                    netFieldEval.SetExpression("[NationalSTAARoute] = \"Y\" Or [DesignatedTruckRoute] = \"Y\"", "");
                    evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAlongDigitized, (INetworkEvaluator)netFieldEval);

                    netFieldEval = new NetworkFieldEvaluatorClass();
                    netFieldEval.SetExpression("[NationalSTAARoute] = \"Y\" Or [DesignatedTruckRoute] = \"Y\"", "");
                    evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAgainstDigitized, (INetworkEvaluator)netFieldEval);

                    netConstEval = new NetworkConstantEvaluatorClass();
                    netConstEval.ConstantValue = false; // False = traversable.
                    evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETEdge, (INetworkEvaluator)netConstEval);

                    netConstEval = new NetworkConstantEvaluatorClass();
                    netConstEval.ConstantValue = false; // False = traversable.
                    evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETJunction, (INetworkEvaluator)netConstEval);

                    netConstEval = new NetworkConstantEvaluatorClass();
                    netConstEval.ConstantValue = false; // False = traversable.
                    evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETTurn, (INetworkEvaluator)netConstEval);

                    // Add the attribute to the array.
                    attributeArray.Add(evalNetAttr);

                    //
                    // Prefer National (STAA) Route and Locally Designated Truck Route and Bypass Roads network attribute
                    //

                    // Create an EvaluatedNetworkAttribute object and populate its settings.
                    evalNetAttr = CreateRestrAttrNoEvals("Prefer National (STAA) Route and Locally Designated Truck Route and Bypass Roads",
                                                         fgdbVersion, PreferMediumFactor, false, "");

                    // Create evaluator objects and set them on the EvaluatedNetworkAttribute object.
                    netFieldEval = new NetworkFieldEvaluatorClass();
                    netFieldEval.SetExpression("[NationalSTAARoute] = \"Y\" Or [DesignatedTruckRoute] = \"Y\" Or [TruckBypassRoad] = \"Y\"", "");
                    evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAlongDigitized, (INetworkEvaluator)netFieldEval);

                    netFieldEval = new NetworkFieldEvaluatorClass();
                    netFieldEval.SetExpression("[NationalSTAARoute] = \"Y\" Or [DesignatedTruckRoute] = \"Y\" Or [TruckBypassRoad] = \"Y\"", "");
                    evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAgainstDigitized, (INetworkEvaluator)netFieldEval);

                    netConstEval = new NetworkConstantEvaluatorClass();
                    netConstEval.ConstantValue = false; // False = traversable.
                    evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETEdge, (INetworkEvaluator)netConstEval);

                    netConstEval = new NetworkConstantEvaluatorClass();
                    netConstEval.ConstantValue = false; // False = traversable.
                    evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETJunction, (INetworkEvaluator)netConstEval);

                    netConstEval = new NetworkConstantEvaluatorClass();
                    netConstEval.ConstantValue = false; // False = traversable.
                    evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETTurn, (INetworkEvaluator)netConstEval);

                    // Add the attribute to the array.
                    attributeArray.Add(evalNetAttr);
                }

                evalNetAttr = CreateLTRRestrictionAttribute("Prohibit No Commercial Vehicles Roads", -1,
                                                            "NoCommercialVehicles", fgdbVersion, edgeNetworkSource);
                attributeArray.Add(evalNetAttr);

                evalNetAttr = CreateLTRRestrictionAttribute("Avoid Immediate Access Only Roads", AvoidHighFactor,
                                                            "ImmediateAccessOnly", fgdbVersion, edgeNetworkSource);
                attributeArray.Add(evalNetAttr);

                evalNetAttr = CreateLTRRestrictionAttribute("Avoid Trucks Restricted Roads", AvoidMediumFactor,
                                                            "TrucksRestricted", fgdbVersion, edgeNetworkSource);
                attributeArray.Add(evalNetAttr);
            }

            //
            // Add Logistics Restrictions network attributes
            //

            if (usesLRSTable)
            {
                // Open the lrsStatsTable and find the fields we need
                ITable lrsStatsTable = featureWorkspace.OpenTable(lrsStatsTableName);
                int restrTypField = lrsStatsTable.FindField("RESTRTYP");
                int vtField = lrsStatsTable.FindField("VT");
                int restrValField = lrsStatsTable.FindField("RESTRVAL");

                // Loop through the lrsStatsTable
                ICursor cur = lrsStatsTable.Search(null, true);
                IRow statsTableRow = null;
                while ((statsTableRow = cur.NextRow()) != null)
                {
                    // Get the RESTRTYP, VT, and RESTRVAL field values
                    string restrTyp = (string)(statsTableRow.get_Value(restrTypField));
                    short vt = (short)(statsTableRow.get_Value(vtField));
                    short restrVal = (short)(statsTableRow.get_Value(restrValField));

                    // Determine the restriction usage factor
                    double restrictionUsageFactor;
                    switch (restrVal)
                    {
                        case 1:
                            restrictionUsageFactor = -1;
                            break;
                        case 2:
                            restrictionUsageFactor = AvoidHighFactor;
                            break;
                        case 3:
                            restrictionUsageFactor = AvoidMediumFactor;
                            break;
                        case 4:
                            restrictionUsageFactor = PreferMediumFactor;
                            break;
                        default:
                            restrictionUsageFactor = 1.0;
                            break;
                    }

                    // Create new network attribute(s) for this restriction
                    if (restrTyp == null || restrTyp.Trim().Length == 0)
                        continue;
                    switch (restrTyp.Remove(1))
                    {
                        case "!":
                            // Determine attribute and parameter names to be used
                            string limitAttrName = MakeLogisticsLimitAttributeName(restrTyp, vt, restrVal, createNetworkAttributesInMetric);
                            string restrAttrName = MakeLogisticsRestrictionAttributeName(restrTyp, vt, restrVal);
                            string paramName = MakeLogisticsAttributeParameterName(restrTyp, createNetworkAttributesInMetric);

                            //
                            // A "dimensional" limit attribute
                            //

                            // Create an EvaluatedNetworkAttribute object and populate its settings.
                            evalNetAttr = new EvaluatedNetworkAttributeClass();
                            netAttr2 = (INetworkAttribute2)evalNetAttr;
                            netAttr2.Name = limitAttrName;
                            netAttr2.UsageType = esriNetworkAttributeUsageType.esriNAUTDescriptor;
                            netAttr2.DataType = esriNetworkAttributeDataType.esriNADTDouble;
                            netAttr2.Units = esriNetworkAttributeUnits.esriNAUUnknown;
                            netAttr2.UseByDefault = false;

                            string conversionExpr = "";
                            if (createNetworkAttributesInMetric)
                            {
                                switch (restrTyp)
                                {
                                    case "!A":
                                    case "!B":
                                    case "!C":
                                    case "!D":
                                    case "!E":
                                    case "!F":
                                        // convert tons to metric tons
                                        conversionExpr = " * 0.90718474";
                                        break;
                                    default:
                                        // convert feet to meters
                                        conversionExpr = " * 0.3048";
                                        break;
                                }
                            }

                            // Create evaluator objects and set them on the EvaluatedNetworkAttribute object.
                            netFieldEval = new NetworkFieldEvaluatorClass();
                            netFieldEval.SetExpression("[" + MakeLogisticsFieldName(restrTyp, vt, restrVal) + "]" + conversionExpr, "");
                            evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAlongDigitized, (INetworkEvaluator)netFieldEval);

                            netFieldEval = new NetworkFieldEvaluatorClass();
                            netFieldEval.SetExpression("[" + MakeLogisticsFieldName(restrTyp, vt, restrVal) + "]" + conversionExpr, "");
                            evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAgainstDigitized, (INetworkEvaluator)netFieldEval);

                            netConstEval = new NetworkConstantEvaluatorClass();
                            netConstEval.ConstantValue = 0;
                            evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETEdge, (INetworkEvaluator)netConstEval);

                            netConstEval = new NetworkConstantEvaluatorClass();
                            netConstEval.ConstantValue = 0;
                            evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETJunction, (INetworkEvaluator)netConstEval);

                            netConstEval = new NetworkConstantEvaluatorClass();
                            netConstEval.ConstantValue = 0;
                            evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETTurn, (INetworkEvaluator)netConstEval);

                            // Add the attribute to the array.
                            attributeArray.Add(evalNetAttr);

                            //
                            // A "dimensional" restriction attribute
                            //

                            // Create an EvaluatedNetworkAttribute object and populate its settings.
                            evalNetAttr = CreateRestrAttrNoEvals(restrAttrName, fgdbVersion, restrictionUsageFactor, false, paramName);

                            // Create evaluator objects and set them on the EvaluatedNetworkAttribute object.
                            INetworkFunctionEvaluator netFuncEval = new NetworkFunctionEvaluatorClass();
                            netFuncEval.FirstArgument = limitAttrName;
                            netFuncEval.Operator = "<";
                            netFuncEval.SecondArgument = paramName;
                            evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAlongDigitized, (INetworkEvaluator)netFuncEval);

                            netFuncEval = new NetworkFunctionEvaluatorClass();
                            netFuncEval.FirstArgument = limitAttrName;
                            netFuncEval.Operator = "<";
                            netFuncEval.SecondArgument = paramName;
                            evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAgainstDigitized, (INetworkEvaluator)netFuncEval);

                            netConstEval = new NetworkConstantEvaluatorClass();
                            netConstEval.ConstantValue = 0;
                            evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETEdge, (INetworkEvaluator)netConstEval);

                            netConstEval = new NetworkConstantEvaluatorClass();
                            netConstEval.ConstantValue = 0;
                            evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETJunction, (INetworkEvaluator)netConstEval);

                            netConstEval = new NetworkConstantEvaluatorClass();
                            netConstEval.ConstantValue = 0;
                            evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETTurn, (INetworkEvaluator)netConstEval);

                            // Add the attribute to the array.
                            attributeArray.Add(evalNetAttr);
                            break;
                        case "@":
                            //
                            // A "load" restriction attribute
                            //

                            // Create an EvaluatedNetworkAttribute object and populate its settings.
                            evalNetAttr = CreateRestrAttrNoEvals(MakeLogisticsRestrictionAttributeName(restrTyp, vt, restrVal),
                                                                 fgdbVersion, restrictionUsageFactor, false, "");

                            // Create evaluator objects and set them on the EvaluatedNetworkAttribute object.
                            netFieldEval = new NetworkFieldEvaluatorClass();
                            netFieldEval.SetExpression("[" + MakeLogisticsFieldName(restrTyp, vt, restrVal) + "] = \"Y\"", "");
                            evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAlongDigitized, (INetworkEvaluator)netFieldEval);

                            netFieldEval = new NetworkFieldEvaluatorClass();
                            netFieldEval.SetExpression("[" + MakeLogisticsFieldName(restrTyp, vt, restrVal) + "] = \"Y\"", "");
                            evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAgainstDigitized, (INetworkEvaluator)netFieldEval);

                            netConstEval = new NetworkConstantEvaluatorClass();
                            netConstEval.ConstantValue = 0;
                            evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETEdge, (INetworkEvaluator)netConstEval);

                            netConstEval = new NetworkConstantEvaluatorClass();
                            netConstEval.ConstantValue = 0;
                            evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETJunction, (INetworkEvaluator)netConstEval);

                            netConstEval = new NetworkConstantEvaluatorClass();
                            netConstEval.ConstantValue = 0;
                            evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETTurn, (INetworkEvaluator)netConstEval);

                            // Add the attribute to the array.
                            attributeArray.Add(evalNetAttr);
                            break;
                        default:
                            continue;    // Only process dimensional (!_) and load (@_) restrictions
                    }
                }

                //
                // TruckTravelTime attribute
                //

                // Create an EvaluatedNetworkAttribute object and populate its settings.
                evalNetAttr = new EvaluatedNetworkAttributeClass();
                netAttr2 = (INetworkAttribute2)evalNetAttr;
                netAttr2.Name = "TruckTravelTime";
                netAttr2.UsageType = esriNetworkAttributeUsageType.esriNAUTCost;
                netAttr2.DataType = esriNetworkAttributeDataType.esriNADTDouble;
                netAttr2.Units = esriNetworkAttributeUnits.esriNAUMinutes;
                netAttr2.UseByDefault = false;

                string truckSpeedExpression = "sp = [RecommendedSpeed_KPH_AllTrucks]\n" +
                                              "If IsNull(sp) Then sp = [MaximumSpeed_KPH_AllTrucks]\n" +
                                              "If IsNull(sp) Then sp = [KPH]\n" +
                                              "If sp > [KPH] Then sp = [KPH]";

                // Create evaluator objects and set them on the EvaluatedNetworkAttribute object.
                netFieldEval = new NetworkFieldEvaluatorClass();
                netFieldEval.SetExpression("[METERS] * 0.06 / sp", truckSpeedExpression);
                evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAlongDigitized, (INetworkEvaluator)netFieldEval);

                netFieldEval = new NetworkFieldEvaluatorClass();
                netFieldEval.SetExpression("[METERS] * 0.06 / sp", truckSpeedExpression);
                evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAgainstDigitized, (INetworkEvaluator)netFieldEval);

                netConstEval = new NetworkConstantEvaluatorClass();
                netConstEval.ConstantValue = 0;
                evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETEdge, (INetworkEvaluator)netConstEval);

                netConstEval = new NetworkConstantEvaluatorClass();
                netConstEval.ConstantValue = 0;
                evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETJunction, (INetworkEvaluator)netConstEval);

                netConstEval = new NetworkConstantEvaluatorClass();
                netConstEval.ConstantValue = 0;
                evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETTurn, (INetworkEvaluator)netConstEval);

                // Add the attribute to the array.
                attributeArray.Add(evalNetAttr);
            }
            #endregion

            //
            // Add all attributes to the data element
            //

            deNetworkDataset.Attributes = attributeArray;

            if (fgdbVersion >= 10.1)
            {
                //
                // Add travel modes
                //

                IArray travelModeArray = new ArrayClass();

                // Initialize variables reused when creating travel modes:
                INetworkTravelMode travelMode;
                string timeAttributeName;
                string distanceAttributeName;
                IStringArray restrictionsArray;
                IArray paramValuesArray;
                INetworkTravelModeParameterValue tmParamValue;

                //
                // Driving Time travel mode
                //

                // Create a NetworkTravelMode object and populate its settings.
                travelMode = new NetworkTravelModeClass();
                travelMode.Name = "Driving Time";
                timeAttributeName = (usesHistoricalTraffic ? "TravelTime" : "Minutes");
                distanceAttributeName = (createNetworkAttributesInMetric ? "Kilometers" : "Miles");
                travelMode.ImpedanceAttributeName = timeAttributeName;
                travelMode.TimeAttributeName = timeAttributeName;
                travelMode.DistanceAttributeName = distanceAttributeName;
                travelMode.UseHierarchy = true;
                travelMode.RestrictUTurns = esriNetworkForwardStarBacktrack.esriNFSBAtDeadEndsAndIntersections;
                travelMode.OutputGeometryPrecision = 10;
                travelMode.OutputGeometryPrecisionUnits = esriUnits.esriMeters;

                // Populate the restriction attributes to use.
                restrictionsArray = new StrArrayClass();
                if (usesRSTable)
                {
                    restrictionsArray.Add("All Vehicles Restricted");
                    restrictionsArray.Add("Driving a Passenger Car");
                }
                else
                {
                    restrictionsArray.Add("Oneway");
                    restrictionsArray.Add("RestrictedTurns");
                }
                restrictionsArray.Add("Avoid Service Roads");
                restrictionsArray.Add("Avoid Pedestrian Zones");
                restrictionsArray.Add("Avoid Walkways");
                restrictionsArray.Add("Avoid Roads for Authorities");
                restrictionsArray.Add("Avoid Private Roads");
                restrictionsArray.Add("Avoid Roads Under Construction");
                restrictionsArray.Add("Through Traffic Prohibited");
                travelMode.RestrictionAttributeNames = restrictionsArray;

                // Add the travel mode to the array.
                travelModeArray.Add(travelMode);

                //
                // Driving Distance travel mode
                //

                // Create a NetworkTravelMode object and populate its settings.
                travelMode = new NetworkTravelModeClass();
                travelMode.Name = "Driving Distance";
                timeAttributeName = (usesHistoricalTraffic ? "TravelTime" : "Minutes");
                distanceAttributeName = (createNetworkAttributesInMetric ? "Kilometers" : "Miles");
                travelMode.ImpedanceAttributeName = distanceAttributeName;
                travelMode.TimeAttributeName = timeAttributeName;
                travelMode.DistanceAttributeName = distanceAttributeName;
                travelMode.UseHierarchy = true;
                travelMode.RestrictUTurns = esriNetworkForwardStarBacktrack.esriNFSBAtDeadEndsAndIntersections;
                travelMode.OutputGeometryPrecision = 10;
                travelMode.OutputGeometryPrecisionUnits = esriUnits.esriMeters;

                // Populate the restriction attributes to use.
                restrictionsArray = new StrArrayClass();
                if (usesRSTable)
                {
                    restrictionsArray.Add("All Vehicles Restricted");
                    restrictionsArray.Add("Driving a Passenger Car");
                }
                else
                {
                    restrictionsArray.Add("Oneway");
                    restrictionsArray.Add("RestrictedTurns");
                }
                restrictionsArray.Add("Avoid Service Roads");
                restrictionsArray.Add("Avoid Pedestrian Zones");
                restrictionsArray.Add("Avoid Walkways");
                restrictionsArray.Add("Avoid Roads for Authorities");
                restrictionsArray.Add("Avoid Private Roads");
                restrictionsArray.Add("Avoid Roads Under Construction");
                restrictionsArray.Add("Through Traffic Prohibited");

                travelMode.RestrictionAttributeNames = restrictionsArray;

                // Add the travel mode to the array.
                travelModeArray.Add(travelMode);

                if (usesLTRTable)
                {
                    //
                    // Trucking Time travel mode
                    //

                    // Create a NetworkTravelMode object and populate its settings.
                    travelMode = new NetworkTravelModeClass();
                    travelMode.Name = "Trucking Time";
                    timeAttributeName = "TruckTravelTime";
                    distanceAttributeName = (createNetworkAttributesInMetric ? "Kilometers" : "Miles");
                    travelMode.ImpedanceAttributeName = timeAttributeName;
                    travelMode.TimeAttributeName = timeAttributeName;
                    travelMode.DistanceAttributeName = distanceAttributeName;
                    travelMode.UseHierarchy = true;
                    travelMode.RestrictUTurns = esriNetworkForwardStarBacktrack.esriNFSBNoBacktrack;
                    travelMode.OutputGeometryPrecision = 10;
                    travelMode.OutputGeometryPrecisionUnits = esriUnits.esriMeters;

                    // Populate attribute parameter values to use.
                    paramValuesArray = new ArrayClass();
                    tmParamValue = new NetworkTravelModeParameterValueClass();
                    tmParamValue.AttributeName = "Prefer National (STAA) Route and Locally Designated Truck Route and Bypass Roads";
                    tmParamValue.ParameterName = "Restriction Usage";
                    tmParamValue.Value = PreferHighFactor;
                    paramValuesArray.Add(tmParamValue);

                    // Populate the restriction attributes to use.
                    restrictionsArray = new StrArrayClass();
                    if (usesRSTable)
                    {
                        restrictionsArray.Add("All Vehicles Restricted");
                    }
                    else
                    {
                        restrictionsArray.Add("Oneway");
                        restrictionsArray.Add("RestrictedTurns");
                    }
                    restrictionsArray.Add("Avoid Service Roads");
                    restrictionsArray.Add("Avoid Pedestrian Zones");
                    restrictionsArray.Add("Avoid Walkways");
                    restrictionsArray.Add("Avoid Roads for Authorities");
                    restrictionsArray.Add("Avoid Private Roads");
                    restrictionsArray.Add("Avoid Roads Under Construction");
                    restrictionsArray.Add("Through Traffic Prohibited");
                    restrictionsArray.Add("Prefer National (STAA) Route and Locally Designated Truck Route and Bypass Roads");
                    restrictionsArray.Add("Prohibit No Commercial Vehicles Roads");
                    restrictionsArray.Add("Avoid Immediate Access Only Roads");
                    restrictionsArray.Add("Avoid Trucks Restricted Roads");

                    travelMode.RestrictionAttributeNames = restrictionsArray;
                    travelMode.AttributeParameterValues = paramValuesArray;

                    // Add the travel mode to the array.
                    travelModeArray.Add(travelMode);

                    //
                    // Trucking Distance travel mode
                    //

                    // Create a NetworkTravelMode object and populate its settings.
                    travelMode = new NetworkTravelModeClass();
                    travelMode.Name = "Trucking Distance";
                    timeAttributeName = "TruckTravelTime";
                    distanceAttributeName = (createNetworkAttributesInMetric ? "Kilometers" : "Miles");
                    travelMode.ImpedanceAttributeName = distanceAttributeName;
                    travelMode.TimeAttributeName = timeAttributeName;
                    travelMode.DistanceAttributeName = distanceAttributeName;
                    travelMode.UseHierarchy = true;
                    travelMode.RestrictUTurns = esriNetworkForwardStarBacktrack.esriNFSBNoBacktrack;
                    travelMode.OutputGeometryPrecision = 10;
                    travelMode.OutputGeometryPrecisionUnits = esriUnits.esriMeters;

                    // Populate attribute parameter values to use.
                    paramValuesArray = new ArrayClass();
                    tmParamValue = new NetworkTravelModeParameterValueClass();
                    tmParamValue.AttributeName = "Prefer National (STAA) Route and Locally Designated Truck Route and Bypass Roads";
                    tmParamValue.ParameterName = "Restriction Usage";
                    tmParamValue.Value = PreferHighFactor;
                    paramValuesArray.Add(tmParamValue);

                    // Populate the restriction attributes to use.
                    restrictionsArray = new StrArrayClass();
                    if (usesRSTable)
                    {
                        restrictionsArray.Add("All Vehicles Restricted");
                    }
                    else
                    {
                        restrictionsArray.Add("Oneway");
                        restrictionsArray.Add("RestrictedTurns");
                    }
                    restrictionsArray.Add("Avoid Service Roads");
                    restrictionsArray.Add("Avoid Pedestrian Zones");
                    restrictionsArray.Add("Avoid Walkways");
                    restrictionsArray.Add("Avoid Roads for Authorities");
                    restrictionsArray.Add("Avoid Private Roads");
                    restrictionsArray.Add("Avoid Roads Under Construction");
                    restrictionsArray.Add("Through Traffic Prohibited");
                    restrictionsArray.Add("Prefer National (STAA) Route and Locally Designated Truck Route and Bypass Roads");
                    restrictionsArray.Add("Prohibit No Commercial Vehicles Roads");
                    restrictionsArray.Add("Avoid Immediate Access Only Roads");
                    restrictionsArray.Add("Avoid Trucks Restricted Roads");

                    travelMode.RestrictionAttributeNames = restrictionsArray;
                    travelMode.AttributeParameterValues = paramValuesArray;

                    // Add the travel mode to the array.
                    travelModeArray.Add(travelMode);
                }

                //
                // Add all travel modes to the data element
                //

                deNetworkDataset.TravelModes = travelModeArray;
            }

            //
            // Specify directions settings
            //

            // Create a NetworkDirections object and populate its settings.
            INetworkDirections networkDirections = new NetworkDirectionsClass();
            networkDirections.DefaultOutputLengthUnits = createNetworkAttributesInMetric ? esriNetworkAttributeUnits.esriNAUKilometers : esriNetworkAttributeUnits.esriNAUMiles;
            networkDirections.LengthAttributeName = createNetworkAttributesInMetric ? "Kilometers" : "Miles";
            networkDirections.TimeAttributeName = "Minutes";
            networkDirections.RoadClassAttributeName = "RoadClass";
            var netDirSignposts = (ISignposts)networkDirections;
            netDirSignposts.SignpostFeatureClassName = SignpostFCName;
            netDirSignposts.SignpostStreetsTableName = SignpostJoinTableName;

            if (fgdbVersion >= 10.1)
            {
                // Specify the RoadSplits table.
                var netDirRoadSplits = (IRoadSplits)networkDirections;
                netDirRoadSplits.RoadSplitsTableName = RoadSplitsTableName;

                // Create a DirectionsAttributeMapping object for the ManeuverClass mapping.
                IDirectionsAttributeMapping dirAttrMapping = new DirectionsAttributeMappingClass();
                dirAttrMapping.KeyName = "ManeuverClass";
                dirAttrMapping.AttributeName = "ManeuverClass";

                // Wrap the DirectionsAttributeMapping object in an Array and add it to the NetworkDirections object.
                IArray damArray = new ArrayClass();
                damArray.Add(dirAttrMapping);
                var networkDirections2 = (INetworkDirections2)networkDirections;
                networkDirections2.AttributeMappings = damArray;
            }

            // Add the NetworkDirections object to the network dataset data element.
            deNetworkDataset.Directions = networkDirections;

            //
            // Create and build the network dataset
            //

            // Get the feature dataset extension and create the network dataset based on the data element.
            var fdxContainer = (IFeatureDatasetExtensionContainer)featureDataset;
            IFeatureDatasetExtension fdExtension = fdxContainer.FindExtension(esriDatasetType.esriDTNetworkDataset);
            var datasetContainer2 = (IDatasetContainer2)fdExtension;
            var deDataset = (IDEDataset)deNetworkDataset;
            var networkDataset = (INetworkDataset)datasetContainer2.CreateDataset(deDataset);

            // Once the network dataset is created, build it.
            var networkBuild = (INetworkBuild)networkDataset;
            networkBuild.BuildNetwork(geoDataset.Extent);

            return;
        }

        private IEvaluatedNetworkAttribute CreateLengthNetworkAttribute(string attrName, esriNetworkAttributeUnits attrUnits,
                                                                        string fieldEvalExpression, INetworkSource edgeNetworkSource)
        {
            // Create an EvaluatedNetworkAttribute object and populate its settings.
            IEvaluatedNetworkAttribute evalNetAttr = new EvaluatedNetworkAttributeClass();
            INetworkAttribute2 netAttr2 = (INetworkAttribute2)evalNetAttr;
            netAttr2.Name = attrName;
            netAttr2.UsageType = esriNetworkAttributeUsageType.esriNAUTCost;
            netAttr2.DataType = esriNetworkAttributeDataType.esriNADTDouble;
            netAttr2.Units = attrUnits;
            netAttr2.UseByDefault = false;

            // Create evaluator objects and set them on the EvaluatedNetworkAttribute object.
            INetworkFieldEvaluator netFieldEval = new NetworkFieldEvaluatorClass();
            netFieldEval.SetExpression(fieldEvalExpression, "");
            evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAlongDigitized, (INetworkEvaluator)netFieldEval);

            netFieldEval = new NetworkFieldEvaluatorClass();
            netFieldEval.SetExpression(fieldEvalExpression, "");
            evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAgainstDigitized, (INetworkEvaluator)netFieldEval);

            INetworkConstantEvaluator netConstEval = new NetworkConstantEvaluatorClass();
            netConstEval.ConstantValue = false;
            evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETEdge, (INetworkEvaluator)netConstEval);

            netConstEval = new NetworkConstantEvaluatorClass();
            netConstEval.ConstantValue = false;
            evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETJunction, (INetworkEvaluator)netConstEval);

            netConstEval = new NetworkConstantEvaluatorClass();
            netConstEval.ConstantValue = false;
            evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETTurn, (INetworkEvaluator)netConstEval);

            return evalNetAttr;
        }

        private IEvaluatedNetworkAttribute CreateRSRestrictionAttribute(string attrName, string fieldNameBase,
                                                                        bool useByDefault, double fgdbVersion,
                                                                        INetworkSource edgeNetworkSource, INetworkSource turnNetworkSource)
        {
            // Create an EvaluatedNetworkAttribute object and populate its settings.
            IEvaluatedNetworkAttribute evalNetAttr = CreateRestrAttrNoEvals(attrName, fgdbVersion, -1, useByDefault, "");

            // Create evaluator objects and set them on the EvaluatedNetworkAttribute object.
            INetworkFieldEvaluator netFieldEval = new NetworkFieldEvaluatorClass();
            netFieldEval.SetExpression("[" + fieldNameBase + "] = \"Y\"", "");
            evalNetAttr.set_Evaluator(turnNetworkSource, esriNetworkEdgeDirection.esriNEDNone, (INetworkEvaluator)netFieldEval);

            netFieldEval = new NetworkFieldEvaluatorClass();
            netFieldEval.SetExpression("[FT_" + fieldNameBase + "] = \"Y\"", "");
            evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAlongDigitized, (INetworkEvaluator)netFieldEval);

            netFieldEval = new NetworkFieldEvaluatorClass();
            netFieldEval.SetExpression("[TF_" + fieldNameBase + "] = \"Y\"", "");
            evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAgainstDigitized, (INetworkEvaluator)netFieldEval);

            INetworkConstantEvaluator netConstEval = new NetworkConstantEvaluatorClass();
            netConstEval.ConstantValue = 0;
            evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETEdge, (INetworkEvaluator)netConstEval);

            netConstEval = new NetworkConstantEvaluatorClass();
            netConstEval.ConstantValue = 0;
            evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETJunction, (INetworkEvaluator)netConstEval);

            netConstEval = new NetworkConstantEvaluatorClass();
            netConstEval.ConstantValue = 0;
            evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETTurn, (INetworkEvaluator)netConstEval);

            return evalNetAttr;
        }

        private IEvaluatedNetworkAttribute CreateAvoidNetworkAttribute(string attrName, string fieldEvalExpression,
                                                                       bool useByDefault, double fgdbVersion,
                                                                       double restrUsageFactor, INetworkSource edgeNetworkSource)
        {
            // Create an EvaluatedNetworkAttribute object and populate its settings.
            IEvaluatedNetworkAttribute evalNetAttr = CreateRestrAttrNoEvals(attrName, fgdbVersion, restrUsageFactor, useByDefault, "");

            // Create evaluator objects and set them on the EvaluatedNetworkAttribute object.
            INetworkFieldEvaluator netFieldEval = new NetworkFieldEvaluatorClass();
            netFieldEval.SetExpression(fieldEvalExpression, "");
            evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAlongDigitized, (INetworkEvaluator)netFieldEval);

            netFieldEval = new NetworkFieldEvaluatorClass();
            netFieldEval.SetExpression(fieldEvalExpression, "");
            evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAgainstDigitized, (INetworkEvaluator)netFieldEval);

            INetworkConstantEvaluator netConstEval = new NetworkConstantEvaluatorClass();
            netConstEval.ConstantValue = false;
            evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETEdge, (INetworkEvaluator)netConstEval);

            netConstEval = new NetworkConstantEvaluatorClass();
            netConstEval.ConstantValue = false;
            evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETJunction, (INetworkEvaluator)netConstEval);

            netConstEval = new NetworkConstantEvaluatorClass();
            netConstEval.ConstantValue = false;
            evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETTurn, (INetworkEvaluator)netConstEval);

            return evalNetAttr;
        }

        private IEvaluatedNetworkAttribute CreateAvoidTollRoadsNetworkAttribute(string attrName, string fieldName,
                                                                                bool useByDefault, double fgdbVersion,
                                                                                double restrUsageFactor, INetworkSource edgeNetworkSource)
        {
            // Create an EvaluatedNetworkAttribute object and populate its settings.
            IEvaluatedNetworkAttribute evalNetAttr = CreateRestrAttrNoEvals(attrName, fgdbVersion, restrUsageFactor, useByDefault, "");

            // Create evaluator objects and set them on the EvaluatedNetworkAttribute object.
            INetworkFieldEvaluator netFieldEval = new NetworkFieldEvaluatorClass();
            netFieldEval.SetExpression("restricted", "restricted = False\n\r" +
                                       "Select Case UCase([" + fieldName + "])\n\r" +
                                       "  Case 11, 12, 21, 22: restricted = True\n\r" +
                                       "End Select");
            evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAlongDigitized, (INetworkEvaluator)netFieldEval);

            netFieldEval = new NetworkFieldEvaluatorClass();
            netFieldEval.SetExpression("restricted", "restricted = False\n\r" +
                                       "Select Case UCase([" + fieldName + "])\n\r" +
                                       "  Case 11, 13, 21, 23: restricted = True\n\r" +
                                       "End Select");
            evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAgainstDigitized, (INetworkEvaluator)netFieldEval);

            INetworkConstantEvaluator netConstEval = new NetworkConstantEvaluatorClass();
            netConstEval.ConstantValue = false;
            evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETEdge, (INetworkEvaluator)netConstEval);

            netConstEval = new NetworkConstantEvaluatorClass();
            netConstEval.ConstantValue = false;
            evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETJunction, (INetworkEvaluator)netConstEval);

            netConstEval = new NetworkConstantEvaluatorClass();
            netConstEval.ConstantValue = false;
            evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETTurn, (INetworkEvaluator)netConstEval);

            return evalNetAttr;
        }

        private IEvaluatedNetworkAttribute CreateDirectionalAvoidNetworkAttribute(string attrName, string fieldName,
                                                                                  bool useByDefault, double fgdbVersion,
                                                                                  double restrUsageFactor, INetworkSource edgeNetworkSource)
        {
            // Create an EvaluatedNetworkAttribute object and populate its settings.
            IEvaluatedNetworkAttribute evalNetAttr = CreateRestrAttrNoEvals(attrName, fgdbVersion, restrUsageFactor, useByDefault, "");

            // Create evaluator objects and set them on the EvaluatedNetworkAttribute object.
            INetworkFieldEvaluator netFieldEval = new NetworkFieldEvaluatorClass();
            netFieldEval.SetExpression("restricted", "restricted = False\n\r" +
                                       "Select Case UCase([" + fieldName + "])\n\r" +
                                       "  Case \"B\", \"FT\": restricted = True\n\r" +
                                       "End Select");
            evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAlongDigitized, (INetworkEvaluator)netFieldEval);

            netFieldEval = new NetworkFieldEvaluatorClass();
            netFieldEval.SetExpression("restricted", "restricted = False\n\r" +
                                       "Select Case UCase([" + fieldName + "])\n\r" +
                                       "  Case \"B\", \"TF\": restricted = True\n\r" +
                                       "End Select");
            evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAgainstDigitized, (INetworkEvaluator)netFieldEval);

            INetworkConstantEvaluator netConstEval = new NetworkConstantEvaluatorClass();
            netConstEval.ConstantValue = false;
            evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETEdge, (INetworkEvaluator)netConstEval);

            netConstEval = new NetworkConstantEvaluatorClass();
            netConstEval.ConstantValue = false;
            evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETJunction, (INetworkEvaluator)netConstEval);

            netConstEval = new NetworkConstantEvaluatorClass();
            netConstEval.ConstantValue = false;
            evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETTurn, (INetworkEvaluator)netConstEval);

            return evalNetAttr;
        }

        private IEvaluatedNetworkAttribute CreateLTRRestrictionAttribute(string attrName, double restrictionUsageFactor, string fieldName,
                                                                         double fgdbVersion, INetworkSource edgeNetworkSource)
        {
            // Create an EvaluatedNetworkAttribute object and populate its settings.
            IEvaluatedNetworkAttribute evalNetAttr = CreateRestrAttrNoEvals(attrName, fgdbVersion, restrictionUsageFactor, false, "");

            // Create evaluator objects and set them on the EvaluatedNetworkAttribute object.
            INetworkFieldEvaluator netFieldEval = new NetworkFieldEvaluatorClass();
            netFieldEval.SetExpression("[" + fieldName + "] = \"Y\"", "");
            evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAlongDigitized, (INetworkEvaluator)netFieldEval);

            netFieldEval = new NetworkFieldEvaluatorClass();
            netFieldEval.SetExpression("[" + fieldName + "] = \"Y\"", "");
            evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAgainstDigitized, (INetworkEvaluator)netFieldEval);

            INetworkConstantEvaluator netConstEval = new NetworkConstantEvaluatorClass();
            netConstEval.ConstantValue = 0;
            evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETEdge, (INetworkEvaluator)netConstEval);

            netConstEval = new NetworkConstantEvaluatorClass();
            netConstEval.ConstantValue = 0;
            evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETJunction, (INetworkEvaluator)netConstEval);

            netConstEval = new NetworkConstantEvaluatorClass();
            netConstEval.ConstantValue = 0;
            evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETTurn, (INetworkEvaluator)netConstEval);

            return evalNetAttr;
        }

        private IEvaluatedNetworkAttribute CreateRestrAttrNoEvals(string attrName, double fgdbVersion, double restrUsageFactor,
                                                                  bool useByDefault, string dimensionalParamName)
        {
            // Create an EvaluatedNetworkAttribute object and populate its settings.
            IEvaluatedNetworkAttribute evalNetAttr = new EvaluatedNetworkAttributeClass();
            INetworkAttribute2 netAttr2 = (INetworkAttribute2)evalNetAttr;
            netAttr2.Name = attrName;
            netAttr2.UsageType = esriNetworkAttributeUsageType.esriNAUTRestriction;
            netAttr2.DataType = esriNetworkAttributeDataType.esriNADTBoolean;
            netAttr2.Units = esriNetworkAttributeUnits.esriNAUUnknown;
            netAttr2.UseByDefault = useByDefault;

            if (fgdbVersion >= 10.1 || dimensionalParamName != "")
            {
                INetworkAttributeParameter2 netAttrParam = null;
                IArray paramArray = new ArrayClass();

                // Create a parameter to hold the restriction usage factor (10.1 and later only)
                if (fgdbVersion >= 10.1)
                {
                    netAttrParam = new NetworkAttributeParameterClass();
                    netAttrParam.Name = "Restriction Usage";
                    netAttrParam.VarType = (int)(VarEnum.VT_R8);
                    netAttrParam.DefaultValue = restrUsageFactor;
                    netAttrParam.ParameterUsageType = esriNetworkAttributeParameterUsageType.esriNAPUTRestriction;
                    paramArray.Add(netAttrParam);
                }

                // Create a parameter to hold the vehicle's dimensional measurement (if provided)
                if (dimensionalParamName != "")
                {
                    netAttrParam = new NetworkAttributeParameterClass();
                    netAttrParam.Name = dimensionalParamName;
                    netAttrParam.VarType = (int)(VarEnum.VT_R8);
                    netAttrParam.DefaultValue = 0.0;
                    netAttrParam.ParameterUsageType = esriNetworkAttributeParameterUsageType.esriNAPUTGeneral;
                    paramArray.Add(netAttrParam);
                }

                // Add the parameter(s) to the network attribute.
                netAttr2.Parameters = paramArray;
            }

            return evalNetAttr;
        }
    }
}
