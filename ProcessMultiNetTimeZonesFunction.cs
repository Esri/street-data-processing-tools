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
    /// Summary description for ProcessMultiNetTimeZonesFunction.
    /// </summary>
    ///

    [Guid("6A1EC56D-8587-446f-A0FC-434ED5ED0D82")]
    [ClassInterface(ClassInterfaceType.None)]
    [ProgId("GPProcessVendorDataFunctions.ProcessMultiNetTimeZonesFunction")]

    class ProcessMultiNetTimeZonesFunction : IGPFunction2
    {
        #region Constants
        // names
        private const string DefaultTimeZoneIDFieldName = "TimeZoneID";
        private const string TimeZoneFCName = "TimeZonePolygons";
        private const string StreetsFCName = "Streets";
        private const string TimeZonesTableName = "TimeZones";
        
        // parameter index constants
        private const int InputAETable = 0;
        private const int InputAdminAreaFeatureClasses = 1;
        private const int OutputFileGDB = 2;
        private const int InputTATable = 3;
        private const int InputNWFeatureClass = 4;
        private const int InputTimeZoneIDBaseFieldName = 5;
        private const int OutputTimeZoneFeatureClass = 6;
        private const int OutputTimeZonesTable = 7;
        private const int OutputStreetsFeatureClass = 8;

        // field names and types
        private static readonly string[] AEFieldNames = new string[]
                                        { "ID", "FEATTYP", "ATTTYP", "ATTVALUE" };
        private static readonly esriFieldType[] AEFieldTypes = new esriFieldType[]
                                        { esriFieldType.esriFieldTypeDouble,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString };
        private static readonly string[] AdminAreaFieldNames = new string[]
                                        { "ID", "FEATTYP", "ORDER00", "FEATAREA", "FEATPERIM", "NAME", "NAMELC" };
        private static readonly esriFieldType[] AdminAreaFieldTypes = new esriFieldType[]
                                        { esriFieldType.esriFieldTypeDouble,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeDouble,
                                          esriFieldType.esriFieldTypeDouble,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeString };
        private static readonly string[] TAFieldNames = new string[]
                                        { "ID", "TRPELTYP", "AREID", "ARETYP", "SOL" };
        private static readonly esriFieldType[] TAFieldTypes = new esriFieldType[]
                                        { esriFieldType.esriFieldTypeDouble,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeDouble,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeSmallInteger };
        private static readonly string[] NWFieldNames = new string[]
                                        { "ID", "FEATTYP", "F_JNCTID", "F_JNCTTYP", "T_JNCTID", "T_JNCTTYP",
                                          "METERS", "FRC", "NET2CLASS", "NAME", "FOW", "FREEWAY",
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
                                          esriFieldType.esriFieldTypeDouble,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeString,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeSmallInteger,
                                          esriFieldType.esriFieldTypeString,
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
        #endregion

        private IArray m_parameters;
        private IGPUtilities m_gpUtils;

        public ProcessMultiNetTimeZonesFunction()
        {
            // Create the GPUtilities Object
            m_gpUtils = new GPUtilitiesClass();
        }

        #region IGPFunction2 Members

        public IArray ParameterInfo
        {
            // create and return the parameters for this function:
            // 0 - Input Administrative Area Extended Attributes (AE) Table
            // 1 - Input Administrative Area (A0~A9) Feature Classes
            // 2 - Output File Geodatabase
            // 3 - Input Transportation Element Belonging to Area (TA) Table (optional)
            // 4 - Input Network Geometry (NW) Feature Class (optional)
            // 5 - Time Zone ID Base Field Name (optional)
            // 6 - Output Time Zone Feature Class (derived parameter)
            // 7 - Output Time Zones Table (derived parameter)
            // 8 - Output Streets Feature Class (derived parameter)

            get
            {
                IArray paramArray = new ArrayClass();

                // 0 - input_mp_table

                IGPParameterEdit paramEdit = new GPParameterClass();
                paramEdit.DataType = new DETableTypeClass() as IGPDataType;
                paramEdit.Value = new DETableClass() as IGPValue;
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Input Administrative Area Extended Attributes (AE) Table";
                paramEdit.Enabled = true;
                paramEdit.Name = "input_ae_table";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeRequired;

                paramArray.Add(paramEdit as object);

                // 1 - input_admin_area_feature_classes

                paramEdit = new GPParameterClass();
                IGPMultiValueType mvType = new GPMultiValueTypeClass();
                mvType.MemberDataType = new DEFeatureClassTypeClass();
                paramEdit.DataType = mvType as IGPDataType;
                IGPMultiValue mvValue = new GPMultiValueClass();
                mvValue.MemberDataType = new DEFeatureClassTypeClass() as IGPDataType;
                paramEdit.Value = mvValue as IGPValue;
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Input Administrative Area (A0~A9) Feature Classes";
                paramEdit.Enabled = true;
                paramEdit.Name = "input_admin_area_feature_classes";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeRequired;

                IGPFeatureClassDomain polygonFeatureClassDomain = new GPFeatureClassDomainClass();
                polygonFeatureClassDomain.AddType(esriGeometryType.esriGeometryPolygon);
                paramEdit.Domain = polygonFeatureClassDomain as IGPDomain;

                paramArray.Add(paramEdit as object);

                // 2 - output_file_geodatabase

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

                // 3 - input_ta_table

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new DETableTypeClass() as IGPDataType;
                paramEdit.Value = new DETableClass() as IGPValue;
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Input Transportation Element Belonging to Area (TA) Table";
                paramEdit.Enabled = true;
                paramEdit.Name = "input_ta_table";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeOptional;

                paramArray.Add(paramEdit as object);

                // 4 - input_nw_feature_class

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new DEFeatureClassTypeClass() as IGPDataType;
                paramEdit.Value = new DEFeatureClass() as IGPValue;
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionInput;
                paramEdit.DisplayName = "Input Network Geometry (NW) Feature Class";
                paramEdit.Enabled = true;
                paramEdit.Name = "input_nw_feature_class";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeOptional;

                IGPFeatureClassDomain lineFeatureClassDomain = new GPFeatureClassDomainClass();
                lineFeatureClassDomain.AddType(esriGeometryType.esriGeometryLine);
                lineFeatureClassDomain.AddType(esriGeometryType.esriGeometryPolyline);
                paramEdit.Domain = lineFeatureClassDomain as IGPDomain;

                paramArray.Add(paramEdit as object);

                // 5 - time_zone_id_base_field_name

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

                // 6 - output_time_zone_feature_class

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new DEFeatureClassTypeClass() as IGPDataType;
                paramEdit.Value = new DEFeatureClassClass() as IGPValue;
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionOutput;
                paramEdit.DisplayName = "Output Time Zone Feature Class";
                paramEdit.Enabled = true;
                paramEdit.Name = "output_time_zone_feature_class";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeDerived;

                paramArray.Add(paramEdit as object);

                // 7 - output_time_zones_table

                paramEdit = new GPParameterClass();
                paramEdit.DataType = new DETableTypeClass() as IGPDataType;
                paramEdit.Value = new DETableClass() as IGPValue;
                paramEdit.Direction = esriGPParameterDirection.esriGPParameterDirectionOutput;
                paramEdit.DisplayName = "Output Time Zones Table";
                paramEdit.Enabled = true;
                paramEdit.Name = "output_time_zones_table";
                paramEdit.ParameterType = esriGPParameterType.esriGPParameterTypeDerived;

                paramArray.Add(paramEdit as object);

                // 8 - output_streets_feature_class

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

            // Verify chosen input AE table has the expected fields

            gpParam = paramvalues.get_Element(InputAETable) as IGPParameter;
            IGPValue tableValue = m_gpUtils.UnpackGPValue(gpParam);
            if (!tableValue.IsEmpty())
            {
                IDETable inputTable = m_gpUtils.DecodeDETable(tableValue);
                CheckForTableFields(inputTable, AEFieldNames, AEFieldTypes, messages.GetMessage(InputAETable));
            }

            // Verify chosen input Admin Area feature classes have the expected fields

            gpParam = paramvalues.get_Element(InputAdminAreaFeatureClasses) as IGPParameter;
            IGPMultiValue multiValue = m_gpUtils.UnpackGPValue(gpParam) as IGPMultiValue;
            for (int i = 0; i < multiValue.Count; i++)
            {
                tableValue = multiValue.get_Value(i);
                if (!tableValue.IsEmpty())
                {
                    IDETable inputTable = m_gpUtils.DecodeDETable(tableValue);
                    CheckForTableFields(inputTable, AdminAreaFieldNames, AdminAreaFieldTypes, messages.GetMessage(InputAdminAreaFeatureClasses));
                }
            }

            // Verify chosen input TA table has the expected fields

            gpParam = paramvalues.get_Element(InputTATable) as IGPParameter;
            tableValue = m_gpUtils.UnpackGPValue(gpParam);
            if (!tableValue.IsEmpty())
            {
                IDETable inputTable = m_gpUtils.DecodeDETable(tableValue);
                CheckForTableFields(inputTable, TAFieldNames, TAFieldTypes, messages.GetMessage(InputTATable));
            }

            // Check the input NW feature class and Time Zone ID Field Name parameters together

            gpParam = paramvalues.get_Element(InputNWFeatureClass) as IGPParameter;
            tableValue = m_gpUtils.UnpackGPValue(gpParam);
            IGPParameter fieldNameParam = paramvalues.get_Element(InputTimeZoneIDBaseFieldName) as IGPParameter;
            IGPValue fieldNameValue = m_gpUtils.UnpackGPValue(fieldNameParam);
            if (!tableValue.IsEmpty())
            {
                // Verify chosen input NW feature class has the expected fields

                IDETable inputTable = m_gpUtils.DecodeDETable(tableValue);
                CheckForTableFields(inputTable, NWFieldNames, NWFieldTypes, messages.GetMessage(InputNWFeatureClass));

                // Check to make sure that the Time Zone ID parameter is specified together with the NW feature class

                if (fieldNameValue.IsEmpty())
                {
                    messages.GetMessage(InputTimeZoneIDBaseFieldName).Type = esriGPMessageType.esriGPMessageTypeError;
                    messages.GetMessage(InputTimeZoneIDBaseFieldName).Description = "This parameter must be specified together with the NW feature class.";
                }
                else
                {
                    // Verify chosen input time zone ID field does not exist on the input NW feature class

                    string fieldName = fieldNameValue.GetAsText();
                    if (inputTable.Fields.FindField(fieldName) != -1)
                    {
                        messages.GetMessage(InputTimeZoneIDBaseFieldName).Type = esriGPMessageType.esriGPMessageTypeError;
                        messages.GetMessage(InputTimeZoneIDBaseFieldName).Description = "Field named " + fieldName + " already exists.";
                    }
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

                IGPParameter gpParam = paramvalues.get_Element(InputAETable) as IGPParameter;
                IGPValue inputAETableValue = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(InputAdminAreaFeatureClasses) as IGPParameter;
                var inputAdminAreaFeatureClassesMultiValue = m_gpUtils.UnpackGPValue(gpParam) as IGPMultiValue;
                gpParam = paramvalues.get_Element(OutputFileGDB) as IGPParameter;
                IGPValue outputFileGDBValue = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(InputTATable) as IGPParameter;
                IGPValue inputTATableValue = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(InputNWFeatureClass) as IGPParameter;
                IGPValue inputNWFeatureClassValue = m_gpUtils.UnpackGPValue(gpParam);
                gpParam = paramvalues.get_Element(InputTimeZoneIDBaseFieldName) as IGPParameter;
                IGPValue inputTimeZoneIDBaseFieldNameValue = m_gpUtils.UnpackGPValue(gpParam);

                if (inputTATableValue.IsEmpty() ^ inputNWFeatureClassValue.IsEmpty())
                {
                    messages.AddError(1, "The TA table and NW feature class must be specified together.");
                    return;
                }

                bool processStreetsFC = (!(inputNWFeatureClassValue.IsEmpty()));
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

                // Copy the admin area feature classes

                int numAdminAreaFCs = inputAdminAreaFeatureClassesMultiValue.Count;
                string mergeToolInputs = "";
                for (int i = 0; i < numAdminAreaFCs; i++)
                {
                    AddMessage("Copying the Administrative Area feature classes (" + Convert.ToString(i+1) + " of " + Convert.ToString(numAdminAreaFCs) + ")...", messages, trackcancel);

                    string origAdminFCPath = inputAdminAreaFeatureClassesMultiValue.get_Value(i).GetAsText();
                    FeatureClassToFeatureClass importFCTool = new FeatureClassToFeatureClass();
                    importFCTool.in_features = origAdminFCPath;
                    importFCTool.out_path = outputFileGdbPath;
                    importFCTool.out_name = "Admin" + Convert.ToString(i, System.Globalization.CultureInfo.InvariantCulture);
                    importFCTool.field_mapping = "ID \"ID\" true true false 8 Double 0 0 ,First,#," + origAdminFCPath + ",ID,-1,-1;" +
                                                 "FEATTYP \"FEATTYP\" true true false 2 Short 0 0 ,First,#," + origAdminFCPath + ",FEATTYP,-1,-1;" +
                                                 "ORDER00 \"ORDER00\" true true false 3 Text 0 0 ,First,#," + origAdminFCPath + ",ORDER00,-1,-1;" +
                                                 "NAME \"NAME\" true true false 100 Text 0 0 ,First,#," + origAdminFCPath + ",NAME,-1,-1;" +
                                                 "NAMELC \"NAMELC\" true true false 3 Text 0 0 ,First,#," + origAdminFCPath + ",NAMELC,-1,-1";
                    gp.Execute(importFCTool, trackcancel);

                    mergeToolInputs = mergeToolInputs + outputFileGdbPath + "\\Admin" + Convert.ToString(i, System.Globalization.CultureInfo.InvariantCulture) + ";";
                }
                mergeToolInputs = mergeToolInputs.Remove(mergeToolInputs.Length - 1);

                // Merge the admin area feature classes together into one feature class

                AddMessage("Merging the Administrative Area feature classes...", messages, trackcancel);

                string adminFCPath = outputFileGdbPath + "\\AdminFC";

                Merge mergeTool = new Merge();
                mergeTool.inputs = mergeToolInputs;
                mergeTool.output = adminFCPath;
                gp.Execute(mergeTool, trackcancel);

                Delete deleteTool = null;
                for (int i = 0; i < numAdminAreaFCs; i++)
                {
                    deleteTool = new Delete();
                    deleteTool.in_data = outputFileGdbPath + "\\Admin" + Convert.ToString(i, System.Globalization.CultureInfo.InvariantCulture);
                    gp.Execute(deleteTool, trackcancel);
                }

                // Extract the time zone information and index it

                AddMessage("Extracting the time zone information...", messages, trackcancel);

                string tzTablePath = outputFileGdbPath + "\\TZ";
                TableSelect tableSelectTool = new TableSelect();
                tableSelectTool.in_table = inputAETableValue.GetAsText();
                tableSelectTool.out_table = tzTablePath;
                tableSelectTool.where_clause = "ATTTYP = 'TZ'";
                gp.Execute(tableSelectTool, trackcancel);

                AddIndex addIndexTool = new AddIndex();
                addIndexTool.in_table = tzTablePath;
                addIndexTool.fields = "ID";
                addIndexTool.index_name = "ID";
                gp.Execute(addIndexTool, trackcancel);

                // Add the UTCOffset field and calculate it

                AddField addFieldTool = new AddField();
                addFieldTool.in_table = adminFCPath;
                addFieldTool.field_name = "UTCOffset";
                addFieldTool.field_type = "SHORT";
                gp.Execute(addFieldTool, trackcancel);

                MakeFeatureLayer makeFeatureLayerTool = new MakeFeatureLayer();
                makeFeatureLayerTool.in_features = adminFCPath;
                makeFeatureLayerTool.out_layer = "AdminFC_Layer";
                gp.Execute(makeFeatureLayerTool, trackcancel);

                AddJoin addJoinTool = new AddJoin();
                addJoinTool.in_layer_or_view = "AdminFC_Layer";
                addJoinTool.in_field = "ID";
                addJoinTool.join_table = tzTablePath;
                addJoinTool.join_field = "ID";
                addJoinTool.join_type = "KEEP_COMMON";
                gp.Execute(addJoinTool, trackcancel);

                AddMessage("Calculating the UTCOffset information on the Administrative Area feature class...", messages, trackcancel);

                CalculateField calcFieldTool = new CalculateField();
                calcFieldTool.in_table = "AdminFC_Layer";
                calcFieldTool.field = "AdminFC.UTCOffset";
                calcFieldTool.code_block = "u = Null\ns = Trim([TZ.ATTVALUE])\nIf Not IsNull(s) Then\n" + 
                                           "  sign = 1\n  If Left(s, 1) = \"-\" Then sign = -1\n" +
                                           "  u = sign * ( (60 * Abs(CInt(Mid(s, 1, Len(s) - 3)))) + CInt(Right(s, 2)) )\n" +
                                           "End If";
                calcFieldTool.expression = "u";
                calcFieldTool.expression_type = "VB";
                gp.Execute(calcFieldTool, trackcancel);

                RemoveJoin removeJoinTool = new RemoveJoin();
                removeJoinTool.in_layer_or_view = "AdminFC_Layer";
                removeJoinTool.join_name = "TZ";
                gp.Execute(removeJoinTool, trackcancel);

                deleteTool = new Delete();
                deleteTool.in_data = "AdminFC_Layer";
                gp.Execute(deleteTool, trackcancel);
                deleteTool = new Delete();
                deleteTool.in_data = tzTablePath;
                gp.Execute(deleteTool, trackcancel);

                // Extract out only the admin areas that have time zone information

                AddMessage("Extracting Administrative Areas with time zone information...", messages, trackcancel);

                string adminTZFCPath = outputFileGdbPath + "\\AdminFCwTZ";
                Select selectTool = new Select();
                selectTool.in_features = adminFCPath;
                selectTool.out_feature_class = adminTZFCPath;
                selectTool.where_clause = "NOT UTCOffset IS NULL";
                gp.Execute(selectTool, trackcancel);

                // Extract the daylight saving time information and index it

                AddMessage("Extracting the daylight saving time information...", messages, trackcancel);

                string suTablePath = outputFileGdbPath + "\\SU";
                tableSelectTool = new TableSelect();
                tableSelectTool.in_table = inputAETableValue.GetAsText();
                tableSelectTool.out_table = suTablePath;
                tableSelectTool.where_clause = "ATTTYP = 'SU'";
                gp.Execute(tableSelectTool, trackcancel);

                addIndexTool = new AddIndex();
                addIndexTool.in_table = suTablePath;
                addIndexTool.fields = "ID";
                addIndexTool.index_name = "ID";
                gp.Execute(addIndexTool, trackcancel);

                // Add the daylight saving field and calculate it

                addFieldTool = new AddField();
                addFieldTool.in_table = adminTZFCPath;
                addFieldTool.field_name = "DST";
                addFieldTool.field_type = "SHORT";
                gp.Execute(addFieldTool, trackcancel);

                makeFeatureLayerTool = new MakeFeatureLayer();
                makeFeatureLayerTool.in_features = adminTZFCPath;
                makeFeatureLayerTool.out_layer = "AdminFCwTZ_Layer";
                gp.Execute(makeFeatureLayerTool, trackcancel);

                addJoinTool = new AddJoin();
                addJoinTool.in_layer_or_view = "AdminFCwTZ_Layer";
                addJoinTool.in_field = "ID";
                addJoinTool.join_table = suTablePath;
                addJoinTool.join_field = "ID";
                addJoinTool.join_type = "KEEP_ALL";
                gp.Execute(addJoinTool, trackcancel);

                AddMessage("Copying the DST information to the Administrative Area feature class...", messages, trackcancel);

                calcFieldTool = new CalculateField();
                calcFieldTool.in_table = "AdminFCwTZ_Layer";
                calcFieldTool.field = "AdminFCwTZ.DST";
                calcFieldTool.code_block = "s = 0\nIf Not IsNull( [SU.ATTVALUE] ) Then s = CInt( [SU.ATTVALUE] )";
                calcFieldTool.expression = "s";
                calcFieldTool.expression_type = "VB";
                gp.Execute(calcFieldTool, trackcancel);

                removeJoinTool = new RemoveJoin();
                removeJoinTool.in_layer_or_view = "AdminFCwTZ_Layer";
                removeJoinTool.join_name = "SU";
                gp.Execute(removeJoinTool, trackcancel);

                deleteTool = new Delete();
                deleteTool.in_data = "AdminFCwTZ_Layer";
                gp.Execute(deleteTool, trackcancel);
                deleteTool = new Delete();
                deleteTool.in_data = suTablePath;
                gp.Execute(deleteTool, trackcancel);

                // Create and calculate the sortable MSTIMEZONE field

                addFieldTool = new AddField();
                addFieldTool.in_table = adminTZFCPath;
                addFieldTool.field_name = "SortableMSTIMEZONE";
                addFieldTool.field_type = "TEXT";
                addFieldTool.field_length = 60;
                gp.Execute(addFieldTool, trackcancel);

                AddMessage("Calculating the time zones...", messages, trackcancel);

                calcFieldTool = new CalculateField();
                calcFieldTool.in_table = adminTZFCPath;
                calcFieldTool.field = "SortableMSTIMEZONE";
                calcFieldTool.code_block = TimeZoneUtilities.MakeSortableMSTIMEZONECode("ORDER00");
                calcFieldTool.expression = "z";
                calcFieldTool.expression_type = "VB";
                gp.Execute(calcFieldTool, trackcancel);

                Rename renameTool = new Rename();
                renameTool.in_data = adminTZFCPath;
                renameTool.out_data = adminTZFCPath + "wNulls";
                gp.Execute(renameTool, trackcancel);

                selectTool = new Select();
                selectTool.in_features = adminTZFCPath + "wNulls";
                selectTool.out_feature_class = adminTZFCPath;
                selectTool.where_clause = "NOT SortableMSTIMEZONE IS NULL";
                gp.Execute(selectTool, trackcancel);

                deleteTool = new Delete();
                deleteTool.in_data = adminTZFCPath + "wNulls";
                gp.Execute(deleteTool, trackcancel);

                // Dissolve the time zone polygons together

                AddMessage("Dissolving the time zones...", messages, trackcancel);

                string timeZoneFCPath = outputFileGdbPath + "\\" + TimeZoneFCName;
                Dissolve dissolveTool = new Dissolve();
                dissolveTool.in_features = adminTZFCPath;
                dissolveTool.out_feature_class = timeZoneFCPath;
                dissolveTool.dissolve_field = "SortableMSTIMEZONE";
                dissolveTool.multi_part = "MULTI_PART";
                gp.Execute(dissolveTool, trackcancel);

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

                DeleteField deleteFieldTool = new DeleteField();
                deleteFieldTool.in_table = timeZoneFCPath;
                deleteFieldTool.drop_field = "SortableMSTIMEZONE";
                gp.Execute(deleteFieldTool, trackcancel);

                if (processStreetsFC)
                {
                    // Create the network dataset time zone table

                    AddMessage("Creating the time zones table...", messages, trackcancel);

                    TableToTable importTableTool = new TableToTable();
                    importTableTool.in_rows = timeZoneFCPath;
                    importTableTool.out_path = outputFileGdbPath;
                    importTableTool.out_name = TimeZonesTableName;
                    importTableTool.field_mapping = "MSTIMEZONE \"MSTIMEZONE\" true true false 50 Text 0 0 ,First,#," +
                                                    timeZoneFCPath + ",MSTIMEZONE,-1,-1";
                    gp.Execute(importTableTool, trackcancel);

                    // Import the NW feature class to the file geodatabase

                    AddMessage("Copying the NW feature class to the geodatabase...", messages, trackcancel);

                    FeatureClassToFeatureClass importFCTool = new FeatureClassToFeatureClass();
                    importFCTool.in_features = inputNWFeatureClassValue.GetAsText();
                    importFCTool.out_path = outputFileGdbPath;
                    importFCTool.out_name = "nw";
                    gp.Execute(importFCTool, trackcancel);

                    string pathToLocalNW = outputFileGdbPath + "\\nw";
                    
                    // Create Join polygon feature class

                    AddMessage("Creating the join polygon feature class...", messages, trackcancel);

                    string joinPolygonFCPath = outputFileGdbPath + "\\JoinPolygonFC";

                    MultipartToSinglepart multipartToSinglepartTool = new MultipartToSinglepart();
                    multipartToSinglepartTool.in_features = timeZoneFCPath;
                    multipartToSinglepartTool.out_feature_class = joinPolygonFCPath;
                    gp.Execute(multipartToSinglepartTool, trackcancel);

                    // Add and calculate the time zone ID fields to the join polygons

                    addFieldTool = new AddField();
                    addFieldTool.in_table = joinPolygonFCPath;
                    addFieldTool.field_type = "SHORT";
                    addFieldTool.field_name = "FT_" + timeZoneIDBaseFieldName;
                    gp.Execute(addFieldTool, trackcancel);
                    addFieldTool.field_name = "TF_" + timeZoneIDBaseFieldName;
                    gp.Execute(addFieldTool, trackcancel);

                    AddMessage("Calculating the FT_" + timeZoneIDBaseFieldName + " field...", messages, trackcancel);

                    calcFieldTool = new CalculateField();
                    calcFieldTool.in_table = joinPolygonFCPath;
                    calcFieldTool.field = "FT_" + timeZoneIDBaseFieldName;
                    calcFieldTool.expression = "[ORIG_FID]";
                    calcFieldTool.expression_type = "VB";
                    gp.Execute(calcFieldTool, trackcancel);

                    AddMessage("Calculating the TF_" + timeZoneIDBaseFieldName + " field...", messages, trackcancel);

                    calcFieldTool = new CalculateField();
                    calcFieldTool.in_table = joinPolygonFCPath;
                    calcFieldTool.field = "TF_" + timeZoneIDBaseFieldName;
                    calcFieldTool.expression = "[ORIG_FID]";
                    calcFieldTool.expression_type = "VB";
                    gp.Execute(calcFieldTool, trackcancel);

                    // Delete the MSTIMEZONE and ORIG_FID fields from the join polygon FC

                    deleteFieldTool = new DeleteField();
                    deleteFieldTool.in_table = joinPolygonFCPath;
                    deleteFieldTool.drop_field = "MSTIMEZONE;ORIG_FID";
                    gp.Execute(deleteFieldTool, trackcancel);

                    // Perform a spatial join between the Streets and the join polygons

                    AddMessage("Creating Streets feature class with time zone ID fields...", messages, trackcancel);

                    string outputStreetsFCPath = outputFileGdbPath + "\\" + StreetsFCName;
                    SpatialJoin spatialJoinTool = new SpatialJoin();
                    spatialJoinTool.target_features = pathToLocalNW;
                    spatialJoinTool.join_features = joinPolygonFCPath;
                    spatialJoinTool.out_feature_class = outputStreetsFCPath;
                    spatialJoinTool.match_option = "IS_WITHIN";
                    gp.Execute(spatialJoinTool, trackcancel);

                    // Delete the extraneous fields

                    deleteFieldTool = new DeleteField();
                    deleteFieldTool.in_table = outputStreetsFCPath;
                    deleteFieldTool.drop_field = "Join_Count;TARGET_FID;Shape_Length_1";
                    gp.Execute(deleteFieldTool, trackcancel);

                    // Delete the temporary NW and Join feature classes

                    deleteTool = new Delete();
                    deleteTool.in_data = pathToLocalNW;
                    gp.Execute(deleteTool, trackcancel);
                    deleteTool = new Delete();
                    deleteTool.in_data = joinPolygonFCPath;
                    gp.Execute(deleteTool, trackcancel);

                    // Extract the drive side information and index it

                    AddMessage("Extracting the drive side information...", messages, trackcancel);

                    string driveSideTablePath = outputFileGdbPath + "\\DriveSide";
                    tableSelectTool = new TableSelect();
                    tableSelectTool.in_table = inputAETableValue.GetAsText();
                    tableSelectTool.out_table = driveSideTablePath;
                    tableSelectTool.where_clause = "ATTTYP = '3D'";
                    gp.Execute(tableSelectTool, trackcancel);

                    addIndexTool = new AddIndex();
                    addIndexTool.in_table = driveSideTablePath;
                    addIndexTool.fields = "ID";
                    addIndexTool.index_name = "ID";
                    gp.Execute(addIndexTool, trackcancel);

                    // Add the DriveSide field and calculate it

                    addFieldTool = new AddField();
                    addFieldTool.in_table = adminFCPath;
                    addFieldTool.field_name = "DriveSide";
                    addFieldTool.field_type = "SHORT";
                    gp.Execute(addFieldTool, trackcancel);

                    makeFeatureLayerTool = new MakeFeatureLayer();
                    makeFeatureLayerTool.in_features = adminFCPath;
                    makeFeatureLayerTool.out_layer = "AdminFC_Layer";
                    gp.Execute(makeFeatureLayerTool, trackcancel);

                    addJoinTool = new AddJoin();
                    addJoinTool.in_layer_or_view = "AdminFC_Layer";
                    addJoinTool.in_field = "ID";
                    addJoinTool.join_table = driveSideTablePath;
                    addJoinTool.join_field = "ID";
                    addJoinTool.join_type = "KEEP_COMMON";
                    gp.Execute(addJoinTool, trackcancel);

                    AddMessage("Copying the drive side information to the Administrative Area feature class...", messages, trackcancel);

                    calcFieldTool = new CalculateField();
                    calcFieldTool.in_table = "AdminFC_Layer";
                    calcFieldTool.field = "AdminFC.DriveSide";
                    calcFieldTool.expression = "CInt( [DriveSide.ATTVALUE] )";
                    calcFieldTool.expression_type = "VB";
                    gp.Execute(calcFieldTool, trackcancel);

                    removeJoinTool = new RemoveJoin();
                    removeJoinTool.in_layer_or_view = "AdminFC_Layer";
                    removeJoinTool.join_name = "DriveSide";
                    gp.Execute(removeJoinTool, trackcancel);

                    deleteTool = new Delete();
                    deleteTool.in_data = "AdminFC_Layer";
                    gp.Execute(deleteTool, trackcancel);
                    deleteTool = new Delete();
                    deleteTool.in_data = driveSideTablePath;
                    gp.Execute(deleteTool, trackcancel);

                    // Extract out only the admin areas that have drive side information and index the ORDER00 field

                    AddMessage("Extracting Administrative Areas with drive side information...", messages, trackcancel);

                    string adminFCwDriveSidePath = outputFileGdbPath + "\\AdminFCwDriveSide";
                    selectTool = new Select();
                    selectTool.in_features = adminFCPath;
                    selectTool.out_feature_class = adminFCwDriveSidePath;
                    selectTool.where_clause = "NOT DriveSide IS NULL";
                    gp.Execute(selectTool, trackcancel);

                    addIndexTool = new AddIndex();
                    addIndexTool.in_table = adminFCwDriveSidePath;
                    addIndexTool.fields = "ORDER00";
                    addIndexTool.index_name = "ORDER00";
                    gp.Execute(addIndexTool, trackcancel);

                    // Add the DriveSide field to the AdminFCwTZ feature class and calculate it.

                    addFieldTool = new AddField();
                    addFieldTool.in_table = adminTZFCPath;
                    addFieldTool.field_name = "DriveSide";
                    addFieldTool.field_type = "SHORT";
                    gp.Execute(addFieldTool, trackcancel);

                    makeFeatureLayerTool = new MakeFeatureLayer();
                    makeFeatureLayerTool.in_features = adminTZFCPath;
                    makeFeatureLayerTool.out_layer = "AdminFCwTZ_Layer";
                    gp.Execute(makeFeatureLayerTool, trackcancel);

                    addJoinTool = new AddJoin();
                    addJoinTool.in_layer_or_view = "AdminFCwTZ_Layer";
                    addJoinTool.in_field = "ORDER00";
                    addJoinTool.join_table = adminFCwDriveSidePath;
                    addJoinTool.join_field = "ORDER00";
                    addJoinTool.join_type = "KEEP_COMMON";
                    gp.Execute(addJoinTool, trackcancel);

                    AddMessage("Calculating the DriveSide field on the AdminFCwTZ feature class...", messages, trackcancel);

                    calcFieldTool = new CalculateField();
                    calcFieldTool.in_table = "AdminFCwTZ_Layer";
                    calcFieldTool.field = "AdminFCwTZ.DriveSide";
                    calcFieldTool.expression = "[AdminFCwDriveSide.DriveSide]";
                    calcFieldTool.expression_type = "VB";
                    gp.Execute(calcFieldTool, trackcancel);

                    removeJoinTool = new RemoveJoin();
                    removeJoinTool.in_layer_or_view = "AdminFCwTZ_Layer";
                    removeJoinTool.join_name = "AdminFCwDriveSide";
                    gp.Execute(removeJoinTool, trackcancel);

                    deleteTool = new Delete();
                    deleteTool.in_data = "AdminFCwTZ_Layer";
                    gp.Execute(deleteTool, trackcancel);

                    deleteTool = new Delete();
                    deleteTool.in_data = adminFCwDriveSidePath;
                    gp.Execute(deleteTool, trackcancel);

                    // Extract the information for boundary roads

                    AddMessage("Extracting the information for boundary roads...", messages, trackcancel);

                    string taTablePath = outputFileGdbPath + "\\TA";
                    tableSelectTool = new TableSelect();
                    tableSelectTool.in_table = inputTATableValue.GetAsText();
                    tableSelectTool.out_table = taTablePath;
                    tableSelectTool.where_clause = "ARETYP <= 1120 AND SOL > 0";
                    gp.Execute(tableSelectTool, trackcancel);

                    // Join the boundary road information with the AdminFCwTZ feature class to
                    // create the FT_BoundaryTimeZones and TF_BoundaryTimeZones join tables
                    
                    addIndexTool = new AddIndex();
                    addIndexTool.in_table = adminTZFCPath;
                    addIndexTool.fields = "ID";
                    addIndexTool.index_name = "ID";
                    gp.Execute(addIndexTool, trackcancel);

                    MakeTableView makeTableViewTool = new MakeTableView();
                    makeTableViewTool.in_table = taTablePath;
                    makeTableViewTool.out_view = "TA_View";
                    gp.Execute(makeTableViewTool, trackcancel);

                    addJoinTool = new AddJoin();
                    addJoinTool.in_layer_or_view = "TA_View";
                    addJoinTool.in_field = "AREID";
                    addJoinTool.join_table = adminTZFCPath;
                    addJoinTool.join_field = "ID";
                    addJoinTool.join_type = "KEEP_COMMON";
                    gp.Execute(addJoinTool, trackcancel);

                    AddMessage("Extracting the boundary FT time zones...", messages, trackcancel);

                    importTableTool = new TableToTable();
                    importTableTool.in_rows = "TA_View";
                    importTableTool.out_path = outputFileGdbPath;
                    importTableTool.out_name = "FT_BoundaryTimeZones";
                    importTableTool.where_clause = "TA.SOL = AdminFCwTZ.DriveSide";
                    importTableTool.field_mapping = "ID \"ID\" true true false 8 Double 0 0 ,First,#," + taTablePath + ",TA.ID,-1,-1;" +
                                                    "SortableMSTIMEZONE \"SortableMSTIMEZONE\" true true false 60 Text 0 0 ,First,#," +
                                                    adminTZFCPath + ",AdminFCwTZ.SortableMSTIMEZONE,-1,-1";
                    gp.Execute(importTableTool, trackcancel);

                    string ftBoundaryTimeZonesPath = outputFileGdbPath + "\\FT_BoundaryTimeZones";

                    addIndexTool = new AddIndex();
                    addIndexTool.in_table = ftBoundaryTimeZonesPath;
                    addIndexTool.fields = "ID";
                    addIndexTool.index_name = "ID";
                    gp.Execute(addIndexTool, trackcancel);

                    AddMessage("Extracting the boundary TF time zones...", messages, trackcancel);

                    importTableTool = new TableToTable();
                    importTableTool.in_rows = "TA_View";
                    importTableTool.out_path = outputFileGdbPath;
                    importTableTool.out_name = "TF_BoundaryTimeZones";
                    importTableTool.where_clause = "TA.SOL <> AdminFCwTZ.DriveSide";
                    importTableTool.field_mapping = "ID \"ID\" true true false 8 Double 0 0 ,First,#," + taTablePath + ",TA.ID,-1,-1;" +
                                                    "SortableMSTIMEZONE \"SortableMSTIMEZONE\" true true false 60 Text 0 0 ,First,#," +
                                                    adminTZFCPath + ",AdminFCwTZ.SortableMSTIMEZONE,-1,-1";
                    gp.Execute(importTableTool, trackcancel);

                    string tfBoundaryTimeZonesPath = outputFileGdbPath + "\\TF_BoundaryTimeZones";

                    addIndexTool = new AddIndex();
                    addIndexTool.in_table = tfBoundaryTimeZonesPath;
                    addIndexTool.fields = "ID";
                    addIndexTool.index_name = "ID";
                    gp.Execute(addIndexTool, trackcancel);

                    removeJoinTool = new RemoveJoin();
                    removeJoinTool.in_layer_or_view = "TA_View";
                    removeJoinTool.join_name = "AdminFCwTZ";
                    gp.Execute(removeJoinTool, trackcancel);

                    deleteTool = new Delete();
                    deleteTool.in_data = "TA_View";
                    gp.Execute(deleteTool, trackcancel);
                    deleteTool.in_data = taTablePath;
                    gp.Execute(deleteTool, trackcancel);

                    // Calculate the boundary time zone ID values

                    makeFeatureLayerTool = new MakeFeatureLayer();
                    makeFeatureLayerTool.in_features = outputStreetsFCPath;
                    makeFeatureLayerTool.out_layer = "Streets_Layer";
                    gp.Execute(makeFeatureLayerTool, trackcancel);

                    addJoinTool = new AddJoin();
                    addJoinTool.in_layer_or_view = "Streets_Layer";
                    addJoinTool.in_field = "ID";
                    addJoinTool.join_table = ftBoundaryTimeZonesPath;
                    addJoinTool.join_field = "ID";
                    addJoinTool.join_type = "KEEP_COMMON";
                    gp.Execute(addJoinTool, trackcancel);

                    AddMessage("Calculating the boundary FT time zones...", messages, trackcancel);

                    calcFieldTool = new CalculateField();
                    calcFieldTool.in_table = "Streets_Layer";
                    calcFieldTool.field = StreetsFCName + ".FT_" + timeZoneIDBaseFieldName;
                    calcFieldTool.code_block = TimeZoneUtilities.MakeTimeZoneIDCode(outputFileGdbPath, TimeZonesTableName, "FT_BoundaryTimeZones.SortableMSTIMEZONE");
                    calcFieldTool.expression = "tzID";
                    calcFieldTool.expression_type = "VB";
                    gp.Execute(calcFieldTool, trackcancel);

                    removeJoinTool = new RemoveJoin();
                    removeJoinTool.in_layer_or_view = "Streets_Layer";
                    removeJoinTool.join_name = "FT_BoundaryTimeZones";
                    gp.Execute(removeJoinTool, trackcancel);

                    addJoinTool = new AddJoin();
                    addJoinTool.in_layer_or_view = "Streets_Layer";
                    addJoinTool.in_field = "ID";
                    addJoinTool.join_table = tfBoundaryTimeZonesPath;
                    addJoinTool.join_field = "ID";
                    addJoinTool.join_type = "KEEP_COMMON";
                    gp.Execute(addJoinTool, trackcancel);

                    AddMessage("Calculating the boundary TF time zones...", messages, trackcancel);

                    calcFieldTool = new CalculateField();
                    calcFieldTool.in_table = "Streets_Layer";
                    calcFieldTool.field = StreetsFCName + ".TF_" + timeZoneIDBaseFieldName;
                    calcFieldTool.code_block = TimeZoneUtilities.MakeTimeZoneIDCode(outputFileGdbPath, TimeZonesTableName, "TF_BoundaryTimeZones.SortableMSTIMEZONE");
                    calcFieldTool.expression = "tzID";
                    calcFieldTool.expression_type = "VB";
                    gp.Execute(calcFieldTool, trackcancel);

                    removeJoinTool = new RemoveJoin();
                    removeJoinTool.in_layer_or_view = "Streets_Layer";
                    removeJoinTool.join_name = "TF_BoundaryTimeZones";
                    gp.Execute(removeJoinTool, trackcancel);

                    deleteTool = new Delete();
                    deleteTool.in_data = "Streets_Layer";
                    gp.Execute(deleteTool, trackcancel);

                    deleteTool = new Delete();
                    deleteTool.in_data = ftBoundaryTimeZonesPath;
                    gp.Execute(deleteTool, trackcancel);
                    deleteTool.in_data = tfBoundaryTimeZonesPath;
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
                deleteTool.in_data = adminFCPath;
                gp.Execute(deleteTool, trackcancel);

                deleteTool = new Delete();
                deleteTool.in_data = adminTZFCPath;
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
                return "Process MultiNet® Time Zones";
            }
        }

        public string MetadataFile
        {
            get
            {
                string filePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                return System.IO.Path.Combine(filePath, "ProcessMultiNetTimeZones_nasample.xml");
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
                return "ProcessMultiNetTimeZones";
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
    }
}
