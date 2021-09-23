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

    [Guid("A1753DCB-9D7A-4cff-B885-87347A864710")]
    [ClassInterface(ClassInterfaceType.None)]
    [ProgId("GPProcessVendorDataFunctions.ProcessNavStreetsDataFunction")]

    class ProcessNavStreetsDataFunction : IGPFunction2
    {
        #region Constants
        // names
        private const string DefaultFdsName = "Routing";
        private const string DefaultNdsName = "Routing_ND";
        private const string StreetsFCName = "Streets";
        private const string ProfilesTableName = "Patterns";
        private const string HistTrafficJoinTableName = "Streets_Patterns";
        private const string TMCJoinTableName = "Streets_TMC";
        private const string TurnFCName = "RestrictedTurns";
        private const string RoadSplitsTableName = "Streets_RoadSplits";
        private const string SignpostFCName = "Signposts";
        private const string SignpostJoinTableName = "Signposts_Streets";

        // restriction usage factor constants
        private const double PreferHighFactor = 0.2;
        private const double PreferMediumFactor = 0.5;
        private const double PreferLowFactor = 0.8;
        private const double AvoidLowFactor = 1.3;
        private const double AvoidMediumFactor = 2.0;
        private const double AvoidHighFactor = 5.0;

        // parameter index constants
        private const int InputStreetsFeatureClass = 0;
        private const int InputAltStreetsFeatureClass = 1;
        private const int InputZLevelsFeatureClass = 2;
        private const int InputCdmsTable = 3;
        private const int InputRdmsTable = 4;
        private const int InputSignsTable = 5;
        private const int OutputFileGDB = 6;
        private const int OutputFileGDBVersion = 7;
        private const int InputFeatureDatasetName = 8;
        private const int InputNetworkDatasetName = 9;
        private const int InputCreateNetworkAttributesInMetric = 10;
        private const int InputCreateArcGISOnlineNetworkAttributes = 11;
        private const int InputTimeZoneIDBaseFieldName = 12;
        private const int InputTimeZoneTable = 13;
        private const int InputCommonTimeZoneForTheEntireDataset = 14;
        private const int InputTrafficTable = 15;
        private const int InputLinkReferenceFileFC14 = 16;
        private const int InputLinkReferenceFileFC5 = 17;
        private const int InputTMCReferenceFile = 18;
        private const int InputSPDFile = 19;
        private const int InputLiveTrafficFeedFolder = 20;
        private const int InputLiveTrafficFeedArcGISServerConnection = 21;
        private const int InputLiveTrafficFeedGeoprocessingServiceName = 22;
        private const int InputLiveTrafficFeedGeoprocessingTaskName = 23;
        private const int InputUSCndModTable = 24;
        private const int InputNonUSCndModTable = 25;
        private const int OutputNetworkDataset = 26;

        // field names and types
        private static readonly string[] StreetsFieldNames = new string[]
                                        { "LINK_ID", "ST_NAME", "ST_LANGCD", "ST_NM_PREF", "ST_TYP_BEF",
                                          "ST_NM_BASE", "ST_NM_SUFF", "ST_TYP_AFT", "FUNC_CLASS", "SPEED_CAT",
                                          "DIR_TRAVEL", "AR_AUTO", "AR_BUS", "AR_TAXIS", "AR_CARPOOL", "AR_PEDEST",
                                          "AR_TRUCKS", "AR_TRAFF", "AR_DELIV", "AR_EMERVEH", "AR_MOTOR", "PAVED",
                                          "RAMP", "TOLLWAY", "CONTRACC", "ROUNDABOUT", "INTERINTER", "FERRY_TYPE",
                                          "SPECTRFIG", "MANOEUVRE", "DIRONSIGN", "EXPR_LANE", "CARPOOLRD", "PUB_ACCESS" };
        private static readonly esriFieldType[] StreetsFieldTypes = new esriFieldType[]
                                        { esriFieldType.esriFieldTypeInteger,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString };
        private static readonly string[] AltStreetsFieldNames = new string[]
                                        { "LINK_ID", "ST_NAME", "ST_LANGCD", "ST_NM_PREF", "ST_TYP_BEF",
                                          "ST_NM_BASE", "ST_NM_SUFF", "ST_TYP_AFT", "DIRONSIGN", "EXPLICATBL" };
        private static readonly esriFieldType[] AltStreetsFieldTypes = new esriFieldType[]
                                        { esriFieldType.esriFieldTypeInteger,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString };
        private static readonly string[] ZLevelsFieldNames = new string[]
                                        { "LINK_ID", "POINT_NUM", "NODE_ID", "Z_LEVEL",
                                          "INTRSECT", "DOT_SHAPE", "ALIGNED" };
        private static readonly esriFieldType[] ZLevelsFieldTypes = new esriFieldType[]
                                        { esriFieldType.esriFieldTypeInteger,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeInteger,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeString };
        private static readonly string[] CdmsFieldNames = new string[]
                                        { "LINK_ID", "COND_ID", "COND_TYPE", "END_OF_LK",
                                          "AR_AUTO", "AR_BUS", "AR_TAXIS", "AR_CARPOOL", "AR_PEDSTRN",
                                          "AR_TRUCKS", "AR_THRUTR", "AR_DELIVER", "AR_EMERVEH", "AR_MOTOR" };
        private static readonly esriFieldType[] CdmsFieldTypes = new esriFieldType[]
                                        { esriFieldType.esriFieldTypeInteger,
                                          esriFieldType.esriFieldTypeInteger,
                                          esriFieldType.esriFieldTypeInteger,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString };
        private static readonly string[] RdmsFieldNames = new string[] 
                                        { "LINK_ID", "COND_ID", "MAN_LINKID", "SEQ_NUMBER" };
        private static readonly esriFieldType[] RdmsFieldTypes = new esriFieldType[]
                                        { esriFieldType.esriFieldTypeInteger,
                                          esriFieldType.esriFieldTypeInteger,
                                          esriFieldType.esriFieldTypeInteger,
                                          esriFieldType.esriFieldTypeSmallInteger };
        private static readonly string[] SignsFieldNames = new string[]
                                        { "SRC_LINKID", "DST_LINKID", "SIGN_ID", "SEQ_NUM", "EXIT_NUM",
                                          "BR_RTEID", "BR_RTEDIR", "SIGN_TXTTP", "SIGN_TEXT", "LANG_CODE",
                                          "TOW_RTEID", "SIGN_TXTTP" };
        private static readonly esriFieldType[] SignsFieldTypes = new esriFieldType[]
                                        { esriFieldType.esriFieldTypeInteger,
                                          esriFieldType.esriFieldTypeInteger,
                                          esriFieldType.esriFieldTypeInteger,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString };
        private static readonly string[] TimeZoneFieldNames = new string[]
                                        { "MSTIMEZONE" };
        private static readonly esriFieldType[] TimeZoneFieldTypes = new esriFieldType[]
                                        { esriFieldType.esriFieldTypeString };
        private static readonly string[] TrafficFieldNames = new string[]
                                        { "LINK_ID", "TRAFFIC_CD"  };
        private static readonly esriFieldType[] TrafficFieldTypes = new esriFieldType[]
                                        { esriFieldType.esriFieldTypeInteger,
                                          esriFieldType.esriFieldTypeString };
        private static readonly string[] CndModFieldNames = new string[]
                                        { "COND_ID", "LANG_CODE", "MOD_TYPE", "MOD_VAL" };
        private static readonly esriFieldType[] CndModFieldTypes = new esriFieldType[]
                                        { esriFieldType.esriFieldTypeInteger,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeInteger,
                                          esriFieldType.esriFieldTypeString };
        #endregion

        private IArray m_parameters;
        private IGPUtilities m_gpUtils;

        public ProcessNavStreetsDataFunction()
        {
            // Create the GPUtilities Object
            m_gpUtils = new GPUtilitiesClass();
        }

        #region IGPFunction2 Members

        public IArray ParameterInfo
        {
            // create and return the parameters for this function:
            //  0 - Input Streets Feature Class
            //  1 - Input AltStreets Feature Class
            //  2 - Input Z-Levels Feature Class
            //  3 - Input Condition/Driving Manoeuvres (CDMS) Table
            //  4 - Input Restricted Driving Manoeuvres (RDMS) Table
            //  5 - Input Signs Table
            //  6 - Output File Geodatabase
            //  7 - Output File Geodatabase Version
            //  8 - Feature Dataset Name
            //  9 - Network Dataset Name
            // 10 - Create Network Attributes in Metric (optional)
            // 11 - Create ArcGIS Online Network Attributes (optional)
            // 12 - Input Time Zone ID Base Field Name (optional)
            // 13 - Input Time Zone Table (optional)
            // 14 - Input Common Time Zone for the Entire Dataset (optional)
            // 15 - Input Traffic Table (optional)
            // 16 - Input Traffic Patterns Link Reference File covering Functional Classes 1-4 (optional)
            // 17 - Input Traffic Patterns Link Reference File covering Functional Class 5 (optional)
            // 18 - Input Traffic Patterns TMC Reference File (optional)
            // 19 - Input Traffic Patterns Speed Patterns Dictionary (SPD) File (optional)
            // 20 - Input Live Traffic Feed Folder (optional)
            // 21 - Input Live Traffic Feed ArcGIS Server Connection (optional)
            // 22 - Input Live Traffic Feed Geoprocessing Service Name (optional)
            // 23 - Input Live Traffic Feed Geoprocessing Task Name (optional)
            // 24 - Input US Condition Modifiers Table (optional)
            // 25 - Input Non-US Condition Modifiers Table (optional)
            // 26 - Output Network Dataset (derived parameter)

            get
            {
                IArray paramArray = new ArrayClass();

                // 0 - input_streets_feature_class

                IGPParameterEdit paramEdit = new GPParameterClass();
                paramEdit.DataType = new DEFeatureClassTypeClass() as IGPDataType;
                paramEdit.Value = new DEFeatureClass() as IGPValue;
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Input Streets Feature Class";
                paramEdit.DisplayOrder = InputStreetsFeatureClass;
                paramEdit.Enabled = true;
                paramEdit.Name = "input_streets_feature_class";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeRequired;

                IGPFeatureClassDomain lineFeatureClassDomain = new GPFeatureClassDomainClass();
                lineFeatureClassDomain.AddType(esriGeometryType.esriGeometryLine);
                lineFeatureClassDomain.AddType(esriGeometryType.esriGeometryPolyline);
                paramEdit.Domain = lineFeatureClassDomain as IGPDomain;

                paramArray.Add(paramEdit as object);

                // 1 - input_altstreets_feature_class

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new DEFeatureClassTypeClass() as IGPDataType;
                paramEdit.Value = new DEFeatureClass() as IGPValue;
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Input AltStreets Feature Class";
                paramEdit.DisplayOrder = InputAltStreetsFeatureClass;
                paramEdit.Enabled = true;
                paramEdit.Name = "input_altstreets_feature_class";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeRequired;

                lineFeatureClassDomain = new GPFeatureClassDomainClass();
                lineFeatureClassDomain.AddType(esriGeometryType.esriGeometryLine);
                lineFeatureClassDomain.AddType(esriGeometryType.esriGeometryPolyline);
                paramEdit.Domain = lineFeatureClassDomain as IGPDomain;

                paramArray.Add(paramEdit as object);

                // 2 - input_zlevels_feature_class

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new DEFeatureClassTypeClass() as IGPDataType;
                paramEdit.Value = new DEFeatureClass() as IGPValue;
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Input Z-Levels Feature Class";
                paramEdit.DisplayOrder = InputZLevelsFeatureClass;
                paramEdit.Enabled = true;
                paramEdit.Name = "input_zlevels_feature_class";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeRequired;

                IGPFeatureClassDomain pointFeatureClassDomain = new GPFeatureClassDomainClass();
                pointFeatureClassDomain.AddType(esriGeometryType.esriGeometryPoint);
                paramEdit.Domain = pointFeatureClassDomain as IGPDomain;

                paramArray.Add(paramEdit as object);

                // 3 - input_cdms_table

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new DETableTypeClass() as IGPDataType;
                paramEdit.Value = new DETableClass() as IGPValue;
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Input Condition/Driving Manoeuvres (CDMS) Table";
                paramEdit.DisplayOrder = InputCdmsTable;
                paramEdit.Enabled = true;
                paramEdit.Name = "input_cdms_table";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeRequired;

                paramArray.Add(paramEdit as object);

                // 4 - input_rdms_table

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new DETableTypeClass() as IGPDataType;
                paramEdit.Value = new DETableClass() as IGPValue;
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Input Restricted Driving Manoeuvres (RDMS) Table";
                paramEdit.DisplayOrder = InputRdmsTable;
                paramEdit.Enabled = true;
                paramEdit.Name = "input_rdms_table";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeRequired;

                paramArray.Add(paramEdit as object);

                // 5 - input_signs_table

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new DETableTypeClass() as IGPDataType;
                paramEdit.Value = new DETableClass() as IGPValue;
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Input Signs Table";
                paramEdit.DisplayOrder = InputSignsTable;
                paramEdit.Enabled = true;
                paramEdit.Name = "input_signs_table";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeRequired;

                paramArray.Add(paramEdit as object);

                // 6 - output_file_geodatabase

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

                // 7 - output_file_geodatabase_version

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

                // 8 - feature_dataset_name

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

                // 9 - network_dataset_name

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

                // 10 - input_create_network_attributes_in_metric

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

                // 11 - input_create_arcgis_online_network_attributes

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new GPBooleanTypeClass() as IGPDataType;
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Create ArcGIS Online Network Attributes";
                paramEdit.DisplayOrder = InputCreateArcGISOnlineNetworkAttributes;
                paramEdit.Enabled = true;
                paramEdit.Name = "input_create_arcgis_online_network_attributes";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeOptional;
                gpBool = new GPBooleanClass();
                gpBool.Value = false;
                paramEdit.Value = gpBool as IGPValue;

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

                // 15 - input_traffic_table

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new DETableTypeClass() as IGPDataType;
                paramEdit.Value = new DETableClass() as IGPValue;
                paramEdit.Category = "Historical and Live Traffic";
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Input Traffic Table";
                paramEdit.DisplayOrder = InputTrafficTable;
                paramEdit.Enabled = true;
                paramEdit.Name = "input_traffic_table";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeOptional;

                paramArray.Add(paramEdit as object);

                // 16 - input_link_reference_file_fc1_4

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new DEFileTypeClass() as IGPDataType;
                paramEdit.Value = new DEFileClass() as IGPValue;
                paramEdit.Category = "Historical and Live Traffic";
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Input Link Reference File covering Functional Classes 1-4";
                paramEdit.DisplayOrder = InputLinkReferenceFileFC14;
                paramEdit.Enabled = true;
                paramEdit.Name = "input_link_reference_file_fc1_4";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeOptional;

                paramArray.Add(paramEdit as object);

                // 17 - input_link_reference_file_fc5

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new DEFileTypeClass() as IGPDataType;
                paramEdit.Value = new DEFileClass() as IGPValue;
                paramEdit.Category = "Historical and Live Traffic";
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Input Link Reference File covering Functional Class 5";
                paramEdit.DisplayOrder = InputLinkReferenceFileFC5;
                paramEdit.Enabled = true;
                paramEdit.Name = "input_link_reference_file_fc5";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeOptional;

                paramArray.Add(paramEdit as object);

                // 18 - input_tmc_reference_file

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new DEFileTypeClass() as IGPDataType;
                paramEdit.Value = new DEFileClass() as IGPValue;
                paramEdit.Category = "Historical and Live Traffic";
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Input TMC Reference File";
                paramEdit.DisplayOrder = InputTMCReferenceFile;
                paramEdit.Enabled = true;
                paramEdit.Name = "input_tmc_reference_file";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeOptional;

                paramArray.Add(paramEdit as object);

                // 19 - input_spd_file

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new DEFileTypeClass() as IGPDataType;
                paramEdit.Value = new DEFileClass() as IGPValue;
                paramEdit.Category = "Historical and Live Traffic";
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Input Speed Patterns Dictionary (SPD) File";
                paramEdit.DisplayOrder = InputSPDFile;
                paramEdit.Enabled = true;
                paramEdit.Name = "input_spd_file";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeOptional;

                paramArray.Add(paramEdit as object);

                // 20 - input_live_traffic_feed_folder

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new GPStringTypeClass() as IGPDataType;
                paramEdit.Value = new GPStringClass() as IGPValue;
                paramEdit.Category = "Historical and Live Traffic";
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Input Live Traffic Feed Folder";
                paramEdit.DisplayOrder = InputLiveTrafficFeedFolder;
                paramEdit.Enabled = true;
                paramEdit.Name = "input_live_traffic_feed_folder";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeOptional;

                paramArray.Add(paramEdit as object);

                // 21 - input_live_traffic_feed_arcgis_server_connection

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new DEServerConnectionTypeClass() as IGPDataType;
                paramEdit.Value = new DEServerConnectionClass() as IGPValue;
                paramEdit.Category = "Historical and Live Traffic";
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Input Live Traffic Feed ArcGIS Server Connection";
                paramEdit.DisplayOrder = InputLiveTrafficFeedArcGISServerConnection;
                paramEdit.Enabled = true;
                paramEdit.Name = "input_live_traffic_feed_arcgis_server_connection";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeOptional;

                paramArray.Add(paramEdit as object);

                // 22 - input_live_traffic_feed_geoprocessing_service_name

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new GPStringTypeClass() as IGPDataType;
                paramEdit.Value = new GPStringClass() as IGPValue;
                paramEdit.Category = "Historical and Live Traffic";
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Input Live Traffic Feed Geoprocessing Service Name";
                paramEdit.DisplayOrder = InputLiveTrafficFeedGeoprocessingServiceName;
                paramEdit.Enabled = true;
                paramEdit.Name = "input_live_traffic_feed_geoprocessing_service_name";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeOptional;

                paramArray.Add(paramEdit as object);

                // 23 - input_live_traffic_feed_geoprocessing_task_name

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new GPStringTypeClass() as IGPDataType;
                paramEdit.Value = new GPStringClass() as IGPValue;
                paramEdit.Category = "Historical and Live Traffic";
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Input Live Traffic Feed Geoprocessing Task Name";
                paramEdit.DisplayOrder = InputLiveTrafficFeedGeoprocessingTaskName;
                paramEdit.Enabled = true;
                paramEdit.Name = "input_live_traffic_feed_geoprocessing_task_name";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeOptional;

                paramArray.Add(paramEdit as object);

                // 24 - input_us_cndmod_table

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new DETableTypeClass() as IGPDataType;
                paramEdit.Value = new DETableClass() as IGPValue;
                paramEdit.Category = "Transport Condition Modifiers";
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Input US Condition Modifier (CndMod) Table";
                paramEdit.DisplayOrder = InputUSCndModTable;
                paramEdit.Enabled = true;
                paramEdit.Name = "input_us_cndmod_table";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeOptional;

                paramArray.Add(paramEdit as object);

                // 25 - input_non_us_cndmod_table

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new DETableTypeClass() as IGPDataType;
                paramEdit.Value = new DETableClass() as IGPValue;
                paramEdit.Category = "Transport Condition Modifiers";
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Input Non-US Condition Modifier (CndMod) Table";
                paramEdit.DisplayOrder = InputNonUSCndModTable;
                paramEdit.Enabled = true;
                paramEdit.Name = "input_non_us_cndmod_table";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeOptional;

                paramArray.Add(paramEdit as object);

                // 26 - output_network_dataset

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
                        // Disable ArcGIS Online Network Attributes, time zones, historical traffic, and live traffic
                        DisableParameter(m_parameters.get_Element(InputCreateArcGISOnlineNetworkAttributes) as IGPParameterEdit,
                                         new GPBooleanClass() as IGPValue);
                        DisableParameter(m_parameters.get_Element(InputTimeZoneIDBaseFieldName) as IGPParameterEdit,
                                         new GPStringClass() as IGPValue);
                        DisableParameter(m_parameters.get_Element(InputTimeZoneTable) as IGPParameterEdit,
                                         new DETableClass() as IGPValue);
                        DisableParameter(m_parameters.get_Element(InputCommonTimeZoneForTheEntireDataset) as IGPParameterEdit,
                                         new GPStringClass() as IGPValue);
                        DisableParameter(m_parameters.get_Element(InputTrafficTable) as IGPParameterEdit,
                                         new DETableClass() as IGPValue);
                        DisableParameter(m_parameters.get_Element(InputLinkReferenceFileFC14) as IGPParameterEdit,
                                         new DEFileClass() as IGPValue);
                        DisableParameter(m_parameters.get_Element(InputLinkReferenceFileFC5) as IGPParameterEdit,
                                         new DEFileClass() as IGPValue);
                        DisableParameter(m_parameters.get_Element(InputTMCReferenceFile) as IGPParameterEdit,
                                         new DEFileClass() as IGPValue);
                        DisableParameter(m_parameters.get_Element(InputSPDFile) as IGPParameterEdit,
                                         new DEFileClass() as IGPValue);
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
                        // Enable time zones and historical traffic; Disable live traffic and ArcGIS Online Network Attributes
                        EnableParameter(m_parameters.get_Element(InputTimeZoneIDBaseFieldName) as IGPParameterEdit);
                        EnableParameter(m_parameters.get_Element(InputTimeZoneTable) as IGPParameterEdit);
                        EnableParameter(m_parameters.get_Element(InputCommonTimeZoneForTheEntireDataset) as IGPParameterEdit);
                        EnableParameter(m_parameters.get_Element(InputTrafficTable) as IGPParameterEdit);
                        EnableParameter(m_parameters.get_Element(InputLinkReferenceFileFC14) as IGPParameterEdit);
                        EnableParameter(m_parameters.get_Element(InputLinkReferenceFileFC5) as IGPParameterEdit);
                        EnableParameter(m_parameters.get_Element(InputTMCReferenceFile) as IGPParameterEdit);
                        EnableParameter(m_parameters.get_Element(InputSPDFile) as IGPParameterEdit);
                        DisableParameter(m_parameters.get_Element(InputLiveTrafficFeedFolder) as IGPParameterEdit,
                                         new GPStringClass() as IGPValue);
                        DisableParameter(m_parameters.get_Element(InputLiveTrafficFeedArcGISServerConnection) as IGPParameterEdit,
                                         new DEServerConnectionClass() as IGPValue);
                        DisableParameter(m_parameters.get_Element(InputLiveTrafficFeedGeoprocessingServiceName) as IGPParameterEdit,
                                         new GPStringClass() as IGPValue);
                        DisableParameter(m_parameters.get_Element(InputLiveTrafficFeedGeoprocessingTaskName) as IGPParameterEdit,
                                         new GPStringClass() as IGPValue);
                        DisableParameter(m_parameters.get_Element(InputCreateArcGISOnlineNetworkAttributes) as IGPParameterEdit,
                                         new GPBooleanClass() as IGPValue);
                        break;

                    default:
                        // Enable all parameters
                        EnableParameter(m_parameters.get_Element(InputCreateArcGISOnlineNetworkAttributes) as IGPParameterEdit);
                        EnableParameter(m_parameters.get_Element(InputTimeZoneIDBaseFieldName) as IGPParameterEdit);
                        EnableParameter(m_parameters.get_Element(InputTimeZoneTable) as IGPParameterEdit);
                        EnableParameter(m_parameters.get_Element(InputCommonTimeZoneForTheEntireDataset) as IGPParameterEdit);
                        EnableParameter(m_parameters.get_Element(InputTrafficTable) as IGPParameterEdit);
                        EnableParameter(m_parameters.get_Element(InputLinkReferenceFileFC14) as IGPParameterEdit);
                        EnableParameter(m_parameters.get_Element(InputLinkReferenceFileFC5) as IGPParameterEdit);
                        EnableParameter(m_parameters.get_Element(InputTMCReferenceFile) as IGPParameterEdit);
                        EnableParameter(m_parameters.get_Element(InputSPDFile) as IGPParameterEdit);
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

            // Verify chosen input Streets feature class has the expected fields

            gpParam = paramvalues.get_Element(InputStreetsFeatureClass) as IGPParameter;
            IGPValue tableValue = m_gpUtils.UnpackGPValue(gpParam);
            if (!tableValue.IsEmpty())
            {
                IDETable inputTable = m_gpUtils.DecodeDETable(tableValue);
                CheckForTableFields(inputTable, StreetsFieldNames, StreetsFieldTypes, messages.GetMessage(InputStreetsFeatureClass));
            }

            // Verify chosen input AltStreets feature class has the expected fields

            gpParam = paramvalues.get_Element(InputAltStreetsFeatureClass) as IGPParameter;
            tableValue = m_gpUtils.UnpackGPValue(gpParam);
            if (!tableValue.IsEmpty())
            {
                IDETable inputTable = m_gpUtils.DecodeDETable(tableValue);
                CheckForTableFields(inputTable, AltStreetsFieldNames, AltStreetsFieldTypes, messages.GetMessage(InputAltStreetsFeatureClass));
            }

            // Verify chosen input Z-Levels feature class has the expected fields

            gpParam = paramvalues.get_Element(InputZLevelsFeatureClass) as IGPParameter;
            tableValue = m_gpUtils.UnpackGPValue(gpParam);
            if (!tableValue.IsEmpty())
            {
                IDETable inputTable = m_gpUtils.DecodeDETable(tableValue);
                CheckForTableFields(inputTable, ZLevelsFieldNames, ZLevelsFieldTypes, messages.GetMessage(InputZLevelsFeatureClass));
            }

            // Verify chosen input Cdms table has the expected fields

            gpParam = paramvalues.get_Element(InputCdmsTable) as IGPParameter;
            tableValue = m_gpUtils.UnpackGPValue(gpParam);
            if (!tableValue.IsEmpty())
            {
                IDETable inputTable = m_gpUtils.DecodeDETable(tableValue);
                CheckForTableFields(inputTable, CdmsFieldNames, CdmsFieldTypes, messages.GetMessage(InputCdmsTable));
            }

            // Verify chosen input Rdms table has the expected fields

            gpParam = paramvalues.get_Element(InputRdmsTable) as IGPParameter;
            tableValue = m_gpUtils.UnpackGPValue(gpParam);
            if (!tableValue.IsEmpty())
            {
                IDETable inputTable = m_gpUtils.DecodeDETable(tableValue);
                CheckForTableFields(inputTable, RdmsFieldNames, RdmsFieldTypes, messages.GetMessage(InputRdmsTable));
            }

            // Verify chosen input Signs table has the expected fields

            gpParam = paramvalues.get_Element(InputSignsTable) as IGPParameter;
            tableValue = m_gpUtils.UnpackGPValue(gpParam);
            if (!tableValue.IsEmpty())
            {
                IDETable inputTable = m_gpUtils.DecodeDETable(tableValue);
                CheckForTableFields(inputTable, SignsFieldNames, SignsFieldTypes, messages.GetMessage(InputSignsTable));
            }

            // Verify chosen input time zone table has the expected fields

            gpParam = paramvalues.get_Element(InputTimeZoneTable) as IGPParameter;
            tableValue = m_gpUtils.UnpackGPValue(gpParam);
            if (!tableValue.IsEmpty())
            {
                IDETable inputTable = m_gpUtils.DecodeDETable(tableValue);
                CheckForTableFields(inputTable, TimeZoneFieldNames, TimeZoneFieldTypes, messages.GetMessage(InputTimeZoneTable));
            }

            // Verify chosen input Traffic table has the expected fields

            gpParam = paramvalues.get_Element(InputTrafficTable) as IGPParameter;
            tableValue = m_gpUtils.UnpackGPValue(gpParam);
            if (!tableValue.IsEmpty())
            {
                IDETable inputTable = m_gpUtils.DecodeDETable(tableValue);
                CheckForTableFields(inputTable, TrafficFieldNames, TrafficFieldTypes, messages.GetMessage(InputTrafficTable));
            }

            // Verify chosen Traffic Patterns files have a ".csv" extension

            gpParam = paramvalues.get_Element(InputLinkReferenceFileFC14) as IGPParameter;
            IGPValue inputLinkReferenceFileFC14Value = m_gpUtils.UnpackGPValue(gpParam);
            if (!inputLinkReferenceFileFC14Value.IsEmpty())
            {
                string inputLinkReferenceFileFC14ValueAsText = inputLinkReferenceFileFC14Value.GetAsText();
                if (!(inputLinkReferenceFileFC14ValueAsText.EndsWith(".csv")))
                {
                    IGPMessage gpMessage = messages.GetMessage(InputTMCReferenceFile);
                    gpMessage.Type = esriGPMessageType.esriGPMessageTypeError;
                    gpMessage.Description = "Input value is not a .csv file.";
                }
                else
                {
                    System.IO.StreamReader f = new System.IO.StreamReader(inputLinkReferenceFileFC14ValueAsText);
                    string firstLine;
                    if ((firstLine = f.ReadLine()) == null)
                    {
                        IGPMessage gpMessage = messages.GetMessage(InputLinkReferenceFileFC14);
                        gpMessage.Type = esriGPMessageType.esriGPMessageTypeError;
                        gpMessage.Description = "Input value is an empty .csv file.";
                    }
                    else if (firstLine != "LINK_PVID,TRAVEL_DIRECTION,U,M,T,W,R,F,S")
                    {
                        IGPMessage gpMessage = messages.GetMessage(InputLinkReferenceFileFC14);
                        gpMessage.Type = esriGPMessageType.esriGPMessageTypeError;
                        gpMessage.Description = "Input value is not a valid Traffic Patterns Link reference file.";
                    }
                    f.Close();
                }
            }

            gpParam = paramvalues.get_Element(InputLinkReferenceFileFC5) as IGPParameter;
            IGPValue inputLinkReferenceFileFC5Value = m_gpUtils.UnpackGPValue(gpParam);
            if (!inputLinkReferenceFileFC5Value.IsEmpty())
            {
                string inputLinkReferenceFileFC5ValueAsText = inputLinkReferenceFileFC5Value.GetAsText();
                if (!(inputLinkReferenceFileFC5ValueAsText.EndsWith(".csv")))
                {
                    IGPMessage gpMessage = messages.GetMessage(InputTMCReferenceFile);
                    gpMessage.Type = esriGPMessageType.esriGPMessageTypeError;
                    gpMessage.Description = "Input value is not a .csv file.";
                }
                else
                {
                    System.IO.StreamReader f = new System.IO.StreamReader(inputLinkReferenceFileFC5ValueAsText);
                    string firstLine;
                    if ((firstLine = f.ReadLine()) == null)
                    {
                        IGPMessage gpMessage = messages.GetMessage(InputLinkReferenceFileFC5);
                        gpMessage.Type = esriGPMessageType.esriGPMessageTypeError;
                        gpMessage.Description = "Input value is an empty .csv file.";
                    }
                    else if (firstLine != "LINK_PVID,TRAVEL_DIRECTION,U,M,T,W,R,F,S")
                    {
                        IGPMessage gpMessage = messages.GetMessage(InputLinkReferenceFileFC5);
                        gpMessage.Type = esriGPMessageType.esriGPMessageTypeError;
                        gpMessage.Description = "Input value is not a valid Traffic Patterns Link reference file.";
                    }
                    f.Close();
                }
            }

            gpParam = paramvalues.get_Element(InputTMCReferenceFile) as IGPParameter;
            IGPValue inputTMCReferenceFileValue = m_gpUtils.UnpackGPValue(gpParam);
            if (!inputTMCReferenceFileValue.IsEmpty())
            {
                string inputTMCReferenceFileValueAsText = inputTMCReferenceFileValue.GetAsText();
                if (!(inputTMCReferenceFileValueAsText.EndsWith(".csv")))
                {
                    IGPMessage gpMessage = messages.GetMessage(InputTMCReferenceFile);
                    gpMessage.Type = esriGPMessageType.esriGPMessageTypeError;
                    gpMessage.Description = "Input value is not a .csv file.";
                }
                else
                {
                    System.IO.StreamReader f = new System.IO.StreamReader(inputTMCReferenceFileValueAsText);
                    string firstLine;
                    if ((firstLine = f.ReadLine()) == null)
                    {
                        IGPMessage gpMessage = messages.GetMessage(InputTMCReferenceFile);
                        gpMessage.Type = esriGPMessageType.esriGPMessageTypeError;
                        gpMessage.Description = "Input value is an empty .csv file.";
                    }
                    else if (firstLine != "TMC,U,M,T,W,R,F,S")
                    {
                        IGPMessage gpMessage = messages.GetMessage(InputTMCReferenceFile);
                        gpMessage.Type = esriGPMessageType.esriGPMessageTypeError;
                        gpMessage.Description = "Input value is not a valid Traffic Patterns TMC reference file.";
                    }
                    f.Close();
                }
            }

            gpParam = paramvalues.get_Element(InputSPDFile) as IGPParameter;
            IGPValue inputSPDFileValue = m_gpUtils.UnpackGPValue(gpParam);
            if (!inputSPDFileValue.IsEmpty())
            {
                string inputSPDFileValueAsText = inputSPDFileValue.GetAsText();
                if (!(inputSPDFileValueAsText.EndsWith(".csv")))
                {
                    IGPMessage gpMessage = messages.GetMessage(InputSPDFile);
                    gpMessage.Type = esriGPMessageType.esriGPMessageTypeError;
                    gpMessage.Description = "Input value is not a .csv file.";
                }
                else
                {
                    System.IO.StreamReader f = new System.IO.StreamReader(inputSPDFileValueAsText);
                    string firstLine;
                    if ((firstLine = f.ReadLine()) == null)
                    {
                        IGPMessage gpMessage = messages.GetMessage(InputSPDFile);
                        gpMessage.Type = esriGPMessageType.esriGPMessageTypeError;
                        gpMessage.Description = "Input value is an empty .csv file.";
                    }
                    else if (firstLine.Length != 682)
                    {
                        IGPMessage gpMessage = messages.GetMessage(InputSPDFile);
                        gpMessage.Type = esriGPMessageType.esriGPMessageTypeError;
                        gpMessage.Description = "Input value is not a valid 15-minute granular Traffic Patterns speed file.";
                    }
                    f.Close();
                }
            }

            // Verify chosen input US CndMod table has the expected fields

            gpParam = paramvalues.get_Element(InputUSCndModTable) as IGPParameter;
            tableValue = m_gpUtils.UnpackGPValue(gpParam);
            if (!tableValue.IsEmpty())
            {
                IDETable inputTable = m_gpUtils.DecodeDETable(tableValue);
                CheckForTableFields(inputTable, CndModFieldNames, CndModFieldTypes, messages.GetMessage(InputUSCndModTable));
            }

            // Verify chosen input Non-US CndMod table has the expected fields

            gpParam = paramvalues.get_Element(InputNonUSCndModTable) as IGPParameter;
            tableValue = m_gpUtils.UnpackGPValue(gpParam);
            if (!tableValue.IsEmpty())
            {
                IDETable inputTable = m_gpUtils.DecodeDETable(tableValue);
                CheckForTableFields(inputTable, CndModFieldNames, CndModFieldTypes, messages.GetMessage(InputNonUSCndModTable));
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

                IGPParameter gpParam = paramvalues.get_Element(InputStreetsFeatureClass) as IGPParameter;
                IGPValue inputStreetsFeatureClassValue = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(InputAltStreetsFeatureClass) as IGPParameter;
                IGPValue inputAltStreetsFeatureClassValue = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(InputZLevelsFeatureClass) as IGPParameter;
                IGPValue inputZLevelsFeatureClassValue = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(InputCdmsTable) as IGPParameter;
                IGPValue inputCdmsTableValue = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(InputRdmsTable) as IGPParameter;
                IGPValue inputRdmsTableValue = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(InputSignsTable) as IGPParameter;
                IGPValue inputSignsTableValue = m_gpUtils.UnpackGPValue(gpParam);
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
                gpParam = paramvalues.get_Element(InputCreateArcGISOnlineNetworkAttributes) as IGPParameter;
                IGPValue inputCreateArcGISOnlineNetworkAttributesValue = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(InputTimeZoneIDBaseFieldName) as IGPParameter;
                IGPValue inputTimeZoneIDBaseFieldNameValue = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(InputTimeZoneTable) as IGPParameter;
                IGPValue inputTimeZoneTableValue = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(InputCommonTimeZoneForTheEntireDataset) as IGPParameter;
                IGPValue inputCommonTimeZoneForTheEntireDatasetValue = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(InputTrafficTable) as IGPParameter;
                IGPValue inputTrafficTableValue = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(InputLinkReferenceFileFC14) as IGPParameter;
                IGPValue inputLinkReferenceFileFC14Value = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(InputLinkReferenceFileFC5) as IGPParameter;
                IGPValue inputLinkReferenceFileFC5Value = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(InputTMCReferenceFile) as IGPParameter;
                IGPValue inputTMCReferenceFileValue = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(InputSPDFile) as IGPParameter;
                IGPValue inputSPDFileValue = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(InputLiveTrafficFeedFolder) as IGPParameter;
                IGPValue inputLiveTrafficFeedFolderValue = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(InputLiveTrafficFeedArcGISServerConnection) as IGPParameter;
                IGPValue inputLiveTrafficFeedArcGISServerConnectionValue = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(InputLiveTrafficFeedGeoprocessingServiceName) as IGPParameter;
                IGPValue inputLiveTrafficFeedGeoprocessingServiceNameValue = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(InputLiveTrafficFeedGeoprocessingTaskName) as IGPParameter;
                IGPValue inputLiveTrafficFeedGeoprocessingTaskNameValue = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(InputUSCndModTable) as IGPParameter;
                IGPValue inputUSCndModTableValue = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(InputNonUSCndModTable) as IGPParameter;
                IGPValue inputNonUSCndModTableValue = m_gpUtils.UnpackGPValue(gpParam);

                double fgdbVersion = 0.0;
                if (!(outputFileGDBVersionValue.IsEmpty()))
                    fgdbVersion = Convert.ToDouble(outputFileGDBVersionValue.GetAsText(), System.Globalization.CultureInfo.InvariantCulture);

                bool createNetworkAttributesInMetric = false;
                if (!(inputCreateNetworkAttributeInMetricValue.IsEmpty()))
                    createNetworkAttributesInMetric = ((inputCreateNetworkAttributeInMetricValue.GetAsText()).ToUpper() == "TRUE");

                bool createArcGISOnlineNetworkAttributes = false;
                if (!(inputCreateArcGISOnlineNetworkAttributesValue.IsEmpty()))
                    createArcGISOnlineNetworkAttributes = ((inputCreateArcGISOnlineNetworkAttributesValue.GetAsText()).ToUpper() == "TRUE");

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
                    IDETable inputTable = m_gpUtils.DecodeDETable(inputStreetsFeatureClassValue);
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

                if (inputLinkReferenceFileFC14Value.IsEmpty() ^ inputLinkReferenceFileFC5Value.IsEmpty())
                {
                    messages.AddError(1, "Both Traffic Patterns Link Reference .csv files must be specified together.");
                    return;
                }

                bool usesHistoricalTraffic = !(inputSPDFileValue.IsEmpty());
                bool usesNTPFullCoverage = !(inputLinkReferenceFileFC5Value.IsEmpty());
                if ((inputTMCReferenceFileValue.IsEmpty() & !usesNTPFullCoverage) ^ inputSPDFileValue.IsEmpty())
                {
                    messages.AddError(1, "The Traffic Patterns SPD .csv file must be specified with the Traffic Patterns Link/TMC Reference .csv file(s).");
                    return;
                }

                if (inputTrafficTableValue.IsEmpty() & !inputTMCReferenceFileValue.IsEmpty())
                {
                    messages.AddError(1, "If the TMC Reference .csv file is specified, then the Traffic table must also be specified.");
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

                if (inputTrafficTableValue.IsEmpty() & !(feedFolderIsEmpty & agsConnectionIsEmpty))
                {
                    messages.AddError(1, "The Traffic table must be specified to use Live Traffic.");
                    return;
                }

                bool usesTransport = true;
                if (inputUSCndModTableValue.IsEmpty() && inputNonUSCndModTableValue.IsEmpty())
                    usesTransport = false;

                ITrafficFeedLocation trafficFeedLocation = null;
                if (!agsConnectionIsEmpty)
                {
                    // We're using an ArcGIS Server Connection and Geoprocessing Service/Task

                    ITrafficFeedGPService tfgps = new TrafficFeedGPServiceClass();
                    IName trafficFeedGPServiceName = m_gpUtils.GetNameObject(inputLiveTrafficFeedArcGISServerConnectionValue as IDataElement);
                    tfgps.ConnectionProperties = ((IAGSServerConnectionName)trafficFeedGPServiceName).ConnectionProperties;
                    tfgps.ServiceName = inputLiveTrafficFeedGeoprocessingServiceNameValue.GetAsText();
                    tfgps.TaskName = inputLiveTrafficFeedGeoprocessingTaskNameValue.GetAsText();
                    trafficFeedLocation = tfgps as ITrafficFeedLocation;
                }
                else if (!feedFolderIsEmpty)
                {
                    // We're using a Traffic Feed Folder

                    ITrafficFeedDirectory tfd = new TrafficFeedDirectoryClass();
                    tfd.TrafficDirectory = inputLiveTrafficFeedFolderValue.GetAsText();
                    trafficFeedLocation = tfd as ITrafficFeedLocation;
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
                createFDSTool.spatial_reference = inputStreetsFeatureClassValue.GetAsText();
                gp.Execute(createFDSTool, trackcancel);

                // Import the Streets feature class to the file geodatabase
                // If we're using ArcInfo, also sort the feature class

                string pathToFds = outputFileGdbPath + "\\" + fdsName;
                string streetsFeatureClassPath = pathToFds + "\\" + StreetsFCName;

                IAoInitialize aoi = new AoInitializeClass();
                if (aoi.InitializedProduct() == esriLicenseProductCode.esriLicenseProductCodeAdvanced)
                {
                    AddMessage("Importing and spatially sorting the Streets feature class...", messages, trackcancel);

                    Sort sortTool = new Sort();
                    sortTool.in_dataset = inputStreetsFeatureClassValue.GetAsText();
                    sortTool.out_dataset = streetsFeatureClassPath;
                    sortTool.sort_field = "Shape";
                    sortTool.spatial_sort_method = "PEANO";
                    gp.Execute(sortTool, trackcancel);
                }
                else
                {
                    AddMessage("Importing the Streets feature class...", messages, trackcancel);

                    FeatureClassToFeatureClass importFCTool = new FeatureClassToFeatureClass();
                    importFCTool.in_features = inputStreetsFeatureClassValue.GetAsText();
                    importFCTool.out_path = pathToFds;
                    importFCTool.out_name = StreetsFCName;
                    gp.Execute(importFCTool, trackcancel);
                }

                // Add an index to the Streets feature class's LINK_ID field

                AddMessage("Indexing the LINK_ID field...", messages, trackcancel);

                AddIndex addIndexTool = new AddIndex();
                addIndexTool.in_table = streetsFeatureClassPath;
                addIndexTool.fields = "LINK_ID";
                addIndexTool.index_name = "LINK_ID";
                gp.Execute(addIndexTool, trackcancel);

                // Add the alternate street name fields to the Streets feature class

                AddMessage("Creating fields on the Streets feature class for the alternate street names...", messages, trackcancel);

                AddField addFieldTool = new AddField();
                addFieldTool.in_table = streetsFeatureClassPath;

                addFieldTool.field_type = "TEXT";
                addFieldTool.field_length = 240;
                addFieldTool.field_name = "ST_NAME_Alt";
                gp.Execute(addFieldTool, trackcancel);
                addFieldTool.field_length = 3;
                addFieldTool.field_name = "ST_LANGCD_Alt";
                gp.Execute(addFieldTool, trackcancel);
                addFieldTool.field_length = 6;
                addFieldTool.field_name = "ST_NM_PREF_Alt";
                gp.Execute(addFieldTool, trackcancel);
                addFieldTool.field_length = 90;
                addFieldTool.field_name = "ST_TYP_BEF_Alt";
                gp.Execute(addFieldTool, trackcancel);
                addFieldTool.field_length = 105;
                addFieldTool.field_name = "ST_NM_BASE_Alt";
                gp.Execute(addFieldTool, trackcancel);
                addFieldTool.field_length = 6;
                addFieldTool.field_name = "ST_NM_SUFF_Alt";
                gp.Execute(addFieldTool, trackcancel);
                addFieldTool.field_length = 90;
                addFieldTool.field_name = "ST_TYP_AFT_Alt";
                gp.Execute(addFieldTool, trackcancel);
                addFieldTool.field_length = 1;
                addFieldTool.field_name = "DIRONSIGN_Alt";
                gp.Execute(addFieldTool, trackcancel);

                // Extract the explicatable street names

                string ExplicatblTablePath = outputFileGdbPath + "\\Explicatbl";

                AddMessage("Extracting the explicatable street names...", messages, trackcancel);

                TableSelect tableSelectTool = new TableSelect();
                tableSelectTool.in_table = inputAltStreetsFeatureClassValue.GetAsText();
                tableSelectTool.out_table = ExplicatblTablePath;
                tableSelectTool.where_clause = "EXPLICATBL = 'Y'";
                gp.Execute(tableSelectTool, trackcancel);

                // Determine which explicatable names are alternate names and extract them

                MakeTableView makeTableViewTool = new MakeTableView();
                makeTableViewTool.in_table = ExplicatblTablePath;
                makeTableViewTool.out_view = "Explicatbl_View";
                gp.Execute(makeTableViewTool, trackcancel);

                AddJoin addJoinTool = new AddJoin();
                addJoinTool.in_layer_or_view = "Explicatbl_View";
                addJoinTool.in_field = "LINK_ID";
                addJoinTool.join_table = streetsFeatureClassPath;
                addJoinTool.join_field = "LINK_ID";
                gp.Execute(addJoinTool, trackcancel);

                AddMessage("Extracting the alternate street names...", messages, trackcancel);

                TableToTable importTableTool = new TableToTable();
                importTableTool.in_rows = "Explicatbl_View";
                importTableTool.out_path = outputFileGdbPath;
                importTableTool.out_name = "AltNames";
                importTableTool.where_clause = "Explicatbl.ST_TYP_BEF <> " + StreetsFCName + ".ST_TYP_BEF OR " +
                                               "Explicatbl.ST_NM_BASE <> " + StreetsFCName + ".ST_NM_BASE OR " +
                                               "Explicatbl.ST_TYP_AFT <> " + StreetsFCName + ".ST_TYP_AFT";
                importTableTool.field_mapping = "LINK_ID \"LINK_ID\" true true false 4 Long 0 0 ,First,#," + ExplicatblTablePath + ",Explicatbl.LINK_ID,-1,-1;" +
                                                "ST_NAME \"ST_NAME\" true true false 240 Text 0 0 ,First,#," + ExplicatblTablePath + ",Explicatbl.ST_NAME,-1,-1;" +
                                                "ST_LANGCD \"ST_LANGCD\" true true false 3 Text 0 0 ,First,#," + ExplicatblTablePath + ",Explicatbl.ST_LANGCD,-1,-1;" +
                                                "ST_NM_PREF \"ST_NM_PREF\" true true false 6 Text 0 0 ,First,#," + ExplicatblTablePath + ",Explicatbl.ST_NM_PREF,-1,-1;" +
                                                "ST_TYP_BEF \"ST_TYP_BEF\" true true false 90 Text 0 0 ,First,#," + ExplicatblTablePath + ",Explicatbl.ST_TYP_BEF,-1,-1;" +
                                                "ST_NM_BASE \"ST_NM_BASE\" true true false 105 Text 0 0 ,First,#," + ExplicatblTablePath + ",Explicatbl.ST_NM_BASE,-1,-1;" +
                                                "ST_NM_SUFF \"ST_NM_SUFF\" true true false 6 Text 0 0 ,First,#," + ExplicatblTablePath + ",Explicatbl.ST_NM_SUFF,-1,-1;" +
                                                "ST_TYP_AFT \"ST_TYP_AFT\" true true false 90 Text 0 0 ,First,#," + ExplicatblTablePath + ",Explicatbl.ST_TYP_AFT,-1,-1;" +
                                                "DIRONSIGN \"DIRONSIGN\" true true false 1 Text 0 0 ,First,#," + ExplicatblTablePath + ",Explicatbl.DIRONSIGN,-1,-1";
                gp.Execute(importTableTool, trackcancel);

                RemoveJoin removeJoinTool = new RemoveJoin();
                removeJoinTool.in_layer_or_view = "Explicatbl_View";
                removeJoinTool.join_name = StreetsFCName;
                gp.Execute(removeJoinTool, trackcancel);

                Delete deleteTool = new Delete();
                deleteTool.in_data = "Explicatbl_View";
                gp.Execute(deleteTool, trackcancel);
                deleteTool.in_data = ExplicatblTablePath;
                gp.Execute(deleteTool, trackcancel);

                string AltNamesTablePath = outputFileGdbPath + "\\AltNames";

                addIndexTool = new AddIndex();
                addIndexTool.in_table = AltNamesTablePath;
                addIndexTool.fields = "LINK_ID";
                addIndexTool.index_name = "LINK_ID";
                gp.Execute(addIndexTool, trackcancel);

                // Calculate the alternate street name fields

                MakeFeatureLayer makeFeatureLayerTool = new MakeFeatureLayer();
                makeFeatureLayerTool.in_features = streetsFeatureClassPath;
                makeFeatureLayerTool.out_layer = "Streets_Layer";
                gp.Execute(makeFeatureLayerTool, trackcancel);

                addJoinTool = new AddJoin();
                addJoinTool.in_layer_or_view = "Streets_Layer";
                addJoinTool.in_field = "LINK_ID";
                addJoinTool.join_table = AltNamesTablePath;
                addJoinTool.join_field = "LINK_ID";
                gp.Execute(addJoinTool, trackcancel);

                AddMessage("Copying over the alternate ST_NAME values...", messages, trackcancel);

                CalculateField calcFieldTool = new CalculateField();
                calcFieldTool.in_table = "Streets_Layer";
                calcFieldTool.expression_type = "VB";
                calcFieldTool.field = StreetsFCName + ".ST_NAME_Alt";
                calcFieldTool.expression = "[AltNames.ST_NAME]";
                gp.Execute(calcFieldTool, trackcancel);

                AddMessage("Copying over the alternate ST_LANGCD values...", messages, trackcancel);

                calcFieldTool.field = StreetsFCName + ".ST_LANGCD_Alt";
                calcFieldTool.expression = "[AltNames.ST_LANGCD]";
                gp.Execute(calcFieldTool, trackcancel);

                AddMessage("Copying over the alternate ST_NM_PREF values...", messages, trackcancel);

                calcFieldTool.field = StreetsFCName + ".ST_NM_PREF_Alt";
                calcFieldTool.expression = "[AltNames.ST_NM_PREF]";
                gp.Execute(calcFieldTool, trackcancel);

                AddMessage("Copying over the alternate ST_TYP_BEF values...", messages, trackcancel);

                calcFieldTool.field = StreetsFCName + ".ST_TYP_BEF_Alt";
                calcFieldTool.expression = "[AltNames.ST_TYP_BEF]";
                gp.Execute(calcFieldTool, trackcancel);

                AddMessage("Copying over the alternate ST_NM_BASE values...", messages, trackcancel);

                calcFieldTool.field = StreetsFCName + ".ST_NM_BASE_Alt";
                calcFieldTool.expression = "[AltNames.ST_NM_BASE]";
                gp.Execute(calcFieldTool, trackcancel);

                AddMessage("Copying over the alternate ST_NM_SUFF values...", messages, trackcancel);

                calcFieldTool.field = StreetsFCName + ".ST_NM_SUFF_Alt";
                calcFieldTool.expression = "[AltNames.ST_NM_SUFF]";
                gp.Execute(calcFieldTool, trackcancel);

                AddMessage("Copying over the alternate ST_TYP_AFT values...", messages, trackcancel);

                calcFieldTool.field = StreetsFCName + ".ST_TYP_AFT_Alt";
                calcFieldTool.expression = "[AltNames.ST_TYP_AFT]";
                gp.Execute(calcFieldTool, trackcancel);

                AddMessage("Copying over the alternate DIRONSIGN values...", messages, trackcancel);

                calcFieldTool.field = StreetsFCName + ".DIRONSIGN_Alt";
                calcFieldTool.expression = "[AltNames.DIRONSIGN]";
                gp.Execute(calcFieldTool, trackcancel);

                removeJoinTool = new RemoveJoin();
                removeJoinTool.in_layer_or_view = "Streets_Layer";
                removeJoinTool.join_name = "AltNames";
                gp.Execute(removeJoinTool, trackcancel);

                deleteTool = new Delete();
                deleteTool.in_data = "Streets_Layer";
                gp.Execute(deleteTool, trackcancel);
                deleteTool.in_data = AltNamesTablePath;
                gp.Execute(deleteTool, trackcancel);

                // Add fields for the Z-Level values to the Streets feature class

                AddMessage("Creating fields on the Streets feature class for the Z-Level values...", messages, trackcancel);

                addFieldTool = new AddField();
                addFieldTool.in_table = streetsFeatureClassPath;

                addFieldTool.field_type = "SHORT";
                addFieldTool.field_name = "F_ZLEV";
                gp.Execute(addFieldTool, trackcancel);
                addFieldTool.field_name = "T_ZLEV";
                gp.Execute(addFieldTool, trackcancel);

                // Separate out the From and To Z-Level values into separate tables and index the LINK_ID fields

                string FromZsTablePath = outputFileGdbPath + "\\FromZs";
                string ToZsTablePath = outputFileGdbPath + "\\ToZs";

                AddMessage("Extracting the From Z-Level information...", messages, trackcancel);

                tableSelectTool = new TableSelect();
                tableSelectTool.in_table = inputZLevelsFeatureClassValue.GetAsText();
                tableSelectTool.out_table = FromZsTablePath;
                tableSelectTool.where_clause = "INTRSECT = 'Y' AND POINT_NUM = 1";
                gp.Execute(tableSelectTool, trackcancel);

                AddMessage("Extracting the To Z-Level information...", messages, trackcancel);

                tableSelectTool.out_table = ToZsTablePath;
                tableSelectTool.where_clause = "INTRSECT = 'Y' AND POINT_NUM > 1";
                gp.Execute(tableSelectTool, trackcancel);

                AddMessage("Analyzing the Z-Level information...", messages, trackcancel);

                addIndexTool = new AddIndex();
                addIndexTool.in_table = FromZsTablePath;
                addIndexTool.fields = "LINK_ID";
                addIndexTool.index_name = "LINK_ID";
                gp.Execute(addIndexTool, trackcancel);
                addIndexTool.in_table = ToZsTablePath;
                gp.Execute(addIndexTool, trackcancel);

                // Calculate the Z-Level fields

                makeFeatureLayerTool = new MakeFeatureLayer();
                makeFeatureLayerTool.in_features = streetsFeatureClassPath;
                makeFeatureLayerTool.out_layer = "Streets_Layer";
                gp.Execute(makeFeatureLayerTool, trackcancel);

                addJoinTool = new AddJoin();
                addJoinTool.in_layer_or_view = "Streets_Layer";
                addJoinTool.in_field = "LINK_ID";
                addJoinTool.join_table = FromZsTablePath;
                addJoinTool.join_field = "LINK_ID";
                gp.Execute(addJoinTool, trackcancel);

                AddMessage("Copying over the F_ZLEV values...", messages, trackcancel);

                calcFieldTool = new CalculateField();
                calcFieldTool.in_table = "Streets_Layer";
                calcFieldTool.field = StreetsFCName + ".F_ZLEV";
                calcFieldTool.expression = "[FromZs.Z_LEVEL]";
                calcFieldTool.expression_type = "VB";
                gp.Execute(calcFieldTool, trackcancel);

                removeJoinTool = new RemoveJoin();
                removeJoinTool.in_layer_or_view = "Streets_Layer";
                removeJoinTool.join_name = "FromZs";
                gp.Execute(removeJoinTool, trackcancel);

                addJoinTool = new AddJoin();
                addJoinTool.in_layer_or_view = "Streets_Layer";
                addJoinTool.in_field = "LINK_ID";
                addJoinTool.join_table = ToZsTablePath;
                addJoinTool.join_field = "LINK_ID";
                gp.Execute(addJoinTool, trackcancel);

                AddMessage("Copying over the T_ZLEV values...", messages, trackcancel);

                calcFieldTool = new CalculateField();
                calcFieldTool.in_table = "Streets_Layer";
                calcFieldTool.field = StreetsFCName + ".T_ZLEV";
                calcFieldTool.expression = "[ToZs.Z_LEVEL]";
                calcFieldTool.expression_type = "VB";
                gp.Execute(calcFieldTool, trackcancel);

                removeJoinTool = new RemoveJoin();
                removeJoinTool.in_layer_or_view = "Streets_Layer";
                removeJoinTool.join_name = "ToZs";
                gp.Execute(removeJoinTool, trackcancel);

                deleteTool = new Delete();
                deleteTool.in_data = "Streets_Layer";
                gp.Execute(deleteTool, trackcancel);

                deleteTool.in_data = FromZsTablePath;
                gp.Execute(deleteTool, trackcancel);
                deleteTool.in_data = ToZsTablePath;
                gp.Execute(deleteTool, trackcancel);

                // Add fields for the distance (Meters), language, and speed (KPH) values to the Streets feature class

                AddMessage("Creating fields on the Streets feature class for distance, speed, and language...", messages, trackcancel);

                addFieldTool = new AddField();
                addFieldTool.in_table = streetsFeatureClassPath;

                addFieldTool.field_type = "DOUBLE";
                addFieldTool.field_name = "Meters";
                gp.Execute(addFieldTool, trackcancel);
                addFieldTool.field_type = "FLOAT";
                addFieldTool.field_name = "KPH";
                gp.Execute(addFieldTool, trackcancel);
                addFieldTool.field_type = "TEXT";
                addFieldTool.field_name = "Language";
                addFieldTool.field_length = 2;
                gp.Execute(addFieldTool, trackcancel);
                addFieldTool.field_type = "TEXT";
                addFieldTool.field_name = "Language_Alt";
                addFieldTool.field_length = 2;
                gp.Execute(addFieldTool, trackcancel);

                // Calculate the distance (Meters), language, and speed (KPH) fields

                AddMessage("Calculating the distance, speed, and language fields...", messages, trackcancel);

                CalculateMetersKPHAndLanguageFields(outputFileGdbPath);

                if (createArcGISOnlineNetworkAttributes)
                {
                    CreateAndPopulateDirectionalVehicleAccessFields("AR_AUTO", streetsFeatureClassPath, gp, messages, trackcancel);
                    CreateAndPopulateDirectionalVehicleAccessFields("AR_BUS", streetsFeatureClassPath, gp, messages, trackcancel);
                    CreateAndPopulateDirectionalVehicleAccessFields("AR_TAXIS", streetsFeatureClassPath, gp, messages, trackcancel);
                    CreateAndPopulateDirectionalVehicleAccessFields("AR_TRUCKS", streetsFeatureClassPath, gp, messages, trackcancel);
                    CreateAndPopulateDirectionalVehicleAccessFields("AR_DELIV", streetsFeatureClassPath, gp, messages, trackcancel);
                    CreateAndPopulateDirectionalVehicleAccessFields("AR_EMERVEH", streetsFeatureClassPath, gp, messages, trackcancel);
                    CreateAndPopulateDirectionalVehicleAccessFields("AR_MOTOR", streetsFeatureClassPath, gp, messages, trackcancel);
                }

                // Add a field to the Streets feature class indicating roads closed for construction

                AddMessage("Creating the ClosedForConstruction field on the Streets feature class...", messages, trackcancel);

                addFieldTool = new AddField();
                addFieldTool.in_table = streetsFeatureClassPath;
                addFieldTool.field_type = "TEXT";
                addFieldTool.field_name = "ClosedForConstruction";
                addFieldTool.field_length = 1;
                gp.Execute(addFieldTool, trackcancel);

                // Create a table for looking up roads closed for construction

                AddMessage("Creating and indexing the table containing roads closed for construction...", messages, trackcancel);

                string closedForConstructionTablePath = outputFileGdbPath + "\\ClosedForConstruction";

                tableSelectTool = new TableSelect();
                tableSelectTool.in_table = inputCdmsTableValue.GetAsText();
                tableSelectTool.out_table = closedForConstructionTablePath;
                tableSelectTool.where_clause = "COND_TYPE = 3";
                gp.Execute(tableSelectTool, trackcancel);

                addIndexTool = new AddIndex();
                addIndexTool.in_table = closedForConstructionTablePath;
                addIndexTool.fields = "LINK_ID";
                addIndexTool.index_name = "LINK_ID";
                gp.Execute(addIndexTool, trackcancel);

                makeFeatureLayerTool = new MakeFeatureLayer();
                makeFeatureLayerTool.in_features = streetsFeatureClassPath;
                makeFeatureLayerTool.out_layer = "Streets_Layer";
                gp.Execute(makeFeatureLayerTool, trackcancel);

                addJoinTool = new AddJoin();
                addJoinTool.in_layer_or_view = "Streets_Layer";
                addJoinTool.in_field = "LINK_ID";
                addJoinTool.join_table = closedForConstructionTablePath;
                addJoinTool.join_field = "LINK_ID";
                addJoinTool.join_type = "KEEP_COMMON";
                gp.Execute(addJoinTool, trackcancel);

                AddMessage("Calculating the ClosedForConstruction field...", messages, trackcancel);

                calcFieldTool = new CalculateField();
                calcFieldTool.in_table = "Streets_Layer";
                calcFieldTool.field = "Streets.ClosedForConstruction";
                calcFieldTool.expression = "\"Y\"";
                calcFieldTool.expression_type = "VB";
                gp.Execute(calcFieldTool, trackcancel);

                removeJoinTool = new RemoveJoin();
                removeJoinTool.in_layer_or_view = "Streets_Layer";
                removeJoinTool.join_name = "ClosedForConstruction";
                gp.Execute(removeJoinTool, trackcancel);

                deleteTool = new Delete();
                deleteTool.in_data = "Streets_Layer";
                gp.Execute(deleteTool, trackcancel);
                deleteTool.in_data = closedForConstructionTablePath;
                gp.Execute(deleteTool, trackcancel);

                // Add fields to the Streets feature class indicating roads with usage fees

                string[] arFieldSuffixes = new string[] { "AUTO", "BUS", "TAXIS", "CARPOOL", "PEDSTRN",
                                                          "TRUCKS", "THRUTR", "DELIVER", "EMERVEH", "MOTOR" };
 
                AddMessage("Creating the UFR fields on the Streets feature class...", messages, trackcancel);

                foreach (string arFieldSuffix in arFieldSuffixes)
                {
                    addFieldTool = new AddField();
                    addFieldTool.in_table = streetsFeatureClassPath;
                    addFieldTool.field_name = "UFR_" + arFieldSuffix;
                    addFieldTool.field_type = "TEXT";
                    addFieldTool.field_length = 1;
                    gp.Execute(addFieldTool, trackcancel);
                }

                // Create a table for looking up roads with usage fees

                AddMessage("Creating and indexing the table containing roads with usage fees...", messages, trackcancel);

                string usageFeeRequiredTablePath = outputFileGdbPath + "\\UsageFeeRequired";

                tableSelectTool = new TableSelect();
                tableSelectTool.in_table = inputCdmsTableValue.GetAsText();
                tableSelectTool.out_table = usageFeeRequiredTablePath;
                tableSelectTool.where_clause = "COND_TYPE = 12";
                gp.Execute(tableSelectTool, trackcancel);

                addIndexTool = new AddIndex();
                addIndexTool.in_table = usageFeeRequiredTablePath;
                addIndexTool.fields = "LINK_ID";
                addIndexTool.index_name = "LINK_ID";
                gp.Execute(addIndexTool, trackcancel);

                makeFeatureLayerTool = new MakeFeatureLayer();
                makeFeatureLayerTool.in_features = streetsFeatureClassPath;
                makeFeatureLayerTool.out_layer = "Streets_Layer";
                gp.Execute(makeFeatureLayerTool, trackcancel);

                addJoinTool = new AddJoin();
                addJoinTool.in_layer_or_view = "Streets_Layer";
                addJoinTool.in_field = "LINK_ID";
                addJoinTool.join_table = usageFeeRequiredTablePath;
                addJoinTool.join_field = "LINK_ID";
                addJoinTool.join_type = "KEEP_COMMON";
                gp.Execute(addJoinTool, trackcancel);

                AddMessage("Calculating the ClosedForConstruction field...", messages, trackcancel);

                foreach (string arFieldSuffix in arFieldSuffixes)
                {
                    AddMessage("Calculating the UFR_" + arFieldSuffix + " field on the Streets feature class...", messages, trackcancel);

                    calcFieldTool = new CalculateField();
                    calcFieldTool.in_table = "Streets_Layer";
                    calcFieldTool.field = "Streets.UFR_" + arFieldSuffix;
                    calcFieldTool.expression = "[UsageFeeRequired.AR_" + arFieldSuffix + "]";
                    calcFieldTool.expression_type = "VB";
                    gp.Execute(calcFieldTool, trackcancel);
                }

                removeJoinTool = new RemoveJoin();
                removeJoinTool.in_layer_or_view = "Streets_Layer";
                removeJoinTool.join_name = "UsageFeeRequired";
                gp.Execute(removeJoinTool, trackcancel);

                deleteTool = new Delete();
                deleteTool.in_data = "Streets_Layer";
                gp.Execute(deleteTool, trackcancel);
                deleteTool.in_data = usageFeeRequiredTablePath;
                gp.Execute(deleteTool, trackcancel);

                // Copy the time zones table to the file geodatabase

                if (timeZoneIDBaseFieldName != "")
                {
                    AddMessage("Copying the TimeZones table...", messages, trackcancel);

                    importTableTool = new TableToTable();
                    importTableTool.in_rows = inputTimeZoneTableValue.GetAsText();
                    importTableTool.out_path = outputFileGdbPath;
                    importTableTool.out_name = "TimeZones";
                    gp.Execute(importTableTool, trackcancel);
                }

                #region Process Historical Traffic Tables

                if (usesHistoricalTraffic)
                {
                    // Create the Patterns table and index its PatternID field

                    AddMessage("Creating the Patterns table...", messages, trackcancel);

                    string listOfConstantPatterns = CreatePatternsTable(inputSPDFileValue.GetAsText(), outputFileGdbPath, fgdbVersion);

                    string patternsTablePath = outputFileGdbPath + "\\" + ProfilesTableName;

                    addIndexTool = new AddIndex();
                    addIndexTool.in_table = patternsTablePath;
                    addIndexTool.fields = "PatternID";
                    addIndexTool.index_name = "PatternID";
                    gp.Execute(addIndexTool, trackcancel);

                    string histTrafficJoinTablePath = outputFileGdbPath + "\\" + HistTrafficJoinTableName;

                    // Add fields for the average speeds to the Streets feature class

                    AddMessage("Creating fields for the average speeds and travel times...", messages, trackcancel);

                    addFieldTool = new AddField();
                    addFieldTool.in_table = streetsFeatureClassPath;

                    addFieldTool.field_type = "FLOAT";
                    addFieldTool.field_name = "FT_AverageSpeed";
                    gp.Execute(addFieldTool, trackcancel);
                    addFieldTool.field_name = "TF_AverageSpeed";
                    gp.Execute(addFieldTool, trackcancel);

                    if (usesNTPFullCoverage)
                    {
                        // Convert the Link Reference files to a FileGDB table

                        AddMessage("Converting the Link Reference files to a table...", messages, trackcancel);

                        ConvertLinkReferenceFilesToFGDBTable(inputLinkReferenceFileFC14Value.GetAsText(),
                                                             inputLinkReferenceFileFC5Value.GetAsText(),
                                                             outputFileGdbPath);

                        string referenceTablePath = outputFileGdbPath + "\\TempRefTable";

                        // Extract out the rows we want in our historical traffic table

                        AddMessage("Creating the historical traffic join table...", messages, trackcancel);

                        if (listOfConstantPatterns == "")
                        {
                            Rename renameTool = new Rename();
                            renameTool.in_data = referenceTablePath;
                            renameTool.out_data = histTrafficJoinTablePath;
                            gp.Execute(renameTool, trackcancel);
                        }
                        else
                        {
                            tableSelectTool = new TableSelect();
                            tableSelectTool.in_table = referenceTablePath;
                            tableSelectTool.out_table = histTrafficJoinTablePath;
                            tableSelectTool.where_clause = "NOT ( IS_FC5 = 'Y' AND U IN (" + listOfConstantPatterns +
                                                           ") AND U = M AND M = T AND T = W AND W = R AND R = F AND F = S )";
                            gp.Execute(tableSelectTool, trackcancel);

                            deleteTool = new Delete();
                            deleteTool.in_data = referenceTablePath;
                            gp.Execute(deleteTool, trackcancel);
                        }
                    }
                    else
                    {
                        // Convert the TMC Reference file to a FileGDB table and index its TMC field

                        AddMessage("Converting the TMC Reference file to a table...", messages, trackcancel);

                        ConvertTMCReferenceFileToFGDBTable(inputTMCReferenceFileValue.GetAsText(), outputFileGdbPath);

                        string referenceTablePath = outputFileGdbPath + "\\TempRefTable";

                        addIndexTool = new AddIndex();
                        addIndexTool.in_table = referenceTablePath;
                        addIndexTool.fields = "TMC";
                        addIndexTool.index_name = "TMC";
                        gp.Execute(addIndexTool, trackcancel);

                        // Copy over the Traffic table to the file geodatabase

                        AddMessage("Copying Traffic table to the file geodatabase...", messages, trackcancel);

                        importTableTool = new TableToTable();
                        importTableTool.in_rows = inputTrafficTableValue.GetAsText();
                        importTableTool.out_path = outputFileGdbPath;
                        importTableTool.out_name = "Street";
                        gp.Execute(importTableTool, trackcancel);

                        string trafficTablePath = outputFileGdbPath + "\\Street";

                        // Add the TMC field and calculate it

                        AddMessage("Calculating TMC values for historical traffic...", messages, trackcancel);

                        addFieldTool = new AddField();
                        addFieldTool.in_table = trafficTablePath;
                        addFieldTool.field_type = "TEXT";
                        addFieldTool.field_length = 9;
                        addFieldTool.field_name = "TMC";
                        gp.Execute(addFieldTool, trackcancel);

                        calcFieldTool = new CalculateField();
                        calcFieldTool.in_table = trafficTablePath;
                        calcFieldTool.field = "TMC";
                        calcFieldTool.expression = "Replace(Replace(Right([TRAFFIC_CD], 9), \"+\", \"P\"), \"-\", \"N\")";
                        calcFieldTool.expression_type = "VB";
                        gp.Execute(calcFieldTool, trackcancel);

                        AddMessage("Creating the historical traffic join table...", messages, trackcancel);

                        // Join the Traffic table to the Reference table...

                        makeTableViewTool = new MakeTableView();
                        makeTableViewTool.in_table = trafficTablePath;
                        makeTableViewTool.out_view = "Traffic_View";
                        gp.Execute(makeTableViewTool, trackcancel);

                        addJoinTool = new AddJoin();
                        addJoinTool.in_layer_or_view = "Traffic_View";
                        addJoinTool.in_field = "TMC";
                        addJoinTool.join_table = referenceTablePath;
                        addJoinTool.join_field = "TMC";
                        addJoinTool.join_type = "KEEP_COMMON";
                        gp.Execute(addJoinTool, trackcancel);

                        // ...then create the Streets_Patterns table by copying out the joined rows

                        importTableTool = new TableToTable();
                        importTableTool.in_rows = "Traffic_View";
                        importTableTool.out_path = outputFileGdbPath;
                        importTableTool.out_name = HistTrafficJoinTableName;
                        importTableTool.field_mapping = "LINK_ID \"LINK_ID\" true true false 4 Long 0 0 ,First,#," + trafficTablePath + ",Street.LINK_ID,-1,-1;" +
                                                        "TRAFFIC_CD \"TRAFFIC_CD\" true true false 10 Text 0 0 ,First,#," + trafficTablePath + ",Street.TRAFFIC_CD,-1,-1;" +
                                                        "TMC \"TMC\" true true false 9 Text 0 0 ,First,#," + trafficTablePath + ",Street.TMC,-1,-1;" +
                                                        "U \"U\" true true false 2 Short 0 0 ,First,#," + trafficTablePath + ",TempRefTable.U,-1,-1;" +
                                                        "M \"M\" true true false 2 Short 0 0 ,First,#," + trafficTablePath + ",TempRefTable.M,-1,-1;" +
                                                        "T \"T\" true true false 2 Short 0 0 ,First,#," + trafficTablePath + ",TempRefTable.T,-1,-1;" +
                                                        "W \"W\" true true false 2 Short 0 0 ,First,#," + trafficTablePath + ",TempRefTable.W,-1,-1;" +
                                                        "R \"R\" true true false 2 Short 0 0 ,First,#," + trafficTablePath + ",TempRefTable.R,-1,-1;" +
                                                        "F \"F\" true true false 2 Short 0 0 ,First,#," + trafficTablePath + ",TempRefTable.F,-1,-1;" +
                                                        "S \"S\" true true false 2 Short 0 0 ,First,#," + trafficTablePath + ",TempRefTable.S,-1,-1";
                        gp.Execute(importTableTool, trackcancel);

                        // Delete the join, view, and temporary tables

                        removeJoinTool = new RemoveJoin();
                        removeJoinTool.in_layer_or_view = "Traffic_View";
                        removeJoinTool.join_name = "TempRefTable";
                        gp.Execute(removeJoinTool, trackcancel);

                        deleteTool = new Delete();
                        deleteTool.in_data = "Traffic_View";
                        gp.Execute(deleteTool, trackcancel);

                        deleteTool = new Delete();
                        deleteTool.in_data = referenceTablePath;
                        gp.Execute(deleteTool, trackcancel);

                        deleteTool = new Delete();
                        deleteTool.in_data = trafficTablePath;
                        gp.Execute(deleteTool, trackcancel);
                    }

                    AddMessage("Creating fields on the historical traffic join table...", messages, trackcancel);

                    addFieldTool = new AddField();
                    addFieldTool.in_table = histTrafficJoinTablePath;

                    // Add FCID, FID, position, AverageSpeed[_X], and BaseSpeed[_X] fields to the Streets_Patterns table

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

                    addFieldTool.field_type = "FLOAT";
                    addFieldTool.field_name = "AverageSpeed";
                    gp.Execute(addFieldTool, trackcancel);
                    addFieldTool.field_name = "AverageSpeed_U";
                    gp.Execute(addFieldTool, trackcancel);
                    addFieldTool.field_name = "AverageSpeed_M";
                    gp.Execute(addFieldTool, trackcancel);
                    addFieldTool.field_name = "AverageSpeed_T";
                    gp.Execute(addFieldTool, trackcancel);
                    addFieldTool.field_name = "AverageSpeed_W";
                    gp.Execute(addFieldTool, trackcancel);
                    addFieldTool.field_name = "AverageSpeed_R";
                    gp.Execute(addFieldTool, trackcancel);
                    addFieldTool.field_name = "AverageSpeed_F";
                    gp.Execute(addFieldTool, trackcancel);
                    addFieldTool.field_name = "AverageSpeed_S";
                    gp.Execute(addFieldTool, trackcancel);

                    addFieldTool.field_type = "FLOAT";
                    addFieldTool.field_name = "BaseSpeed";
                    gp.Execute(addFieldTool, trackcancel);
                    addFieldTool.field_name = "BaseSpeed_U";
                    gp.Execute(addFieldTool, trackcancel);
                    addFieldTool.field_name = "BaseSpeed_M";
                    gp.Execute(addFieldTool, trackcancel);
                    addFieldTool.field_name = "BaseSpeed_T";
                    gp.Execute(addFieldTool, trackcancel);
                    addFieldTool.field_name = "BaseSpeed_W";
                    gp.Execute(addFieldTool, trackcancel);
                    addFieldTool.field_name = "BaseSpeed_R";
                    gp.Execute(addFieldTool, trackcancel);
                    addFieldTool.field_name = "BaseSpeed_F";
                    gp.Execute(addFieldTool, trackcancel);
                    addFieldTool.field_name = "BaseSpeed_S";
                    gp.Execute(addFieldTool, trackcancel);

                    // If we're creating 10.0, then we also need to create the BaseMinutes field

                    if (fgdbVersion == 10.0)
                    {
                        addFieldTool.field_type = "DOUBLE";
                        addFieldTool.field_name = "BaseMinutes";
                        gp.Execute(addFieldTool, trackcancel);
                    }

                    // Populate the AverageSpeed_X and BaseSpeed_X fields

                    makeTableViewTool = new MakeTableView();
                    makeTableViewTool.in_table = histTrafficJoinTablePath;
                    makeTableViewTool.out_view = "Streets_Patterns_View";
                    gp.Execute(makeTableViewTool, trackcancel);

                    PopulateAverageSpeedAndBaseSpeedFields(patternsTablePath, "U", gp, messages, trackcancel);
                    PopulateAverageSpeedAndBaseSpeedFields(patternsTablePath, "M", gp, messages, trackcancel);
                    PopulateAverageSpeedAndBaseSpeedFields(patternsTablePath, "T", gp, messages, trackcancel);
                    PopulateAverageSpeedAndBaseSpeedFields(patternsTablePath, "W", gp, messages, trackcancel);
                    PopulateAverageSpeedAndBaseSpeedFields(patternsTablePath, "R", gp, messages, trackcancel);
                    PopulateAverageSpeedAndBaseSpeedFields(patternsTablePath, "F", gp, messages, trackcancel);
                    PopulateAverageSpeedAndBaseSpeedFields(patternsTablePath, "S", gp, messages, trackcancel);

                    deleteTool = new Delete();
                    deleteTool.in_data = "Streets_Patterns_View";
                    gp.Execute(deleteTool, trackcancel);

                    // Calculate the AverageSpeed and BaseSpeed fields

                    AddMessage("Calculating the AverageSpeed field...", messages, trackcancel);

                    calcFieldTool = new CalculateField();
                    calcFieldTool.in_table = histTrafficJoinTablePath;
                    calcFieldTool.field = "AverageSpeed";
                    calcFieldTool.expression = "( (1/[AverageSpeed_U]) + (1/[AverageSpeed_M]) + (1/[AverageSpeed_T]) + (1/[AverageSpeed_W]) + (1/[AverageSpeed_R]) + (1/[AverageSpeed_F]) + (1/[AverageSpeed_S]) ) / " +
                                               "( (1/[AverageSpeed_U]/[AverageSpeed_U]) + (1/[AverageSpeed_M]/[AverageSpeed_M]) + (1/[AverageSpeed_T]/[AverageSpeed_T]) + (1/[AverageSpeed_W]/[AverageSpeed_W]) + (1/[AverageSpeed_R]/[AverageSpeed_R]) + (1/[AverageSpeed_F]/[AverageSpeed_F]) + (1/[AverageSpeed_S]/[AverageSpeed_S]) )";
                    calcFieldTool.expression_type = "VB";
                    gp.Execute(calcFieldTool, trackcancel);

                    AddMessage("Calculating the BaseSpeed field...", messages, trackcancel);

                    calcFieldTool = new CalculateField();
                    calcFieldTool.in_table = histTrafficJoinTablePath;
                    calcFieldTool.field = "BaseSpeed";
                    calcFieldTool.expression = "( (1/[BaseSpeed_U]) + (1/[BaseSpeed_M]) + (1/[BaseSpeed_T]) + (1/[BaseSpeed_W]) + (1/[BaseSpeed_R]) + (1/[BaseSpeed_F]) + (1/[BaseSpeed_S]) ) / " +
                                               "( (1/[BaseSpeed_U]/[BaseSpeed_U]) + (1/[BaseSpeed_M]/[BaseSpeed_M]) + (1/[BaseSpeed_T]/[BaseSpeed_T]) + (1/[BaseSpeed_W]/[BaseSpeed_W]) + (1/[BaseSpeed_R]/[BaseSpeed_R]) + (1/[BaseSpeed_F]/[BaseSpeed_F]) + (1/[BaseSpeed_S]/[BaseSpeed_S]) )";
                    calcFieldTool.expression_type = "VB";
                    gp.Execute(calcFieldTool, trackcancel);

                    DeleteField deleteFieldTool = new DeleteField();
                    deleteFieldTool.in_table = histTrafficJoinTablePath;
                    deleteFieldTool.drop_field = "AverageSpeed_U;AverageSpeed_M;AverageSpeed_T;AverageSpeed_W;AverageSpeed_R;AverageSpeed_F;AverageSpeed_S;BaseSpeed_U;BaseSpeed_M;BaseSpeed_T;BaseSpeed_W;BaseSpeed_R;BaseSpeed_F;BaseSpeed_S";
                    gp.Execute(deleteFieldTool, trackcancel);

                    makeTableViewTool = new MakeTableView();
                    makeTableViewTool.in_table = histTrafficJoinTablePath;
                    makeTableViewTool.out_view = "Streets_Patterns_View";
                    gp.Execute(makeTableViewTool, trackcancel);

                    addJoinTool = new AddJoin();
                    addJoinTool.in_layer_or_view = "Streets_Patterns_View";
                    addJoinTool.in_field = (usesNTPFullCoverage ? "LINK_PVID" : "LINK_ID");
                    addJoinTool.join_table = streetsFeatureClassPath;
                    addJoinTool.join_field = "LINK_ID";
                    gp.Execute(addJoinTool, trackcancel);

                    // Calculate the BaseMinutes field (if 10.0)

                    if (fgdbVersion == 10.0)
                    {
                        AddMessage("Calculating the BaseMinutes field...", messages, trackcancel);

                        calcFieldTool = new CalculateField();
                        calcFieldTool.in_table = "Streets_Patterns_View";
                        calcFieldTool.field = HistTrafficJoinTableName + ".BaseMinutes";
                        calcFieldTool.expression = "[" + StreetsFCName + ".Meters] * 0.06 / [" + HistTrafficJoinTableName + ".BaseSpeed]";
                        calcFieldTool.expression_type = "VB";
                        gp.Execute(calcFieldTool, trackcancel);
                    }

                    // Calculate the FCID, FID, and position fields

                    AddMessage("Calculating the EdgeFID field for historical traffic...", messages, trackcancel);

                    calcFieldTool = new CalculateField();
                    calcFieldTool.in_table = "Streets_Patterns_View";
                    calcFieldTool.field = HistTrafficJoinTableName + ".EdgeFID";
                    calcFieldTool.expression = "[" + StreetsFCName + ".OBJECTID]";
                    calcFieldTool.expression_type = "VB";
                    gp.Execute(calcFieldTool, trackcancel);

                    removeJoinTool = new RemoveJoin();
                    removeJoinTool.in_layer_or_view = "Streets_Patterns_View";
                    removeJoinTool.join_name = StreetsFCName;
                    gp.Execute(removeJoinTool, trackcancel);

                    deleteTool = new Delete();
                    deleteTool.in_data = "Streets_Patterns_View";
                    gp.Execute(deleteTool, trackcancel);

                    AddMessage("Calculating the EdgeFCID field for historical traffic...", messages, trackcancel);

                    calcFieldTool = new CalculateField();
                    calcFieldTool.in_table = histTrafficJoinTablePath;
                    calcFieldTool.field = "EdgeFCID";
                    calcFieldTool.expression = "1";
                    calcFieldTool.expression_type = "VB";
                    gp.Execute(calcFieldTool, trackcancel);

                    AddMessage("Calculating the EdgeFrmPos field for historical traffic...", messages, trackcancel);

                    calcFieldTool = new CalculateField();
                    calcFieldTool.in_table = histTrafficJoinTablePath;

                    calcFieldTool.field = "EdgeFrmPos";
                    calcFieldTool.expression = "x";
                    calcFieldTool.code_block = (usesNTPFullCoverage ? "Select Case [TRAVEL_DIRECTION]\n  Case \"F\": x = 0\n  Case \"T\": x = 1\nEnd Select" : 
                                                                      "Select Case Left([TRAFFIC_CD], 1)\n  Case \"+\": x = 0\n  Case \"-\": x = 1\nEnd Select");
                    calcFieldTool.expression_type = "VB";
                    gp.Execute(calcFieldTool, trackcancel);

                    AddMessage("Calculating the EdgeToPos field for historical traffic...", messages, trackcancel);

                    calcFieldTool.field = "EdgeToPos";
                    calcFieldTool.expression = "x";
                    calcFieldTool.code_block = (usesNTPFullCoverage ? "Select Case [TRAVEL_DIRECTION]\n  Case \"T\": x = 0\n  Case \"F\": x = 1\nEnd Select" :
                                                                      "Select Case Left([TRAFFIC_CD], 1)\n  Case \"-\": x = 0\n  Case \"+\": x = 1\nEnd Select");
                    calcFieldTool.expression_type = "VB";
                    gp.Execute(calcFieldTool, trackcancel);

                    // Write the average speeds to the streets feature class

                    WriteAverageSpeedsToStreets(outputFileGdbPath, streetsFeatureClassPath, usesNTPFullCoverage, gp, messages, trackcancel);

                    // Add fields for the average travel times to the Streets feature class

                    AddMessage("Creating fields for the average travel times...", messages, trackcancel);

                    addFieldTool = new AddField();
                    addFieldTool.in_table = streetsFeatureClassPath;

                    addFieldTool.field_type = "FLOAT";
                    addFieldTool.field_name = "FT_Minutes";
                    gp.Execute(addFieldTool, trackcancel);
                    addFieldTool.field_name = "TF_Minutes";
                    gp.Execute(addFieldTool, trackcancel);

                    // Calculate the average travel time fields

                    AddMessage("Calculating the FT travel times...", messages, trackcancel);

                    calcFieldTool = new CalculateField();
                    calcFieldTool.in_table = streetsFeatureClassPath;

                    calcFieldTool.field = "FT_Minutes";
                    calcFieldTool.expression = "[Meters] * 0.06 / s";
                    calcFieldTool.code_block = "s = [FT_AverageSpeed]\nIf IsNull(s) Then s = [KPH]";
                    calcFieldTool.expression_type = "VB";
                    gp.Execute(calcFieldTool, trackcancel);

                    AddMessage("Calculating the TF travel times...", messages, trackcancel);

                    calcFieldTool.field = "TF_Minutes";
                    calcFieldTool.expression = "[Meters] * 0.06 / s";
                    calcFieldTool.code_block = "s = [TF_AverageSpeed]\nIf IsNull(s) Then s = [KPH]";
                    calcFieldTool.expression_type = "VB";
                    gp.Execute(calcFieldTool, trackcancel);

                    // Offset the daily pattern fields (U,M,T,W,R,F,S) on the historical traffic join table

                    OffsetDailyPatternFields(histTrafficJoinTablePath, patternsTablePath);

                    // Add an index to the Streets feature class's FUNC_CLASS field

                    AddMessage("Indexing the FUNC_CLASS field...", messages, trackcancel);

                    addIndexTool = new AddIndex();
                    addIndexTool.in_table = streetsFeatureClassPath;
                    addIndexTool.fields = "FUNC_CLASS";
                    addIndexTool.index_name = "FUNC_CLASS";
                    gp.Execute(addIndexTool, trackcancel);
                }
                else
                {
                    // Create and calculate the generic Minutes field from speed categories

                    addFieldTool = new AddField();
                    addFieldTool.in_table = streetsFeatureClassPath;
                    addFieldTool.field_type = "FLOAT";
                    addFieldTool.field_name = "Minutes";
                    gp.Execute(addFieldTool, trackcancel);

                    AddMessage("Calculating the Minutes field...", messages, trackcancel);

                    calcFieldTool = new CalculateField();
                    calcFieldTool.in_table = streetsFeatureClassPath;
                    calcFieldTool.field = "Minutes";
                    calcFieldTool.expression = "[Meters] * 0.06 / [KPH]";
                    calcFieldTool.expression_type = "VB";
                    gp.Execute(calcFieldTool, trackcancel);
                }
                #endregion

                #region Process Live Traffic Table

                // Create the live traffic table (for 10.1 or later)

                if (fgdbVersion >= 10.1 && !inputTrafficTableValue.IsEmpty())
                {
                    // Copy over the Traffic table to the file geodatabase

                    AddMessage("Copying Traffic table to the file geodatabase...", messages, trackcancel);

                    importTableTool = new TableToTable();
                    importTableTool.in_rows = inputTrafficTableValue.GetAsText();
                    importTableTool.out_path = outputFileGdbPath;
                    importTableTool.out_name = TMCJoinTableName;
                    gp.Execute(importTableTool, trackcancel);

                    string TMCJoinTablePath = outputFileGdbPath + "\\" + TMCJoinTableName;

                    // Add the TMC field and calculate it

                    AddMessage("Calculating TMC values for live traffic...", messages, trackcancel);

                    addFieldTool = new AddField();
                    addFieldTool.in_table = TMCJoinTablePath;
                    addFieldTool.field_type = "TEXT";
                    addFieldTool.field_length = 9;
                    addFieldTool.field_name = "TMC";
                    gp.Execute(addFieldTool, trackcancel);

                    calcFieldTool = new CalculateField();
                    calcFieldTool.in_table = TMCJoinTablePath;
                    calcFieldTool.field = "TMC";
                    calcFieldTool.expression = "Right([TRAFFIC_CD], 9)";
                    calcFieldTool.expression_type = "VB";
                    gp.Execute(calcFieldTool, trackcancel);

                    AddMessage("Creating the live traffic join table...", messages, trackcancel);

                    // Add FCID, FID, and position fields to the Streets_TMC table

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

                    // Calculate the fields

                    AddMessage("Calculating the EdgeFrmPos field for live traffic...", messages, trackcancel);

                    calcFieldTool = new CalculateField();
                    calcFieldTool.in_table = TMCJoinTablePath;

                    calcFieldTool.field = "EdgeFrmPos";
                    calcFieldTool.expression = "x";
                    calcFieldTool.code_block = "Select Case Left([TRAFFIC_CD], 1)\n  Case \"+\": x = 0\n  Case \"-\": x = 1\nEnd Select";
                    calcFieldTool.expression_type = "VB";
                    gp.Execute(calcFieldTool, trackcancel);

                    AddMessage("Calculating the EdgeToPos field for live traffic...", messages, trackcancel);

                    calcFieldTool.field = "EdgeToPos";
                    calcFieldTool.expression = "x";
                    calcFieldTool.code_block = "Select Case Left([TRAFFIC_CD], 1)\n  Case \"-\": x = 0\n  Case \"+\": x = 1\nEnd Select";
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
                    addJoinTool.in_field = "LINK_ID";
                    addJoinTool.join_table = streetsFeatureClassPath;
                    addJoinTool.join_field = "LINK_ID";
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
                        // Create the Streets_Patterns table by starting with a copy of the Streets_TMC table

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
                        addFieldTool.field_type = "FLOAT";
                        addFieldTool.field_name = "KPH";
                        gp.Execute(addFieldTool, trackcancel);

                        makeTableViewTool = new MakeTableView();
                        makeTableViewTool.in_table = histTrafficJoinTablePath;
                        makeTableViewTool.out_view = "Streets_Patterns_View";
                        gp.Execute(makeTableViewTool, trackcancel);

                        addJoinTool = new AddJoin();
                        addJoinTool.in_layer_or_view = "Streets_Patterns_View";
                        addJoinTool.in_field = "EdgeFID";
                        addJoinTool.join_table = streetsFeatureClassPath;
                        addJoinTool.join_field = "OBJECTID";
                        gp.Execute(addJoinTool, trackcancel);

                        calcFieldTool = new CalculateField();
                        calcFieldTool.in_table = "Streets_Patterns_View";
                        calcFieldTool.field = HistTrafficJoinTableName + ".KPH";
                        calcFieldTool.expression = "[" + StreetsFCName + ".KPH]";
                        calcFieldTool.expression_type = "VB";
                        gp.Execute(calcFieldTool, trackcancel);

                        removeJoinTool = new RemoveJoin();
                        removeJoinTool.in_layer_or_view = "Streets_Patterns_View";
                        removeJoinTool.join_name = StreetsFCName;
                        gp.Execute(removeJoinTool, trackcancel);

                        deleteTool = new Delete();
                        deleteTool.in_data = "Streets_Patterns_View";
                        gp.Execute(deleteTool, trackcancel);

                        AddMessage("Creating and calculating the daily patterns fields on the historical traffic join table...", messages, trackcancel);

                        string[] fieldNames = new string[] { "S", "M", "T", "W", "R", "F", "S" };
                        foreach (string f in fieldNames)
                        {
                            addFieldTool = new AddField();
                            addFieldTool.in_table = histTrafficJoinTablePath;
                            addFieldTool.field_type = "SHORT";
                            addFieldTool.field_name = f;
                            gp.Execute(addFieldTool, trackcancel);

                            calcFieldTool = new CalculateField();
                            calcFieldTool.in_table = histTrafficJoinTablePath;
                            calcFieldTool.field = f;
                            calcFieldTool.expression = "1";
                            calcFieldTool.expression_type = "VB";
                            gp.Execute(calcFieldTool, trackcancel);
                        }

                        // Create the Patterns table

                        CreateNonHistoricalPatternsTable(outputFileGdbPath);
                    }
                }
                #endregion

                // Copy the Cdms table to the file geodatabase

                AddMessage("Copying the Condition/Driving Manoeuvres (CDMS) table and indexing...", messages, trackcancel);

                importTableTool = new TableToTable();
                importTableTool.in_rows = inputCdmsTableValue.GetAsText();
                importTableTool.out_path = outputFileGdbPath;
                importTableTool.out_name = "Cdms";
                gp.Execute(importTableTool, trackcancel);

                string cdmsTablePath = outputFileGdbPath + "\\Cdms";

                addIndexTool = new AddIndex();
                addIndexTool.in_table = cdmsTablePath;
                addIndexTool.fields = "COND_ID";
                addIndexTool.index_name = "COND_ID";
                gp.Execute(addIndexTool, trackcancel);

                // Copy the Rdms table to the file geodatabase

                AddMessage("Copying the Restricted Driving Manoeuvres (RDMS) table...", messages, trackcancel);

                importTableTool = new TableToTable();
                importTableTool.in_rows = inputRdmsTableValue.GetAsText();
                importTableTool.out_path = outputFileGdbPath;
                importTableTool.out_name = "Rdms";
                gp.Execute(importTableTool, trackcancel);

                string rdmsTablePath = outputFileGdbPath + "\\Rdms";

                // Add and calculate the end of link and condition type fields to the Rdms table

                AddMessage("Creating and calculating fields for the manoeuvre types and ends of link...", messages, trackcancel);

                addFieldTool = new AddField();
                addFieldTool.in_table = rdmsTablePath;
                addFieldTool.field_name = "COND_TYPE";
                addFieldTool.field_type = "LONG";
                gp.Execute(addFieldTool, trackcancel);

                addFieldTool.field_name = "END_OF_LK";
                addFieldTool.field_type = "TEXT";
                addFieldTool.field_length = 1;
                gp.Execute(addFieldTool, trackcancel);

                makeTableViewTool = new MakeTableView();
                makeTableViewTool.in_table = rdmsTablePath;
                makeTableViewTool.out_view = "Rdms_View";
                gp.Execute(makeTableViewTool, trackcancel);

                addJoinTool = new AddJoin();
                addJoinTool.in_layer_or_view = "Rdms_View";
                addJoinTool.in_field = "COND_ID";
                addJoinTool.join_table = cdmsTablePath;
                addJoinTool.join_field = "COND_ID";
                gp.Execute(addJoinTool, trackcancel);

                calcFieldTool = new CalculateField();
                calcFieldTool.in_table = "Rdms_View";
                calcFieldTool.field = "Rdms.COND_TYPE";
                calcFieldTool.expression = "[Cdms.COND_TYPE]";
                calcFieldTool.expression_type = "VB";
                gp.Execute(calcFieldTool, trackcancel);

                calcFieldTool.field = "Rdms.END_OF_LK";
                calcFieldTool.expression = "[Cdms.END_OF_LK]";
                calcFieldTool.expression_type = "VB";
                gp.Execute(calcFieldTool, trackcancel);

                removeJoinTool = new RemoveJoin();
                removeJoinTool.in_layer_or_view = "Rdms_View";
                removeJoinTool.join_name = "Cdms";
                gp.Execute(removeJoinTool, trackcancel);

                deleteTool = new Delete();
                deleteTool.in_data = "Rdms_View";
                gp.Execute(deleteTool, trackcancel);

                // Extract only the gates (condition type 4) and prohibitied manoeuvres (condition type 7)
                // If using Transport, also extract the Transport manoeuvres (condition type 26)

                AddMessage("Extracting restricted driving manoeuvres...", messages, trackcancel);

                string prohibRdmsWEndOfLkTablePath = outputFileGdbPath + "\\ProhibRdmsWEndOfLk";

                tableSelectTool = new TableSelect();
                tableSelectTool.in_table = rdmsTablePath;
                tableSelectTool.out_table = prohibRdmsWEndOfLkTablePath;
                if (usesTransport)
                    tableSelectTool.where_clause = "COND_TYPE IN (4, 7, 26)";
                else
                    tableSelectTool.where_clause = "COND_TYPE IN (4, 7)";
                gp.Execute(tableSelectTool, trackcancel);

                AddMessage("Creating turn feature class...", messages, trackcancel);

                // Create the turn feature class

                string tempStatsTablePath = outputFileGdbPath + "\\tempStatsTable";

                Statistics statsTool = new Statistics();
                statsTool.in_table = prohibRdmsWEndOfLkTablePath;
                statsTool.out_table = tempStatsTablePath;
                statsTool.statistics_fields = "SEQ_NUMBER MAX";
                gp.Execute(statsTool, null);

                CreateAndPopulateTurnFeatureClass(outputFileGdbPath, fdsName, "ProhibRdmsWEndOfLk", "tempStatsTable",
                                                  messages, trackcancel);

                deleteTool = new Delete();
                deleteTool.in_data = tempStatsTablePath;
                gp.Execute(deleteTool, trackcancel);

                string pathToTurnFC = pathToFds + "\\" + TurnFCName;

                // Create and calculate condition type and access restriction fields on the turn feature class

                addFieldTool = new AddField();
                addFieldTool.in_table = pathToTurnFC;
                addFieldTool.field_name = "COND_TYPE";
                addFieldTool.field_type = "LONG";
                gp.Execute(addFieldTool, trackcancel);

                AddMessage("Creating access restriction fields on the turn feature class...", messages, trackcancel);

                foreach (string arFieldSuffix in arFieldSuffixes)
                {
                    addFieldTool = new AddField();
                    addFieldTool.in_table = pathToTurnFC;
                    addFieldTool.field_name = "AR_" + arFieldSuffix;
                    addFieldTool.field_type = "TEXT";
                    addFieldTool.field_length = 1;
                    gp.Execute(addFieldTool, trackcancel);
                }

                makeFeatureLayerTool = new MakeFeatureLayer();
                makeFeatureLayerTool.in_features = pathToTurnFC;
                makeFeatureLayerTool.out_layer = "Turn_Layer";
                gp.Execute(makeFeatureLayerTool, trackcancel);

                addJoinTool = new AddJoin();
                addJoinTool.in_layer_or_view = "Turn_Layer";
                addJoinTool.in_field = "COND_ID";
                addJoinTool.join_table = cdmsTablePath;
                addJoinTool.join_field = "COND_ID";
                gp.Execute(addJoinTool, trackcancel);

                AddMessage("Calculating the COND_TYPE field on the turn feature class...", messages, trackcancel);

                calcFieldTool = new CalculateField();
                calcFieldTool.in_table = "Turn_Layer";
                calcFieldTool.field = TurnFCName + ".COND_TYPE";
                calcFieldTool.expression = "[Cdms.COND_TYPE]";
                calcFieldTool.expression_type = "VB";
                gp.Execute(calcFieldTool, trackcancel);

                foreach (string arFieldSuffix in arFieldSuffixes)
                {
                    AddMessage("Calculating the AR_" + arFieldSuffix + " field on the turn feature class...", messages, trackcancel);

                    calcFieldTool = new CalculateField();
                    calcFieldTool.in_table = "Turn_Layer";
                    calcFieldTool.field = TurnFCName + ".AR_" + arFieldSuffix;
                    calcFieldTool.expression = "[Cdms.AR_" + arFieldSuffix + "]";
                    calcFieldTool.expression_type = "VB";
                    gp.Execute(calcFieldTool, trackcancel);
                }

                removeJoinTool = new RemoveJoin();
                removeJoinTool.in_layer_or_view = "Turn_Layer";
                removeJoinTool.join_name = "Cdms";
                gp.Execute(removeJoinTool, trackcancel);

                deleteTool = new Delete();
                deleteTool.in_data = "Turn_Layer";
                gp.Execute(deleteTool, trackcancel);

                deleteTool.in_data = cdmsTablePath;
                gp.Execute(deleteTool, trackcancel);

                deleteTool.in_data = prohibRdmsWEndOfLkTablePath;
                gp.Execute(deleteTool, trackcancel);

                GC.Collect();

                #region Process Transport Tables

                if (usesTransport)
                {
                    string nonDimensionalCndModTablePath = outputFileGdbPath + "\\nonDimensionalCndMod";
                    string dimensionalCndModTablePath = outputFileGdbPath + "\\dimensionalCndMod";

                    if (!(inputUSCndModTableValue.IsEmpty()))
                    {
                        // Extract the Transport information for the US

                        AddMessage("Extracting the US non-dimensional conditions...", messages, trackcancel);

                        tableSelectTool = new TableSelect();
                        tableSelectTool.in_table = inputUSCndModTableValue.GetAsText();
                        tableSelectTool.out_table = nonDimensionalCndModTablePath;
                        tableSelectTool.where_clause = "MOD_TYPE IN (38, 39, 46, 49, 60, 75)";
                        gp.Execute(tableSelectTool, trackcancel);

                        AddMessage("Extracting the US dimensional conditions...", messages, trackcancel);

                        tableSelectTool.out_table = dimensionalCndModTablePath;
                        tableSelectTool.where_clause = "MOD_TYPE IN (41, 42, 43, 44, 45, 48, 81)";
                        gp.Execute(tableSelectTool, trackcancel);

                        // Create a new field to hold the dimension in a common unit (m or kg or KPH)

                        addFieldTool = new AddField();
                        addFieldTool.in_table = dimensionalCndModTablePath;
                        addFieldTool.field_name = "MetersOrKilogramsOrKPH";
                        addFieldTool.field_type = "DOUBLE";
                        gp.Execute(addFieldTool, trackcancel);

                        AddMessage("Calculating common units of measure...", messages, trackcancel);

                        calcFieldTool = new CalculateField();
                        calcFieldTool.in_table = dimensionalCndModTablePath;
                        calcFieldTool.field = "MetersOrKilogramsOrKPH";
                        calcFieldTool.code_block = "x = CLng([MOD_VAL])\nSelect Case [MOD_TYPE]\n" +
                                                   "  Case 41, 44, 45, 81: x = x * 0.0254\n" +
                                                   "  Case 42, 43: x = x * 0.45359237\n" +
                                                   "  Case 48: x = x * 1.609344\nEnd Select";
                        calcFieldTool.expression = "x";
                        calcFieldTool.expression_type = "VB";
                        gp.Execute(calcFieldTool, trackcancel);

                        if (!(inputNonUSCndModTableValue.IsEmpty()))
                        {
                            string tempNonDimensionalCndModTablePath = outputFileGdbPath + "\\tempNonDimensionalCndMod";
                            string tempDimensionalCndModTablePath = outputFileGdbPath + "\\tempDimensionalCndMod";

                            // Extract the Transport information for outside the US to temporary tables

                            AddMessage("Extracting the non-US non-dimensional conditions...", messages, trackcancel);

                            tableSelectTool = new TableSelect();
                            tableSelectTool.in_table = inputNonUSCndModTableValue.GetAsText();
                            tableSelectTool.out_table = tempNonDimensionalCndModTablePath;
                            tableSelectTool.where_clause = "MOD_TYPE IN (38, 39, 46, 49, 60, 75)";
                            gp.Execute(tableSelectTool, trackcancel);

                            AddMessage("Extracting the non-US dimensional conditions...", messages, trackcancel);

                            tableSelectTool.out_table = tempDimensionalCndModTablePath;
                            tableSelectTool.where_clause = "MOD_TYPE IN (41, 42, 43, 44, 45, 48, 81)";
                            gp.Execute(tableSelectTool, trackcancel);

                            // Create a new field to hold the dimension in a common unit (m or kg or KPH)

                            addFieldTool = new AddField();
                            addFieldTool.in_table = tempDimensionalCndModTablePath;
                            addFieldTool.field_name = "MetersOrKilogramsOrKPH";
                            addFieldTool.field_type = "DOUBLE";
                            gp.Execute(addFieldTool, trackcancel);

                            AddMessage("Calculating common units of measure...", messages, trackcancel);

                            calcFieldTool = new CalculateField();
                            calcFieldTool.in_table = tempDimensionalCndModTablePath;
                            calcFieldTool.field = "MetersOrKilogramsOrKPH";
                            calcFieldTool.code_block = "x = CLng([MOD_VAL])\nSelect Case [MOD_TYPE]\n" +
                                                       "  Case 41, 44, 45, 81: x = x * 0.01\nEnd Select";
                            calcFieldTool.expression = "x";
                            calcFieldTool.expression_type = "VB";
                            gp.Execute(calcFieldTool, trackcancel);

                            // Append the temporary tables to the main table containing US attributes

                            Append appendTool = new Append();
                            appendTool.inputs = tempNonDimensionalCndModTablePath;
                            appendTool.target = nonDimensionalCndModTablePath;
                            gp.Execute(appendTool, trackcancel);

                            appendTool.inputs = tempDimensionalCndModTablePath;
                            appendTool.target = dimensionalCndModTablePath;
                            gp.Execute(appendTool, trackcancel);

                            deleteTool = new Delete();
                            deleteTool.in_data = tempNonDimensionalCndModTablePath;
                            gp.Execute(deleteTool, trackcancel);
                            deleteTool.in_data = tempDimensionalCndModTablePath;
                            gp.Execute(deleteTool, trackcancel);
                        }
                    }
                    else
                    {
                        // Extract the Transport information for outside the US

                        AddMessage("Extracting the non-US non-dimensional conditions...", messages, trackcancel);

                        tableSelectTool = new TableSelect();
                        tableSelectTool.in_table = inputNonUSCndModTableValue.GetAsText();
                        tableSelectTool.out_table = nonDimensionalCndModTablePath;
                        tableSelectTool.where_clause = "MOD_TYPE IN (38, 39, 46, 49, 60, 75)";
                        gp.Execute(tableSelectTool, trackcancel);

                        AddMessage("Extracting the non-US dimensional conditions...", messages, trackcancel);

                        tableSelectTool.out_table = dimensionalCndModTablePath;
                        tableSelectTool.where_clause = "MOD_TYPE IN (41, 42, 43, 44, 45, 48, 81)";
                        gp.Execute(tableSelectTool, trackcancel);

                        // Create a new field to hold the dimension in a common unit (m or kg or KPH)

                        addFieldTool = new AddField();
                        addFieldTool.in_table = dimensionalCndModTablePath;
                        addFieldTool.field_name = "MetersOrKilogramsOrKPH";
                        addFieldTool.field_type = "DOUBLE";
                        gp.Execute(addFieldTool, trackcancel);

                        AddMessage("Calculating common units of measure...", messages, trackcancel);

                        calcFieldTool = new CalculateField();
                        calcFieldTool.in_table = dimensionalCndModTablePath;
                        calcFieldTool.field = "MetersOrKilogramsOrKPH";
                        calcFieldTool.code_block = "x = CLng([MOD_VAL])\nSelect Case [MOD_TYPE]\n" +
                                                   "  Case 41, 44, 45, 81: x = x * 0.01\nEnd Select";
                        calcFieldTool.expression = "x";
                        calcFieldTool.expression_type = "VB";
                        gp.Execute(calcFieldTool, trackcancel);
                    }

                    // Create a table for looking up LINK_IDs from COND_IDs

                    AddMessage("Creating and indexing LINK_ID/COND_ID look-up table for preferred roads...", messages, trackcancel);

                    string preferredLinkIDLookupTablePath = outputFileGdbPath + "\\preferredLinkIDLookupTable";

                    tableSelectTool = new TableSelect();
                    tableSelectTool.in_table = inputCdmsTableValue.GetAsText();
                    tableSelectTool.out_table = preferredLinkIDLookupTablePath;
                    tableSelectTool.where_clause = "COND_TYPE IN (25, 27)";
                    gp.Execute(tableSelectTool, trackcancel);

                    addIndexTool = new AddIndex();
                    addIndexTool.in_table = preferredLinkIDLookupTablePath;
                    addIndexTool.fields = "COND_ID";
                    addIndexTool.index_name = "COND_ID";
                    gp.Execute(addIndexTool, trackcancel);

                    // Create a table for looking up the condition's direction

                    AddMessage("Creating and indexing direction look-up table for preferred roads...", messages, trackcancel);

                    string preferredDirectionLookupTablePath = outputFileGdbPath + "\\preferredDirectionLookupTable";

                    tableSelectTool = new TableSelect();
                    tableSelectTool.in_table = nonDimensionalCndModTablePath;
                    tableSelectTool.out_table = preferredDirectionLookupTablePath;
                    tableSelectTool.where_clause = "MOD_TYPE = 60";
                    gp.Execute(tableSelectTool, trackcancel);

                    addIndexTool = new AddIndex();
                    addIndexTool.in_table = preferredDirectionLookupTablePath;
                    addIndexTool.fields = "COND_ID";
                    addIndexTool.index_name = "COND_ID";
                    gp.Execute(addIndexTool, trackcancel);

                    // Create a table for looking up LINK_IDs from COND_IDs

                    AddMessage("Creating and indexing LINK_ID/COND_ID look-up table for restrictions...", messages, trackcancel);

                    string restrictionLinkIDLookupTablePath = outputFileGdbPath + "\\restrictionLinkIDLookupTable";

                    tableSelectTool = new TableSelect();
                    tableSelectTool.in_table = inputCdmsTableValue.GetAsText();
                    tableSelectTool.out_table = restrictionLinkIDLookupTablePath;
                    tableSelectTool.where_clause = "COND_TYPE = 23";
                    gp.Execute(tableSelectTool, trackcancel);

                    addIndexTool = new AddIndex();
                    addIndexTool.in_table = restrictionLinkIDLookupTablePath;
                    addIndexTool.fields = "COND_ID";
                    addIndexTool.index_name = "COND_ID";
                    gp.Execute(addIndexTool, trackcancel);

                    // Create a table for looking up the condition's direction

                    AddMessage("Creating and indexing direction look-up table for restrictions...", messages, trackcancel);

                    string restrictionDirectionLookupTablePath = outputFileGdbPath + "\\restrictionDirectionLookupTable";

                    tableSelectTool = new TableSelect();
                    tableSelectTool.in_table = nonDimensionalCndModTablePath;
                    tableSelectTool.out_table = restrictionDirectionLookupTablePath;
                    tableSelectTool.where_clause = "MOD_TYPE = 38";
                    gp.Execute(tableSelectTool, trackcancel);

                    addIndexTool = new AddIndex();
                    addIndexTool.in_table = restrictionDirectionLookupTablePath;
                    addIndexTool.fields = "COND_ID";
                    addIndexTool.index_name = "COND_ID";
                    gp.Execute(addIndexTool, trackcancel);

                    makeFeatureLayerTool = new MakeFeatureLayer();
                    makeFeatureLayerTool.in_features = streetsFeatureClassPath;
                    makeFeatureLayerTool.out_layer = "Streets_Layer";
                    gp.Execute(makeFeatureLayerTool, trackcancel);

                    CreateAndPopulateTruckFCOverrideField(outputFileGdbPath, gp, messages, trackcancel);
                    
                    if (fgdbVersion >= 10.1)
                    {
                        // Create and calculate the preferred fields for Streets

                        CreateAndPopulateTransportFieldsOnStreets(outputFileGdbPath, false, false, true, "STAAPreferred",
                                                                  "MOD_TYPE = 49 AND MOD_VAL = '1'", gp, messages, trackcancel);
                        CreateAndPopulateTransportFieldsOnStreets(outputFileGdbPath, false, false, true, "TruckDesignatedPreferred",
                                                                  "MOD_TYPE = 49 AND MOD_VAL = '2'", gp, messages, trackcancel);
                        CreateAndPopulateTransportFieldsOnStreets(outputFileGdbPath, false, false, true, "NRHMPreferred",
                                                                  "MOD_TYPE = 49 AND MOD_VAL = '3'", gp, messages, trackcancel);
                        CreateAndPopulateTransportFieldsOnStreets(outputFileGdbPath, false, false, true, "ExplosivesPreferred",
                                                                  "MOD_TYPE = 49 AND MOD_VAL = '4'", gp, messages, trackcancel);
                        CreateAndPopulateTransportFieldsOnStreets(outputFileGdbPath, false, false, true, "PIHPreferred",
                                                                  "MOD_TYPE = 49 AND MOD_VAL = '5'", gp, messages, trackcancel);
                        CreateAndPopulateTransportFieldsOnStreets(outputFileGdbPath, false, false, true, "MedicalWastePreferred",
                                                                  "MOD_TYPE = 49 AND MOD_VAL = '6'", gp, messages, trackcancel);
                        CreateAndPopulateTransportFieldsOnStreets(outputFileGdbPath, false, false, true, "RadioactivePreferred",
                                                                  "MOD_TYPE = 49 AND MOD_VAL = '7'", gp, messages, trackcancel);
                        CreateAndPopulateTransportFieldsOnStreets(outputFileGdbPath, false, false, true, "HazmatPreferred",
                                                                  "MOD_TYPE = 49 AND MOD_VAL = '8'", gp, messages, trackcancel);
                        CreateAndPopulateTransportFieldsOnStreets(outputFileGdbPath, false, false, true, "LocallyPreferred",
                                                                  "MOD_TYPE = 49 AND MOD_VAL = '9'", gp, messages, trackcancel);

                        if (createArcGISOnlineNetworkAttributes)
                        {
                            CreateAndPopulateAGOLTextTransportFieldsOnStreets(outputFileGdbPath, "PreferredTruckRoute",
                                                                              new string[] { "STAAPreferred", "TruckDesignatedPreferred", "LocallyPreferred" },
                                                                              gp, messages, trackcancel);
                            CreateAndPopulateAGOLTextTransportFieldsOnStreets(outputFileGdbPath, "PreferredHazmatRoute",
                                                                              new string[] { "NRHMPreferred", "ExplosivesPreferred", "PIHPreferred", "MedicalWastePreferred", "RadioactivePreferred", "HazmatPreferred" },
                                                                              gp, messages, trackcancel);
                        }
                    }

                    // Create and calculate the HazMat restriction fields for Streets

                    CreateAndPopulateTransportFieldsOnStreets(outputFileGdbPath, false, false, false, "ExplosivesProhibited",
                                                              "MOD_TYPE = 39 AND MOD_VAL = '1'", gp, messages, trackcancel);
                    CreateAndPopulateTransportFieldsOnStreets(outputFileGdbPath, false, false, false, "GasProhibited",
                                                              "MOD_TYPE = 39 AND MOD_VAL = '2'", gp, messages, trackcancel);
                    CreateAndPopulateTransportFieldsOnStreets(outputFileGdbPath, false, false, false, "FlammableProhibited",
                                                              "MOD_TYPE = 39 AND MOD_VAL = '3'", gp, messages, trackcancel);
                    CreateAndPopulateTransportFieldsOnStreets(outputFileGdbPath, false, false, false, "CombustibleProhibited",
                                                              "MOD_TYPE = 39 AND MOD_VAL = '4'", gp, messages, trackcancel);
                    CreateAndPopulateTransportFieldsOnStreets(outputFileGdbPath, false, false, false, "OrganicProhibited",
                                                              "MOD_TYPE = 39 AND MOD_VAL = '5'", gp, messages, trackcancel);
                    CreateAndPopulateTransportFieldsOnStreets(outputFileGdbPath, false, false, false, "PoisonProhibited",
                                                              "MOD_TYPE = 39 AND MOD_VAL = '6'", gp, messages, trackcancel);
                    CreateAndPopulateTransportFieldsOnStreets(outputFileGdbPath, false, false, false, "RadioactiveProhibited",
                                                              "MOD_TYPE = 39 AND MOD_VAL = '7'", gp, messages, trackcancel);
                    CreateAndPopulateTransportFieldsOnStreets(outputFileGdbPath, false, false, false, "CorrosiveProhibited",
                                                              "MOD_TYPE = 39 AND MOD_VAL = '8'", gp, messages, trackcancel);
                    CreateAndPopulateTransportFieldsOnStreets(outputFileGdbPath, false, false, false, "OtherHazmatProhibited",
                                                              "MOD_TYPE = 39 AND MOD_VAL = '9'", gp, messages, trackcancel);
                    CreateAndPopulateTransportFieldsOnStreets(outputFileGdbPath, false, false, false, "AnyHazmatProhibited",
                                                              "MOD_TYPE = 39 AND MOD_VAL = '20'", gp, messages, trackcancel);
                    CreateAndPopulateTransportFieldsOnStreets(outputFileGdbPath, false, false, false, "PIHProhibited",
                                                              "MOD_TYPE = 39 AND MOD_VAL = '21'", gp, messages, trackcancel);
                    CreateAndPopulateTransportFieldsOnStreets(outputFileGdbPath, false, false, false, "HarmfulToWaterProhibited",
                                                              "MOD_TYPE = 39 AND MOD_VAL = '22'", gp, messages, trackcancel);
                    CreateAndPopulateTransportFieldsOnStreets(outputFileGdbPath, false, false, false, "ExplosiveAndFlammableProhibited",
                                                              "MOD_TYPE = 39 AND MOD_VAL = '23'", gp, messages, trackcancel);

                    if (createArcGISOnlineNetworkAttributes)
                    {
                        CreateAndPopulateAGOLTextTransportFieldsOnStreets(outputFileGdbPath, "AGOL_AnyHazmatProhibited",
                                                                          new string[] { "ExplosivesProhibited", "GasProhibited", "FlammableProhibited", "CombustibleProhibited", "OrganicProhibited", "PoisonProhibited", "RadioactiveProhibited", "CorrosiveProhibited",
                                                                                         "OtherHazmatProhibited", "AnyHazmatProhibited", "PIHProhibited", "HarmfulToWaterProhibited", "ExplosiveAndFlammableProhibited" },
                                                                          gp, messages, trackcancel);
                    }

                    // Create and calculate the other restriction fields for Streets

                    CreateAndPopulateTransportFieldsOnStreets(outputFileGdbPath, true, false, false, "HeightLimit_Meters",
                                                              "MOD_TYPE = 41", gp, messages, trackcancel);
                    CreateAndPopulateTransportFieldsOnStreets(outputFileGdbPath, true, false, false, "WeightLimit_Kilograms",
                                                              "MOD_TYPE = 42", gp, messages, trackcancel);
                    CreateAndPopulateTransportFieldsOnStreets(outputFileGdbPath, true, false, false, "WeightLimitPerAxle_Kilograms",
                                                              "MOD_TYPE = 43", gp, messages, trackcancel);
                    CreateAndPopulateTransportFieldsOnStreets(outputFileGdbPath, true, false, false, "LengthLimit_Meters",
                                                              "MOD_TYPE = 44", gp, messages, trackcancel);
                    CreateAndPopulateTransportFieldsOnStreets(outputFileGdbPath, true, false, false, "WidthLimit_Meters",
                                                              "MOD_TYPE = 45", gp, messages, trackcancel);
                    CreateAndPopulateTransportFieldsOnStreets(outputFileGdbPath, false, true, false, "MaxTrailersAllowedOnTruck",
                                                              "MOD_TYPE = 46 AND MOD_VAL IN ('1', '2', '3')", gp, messages, trackcancel);
                    CreateAndPopulateTransportFieldsOnStreets(outputFileGdbPath, false, false, false, "SemiOrTractorWOneOrMoreTrailersProhibited",
                                                              "MOD_TYPE = 46 AND MOD_VAL = '4'", gp, messages, trackcancel);
                    CreateAndPopulateTransportFieldsOnStreets(outputFileGdbPath, false, true, false, "MaxAxlesAllowed",
                                                              "MOD_TYPE = 75 AND MOD_VAL IN ('1', '2', '3', '4', '5')", gp, messages, trackcancel);
                    CreateAndPopulateTransportFieldsOnStreets(outputFileGdbPath, false, false, false, "SingleAxleProhibited",
                                                              "MOD_TYPE = 75 AND MOD_VAL = '6'", gp, messages, trackcancel);
                    CreateAndPopulateTransportFieldsOnStreets(outputFileGdbPath, false, false, false, "TandemAxleProhibited",
                                                              "MOD_TYPE = 75 AND MOD_VAL = '7'", gp, messages, trackcancel);
                    CreateAndPopulateTransportFieldsOnStreets(outputFileGdbPath, true, false, false, "KingpinToRearAxleLengthLimit_Meters",
                                                              "MOD_TYPE = 81", gp, messages, trackcancel);

                    // Create and calculate the truck speed fields for Streets

                    CreateAndPopulateTransportFieldsOnStreets(outputFileGdbPath, true, false, true, "TruckKPH",
                                                              "MOD_TYPE = 48", gp, messages, trackcancel);

                    deleteTool = new Delete();
                    deleteTool.in_data = "Streets_Layer";
                    gp.Execute(deleteTool, trackcancel);

                    deleteTool.in_data = preferredLinkIDLookupTablePath;
                    gp.Execute(deleteTool, trackcancel);
                    deleteTool.in_data = preferredDirectionLookupTablePath;
                    gp.Execute(deleteTool, trackcancel);

                    deleteTool.in_data = restrictionLinkIDLookupTablePath;
                    gp.Execute(deleteTool, trackcancel);
                    deleteTool.in_data = restrictionDirectionLookupTablePath;
                    gp.Execute(deleteTool, trackcancel);

                    makeFeatureLayerTool = new MakeFeatureLayer();
                    makeFeatureLayerTool.in_features = pathToTurnFC;
                    makeFeatureLayerTool.out_layer = "RestrictedTurns_Layer";
                    gp.Execute(makeFeatureLayerTool, trackcancel);

                    // Create and calculate the HazMat restriction fields for RestrictedTurns

                    CreateAndPopulateTransportFieldOnTurns(outputFileGdbPath, false, false, "ExplosivesProhibited",
                                                           "MOD_TYPE = 39 AND MOD_VAL = '1'", gp, messages, trackcancel);
                    CreateAndPopulateTransportFieldOnTurns(outputFileGdbPath, false, false, "GasProhibited",
                                                           "MOD_TYPE = 39 AND MOD_VAL = '2'", gp, messages, trackcancel);
                    CreateAndPopulateTransportFieldOnTurns(outputFileGdbPath, false, false, "FlammableProhibited",
                                                           "MOD_TYPE = 39 AND MOD_VAL = '3'", gp, messages, trackcancel);
                    CreateAndPopulateTransportFieldOnTurns(outputFileGdbPath, false, false, "CombustibleProhibited",
                                                           "MOD_TYPE = 39 AND MOD_VAL = '4'", gp, messages, trackcancel);
                    CreateAndPopulateTransportFieldOnTurns(outputFileGdbPath, false, false, "OrganicProhibited",
                                                           "MOD_TYPE = 39 AND MOD_VAL = '5'", gp, messages, trackcancel);
                    CreateAndPopulateTransportFieldOnTurns(outputFileGdbPath, false, false, "PoisonProhibited",
                                                           "MOD_TYPE = 39 AND MOD_VAL = '6'", gp, messages, trackcancel);
                    CreateAndPopulateTransportFieldOnTurns(outputFileGdbPath, false, false, "RadioactiveProhibited",
                                                           "MOD_TYPE = 39 AND MOD_VAL = '7'", gp, messages, trackcancel);
                    CreateAndPopulateTransportFieldOnTurns(outputFileGdbPath, false, false, "CorrosiveProhibited",
                                                           "MOD_TYPE = 39 AND MOD_VAL = '8'", gp, messages, trackcancel);
                    CreateAndPopulateTransportFieldOnTurns(outputFileGdbPath, false, false, "OtherHazmatProhibited",
                                                           "MOD_TYPE = 39 AND MOD_VAL = '9'", gp, messages, trackcancel);
                    CreateAndPopulateTransportFieldOnTurns(outputFileGdbPath, false, false, "AnyHazmatProhibited",
                                                           "MOD_TYPE = 39 AND MOD_VAL = '20'", gp, messages, trackcancel);
                    CreateAndPopulateTransportFieldOnTurns(outputFileGdbPath, false, false, "PIHProhibited",
                                                           "MOD_TYPE = 39 AND MOD_VAL = '21'", gp, messages, trackcancel);
                    CreateAndPopulateTransportFieldOnTurns(outputFileGdbPath, false, false, "HarmfulToWaterProhibited",
                                                           "MOD_TYPE = 39 AND MOD_VAL = '22'", gp, messages, trackcancel);
                    CreateAndPopulateTransportFieldOnTurns(outputFileGdbPath, false, false, "ExplosiveAndFlammableProhibited",
                                                           "MOD_TYPE = 39 AND MOD_VAL = '23'", gp, messages, trackcancel);

                    if (createArcGISOnlineNetworkAttributes)
                    {
                        CreateAndPopulateAGOLTextTransportFieldOnTurns(outputFileGdbPath, "AGOL_AnyHazmatProhibited",
                                                                       new string[] { "ExplosivesProhibited", "GasProhibited", "FlammableProhibited", "CombustibleProhibited", "OrganicProhibited", "PoisonProhibited", "RadioactiveProhibited", "CorrosiveProhibited",
                                                                                      "OtherHazmatProhibited", "AnyHazmatProhibited", "PIHProhibited", "HarmfulToWaterProhibited", "ExplosiveAndFlammableProhibited" },
                                                                       gp, messages, trackcancel);
                    }

                    // Create and calculate the other restriction fields for RestrictedTurns

                    CreateAndPopulateTransportFieldOnTurns(outputFileGdbPath, true, false, "HeightLimit_Meters",
                                                           "MOD_TYPE = 41", gp, messages, trackcancel);
                    CreateAndPopulateTransportFieldOnTurns(outputFileGdbPath, true, false, "WeightLimit_Kilograms",
                                                           "MOD_TYPE = 42", gp, messages, trackcancel);
                    CreateAndPopulateTransportFieldOnTurns(outputFileGdbPath, true, false, "WeightLimitPerAxle_Kilograms",
                                                           "MOD_TYPE = 43", gp, messages, trackcancel);
                    CreateAndPopulateTransportFieldOnTurns(outputFileGdbPath, true, false, "LengthLimit_Meters",
                                                           "MOD_TYPE = 44", gp, messages, trackcancel);
                    CreateAndPopulateTransportFieldOnTurns(outputFileGdbPath, true, false, "WidthLimit_Meters",
                                                           "MOD_TYPE = 45", gp, messages, trackcancel);
                    CreateAndPopulateTransportFieldOnTurns(outputFileGdbPath, false, true, "MaxTrailersAllowedOnTruck",
                                                           "MOD_TYPE = 46 AND MOD_VAL IN ('1', '2', '3')", gp, messages, trackcancel);
                    CreateAndPopulateTransportFieldOnTurns(outputFileGdbPath, false, false, "SemiOrTractorWOneOrMoreTrailersProhibited",
                                                           "MOD_TYPE = 46 AND MOD_VAL = '4'", gp, messages, trackcancel);
                    CreateAndPopulateTransportFieldOnTurns(outputFileGdbPath, false, true, "MaxAxlesAllowed",
                                                           "MOD_TYPE = 75 AND MOD_VAL IN ('1', '2', '3', '4', '5')", gp, messages, trackcancel);
                    CreateAndPopulateTransportFieldOnTurns(outputFileGdbPath, false, false, "SingleAxleProhibited",
                                                           "MOD_TYPE = 75 AND MOD_VAL = '6'", gp, messages, trackcancel);
                    CreateAndPopulateTransportFieldOnTurns(outputFileGdbPath, false, false, "TandemAxleProhibited",
                                                           "MOD_TYPE = 75 AND MOD_VAL = '7'", gp, messages, trackcancel);
                    CreateAndPopulateTransportFieldOnTurns(outputFileGdbPath, true, false, "KingpinToRearAxleLengthLimit_Meters",
                                                           "MOD_TYPE = 81", gp, messages, trackcancel);

                    deleteTool = new Delete();
                    deleteTool.in_data = "RestrictedTurns_Layer";
                    gp.Execute(deleteTool, trackcancel);

                    // Create and calculate the AllTransportProhibited field

                    addFieldTool = new AddField();
                    addFieldTool.in_table = pathToTurnFC;
                    addFieldTool.field_name = "AllTransportProhibited";
                    addFieldTool.field_type = "TEXT";
                    addFieldTool.field_length = 1;
                    gp.Execute(addFieldTool, trackcancel);

                    makeFeatureLayerTool = new MakeFeatureLayer();
                    makeFeatureLayerTool.in_features = pathToTurnFC;
                    makeFeatureLayerTool.out_layer = "RestrictedTurns_Layer";
                    makeFeatureLayerTool.where_clause = "COND_TYPE = 26 AND ExplosivesProhibited IS NULL AND GasProhibited IS NULL AND " +
                                                        "FlammableProhibited IS NULL AND CombustibleProhibited IS NULL AND OrganicProhibited IS NULL AND " +
                                                        "PoisonProhibited IS NULL AND RadioactiveProhibited IS NULL AND CorrosiveProhibited IS NULL AND " +
                                                        "OtherHazmatProhibited IS NULL AND AnyHazmatProhibited IS NULL AND PIHProhibited IS NULL AND " +
                                                        "HarmfulToWaterProhibited IS NULL AND ExplosiveAndFlammableProhibited IS NULL AND " +
                                                        "HeightLimit_Meters IS NULL AND WeightLimit_Kilograms IS NULL AND WeightLimitPerAxle_Kilograms IS NULL AND " +
                                                        "LengthLimit_Meters IS NULL AND WidthLimit_Meters IS NULL AND MaxTrailersAllowedOnTruck IS NULL AND " +
                                                        "SemiOrTractorWOneOrMoreTrailersProhibited IS NULL AND MaxAxlesAllowed IS NULL AND " +
                                                        "SingleAxleProhibited IS NULL AND TandemAxleProhibited IS NULL AND KingpinToRearAxleLengthLimit_Meters IS NULL";
                    gp.Execute(makeFeatureLayerTool, trackcancel);

                    AddMessage("Calculating the AllTransportProhibited field...", messages, trackcancel);

                    calcFieldTool = new CalculateField();
                    calcFieldTool.in_table = "RestrictedTurns_Layer";
                    calcFieldTool.field = "AllTransportProhibited";
                    calcFieldTool.expression = "\"Y\"";
                    calcFieldTool.expression_type = "VB";
                    gp.Execute(calcFieldTool, trackcancel);

                    deleteTool = new Delete();
                    deleteTool.in_data = "RestrictedTurns_Layer";
                    gp.Execute(deleteTool, trackcancel);

                    deleteTool.in_data = nonDimensionalCndModTablePath;
                    gp.Execute(deleteTool, trackcancel);
                    deleteTool.in_data = dimensionalCndModTablePath;
                    gp.Execute(deleteTool, trackcancel);

                    GC.Collect();
                }
                #endregion

                // Extract the special explications (condition type 9) from the Rdms Table and create RoadSplits table

                AddMessage("Creating RoadSplits table...", messages, trackcancel);

                string specialExplicationRdmsWEndOfLkTablePath = outputFileGdbPath + "\\SpecialExplicationRdmsWEndOfLk";

                tableSelectTool = new TableSelect();
                tableSelectTool.in_table = rdmsTablePath;
                tableSelectTool.out_table = specialExplicationRdmsWEndOfLkTablePath;
                tableSelectTool.where_clause = "COND_TYPE = 9";
                gp.Execute(tableSelectTool, trackcancel);

                deleteTool.in_data = rdmsTablePath;
                gp.Execute(deleteTool, trackcancel);

                CreateRoadSplitsTable("SpecialExplicationRdmsWEndOfLk", outputFileGdbPath, messages, trackcancel);

                deleteTool.in_data = specialExplicationRdmsWEndOfLkTablePath;
                gp.Execute(deleteTool, trackcancel);

                // Create Signpost feature class and table

                AddMessage("Creating signpost feature class and table...", messages, trackcancel);

                CreateSignposts(inputSignsTableValue.GetAsText(), outputFileGdbPath, messages, trackcancel);

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
                                             createArcGISOnlineNetworkAttributes, timeZoneIDBaseFieldName, directedTimeZoneIDFields, commonTimeZone,
                                             usesHistoricalTraffic, trafficFeedLocation, usesTransport);

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
                return "Process NAVSTREETS™ Street Data";
            }
        }

        public string MetadataFile
        {
            get
            {
                string filePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                return System.IO.Path.Combine(filePath, "ProcessNavStreetsData_nasample.xml");
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
                return "ProcessNavStreetsData";
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

        private void CalculateMetersKPHAndLanguageFields(string outputFileGdbPath)
        {
            // Open the Streets feature class and find the Meters and Language fields

            Type factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.FileGDBWorkspaceFactory");
            var gdbWSF = Activator.CreateInstance(factoryType) as IWorkspaceFactory;
            var gdbFWS = gdbWSF.OpenFromFile(outputFileGdbPath, 0) as IFeatureWorkspace;
            IFeatureClass fc = gdbFWS.OpenFeatureClass(StreetsFCName);
            int metersField = fc.FindField("Meters");
            int contrAccField = fc.FindField("CONTRACC");
            int speedCatField = fc.FindField("SPEED_CAT");
            int kphField = fc.FindField("KPH");
            int suppliedLangField = fc.FindField("ST_LANGCD");
            int suppliedLangAltField = fc.FindField("ST_LANGCD_Alt");
            int languageField = fc.FindField("Language");
            int languageAltField = fc.FindField("Language_Alt");

            // Define a LinearUnit for meters

            ILinearUnit metersUnit = new LinearUnitClass();
            double metersPerUnit = 1.0;
            ((ILinearUnitEdit)metersUnit).DefineEx("Meter", "Meter", "M", "Meter is the linear unit", ref metersPerUnit);

            // Get the language lookup hash

            System.Collections.Hashtable langLookup = CreateLanguageLookup();

            // Use an UpdateCursor to populate the Meters field and Language fields

            IFeatureCursor cur = fc.Update(null, true);
            IFeature feat = null;
            while ((feat = cur.NextFeature()) != null)
            {
                var pg = feat.Shape as IPolycurveGeodetic;
                double geodLength = pg.get_LengthGeodetic(esriGeodeticType.esriGeodeticTypeGeodesic, metersUnit);
                feat.set_Value(metersField, geodLength);
                double kph = LookupKPH((string)(feat.get_Value(contrAccField)), (string)(feat.get_Value(speedCatField)));
                feat.set_Value(kphField, kph);
                string lang = SignpostUtilities.GetLanguageValue((string)(feat.get_Value(suppliedLangField)), langLookup);
                feat.set_Value(languageField, lang);

                // Alternate name language may be null -- we need to check for this condition before setting the value
                var getValueResult = feat.get_Value(suppliedLangAltField) as string;
                if (getValueResult != null)
                {
                    string langAlt = SignpostUtilities.GetLanguageValue(getValueResult, langLookup);
                    feat.set_Value(languageAltField, langAlt);
                }

                cur.UpdateFeature(feat);
            }
        }

        private double LookupKPH(string contrAcc, string speedCat)
        {
            double kph = 1;
            switch (speedCat)
            {
                case "1":
                    kph = 112;
                    break;
                case "2":
                    kph = 92;
                    break;
                case "3":
                    kph = 76;
                    break;
                case "4":
                    kph = 64;
                    break;
                case "5":
                    kph = 48;
                    break;
                case "6":
                    kph = 32;
                    break;
                case "7":
                    kph = 16;
                    break;
                case "8":
                    kph = 4;
                    break;
                default:
                    return kph;
            }

            if (contrAcc == "Y")
                kph *= 1.2;

            return kph;
        }

        private void CreateAndPopulateDirectionalVehicleAccessFields(string baseFieldName, string streetsFeatureClassPath,
                                                                     Geoprocessor gp, IGPMessages messages, ITrackCancel trackcancel)
        {
            AddMessage("Creating the FT_" + baseFieldName + " and TF_" + baseFieldName + " fields...", messages, trackcancel);

            AddField addFieldTool = new AddField();
            addFieldTool.in_table = streetsFeatureClassPath;
            addFieldTool.field_type = "TEXT";
            addFieldTool.field_length = 1;
            addFieldTool.field_name = "FT_" + baseFieldName;
            gp.Execute(addFieldTool, trackcancel);
            addFieldTool.field_name = "TF_" + baseFieldName;
            gp.Execute(addFieldTool, trackcancel);

            AddMessage("Calculating the FT_" + baseFieldName + " field...", messages, trackcancel);

            CalculateField calcFieldTool = new CalculateField();
            calcFieldTool.in_table = streetsFeatureClassPath;
            calcFieldTool.field = "FT_" + baseFieldName;
            calcFieldTool.code_block = "x = \"Y\"\nIf [DIR_TRAVEL] = \"T\" Or [" + baseFieldName + "] = \"N\" Then x = \"N\"";
            calcFieldTool.expression = "x";
            calcFieldTool.expression_type = "VB";
            gp.Execute(calcFieldTool, trackcancel);

            AddMessage("Calculating the TF_" + baseFieldName + " field...", messages, trackcancel);

            calcFieldTool = new CalculateField();
            calcFieldTool.in_table = streetsFeatureClassPath;
            calcFieldTool.field = "TF_" + baseFieldName;
            calcFieldTool.code_block = "x = \"Y\"\nIf [DIR_TRAVEL] = \"F\" Or [" + baseFieldName + "] = \"N\" Then x = \"N\"";
            calcFieldTool.expression = "x";
            calcFieldTool.expression_type = "VB";
            gp.Execute(calcFieldTool, trackcancel);

            return;
        }

        private string CreatePatternsTable(string speedFilePath, string outputFileGdbPath, double fgdbVersion)
        {
            // Create the Profiles table in the output file geodatabase and open an InsertCursor on it

            Type factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.FileGDBWorkspaceFactory");
            var gdbWSF = Activator.CreateInstance(factoryType) as IWorkspaceFactory;
            var gdbFWS = gdbWSF.OpenFromFile(outputFileGdbPath, 0) as IFeatureWorkspace;
            var ocd = new ObjectClassDescriptionClass() as IObjectClassDescription;
            var tableFields = ocd.RequiredFields as IFieldsEdit;
            var newField = new FieldClass() as IFieldEdit;
            newField.Name_2 = "PatternID";
            newField.Type_2 = esriFieldType.esriFieldTypeSmallInteger;
            tableFields.AddField(newField as IField);
            newField = new FieldClass();
            newField.Name_2 = "BaseSpeed";
            newField.Type_2 = esriFieldType.esriFieldTypeSmallInteger;
            tableFields.AddField(newField as IField);
            for (int i = 0; i < 96; i++)
            {
                newField = new FieldClass();
                newField.Type_2 = esriFieldType.esriFieldTypeSingle;
                newField.Name_2 = ((fgdbVersion == 10.0) ? "TimeFactor_" : "SpeedFactor_") + String.Format("{0:00}", i / 4) + String.Format("{0:00}", (i % 4) * 15);
                tableFields.AddField(newField as IField);
            }
            newField = new FieldClass();
            newField.Name_2 = "AverageSpeed";
            newField.Type_2 = esriFieldType.esriFieldTypeSingle;
            tableFields.AddField(newField as IField);
            ITable newTable = gdbFWS.CreateTable(ProfilesTableName, tableFields as IFields, ocd.InstanceCLSID, ocd.ClassExtensionCLSID, "");
            int newTablePatternIDField = newTable.FindField("PatternID");
            int newTableBaseSpeedField = newTable.FindField("BaseSpeed");
            int newTableAverageSpeedField = newTable.FindField("AverageSpeed");
            IRowBuffer buff = newTable.CreateRowBuffer();
            ICursor insertCursor = newTable.Insert(true);
            
            // Read each line of the Speed file and populate the newly-created Patterns table

            string listOfConstantPatterns = "";
            System.IO.StreamReader f = new System.IO.StreamReader(speedFilePath);
            string s = f.ReadLine();    // skip the first line (header)
            while ((s = f.ReadLine()) != null)
            {
                bool isConstantPattern = true;
                int commaPos = s.IndexOf(",");

                // First value is the ProfileID
                int patternID = Convert.ToInt32(s.Remove(commaPos), System.Globalization.CultureInfo.InvariantCulture);
                buff.set_Value(newTablePatternIDField, patternID);

                s = s.Substring(commaPos + 1);
                commaPos = s.IndexOf(",");

                // Second value is the BaseSpeed (midnight speed)
                int baseSpeed = Convert.ToInt32(s.Remove(commaPos), System.Globalization.CultureInfo.InvariantCulture);
                buff.set_Value(newTableBaseSpeedField, baseSpeed);

                // The midnight ratio is always 1.0 
                buff.set_Value(newTableBaseSpeedField + 1, 1.0);
                float timeFactorSum = (float)1.0;
                float timeFactorSqSum = (float)1.0;

                // Remaining values are the other speeds
                for (int i = 2; i <= 96; i++)
                {
                    s = s.Substring(commaPos + 1);
                    commaPos = s.IndexOf(",");

                    int speed = Convert.ToInt32((commaPos < 0) ? s : s.Remove(commaPos), System.Globalization.CultureInfo.InvariantCulture);
                    float timeFactor = (float)baseSpeed / speed;
                    float timeFactorSq = timeFactor * timeFactor;
                    timeFactorSum += timeFactor;
                    timeFactorSqSum += timeFactorSq;
                    buff.set_Value(newTableBaseSpeedField + i, ((fgdbVersion == 10.0) ? timeFactor : (1 / timeFactor)));

                    if (speed != baseSpeed)
                        isConstantPattern = false;
                }

                buff.set_Value(newTableAverageSpeedField, baseSpeed * timeFactorSum / timeFactorSqSum);

                // Insert the row into the table
                insertCursor.InsertRow(buff);

                // If this is a constant pattern, add it to the list
                if (isConstantPattern)
                    listOfConstantPatterns += (patternID.ToString() + ", ");
            }
            f.Close();

            // Flush any outstanding writes to the table
            insertCursor.Flush();

            // Trim the excess ", " off the last item
            if (listOfConstantPatterns != "")
                listOfConstantPatterns = listOfConstantPatterns.Remove(listOfConstantPatterns.Length - 2);

            return listOfConstantPatterns;
        }

        private void CreateNonHistoricalPatternsTable(string outputFileGdbPath)
        {
            // Create the Patterns table in the output file geodatabase and open an InsertCursor on it

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

        private void ConvertLinkReferenceFilesToFGDBTable(string linkReferenceFileFC14Path, string linkReferenceFileFC5Path, string outputFileGdbPath)
        {
            // Create the Reference table in the output file geodatabase and open an InsertCursor on it

            Type factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.FileGDBWorkspaceFactory");
            var gdbWSF = Activator.CreateInstance(factoryType) as IWorkspaceFactory;
            var gdbFWS = gdbWSF.OpenFromFile(outputFileGdbPath, 0) as IFeatureWorkspace;
            var ocd = new ObjectClassDescriptionClass() as IObjectClassDescription;
            var tableFields = ocd.RequiredFields as IFieldsEdit;
            var newField = new FieldClass() as IFieldEdit;
            newField.Name_2 = "LINK_PVID";
            newField.Type_2 = esriFieldType.esriFieldTypeInteger;
            tableFields.AddField(newField as IField);
            newField = new FieldClass();
            newField.Name_2 = "TRAVEL_DIRECTION";
            newField.Type_2 = esriFieldType.esriFieldTypeString;
            newField.Length_2 = 1;
            tableFields.AddField(newField as IField);
            newField = new FieldClass();
            newField.Name_2 = "U";
            newField.Type_2 = esriFieldType.esriFieldTypeSmallInteger;
            tableFields.AddField(newField as IField);
            newField = new FieldClass();
            newField.Name_2 = "M";
            newField.Type_2 = esriFieldType.esriFieldTypeSmallInteger;
            tableFields.AddField(newField as IField);
            newField = new FieldClass();
            newField.Name_2 = "T";
            newField.Type_2 = esriFieldType.esriFieldTypeSmallInteger;
            tableFields.AddField(newField as IField);
            newField = new FieldClass();
            newField.Name_2 = "W";
            newField.Type_2 = esriFieldType.esriFieldTypeSmallInteger;
            tableFields.AddField(newField as IField);
            newField = new FieldClass();
            newField.Name_2 = "R";
            newField.Type_2 = esriFieldType.esriFieldTypeSmallInteger;
            tableFields.AddField(newField as IField);
            newField = new FieldClass();
            newField.Name_2 = "F";
            newField.Type_2 = esriFieldType.esriFieldTypeSmallInteger;
            tableFields.AddField(newField as IField);
            newField = new FieldClass();
            newField.Name_2 = "S";
            newField.Type_2 = esriFieldType.esriFieldTypeSmallInteger;
            tableFields.AddField(newField as IField);
            newField = new FieldClass();
            newField.Name_2 = "IS_FC5";
            newField.Type_2 = esriFieldType.esriFieldTypeString;
            newField.Length_2 = 1;
            tableFields.AddField(newField as IField);
            ITable newTable = gdbFWS.CreateTable("TempRefTable", tableFields as IFields, ocd.InstanceCLSID, ocd.ClassExtensionCLSID, "");
            int newTableLinkPVIDField = newTable.FindField("LINK_PVID");
            int newTableTravelDirectionField = newTable.FindField("TRAVEL_DIRECTION");
            int newTableIsFC5Field = newTable.FindField("IS_FC5");
            IRowBuffer buff = newTable.CreateRowBuffer();
            ICursor insertCursor = newTable.Insert(true);

            // Read each line of the Reference file for FC 1-4 and populate the newly-created Reference table

            System.IO.StreamReader f = new System.IO.StreamReader(linkReferenceFileFC14Path);
            string s = f.ReadLine();    // skip the first line (header)
            while ((s = f.ReadLine()) != null)
            {
                int commaPos = s.IndexOf(",");

                // First value is the LINK_PVID
                int linkPVID = Convert.ToInt32(s.Remove(commaPos), System.Globalization.CultureInfo.InvariantCulture);
                buff.set_Value(newTableLinkPVIDField, linkPVID);

                s = s.Substring(commaPos + 1);
                commaPos = s.IndexOf(",");

                // Second value is the TRAVEL_DIRECTION
                string travelDirection = s.Remove(commaPos);
                buff.set_Value(newTableTravelDirectionField, travelDirection);

                // Remaining values are the ProfileIDs for each day
                for (int i = 1; i <= 7; i++)
                {
                    s = s.Substring(commaPos + 1);
                    commaPos = s.IndexOf(",");

                    int profileID = Convert.ToInt32((commaPos < 0) ? s : s.Remove(commaPos), System.Globalization.CultureInfo.InvariantCulture);
                    buff.set_Value(newTableTravelDirectionField + i, profileID);
                }

                // Populate "N" in the IS_FC5 field
                buff.set_Value(newTableIsFC5Field, "N");

                // Insert the row into the table
                insertCursor.InsertRow(buff);
            }
            f.Close();

            // Read each line of the Reference file for FC 5 and continue populating the newly-created Reference table

            f = new System.IO.StreamReader(linkReferenceFileFC5Path);
            s = f.ReadLine();    // skip the first line (header)
            while ((s = f.ReadLine()) != null)
            {
                int commaPos = s.IndexOf(",");

                // First value is the LINK_PVID
                int linkPVID = Convert.ToInt32(s.Remove(commaPos), System.Globalization.CultureInfo.InvariantCulture);
                buff.set_Value(newTableLinkPVIDField, linkPVID);

                s = s.Substring(commaPos + 1);
                commaPos = s.IndexOf(",");

                // Second value is the TRAVEL_DIRECTION
                string travelDirection = s.Remove(commaPos);
                buff.set_Value(newTableTravelDirectionField, travelDirection);

                // Remaining values are the ProfileIDs for each day
                for (int i = 1; i <= 7; i++)
                {
                    s = s.Substring(commaPos + 1);
                    commaPos = s.IndexOf(",");

                    int profileID = Convert.ToInt32((commaPos < 0) ? s : s.Remove(commaPos), System.Globalization.CultureInfo.InvariantCulture);
                    buff.set_Value(newTableTravelDirectionField + i, profileID);
                }

                // Populate "Y" in the IS_FC5 field
                buff.set_Value(newTableIsFC5Field, "Y");

                // Insert the row into the table
                insertCursor.InsertRow(buff);
            }
            f.Close();

            // Flush any outstanding writes to the table
            insertCursor.Flush();
        }

        private void ConvertTMCReferenceFileToFGDBTable(string referenceFilePath, string outputFileGdbPath)
        {
            // Create the Reference table in the output file geodatabase and open an InsertCursor on it

            Type factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.FileGDBWorkspaceFactory");
            var gdbWSF = Activator.CreateInstance(factoryType) as IWorkspaceFactory;
            var gdbFWS = gdbWSF.OpenFromFile(outputFileGdbPath, 0) as IFeatureWorkspace;
            var ocd = new ObjectClassDescriptionClass() as IObjectClassDescription;
            var tableFields = ocd.RequiredFields as IFieldsEdit;
            var newField = new FieldClass() as IFieldEdit;
            newField.Name_2 = "TMC";
            newField.Type_2 = esriFieldType.esriFieldTypeString;
            newField.Length_2 = 9;
            tableFields.AddField(newField as IField);
            newField = new FieldClass();
            newField.Name_2 = "U";
            newField.Type_2 = esriFieldType.esriFieldTypeSmallInteger;
            tableFields.AddField(newField as IField);
            newField = new FieldClass();
            newField.Name_2 = "M";
            newField.Type_2 = esriFieldType.esriFieldTypeSmallInteger;
            tableFields.AddField(newField as IField);
            newField = new FieldClass();
            newField.Name_2 = "T";
            newField.Type_2 = esriFieldType.esriFieldTypeSmallInteger;
            tableFields.AddField(newField as IField);
            newField = new FieldClass();
            newField.Name_2 = "W";
            newField.Type_2 = esriFieldType.esriFieldTypeSmallInteger;
            tableFields.AddField(newField as IField);
            newField = new FieldClass();
            newField.Name_2 = "R";
            newField.Type_2 = esriFieldType.esriFieldTypeSmallInteger;
            tableFields.AddField(newField as IField);
            newField = new FieldClass();
            newField.Name_2 = "F";
            newField.Type_2 = esriFieldType.esriFieldTypeSmallInteger;
            tableFields.AddField(newField as IField);
            newField = new FieldClass();
            newField.Name_2 = "S";
            newField.Type_2 = esriFieldType.esriFieldTypeSmallInteger;
            tableFields.AddField(newField as IField);
            ITable newTable = gdbFWS.CreateTable("TempRefTable", tableFields as IFields, ocd.InstanceCLSID, ocd.ClassExtensionCLSID, "");
            int newTableTMCField = newTable.FindField("TMC");
            IRowBuffer buff = newTable.CreateRowBuffer();
            ICursor insertCursor = newTable.Insert(true);

            // Read each line of the Reference file and populate the newly-created Reference table

            System.IO.StreamReader f = new System.IO.StreamReader(referenceFilePath);
            string s = f.ReadLine();    // skip the first line (header)
            while ((s = f.ReadLine()) != null)
            {
                int commaPos = s.IndexOf(",");

                // First value is the TMC
                string tmc = s.Remove(commaPos);
                buff.set_Value(newTableTMCField, tmc);

                // Remaining values are the ProfileIDs for each day
                for (int i = 1; i <= 7; i++)
                {
                    s = s.Substring(commaPos + 1);
                    commaPos = s.IndexOf(",");

                    int profileID = Convert.ToInt32((commaPos < 0) ? s : s.Remove(commaPos), System.Globalization.CultureInfo.InvariantCulture);
                    buff.set_Value(newTableTMCField + i, profileID);
                }

                // Insert the row into the table
                insertCursor.InsertRow(buff);
            }
            f.Close();

            // Flush any outstanding writes to the table
            insertCursor.Flush();
        }

        private void WriteAverageSpeedsToStreets(string outputFileGdbPath, string streetsFeatureClassPath, bool usesNTPFullCoverage, 
                                                 Geoprocessor gp, IGPMessages messages, ITrackCancel trackcancel)
        {
            string joinFieldName = (usesNTPFullCoverage ? "LINK_PVID" : "LINK_ID");
            string refTablePath = outputFileGdbPath + "\\" + HistTrafficJoinTableName;

            // Separate out the FT and TF speeds into separate tables and index the ID fields

            string FTSpeedsTablePath = outputFileGdbPath + "\\FT_Speeds";
            string TFSpeedsTablePath = outputFileGdbPath + "\\TF_Speeds";

            AddMessage("Extracting FT average speeds...", messages, trackcancel);

            TableSelect tableSelectTool = new TableSelect();
            tableSelectTool.in_table = refTablePath;
            tableSelectTool.out_table = FTSpeedsTablePath;
            tableSelectTool.where_clause = (usesNTPFullCoverage ? "\"TRAVEL_DIRECTION\" = 'F'" : "\"TRAFFIC_CD\" LIKE '+%'");
            gp.Execute(tableSelectTool, trackcancel);

            AddMessage("Extracting TF average speeds...", messages, trackcancel);

            tableSelectTool.out_table = TFSpeedsTablePath;
            tableSelectTool.where_clause = (usesNTPFullCoverage ? "\"TRAVEL_DIRECTION\" = 'T'" : "\"TRAFFIC_CD\" LIKE '-%'");
            gp.Execute(tableSelectTool, trackcancel);

            AddIndex addIndexTool = new AddIndex();
            addIndexTool.in_table = FTSpeedsTablePath;
            addIndexTool.fields = joinFieldName;
            addIndexTool.index_name = joinFieldName;
            gp.Execute(addIndexTool, trackcancel);
            addIndexTool.in_table = TFSpeedsTablePath;
            gp.Execute(addIndexTool, trackcancel);

            // Calculate the average speed fields on the Streets feature class

            string FTCodeBlock = "x = [" + StreetsFCName + ".FT_AverageSpeed]\na = [FT_Speeds.AverageSpeed]\nIf Not IsNull(a) Then x = a";
            string TFCodeBlock = "x = [" + StreetsFCName + ".TF_AverageSpeed]\na = [TF_Speeds.AverageSpeed]\nIf Not IsNull(a) Then x = a";
            string FTExpression = "x";
            string TFExpression = "x";

            MakeFeatureLayer makeFeatureLayerTool = new MakeFeatureLayer();
            makeFeatureLayerTool.in_features = streetsFeatureClassPath;
            makeFeatureLayerTool.out_layer = "Streets_Layer";
            gp.Execute(makeFeatureLayerTool, trackcancel);

            AddJoin addJoinTool = new AddJoin();
            addJoinTool.in_layer_or_view = "Streets_Layer";
            addJoinTool.in_field = "LINK_ID";
            addJoinTool.join_table = FTSpeedsTablePath;
            addJoinTool.join_field = joinFieldName;
            gp.Execute(addJoinTool, trackcancel);

            AddMessage("Copying over the FT average speeds...", messages, trackcancel);

            CalculateField calcFieldTool = new CalculateField();
            calcFieldTool.in_table = "Streets_Layer";
            calcFieldTool.field = StreetsFCName + ".FT_AverageSpeed";
            calcFieldTool.code_block = FTCodeBlock;
            calcFieldTool.expression = FTExpression;
            calcFieldTool.expression_type = "VB";
            gp.Execute(calcFieldTool, trackcancel);

            RemoveJoin removeJoinTool = new RemoveJoin();
            removeJoinTool.in_layer_or_view = "Streets_Layer";
            removeJoinTool.join_name = "FT_Speeds";
            gp.Execute(removeJoinTool, trackcancel);

            addJoinTool = new AddJoin();
            addJoinTool.in_layer_or_view = "Streets_Layer";
            addJoinTool.in_field = "LINK_ID";
            addJoinTool.join_table = TFSpeedsTablePath;
            addJoinTool.join_field = joinFieldName;
            gp.Execute(addJoinTool, trackcancel);

            AddMessage("Copying over the TF average speeds...", messages, trackcancel);

            calcFieldTool = new CalculateField();
            calcFieldTool.in_table = "Streets_Layer";
            calcFieldTool.field = StreetsFCName + ".TF_AverageSpeed";
            calcFieldTool.code_block = TFCodeBlock;
            calcFieldTool.expression = TFExpression;
            calcFieldTool.expression_type = "VB";
            gp.Execute(calcFieldTool, trackcancel);

            removeJoinTool = new RemoveJoin();
            removeJoinTool.in_layer_or_view = "Streets_Layer";
            removeJoinTool.join_name = "TF_Speeds";
            gp.Execute(removeJoinTool, trackcancel);

            Delete deleteTool = new Delete();
            deleteTool.in_data = "Streets_Layer";
            gp.Execute(deleteTool, trackcancel);

            deleteTool.in_data = FTSpeedsTablePath;
            gp.Execute(deleteTool, trackcancel);
            deleteTool.in_data = TFSpeedsTablePath;
            gp.Execute(deleteTool, trackcancel);

            return;
        }

        private void PopulateAverageSpeedAndBaseSpeedFields(string patternsTablePath, string day,
                                                            Geoprocessor gp, IGPMessages messages, ITrackCancel trackcancel)
        {
            AddJoin addJoinTool = new AddJoin();
            addJoinTool.in_layer_or_view = "Streets_Patterns_View";
            addJoinTool.in_field = day;
            addJoinTool.join_table = patternsTablePath;
            addJoinTool.join_field = "PatternID";
            gp.Execute(addJoinTool, trackcancel);

            AddMessage("Calculating the AverageSpeed_" + day + " field...", messages, trackcancel);

            CalculateField calcFieldTool = new CalculateField();
            calcFieldTool.in_table = "Streets_Patterns_View";
            calcFieldTool.field = HistTrafficJoinTableName + ".AverageSpeed_" + day;
            calcFieldTool.expression = "[" + ProfilesTableName + ".AverageSpeed]";
            calcFieldTool.expression_type = "VB";
            gp.Execute(calcFieldTool, trackcancel);

            AddMessage("Calculating the BaseSpeed_" + day + " field...", messages, trackcancel);

            calcFieldTool = new CalculateField();
            calcFieldTool.in_table = "Streets_Patterns_View";
            calcFieldTool.field = HistTrafficJoinTableName + ".BaseSpeed_" + day;
            calcFieldTool.expression = "[" + ProfilesTableName + ".BaseSpeed]";
            calcFieldTool.expression_type = "VB";
            gp.Execute(calcFieldTool, trackcancel);

            RemoveJoin removeJoinTool = new RemoveJoin();
            removeJoinTool.in_layer_or_view = "Streets_Patterns_View";
            removeJoinTool.join_name = ProfilesTableName;
            gp.Execute(removeJoinTool, trackcancel);

            return;
        }

        private void OffsetDailyPatternFields(string histTrafficJoinTablePath, string patternsTablePath)
        {
            // Open the historical traffic join table and the patterns table

            ITable histTrafficJoinTable = m_gpUtils.OpenTableFromString(histTrafficJoinTablePath);
            ITable patternsTable = m_gpUtils.OpenTableFromString(patternsTablePath);

            // Find the daily pattern fields on the historical traffic join table

            IFields histTrafficJoinTableFields = histTrafficJoinTable.Fields;
            int inSundayFI = histTrafficJoinTableFields.FindField("U");
            int inMondayFI = histTrafficJoinTableFields.FindField("M");
            int inTuesdayFI = histTrafficJoinTableFields.FindField("T");
            int inWednesdayFI = histTrafficJoinTableFields.FindField("W");
            int inThursdayFI = histTrafficJoinTableFields.FindField("R");
            int inFridayFI = histTrafficJoinTableFields.FindField("F");
            int inSaturdayFI = histTrafficJoinTableFields.FindField("S");

            // Find the difference between the PatternID and OID of the first row in the patterns table

            IRow firstPatternRow = patternsTable.GetRow(1);
            int inPatternIDFI = patternsTable.Fields.FindField("PatternID");
            int diff = firstPatternRow.OID - (short)(firstPatternRow.get_Value(inPatternIDFI));

            if (diff == 0)
                return;

            // Offset the daily pattern field values for all rows in the historical traffic join table

            ICursor updateCur = histTrafficJoinTable.Update(null, true);
            IRow rowToUpdate = updateCur.NextRow();
            while (rowToUpdate != null)
            {
                rowToUpdate.set_Value(inSundayFI, (short)(rowToUpdate.get_Value(inSundayFI)) + diff);
                rowToUpdate.set_Value(inMondayFI, (short)(rowToUpdate.get_Value(inMondayFI)) + diff);
                rowToUpdate.set_Value(inTuesdayFI, (short)(rowToUpdate.get_Value(inTuesdayFI)) + diff);
                rowToUpdate.set_Value(inWednesdayFI, (short)(rowToUpdate.get_Value(inWednesdayFI)) + diff);
                rowToUpdate.set_Value(inThursdayFI, (short)(rowToUpdate.get_Value(inThursdayFI)) + diff);
                rowToUpdate.set_Value(inFridayFI, (short)(rowToUpdate.get_Value(inFridayFI)) + diff);
                rowToUpdate.set_Value(inSaturdayFI, (short)(rowToUpdate.get_Value(inSaturdayFI)) + diff);
                updateCur.UpdateRow(rowToUpdate);

                rowToUpdate = updateCur.NextRow();
            }

            return;
        }
        
        private void CreateAndPopulateTurnFeatureClass(string outputFileGdbPath, string fdsName,
                                                       string ProhibRdmsTableName, string tempStatsTableName,
                                                       IGPMessages messages, ITrackCancel trackcancel)
        {
            // Determine the number of AltID fields we need (one more than the MAX_SEQ_NUMBER value).

            Type factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.FileGDBWorkspaceFactory");
            var wsf = Activator.CreateInstance(factoryType) as IWorkspaceFactory;
            var fws = wsf.OpenFromFile(outputFileGdbPath, 0) as IFeatureWorkspace;
            ITable tempStatsTable = fws.OpenTable(tempStatsTableName);
            short numAltIDFields = 2;
            if (tempStatsTable.RowCount(null) == 1)
                numAltIDFields = (short)(1 + (double)(tempStatsTable.GetRow(1).get_Value(tempStatsTable.FindField("MAX_SEQ_NUMBER"))));
            
            // Open the Rdms table and find the fields we need

            ITable rdmsTable = fws.OpenTable(ProhibRdmsTableName);
            int seqNumberField = rdmsTable.FindField("SEQ_NUMBER");
            int linkIDFieldOnRdms = rdmsTable.FindField("LINK_ID");
            int manLinkIDField = rdmsTable.FindField("MAN_LINKID");
            int condIDFieldOnRdms = rdmsTable.FindField("COND_ID");
            int endOfLkFieldOnRdms = rdmsTable.FindField("END_OF_LK");

            // Create a temporary template feature class

            var fcd = new FeatureClassDescriptionClass() as IFeatureClassDescription;
            var ocd = fcd as IObjectClassDescription;
            var fieldsEdit = ocd.RequiredFields as IFieldsEdit;
            IField fieldOnRdmsTable = rdmsTable.Fields.get_Field(linkIDFieldOnRdms);  // use the LINK_ID field as a template for the AltID fields
            for (short i = 1; i <= numAltIDFields; i++)
            {
                IFieldEdit newField = new FieldClass();
                newField.Name_2 = "AltID" + i;
                newField.Precision_2 = fieldOnRdmsTable.Precision;
                newField.Scale_2 = fieldOnRdmsTable.Scale;
                newField.Type_2 = fieldOnRdmsTable.Type;
                fieldsEdit.AddField(newField as IField);
            }
            fieldOnRdmsTable = rdmsTable.Fields.get_Field(condIDFieldOnRdms);
            fieldsEdit.AddField(fieldOnRdmsTable);
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
            int condIDFieldOnTurnFC = turnFC.FindField("COND_ID");

            // Look up the FCID of the Streets feature class

            IFeatureClass streetsFC = fws.OpenFeatureClass(StreetsFCName);
            int streetsFCID = streetsFC.FeatureClassID;

            // Set up queries

            var ts = new TableSortClass() as ITableSort;
            ts.Fields = "COND_ID, SEQ_NUMBER";
            ts.set_Ascending("COND_ID", true);
            ts.set_Ascending("SEQ_NUMBER", true);
            ts.QueryFilter = new QueryFilterClass();
            ts.Table = rdmsTable;
            ts.Sort(null);
            ICursor rdmsCursor = ts.Rows;
            IFeatureCursor turnFCCursor = turnFC.Insert(true);
            IFeatureBuffer turnBuffer = turnFC.CreateFeatureBuffer();

            // Write the field values to the turn feature class accordingly

            int numFeatures = 0;
            IRow rdmsRow = rdmsCursor.NextRow();
            while (rdmsRow != null)
            {
                // Transfer the non-edge identifying field values to the buffer
                turnBuffer.set_Value(condIDFieldOnTurnFC, rdmsRow.get_Value(condIDFieldOnRdms));

                // Write the Edge1End field value to the buffer
                switch ((string)(rdmsRow.get_Value(endOfLkFieldOnRdms)))
                {
                    case "N":
                        turnBuffer.set_Value(edge1endField, "Y");
                        break;
                    case "R":
                        turnBuffer.set_Value(edge1endField, "N");
                        break;
                    default:
                        break;    // not expected
                }

                // Write the AltID values to the buffer
                turnBuffer.set_Value(altIDFields[0], rdmsRow.get_Value(linkIDFieldOnRdms));
                short seq = (short)(rdmsRow.get_Value(seqNumberField));
                short lastEntry;
                do
                {
                    lastEntry = seq;
                    turnBuffer.set_Value(altIDFields[lastEntry], rdmsRow.get_Value(manLinkIDField));
                    rdmsRow = rdmsCursor.NextRow();
                    if (rdmsRow == null) break;
                    seq = (short)(rdmsRow.get_Value(seqNumberField));
                } while (seq != 1);

                // Zero-out the unused fields
                for (int i = lastEntry + 1; i < numAltIDFields; i++)
                    turnBuffer.set_Value(altIDFields[i], 0);

                // Write the FCID and Pos field values to the buffer
                for (short i = 0; i < numAltIDFields; i++)
                {
                    int altID = (int)(turnBuffer.get_Value(altIDFields[i]));
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

            messages.AddMessage("Updating the EdgeFID values...");

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
            updateByAltIDTool.alternate_ID_field_name = "LINK_ID";
            gp.Execute(updateByAltIDTool, trackcancel);

            // Delete the temporary network dataset

            tempNDS.Delete();

            // Write the turn geometries

            TurnGeometryUtilities.WriteTurnGeometry(outputFileGdbPath, StreetsFCName, TurnFCName,
                                                    numAltIDFields, 0.3, messages, trackcancel);

            // Index the turn geometries

            messages.AddMessage("Creating spatial index on the turn feature class...");

            AddSpatialIndex addSpatialIndexTool = new AddSpatialIndex();
            addSpatialIndexTool.in_features = pathToFds + "\\" + TurnFCName;
            gp.Execute(addSpatialIndexTool, trackcancel);

            return;
        }

        private void CreateAndPopulateTruckFCOverrideField(string outputFileGdbPath, Geoprocessor gp, IGPMessages messages, ITrackCancel trackcancel)
        {
            // Add the TruckFCOverride field to the Streets feature class

            AddField addFieldTool = new AddField();
            addFieldTool.in_table = outputFileGdbPath + "\\" + StreetsFCName;
            addFieldTool.field_name = "TruckFCOverride";
            addFieldTool.field_type = "SHORT";
            gp.Execute(addFieldTool, trackcancel);

            string extractTablePath = outputFileGdbPath + "\\cndModExtract";
            string cndModTablePath = outputFileGdbPath + "\\nonDimensionalCndMod";

            for (short fc = 2; fc >= 1; fc--)
            {
                string fcAsString = Convert.ToString(fc, System.Globalization.CultureInfo.InvariantCulture);

                // Extract the FC Override information from the CndMod table

                AddMessage("Extracting the FC " + fcAsString + " Override information...", messages, trackcancel);

                TableSelect tableSelectTool = new TableSelect();
                tableSelectTool.in_table = cndModTablePath;
                tableSelectTool.out_table = extractTablePath;
                tableSelectTool.where_clause = "MOD_TYPE = 49 AND MOD_VAL = '" + Convert.ToString(fc + 14, System.Globalization.CultureInfo.InvariantCulture) + "'";
                gp.Execute(tableSelectTool, trackcancel);

                // Create the LINK_ID field on the extract table

                addFieldTool = new AddField();
                addFieldTool.in_table = extractTablePath;
                addFieldTool.field_name = "LINK_ID";
                addFieldTool.field_type = "LONG";
                gp.Execute(addFieldTool, trackcancel);

                // Copy over the LINK_ID values to the extract table

                MakeTableView makeTableViewTool = new MakeTableView();
                makeTableViewTool.in_table = extractTablePath;
                makeTableViewTool.out_view = "cndModExtract_View";
                gp.Execute(makeTableViewTool, trackcancel);

                AddJoin addJoinTool = new AddJoin();
                addJoinTool.in_layer_or_view = "cndModExtract_View";
                addJoinTool.in_field = "COND_ID";
                addJoinTool.join_table = outputFileGdbPath + "\\preferredLinkIDLookupTable";
                addJoinTool.join_field = "COND_ID";
                addJoinTool.join_type = "KEEP_COMMON";
                gp.Execute(addJoinTool, trackcancel);

                AddMessage("Looking up LINK_ID values for the FC " + fcAsString + " Override values...", messages, trackcancel);

                CalculateField calcFieldTool = new CalculateField();
                calcFieldTool.in_table = "cndModExtract_View";
                calcFieldTool.field = "cndModExtract.LINK_ID";
                calcFieldTool.expression = "[preferredLinkIDLookupTable.LINK_ID]";
                calcFieldTool.expression_type = "VB";
                gp.Execute(calcFieldTool, trackcancel);

                RemoveJoin removeJoinTool = new RemoveJoin();
                removeJoinTool.in_layer_or_view = "cndModExtract_View";
                removeJoinTool.join_name = "preferredLinkIDLookupTable";
                gp.Execute(removeJoinTool, trackcancel);

                Delete deleteTool = new Delete();
                deleteTool.in_data = "cndModExtract_View";
                gp.Execute(deleteTool, trackcancel);

                AddMessage("Indexing the LINK_ID lookup values...", messages, trackcancel);

                AddIndex addIndexTool = new AddIndex();
                addIndexTool.fields = "LINK_ID";
                addIndexTool.index_name = "LINK_ID";
                addIndexTool.in_table = extractTablePath;
                gp.Execute(addIndexTool, trackcancel);

                // Populate the TruckFCOverride field with the FC Override values

                addJoinTool.in_layer_or_view = "Streets_Layer";
                addJoinTool.in_field = "LINK_ID";
                addJoinTool.join_table = extractTablePath;
                addJoinTool.join_field = "LINK_ID";
                addJoinTool.join_type = "KEEP_COMMON";
                gp.Execute(addJoinTool, trackcancel);

                AddMessage("Populating the TruckFCOverride field with FC " + fcAsString + " Override values...", messages, trackcancel);

                calcFieldTool.in_table = "Streets_Layer";
                calcFieldTool.field = StreetsFCName + ".TruckFCOverride";
                calcFieldTool.expression = fcAsString;
                calcFieldTool.expression_type = "VB";
                gp.Execute(calcFieldTool, trackcancel);

                removeJoinTool.in_layer_or_view = "Streets_Layer";
                removeJoinTool.join_name = "cndModExtract";
                gp.Execute(removeJoinTool, trackcancel);

                deleteTool.in_data = extractTablePath;
                gp.Execute(deleteTool, trackcancel);
            }
        }

        private void CreateAndPopulateTransportFieldsOnStreets(string outputFileGdbPath, bool isDimensional, bool isQuantitative,
                                                               bool isPreferred, string newFieldNameBase, string queryExpression,
                                                               Geoprocessor gp, IGPMessages messages, ITrackCancel trackcancel)
        {
            string cndModTableName = isDimensional ? "dimensionalCndMod" : "nonDimensionalCndMod";
            string linkIDLookupTableName = isPreferred ? "preferredLinkIDLookupTable" : "restrictionLinkIDLookupTable";
            string directionLookupTableName = isPreferred ? "preferredDirectionLookupTable" : "restrictionDirectionLookupTable";

            // Add new fields to the Streets feature class

            AddField addFieldTool = new AddField();
            addFieldTool.in_table = outputFileGdbPath + "\\" + StreetsFCName;
            if (isDimensional)
            {
                addFieldTool.field_type = "DOUBLE";
            }
            else if (isQuantitative)
            {
                addFieldTool.field_type = "SHORT";
            }
            else
            {
                addFieldTool.field_type = "TEXT";
                addFieldTool.field_length = 1;
            }
            addFieldTool.field_name = "FT_" + newFieldNameBase;
            gp.Execute(addFieldTool, trackcancel);
            addFieldTool.field_name = "TF_" + newFieldNameBase;
            gp.Execute(addFieldTool, trackcancel);

            // Extract the information needed for this field from the CndMod table

            string extractTablePath = outputFileGdbPath + "\\cndModExtract";

            AddMessage("Extracting information for the " + newFieldNameBase + " fields...", messages, trackcancel);

            string cndModTablePath = outputFileGdbPath + "\\" + cndModTableName;

            TableSelect tableSelectTool = new TableSelect();
            tableSelectTool.in_table = cndModTablePath;
            tableSelectTool.out_table = extractTablePath;
            tableSelectTool.where_clause = queryExpression;
            gp.Execute(tableSelectTool, trackcancel);

            // Create LINK_ID and Direction fields on the extract table

            addFieldTool = new AddField();
            addFieldTool.in_table = extractTablePath;
            addFieldTool.field_name = "LINK_ID";
            addFieldTool.field_type = "LONG";
            gp.Execute(addFieldTool, trackcancel);
            addFieldTool.field_name = "Direction";
            addFieldTool.field_type = "TEXT";
            addFieldTool.field_length = 2;
            gp.Execute(addFieldTool, trackcancel);

            // Copy over the LINK_ID values to the extract table

            MakeTableView makeTableViewTool = new MakeTableView();
            makeTableViewTool.in_table = extractTablePath;
            makeTableViewTool.out_view = "cndModExtract_View";
            gp.Execute(makeTableViewTool, trackcancel);

            AddJoin addJoinTool = new AddJoin();
            addJoinTool.in_layer_or_view = "cndModExtract_View";
            addJoinTool.in_field = "COND_ID";
            addJoinTool.join_table = outputFileGdbPath + "\\" + linkIDLookupTableName;
            addJoinTool.join_field = "COND_ID";
            addJoinTool.join_type = "KEEP_COMMON";
            gp.Execute(addJoinTool, trackcancel);

            AddMessage("Looking up LINK_ID values for the " + newFieldNameBase + " fields...", messages, trackcancel);

            CalculateField calcFieldTool = new CalculateField();
            calcFieldTool.in_table = "cndModExtract_View";
            calcFieldTool.field = "cndModExtract.LINK_ID";
            calcFieldTool.expression = "[" + linkIDLookupTableName + ".LINK_ID]";
            calcFieldTool.expression_type = "VB";
            gp.Execute(calcFieldTool, trackcancel);

            RemoveJoin removeJoinTool = new RemoveJoin();
            removeJoinTool.in_layer_or_view = "cndModExtract_View";
            removeJoinTool.join_name = linkIDLookupTableName;
            gp.Execute(removeJoinTool, trackcancel);

            // Calculate the Direction values in the extract table

            addJoinTool.in_layer_or_view = "cndModExtract_View";
            addJoinTool.in_field = "COND_ID";
            addJoinTool.join_table = outputFileGdbPath + "\\" + directionLookupTableName;
            addJoinTool.join_field = "COND_ID";
            addJoinTool.join_type = "KEEP_COMMON";
            gp.Execute(addJoinTool, trackcancel);

            AddMessage("Looking up direction values for the " + newFieldNameBase + " fields...", messages, trackcancel);

            calcFieldTool.in_table = "cndModExtract_View";
            calcFieldTool.field = "cndModExtract.Direction";
            if (isPreferred)
                calcFieldTool.code_block = "dir = \"\"\nSelect Case [" + directionLookupTableName + ".MOD_VAL]\n" +
                                           "  Case \"1\": dir = \"FT\"\n  Case \"2\": dir = \"TF\"\n  Case \"3\": dir = \"B\"\nEnd Select";
            else
                calcFieldTool.code_block = "dir = \"\"\nSelect Case [" + directionLookupTableName + ".MOD_VAL]\n" +
                                           "  Case \"1\": dir = \"B\"\n  Case \"2\": dir = \"FT\"\n  Case \"3\": dir = \"TF\"\nEnd Select";
            calcFieldTool.expression = "dir";
            calcFieldTool.expression_type = "VB";
            gp.Execute(calcFieldTool, trackcancel);

            removeJoinTool.in_layer_or_view = "cndModExtract_View";
            removeJoinTool.join_name = directionLookupTableName;
            gp.Execute(removeJoinTool, trackcancel);

            Delete deleteTool = new Delete();
            deleteTool.in_data = "cndModExtract_View";
            gp.Execute(deleteTool, trackcancel);

            AddMessage("Indexing the LINK_ID lookup values...", messages, trackcancel);

            AddIndex addIndexTool = new AddIndex();
            addIndexTool.fields = "LINK_ID";
            addIndexTool.index_name = "LINK_ID";
            addIndexTool.in_table = extractTablePath;
            gp.Execute(addIndexTool, trackcancel);

            // Calculate the preferred route/restriction fields

            addJoinTool.in_layer_or_view = "Streets_Layer";
            addJoinTool.in_field = "LINK_ID";
            addJoinTool.join_table = extractTablePath;
            addJoinTool.join_field = "LINK_ID";
            addJoinTool.join_type = "KEEP_COMMON";
            gp.Execute(addJoinTool, trackcancel);

            calcFieldTool.in_table = "Streets_Layer";
            calcFieldTool.expression_type = "VB";
            string fieldVal = isDimensional ? "[cndModExtract.MetersOrKilogramsOrKPH]" : (isQuantitative ? "CInt( [cndModExtract.MOD_VAL] )" : "\"Y\"");

            AddMessage("Calculating the FT_" + newFieldNameBase + " field...", messages, trackcancel);

            calcFieldTool.field = StreetsFCName + ".FT_" + newFieldNameBase;
            calcFieldTool.code_block = "Select Case [cndModExtract.Direction]\n" +
                                       "  Case \"FT\", \"B\": val = " + fieldVal + "\n  Case Else: val = Null\nEnd Select";
            calcFieldTool.expression = "val";
            gp.Execute(calcFieldTool, trackcancel);

            AddMessage("Calculating the TF_" + newFieldNameBase + " field...", messages, trackcancel);

            calcFieldTool.field = StreetsFCName + ".TF_" + newFieldNameBase;
            calcFieldTool.code_block = "Select Case [cndModExtract.Direction]\n" +
                                       "  Case \"TF\", \"B\": val = " + fieldVal + "\n  Case Else: val = Null\nEnd Select";
            calcFieldTool.expression = "val";
            gp.Execute(calcFieldTool, trackcancel);

            removeJoinTool.in_layer_or_view = "Streets_Layer";
            removeJoinTool.join_name = "cndModExtract";
            gp.Execute(removeJoinTool, trackcancel);

            deleteTool.in_data = extractTablePath;
            gp.Execute(deleteTool, trackcancel);
        }

        private void CreateAndPopulateAGOLTextTransportFieldsOnStreets(string outputFileGdbPath, string newFieldNameBase, string[] arrayOfFieldNameBase, 
                                                                       Geoprocessor gp, IGPMessages messages, ITrackCancel trackcancel)
        {
            // Add new fields to the Streets feature class

            AddField addFieldTool = new AddField();
            addFieldTool.in_table = outputFileGdbPath + "\\" + StreetsFCName;
            addFieldTool.field_type = "TEXT";
            addFieldTool.field_length = 1;
            addFieldTool.field_name = "FT_" + newFieldNameBase;
            gp.Execute(addFieldTool, trackcancel);
            addFieldTool.field_name = "TF_" + newFieldNameBase;
            gp.Execute(addFieldTool, trackcancel);

            string ftCodeBlock = "val = Null";
            string tfCodeBlock = "val = Null";
            foreach (string fieldNameBase in arrayOfFieldNameBase)
            {
                ftCodeBlock += "\nIf [FT_" + fieldNameBase + "] = \"Y\" Then val = \"Y\"";
                tfCodeBlock += "\nIf [TF_" + fieldNameBase + "] = \"Y\" Then val = \"Y\"";
            }

            AddMessage("Calculating the FT_" + newFieldNameBase + " field...", messages, trackcancel);

            CalculateField calcFieldTool = new CalculateField();
            calcFieldTool.in_table = outputFileGdbPath + "\\" + StreetsFCName;
            calcFieldTool.field = "FT_" + newFieldNameBase;
            calcFieldTool.code_block = ftCodeBlock;
            calcFieldTool.expression = "val";
            gp.Execute(calcFieldTool, trackcancel);

            AddMessage("Calculating the TF_" + newFieldNameBase + " field...", messages, trackcancel);

            calcFieldTool.field = "TF_" + newFieldNameBase;
            calcFieldTool.code_block = tfCodeBlock;
            calcFieldTool.expression = "val";
            gp.Execute(calcFieldTool, trackcancel);
        }

        private void CreateAndPopulateTransportFieldOnTurns(string outputFileGdbPath, bool isDimensional, bool isQuantitative,
                                                            string newFieldName, string queryExpression,
                                                            Geoprocessor gp, IGPMessages messages, ITrackCancel trackcancel)
        {
            string cndModTableName = isDimensional ? "dimensionalCndMod" : "nonDimensionalCndMod";

            // Add a new field to the Turns feature class

            AddField addFieldTool = new AddField();
            addFieldTool.in_table = outputFileGdbPath + "\\" + TurnFCName;
            if (isDimensional)
            {
                addFieldTool.field_type = "DOUBLE";
            }
            else if (isQuantitative)
            {
                addFieldTool.field_type = "SHORT";
            }
            else
            {
                addFieldTool.field_type = "TEXT";
                addFieldTool.field_length = 1;
            }
            addFieldTool.field_name = newFieldName;
            gp.Execute(addFieldTool, trackcancel);

            // Extract the information needed for this field from the CndMod table

            string extractTablePath = outputFileGdbPath + "\\cndModExtract";

            AddMessage("Extracting information for the " + newFieldName + " field on " + TurnFCName + "...", messages, trackcancel);

            string cndModTablePath = outputFileGdbPath + "\\" + cndModTableName;

            TableSelect tableSelectTool = new TableSelect();
            tableSelectTool.in_table = cndModTablePath;
            tableSelectTool.out_table = extractTablePath;
            tableSelectTool.where_clause = queryExpression;
            gp.Execute(tableSelectTool, trackcancel);

            AddMessage("Indexing the COND_ID field...", messages, trackcancel);

            AddIndex addIndexTool = new AddIndex();
            addIndexTool.fields = "COND_ID";
            addIndexTool.index_name = "COND_ID";
            addIndexTool.in_table = extractTablePath;
            gp.Execute(addIndexTool, trackcancel);

            // Calculate the turn restriction field

            AddJoin addJoinTool = new AddJoin();
            addJoinTool.in_layer_or_view = "RestrictedTurns_Layer";
            addJoinTool.in_field = "COND_ID";
            addJoinTool.join_table = extractTablePath;
            addJoinTool.join_field = "COND_ID";
            addJoinTool.join_type = "KEEP_COMMON";
            gp.Execute(addJoinTool, trackcancel);

            AddMessage("Calculating the " + newFieldName + " field on " + TurnFCName + "...", messages, trackcancel);

            CalculateField calcFieldTool = new CalculateField();
            calcFieldTool.in_table = "RestrictedTurns_Layer";
            calcFieldTool.field = TurnFCName + "." + newFieldName;
            calcFieldTool.expression = isDimensional ? "[cndModExtract.MetersOrKilogramsOrKPH]" : (isQuantitative ? "CInt( [cndModExtract.MOD_VAL] )" : "\"Y\"");
            calcFieldTool.expression_type = "VB";
            gp.Execute(calcFieldTool, trackcancel);

            RemoveJoin removeJoinTool = new RemoveJoin();
            removeJoinTool.in_layer_or_view = "RestrictedTurns_Layer";
            removeJoinTool.join_name = "cndModExtract";
            gp.Execute(removeJoinTool, trackcancel);

            Delete deleteTool = new Delete();
            deleteTool.in_data = extractTablePath;
            gp.Execute(deleteTool, trackcancel);
        }

        private void CreateAndPopulateAGOLTextTransportFieldOnTurns(string outputFileGdbPath, string newFieldNameBase, string[] arrayOfFieldNameBase,
                                                                    Geoprocessor gp, IGPMessages messages, ITrackCancel trackcancel)
        {
            // Add new fields to the Turns feature class

            AddField addFieldTool = new AddField();
            addFieldTool.in_table = outputFileGdbPath + "\\" + TurnFCName;
            addFieldTool.field_type = "TEXT";
            addFieldTool.field_name = newFieldNameBase;
            addFieldTool.field_length = 1;
            gp.Execute(addFieldTool, trackcancel);

            string codeBlock = "val = Null";
            foreach (string fieldNameBase in arrayOfFieldNameBase)
            {
                codeBlock += "\nIf [" + fieldNameBase + "] = \"Y\" Then val = \"Y\"";
            }

            AddMessage("Calculating the " + newFieldNameBase + " field...", messages, trackcancel);

            CalculateField calcFieldTool = new CalculateField();
            calcFieldTool.in_table = outputFileGdbPath + "\\" + TurnFCName;
            calcFieldTool.field = newFieldNameBase;
            calcFieldTool.code_block = codeBlock;
            calcFieldTool.expression = "val";
            gp.Execute(calcFieldTool, trackcancel);
        }

        private void CreateRoadSplitsTable(string inputRdmsTable, string outputFileGdbPath, IGPMessages messages, ITrackCancel trackcancel)
        {
            // Open the Rdms table and find all the fields we need

            Type factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.FileGDBWorkspaceFactory");
            var wsf = Activator.CreateInstance(factoryType) as IWorkspaceFactory;
            var fws = wsf.OpenFromFile(outputFileGdbPath, 0) as IFeatureWorkspace;
            ITable rdmsTable = fws.OpenTable(inputRdmsTable);
            int seqNumberField = rdmsTable.FindField("SEQ_NUMBER");
            int linkIDFieldOnRdms = rdmsTable.FindField("LINK_ID");
            int manLinkIDField = rdmsTable.FindField("MAN_LINKID");
            int condIDFieldOnRdms = rdmsTable.FindField("COND_ID");
            int endOfLkFieldOnRdms = rdmsTable.FindField("END_OF_LK");

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

            for (int i = 0; i < 3; i++)
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
            
            // Fetch all line features referenced by the input Rdms table.  We do the
            // "join" this hard way to support all data sources in the sample. 
            // Also, for large numbers of special explications, this strategy of fetching all
            // related features and holding them in RAM could be a problem.  To fix
            // this, one could process the input records from the Rdms table in batches.

            System.Collections.Hashtable lineFeaturesList = SignpostUtilities.FillFeatureCache(rdmsTable, linkIDFieldOnRdms, manLinkIDField,
                                                                                               inputLineFeatures, "LINK_ID", trackcancel);

            // Create insert cursor and row buffer for output

            ICursor tableInsertCursor = roadSplitsTable.Insert(true);
            IRowBuffer tableBuffer = roadSplitsTable.CreateRowBuffer();
            IRow row = tableBuffer as IRow;

            // Create input cursor for the Rdms table we are importing

            ITableSort tableSort = new TableSortClass();
            tableSort.Fields = "LINK_ID, COND_ID, SEQ_NUMBER";
            tableSort.set_Ascending("LINK_ID", true);
            tableSort.set_Ascending("COND_ID", true);
            tableSort.set_Ascending("SEQ_NUMBER", true);
            tableSort.QueryFilter = null;
            tableSort.Table = rdmsTable;
            tableSort.Sort(null);
            ICursor inputCursor = tableSort.Rows;

            IRow inputTableRow = inputCursor.NextRow();
            if (inputTableRow == null)
                return;     // if Rdms table is empty, there's nothing to do

            // these are initialized to prevent uninitialized variable compiler error
            SignpostUtilities.FeatureData linkFeatureData = new SignpostUtilities.FeatureData(-1, null);
            SignpostUtilities.FeatureData manLinkFeatureData = new SignpostUtilities.FeatureData(-1, null);

            ICurve fromEdgeCurve, toEdgeCurve;
            IPoint fromEdgeStart, fromEdgeEnd, toEdgeStart, toEdgeEnd;
            double fromEdgeFromPos = 0.0;
            double fromEdgeToPos = 1.0;
            double toEdgeFromPos = 0.0;
            double toEdgeToPos = 1.0;

            long currentLinkID = Convert.ToInt64(inputTableRow.get_Value(linkIDFieldOnRdms));
            long manLinkID = Convert.ToInt64(inputTableRow.get_Value(manLinkIDField));
            try
            {
                linkFeatureData = (SignpostUtilities.FeatureData)lineFeaturesList[currentLinkID];
                manLinkFeatureData = (SignpostUtilities.FeatureData)lineFeaturesList[manLinkID];

                // To set from and to position in the output table, we need see where and 
                // if the two edge features connect to figure out their digitized direction.

                fromEdgeCurve = linkFeatureData.feature as ICurve;
                toEdgeCurve = manLinkFeatureData.feature as ICurve;

                fromEdgeStart = fromEdgeCurve.FromPoint;
                fromEdgeEnd = fromEdgeCurve.ToPoint;
                toEdgeStart = toEdgeCurve.FromPoint;
                toEdgeEnd = toEdgeCurve.ToPoint;

                // flip the from edge?

                if (TurnGeometryUtilities.EqualPoints(fromEdgeStart, toEdgeStart) || TurnGeometryUtilities.EqualPoints(fromEdgeStart, toEdgeEnd))
                {
                    fromEdgeFromPos = 1.0;
                    fromEdgeToPos = 0.0;
                }

                // flip the to edge?

                if (TurnGeometryUtilities.EqualPoints(toEdgeEnd, fromEdgeStart) || TurnGeometryUtilities.EqualPoints(toEdgeEnd, fromEdgeEnd))
                {
                    toEdgeFromPos = 1.0;
                    toEdgeToPos = 0.0;
                }

                // set the field values in the buffer

                tableBuffer.set_Value(EdgeFCIDFI, streetsFCID);
                tableBuffer.set_Value(EdgeFIDFI, linkFeatureData.OID);
                tableBuffer.set_Value(EdgeFrmPosFI, fromEdgeFromPos);
                tableBuffer.set_Value(EdgeToPosFI, fromEdgeToPos);
                tableBuffer.set_Value(Branch0FCIDFI, streetsFCID);
                tableBuffer.set_Value(Branch0FIDFI, manLinkFeatureData.OID);
                tableBuffer.set_Value(Branch0FrmPosFI, toEdgeFromPos);
                tableBuffer.set_Value(Branch0ToPosFI, toEdgeToPos);
                tableBuffer.set_Value(Branch1FCIDFI, null);
                tableBuffer.set_Value(Branch1FIDFI, null);
                tableBuffer.set_Value(Branch1FrmPosFI, null);
                tableBuffer.set_Value(Branch1ToPosFI, null);
                tableBuffer.set_Value(Branch2FCIDFI, null);
                tableBuffer.set_Value(Branch2FIDFI, null);
                tableBuffer.set_Value(Branch2FrmPosFI, null);
                tableBuffer.set_Value(Branch2ToPosFI, null);
            }
            catch
            {
                messages.AddWarning("Line feature not found for explication with from ID: " +
                    Convert.ToString(currentLinkID, System.Globalization.CultureInfo.InvariantCulture) +
                    ", To ID: " + Convert.ToString(manLinkID, System.Globalization.CultureInfo.InvariantCulture));
            }

            long previousLinkID = currentLinkID;
            int nextBranch = 1;

            while ((inputTableRow = inputCursor.NextRow()) != null)
            {
                currentLinkID = Convert.ToInt64(inputTableRow.get_Value(linkIDFieldOnRdms));
                manLinkID = Convert.ToInt64(inputTableRow.get_Value(manLinkIDField));
                try
                {
                    linkFeatureData = (SignpostUtilities.FeatureData)lineFeaturesList[currentLinkID];
                    manLinkFeatureData = (SignpostUtilities.FeatureData)lineFeaturesList[manLinkID];
                }
                catch
                {
                    messages.AddWarning("Line feature not found for explication with from ID: " +
                        Convert.ToString(currentLinkID, System.Globalization.CultureInfo.InvariantCulture) +
                        ", To ID: " + Convert.ToString(manLinkID, System.Globalization.CultureInfo.InvariantCulture));
                    continue;
                }

                // To set from and to position in the output table, we need see where and 
                // if the two edge features connect to figure out their digitized direction.

                fromEdgeCurve = linkFeatureData.feature as ICurve;
                toEdgeCurve = manLinkFeatureData.feature as ICurve;

                fromEdgeStart = fromEdgeCurve.FromPoint;
                fromEdgeEnd = fromEdgeCurve.ToPoint;
                toEdgeStart = toEdgeCurve.FromPoint;
                toEdgeEnd = toEdgeCurve.ToPoint;

                fromEdgeFromPos = 0.0;
                fromEdgeToPos = 1.0;
                toEdgeFromPos = 0.0;
                toEdgeToPos = 1.0;

                // flip the from edge?

                if (TurnGeometryUtilities.EqualPoints(fromEdgeStart, toEdgeStart) || TurnGeometryUtilities.EqualPoints(fromEdgeStart, toEdgeEnd))
                {
                    fromEdgeFromPos = 1.0;
                    fromEdgeToPos = 0.0;
                }

                // flip the to edge?

                if (TurnGeometryUtilities.EqualPoints(toEdgeEnd, fromEdgeStart) || TurnGeometryUtilities.EqualPoints(toEdgeEnd, fromEdgeEnd))
                {
                    toEdgeFromPos = 1.0;
                    toEdgeToPos = 0.0;
                }

                // set the field values in the buffer

                if (previousLinkID == currentLinkID)
                {
                    switch (nextBranch)
                    {
                        case 1:
                            tableBuffer.set_Value(Branch1FCIDFI, streetsFCID);
                            tableBuffer.set_Value(Branch1FIDFI, manLinkFeatureData.OID);
                            tableBuffer.set_Value(Branch1FrmPosFI, toEdgeFromPos);
                            tableBuffer.set_Value(Branch1ToPosFI, toEdgeToPos);
                            nextBranch = 2;
                            break;
                        case 2:
                            tableBuffer.set_Value(Branch2FCIDFI, streetsFCID);
                            tableBuffer.set_Value(Branch2FIDFI, manLinkFeatureData.OID);
                            tableBuffer.set_Value(Branch2FrmPosFI, toEdgeFromPos);
                            tableBuffer.set_Value(Branch2ToPosFI, toEdgeToPos);
                            nextBranch = 3;
                            break;
                        case 3:
                            messages.AddWarning("There are more than three road splits for From ID: " +
                                Convert.ToString(currentLinkID, System.Globalization.CultureInfo.InvariantCulture));
                            nextBranch = 4;
                            break;
                        case 4:
                            // do nothing here, as there's no need to repeat the warning message.
                            break;
                    }
                }
                else
                {
                    // write out the previous buffered row...
                    tableInsertCursor.InsertRow(tableBuffer);

                    // ...and then set field values in the fresh buffer
                    tableBuffer.set_Value(EdgeFCIDFI, streetsFCID);
                    tableBuffer.set_Value(EdgeFIDFI, linkFeatureData.OID);
                    tableBuffer.set_Value(EdgeFrmPosFI, fromEdgeFromPos);
                    tableBuffer.set_Value(EdgeToPosFI, fromEdgeToPos);
                    tableBuffer.set_Value(Branch0FCIDFI, streetsFCID);
                    tableBuffer.set_Value(Branch0FIDFI, manLinkFeatureData.OID);
                    tableBuffer.set_Value(Branch0FrmPosFI, toEdgeFromPos);
                    tableBuffer.set_Value(Branch0ToPosFI, toEdgeToPos);
                    tableBuffer.set_Value(Branch1FCIDFI, null);
                    tableBuffer.set_Value(Branch1FIDFI, null);
                    tableBuffer.set_Value(Branch1FrmPosFI, null);
                    tableBuffer.set_Value(Branch1ToPosFI, null);
                    tableBuffer.set_Value(Branch2FCIDFI, null);
                    tableBuffer.set_Value(Branch2FIDFI, null);
                    tableBuffer.set_Value(Branch2FrmPosFI, null);
                    tableBuffer.set_Value(Branch2ToPosFI, null);
                    nextBranch = 1;
                }
                previousLinkID = currentLinkID;
            }

            // Write out the final row and flush
            tableInsertCursor.InsertRow(tableBuffer);
            tableInsertCursor.Flush();
        }

        private void CreateSignposts(string inputSignsTablePath, string outputFileGdbPath, IGPMessages messages, ITrackCancel trackcancel)
        {
            // Open the input Signs table
            
            ITable inputSignsTable = m_gpUtils.OpenTableFromString(inputSignsTablePath);

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
            IFields inputTableFields = inputSignsTable.Fields;
            int inSequenceFI = inputTableFields.FindField("SEQ_NUM");
            int inExitNumFI = inputTableFields.FindField("EXIT_NUM");
            int inFromIDFI = inputTableFields.FindField("SRC_LINKID");
            int inToIDFI = inputTableFields.FindField("DST_LINKID");
            int inLangFI = inputTableFields.FindField("LANG_CODE");
            int inBranchRteIDFI = inputTableFields.FindField("BR_RTEID");
            int inDirectionFI = inputTableFields.FindField("BR_RTEDIR");
            int inToNameFI = inputTableFields.FindField("SIGN_TEXT");
            int inAccessFI = inputTableFields.FindField("SIGN_TXTTP");
            int inToLocaleFI = inputTableFields.FindField("TOW_RTEID");

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
            int inLinesUserIDFI = inputLineFeatures.FindField("LINK_ID");
            int inLinesShapeFI = inputLineFeatures.FindField(inputLineFeatures.ShapeFieldName);

            #endregion

            // Get the language lookup hash

            System.Collections.Hashtable langLookup = CreateLanguageLookup();

            // Fetch all line features referenced by the input signs table.  We do the
            // "join" this hard way to support all data sources in the sample. 
            // Also, for large numbers of sign records, this strategy of fetching all
            // related features and holding them in RAM could be a problem.  To fix
            // this, one could process the input sign records in batches.

            System.Collections.Hashtable lineFeaturesList = SignpostUtilities.FillFeatureCache(inputSignsTable, inFromIDFI, inToIDFI, inputLineFeatures, "LINK_ID", trackcancel);

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

            // Create input cursor for the signs table we are importing

            ITableSort tableSort = new TableSortClass();
            tableSort.Fields = "SRC_LINKID, DST_LINKID, SEQ_NUM";
            tableSort.set_Ascending("SRC_LINKID", true);
            tableSort.set_Ascending("DST_LINKID", true);
            tableSort.set_Ascending("SEQ_NUM", true);
            tableSort.QueryFilter = null;
            tableSort.Table = inputSignsTable;
            tableSort.Sort(null);
            ICursor inputCursor = tableSort.Rows;

            IRow inputTableRow;
            int numOutput = 0;
            int numInput = 0;
            short inSequenceValue;
            long fromIDVal, toIDVal;

            int nextBranchNum = -1, nextTowardNum = -1;

            // these are initialized to prevent uninitialized variable compiler error

            SignpostUtilities.FeatureData fromFeatureData = new SignpostUtilities.FeatureData(-1, null);
            SignpostUtilities.FeatureData toFeatureData = new SignpostUtilities.FeatureData(-1, null);

            object newOID;
            string branchText, towardText, signText, accessText;
            string langText, langValue;

            ICurve fromEdgeCurve, toEdgeCurve;
            IPoint fromEdgeStart, fromEdgeEnd, toEdgeStart, toEdgeEnd;

            int refLinesFCID = inputLineFeatures.ObjectClassID;
            IGeometry outputSignGeometry;

            double lastSrcLinkID = -1.0, currentSrcLinkID = -1.0;
            double lastDstLinkID = -1.0, currentDstLinkID = -1.0;
            double fromEdgeFromPos = 0.0;
            double fromEdgeToPos = 1.0;
            double toEdgeFromPos = 0.0;
            double toEdgeToPos = 1.0;

            while ((inputTableRow = inputCursor.NextRow()) != null)
            {
                currentSrcLinkID = Convert.ToInt32(inputTableRow.get_Value(inFromIDFI));
                currentDstLinkID = Convert.ToInt32(inputTableRow.get_Value(inToIDFI));

                // If we have a new source/destination link ID, we need to
                // insert the signpost feature in progress and write the detail records.
                // (identical code is also after the while loop for the last sign record)

                if (((currentSrcLinkID != lastSrcLinkID) || (currentDstLinkID != lastDstLinkID)) &&
                    ((lastSrcLinkID != -1) && (lastDstLinkID != -1)))
                {
                    // clean up unused parts of the row and pack toward/branch items

                    SignpostUtilities.CleanUpSignpostFeatureValues(featureBuffer, nextBranchNum - 1, nextTowardNum - 1,
                                                                   outBranchXFI, outBranchXDirFI, outBranchXLngFI,
                                                                   outTowardXFI, outTowardXLngFI);

                    // save sign feature record

                    newOID = featureInsertCursor.InsertFeature(featureBuffer);

                    // set streets table values

                    tableRowBuffer.set_Value(outTblSignpostIDFI, newOID);
                    tableRowBuffer.set_Value(outTblSequenceFI, 1);
                    tableRowBuffer.set_Value(outTblEdgeFCIDFI, refLinesFCID);
                    tableRowBuffer.set_Value(outTblEdgeFIDFI, fromFeatureData.OID);
                    tableRowBuffer.set_Value(outTblEdgeFrmPosFI, fromEdgeFromPos);
                    tableRowBuffer.set_Value(outTblEdgeToPosFI, fromEdgeToPos);

                    // insert first detail record

                    tableInsertCursor.InsertRow(tableRowBuffer);

                    tableRowBuffer.set_Value(outTblSequenceFI, 0);
                    tableRowBuffer.set_Value(outTblEdgeFIDFI, toFeatureData.OID);
                    tableRowBuffer.set_Value(outTblEdgeFrmPosFI, toEdgeFromPos);
                    tableRowBuffer.set_Value(outTblEdgeToPosFI, toEdgeToPos);

                    // insert second detail record

                    tableInsertCursor.InsertRow(tableRowBuffer);

                    numOutput++;
                    if ((numOutput % 100) == 0)
                    {
                        // check for user cancel

                        if (trackcancel != null && !trackcancel.Continue())
                            throw (new COMException("Function cancelled."));
                    }
                }

                lastSrcLinkID = currentSrcLinkID;
                lastDstLinkID = currentDstLinkID;

                inSequenceValue = Convert.ToInt16(inputTableRow.get_Value(inSequenceFI));
                if (inSequenceValue == 1)
                {
                    // We are starting a sequence of records for a new sign.
                    // nextBranchNum and nextTowardNum keep track of which branch and
                    // toward item numbers we have used and are not necessarily the same
                    // as inSequenceValue.

                    nextBranchNum = 0;
                    nextTowardNum = 0;

                    fromIDVal = Convert.ToInt64(inputTableRow.get_Value(inFromIDFI));
                    toIDVal = Convert.ToInt64(inputTableRow.get_Value(inToIDFI));

                    // If the signpost references a line feature that is not in the lines
                    // feature class, add a warning message and keep going.
                    // Only warn for the first 100 not found.

                    numInput++;

                    try
                    {
                        fromFeatureData = (SignpostUtilities.FeatureData)lineFeaturesList[fromIDVal];
                        toFeatureData = (SignpostUtilities.FeatureData)lineFeaturesList[toIDVal];
                    }
                    catch
                    {
                        if (numInput - numOutput < 100)
                        {
                            messages.AddWarning("Line feature not found for sign with FromID: " +
                                Convert.ToString(fromIDVal, System.Globalization.CultureInfo.InvariantCulture) +
                                ", ToID: " + Convert.ToString(toIDVal, System.Globalization.CultureInfo.InvariantCulture));
                        }
                        continue;
                    }


                    // To set from and to position in the detail table and to construct geometry
                    // for the output signs feature class, we need see where and 
                    // if the two edge features connect to figure out their digitized direction.

                    fromEdgeCurve = fromFeatureData.feature as ICurve;
                    toEdgeCurve = toFeatureData.feature as ICurve;

                    fromEdgeStart = fromEdgeCurve.FromPoint;
                    fromEdgeEnd = fromEdgeCurve.ToPoint;
                    toEdgeStart = toEdgeCurve.FromPoint;
                    toEdgeEnd = toEdgeCurve.ToPoint;

                    fromEdgeFromPos = 0.0;
                    fromEdgeToPos = 1.0;
                    toEdgeFromPos = 0.0;
                    toEdgeToPos = 1.0;

                    // flip the from edge?

                    if (TurnGeometryUtilities.EqualPoints(fromEdgeStart, toEdgeStart) || TurnGeometryUtilities.EqualPoints(fromEdgeStart, toEdgeEnd))
                    {
                        fromEdgeFromPos = 1.0;
                        fromEdgeToPos = 0.0;
                    }

                    // flip the to edge?

                    if (TurnGeometryUtilities.EqualPoints(toEdgeEnd, fromEdgeStart) || TurnGeometryUtilities.EqualPoints(toEdgeEnd, fromEdgeEnd))
                    {
                        toEdgeFromPos = 1.0;
                        toEdgeToPos = 0.0;
                    }

                    // set sign feature values

                    // construct shape - the only purpose of the shape is visualization and it can be null

                    outputSignGeometry = MakeSignGeometry(fromEdgeCurve, toEdgeCurve, fromEdgeFromPos == 1.0, toEdgeFromPos == 1.0);

                    featureBuffer.Shape = outputSignGeometry;

                    featureBuffer.set_Value(outExitNameFI, inputTableRow.get_Value(inExitNumFI));
                }

                // Look up the language code

                langText = (inputTableRow.get_Value(inLangFI) as string).Trim();
                langValue = SignpostUtilities.GetLanguageValue(langText, langLookup);

                // Populate Branch items from BR_RTEID and BR_RTEDIR 

                branchText = inputTableRow.get_Value(inBranchRteIDFI) is DBNull ? "": (inputTableRow.get_Value(inBranchRteIDFI) as string).Trim();

                if (branchText.Length > 0)
                {
                    // check for schema overflow
                    if (nextBranchNum > SignpostUtilities.MaxBranchCount - 1)
                        continue;

                    // set values
                    featureBuffer.set_Value(outBranchXFI[nextBranchNum], branchText);
                    featureBuffer.set_Value(outBranchXDirFI[nextBranchNum], inputTableRow.get_Value(inDirectionFI));
                    featureBuffer.set_Value(outBranchXLngFI[nextBranchNum], langValue);

                    // get ready for next branch
                    nextBranchNum++;
                }

                // Populate Branch or Toward items from SIGN_TEXT depending upon the value in the SIGN_TXTTP field:
                //  - if SIGN_TXTTP == "B" (direct), populate a branch
                //  - if SIGN_TXTTP == "T" (direct), populate a toward

                signText = (inputTableRow.get_Value(inToNameFI) as string).Trim();

                if (signText.Length > 0)
                {
                    accessText = (inputTableRow.get_Value(inAccessFI) as string);

                    if (accessText == "B")
                    {
                        // check for schema overflow
                        if (nextBranchNum > SignpostUtilities.MaxBranchCount - 1)
                            continue;

                        // set values
                        featureBuffer.set_Value(outBranchXFI[nextBranchNum], signText);
                        featureBuffer.set_Value(outBranchXDirFI[nextBranchNum], inputTableRow.get_Value(inDirectionFI));
                        featureBuffer.set_Value(outBranchXLngFI[nextBranchNum], langValue);

                        // get ready for next branch
                        nextBranchNum++;
                    }
                    else if (accessText == "T")
                    {
                        // check for schema overflow
                        if (nextTowardNum > SignpostUtilities.MaxBranchCount - 1)
                            continue;

                        // set values
                        featureBuffer.set_Value(outTowardXFI[nextTowardNum], signText);
                        featureBuffer.set_Value(outTowardXLngFI[nextTowardNum], langValue);

                        // get ready for next toward
                        nextTowardNum++;
                    }
                    else
                        continue;    // not expected
                }

                // Populate Toward items from TOW_RTEID

                towardText = inputTableRow.get_Value(inToLocaleFI) is DBNull ? "" : (inputTableRow.get_Value(inToLocaleFI) as string).Trim();

                if (towardText.Length > 0)
                {
                    // check for schema overflow
                    if (nextTowardNum > SignpostUtilities.MaxBranchCount - 1)
                        continue;

                    // set values
                    featureBuffer.set_Value(outTowardXFI[nextTowardNum], towardText);
                    featureBuffer.set_Value(outTowardXLngFI[nextTowardNum], langValue);

                    // get ready for next toward
                    nextTowardNum++;
                }

            }  // each input table record

            // Assuming the table wasn't empty to begin with (detected by the Currents no longer being -1.0),
            // add the last signpost feature and detail records (same code as above)

            if (currentSrcLinkID != -1.0 && currentDstLinkID != -1.0)
            {
                // clean up unused parts of the row and pack toward/branch items

                SignpostUtilities.CleanUpSignpostFeatureValues(featureBuffer, nextBranchNum - 1, nextTowardNum - 1,
                                                               outBranchXFI, outBranchXDirFI, outBranchXLngFI,
                                                               outTowardXFI, outTowardXLngFI);

                // save sign feature record

                newOID = featureInsertCursor.InsertFeature(featureBuffer);

                // set streets table values

                tableRowBuffer.set_Value(outTblSignpostIDFI, newOID);
                tableRowBuffer.set_Value(outTblSequenceFI, 1);
                tableRowBuffer.set_Value(outTblEdgeFCIDFI, refLinesFCID);
                tableRowBuffer.set_Value(outTblEdgeFIDFI, fromFeatureData.OID);
                tableRowBuffer.set_Value(outTblEdgeFrmPosFI, fromEdgeFromPos);
                tableRowBuffer.set_Value(outTblEdgeToPosFI, fromEdgeToPos);

                // insert first detail record

                tableInsertCursor.InsertRow(tableRowBuffer);

                tableRowBuffer.set_Value(outTblSequenceFI, 0);
                tableRowBuffer.set_Value(outTblEdgeFIDFI, toFeatureData.OID);
                tableRowBuffer.set_Value(outTblEdgeFrmPosFI, toEdgeFromPos);
                tableRowBuffer.set_Value(outTblEdgeToPosFI, toEdgeToPos);

                // insert second detail record

                tableInsertCursor.InsertRow(tableRowBuffer);

                numOutput++;

                // Flush any outstanding writes to the feature class and table
                featureInsertCursor.Flush();
                tableInsertCursor.Flush();
            }

            // add a summary message

            messages.AddMessage(Convert.ToString(numOutput) + " of " + Convert.ToString(numInput) + " signposts added.");

            return;
        }

        private Hashtable CreateLanguageLookup()
        {
            Hashtable lookupHash = new System.Collections.Hashtable(128);
            lookupHash.Add("ALB", "sq");  // Albanian
            lookupHash.Add("AMT", "hy");  // Armenian Transcribed
            lookupHash.Add("ARA", "ar");  // Arabic
            lookupHash.Add("ARE", "en");  // Arabic English
            lookupHash.Add("ARM", "hy");  // Armenian
            lookupHash.Add("ARX", "hy");  // Armenian Transliterated
            lookupHash.Add("ASM", "as");  // Assamese
            lookupHash.Add("ASX", "as");  // Assamese Transliterated
            lookupHash.Add("AZE", "az");  // Azerbaijan
            lookupHash.Add("AZX", "az");  // Azerbaijan Transliterated
            lookupHash.Add("BAQ", "eu");  // Basque
            lookupHash.Add("BEL", "be");  // Belarusian
            lookupHash.Add("BEN", "bn");  // Bengali
            lookupHash.Add("BET", "be");  // Belarusian Transcribed
            lookupHash.Add("BEX", "be");  // Belarusian Transliterated
            lookupHash.Add("BGX", "bn");  // Bengali Transliterated
            lookupHash.Add("BOS", "bs");  // Bosnian
            lookupHash.Add("BOX", "bs");  // Bosnian Transliterated
            lookupHash.Add("BUL", "bg");  // Bulgarian
            lookupHash.Add("BUT", "bg");  // Bulgarian Transcribed
            lookupHash.Add("BUX", "bg");  // Bulgarian Transliterated
            lookupHash.Add("CAT", "ca");  // Catalan
            lookupHash.Add("CHI", "zh");  // Chinese (Modern)
            lookupHash.Add("CHT", "zh");  // Chinese (Traditional)
            lookupHash.Add("CZE", "cs");  // Czech
            lookupHash.Add("CZX", "cs");  // Czech Transliterated
            lookupHash.Add("DAN", "da");  // Danish
            lookupHash.Add("DUT", "nl");  // Dutch
            lookupHash.Add("ENG", "en");  // English
            lookupHash.Add("EST", "et");  // Estonian
            lookupHash.Add("ESX", "et");  // Estonian Transliterated
            lookupHash.Add("FAO", "fo");  // Faroese
            lookupHash.Add("FIN", "fi");  // Finnish
            lookupHash.Add("FRE", "fr");  // French
            lookupHash.Add("GEO", "ka");  // Georgian
            lookupHash.Add("GER", "de");  // German
            lookupHash.Add("GET", "ka");  // Georgian Transcribed
            lookupHash.Add("GEX", "ka");  // Georgian Transliterated
            lookupHash.Add("GJX", "gu");  // Gujarati Transliterated
            lookupHash.Add("GLE", "ga");  // Irish Gaelic
            lookupHash.Add("GLG", "gl");  // Galician
            lookupHash.Add("GRE", "el");  // Greek
            lookupHash.Add("GRN", "gn");  // Guarani
            lookupHash.Add("GRT", "el");  // Greek Transcribed
            lookupHash.Add("GRX", "el");  // Greek Transliterated
            lookupHash.Add("GUJ", "gu");  // Gujarati
            lookupHash.Add("HEB", "he");  // Hebrew
            lookupHash.Add("HIN", "hi");  // Hindi
            lookupHash.Add("HUN", "hu");  // Hungarian
            lookupHash.Add("HUX", "hu");  // Hungarian Transliterated
            lookupHash.Add("ICE", "is");  // Icelandic
            lookupHash.Add("IND", "id");  // Bahasa Indonesia
            lookupHash.Add("ITA", "it");  // Italian
            lookupHash.Add("JPN", "ja");  // Japanese
            lookupHash.Add("KAN", "kn");  // Kannada
            lookupHash.Add("KAT", "kk");  // Kazakh Transcribed
            lookupHash.Add("KAX", "kk");  // Kazakh Transliterated
            lookupHash.Add("KAZ", "kk");  // Kazakh
            lookupHash.Add("KIR", "ky");  // Kyrgyz
            lookupHash.Add("KIT", "ky");  // Kyrgyz Transcribed
            lookupHash.Add("KIX", "ky");  // Kyrgyz Transliterated
            lookupHash.Add("KNX", "kn");  // Kannada Transliterated
            lookupHash.Add("KOR", "ko");  // Korean
            lookupHash.Add("KOX", "ko");  // Korean Transliterated
            lookupHash.Add("LAV", "lv");  // Latvian
            lookupHash.Add("LAX", "lv");  // Latvian Transliterated
            lookupHash.Add("LIT", "lt");  // Lithuanian
            lookupHash.Add("LIX", "lt");  // Lithuanian Transliterated
            lookupHash.Add("MAC", "mk");  // Macedonian
            lookupHash.Add("MAL", "ml");  // Malayalam
            lookupHash.Add("MAR", "mr");  // Marathi
            lookupHash.Add("MAT", "mk");  // Macedonian Transcribed
            lookupHash.Add("MAY", "ms");  // Malaysian
            lookupHash.Add("MGX", "mn");  // Mongolian Transliterated
            lookupHash.Add("MLT", "mt");  // Maltese
            lookupHash.Add("MLX", "mt");  // Maltese Transliterated
            lookupHash.Add("MNE", "");  // Montenegrin
            lookupHash.Add("MNX", "");  // Montenegrin Transliterated
            lookupHash.Add("MOL", "mo");  // Moldovan
            lookupHash.Add("MON", "mn");  // Mongolian
            lookupHash.Add("MOX", "mo");  // Moldovan Transliterated
            lookupHash.Add("MRX", "mr");  // Marathi Transliterated
            lookupHash.Add("MYX", "ml");  // Malayalam Transliterated
            lookupHash.Add("NOR", "no");  // Norwegian
            lookupHash.Add("ORI", "or");  // Oriya
            lookupHash.Add("ORX", "or");  // Oriya Transliterated
            lookupHash.Add("OTH", "");  // Other
            lookupHash.Add("PAN", "pa");  // Punjabi
            lookupHash.Add("PNX", "pa");  // Punjabi Transliterated
            lookupHash.Add("POL", "pl");  // Polish
            lookupHash.Add("POR", "pt");  // Portuguese
            lookupHash.Add("POX", "pl");  // Polish Transliterated
            lookupHash.Add("PYN", "zh");  // Pinyin
            lookupHash.Add("RMX", "ro");  // Romanian Transliterated
            lookupHash.Add("RST", "ru");  // Russian Transcribed
            lookupHash.Add("RUM", "ro");  // Romanian
            lookupHash.Add("RUS", "ru");  // Russian
            lookupHash.Add("RUX", "ru");  // Russian Transliterated
            lookupHash.Add("SCR", "sh");  // Croatian
            lookupHash.Add("SCT", "sr");  // Serbian Transcribed
            lookupHash.Add("SCX", "sr");  // Serbian Transliterated
            lookupHash.Add("SIX", "sv");  // Slovenian Transliterated
            lookupHash.Add("SLO", "sk");  // Slovak
            lookupHash.Add("SLV", "sv");  // Slovenian
            lookupHash.Add("SLX", "sk");  // Slovak Transliterated
            lookupHash.Add("SPA", "es");  // Spanish
            lookupHash.Add("SRB", "sr");  // Serbian
            lookupHash.Add("SRX", "sh");  // Croatian Transliterated
            lookupHash.Add("SWE", "sv");  // Swedish
            lookupHash.Add("TAM", "ta");  // Tamil
            lookupHash.Add("TEL", "te");  // Telugu
            lookupHash.Add("THA", "th");  // Thai
            lookupHash.Add("THE", "en");  // Thai English
            lookupHash.Add("TKT", "tr");  // Turkish Transcribed
            lookupHash.Add("TLX", "te");  // Telugu Transliterated
            lookupHash.Add("TMX", "ta");  // Tamil Transliterated
            lookupHash.Add("TUR", "tr");  // Turkish
            lookupHash.Add("TUX", "tr");  // Turkish Transliterated
            lookupHash.Add("TWE", "en");  // Taiwan English
            lookupHash.Add("UKR", "uk");  // Ukranian
            lookupHash.Add("UKT", "uk");  // Ukranian Transcribed
            lookupHash.Add("UKX", "uk");  // Ukranian Transliterated
            lookupHash.Add("UND", "");  // Undefined
            lookupHash.Add("URD", "ur");  // Urdu
            lookupHash.Add("UZB", "uz");  // Uzbek
            lookupHash.Add("VIE", "vi");  // Vietnamese
            lookupHash.Add("WEL", "cy");  // Welsh
            lookupHash.Add("WEN", "en");  // World English
 
            return lookupHash;
        }

        private IGeometry MakeSignGeometry(ICurve fromEdgeCurve, ICurve toEdgeCurve,
                                           bool reverseFromEdge, bool reverseToEdge)
        {
            ISegmentCollection resultSegments = new PolylineClass();
            ICurve fromResultCurve, toResultCurve;

            // add the part from the first line

            if (reverseFromEdge)
            {
                fromEdgeCurve.GetSubcurve(0.0, 0.25, true, out fromResultCurve);
                fromResultCurve.ReverseOrientation();
            }
            else
            {
                fromEdgeCurve.GetSubcurve(0.75, 1.0, true, out fromResultCurve);
            }

            resultSegments.AddSegmentCollection(fromResultCurve as ISegmentCollection);


            // add the part from the second line

            if (reverseToEdge)
            {
                toEdgeCurve.GetSubcurve(0.75, 1.0, true, out toResultCurve);
                toResultCurve.ReverseOrientation();
            }
            else
            {
                toEdgeCurve.GetSubcurve(0.0, 0.25, true, out toResultCurve);
            }

            resultSegments.AddSegmentCollection(toResultCurve as ISegmentCollection);

            return resultSegments as IGeometry;
        }

        private void CreateAndBuildNetworkDataset(string outputFileGdbPath, double fgdbVersion, string fdsName, string ndsName,
                                                  bool createNetworkAttributesInMetric, bool createArcGISOnlineNetworkAttributes,
                                                  string timeZoneIDBaseFieldName, bool directedTimeZoneIDFields, string commonTimeZone,
                                                  bool usesHistoricalTraffic, ITrafficFeedLocation trafficFeedLocation, bool usesTransport)
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
            edgeFeatureSource.FromElevationFieldName = "F_ZLEV";
            edgeFeatureSource.ToElevationFieldName = "T_ZLEV";

            //
            // Specify directions settings for the edge source
            //

            // Create a StreetNameFields object for the primary street names and populate its settings.
            IStreetNameFields2 streetNameFields = new StreetNameFieldsClass();
            streetNameFields.Priority = 1; // Priority 1 indicates the primary street name.
            streetNameFields.LanguageFieldName = "Language";
            streetNameFields.FullNameFieldName = "ST_NAME";
            streetNameFields.PrefixDirectionFieldName = "ST_NM_PREF";
            streetNameFields.PrefixTypeFieldName = "ST_TYP_BEF";
            streetNameFields.StreetNameFieldName = "ST_NM_BASE";
            streetNameFields.SuffixTypeFieldName = "ST_TYP_AFT";
            streetNameFields.SuffixDirectionFieldName = "ST_NM_SUFF";
            streetNameFields.HighwayDirectionFieldName = "DIRONSIGN";

            // Create a StreetNameFields object for the alternate street names and populate its settings.
            IStreetNameFields2 altStreetNameFields = new StreetNameFieldsClass();
            altStreetNameFields.Priority = 2; // Priority 2 indicates the alternate street name.
            altStreetNameFields.LanguageFieldName = "Language_Alt";
            altStreetNameFields.FullNameFieldName = "ST_NAME_Alt";
            altStreetNameFields.PrefixDirectionFieldName = "ST_NM_PREF_Alt";
            altStreetNameFields.PrefixTypeFieldName = "ST_TYP_BEF_Alt";
            altStreetNameFields.StreetNameFieldName = "ST_NM_BASE_Alt";
            altStreetNameFields.SuffixTypeFieldName = "ST_TYP_AFT_Alt";
            altStreetNameFields.SuffixDirectionFieldName = "ST_NM_SUFF_Alt";
            altStreetNameFields.HighwayDirectionFieldName = "DIRONSIGN_Alt";

            // Add the StreetNameFields objects to a new NetworkSourceDirections object,
            // then add it to the EdgeFeatureSource created earlier.
            INetworkSourceDirections nsDirections = new NetworkSourceDirectionsClass();
            IArray nsdArray = new ArrayClass();
            nsdArray.Add(streetNameFields as IStreetNameFields);
            nsdArray.Add(altStreetNameFields as IStreetNameFields);
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
            
            if (usesHistoricalTraffic || trafficFeedLocation != null)
            {
                // Create a new TrafficData object and populate its historical and live traffic settings.
                var traffData = new TrafficDataClass() as ITrafficData2;
                traffData.LengthAttributeName = createNetworkAttributesInMetric ? "Kilometers" : "Miles";

                // Populate the speed profile table settings.
                var histTraff = traffData as IHistoricalTrafficData2;
                histTraff.ProfilesTableName = ProfilesTableName;
                if (usesHistoricalTraffic)
                {
                    if (fgdbVersion == 10.0)
                    {
                        histTraff.FirstTimeSliceFieldName = "TimeFactor_0000";
                        histTraff.LastTimeSliceFieldName = "TimeFactor_2345";
                    }
                    else
                    {
                        histTraff.FirstTimeSliceFieldName = "SpeedFactor_0000";
                        histTraff.LastTimeSliceFieldName = "SpeedFactor_2345";
                    }
                }
                else
                {
                    histTraff.FirstTimeSliceFieldName = "SpeedFactor_AM";
                    histTraff.LastTimeSliceFieldName = "SpeedFactor_PM";
                }
                histTraff.TimeSliceDurationInMinutes = usesHistoricalTraffic ? 15 : 720;
                histTraff.FirstTimeSliceStartTime = new DateTime(1, 1, 1, 0, 0, 0); // 12 AM
                // Note: the last time slice finish time is implied from the above settings and need not be specified.

                // Populate the street-speed profile join table settings.
                histTraff.JoinTableName = HistTrafficJoinTableName;
                if (usesHistoricalTraffic)
                {
                    if (fgdbVersion == 10.0)
                    {
                        histTraff.JoinTableBaseTravelTimeFieldName = "BaseMinutes";
                        histTraff.JoinTableBaseTravelTimeUnits = esriNetworkAttributeUnits.esriNAUMinutes;
                    }
                    else
                    {
                        histTraff.JoinTableBaseSpeedFieldName = "BaseSpeed";
                        histTraff.JoinTableBaseSpeedUnits = esriNetworkAttributeUnits.esriNAUKilometersPerHour;
                    }
                }
                else
                {
                    histTraff.JoinTableBaseSpeedFieldName = "KPH";
                    histTraff.JoinTableBaseSpeedUnits = esriNetworkAttributeUnits.esriNAUKilometersPerHour;
                }
                IStringArray fieldNames = new NamesClass();
                fieldNames.Add("U");
                fieldNames.Add("M");
                fieldNames.Add("T");
                fieldNames.Add("W");
                fieldNames.Add("R");
                fieldNames.Add("F");
                fieldNames.Add("S");
                histTraff.JoinTableProfileIDFieldNames = fieldNames;

                // For creating 10.1 and later, populate the dynamic traffic settings.
                if (fgdbVersion >= 10.1 && trafficFeedLocation != null)
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

            if (!createArcGISOnlineNetworkAttributes)
            {
                //
                // Oneway network attribute
                //

                // Create an EvaluatedNetworkAttribute object and populate its settings.
                evalNetAttr = CreateRestrAttrNoEvals("Oneway", fgdbVersion, -1.0, true, "", "");

                // Create evaluator objects and set them on the EvaluatedNetworkAttribute object.
                netFieldEval = new NetworkFieldEvaluatorClass();
                netFieldEval.SetExpression("restricted", "restricted = False\n\r" +
                                           "Select Case UCase([DIR_TRAVEL])\n\r" +
                                           "  Case \"N\", \"TF\", \"T\": restricted = True\n\r" +
                                           "End Select");
                evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAlongDigitized, (INetworkEvaluator)netFieldEval);

                netFieldEval = new NetworkFieldEvaluatorClass();
                netFieldEval.SetExpression("restricted", "restricted = False\n\r" +
                                           "Select Case UCase([DIR_TRAVEL])\n\r" +
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
            }

            //
            // Minutes network attribute
            //

            // Create an EvaluatedNetworkAttribute object and populate its settings.
            evalNetAttr = new EvaluatedNetworkAttributeClass();
            netAttr2 = (INetworkAttribute2)evalNetAttr;
            netAttr2.Name = "Minutes";
            netAttr2.UsageType = esriNetworkAttributeUsageType.esriNAUTCost;
            netAttr2.DataType = esriNetworkAttributeDataType.esriNADTDouble;
            netAttr2.Units = esriNetworkAttributeUnits.esriNAUMinutes;
            netAttr2.UseByDefault = createArcGISOnlineNetworkAttributes ? false : !(usesHistoricalTraffic || (trafficFeedLocation != null));

            if (usesHistoricalTraffic)
            {
                // Create evaluator objects and set them on the EvaluatedNetworkAttribute object.
                netFieldEval = new NetworkFieldEvaluatorClass();
                netFieldEval.SetExpression("[FT_Minutes]", "");
                evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAlongDigitized, (INetworkEvaluator)netFieldEval);

                netFieldEval = new NetworkFieldEvaluatorClass();
                netFieldEval.SetExpression("[TF_Minutes]", "");
                evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAgainstDigitized, (INetworkEvaluator)netFieldEval);
            }
            else
            {
                // Create evaluator objects and set them on the EvaluatedNetworkAttribute object.
                netFieldEval = new NetworkFieldEvaluatorClass();
                netFieldEval.SetExpression("[Minutes]", "");
                evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAlongDigitized, (INetworkEvaluator)netFieldEval);

                netFieldEval = new NetworkFieldEvaluatorClass();
                netFieldEval.SetExpression("[Minutes]", "");
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

            // Add the attribute to the array.
            attributeArray.Add(evalNetAttr);

            //
            // Length network attribute(s)
            //

            if (createArcGISOnlineNetworkAttributes || createNetworkAttributesInMetric)
            {
                evalNetAttr = CreateLengthNetworkAttribute("Kilometers", esriNetworkAttributeUnits.esriNAUKilometers,
                                                           "[Meters] / 1000", edgeNetworkSource);
                // Add the attribute to the array.
                attributeArray.Add(evalNetAttr);
            }
            if (createArcGISOnlineNetworkAttributes || !createNetworkAttributesInMetric)
            {
                evalNetAttr = CreateLengthNetworkAttribute("Miles", esriNetworkAttributeUnits.esriNAUMiles,
                                                           "[Meters] / 1609.344", edgeNetworkSource);
                // Add the attribute to the array.
                attributeArray.Add(evalNetAttr);
            }

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
                                         "If UCase([FERRY_TYPE]) = \"B\" Then\n\r" +
                                         "  rc = 4          'Ferry\n\r" +
                                         "ElseIf UCase([ROUNDABOUT]) = \"Y\" Or UCase([SPECTRFIG]) = \"Y\" Then\n\r" +
                                         "  rc = 5          'Roundabout\n\r" +
                                         "ElseIf UCase([RAMP]) = \"Y\" Then\n\r" +
                                         "  rc = 3          'Ramp\n\r" +
                                         "ElseIf UCase([CONTRACC]) = \"Y\" Then\n\r" +
                                         "  rc = 2          'Highway\n\r" +
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
                                                 "If UCase([INTERINTER]) = \"Y\" Then\n\r" +
                                                 "  mc = 1          'Intersection Internal\n\r" +
                                                 "ElseIf UCase([MANOEUVRE]) = \"Y\" Then\n\r" +
                                                 "  mc = 2          'Maneuver\n\r" +
                                                 "End If";

                netFieldEval = new NetworkFieldEvaluatorClass();
                netFieldEval.SetExpression("mc", maneuverClassExpression);
                evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAlongDigitized, (INetworkEvaluator)netFieldEval);

                netFieldEval = new NetworkFieldEvaluatorClass();
                netFieldEval.SetExpression("mc", maneuverClassExpression);
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

            if (usesHistoricalTraffic || trafficFeedLocation != null)
            {
                //
                // TravelTime network attribute
                //

                evalNetAttr = CreateTrafficAttrWSpeedCapParam("TravelTime", edgeNetworkSource, "Minutes", "Minutes", "Minutes", true);

                // Add the attribute to the array.
                attributeArray.Add(evalNetAttr);
            }
            else if (createArcGISOnlineNetworkAttributes)
            {
                //
                // TravelTime network attribute (dummy that is the same as the Minutes network attribute)
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
                netFieldEval = new NetworkFieldEvaluatorClass();
                netFieldEval.SetExpression("[Minutes]", "");
                evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAlongDigitized, (INetworkEvaluator)netFieldEval);

                netFieldEval = new NetworkFieldEvaluatorClass();
                netFieldEval.SetExpression("[Minutes]", "");
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

            //
            // TimeAt1KPH attribute
            //

            // Create an EvaluatedNetworkAttribute object and populate its settings.
            evalNetAttr = new EvaluatedNetworkAttributeClass();
            netAttr2 = (INetworkAttribute2)evalNetAttr;
            netAttr2.Name = "TimeAt1KPH";
            netAttr2.UsageType = esriNetworkAttributeUsageType.esriNAUTCost;
            netAttr2.DataType = esriNetworkAttributeDataType.esriNADTDouble;
            netAttr2.Units = esriNetworkAttributeUnits.esriNAUMinutes;
            netAttr2.UseByDefault = false;

            // Create evaluator objects and set them on the EvaluatedNetworkAttribute object.
            netFieldEval = new NetworkFieldEvaluatorClass();
            netFieldEval.SetExpression("[Meters] * 0.06", "");
            evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAlongDigitized, (INetworkEvaluator)netFieldEval);

            netFieldEval = new NetworkFieldEvaluatorClass();
            netFieldEval.SetExpression("[Meters] * 0.06", "");
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
            // WalkTime attribute
            //

            // Create an EvaluatedNetworkAttribute object and populate its settings.
            evalNetAttr = new EvaluatedNetworkAttributeClass();
            netAttr2 = (INetworkAttribute2)evalNetAttr;
            netAttr2.Name = "WalkTime";
            netAttr2.UsageType = esriNetworkAttributeUsageType.esriNAUTCost;
            netAttr2.DataType = esriNetworkAttributeDataType.esriNADTDouble;
            netAttr2.Units = esriNetworkAttributeUnits.esriNAUMinutes;
            netAttr2.UseByDefault = false;

            // Create a parameter for Walking Speed (km/h) and add it to the network attribute.
            INetworkAttributeParameter2 netAttrParam = new NetworkAttributeParameterClass();
            netAttrParam.Name = "Walking Speed (km/h)";
            netAttrParam.VarType = (int)(VarEnum.VT_R8);
            netAttrParam.DefaultValue = 5.0;
            netAttrParam.ParameterUsageType = esriNetworkAttributeParameterUsageType.esriNAPUTGeneral;
            IArray paramArray = new ArrayClass();
            paramArray.Add(netAttrParam);
            netAttr2.Parameters = paramArray;

            // Create evaluator objects and set them on the EvaluatedNetworkAttribute object.
            INetworkFunctionEvaluator netFuncEval = new NetworkFunctionEvaluatorClass();
            netFuncEval.FirstArgument = "TimeAt1KPH";
            netFuncEval.Operator = "/";
            netFuncEval.SecondArgument = "Walking Speed (km/h)";
            evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAlongDigitized, (INetworkEvaluator)netFuncEval);

            netFuncEval = new NetworkFunctionEvaluatorClass();
            netFuncEval.FirstArgument = "TimeAt1KPH";
            netFuncEval.Operator = "/";
            netFuncEval.SecondArgument = "Walking Speed (km/h)";
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

            //
            // Vehicle-specific network attributes
            //

            evalNetAttr = CreateVehicleNetworkAttribute("Driving an Automobile", true, -1.0, createArcGISOnlineNetworkAttributes,
                                                        "AR_AUTO", "AR_AUTO", fgdbVersion, edgeNetworkSource, turnNetworkSource, usesTransport);
            attributeArray.Add(evalNetAttr);

            evalNetAttr = CreateVehicleNetworkAttribute("Driving a Bus", false, -1.0, createArcGISOnlineNetworkAttributes,
                                                        "AR_BUS", "AR_BUS", fgdbVersion, edgeNetworkSource, turnNetworkSource, usesTransport);
            attributeArray.Add(evalNetAttr);

            evalNetAttr = CreateVehicleNetworkAttribute("Driving a Taxi", false, -1.0, createArcGISOnlineNetworkAttributes,
                                                        "AR_TAXIS", "AR_TAXIS", fgdbVersion, edgeNetworkSource, turnNetworkSource, usesTransport);
            attributeArray.Add(evalNetAttr);

            evalNetAttr = CreateVehicleNetworkAttribute("Walking", false, -1.0, false, "AR_PEDEST", "AR_PEDSTRN",
                                                        fgdbVersion, edgeNetworkSource, turnNetworkSource, usesTransport);
            attributeArray.Add(evalNetAttr);

            evalNetAttr = CreateVehicleNetworkAttribute("Avoid Truck Restricted Roads", false, AvoidHighFactor, createArcGISOnlineNetworkAttributes,
                                                        "AR_TRUCKS", "", fgdbVersion, edgeNetworkSource, turnNetworkSource, usesTransport);
            attributeArray.Add(evalNetAttr);

            evalNetAttr = CreateDrivingATruckNetworkAttribute(createArcGISOnlineNetworkAttributes, fgdbVersion, edgeNetworkSource, turnNetworkSource, usesTransport);
            attributeArray.Add(evalNetAttr);

            evalNetAttr = CreateVehicleNetworkAttribute("Driving an Emergency Vehicle", false, -1.0, createArcGISOnlineNetworkAttributes,
                                                        "AR_EMERVEH", "AR_EMERVEH", fgdbVersion, edgeNetworkSource, turnNetworkSource, usesTransport);
            attributeArray.Add(evalNetAttr);

            evalNetAttr = CreateVehicleNetworkAttribute("Riding a Motorcycle", false, -1.0, createArcGISOnlineNetworkAttributes,
                                                        "AR_MOTOR", "AR_MOTOR", fgdbVersion, edgeNetworkSource, turnNetworkSource, usesTransport);
            attributeArray.Add(evalNetAttr);

            //
            // Through Traffic Prohibited network attribute
            //

            evalNetAttr = CreateVehicleNetworkAttribute("Through Traffic Prohibited", (fgdbVersion >= 10.1), AvoidHighFactor, false,
                                                        "AR_TRAFF", "AR_THRUTR", fgdbVersion, edgeNetworkSource, turnNetworkSource, usesTransport);
            attributeArray.Add(evalNetAttr);

            //
            // Avoid-type network attributes
            //

            evalNetAttr = CreateAvoidNetworkAttribute("Avoid Unpaved Roads", "[PAVED] = \"N\"",
                                                      true, fgdbVersion, edgeNetworkSource, AvoidHighFactor);
            attributeArray.Add(evalNetAttr);

            evalNetAttr = CreateAvoidNetworkAttribute("Avoid Toll Roads", "[UFR_AUTO] = \"Y\"",
                                                      false, fgdbVersion, edgeNetworkSource);
            attributeArray.Add(evalNetAttr);

            evalNetAttr = CreateAvoidNetworkAttribute("Avoid Toll Roads for Trucks", "[UFR_TRUCKS] = \"Y\"",
                                                      false, fgdbVersion, edgeNetworkSource);
            attributeArray.Add(evalNetAttr);

            evalNetAttr = CreateAvoidNetworkAttribute("Avoid Limited Access Roads", "[CONTRACC] = \"Y\"",
                                                      false, fgdbVersion, edgeNetworkSource);
            attributeArray.Add(evalNetAttr);

            evalNetAttr = CreateAvoidNetworkAttribute("Avoid Ferries", "[FERRY_TYPE] = \"B\" Or [FERRY_TYPE] = \"R\"",
                                                      false, fgdbVersion, edgeNetworkSource);
            attributeArray.Add(evalNetAttr);

            evalNetAttr = CreateAvoidNetworkAttribute("Avoid Express Lanes", "[EXPR_LANE] = \"Y\"",
                                                      true, fgdbVersion, edgeNetworkSource, -1.0);
            attributeArray.Add(evalNetAttr);

            evalNetAttr = CreateAvoidNetworkAttribute("Avoid Carpool Roads", "[CARPOOLRD] = \"Y\"",
                                                      true, fgdbVersion, edgeNetworkSource, -1.0);
            attributeArray.Add(evalNetAttr);

            evalNetAttr = CreateAvoidNetworkAttribute("Avoid Private Roads", "[PUB_ACCESS] = \"N\"",
                                                      true, fgdbVersion, edgeNetworkSource);
            attributeArray.Add(evalNetAttr);

            evalNetAttr = CreateAvoidNetworkAttribute("Roads Under Construction Prohibited", "[ClosedForConstruction] = \"Y\"",
                                                      true, fgdbVersion, edgeNetworkSource, -1.0);
            attributeArray.Add(evalNetAttr);

            //
            // Avoid Gates network attribute
            //

            // Create an EvaluatedNetworkAttribute object and populate its settings.
            evalNetAttr = CreateRestrAttrNoEvals("Avoid Gates", fgdbVersion, AvoidMediumFactor, (fgdbVersion >= 10.1), "", "");

            // Create evaluator objects and set them on the EvaluatedNetworkAttribute object.
            netFieldEval = new NetworkFieldEvaluatorClass();
            netFieldEval.SetExpression("[COND_TYPE] = 4", "");
            evalNetAttr.set_Evaluator(turnNetworkSource, esriNetworkEdgeDirection.esriNEDNone, (INetworkEvaluator)netFieldEval);

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
            if (usesTransport)
            {
                string transportHierarchyPreLogic = "h = CInt( [FUNC_CLASS] )\n" +
                                                    "If Not IsNull([TruckFCOverride]) Then h = [TruckFCOverride]";

                netFieldEval = new NetworkFieldEvaluatorClass();
                netFieldEval.SetExpression("h", transportHierarchyPreLogic);
                evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAlongDigitized, (INetworkEvaluator)netFieldEval);

                netFieldEval = new NetworkFieldEvaluatorClass();
                netFieldEval.SetExpression("h", transportHierarchyPreLogic);
                evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAgainstDigitized, (INetworkEvaluator)netFieldEval);
            }
            else
            {
                netFieldEval = new NetworkFieldEvaluatorClass();
                netFieldEval.SetExpression("CInt( [FUNC_CLASS] )", "");
                evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAlongDigitized, (INetworkEvaluator)netFieldEval);

                netFieldEval = new NetworkFieldEvaluatorClass();
                netFieldEval.SetExpression("CInt( [FUNC_CLASS] )", "");
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

            // Add the attribute to the array.
            attributeArray.Add(evalNetAttr);

            // Since this is the hierarchy attribute, also set it as the hierarchy cluster attribute.
            deNetworkDataset.HierarchyClusterAttribute = (INetworkAttribute)evalNetAttr;

            // Specify the ranges for the hierarchy levels.
            deNetworkDataset.HierarchyLevelCount = 3;
            deNetworkDataset.set_MaxValueForHierarchy(1, 2); // level 1: up to 2
            deNetworkDataset.set_MaxValueForHierarchy(2, 4); // level 2: 3 - 4
            deNetworkDataset.set_MaxValueForHierarchy(3, 5); // level 3: 5 and higher (these values only go up to 5)

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

            #region Add network attributes for Transport restrictions

            if (usesTransport)
            {
                string distUnit = " (feet)";
                string wtUnit = " (pounds)";
                if (createNetworkAttributesInMetric || createArcGISOnlineNetworkAttributes)
                {
                    distUnit = " (meters)";
                    wtUnit = " (kilograms)";
                }

                if (fgdbVersion >= 10.1)
                {
                    // Create attributes for preferred truck routes

                    if (createArcGISOnlineNetworkAttributes)
                    {
                        evalNetAttr = CreateLoadRestrictionAttribute("Use Preferred Truck Routes", true,
                                                                     "PreferredTruckRoute", fgdbVersion, edgeNetworkSource, null);
                        attributeArray.Add(evalNetAttr);
                    }
                    else
                    {
                        evalNetAttr = CreateMultiFieldLoadRestrictionAttribute("National STAA Preferred Route", true,
                                                                               new string[] { "STAAPreferred" },
                                                                               fgdbVersion, edgeNetworkSource, null);
                        attributeArray.Add(evalNetAttr);

                        evalNetAttr = CreateMultiFieldLoadRestrictionAttribute("National STAA and State Truck Designated Preferred Routes", true,
                                                                               new string[] { "STAAPreferred", "TruckDesignatedPreferred" },
                                                                               fgdbVersion, edgeNetworkSource, null);
                        attributeArray.Add(evalNetAttr);

                        evalNetAttr = CreateMultiFieldLoadRestrictionAttribute("National STAA and State Truck Designated and Locally Preferred Routes", true,
                                                                               new string[] { "STAAPreferred", "TruckDesignatedPreferred", "LocallyPreferred" },
                                                                               fgdbVersion, edgeNetworkSource, null);
                        attributeArray.Add(evalNetAttr);
                    }

                    // Create attributes for other preferred routes

                    if (createArcGISOnlineNetworkAttributes)
                    {
                        evalNetAttr = CreateLoadRestrictionAttribute("Use Preferred Hazmat Routes", true,
                                                                     "PreferredHazmatRoute", fgdbVersion, edgeNetworkSource, null);
                        attributeArray.Add(evalNetAttr);
                    }
                    else
                    {
                        evalNetAttr = CreateLoadRestrictionAttribute("Hazmat: NRHM Preferred Route", true,
                                                                     "NRHMPreferred", fgdbVersion, edgeNetworkSource, null);
                        attributeArray.Add(evalNetAttr);

                        evalNetAttr = CreateLoadRestrictionAttribute("Hazmat: Explosives Preferred Route", true,
                                                                     "ExplosivesPreferred", fgdbVersion, edgeNetworkSource, null);
                        attributeArray.Add(evalNetAttr);

                        evalNetAttr = CreateLoadRestrictionAttribute("Hazmat: Poisonous Inhalation Hazard Preferred Route", true,
                                                                     "PIHPreferred", fgdbVersion, edgeNetworkSource, null);
                        attributeArray.Add(evalNetAttr);

                        evalNetAttr = CreateLoadRestrictionAttribute("Hazmat: Medical Waste Materials Preferred Route", true,
                                                                     "MedicalWastePreferred", fgdbVersion, edgeNetworkSource, null);
                        attributeArray.Add(evalNetAttr);

                        evalNetAttr = CreateLoadRestrictionAttribute("Hazmat: Radioactive Materials Preferred Route", true,
                                                                     "RadioactivePreferred", fgdbVersion, edgeNetworkSource, null);
                        attributeArray.Add(evalNetAttr);

                        evalNetAttr = CreateLoadRestrictionAttribute("Hazmat: General Hazardous Materials Preferred Route", true,
                                                                     "HazmatPreferred", fgdbVersion, edgeNetworkSource, null);
                        attributeArray.Add(evalNetAttr);
                    }
                }

                // Create attributes for prohibited Hazmat routes

                if (createArcGISOnlineNetworkAttributes)
                {
                    evalNetAttr = CreateLoadRestrictionAttribute("Any Hazmat Prohibited", false,
                                                                 "AGOL_AnyHazmatProhibited", fgdbVersion, edgeNetworkSource, turnNetworkSource);
                    attributeArray.Add(evalNetAttr);
                }
                else
                {
                    evalNetAttr = CreateLoadRestrictionAttribute("Hazmat: Explosives Prohibited", false,
                                                                 "ExplosivesProhibited", fgdbVersion, edgeNetworkSource, turnNetworkSource);
                    attributeArray.Add(evalNetAttr);

                    evalNetAttr = CreateLoadRestrictionAttribute("Hazmat: Gas Prohibited", false,
                                                                 "GasProhibited", fgdbVersion, edgeNetworkSource, turnNetworkSource);
                    attributeArray.Add(evalNetAttr);

                    evalNetAttr = CreateLoadRestrictionAttribute("Hazmat: Flammable Goods Prohibited", false,
                                                                 "FlammableProhibited", fgdbVersion, edgeNetworkSource, turnNetworkSource);
                    attributeArray.Add(evalNetAttr);

                    evalNetAttr = CreateLoadRestrictionAttribute("Hazmat: Flammable solid/Combustible Prohibited", false,
                                                                 "CombustibleProhibited", fgdbVersion, edgeNetworkSource, turnNetworkSource);
                    attributeArray.Add(evalNetAttr);

                    evalNetAttr = CreateLoadRestrictionAttribute("Hazmat: Organic Goods Prohibited", false,
                                                                 "OrganicProhibited", fgdbVersion, edgeNetworkSource, turnNetworkSource);
                    attributeArray.Add(evalNetAttr);

                    evalNetAttr = CreateLoadRestrictionAttribute("Hazmat: Poison Goods Prohibited", false,
                                                                 "PoisonProhibited", fgdbVersion, edgeNetworkSource, turnNetworkSource);
                    attributeArray.Add(evalNetAttr);

                    evalNetAttr = CreateLoadRestrictionAttribute("Hazmat: Radioactive Goods Prohibited", false,
                                                                 "RadioactiveProhibited", fgdbVersion, edgeNetworkSource, turnNetworkSource);
                    attributeArray.Add(evalNetAttr);

                    evalNetAttr = CreateLoadRestrictionAttribute("Hazmat: Corrosive Goods Prohibited", false,
                                                                 "CorrosiveProhibited", fgdbVersion, edgeNetworkSource, turnNetworkSource);
                    attributeArray.Add(evalNetAttr);

                    evalNetAttr = CreateLoadRestrictionAttribute("Hazmat: Other Hazardous Materials Prohibited", false,
                                                                 "OtherHazmatProhibited", fgdbVersion, edgeNetworkSource, turnNetworkSource);
                    attributeArray.Add(evalNetAttr);

                    evalNetAttr = CreateLoadRestrictionAttribute("Hazmat: Any Hazardous Materials Prohibited", false,
                                                                 "AnyHazmatProhibited", fgdbVersion, edgeNetworkSource, turnNetworkSource);
                    attributeArray.Add(evalNetAttr);

                    evalNetAttr = CreateLoadRestrictionAttribute("Hazmat: Poisonous Inhalation Hazard Prohibited", false,
                                                                 "PIHProhibited", fgdbVersion, edgeNetworkSource, turnNetworkSource);
                    attributeArray.Add(evalNetAttr);

                    evalNetAttr = CreateLoadRestrictionAttribute("Hazmat: Goods Harmful to Water Prohibited", false,
                                                                 "HarmfulToWaterProhibited", fgdbVersion, edgeNetworkSource, turnNetworkSource);
                    attributeArray.Add(evalNetAttr);

                    evalNetAttr = CreateLoadRestrictionAttribute("Hazmat: Explosive and Flammable Prohibited", false,
                                                                 "ExplosiveAndFlammableProhibited", fgdbVersion, edgeNetworkSource, turnNetworkSource);
                    attributeArray.Add(evalNetAttr);
                }

                // Create attributes for other restricted routes

                evalNetAttr = CreateDimensionalLimitAttribute("Height Limit" + distUnit, "HeightLimit_Meters", createNetworkAttributesInMetric,
                                                              false, edgeNetworkSource, turnNetworkSource);
                attributeArray.Add(evalNetAttr);
                evalNetAttr = CreateDimensionalRestrictionAttribute("Height Restriction", "Height Limit" + distUnit, "Vehicle Height" + distUnit,
                                                                    fgdbVersion, edgeNetworkSource, turnNetworkSource);
                attributeArray.Add(evalNetAttr);

                evalNetAttr = CreateDimensionalLimitAttribute("Weight Limit" + wtUnit, "WeightLimit_Kilograms", createNetworkAttributesInMetric,
                                                              true, edgeNetworkSource, turnNetworkSource);
                attributeArray.Add(evalNetAttr);
                evalNetAttr = CreateDimensionalRestrictionAttribute("Weight Restriction", "Weight Limit" + wtUnit, "Vehicle Weight" + wtUnit,
                                                                    fgdbVersion, edgeNetworkSource, turnNetworkSource);
                attributeArray.Add(evalNetAttr);

                evalNetAttr = CreateDimensionalLimitAttribute("Weight Limit per Axle" + wtUnit, "WeightLimitPerAxle_Kilograms", createNetworkAttributesInMetric,
                                                              true, edgeNetworkSource, turnNetworkSource);
                attributeArray.Add(evalNetAttr);
                evalNetAttr = CreateDimensionalRestrictionAttribute("Weight per Axle Restriction", "Weight Limit per Axle" + wtUnit, "Vehicle Weight per Axle" + wtUnit,
                                                                    fgdbVersion, edgeNetworkSource, turnNetworkSource);
                attributeArray.Add(evalNetAttr);

                evalNetAttr = CreateDimensionalLimitAttribute("Length Limit" + distUnit, "LengthLimit_Meters", createNetworkAttributesInMetric,
                                                              false, edgeNetworkSource, turnNetworkSource);
                attributeArray.Add(evalNetAttr);
                evalNetAttr = CreateDimensionalRestrictionAttribute("Length Restriction", "Length Limit" + distUnit, "Vehicle Length" + distUnit,
                                                                    fgdbVersion, edgeNetworkSource, turnNetworkSource);
                attributeArray.Add(evalNetAttr);

                evalNetAttr = CreateDimensionalLimitAttribute("Width Limit" + distUnit, "WidthLimit_Meters", createNetworkAttributesInMetric,
                                                              false, edgeNetworkSource, turnNetworkSource);
                attributeArray.Add(evalNetAttr);
                evalNetAttr = CreateDimensionalRestrictionAttribute("Width Restriction", "Width Limit" + distUnit, "Vehicle Width" + distUnit,
                                                                    fgdbVersion, edgeNetworkSource, turnNetworkSource);
                attributeArray.Add(evalNetAttr);

                evalNetAttr = CreateQuantityLimitAttribute("Maximum Number of Trailers Allowed on Truck", "MaxTrailersAllowedOnTruck",
                                                           edgeNetworkSource, turnNetworkSource);
                attributeArray.Add(evalNetAttr);
                evalNetAttr = CreateQuantityRestrictionAttribute("Truck with Trailers Restriction", "Maximum Number of Trailers Allowed on Truck", "Number of Trailers on Truck",
                                                                 fgdbVersion, edgeNetworkSource, turnNetworkSource);
                attributeArray.Add(evalNetAttr);

                evalNetAttr = CreateLoadRestrictionAttribute("Semi or Tractor with One or More Trailers Prohibited", false,
                                                             "SemiOrTractorWOneOrMoreTrailersProhibited", fgdbVersion, edgeNetworkSource, turnNetworkSource);
                attributeArray.Add(evalNetAttr);

                evalNetAttr = CreateQuantityLimitAttribute("Maximum Number of Axles Allowed", "MaxAxlesAllowed",
                                                           edgeNetworkSource, turnNetworkSource);
                attributeArray.Add(evalNetAttr);
                evalNetAttr = CreateQuantityRestrictionAttribute("Axle Count Restriction", "Maximum Number of Axles Allowed", "Number of Axles",
                                                                 fgdbVersion, edgeNetworkSource, turnNetworkSource);
                attributeArray.Add(evalNetAttr);

                evalNetAttr = CreateLoadRestrictionAttribute("Single Axle Vehicles Prohibited", false,
                                                             "SingleAxleProhibited", fgdbVersion, edgeNetworkSource, turnNetworkSource);
                attributeArray.Add(evalNetAttr);

                evalNetAttr = CreateLoadRestrictionAttribute("Tandem Axle Vehicles Prohibited", false,
                                                             "TandemAxleProhibited", fgdbVersion, edgeNetworkSource, turnNetworkSource);
                attributeArray.Add(evalNetAttr);

                evalNetAttr = CreateDimensionalLimitAttribute("Kingpin to Rear Axle Length Limit" + distUnit, "KingpinToRearAxleLengthLimit_Meters", createNetworkAttributesInMetric,
                                                              false, edgeNetworkSource, turnNetworkSource);
                attributeArray.Add(evalNetAttr);
                evalNetAttr = CreateDimensionalRestrictionAttribute("Kingpin to Rear Axle Length Restriction", "Kingpin to Rear Axle Length Limit" + distUnit, "Vehicle Kingpin to Rear Axle Length" + distUnit,
                                                                    fgdbVersion, edgeNetworkSource, turnNetworkSource);
                attributeArray.Add(evalNetAttr);

                //
                // TruckMinutes attribute
                //

                // Create an EvaluatedNetworkAttribute object and populate its settings.
                evalNetAttr = new EvaluatedNetworkAttributeClass();
                netAttr2 = (INetworkAttribute2)evalNetAttr;
                netAttr2.Name = "TruckMinutes";
                netAttr2.UsageType = esriNetworkAttributeUsageType.esriNAUTCost;
                netAttr2.DataType = esriNetworkAttributeDataType.esriNADTDouble;
                netAttr2.Units = esriNetworkAttributeUnits.esriNAUMinutes;
                netAttr2.UseByDefault = false;

                // Create evaluator objects and set them on the EvaluatedNetworkAttribute object.
                if (usesHistoricalTraffic)
                {
                    netFieldEval = new NetworkFieldEvaluatorClass();
                    netFieldEval.SetExpression("ttt", "ttt = [FT_Minutes]\ntruckSpeed = [FT_TruckKPH]\nIf Not IsNull(truckSpeed) Then ttt = [Meters] * 0.06 / truckSpeed\nIf ttt < [FT_Minutes] Then ttt = [FT_Minutes]");
                    evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAlongDigitized, (INetworkEvaluator)netFieldEval);

                    netFieldEval = new NetworkFieldEvaluatorClass();
                    netFieldEval.SetExpression("ttt", "ttt = [TF_Minutes]\ntruckSpeed = [TF_TruckKPH]\nIf Not IsNull(truckSpeed) Then ttt = [Meters] * 0.06 / truckSpeed\nIf ttt < [TF_Minutes] Then ttt = [TF_Minutes]");
                    evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAgainstDigitized, (INetworkEvaluator)netFieldEval);
                }
                else
                {
                    netFieldEval = new NetworkFieldEvaluatorClass();
                    netFieldEval.SetExpression("[Meters] * 0.06 / truckSpeed", "truckSpeed = [FT_TruckKPH]\nIf IsNull(truckSpeed) Then truckSpeed = [KPH]\nIf truckSpeed > [KPH] Then truckSpeed = [KPH]");
                    evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAlongDigitized, (INetworkEvaluator)netFieldEval);

                    netFieldEval = new NetworkFieldEvaluatorClass();
                    netFieldEval.SetExpression("[Meters] * 0.06 / truckSpeed", "truckSpeed = [TF_TruckKPH]\nIf IsNull(truckSpeed) Then truckSpeed = [KPH]\nIf truckSpeed > [KPH] Then truckSpeed = [KPH]");
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

                // Add the attribute to the array.
                attributeArray.Add(evalNetAttr);

                if (usesHistoricalTraffic)
                {
                    //
                    // TruckTravelTime network attribute
                    //

                    evalNetAttr = CreateTrafficAttrWSpeedCapParam("TruckTravelTime", edgeNetworkSource, "TruckMinutes", "TruckMinutes", "TruckMinutes", false);

                    // Add the attribute to the array.
                    attributeArray.Add(evalNetAttr);

                    //
                    // TruckTravelTime Speed Limit (km/h) network attribute
                    //

                    // Create an EvaluatedNetworkAttribute object and populate its settings.
                    evalNetAttr = new EvaluatedNetworkAttributeClass();
                    netAttr2 = (INetworkAttribute2)evalNetAttr;
                    netAttr2.Name = "TruckTravelTime Speed Limit (km/h)";
                    netAttr2.UsageType = esriNetworkAttributeUsageType.esriNAUTDescriptor;
                    netAttr2.DataType = esriNetworkAttributeDataType.esriNADTDouble;
                    netAttr2.Units = esriNetworkAttributeUnits.esriNAUUnknown;
                    netAttr2.UseByDefault = false;

                    // Create evaluator objects and set them on the EvaluatedNetworkAttribute object.
                    netFieldEval = new NetworkFieldEvaluatorClass();
                    netFieldEval.SetExpression("[FT_TruckKPH]", "");
                    evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAlongDigitized, (INetworkEvaluator)netFieldEval);

                    netFieldEval = new NetworkFieldEvaluatorClass();
                    netFieldEval.SetExpression("[TF_TruckKPH]", "");
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
                else
                {
                    //
                    // TruckTravelTime network attribute (dummy that is the same as the TruckMinutes network attribute)
                    //

                    // Create an EvaluatedNetworkAttribute object and populate its settings.
                    evalNetAttr = new EvaluatedNetworkAttributeClass();
                    netAttr2 = (INetworkAttribute2)evalNetAttr;
                    netAttr2.Name = "TruckTravelTime";
                    netAttr2.UsageType = esriNetworkAttributeUsageType.esriNAUTCost;
                    netAttr2.DataType = esriNetworkAttributeDataType.esriNADTDouble;
                    netAttr2.Units = esriNetworkAttributeUnits.esriNAUMinutes;
                    netAttr2.UseByDefault = false;

                    // Create evaluator objects and set them on the EvaluatedNetworkAttribute object.
                    if (usesHistoricalTraffic)
                    {
                        netFieldEval = new NetworkFieldEvaluatorClass();
                        netFieldEval.SetExpression("ttt", "ttt = [FT_Minutes]\ntruckSpeed = [FT_TruckKPH]\nIf Not IsNull(truckSpeed) Then ttt = [Meters] * 0.06 / truckSpeed\nIf ttt < [FT_Minutes] Then ttt = [FT_Minutes]");
                        evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAlongDigitized, (INetworkEvaluator)netFieldEval);

                        netFieldEval = new NetworkFieldEvaluatorClass();
                        netFieldEval.SetExpression("ttt", "ttt = [TF_Minutes]\ntruckSpeed = [TF_TruckKPH]\nIf Not IsNull(truckSpeed) Then ttt = [Meters] * 0.06 / truckSpeed\nIf ttt < [TF_Minutes] Then ttt = [TF_Minutes]");
                        evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAgainstDigitized, (INetworkEvaluator)netFieldEval);
                    }
                    else
                    {
                        netFieldEval = new NetworkFieldEvaluatorClass();
                        netFieldEval.SetExpression("[Meters] * 0.06 / truckSpeed", "truckSpeed = [FT_TruckKPH]\nIf IsNull(truckSpeed) Then truckSpeed = [KPH]\nIf truckSpeed > [KPH] Then truckSpeed = [KPH]");
                        evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAlongDigitized, (INetworkEvaluator)netFieldEval);

                        netFieldEval = new NetworkFieldEvaluatorClass();
                        netFieldEval.SetExpression("[Meters] * 0.06 / truckSpeed", "truckSpeed = [TF_TruckKPH]\nIf IsNull(truckSpeed) Then truckSpeed = [KPH]\nIf truckSpeed > [KPH] Then truckSpeed = [KPH]");
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

                    // Add the attribute to the array.
                    attributeArray.Add(evalNetAttr);
                }
            }
            else if (createArcGISOnlineNetworkAttributes)
            {
                //
                // TruckTravelTime network attribute (dummy that is the same as the Minutes network attribute)
                //

                // Create an EvaluatedNetworkAttribute object and populate its settings.
                evalNetAttr = new EvaluatedNetworkAttributeClass();
                netAttr2 = (INetworkAttribute2)evalNetAttr;
                netAttr2.Name = "TruckTravelTime";
                netAttr2.UsageType = esriNetworkAttributeUsageType.esriNAUTCost;
                netAttr2.DataType = esriNetworkAttributeDataType.esriNADTDouble;
                netAttr2.Units = esriNetworkAttributeUnits.esriNAUMinutes;
                netAttr2.UseByDefault = false;

                if (usesHistoricalTraffic)
                {
                    // Create evaluator objects and set them on the EvaluatedNetworkAttribute object.
                    netFieldEval = new NetworkFieldEvaluatorClass();
                    netFieldEval.SetExpression("[FT_Minutes]", "");
                    evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAlongDigitized, (INetworkEvaluator)netFieldEval);

                    netFieldEval = new NetworkFieldEvaluatorClass();
                    netFieldEval.SetExpression("[TF_Minutes]", "");
                    evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAgainstDigitized, (INetworkEvaluator)netFieldEval);
                }
                else
                {
                    // Create evaluator objects and set them on the EvaluatedNetworkAttribute object.
                    netFieldEval = new NetworkFieldEvaluatorClass();
                    netFieldEval.SetExpression("[Minutes]", "");
                    evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAlongDigitized, (INetworkEvaluator)netFieldEval);

                    netFieldEval = new NetworkFieldEvaluatorClass();
                    netFieldEval.SetExpression("[Minutes]", "");
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
                INetworkTravelMode2 travelMode;
                string timeAttributeName;
                string distanceAttributeName;
                IStringArray restrictionsArray;
                IArray paramValuesArray;
                INetworkTravelModeParameterValue tmParamValue;
                string preferredTruckRoutesAttributeName = createArcGISOnlineNetworkAttributes ? 
                                                           "Use Preferred Truck Routes" : "National STAA and State Truck Designated and Locally Preferred Routes";

                //
                // Driving Time travel mode
                //

                // Create a NetworkTravelMode object and populate its settings.
                travelMode = new NetworkTravelModeClass();
                travelMode.Name = "Driving Time";
                timeAttributeName = ((usesHistoricalTraffic || createArcGISOnlineNetworkAttributes) ? "TravelTime" : "Minutes");
                distanceAttributeName = (createNetworkAttributesInMetric ? "Kilometers" : "Miles");
                travelMode.ImpedanceAttributeName = timeAttributeName;
                travelMode.TimeAttributeName = timeAttributeName;
                travelMode.DistanceAttributeName = distanceAttributeName;
                travelMode.UseHierarchy = true;
                travelMode.RestrictUTurns = esriNetworkForwardStarBacktrack.esriNFSBAtDeadEndsAndIntersections;
                travelMode.OutputGeometryPrecision = 10;
                travelMode.OutputGeometryPrecisionUnits = esriUnits.esriMeters;
                travelMode.Type = "AUTOMOBILE";
                travelMode.Description = "Models the movement of cars and other similar small automobiles, such as pickup trucks, and finds solutions that optimize travel time. " +
                                         "Travel obeys one-way roads, avoids illegal turns, and follows other rules that are specific to cars. " +
                                         "Dynamic travel speeds based on traffic are used where it is available when you specify a start time.";

                // Populate the restriction attributes to use.
                restrictionsArray = new StrArrayClass();
                restrictionsArray.Add("Driving an Automobile");
                restrictionsArray.Add("Through Traffic Prohibited");
                restrictionsArray.Add("Avoid Unpaved Roads");
                restrictionsArray.Add("Avoid Express Lanes");
                restrictionsArray.Add("Avoid Carpool Roads");
                restrictionsArray.Add("Avoid Private Roads");
                restrictionsArray.Add("Roads Under Construction Prohibited");
                restrictionsArray.Add("Avoid Gates");
                if (!createArcGISOnlineNetworkAttributes)
                    restrictionsArray.Add("Oneway");    // For ArcGIS Online, the Oneway information is rolled into the Driving restrictions.

                travelMode.RestrictionAttributeNames = restrictionsArray;

                // Add the travel mode to the array.
                travelModeArray.Add(travelMode);

                //
                // Driving Distance travel mode
                //

                // Create a NetworkTravelMode object and populate its settings.
                travelMode = new NetworkTravelModeClass();
                travelMode.Name = "Driving Distance";
                timeAttributeName = ((usesHistoricalTraffic || createArcGISOnlineNetworkAttributes) ? "TravelTime" : "Minutes");
                distanceAttributeName = (createNetworkAttributesInMetric ? "Kilometers" : "Miles");
                travelMode.ImpedanceAttributeName = distanceAttributeName;
                travelMode.TimeAttributeName = timeAttributeName;
                travelMode.DistanceAttributeName = distanceAttributeName;
                travelMode.UseHierarchy = true;
                travelMode.RestrictUTurns = esriNetworkForwardStarBacktrack.esriNFSBAtDeadEndsAndIntersections;
                travelMode.OutputGeometryPrecision = 10;
                travelMode.OutputGeometryPrecisionUnits = esriUnits.esriMeters;
                travelMode.Type = "AUTOMOBILE";
                travelMode.Description = "Models the movement of cars and other similar small automobiles, such as pickup trucks, and finds solutions that optimize travel distance. " +
                                         "Travel obeys one-way roads, avoids illegal turns, and follows other rules that are specific to cars.";

                // Populate the restriction attributes to use.
                restrictionsArray = new StrArrayClass();
                restrictionsArray.Add("Driving an Automobile");
                restrictionsArray.Add("Through Traffic Prohibited");
                restrictionsArray.Add("Avoid Unpaved Roads");
                restrictionsArray.Add("Avoid Express Lanes");
                restrictionsArray.Add("Avoid Carpool Roads");
                restrictionsArray.Add("Avoid Private Roads");
                restrictionsArray.Add("Roads Under Construction Prohibited");
                restrictionsArray.Add("Avoid Gates");
                if (!createArcGISOnlineNetworkAttributes)
                    restrictionsArray.Add("Oneway");    // For ArcGIS Online, the Oneway information is rolled into the Driving restrictions.

                travelMode.RestrictionAttributeNames = restrictionsArray;

                // Add the travel mode to the array.
                travelModeArray.Add(travelMode);

                //
                // Rural Driving Time travel mode
                //

                // Create a NetworkTravelMode object and populate its settings.
                travelMode = new NetworkTravelModeClass();
                travelMode.Name = "Rural Driving Time";
                timeAttributeName = ((usesHistoricalTraffic || createArcGISOnlineNetworkAttributes) ? "TravelTime" : "Minutes");
                distanceAttributeName = (createNetworkAttributesInMetric ? "Kilometers" : "Miles");
                travelMode.ImpedanceAttributeName = timeAttributeName;
                travelMode.TimeAttributeName = timeAttributeName;
                travelMode.DistanceAttributeName = distanceAttributeName;
                travelMode.UseHierarchy = true;
                travelMode.RestrictUTurns = esriNetworkForwardStarBacktrack.esriNFSBAtDeadEndsAndIntersections;
                travelMode.OutputGeometryPrecision = 10;
                travelMode.OutputGeometryPrecisionUnits = esriUnits.esriMeters;
                travelMode.Type = "AUTOMOBILE";
                travelMode.Description = "Models the movement of cars and other similar small automobiles, such as pickup trucks, and finds solutions that optimize travel time. " +
                                         "Travel obeys one-way roads, avoids illegal turns, and follows other rules that are specific to cars, but does not discourage travel on unpaved roads. " +
                                         "Dynamic travel speeds based on traffic are used where it is available when you specify a start time.";

                // Populate the restriction attributes to use.
                restrictionsArray = new StrArrayClass();
                restrictionsArray.Add("Driving an Automobile");
                restrictionsArray.Add("Through Traffic Prohibited");
                restrictionsArray.Add("Avoid Express Lanes");
                restrictionsArray.Add("Avoid Carpool Roads");
                restrictionsArray.Add("Avoid Private Roads");
                restrictionsArray.Add("Roads Under Construction Prohibited");
                restrictionsArray.Add("Avoid Gates");
                if (!createArcGISOnlineNetworkAttributes)
                    restrictionsArray.Add("Oneway");    // For ArcGIS Online, the Oneway information is rolled into the Driving restrictions.

                travelMode.RestrictionAttributeNames = restrictionsArray;

                // Add the travel mode to the array.
                travelModeArray.Add(travelMode);

                //
                // Rural Driving Distance travel mode
                //

                // Create a NetworkTravelMode object and populate its settings.
                travelMode = new NetworkTravelModeClass();
                travelMode.Name = "Rural Driving Distance";
                timeAttributeName = ((usesHistoricalTraffic || createArcGISOnlineNetworkAttributes) ? "TravelTime" : "Minutes");
                distanceAttributeName = (createNetworkAttributesInMetric ? "Kilometers" : "Miles");
                travelMode.ImpedanceAttributeName = distanceAttributeName;
                travelMode.TimeAttributeName = timeAttributeName;
                travelMode.DistanceAttributeName = distanceAttributeName;
                travelMode.UseHierarchy = true;
                travelMode.RestrictUTurns = esriNetworkForwardStarBacktrack.esriNFSBAtDeadEndsAndIntersections;
                travelMode.OutputGeometryPrecision = 10;
                travelMode.OutputGeometryPrecisionUnits = esriUnits.esriMeters;
                travelMode.Type = "AUTOMOBILE";
                travelMode.Description = "Models the movement of cars and other similar small automobiles, such as pickup trucks, and finds solutions that optimize travel distance. " +
                                         "Travel obeys one-way roads, avoids illegal turns, and follows other rules that are specific to cars, but does not discourage travel on unpaved roads.";

                // Populate the restriction attributes to use.
                restrictionsArray = new StrArrayClass();
                restrictionsArray.Add("Driving an Automobile");
                restrictionsArray.Add("Through Traffic Prohibited");
                restrictionsArray.Add("Avoid Express Lanes");
                restrictionsArray.Add("Avoid Carpool Roads");
                restrictionsArray.Add("Avoid Private Roads");
                restrictionsArray.Add("Roads Under Construction Prohibited");
                restrictionsArray.Add("Avoid Gates");
                if (!createArcGISOnlineNetworkAttributes)
                    restrictionsArray.Add("Oneway");    // For ArcGIS Online, the Oneway information is rolled into the Driving restrictions.

                travelMode.RestrictionAttributeNames = restrictionsArray;

                // Add the travel mode to the array.
                travelModeArray.Add(travelMode);

                //
                // Trucking Time travel mode
                //

                // Create a NetworkTravelMode object and populate its settings.
                travelMode = new NetworkTravelModeClass();
                travelMode.Name = "Trucking Time";
                timeAttributeName = ((usesTransport || createArcGISOnlineNetworkAttributes) ? "TruckTravelTime" : "Minutes");
                distanceAttributeName = (createNetworkAttributesInMetric ? "Kilometers" : "Miles");
                travelMode.ImpedanceAttributeName = timeAttributeName;
                travelMode.TimeAttributeName = timeAttributeName;
                travelMode.DistanceAttributeName = distanceAttributeName;
                travelMode.UseHierarchy = true;
                travelMode.RestrictUTurns = esriNetworkForwardStarBacktrack.esriNFSBNoBacktrack;
                travelMode.OutputGeometryPrecision = 10;
                travelMode.OutputGeometryPrecisionUnits = esriUnits.esriMeters;
                travelMode.Type = "TRUCK";
                travelMode.Description = "Models basic truck travel by preferring designated truck routes, and finds solutions that optimize travel time. " +
                                         "Routes must obey one-way roads, avoid illegal turns, and so on.";

                // Populate attribute parameter values to use.
                paramValuesArray = new ArrayClass();
                tmParamValue = new NetworkTravelModeParameterValueClass();
                tmParamValue.AttributeName = "Driving a Truck";
                tmParamValue.ParameterName = "Restriction Usage";
                tmParamValue.Value = AvoidHighFactor;
                paramValuesArray.Add(tmParamValue);
                
                // Populate the restriction attributes to use.
                restrictionsArray = new StrArrayClass();
                restrictionsArray.Add("Driving a Truck");
                restrictionsArray.Add("Avoid Truck Restricted Roads");
                restrictionsArray.Add("Avoid Unpaved Roads");
                restrictionsArray.Add("Avoid Express Lanes");
                restrictionsArray.Add("Avoid Carpool Roads");
                restrictionsArray.Add("Avoid Private Roads");
                restrictionsArray.Add("Roads Under Construction Prohibited");
                restrictionsArray.Add("Avoid Gates");
                if (!createArcGISOnlineNetworkAttributes)
                    restrictionsArray.Add("Oneway");    // For ArcGIS Online, the Oneway information is rolled into the Driving restrictions.
                if (usesTransport)
                {
                    restrictionsArray.Add(preferredTruckRoutesAttributeName);
                    tmParamValue = new NetworkTravelModeParameterValueClass();
                    tmParamValue.AttributeName = preferredTruckRoutesAttributeName;
                    tmParamValue.ParameterName = "Restriction Usage";
                    tmParamValue.Value = PreferHighFactor;
                    paramValuesArray.Add(tmParamValue);
                }

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
                timeAttributeName = ((usesTransport || createArcGISOnlineNetworkAttributes) ? "TruckTravelTime" : "Minutes");
                distanceAttributeName = (createNetworkAttributesInMetric ? "Kilometers" : "Miles");
                travelMode.ImpedanceAttributeName = distanceAttributeName;
                travelMode.TimeAttributeName = timeAttributeName;
                travelMode.DistanceAttributeName = distanceAttributeName;
                travelMode.UseHierarchy = true;
                travelMode.RestrictUTurns = esriNetworkForwardStarBacktrack.esriNFSBNoBacktrack;
                travelMode.OutputGeometryPrecision = 10;
                travelMode.OutputGeometryPrecisionUnits = esriUnits.esriMeters;
                travelMode.Type = "TRUCK";
                travelMode.Description = "Models basic truck travel by preferring designated truck routes, and finds solutions that optimize travel distance. " +
                                         "Routes must obey one-way roads, avoid illegal turns, and so on.";

                // Populate attribute parameter values to use.
                paramValuesArray = new ArrayClass();
                tmParamValue = new NetworkTravelModeParameterValueClass();
                tmParamValue.AttributeName = "Driving a Truck";
                tmParamValue.ParameterName = "Restriction Usage";
                tmParamValue.Value = AvoidHighFactor;
                paramValuesArray.Add(tmParamValue);

                // Populate the restriction attributes to use.
                restrictionsArray = new StrArrayClass();
                restrictionsArray.Add("Driving a Truck");
                restrictionsArray.Add("Avoid Truck Restricted Roads");
                restrictionsArray.Add("Avoid Unpaved Roads");
                restrictionsArray.Add("Avoid Express Lanes");
                restrictionsArray.Add("Avoid Carpool Roads");
                restrictionsArray.Add("Avoid Private Roads");
                restrictionsArray.Add("Roads Under Construction Prohibited");
                restrictionsArray.Add("Avoid Gates");
                if (!createArcGISOnlineNetworkAttributes)
                    restrictionsArray.Add("Oneway");    // For ArcGIS Online, the Oneway information is rolled into the Driving restrictions.
                if (usesTransport)
                {
                    restrictionsArray.Add(preferredTruckRoutesAttributeName);
                    tmParamValue = new NetworkTravelModeParameterValueClass();
                    tmParamValue.AttributeName = preferredTruckRoutesAttributeName;
                    tmParamValue.ParameterName = "Restriction Usage";
                    tmParamValue.Value = PreferHighFactor;
                    paramValuesArray.Add(tmParamValue);
                }

                travelMode.RestrictionAttributeNames = restrictionsArray;
                travelMode.AttributeParameterValues = paramValuesArray;

                // Add the travel mode to the array.
                travelModeArray.Add(travelMode);

                //
                // Walking Time travel mode
                //

                // Create a NetworkTravelMode object and populate its settings.
                travelMode = new NetworkTravelModeClass();
                travelMode.Name = "Walking Time";
                timeAttributeName = "WalkTime";
                distanceAttributeName = (createNetworkAttributesInMetric ? "Kilometers" : "Miles");
                travelMode.ImpedanceAttributeName = timeAttributeName;
                travelMode.TimeAttributeName = timeAttributeName;
                travelMode.DistanceAttributeName = distanceAttributeName;
                travelMode.UseHierarchy = false;
                travelMode.RestrictUTurns = esriNetworkForwardStarBacktrack.esriNFSBAllowBacktrack;
                travelMode.OutputGeometryPrecision = 2;
                travelMode.OutputGeometryPrecisionUnits = esriUnits.esriMeters;
                travelMode.Type = "WALK";

                string walkDescription = "Follows paths and roads that allow pedestrian traffic and finds solutions that optimize travel time. ";
                if (createArcGISOnlineNetworkAttributes)
                    walkDescription += "The walking speed is set to 5 kilometers per hour.";
                else
                    walkDescription += "By default, the walking speed is set to 5 kilometers per hour.";
                travelMode.Description = walkDescription;

                // Populate the restriction attributes to use.
                restrictionsArray = new StrArrayClass();
                restrictionsArray.Add("Walking");
                restrictionsArray.Add("Avoid Private Roads");
                travelMode.RestrictionAttributeNames = restrictionsArray;

                // Add the travel mode to the array.
                travelModeArray.Add(travelMode);

                //
                // Walking Distance travel mode
                //

                // Create a NetworkTravelMode object and populate its settings.
                travelMode = new NetworkTravelModeClass();
                travelMode.Name = "Walking Distance";
                timeAttributeName = "WalkTime";
                distanceAttributeName = (createNetworkAttributesInMetric ? "Kilometers" : "Miles");
                travelMode.ImpedanceAttributeName = distanceAttributeName;
                travelMode.TimeAttributeName = timeAttributeName;
                travelMode.DistanceAttributeName = distanceAttributeName;
                travelMode.UseHierarchy = false;
                travelMode.RestrictUTurns = esriNetworkForwardStarBacktrack.esriNFSBAllowBacktrack;
                travelMode.OutputGeometryPrecision = 2;
                travelMode.OutputGeometryPrecisionUnits = esriUnits.esriMeters;
                travelMode.Type = "WALK";
                travelMode.Description = "Follows paths and roads that allow pedestrian traffic and finds solutions that optimize travel distance.";

                // Populate the restriction attributes to use.
                restrictionsArray = new StrArrayClass();
                restrictionsArray.Add("Walking");
                restrictionsArray.Add("Avoid Private Roads");
                travelMode.RestrictionAttributeNames = restrictionsArray;

                // Add the travel mode to the array.
                travelModeArray.Add(travelMode);

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

        private IEvaluatedNetworkAttribute CreateVehicleNetworkAttribute(string attrName, bool useByDefault, double restrUsageFactor, bool isDirectional,
                                                                         string edgeFieldName, string turnFieldName, double fgdbVersion,
                                                                         INetworkSource edgeNetworkSource, INetworkSource turnNetworkSource,
                                                                         bool usesTransport)
        {
            // Create an EvaluatedNetworkAttribute object and populate its settings.
            IEvaluatedNetworkAttribute evalNetAttr = CreateRestrAttrNoEvals(attrName, fgdbVersion, restrUsageFactor, useByDefault, "", "");

            // Create evaluator objects and set them on the EvaluatedNetworkAttribute object.
            INetworkFieldEvaluator netFieldEval = new NetworkFieldEvaluatorClass();
            netFieldEval.SetExpression((isDirectional ? "[FT_" : "[") + edgeFieldName + "] = \"N\"", "");
            evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAlongDigitized, (INetworkEvaluator)netFieldEval);

            netFieldEval = new NetworkFieldEvaluatorClass();
            netFieldEval.SetExpression((isDirectional ? "[TF_" : "[") + edgeFieldName + "] = \"N\"", "");
            evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAgainstDigitized, (INetworkEvaluator)netFieldEval);

            if (turnFieldName != "")
            {
                netFieldEval = new NetworkFieldEvaluatorClass();
                netFieldEval.SetExpression("[" + turnFieldName + "] = \"Y\" And " + (usesTransport ? "( [COND_TYPE] = 7 Or ( [COND_TYPE] = 26 And [AllTransportProhibited] = \"Y\" ) )" : "[COND_TYPE] = 7"), "");
                evalNetAttr.set_Evaluator(turnNetworkSource, esriNetworkEdgeDirection.esriNEDNone, (INetworkEvaluator)netFieldEval);
            }

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

        private IEvaluatedNetworkAttribute CreateDrivingATruckNetworkAttribute(bool isDirectional, double fgdbVersion,
                                                                               INetworkSource edgeNetworkSource, INetworkSource turnNetworkSource,
                                                                               bool usesTransport)
        {
            // Create an EvaluatedNetworkAttribute object and populate its settings.
            IEvaluatedNetworkAttribute evalNetAttr = CreateRestrAttrNoEvals("Driving a Truck", fgdbVersion, -1.0, false, "", "");

            // Create evaluator objects and set them on the EvaluatedNetworkAttribute object.
            INetworkFieldEvaluator netFieldEval = new NetworkFieldEvaluatorClass();
            netFieldEval.SetExpression((isDirectional ? "[FT_" : "[") + "AR_DELIV] = \"N\"", "");
            evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAlongDigitized, (INetworkEvaluator)netFieldEval);

            netFieldEval = new NetworkFieldEvaluatorClass();
            netFieldEval.SetExpression((isDirectional ? "[TF_" : "[") + "AR_DELIV] = \"N\"", "");
            evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAgainstDigitized, (INetworkEvaluator)netFieldEval);

            netFieldEval = new NetworkFieldEvaluatorClass();
            netFieldEval.SetExpression("( [AR_DELIVER] = \"Y\" Or [AR_TRUCKS] = \"Y\" ) And " + (usesTransport ? "( [COND_TYPE] = 7 Or ( [COND_TYPE] = 26 And [AllTransportProhibited] = \"Y\" ) )" : "[COND_TYPE] = 7"), "");
            evalNetAttr.set_Evaluator(turnNetworkSource, esriNetworkEdgeDirection.esriNEDNone, (INetworkEvaluator)netFieldEval);

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

        private IEvaluatedNetworkAttribute CreateAvoidNetworkAttribute(string attrName, string fieldEvalExpression, bool useByDefault,
                                                                       double fgdbVersion, INetworkSource edgeNetworkSource)
        {
            // CreateAvoidNetworkAttribute without a specified avoidFactor creates the attribute with an AvoidMediumFactor.
            return CreateAvoidNetworkAttribute(attrName, fieldEvalExpression, useByDefault, fgdbVersion, edgeNetworkSource, AvoidMediumFactor);
        }
        
        private IEvaluatedNetworkAttribute CreateAvoidNetworkAttribute(string attrName, string fieldEvalExpression, bool useByDefault,
                                                                       double fgdbVersion, INetworkSource edgeNetworkSource, double avoidFactor)
        {
            // Create an EvaluatedNetworkAttribute object and populate its settings.
            IEvaluatedNetworkAttribute evalNetAttr = CreateRestrAttrNoEvals(attrName, fgdbVersion, avoidFactor, useByDefault, "", "");

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

        private IEvaluatedNetworkAttribute CreateMultiFieldLoadRestrictionAttribute(string attrName, bool isPreferred, string[] arrayOfFieldNameBase, double fgdbVersion,
                                                                                    INetworkSource edgeNetworkSource, INetworkSource turnNetworkSource)
        {
            int arrayLength = arrayOfFieldNameBase.Length;
            if (arrayLength <= 0)
                return null;

            string ftExpression = "[FT_" + arrayOfFieldNameBase[0] + "] = \"Y\"";
            string tfExpression = "[TF_" + arrayOfFieldNameBase[0] + "] = \"Y\"";
            string turnExpression = "[" + arrayOfFieldNameBase[0] + "] = \"Y\"";

            for (int i = 1; i < arrayLength; i++)
            {
                ftExpression += (" Or [FT_" + arrayOfFieldNameBase[i] + "] = \"Y\"");
                tfExpression += (" Or [TF_" + arrayOfFieldNameBase[i] + "] = \"Y\"");
                turnExpression += (" Or [" + arrayOfFieldNameBase[i] + "] = \"Y\"");
            }

            // Create an EvaluatedNetworkAttribute object and populate its settings.
            IEvaluatedNetworkAttribute evalNetAttr = CreateRestrAttrNoEvals(attrName, fgdbVersion, (isPreferred ? PreferMediumFactor : -1.0), false, "", "");

            // Create evaluator objects and set them on the EvaluatedNetworkAttribute object.
            INetworkFieldEvaluator netFieldEval = new NetworkFieldEvaluatorClass();
            netFieldEval.SetExpression(ftExpression, "");
            evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAlongDigitized, (INetworkEvaluator)netFieldEval);

            netFieldEval = new NetworkFieldEvaluatorClass();
            netFieldEval.SetExpression(tfExpression, "");
            evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAgainstDigitized, (INetworkEvaluator)netFieldEval);

            if (turnNetworkSource != null)
            {
                netFieldEval = new NetworkFieldEvaluatorClass();
                netFieldEval.SetExpression(turnExpression, "");
                evalNetAttr.set_Evaluator(turnNetworkSource, esriNetworkEdgeDirection.esriNEDNone, (INetworkEvaluator)netFieldEval);
            }

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

        private IEvaluatedNetworkAttribute CreateLoadRestrictionAttribute(string attrName, bool isPreferred, string fieldNameBase, double fgdbVersion,
                                                                          INetworkSource edgeNetworkSource, INetworkSource turnNetworkSource)
        {
            // Create an EvaluatedNetworkAttribute object and populate its settings.
            IEvaluatedNetworkAttribute evalNetAttr = CreateRestrAttrNoEvals(attrName, fgdbVersion, (isPreferred ? PreferMediumFactor : -1.0), false, "", "");

            // Create evaluator objects and set them on the EvaluatedNetworkAttribute object.
            INetworkFieldEvaluator netFieldEval = new NetworkFieldEvaluatorClass();
            netFieldEval.SetExpression("[FT_" + fieldNameBase + "] = \"Y\"", "");
            evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAlongDigitized, (INetworkEvaluator)netFieldEval);

            netFieldEval = new NetworkFieldEvaluatorClass();
            netFieldEval.SetExpression("[TF_" + fieldNameBase + "] = \"Y\"", "");
            evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAgainstDigitized, (INetworkEvaluator)netFieldEval);

            if (turnNetworkSource != null)
            {
                netFieldEval = new NetworkFieldEvaluatorClass();
                netFieldEval.SetExpression("[" + fieldNameBase + "] = \"Y\"", "");
                evalNetAttr.set_Evaluator(turnNetworkSource, esriNetworkEdgeDirection.esriNEDNone, (INetworkEvaluator)netFieldEval);
            }
            
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

        private IEvaluatedNetworkAttribute CreateDimensionalLimitAttribute(string attrName, string fieldNameBase, bool createNetworkAttributeInMetric,
                                                                           bool isWeightAttr, INetworkSource edgeNetworkSource, INetworkSource turnNetworkSource)
        {
            // Create an EvaluatedNetworkAttribute object and populate its settings.
            IEvaluatedNetworkAttribute evalNetAttr = new EvaluatedNetworkAttributeClass();
            INetworkAttribute2 netAttr2 = (INetworkAttribute2)evalNetAttr;
            netAttr2.Name = attrName;
            netAttr2.UsageType = esriNetworkAttributeUsageType.esriNAUTDescriptor;
            netAttr2.DataType = esriNetworkAttributeDataType.esriNADTDouble;
            netAttr2.Units = esriNetworkAttributeUnits.esriNAUUnknown;
            netAttr2.UseByDefault = false;

            // Weight values are in kilograms and, if required, need to be converted to pounds
            // Distance values are in meters and, if required, need to be converted to feet
            string conversionExpr = "";
            if (!createNetworkAttributeInMetric)
                conversionExpr = isWeightAttr ? " / 0.45359237" : " / 0.3048";

            // Create evaluator objects and set them on the EvaluatedNetworkAttribute object.
            INetworkFieldEvaluator netFieldEval = new NetworkFieldEvaluatorClass();
            netFieldEval.SetExpression("[FT_" + fieldNameBase + "]" + conversionExpr, "");
            evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAlongDigitized, (INetworkEvaluator)netFieldEval);

            netFieldEval = new NetworkFieldEvaluatorClass();
            netFieldEval.SetExpression("[TF_" + fieldNameBase + "]" + conversionExpr, "");
            evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAgainstDigitized, (INetworkEvaluator)netFieldEval);

            if (turnNetworkSource != null)
            {
                netFieldEval = new NetworkFieldEvaluatorClass();
                netFieldEval.SetExpression("[" + fieldNameBase + "]" + conversionExpr, "");
                evalNetAttr.set_Evaluator(turnNetworkSource, esriNetworkEdgeDirection.esriNEDNone, (INetworkEvaluator)netFieldEval);
            }

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

        private IEvaluatedNetworkAttribute CreateDimensionalRestrictionAttribute(string restrAttrName, string limitAttrName,
                                                                                 string paramName, double fgdbVersion,
                                                                                 INetworkSource edgeNetworkSource, INetworkSource turnNetworkSource)
        {
            // Create an EvaluatedNetworkAttribute object and populate its settings.
            IEvaluatedNetworkAttribute evalNetAttr = CreateRestrAttrNoEvals(restrAttrName, fgdbVersion, -1.0, false, paramName, "");

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

            netFuncEval = new NetworkFunctionEvaluatorClass();
            netFuncEval.FirstArgument = limitAttrName;
            netFuncEval.Operator = "<";
            netFuncEval.SecondArgument = paramName;
            evalNetAttr.set_Evaluator(turnNetworkSource, esriNetworkEdgeDirection.esriNEDNone, (INetworkEvaluator)netFuncEval);

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

        private IEvaluatedNetworkAttribute CreateQuantityLimitAttribute(string attrName, string fieldNameBase, 
                                                                        INetworkSource edgeNetworkSource, INetworkSource turnNetworkSource)
        {
            // Create an EvaluatedNetworkAttribute object and populate its settings.
            IEvaluatedNetworkAttribute evalNetAttr = new EvaluatedNetworkAttributeClass();
            INetworkAttribute2 netAttr2 = (INetworkAttribute2)evalNetAttr;
            netAttr2.Name = attrName;
            netAttr2.UsageType = esriNetworkAttributeUsageType.esriNAUTDescriptor;
            netAttr2.DataType = esriNetworkAttributeDataType.esriNADTInteger;
            netAttr2.Units = esriNetworkAttributeUnits.esriNAUUnknown;
            netAttr2.UseByDefault = false;

            // Create evaluator objects and set them on the EvaluatedNetworkAttribute object.
            INetworkFieldEvaluator netFieldEval = new NetworkFieldEvaluatorClass();
            netFieldEval.SetExpression("[FT_" + fieldNameBase + "]", "");
            evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAlongDigitized, (INetworkEvaluator)netFieldEval);

            netFieldEval = new NetworkFieldEvaluatorClass();
            netFieldEval.SetExpression("[TF_" + fieldNameBase + "]", "");
            evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAgainstDigitized, (INetworkEvaluator)netFieldEval);

            if (turnNetworkSource != null)
            {
                netFieldEval = new NetworkFieldEvaluatorClass();
                netFieldEval.SetExpression("[" + fieldNameBase + "]", "");
                evalNetAttr.set_Evaluator(turnNetworkSource, esriNetworkEdgeDirection.esriNEDNone, (INetworkEvaluator)netFieldEval);
            }

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

        private IEvaluatedNetworkAttribute CreateQuantityRestrictionAttribute(string restrAttrName, string limitAttrName,
                                                                              string paramName, double fgdbVersion,
                                                                              INetworkSource edgeNetworkSource, INetworkSource turnNetworkSource)
        {
            // Create an EvaluatedNetworkAttribute object and populate its settings.
            IEvaluatedNetworkAttribute evalNetAttr = CreateRestrAttrNoEvals(restrAttrName, fgdbVersion, -1.0, false, "", paramName);

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

            netFuncEval = new NetworkFunctionEvaluatorClass();
            netFuncEval.FirstArgument = limitAttrName;
            netFuncEval.Operator = "<";
            netFuncEval.SecondArgument = paramName;
            evalNetAttr.set_Evaluator(turnNetworkSource, esriNetworkEdgeDirection.esriNEDNone, (INetworkEvaluator)netFuncEval);

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
                                                                  bool useByDefault, string dimensionalParamName, string quantityParamName)
        {
            // Create an EvaluatedNetworkAttribute object and populate its settings.
            IEvaluatedNetworkAttribute evalNetAttr = new EvaluatedNetworkAttributeClass();
            INetworkAttribute2 netAttr2 = (INetworkAttribute2)evalNetAttr;
            netAttr2.Name = attrName;
            netAttr2.UsageType = esriNetworkAttributeUsageType.esriNAUTRestriction;
            netAttr2.DataType = esriNetworkAttributeDataType.esriNADTBoolean;
            netAttr2.Units = esriNetworkAttributeUnits.esriNAUUnknown;
            netAttr2.UseByDefault = useByDefault;

            if (fgdbVersion >= 10.1 || dimensionalParamName != "" || quantityParamName != "")
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

                // Create a parameter to hold the vehicle's trailer/axle quantity (if provided)
                if (quantityParamName != "")
                {
                    netAttrParam = new NetworkAttributeParameterClass();
                    netAttrParam.Name = quantityParamName;
                    netAttrParam.VarType = (int)(VarEnum.VT_I2);
                    netAttrParam.DefaultValue = 0;
                    netAttrParam.ParameterUsageType = esriNetworkAttributeParameterUsageType.esriNAPUTGeneral;
                    paramArray.Add(netAttrParam);
                }

                // Add the parameter(s) to the network attribute.
                netAttr2.Parameters = paramArray;
            }

            return evalNetAttr;
        }

        private IEvaluatedNetworkAttribute CreateTrafficAttrWSpeedCapParam(string attrName, INetworkSource edgeNetworkSource,
                                                                           string weekdayFallbackAttrName,
                                                                           string weekendFallbackAttrName,
                                                                           string timeNeutralAttrName, bool useByDefault)
        {
            // Create an EvaluatedNetworkAttribute object and populate its settings.
            IEvaluatedNetworkAttribute evalNetAttr = new EvaluatedNetworkAttributeClass();
            INetworkAttribute2 netAttr2 = (INetworkAttribute2)evalNetAttr;
            netAttr2.Name = attrName;
            netAttr2.UsageType = esriNetworkAttributeUsageType.esriNAUTCost;
            netAttr2.DataType = esriNetworkAttributeDataType.esriNADTDouble;
            netAttr2.Units = esriNetworkAttributeUnits.esriNAUMinutes;
            netAttr2.UseByDefault = useByDefault;

            // Create evaluator objects and set them on the EvaluatedNetworkAttribute object.
            IHistoricalTravelTimeEvaluator histTravelTimeEval = new NetworkEdgeTrafficEvaluatorClass();
            histTravelTimeEval.WeekdayFallbackAttributeName = weekdayFallbackAttrName;
            histTravelTimeEval.WeekendFallbackAttributeName = weekendFallbackAttrName;
            histTravelTimeEval.TimeNeutralAttributeName = timeNeutralAttrName;
            evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAlongDigitized, (INetworkEvaluator)histTravelTimeEval);

            histTravelTimeEval = new NetworkEdgeTrafficEvaluatorClass();
            histTravelTimeEval.WeekdayFallbackAttributeName = weekdayFallbackAttrName;
            histTravelTimeEval.WeekendFallbackAttributeName = weekendFallbackAttrName;
            histTravelTimeEval.TimeNeutralAttributeName = timeNeutralAttrName;
            evalNetAttr.set_Evaluator(edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAgainstDigitized, (INetworkEvaluator)histTravelTimeEval);

            INetworkConstantEvaluator netConstEval = new NetworkConstantEvaluatorClass();
            netConstEval.ConstantValue = 0;
            evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETEdge, (INetworkEvaluator)netConstEval);

            netConstEval = new NetworkConstantEvaluatorClass();
            netConstEval.ConstantValue = 0;
            evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETJunction, (INetworkEvaluator)netConstEval);

            netConstEval = new NetworkConstantEvaluatorClass();
            netConstEval.ConstantValue = 0;
            evalNetAttr.set_DefaultEvaluator(esriNetworkElementType.esriNETTurn, (INetworkEvaluator)netConstEval);

            INetworkAttributeParameter2 netAttrParam = null;
            IArray paramArray = new ArrayClass();
            netAttrParam = new NetworkAttributeParameterClass();
            netAttrParam.Name = "Vehicle Maximum Speed (km/h)";
            netAttrParam.VarType = (int)(VarEnum.VT_R8);
            netAttrParam.DefaultValue = 0;
            netAttrParam.ParameterUsageType = esriNetworkAttributeParameterUsageType.esriNAPUTGeneral;
            paramArray.Add(netAttrParam);

            // Add the parameter to the network attribute.
            netAttr2.Parameters = paramArray;

            return evalNetAttr;
        }
    }
}
