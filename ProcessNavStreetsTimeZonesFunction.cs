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

using ESRI.ArcGIS.Geoprocessor;
using ESRI.ArcGIS.AnalysisTools;
using ESRI.ArcGIS.ConversionTools;
using ESRI.ArcGIS.DataManagementTools;
using ESRI.ArcGIS.NetworkAnalystTools;

namespace GPProcessVendorDataFunctions
{
    /// <summary>
    /// Summary description for ProcessNavStreetsTimeZonesFunction.
    /// </summary>
    ///

    [Guid("8743E1C9-2453-4558-93B9-A11309F66916")]
    [ClassInterface(ClassInterfaceType.None)]
    [ProgId("GPProcessVendorDataFunctions.ProcessNavStreetsTimeZonesFunction")]

    class ProcessNavStreetsTimeZonesFunction : IGPFunction2
    {
        #region Constants
        // names
        private const string DefaultTimeZoneIDBaseFieldName = "TimeZoneID";
        private const string TimeZoneFCName = "TimeZonePolygons";
        private const string StreetsFCName = "Streets";
        private const string TimeZonesTableName = "TimeZones";
        
        // parameter index constants
        private const int InputMtdDSTTable = 0;
        private const int InputMtdCntryRefTable = 1;
        private const int InputMtdAreaTable = 2;
        private const int InputAdminBndyFeatureClasses = 3;
        private const int OutputFileGDB = 4;
        private const int InputStreetsFeatureClass = 5;
        private const int InputTimeZoneIDBaseFieldName = 6;
        private const int OutputTimeZoneFeatureClass = 7;
        private const int OutputTimeZonesTable = 8;
        private const int OutputStreetsFeatureClass = 9;

        // field names and types
        private static readonly string[] MtdDSTFieldNames = new string[]
                                        { "AREA_ID", "TIME_ZONE", "DST_EXIST",
                                          "DST_STDAY", "DST_STWK", "DST_STMNTH", "DST_STTIME",
                                          "DST_ENDAY", "DST_ENWK", "DST_ENMNTH", "DST_ENTIME" };
        private static readonly esriFieldType[] MtdDSTFieldTypes = new esriFieldType[]
                                        { esriFieldType.esriFieldTypeInteger,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeSmallInteger };
        private static readonly string[] MtdCntryRefFieldNames = new string[]
                                        { "GOVT_CODE", "UNTMEASURE", "ADMINLEVEL", "PRECISION",
                                          "HSENBRFORM", "DRIVING_SD", "CUR_TYPE", "CNTRYCODE",
                                          "SPDLIMUNIT", "ISO_CODE", "PREFIX" };
        private static readonly esriFieldType[] MtdCntryRefFieldTypes = new esriFieldType[]
                                        { esriFieldType.esriFieldTypeInteger,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString };
        private static readonly string[] MtdAreaFieldNames = new string[]
                                        { "AREA_ID", "AREACODE_1", "AREACODE_2", "AREACODE_3",
                                          "AREACODE_4", "AREACODE_5", "AREACODE_6", "AREACODE_7",
                                          "ADMIN_LVL", "AREA_NAME", "LANG_CODE", "AREA_NM_TR",
                                          "TRANS_TYPE", "AREA_TYPE", "GOVT_CODE" };
        private static readonly esriFieldType[] MtdAreaFieldTypes = new esriFieldType[]
                                        { esriFieldType.esriFieldTypeInteger,
                                          esriFieldType.esriFieldTypeInteger,
                                          esriFieldType.esriFieldTypeInteger,
                                          esriFieldType.esriFieldTypeInteger,
                                          esriFieldType.esriFieldTypeInteger,
                                          esriFieldType.esriFieldTypeInteger,
                                          esriFieldType.esriFieldTypeInteger,
                                          esriFieldType.esriFieldTypeInteger,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeInteger };
        private static readonly string[] AdminBndyFieldNames = new string[]
                                        { "POLYGON_ID", "AREA_ID", "NM_AREA_ID", "POLYGON_NM", "NM_LANGCD",
                                          "POLY_NM_TR", "TRANS_TYPE", "FEAT_TYPE", "DETAIL_CTY",
                                          "FEAT_COD", "COVERIND", "CLAIMED_BY", "CONTROL_BY" };
        private static readonly esriFieldType[] AdminBndyFieldTypes = new esriFieldType[]
                                        { esriFieldType.esriFieldTypeInteger,
                                          esriFieldType.esriFieldTypeInteger,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeInteger,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString };
        private static readonly string[] StreetsFieldNames = new string[]
                                        { "LINK_ID", "ST_NAME", "ST_LANGCD", "ST_NM_PREF", "ST_TYP_BEF",
                                          "ST_NM_BASE", "ST_NM_SUFF", "ST_TYP_AFT", "FUNC_CLASS", "SPEED_CAT",
                                          "DIR_TRAVEL", "AR_AUTO", "AR_BUS", "AR_TAXIS", "AR_CARPOOL",
                                          "AR_PEDEST", "AR_TRUCKS", "AR_TRAFF", "AR_DELIV", "AR_EMERVEH",
                                          "PAVED", "RAMP", "TOLLWAY", "CONTRACC", "ROUNDABOUT",
                                          "FERRY_TYPE", "DIRONSIGN", "PUB_ACCESS" };
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
                                          esriFieldType.esriFieldTypeString };
        #endregion

        private IArray m_parameters;
        private IGPUtilities m_gpUtils;

        public ProcessNavStreetsTimeZonesFunction()
        {
            // Create the GPUtilities Object
            m_gpUtils = new GPUtilitiesClass();
        }

        #region IGPFunction2 Members

        public IArray ParameterInfo
        {
            // create and return the parameters for this function:
            // 0 - Input Metadata Daylight Saving Time (MtdDST) Table
            // 1 - Input Metadata Country Reference (MtdCntryRef) Table
            // 2 - Input Metadata Administrative Area (MtdArea) Table
            // 3 - Input Administrative Area Boundaries (AdminBndy) Feature Classes
            // 4 - Output File Geodatabase
            // 5 - Input Streets Feature Class (optional)
            // 6 - Time Zone ID Base Field Name (optional)
            // 7 - Output Time Zone Feature Class (derived parameter)
            // 8 - Output Time Zones Table (derived parameter)
            // 9 - Output Streets Feature Class (derived parameter)

            get
            {
                IArray paramArray = new ArrayClass();

                // 0 - input_mtdDST_table

                IGPParameterEdit paramEdit = new GPParameterClass();
                paramEdit.DataType = new DETableTypeClass() as IGPDataType;
                paramEdit.Value = new DETableClass() as IGPValue;
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Input Metadata Daylight Saving Time (MtdDST) Table";
                paramEdit.Enabled = true;
                paramEdit.Name = "input_mtdDST_table";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeRequired;

                paramArray.Add(paramEdit as object);

                // 1 - input_mtdCntryRef_table

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new DETableTypeClass() as IGPDataType;
                paramEdit.Value = new DETableClass() as IGPValue;
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Input Metadata Country Reference (MtdCntryRef) Table";
                paramEdit.Enabled = true;
                paramEdit.Name = "input_mtdCntryRef_table";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeRequired;

                paramArray.Add(paramEdit as object);

                // 2 - input_mtdArea_table

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new DETableTypeClass() as IGPDataType;
                paramEdit.Value = new DETableClass() as IGPValue;
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Input Metadata Administrative Area (MtdArea) Table";
                paramEdit.Enabled = true;
                paramEdit.Name = "input_mtdArea_table";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeRequired;

                paramArray.Add(paramEdit as object);

                // 3 - input_adminBndy_feature_classes

                paramEdit = new GPParameterClass();
                IGPMultiValueType mvType = new GPMultiValueTypeClass();
                mvType.MemberDataType = new DEFeatureClassTypeClass();
                paramEdit.DataType = mvType as IGPDataType;
                IGPMultiValue mvValue = new GPMultiValueClass();
                mvValue.MemberDataType = new DEFeatureClassTypeClass() as IGPDataType;
                paramEdit.Value = mvValue as IGPValue;
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Input Administrative Area Boundaries (AdminBndy) Feature Classes";
                paramEdit.Enabled = true;
                paramEdit.Name = "input_adminBndy_feature_classes";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeRequired;

                IGPFeatureClassDomain polygonFeatureClassDomain = new GPFeatureClassDomainClass();
                polygonFeatureClassDomain.AddType(esriGeometryType.esriGeometryPolygon);
                paramEdit.Domain = polygonFeatureClassDomain as IGPDomain;

                paramArray.Add(paramEdit as object);

                // 4 - output_file_geodatabase

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new DEWorkspaceTypeClass() as IGPDataType;
                paramEdit.Value = new DEWorkspaceClass() as IGPValue;
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionOutput;
                paramEdit.DisplayName = "Output File Geodatabase";
                paramEdit.Enabled = true;
                paramEdit.Name = "output_file_geodatabase";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeRequired;

                IGPWorkspaceDomain fileGDBDomain = new GPWorkspaceDomainClass();
                fileGDBDomain.AddType(esriWorkspaceType.esriLocalDatabaseWorkspace);
                paramEdit.Domain = fileGDBDomain as IGPDomain;

                paramArray.Add(paramEdit as object);

                // 5 - input_streets_feature_class

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new DEFeatureClassTypeClass() as IGPDataType;
                paramEdit.Value = new DEFeatureClass() as IGPValue;
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Input Streets Feature Class";
                paramEdit.Enabled = true;
                paramEdit.Name = "input_streets_feature_class";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeOptional;

                IGPFeatureClassDomain lineFeatureClassDomain = new GPFeatureClassDomainClass();
                lineFeatureClassDomain.AddType(esriGeometryType.esriGeometryLine);
                lineFeatureClassDomain.AddType(esriGeometryType.esriGeometryPolyline);
                paramEdit.Domain = lineFeatureClassDomain as IGPDomain;

                paramArray.Add(paramEdit as object);

                // 6 - time_zone_id_base_field_name

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new GPStringTypeClass() as IGPDataType;
                IGPString gpString = new GPStringClass();
                gpString.Value = "TimeZoneID";
                paramEdit.Value = gpString as IGPValue;
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Time Zone ID Base Field Name";
                paramEdit.Enabled = true;
                paramEdit.Name = "time_zone_id_base_field_name";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeOptional;

                paramArray.Add(paramEdit as object);

                // 7 - output_time_zone_feature_class

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new DEFeatureClassTypeClass() as IGPDataType;
                paramEdit.Value = new DEFeatureClassClass() as IGPValue;
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionOutput;
                paramEdit.DisplayName = "Output Time Zone Feature Class";
                paramEdit.Enabled = true;
                paramEdit.Name = "output_time_zone_feature_class";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeDerived;

                paramArray.Add(paramEdit as object);

                // 8 - output_time_zones_table

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new DETableTypeClass() as IGPDataType;
                paramEdit.Value = new DETableClass() as IGPValue;
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionOutput;
                paramEdit.DisplayName = "Output Time Zones Table";
                paramEdit.Enabled = true;
                paramEdit.Name = "output_time_zones_table";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeDerived;

                paramArray.Add(paramEdit as object);

                // 9 - output_streets_feature_class

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new DEFeatureClassTypeClass() as IGPDataType;
                paramEdit.Value = new DEFeatureClassClass() as IGPValue;
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionOutput;
                paramEdit.DisplayName = "Output Streets Feature Class";
                paramEdit.Enabled = true;
                paramEdit.Name = "output_streets_feature_class";
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

            // Get the output geodatabase parameter

            var gpParam = m_parameters.get_Element(OutputFileGDB) as IGPParameter;
            IGPValue outputFileGDBValue = m_gpUtils.UnpackGPValue(gpParam);

            // Get the output parameters and pack them based on the path to the output geodatabase

            if (!(outputFileGDBValue.IsEmpty()))
            {
                gpParam = paramvalues.get_Element(OutputTimeZoneFeatureClass) as IGPParameter;
                var defcType = new DEFeatureClassTypeClass() as IGPDataType;
                m_gpUtils.PackGPValue(defcType.CreateValue(outputFileGDBValue.GetAsText() + "\\" + TimeZoneFCName), gpParam);

                gpParam = paramvalues.get_Element(OutputTimeZonesTable) as IGPParameter;
                var deTableType = new DETableTypeClass() as IGPDataType;
                m_gpUtils.PackGPValue(deTableType.CreateValue(outputFileGDBValue.GetAsText() + "\\" + TimeZonesTableName), gpParam);

                gpParam = paramvalues.get_Element(OutputStreetsFeatureClass) as IGPParameter;
                defcType = new DEFeatureClassTypeClass() as IGPDataType;
                m_gpUtils.PackGPValue(defcType.CreateValue(outputFileGDBValue.GetAsText() + "\\" + StreetsFCName), gpParam);
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

            // Verify chosen input MtdDST table has the expected fields

            gpParam = paramvalues.get_Element(InputMtdDSTTable) as IGPParameter;
            IGPValue tableValue = m_gpUtils.UnpackGPValue(gpParam);
            if (!tableValue.IsEmpty())
            {
                IDETable inputTable = m_gpUtils.DecodeDETable(tableValue);
                CheckForTableFields(inputTable, MtdDSTFieldNames, MtdDSTFieldTypes, messages.GetMessage(InputMtdDSTTable));
            }

            // Verify chosen input MtdCntryRef table has the expected fields

            gpParam = paramvalues.get_Element(InputMtdCntryRefTable) as IGPParameter;
            tableValue = m_gpUtils.UnpackGPValue(gpParam);
            if (!tableValue.IsEmpty())
            {
                IDETable inputTable = m_gpUtils.DecodeDETable(tableValue);
                CheckForTableFields(inputTable, MtdCntryRefFieldNames, MtdCntryRefFieldTypes, messages.GetMessage(InputMtdCntryRefTable));
            }

            // Verify chosen input MtdArea table has the expected fields

            gpParam = paramvalues.get_Element(InputMtdAreaTable) as IGPParameter;
            tableValue = m_gpUtils.UnpackGPValue(gpParam);
            if (!tableValue.IsEmpty())
            {
                IDETable inputTable = m_gpUtils.DecodeDETable(tableValue);
                CheckForTableFields(inputTable, MtdAreaFieldNames, MtdAreaFieldTypes, messages.GetMessage(InputMtdAreaTable));
            }

            // Verify chosen input AdminBndy feature classes have the expected fields

            gpParam = paramvalues.get_Element(InputAdminBndyFeatureClasses) as IGPParameter;
            IGPMultiValue multiValue = m_gpUtils.UnpackGPValue(gpParam) as IGPMultiValue;
            for (int i = 0; i < multiValue.Count; i++)
            {
                tableValue = multiValue.get_Value(i);
                if (!tableValue.IsEmpty())
                {
                    IDETable inputTable = m_gpUtils.DecodeDETable(tableValue);
                    CheckForTableFields(inputTable, AdminBndyFieldNames, AdminBndyFieldTypes, messages.GetMessage(InputAdminBndyFeatureClasses));
                }
            }

            // Check the input Streets feature class and Time Zone ID Field Name parameters together

            gpParam = paramvalues.get_Element(InputStreetsFeatureClass) as IGPParameter;
            tableValue = m_gpUtils.UnpackGPValue(gpParam);
            IGPParameter fieldNameParam = paramvalues.get_Element(InputTimeZoneIDBaseFieldName) as IGPParameter;
            IGPValue fieldNameValue = m_gpUtils.UnpackGPValue(fieldNameParam);
            if (!tableValue.IsEmpty())
            {
                // Verify chosen input Streets feature class has the expected fields

                IDETable inputTable = m_gpUtils.DecodeDETable(tableValue);
                CheckForTableFields(inputTable, StreetsFieldNames, StreetsFieldTypes, messages.GetMessage(InputStreetsFeatureClass));

                // Check to make sure that the Time Zone ID Base Field Name parameter is specified together with the Streets feature class

                if (fieldNameValue.IsEmpty())
                {
                    messages.GetMessage(InputTimeZoneIDBaseFieldName).Type = esriGPMessageType.esriGPMessageTypeError;
                    messages.GetMessage(InputTimeZoneIDBaseFieldName).Description = "This parameter must be specified together with the NW feature class.";
                }
                else
                {
                    // Verify chosen input time zone ID fields does not exist on the input Streets feature class

                    string fieldName = fieldNameValue.GetAsText();
                    if (inputTable.Fields.FindField("FT_" + fieldName) != -1)
                    {
                        messages.GetMessage(InputTimeZoneIDBaseFieldName).Type = esriGPMessageType.esriGPMessageTypeError;
                        messages.GetMessage(InputTimeZoneIDBaseFieldName).Description = "Field named FT_" + fieldName + " already exists.";
                    }
                    if (inputTable.Fields.FindField("TF_" + fieldName) != -1)
                    {
                        messages.GetMessage(InputTimeZoneIDBaseFieldName).Type = esriGPMessageType.esriGPMessageTypeError;
                        messages.GetMessage(InputTimeZoneIDBaseFieldName).Description = "Field named TF_" + fieldName + " already exists.";
                    }
                }
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

                IGPParameter gpParam = paramvalues.get_Element(InputMtdDSTTable) as IGPParameter;
                IGPValue inputMtdDSTTableValue = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(InputMtdCntryRefTable) as IGPParameter;
                IGPValue inputMtdCntryRefTableValue = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(InputMtdAreaTable) as IGPParameter;
                IGPValue inputMtdAreaTableValue = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(InputAdminBndyFeatureClasses) as IGPParameter;
                var inputAdminBndyFeatureClassesMultiValue = m_gpUtils.UnpackGPValue(gpParam) as IGPMultiValue;
                gpParam = paramvalues.get_Element(OutputFileGDB) as IGPParameter;
                IGPValue outputFileGDBValue = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(InputStreetsFeatureClass) as IGPParameter;
                IGPValue inputStreetsFeatureClassValue = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(InputTimeZoneIDBaseFieldName) as IGPParameter;
                IGPValue inputTimeZoneIDBaseFieldNameValue = m_gpUtils.UnpackGPValue(gpParam);

                bool processStreetsFC = (!(inputStreetsFeatureClassValue.IsEmpty()));
                string timeZoneIDBaseFieldName = "";
                if (!(inputTimeZoneIDBaseFieldNameValue.IsEmpty()))
                    timeZoneIDBaseFieldName = inputTimeZoneIDBaseFieldNameValue.GetAsText();

                // Get the path to the output file GDB

                string outputFileGdbPath = outputFileGDBValue.GetAsText();

                // Create the new file geodatabase

                AddMessage("Creating the file geodatabase...", messages, trackcancel);

                int lastBackslash = outputFileGdbPath.LastIndexOf("\\");
                CreateFileGDB createFGDBTool = new CreateFileGDB();
                createFGDBTool.out_folder_path = outputFileGdbPath.Remove(lastBackslash);
                createFGDBTool.out_name = outputFileGdbPath.Substring(lastBackslash + 1);
                gp.Execute(createFGDBTool, trackcancel);

                // Copy the MtdDST table to the file geodatabase and add the ADMIN_LVL and AREACODE fields to it

                AddMessage("Copying the MtdDST table to the file geodatabase...", messages, trackcancel);

                TableToTable importTableTool = new TableToTable();
                string inputMtdDSTTablePath = inputMtdDSTTableValue.GetAsText();
                importTableTool.in_rows = inputMtdDSTTablePath;
                importTableTool.out_path = outputFileGdbPath;
                importTableTool.out_name = "MtdDST";
                importTableTool.field_mapping = "AREA_ID \"AREA_ID\" true true false 4 Long 0 0 ,First,#," + inputMtdDSTTablePath + ",AREA_ID,-1,-1;" +
                                                "TIME_ZONE \"TIME_ZONE\" true true false 4 Text 0 0 ,First,#," + inputMtdDSTTablePath + ",TIME_ZONE,-1,-1;" +
                                                "DST_EXIST \"DST_EXIST\" true true false 1 Text 0 0 ,First,#," + inputMtdDSTTablePath + ",DST_EXIST,-1,-1";
                gp.Execute(importTableTool, trackcancel);

                string mtdDSTTablePath = outputFileGdbPath + "\\MtdDST";

                AddField addFieldTool = new AddField();
                addFieldTool.in_table = mtdDSTTablePath;
                addFieldTool.field_name = "ADMIN_LVL";
                addFieldTool.field_type = "SHORT";
                gp.Execute(addFieldTool, trackcancel);

                addFieldTool = new AddField();
                addFieldTool.in_table = mtdDSTTablePath;
                addFieldTool.field_name = "AREACODE";
                addFieldTool.field_type = "TEXT";
                addFieldTool.field_length = 76;
                gp.Execute(addFieldTool, trackcancel);

                // Copy the MtdArea table to the file geodatabase and index the AREA_ID field

                AddMessage("Copying the MtdArea table to the file geodatabase...", messages, trackcancel);

                importTableTool = new TableToTable();
                importTableTool.in_rows = inputMtdAreaTableValue.GetAsText();
                importTableTool.out_path = outputFileGdbPath;
                importTableTool.out_name = "MtdArea";
                gp.Execute(importTableTool, trackcancel);

                string mtdAreaTablePath = outputFileGdbPath + "\\MtdArea";

                AddIndex addIndexTool = new AddIndex();
                addIndexTool.in_table = mtdAreaTablePath;
                addIndexTool.fields = "AREA_ID";
                addIndexTool.index_name = "AREA_ID";
                gp.Execute(addIndexTool, trackcancel);

                // Calculate the ADMIN_LVL and AREACODE fields on the MtdDST table

                MakeTableView makeTableViewTool = new MakeTableView();
                makeTableViewTool.in_table = mtdDSTTablePath;
                makeTableViewTool.out_view = "MtdDST_Layer";
                gp.Execute(makeTableViewTool, trackcancel);

                AddJoin addJoinTool = new AddJoin();
                addJoinTool.in_layer_or_view = "MtdDST_Layer";
                addJoinTool.in_field = "AREA_ID";
                addJoinTool.join_table = mtdAreaTablePath;
                addJoinTool.join_field = "AREA_ID";
                addJoinTool.join_type = "KEEP_COMMON";
                gp.Execute(addJoinTool, trackcancel);

                AddMessage("Calculating the ADMIN_LVL field...", messages, trackcancel);

                CalculateField calcFieldTool = new CalculateField();
                calcFieldTool.in_table = "MtdDST_Layer";
                calcFieldTool.field = "MtdDST.ADMIN_LVL";
                calcFieldTool.expression = "[MtdArea.ADMIN_LVL]";
                calcFieldTool.expression_type = "VB";
                gp.Execute(calcFieldTool, trackcancel);

                AddMessage("Calculating the AREACODE field...", messages, trackcancel);

                calcFieldTool = new CalculateField();
                calcFieldTool.in_table = "MtdDST_Layer";
                calcFieldTool.field = "MtdDST.AREACODE";
                calcFieldTool.code_block = "lvl = [MtdArea.ADMIN_LVL]\n" +
                                           "s = CStr([MtdArea.AREACODE_1])\n" +
                                           "If lvl >= 2 Then s = s & \".\" & CStr([MtdArea.AREACODE_2])\n" +
                                           "If lvl >= 3 Then s = s & \".\" & CStr([MtdArea.AREACODE_3])\n" +
                                           "If lvl >= 4 Then s = s & \".\" & CStr([MtdArea.AREACODE_4])\n" +
                                           "If lvl >= 5 Then s = s & \".\" & CStr([MtdArea.AREACODE_5])\n" +
                                           "If lvl >= 6 Then s = s & \".\" & CStr([MtdArea.AREACODE_6])\n" +
                                           "If lvl >= 7 Then s = s & \".\" & CStr([MtdArea.AREACODE_7])";
                calcFieldTool.expression = "s";
                calcFieldTool.expression_type = "VB";
                gp.Execute(calcFieldTool, trackcancel);

                RemoveJoin removeJoinTool = new RemoveJoin();
                removeJoinTool.in_layer_or_view = "MtdDST_Layer";
                removeJoinTool.join_name = "MtdArea";
                gp.Execute(removeJoinTool, trackcancel);

                Delete deleteTool = new Delete();
                deleteTool.in_data = "MtdDST_Layer";
                gp.Execute(deleteTool, trackcancel);

                // Create the MtdDST# tables by admin levels and index the AREACODE field

                TableSelect tableSelectTool = null;
                for (int i = 1; i <= 7; i++)
                {
                    string iAsString = Convert.ToString(i, System.Globalization.CultureInfo.InvariantCulture);

                    AddMessage("Extracting level " + iAsString + " MtdDST rows...", messages, trackcancel);

                    tableSelectTool = new TableSelect();
                    tableSelectTool.in_table = mtdDSTTablePath;
                    tableSelectTool.out_table = mtdDSTTablePath + iAsString;
                    tableSelectTool.where_clause = "ADMIN_LVL = " + iAsString;
                    gp.Execute(tableSelectTool, trackcancel);

                    addIndexTool = new AddIndex();
                    addIndexTool.in_table = mtdDSTTablePath + iAsString;
                    addIndexTool.fields = "AREACODE";
                    addIndexTool.index_name = "AREACODE";
                    gp.Execute(addIndexTool, trackcancel);
                }

                deleteTool = new Delete();
                deleteTool.in_data = mtdDSTTablePath;
                gp.Execute(deleteTool, trackcancel);

                // Copy the MtdCntryRef table to the file geodatabase (use Statistics tool to remove duplicate rows)

                AddMessage("Copying the MtdCntryRef table to the file geodatabase...", messages, trackcancel);

                string inputMtdCntryRefTablePath = inputMtdCntryRefTableValue.GetAsText();
                string mtdCntryRefTablePath = outputFileGdbPath + "\\MtdCntryRef";
                Statistics statsTool = new Statistics();
                statsTool.in_table = inputMtdCntryRefTablePath;
                statsTool.out_table = mtdCntryRefTablePath;
                statsTool.statistics_fields = "ISO_CODE COUNT";
                statsTool.case_field = "GOVT_CODE;ISO_CODE;DRIVING_SD;ADMINLEVEL";
                gp.Execute(statsTool, trackcancel);

                DeleteField deleteFieldTool = new DeleteField();
                deleteFieldTool.in_table = mtdCntryRefTablePath;
                deleteFieldTool.drop_field = "FREQUENCY;COUNT_ISO_CODE";
                gp.Execute(deleteFieldTool, trackcancel);

                // Index the GOVT_CODE field

                addIndexTool = new AddIndex();
                addIndexTool.in_table = mtdCntryRefTablePath;
                addIndexTool.fields = "GOVT_CODE";
                addIndexTool.index_name = "GOVT_CODE";
                gp.Execute(addIndexTool, trackcancel);

                // Extract the top level (country) records from the MtdArea table and index the AREACODE_1 field

                AddMessage("Extracting the top-level rows from the MtdArea table...", messages, trackcancel);

                string mtdTopAreaTablePath = outputFileGdbPath + "\\TopArea";

                tableSelectTool = new TableSelect();
                tableSelectTool.in_table = mtdAreaTablePath;
                tableSelectTool.out_table = mtdTopAreaTablePath;
                tableSelectTool.where_clause = "AREACODE_2 = 0 AND AREA_TYPE = 'B'";
                gp.Execute(tableSelectTool, trackcancel);

                addIndexTool = new AddIndex();
                addIndexTool.in_table = mtdTopAreaTablePath;
                addIndexTool.fields = "AREACODE_1";
                addIndexTool.index_name = "AREACODE_1";
                gp.Execute(addIndexTool, trackcancel);

                // Create and calculate the TOP_GOVT_CODE field on the MtdArea table

                addFieldTool = new AddField();
                addFieldTool.in_table = mtdAreaTablePath;
                addFieldTool.field_name = "TOP_GOVT_CODE";
                addFieldTool.field_type = "LONG";
                gp.Execute(addFieldTool, trackcancel);

                makeTableViewTool = new MakeTableView();
                makeTableViewTool.in_table = mtdAreaTablePath;
                makeTableViewTool.out_view = "MtdArea_Layer";
                gp.Execute(makeTableViewTool, trackcancel);

                addJoinTool = new AddJoin();
                addJoinTool.in_layer_or_view = "MtdArea_Layer";
                addJoinTool.in_field = "AREACODE_1";
                addJoinTool.join_table = mtdTopAreaTablePath;
                addJoinTool.join_field = "AREACODE_1";
                addJoinTool.join_type = "KEEP_COMMON";
                gp.Execute(addJoinTool, trackcancel);

                AddMessage("Calculating the TOP_GOVT_CODE field...", messages, trackcancel);

                calcFieldTool = new CalculateField();
                calcFieldTool.in_table = "MtdArea_Layer";
                calcFieldTool.field = "MtdArea.TOP_GOVT_CODE";
                calcFieldTool.expression = "[TopArea.GOVT_CODE]";
                calcFieldTool.expression_type = "VB";
                gp.Execute(calcFieldTool, trackcancel);

                removeJoinTool = new RemoveJoin();
                removeJoinTool.in_layer_or_view = "MtdArea_Layer";
                removeJoinTool.join_name = "TopArea";
                gp.Execute(removeJoinTool, trackcancel);

                deleteTool = new Delete();
                deleteTool.in_data = mtdTopAreaTablePath;
                gp.Execute(deleteTool, trackcancel);

                // Create and calculate the ISO_CODE and DRIVING_SD string fields

                addFieldTool = new AddField();
                addFieldTool.in_table = mtdAreaTablePath;
                addFieldTool.field_name = "ISO_CODE";
                addFieldTool.field_type = "TEXT";
                addFieldTool.field_length = 3;
                gp.Execute(addFieldTool, trackcancel);

                addFieldTool = new AddField();
                addFieldTool.in_table = mtdAreaTablePath;
                addFieldTool.field_name = "DRIVING_SD";
                addFieldTool.field_type = "TEXT";
                addFieldTool.field_length = 1;
                gp.Execute(addFieldTool, trackcancel);

                addJoinTool = new AddJoin();
                addJoinTool.in_layer_or_view = "MtdArea_Layer";
                addJoinTool.in_field = "TOP_GOVT_CODE";
                addJoinTool.join_table = mtdCntryRefTablePath;
                addJoinTool.join_field = "GOVT_CODE";
                addJoinTool.join_type = "KEEP_COMMON";
                gp.Execute(addJoinTool, trackcancel);

                AddMessage("Calculating the ISO_CODE field...", messages, trackcancel);

                calcFieldTool = new CalculateField();
                calcFieldTool.in_table = "MtdArea_Layer";
                calcFieldTool.field = "MtdArea.ISO_CODE";
                calcFieldTool.expression = "[MtdCntryRef.ISO_CODE]";
                calcFieldTool.expression_type = "VB";
                gp.Execute(calcFieldTool, trackcancel);

                AddMessage("Calculating the DRIVING_SD field...", messages, trackcancel);

                calcFieldTool = new CalculateField();
                calcFieldTool.in_table = "MtdArea_Layer";
                calcFieldTool.field = "MtdArea.DRIVING_SD";
                calcFieldTool.expression = "[MtdCntryRef.DRIVING_SD]";
                calcFieldTool.expression_type = "VB";
                gp.Execute(calcFieldTool, trackcancel);

                removeJoinTool = new RemoveJoin();
                removeJoinTool.in_layer_or_view = "MtdArea_Layer";
                removeJoinTool.join_name = "MtdCntryRef";
                gp.Execute(removeJoinTool, trackcancel);

                // Create and calculate the FullAREACODE# string fields and the UTCOffset and DST fields

                addFieldTool = new AddField();
                addFieldTool.in_table = mtdAreaTablePath;
                addFieldTool.field_type = "SHORT";
                addFieldTool.field_name = "UTCOffset";
                gp.Execute(addFieldTool, trackcancel);
                addFieldTool.field_name = "DST";
                gp.Execute(addFieldTool, trackcancel);

                string codeBlock = "lvl = [ADMIN_LVL]\ns = CStr([AREACODE_1])";
                for (int i = 1; i <= 7; i++)
                {
                    string iAsString = Convert.ToString(i, System.Globalization.CultureInfo.InvariantCulture);
                    string iPlusOne = Convert.ToString(i+1, System.Globalization.CultureInfo.InvariantCulture);
                    string fullAreaCodeFieldName = "FullAREACODE" + iAsString;
                    addFieldTool = new AddField();
                    addFieldTool.in_table = mtdAreaTablePath;
                    addFieldTool.field_name = fullAreaCodeFieldName;
                    addFieldTool.field_type = "TEXT";
                    addFieldTool.field_length = 76;
                    gp.Execute(addFieldTool, trackcancel);

                    AddMessage("Calculating the FullAREACODE" + iAsString + " field...", messages, trackcancel);

                    calcFieldTool = new CalculateField();
                    calcFieldTool.in_table = mtdAreaTablePath;
                    calcFieldTool.field = fullAreaCodeFieldName;
                    calcFieldTool.code_block = codeBlock;
                    calcFieldTool.expression = "s";
                    calcFieldTool.expression_type = "VB";
                    gp.Execute(calcFieldTool, trackcancel);
                    codeBlock = codeBlock + "\nIf lvl >= " + iPlusOne + " Then s = s & \".\" & CStr([AREACODE_" + iPlusOne + "])";

                    string dstJoinTableName = "MtdDST" + iAsString;
                    string dstJoinTablePath = outputFileGdbPath + "\\" + dstJoinTableName;

                    addJoinTool = new AddJoin();
                    addJoinTool.in_layer_or_view = "MtdArea_Layer";
                    addJoinTool.in_field = fullAreaCodeFieldName;
                    addJoinTool.join_table = dstJoinTablePath;
                    addJoinTool.join_field = "AREACODE";
                    addJoinTool.join_type = "KEEP_COMMON";
                    gp.Execute(addJoinTool, trackcancel);

                    AddMessage("Calculating the UTCOffset field (" + iAsString + " of 7)...", messages, trackcancel);

                    calcFieldTool = new CalculateField();
                    calcFieldTool.in_table = "MtdArea_Layer";
                    calcFieldTool.field = "MtdArea.UTCOffset";
                    calcFieldTool.code_block = "s = [MtdArea.UTCOffset]\n" +
                                               "joinValue = [" + dstJoinTableName + ".TIME_ZONE]\n" +
                                               "If Not IsNull(joinValue) Then\n" +
                                               "  If Trim(joinValue) <> \"\" Then s = CInt(joinValue) * 6\n" +
                                               "End If";
                    calcFieldTool.expression = "s";
                    calcFieldTool.expression_type = "VB";
                    gp.Execute(calcFieldTool, trackcancel);

                    AddMessage("Calculating the DST field (" + iAsString + " of 7)...", messages, trackcancel);

                    calcFieldTool = new CalculateField();
                    calcFieldTool.in_table = "MtdArea_Layer";
                    calcFieldTool.field = "MtdArea.DST";
                    calcFieldTool.code_block = "s = [MtdArea.DST]\n" +
                                               "joinValue = [" + dstJoinTableName + ".DST_EXIST]\n" +
                                               "If Not IsNull(joinValue) Then\n" +
                                               "  Select Case Trim(joinValue)\n" +
                                               "    Case \"Y\": s = 1\n    Case \"N\": s = 0\n" +
                                               "  End Select\n" +
                                               "End If";
                    calcFieldTool.expression = "s";
                    calcFieldTool.expression_type = "VB";
                    gp.Execute(calcFieldTool, trackcancel);

                    removeJoinTool = new RemoveJoin();
                    removeJoinTool.in_layer_or_view = "MtdArea_Layer";
                    removeJoinTool.join_name = dstJoinTableName;
                    gp.Execute(removeJoinTool, trackcancel);

                    deleteTool = new Delete();
                    deleteTool.in_data = dstJoinTablePath;
                    gp.Execute(deleteTool, trackcancel);
                }

                deleteTool = new Delete();
                deleteTool.in_data = "MtdArea_Layer";
                gp.Execute(deleteTool, trackcancel);

                // Create and calculate the sortable MSTIMEZONE field on the MtdArea table

                addFieldTool = new AddField();
                addFieldTool.in_table = mtdAreaTablePath;
                addFieldTool.field_name = "SortableMSTIMEZONE";
                addFieldTool.field_type = "TEXT";
                addFieldTool.field_length = 60;
                gp.Execute(addFieldTool, trackcancel);

                AddMessage("Calculating the time zones...", messages, trackcancel);

                calcFieldTool = new CalculateField();
                calcFieldTool.in_table = mtdAreaTablePath;
                calcFieldTool.field = "SortableMSTIMEZONE";
                calcFieldTool.code_block = TimeZoneUtilities.MakeSortableMSTIMEZONECode("ISO_CODE");
                calcFieldTool.expression = "z";
                calcFieldTool.expression_type = "VB";
                gp.Execute(calcFieldTool, trackcancel);

                // Extract the MtdArea rows to be used for generating the time zone polygons and index the AREA_ID field

                string mtdAreaForTZPolysTablePath = outputFileGdbPath + "\\MtdAreaForTZPolys";
                tableSelectTool = new TableSelect();
                tableSelectTool.in_table = mtdAreaTablePath;
                tableSelectTool.out_table = mtdAreaForTZPolysTablePath;
                tableSelectTool.where_clause = CreateWhereClauseForAdminLvlByCountry(outputFileGdbPath, "MtdCntryRef");
                gp.Execute(tableSelectTool, trackcancel);

                addIndexTool = new AddIndex();
                addIndexTool.in_table = mtdAreaForTZPolysTablePath;
                addIndexTool.fields = "AREA_ID";
                addIndexTool.index_name = "AREA_ID";
                gp.Execute(addIndexTool, trackcancel);

                // We no longer need the MtdCntryRef table anymore

                deleteTool = new Delete();
                deleteTool.in_data = mtdCntryRefTablePath;
                gp.Execute(deleteTool, trackcancel);

                // Merge the AdminBndy feature classes together into one feature class

                int numAdminBndyFCs = inputAdminBndyFeatureClassesMultiValue.Count;
                string mergeToolInputs = "";
                for (int i = 0; i < numAdminBndyFCs; i++)
                {
                    mergeToolInputs = mergeToolInputs + inputAdminBndyFeatureClassesMultiValue.get_Value(i).GetAsText() + ";";
                }
                mergeToolInputs = mergeToolInputs.Remove(mergeToolInputs.Length - 1);
                string adminBndyFCPath = outputFileGdbPath + "\\AdminBndy";

                AddMessage("Merging the Administrative Boundary feature classes...", messages, trackcancel);

                Merge mergeTool = new Merge();
                mergeTool.inputs = mergeToolInputs;
                mergeTool.output = adminBndyFCPath;
                gp.Execute(mergeTool, trackcancel);

                // Join the AdminBndy polygons to the MtdArea rows to be used for generating the time zone polygons

                MakeFeatureLayer makeFeatureLayerTool = new MakeFeatureLayer();
                makeFeatureLayerTool.in_features = adminBndyFCPath;
                makeFeatureLayerTool.out_layer = "AdminBndy_Layer";
                gp.Execute(makeFeatureLayerTool, trackcancel);

                addJoinTool = new AddJoin();
                addJoinTool.in_layer_or_view = "AdminBndy_Layer";
                addJoinTool.in_field = "AREA_ID";
                addJoinTool.join_table = mtdAreaForTZPolysTablePath;
                addJoinTool.join_field = "AREA_ID";
                addJoinTool.join_type = "KEEP_COMMON";
                gp.Execute(addJoinTool, trackcancel);

                FeatureClassToFeatureClass importFCTool = new FeatureClassToFeatureClass();
                importFCTool.in_features = "AdminBndy_Layer";
                importFCTool.out_path = outputFileGdbPath;
                importFCTool.out_name = "UndissolvedTZPolys";
                importFCTool.field_mapping = "SortableMSTIMEZONE \"SortableMSTIMEZONE\" true true false 60 Text 0 0 ,First,#," + 
                                             mtdAreaForTZPolysTablePath + ",MtdAreaForTZPolys.SortableMSTIMEZONE,-1,-1";
                gp.Execute(importFCTool, trackcancel);
                string undissolvedTZPolysFCPath = outputFileGdbPath + "\\UndissolvedTZPolys";

                removeJoinTool = new RemoveJoin();
                removeJoinTool.in_layer_or_view = "AdminBndy_Layer";
                removeJoinTool.join_name = "MtdAreaForTZPolys";
                gp.Execute(removeJoinTool, trackcancel);

                deleteTool = new Delete();
                deleteTool.in_data = "AdminBndy_Layer";
                gp.Execute(deleteTool, trackcancel);

                deleteTool = new Delete();
                deleteTool.in_data = adminBndyFCPath;
                gp.Execute(deleteTool, trackcancel);
                deleteTool.in_data = mtdAreaForTZPolysTablePath;
                gp.Execute(deleteTool, trackcancel);

                // Dissolve the time zone polygons together

                AddMessage("Dissolving the time zones...", messages, trackcancel);

                string timeZoneFCPath = outputFileGdbPath + "\\" + TimeZoneFCName;
                Dissolve dissolveTool = new Dissolve();
                dissolveTool.in_features = undissolvedTZPolysFCPath;
                dissolveTool.out_feature_class = timeZoneFCPath;
                dissolveTool.dissolve_field = "SortableMSTIMEZONE";
                dissolveTool.multi_part = "MULTI_PART";
                gp.Execute(dissolveTool, trackcancel);

                deleteTool = new Delete();
                deleteTool.in_data = undissolvedTZPolysFCPath;
                gp.Execute(deleteTool, trackcancel);

                // Create and calculate the MSTIMEZONE field

                addFieldTool = new AddField();
                addFieldTool.in_table = timeZoneFCPath;
                addFieldTool.field_name = "MSTIMEZONE";
                addFieldTool.field_type = "TEXT";
                addFieldTool.field_length = 50;
                gp.Execute(addFieldTool, trackcancel);

                AddMessage("Calculating the time zones...", messages, trackcancel);

                calcFieldTool = new CalculateField();
                calcFieldTool.in_table = timeZoneFCPath;
                calcFieldTool.field = "MSTIMEZONE";
                calcFieldTool.expression = "Mid([SortableMSTIMEZONE], 7)";
                calcFieldTool.expression_type = "VB";
                gp.Execute(calcFieldTool, trackcancel);

                // Delete the old sortable MSTIMEZONE field

                deleteFieldTool = new DeleteField();
                deleteFieldTool.in_table = timeZoneFCPath;
                deleteFieldTool.drop_field = "SortableMSTIMEZONE";
                gp.Execute(deleteFieldTool, trackcancel);

                if (processStreetsFC)
                {
                    // Create the network dataset time zone table

                    AddMessage("Creating the time zones table...", messages, trackcancel);

                    importTableTool = new TableToTable();
                    importTableTool.in_rows = timeZoneFCPath;
                    importTableTool.out_path = outputFileGdbPath;
                    importTableTool.out_name = TimeZonesTableName;
                    importTableTool.field_mapping = "MSTIMEZONE \"MSTIMEZONE\" true true false 50 Text 0 0 ,First,#," +
                                                    timeZoneFCPath + ",MSTIMEZONE,-1,-1";
                    gp.Execute(importTableTool, trackcancel);

                    // Separate the MtdArea table by driving side and index the AREA_ID field on each

                    AddMessage("Extracting rows for the left-side driving areas...", messages, trackcancel);

                    string drivingLTablePath = mtdAreaTablePath + "DrivingL";
                    string drivingRTablePath = mtdAreaTablePath + "DrivingR";

                    tableSelectTool = new TableSelect();
                    tableSelectTool.in_table = mtdAreaTablePath;
                    tableSelectTool.out_table = drivingLTablePath;
                    tableSelectTool.where_clause = "DRIVING_SD = 'L'";
                    gp.Execute(tableSelectTool, trackcancel);

                    addIndexTool = new AddIndex();
                    addIndexTool.in_table = drivingLTablePath;
                    addIndexTool.fields = "AREA_ID";
                    addIndexTool.index_name = "AREA_ID";
                    gp.Execute(addIndexTool, trackcancel);

                    AddMessage("Extracting rows for the right-side driving areas...", messages, trackcancel);

                    tableSelectTool = new TableSelect();
                    tableSelectTool.in_table = mtdAreaTablePath;
                    tableSelectTool.out_table = drivingRTablePath;
                    tableSelectTool.where_clause = "DRIVING_SD = 'R'";
                    gp.Execute(tableSelectTool, trackcancel);

                    addIndexTool = new AddIndex();
                    addIndexTool.in_table = drivingRTablePath;
                    addIndexTool.fields = "AREA_ID";
                    addIndexTool.index_name = "AREA_ID";
                    gp.Execute(addIndexTool, trackcancel);

                    // Import the Streets feature class to the file geodatabase and
                    // add the FT_TimeZoneID and TF_TimeZoneID fields

                    AddMessage("Copying the Streets feature class to the geodatabase...", messages, trackcancel);

                    importFCTool = new FeatureClassToFeatureClass();
                    importFCTool.in_features = inputStreetsFeatureClassValue.GetAsText();
                    importFCTool.out_path = outputFileGdbPath;
                    importFCTool.out_name = StreetsFCName;
                    gp.Execute(importFCTool, trackcancel);

                    string pathToStreetsFC = outputFileGdbPath + "\\" + StreetsFCName;

                    addFieldTool = new AddField();
                    addFieldTool.in_table = pathToStreetsFC;
                    addFieldTool.field_name = "FT_" + timeZoneIDBaseFieldName;
                    addFieldTool.field_type = "SHORT";
                    gp.Execute(addFieldTool, trackcancel);
                    addFieldTool.field_name = "TF_" + timeZoneIDBaseFieldName;
                    addFieldTool.field_type = "SHORT";
                    gp.Execute(addFieldTool, trackcancel);

                    // Calculate the TimeZoneID fields

                    makeFeatureLayerTool = new MakeFeatureLayer();
                    makeFeatureLayerTool.in_features = pathToStreetsFC;
                    makeFeatureLayerTool.out_layer = "Streets_Layer";
                    gp.Execute(makeFeatureLayerTool, trackcancel);

                    addJoinTool = new AddJoin();
                    addJoinTool.in_layer_or_view = "Streets_Layer";
                    addJoinTool.in_field = "R_AREA_ID";
                    addJoinTool.join_table = drivingLTablePath;
                    addJoinTool.join_field = "AREA_ID";
                    addJoinTool.join_type = "KEEP_COMMON";
                    gp.Execute(addJoinTool, trackcancel);

                    AddMessage("Calculating the FT_" + timeZoneIDBaseFieldName + " field for left driving side roads...", messages, trackcancel);

                    calcFieldTool = new CalculateField();
                    calcFieldTool.in_table = "Streets_Layer";
                    calcFieldTool.field = StreetsFCName + ".FT_" + timeZoneIDBaseFieldName;
                    calcFieldTool.code_block = TimeZoneUtilities.MakeTimeZoneIDCode(outputFileGdbPath, TimeZonesTableName, "MtdAreaDrivingL.SortableMSTIMEZONE");
                    calcFieldTool.expression = "tzID";
                    calcFieldTool.expression_type = "VB";
                    gp.Execute(calcFieldTool, trackcancel);

                    removeJoinTool = new RemoveJoin();
                    removeJoinTool.in_layer_or_view = "Streets_Layer";
                    removeJoinTool.join_name = "MtdAreaDrivingL";
                    gp.Execute(removeJoinTool, trackcancel);

                    addJoinTool = new AddJoin();
                    addJoinTool.in_layer_or_view = "Streets_Layer";
                    addJoinTool.in_field = "L_AREA_ID";
                    addJoinTool.join_table = drivingRTablePath;
                    addJoinTool.join_field = "AREA_ID";
                    addJoinTool.join_type = "KEEP_COMMON";
                    gp.Execute(addJoinTool, trackcancel);

                    AddMessage("Calculating the FT_" + timeZoneIDBaseFieldName + " field for right driving side roads...", messages, trackcancel);

                    calcFieldTool = new CalculateField();
                    calcFieldTool.in_table = "Streets_Layer";
                    calcFieldTool.field = StreetsFCName + ".FT_" + timeZoneIDBaseFieldName;
                    calcFieldTool.code_block = TimeZoneUtilities.MakeTimeZoneIDCode(outputFileGdbPath, TimeZonesTableName, "MtdAreaDrivingR.SortableMSTIMEZONE");
                    calcFieldTool.expression = "tzID";
                    calcFieldTool.expression_type = "VB";
                    gp.Execute(calcFieldTool, trackcancel);

                    removeJoinTool = new RemoveJoin();
                    removeJoinTool.in_layer_or_view = "Streets_Layer";
                    removeJoinTool.join_name = "MtdAreaDrivingR";
                    gp.Execute(removeJoinTool, trackcancel);

                    addJoinTool = new AddJoin();
                    addJoinTool.in_layer_or_view = "Streets_Layer";
                    addJoinTool.in_field = "L_AREA_ID";
                    addJoinTool.join_table = drivingLTablePath;
                    addJoinTool.join_field = "AREA_ID";
                    addJoinTool.join_type = "KEEP_COMMON";
                    gp.Execute(addJoinTool, trackcancel);

                    AddMessage("Calculating the TF_" + timeZoneIDBaseFieldName + " field for left driving side roads...", messages, trackcancel);

                    calcFieldTool = new CalculateField();
                    calcFieldTool.in_table = "Streets_Layer";
                    calcFieldTool.field = StreetsFCName + ".TF_" + timeZoneIDBaseFieldName;
                    calcFieldTool.code_block = TimeZoneUtilities.MakeTimeZoneIDCode(outputFileGdbPath, TimeZonesTableName, "MtdAreaDrivingL.SortableMSTIMEZONE");
                    calcFieldTool.expression = "tzID";
                    calcFieldTool.expression_type = "VB";
                    gp.Execute(calcFieldTool, trackcancel);

                    removeJoinTool = new RemoveJoin();
                    removeJoinTool.in_layer_or_view = "Streets_Layer";
                    removeJoinTool.join_name = "MtdAreaDrivingL";
                    gp.Execute(removeJoinTool, trackcancel);

                    addJoinTool = new AddJoin();
                    addJoinTool.in_layer_or_view = "Streets_Layer";
                    addJoinTool.in_field = "R_AREA_ID";
                    addJoinTool.join_table = drivingRTablePath;
                    addJoinTool.join_field = "AREA_ID";
                    addJoinTool.join_type = "KEEP_COMMON";
                    gp.Execute(addJoinTool, trackcancel);

                    AddMessage("Calculating the TF_" + timeZoneIDBaseFieldName + " field for right driving side roads...", messages, trackcancel);

                    calcFieldTool = new CalculateField();
                    calcFieldTool.in_table = "Streets_Layer";
                    calcFieldTool.field = StreetsFCName + ".TF_" + timeZoneIDBaseFieldName;
                    calcFieldTool.code_block = TimeZoneUtilities.MakeTimeZoneIDCode(outputFileGdbPath, TimeZonesTableName, "MtdAreaDrivingR.SortableMSTIMEZONE");
                    calcFieldTool.expression = "tzID";
                    calcFieldTool.expression_type = "VB";
                    gp.Execute(calcFieldTool, trackcancel);

                    removeJoinTool = new RemoveJoin();
                    removeJoinTool.in_layer_or_view = "Streets_Layer";
                    removeJoinTool.join_name = "MtdAreaDrivingR";
                    gp.Execute(removeJoinTool, trackcancel);

                    deleteTool = new Delete();
                    deleteTool.in_data = "Streets_Layer";
                    gp.Execute(deleteTool, trackcancel);

                    deleteTool = new Delete();
                    deleteTool.in_data = drivingLTablePath;
                    gp.Execute(deleteTool, trackcancel);
                    deleteTool.in_data = drivingRTablePath;
                    gp.Execute(deleteTool, trackcancel);
                }
                else
                {
                    // Create a dummy TimeZones table and a dummy Streets feature class

                    CreateTable createTableTool = new CreateTable();
                    createTableTool.out_path = outputFileGdbPath;
                    createTableTool.out_name = TimeZonesTableName;
                    gp.Execute(createTableTool, trackcancel);

                    CreateFeatureclass createFCTool = new CreateFeatureclass();
                    createFCTool.out_path = outputFileGdbPath;
                    createFCTool.out_name = StreetsFCName;
                    createFCTool.geometry_type = "POLYLINE";
                    gp.Execute(createFCTool, trackcancel);
                }

                deleteTool = new Delete();
                deleteTool.in_data = mtdAreaTablePath;
                gp.Execute(deleteTool, trackcancel);
                
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
                return "Process NAVSTREETS™ Time Zones";
            }
        }

        public string MetadataFile
        {
            get
            {
                string filePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                return System.IO.Path.Combine(filePath, "ProcessNavStreetsTimeZones_nasample.xml");
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
            return true;
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
                return "ProcessNavStreetsTimeZones";
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

        private string CreateWhereClauseForAdminLvlByCountry(string outputFileGdbPath, string mtdCntryRefTableName)
        {
            // Open the MtdCntryRef Table

            Type factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.FileGDBWorkspaceFactory");
            var gdbWSF = Activator.CreateInstance(factoryType) as IWorkspaceFactory;
            var gdbFWS = gdbWSF.OpenFromFile(outputFileGdbPath, 0) as IFeatureWorkspace;
            ITable mtdCntryRefTable = gdbFWS.OpenTable(mtdCntryRefTableName);

            // Find the ISO_CODE and ADMINLEVEL fields

            int isoCodeField = mtdCntryRefTable.FindField("ISO_CODE");
            int adminLevelField = mtdCntryRefTable.FindField("ADMINLEVEL");

            // Loop through the table generating the Where clause

            string s = "";
            ICursor cur = mtdCntryRefTable.Search(null, true);
            IRow inputTableRow = null;
            while ((inputTableRow = cur.NextRow()) != null)
            {
                s = s + "(ISO_CODE = '" + inputTableRow.get_Value(isoCodeField) + "' AND ADMIN_LVL = " + Convert.ToString(Convert.ToInt32(inputTableRow.get_Value(adminLevelField)) - 1, System.Globalization.CultureInfo.InvariantCulture) + ") OR ";
            }
            s = s.Remove(s.Length - 4);    // Remove the last " OR " from the string

            return s;
        }
    }
}
