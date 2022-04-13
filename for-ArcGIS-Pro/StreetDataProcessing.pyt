"""StreetDataProcessing.pyt

Tools to process raw TomTom MultiNet data into a network dataset.

   Copyright 2022 Esri
   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at
       http://www.apache.org/licenses/LICENSE-2.0
   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.'''
"""
import os
import arcpy
import Process_MultiNet


class Toolbox(object):
    def __init__(self):
        """Define the toolbox."""
        self.label = "Street Data Processing Tools"
        self.alias = "SDPT"

        # List of tool classes associated with this toolbox
        self.tools = [ProcessMultiNet]


class ProcessMultiNet(object):
    def __init__(self):
        """Define the tool."""
        self.label = "Process MultiNet"
        self.description = "Process TomTom MultiNet data for a network dataset"
        self.canRunInBackground = True

        self.param_idx_trf_bool = 11  # Parameter index of historical traffic boolean
        self.required_trf_idxs = [12, 13]  # Parameter indices of conditionally required traffic tables
        self.all_trf_idxs = [12, 13, 14]  # Parameter indices of all historical traffic tables
        self.param_idx_logistics_bool = 15  # Parameter index of MultiNet Logistics boolean
        self.required_logistics_idxs = [16, 17]
        self.param_idx_tz_type = 18
        self.param_idx_tz_name = 19
        self.param_idx_tz_table = 20
        self.param_idx_tz_ft_field = 21
        self.param_idx_tz_tf_field = 22

    def getParameterInfo(self):
        """Define parameter definitions"""
        param_in_network_geometry = arcpy.Parameter(
            displayName="Input Network Geometry (NW) Feature Class",
            name="in_network_geometry_fc",
            datatype="GPFeatureLayer",
            parameterType="Required",
            direction="Input"
        )

        param_in_maneuvers_geometry = arcpy.Parameter(
            displayName="Input Maneuvers Geometry (MN) Feature Class",
            name="in_maneuvers_geometry_fc",
            datatype="GPFeatureLayer",
            parameterType="Required",
            direction="Input"
        )

        param_in_maneuver_path_index = arcpy.Parameter(
            displayName="Input Maneuver Path Index (MP) Table",
            name="in_maneuver_path_index_table",
            datatype="GPTableView",
            parameterType="Required",
            direction="Input"
        )

        param_in_sign_information = arcpy.Parameter(
            displayName="Input Sign Information (SI) Table",
            name="in_sign_information_table",
            datatype="GPTableView",
            parameterType="Required",
            direction="Input"
        )

        param_in_sign_path = arcpy.Parameter(
            displayName="Input Sign Path (SP) Table",
            name="in_sign_path_table",
            datatype="GPTableView",
            parameterType="Required",
            direction="Input"
        )

        param_in_restrictions = arcpy.Parameter(
            displayName="Input Restrictions (RS) Table",
            name="in_restrictions_table",
            datatype="GPTableView",
            parameterType="Required",
            direction="Input"
        )

        param_out_folder = arcpy.Parameter(
            displayName="Output Folder",
            name="output_folder",
            datatype="DEFolder",
            parameterType="Required",
            direction="Input"
        )

        param_out_gdb_name = arcpy.Parameter(
            displayName="Output Geodatabase Name",
            name="output_geodatabase_name",
            datatype="GPString",
            parameterType="Required",
            direction="Input"
        )

        param_build_network = arcpy.Parameter(
            displayName="Build the network dataset",
            name="build_network",
            datatype="GPBoolean",
            parameterType="Optional",
            direction="Input"
        )
        param_build_network.value = True

        param_unit_type = arcpy.Parameter(
            displayName="Unit Type",
            name="unit_type",
            datatype="GPString",
            parameterType="Required",
            direction="Input"
        )
        param_unit_type.filter.list = ["Imperial", "Metric"]
        param_unit_type.value = "Imperial"

        param_in_ltr = arcpy.Parameter(
            displayName="Input Logistics Truck Routes (LTR) Table",
            name="in_logistics_truck_routes_table",
            datatype="GPTableView",
            parameterType="Optional",
            direction="Input"
        )

        param_include_traffic = arcpy.Parameter(
            displayName="Include historical traffic",
            name="include_historical_traffic",
            datatype="GPBoolean",
            parameterType="Optional",
            direction="Input",
            category="Traffic"
        )
        param_include_traffic.value = False

        param_in_network_profile_link = arcpy.Parameter(
            displayName="Input Network Profile Link (HSNP) Table",
            name="in_network_profile_link_table",
            datatype="GPTableView",
            parameterType="Optional",
            direction="Input",
            category="Traffic"
        )

        param_in_historical_speed_profiles = arcpy.Parameter(
            displayName="Input Historical Speed Profiles (HSPR) Table",
            name="in_historical_speed_profiles_table",
            datatype="GPTableView",
            parameterType="Optional",
            direction="Input",
            category="Traffic"
        )

        param_in_rds_tmc_info = arcpy.Parameter(
            displayName="Input RDS-TMC Information (RD) Table",
            name="in_rds_tmc_info_table",
            datatype="GPTableView",
            parameterType="Optional",
            direction="Input",
            category="Traffic"
        )

        param_include_multinet = arcpy.Parameter(
            displayName="Include MultiNet Logistics restriction data",
            name="include_multinet_logistics_restriction_data",
            datatype="GPBoolean",
            parameterType="Optional",
            direction="Input",
            category="MultiNet Logistics"
        )
        param_include_multinet.value = False

        param_in_logistics_lrs = arcpy.Parameter(
            displayName="Input Logistics Restrictions (LRS) Table",
            name="in_logistics_restrictions_table",
            datatype="GPTableView",
            parameterType="Optional",
            direction="Input",
            category="MultiNet Logistics"
        )

        param_in_logistics_lvc = arcpy.Parameter(
            displayName="Input Logistics Vehicle Characteristics (LVC) Table",
            name="in_logistics_vehicle_characteristics_table",
            datatype="GPTableView",
            parameterType="Optional",
            direction="Input",
            category="MultiNet Logistics"
        )

        param_time_zone_type = arcpy.Parameter(
            displayName="Time Zone Type",
            name="time_zone_type",
            datatype="GPString",
            parameterType="Optional",
            direction="Input",
            category="Time Zone"
        )
        param_time_zone_type.filter.list = ["None", "Single time zone", "Use time zone table"]
        param_time_zone_type.value = "None"

        param_time_zone_name = arcpy.Parameter(
            displayName="Time Zone Name",
            name="time_zone_name",
            datatype="GPString",
            parameterType="Optional",
            direction="Input",
            category="Time Zone"
        )

        param_in_time_zone_table = arcpy.Parameter(
            displayName="Input Time Zone Table",
            name="in_time_zone_table",
            datatype="GPTableView",
            parameterType="Optional",
            direction="Input",
            category="Time Zone"
        )

        param_in_ft_time_zone_field_name = arcpy.Parameter(
            displayName="Input FT Time Zone ID Field Name",
            name="in_ft_time_zone_field_name",
            datatype="Field",
            parameterType="Optional",
            direction="Input",
            category="Time Zone"
        )
        param_in_ft_time_zone_field_name.parameterDependencies = [param_in_network_geometry.name]
        param_in_ft_time_zone_field_name.filter.list = ["Short", "Long"]

        param_in_tf_time_zone_field_name = arcpy.Parameter(
            displayName="Input TF Time Zone ID Field Name",
            name="in_tf_time_zone_field_name",
            datatype="Field",
            parameterType="Optional",
            direction="Input",
            category="Time Zone"
        )
        param_in_tf_time_zone_field_name.parameterDependencies = [param_in_network_geometry.name]
        param_in_tf_time_zone_field_name.filter.list = ["Short", "Long"]

        param_out_network = arcpy.Parameter(
            displayName="Output Network Dataset",
            name="Output_Network",
            datatype="DENetworkDataset",
            parameterType="Derived",
            direction="Output"
        )

        params = [
            param_in_network_geometry,  # 0
            param_in_maneuvers_geometry,  # 1
            param_in_maneuver_path_index,  # 2
            param_in_sign_information,  # 3
            param_in_sign_path,  # 4
            param_in_restrictions,  # 5
            param_out_folder,  # 6
            param_out_gdb_name,  # 7
            param_unit_type,  # 8
            param_build_network,  # 9
            param_in_ltr,  # 10
            param_include_traffic,  # 11
            param_in_network_profile_link,  # 12
            param_in_historical_speed_profiles,  # 13
            param_in_rds_tmc_info,  # 14
            param_include_multinet,  # 15
            param_in_logistics_lrs,  # 16
            param_in_logistics_lvc,  # 17
            param_time_zone_type,  # 18
            param_time_zone_name,  # 19
            param_in_time_zone_table,  # 20
            param_in_ft_time_zone_field_name,  # 21
            param_in_tf_time_zone_field_name,  # 22
            param_out_network  # 23 Derived output
        ]

        return params

    def isLicensed(self):
        """Set whether tool is licensed to execute."""
        return True

    def updateParameters(self, parameters):
        """Modify the values and properties of parameters before internal
        validation is performed.  This method is called whenever a parameter
        has been changed."""
        # Add .gdb extension to gdb name
        param_out_gdb_name = parameters[7]
        if not param_out_gdb_name.hasBeenValidated and param_out_gdb_name.altered and param_out_gdb_name.valueAsText:
            gdb_name = param_out_gdb_name.valueAsText
            if not gdb_name.lower().endswith(".gdb"):
                gdb_name += ".gdb"
            param_out_gdb_name.value = gdb_name

        # Enable and disable historical traffic parameters based on boolean
        if not parameters[self.param_idx_trf_bool].hasBeenValidated:
            use_traffic = parameters[self.param_idx_trf_bool].value
            for idx in self.all_trf_idxs:
                parameters[idx].enabled = use_traffic
                if not use_traffic:
                    parameters[idx].value = None

        # Enable and disable MultiNet Logistics parameters based on boolean
        if not parameters[self.param_idx_logistics_bool].hasBeenValidated:
            include_multinet = parameters[self.param_idx_logistics_bool].value
            for idx in self.required_logistics_idxs:
                parameters[idx].enabled = include_multinet
                if not include_multinet:
                    parameters[idx].value = None

        # Enable and disable time zone parameters depending on value of time zone type parameter
        if not parameters[self.param_idx_tz_type].hasBeenValidated:
            time_zone_type = param_to_time_zone_enum(parameters[self.param_idx_tz_type].valueAsText)
            if time_zone_type == Process_MultiNet.TimeZoneType.NoTimeZone:
                parameters[self.param_idx_tz_name].enabled = False
                parameters[self.param_idx_tz_name].value = None
                parameters[self.param_idx_tz_table].enabled = False
                parameters[self.param_idx_tz_table].value = None
                parameters[self.param_idx_tz_ft_field].enabled = False
                parameters[self.param_idx_tz_ft_field].value = None
                parameters[self.param_idx_tz_tf_field].enabled = False
                parameters[self.param_idx_tz_tf_field].value = None
            elif time_zone_type == Process_MultiNet.TimeZoneType.Single:
                parameters[self.param_idx_tz_name].enabled = True
                parameters[self.param_idx_tz_table].enabled = False
                parameters[self.param_idx_tz_table].value = None
                parameters[self.param_idx_tz_ft_field].enabled = False
                parameters[self.param_idx_tz_ft_field].value = None
                parameters[self.param_idx_tz_tf_field].enabled = False
                parameters[self.param_idx_tz_tf_field].value = None
            elif time_zone_type == Process_MultiNet.TimeZoneType.Table:
                parameters[self.param_idx_tz_name].enabled = False
                parameters[self.param_idx_tz_name].value = None
                parameters[self.param_idx_tz_table].enabled = True
                parameters[self.param_idx_tz_ft_field].enabled = True
                parameters[self.param_idx_tz_tf_field].enabled = True
        return

    def updateMessages(self, parameters):
        """Modify the messages created by internal validation for each tool
        parameter.  This method is called after internal validation."""
        # Make sure geodatabase doesn't already exist.
        param_out_folder = parameters[6]
        param_out_gdb_name = parameters[7]
        if param_out_gdb_name.altered and param_out_folder.altered and \
                param_out_gdb_name.valueAsText and param_out_folder.valueAsText:
            out_gdb = os.path.join(param_out_folder.valueAsText, param_out_gdb_name.valueAsText)
            if os.path.exists(out_gdb):
                param_out_gdb_name.setErrorMessage("Output geodatabase already exists.")

        # Make historical traffic table parameters required only if boolean is true
        if parameters[self.param_idx_trf_bool].value:
            # Get required parameters for this analysis type
            for param_idx in self.required_trf_idxs:
                if not parameters[param_idx].valueAsText:
                    # The 735 error code doesn't display an actual error but displays the little red star to
                    # indicate that the parameter is required.
                    parameters[param_idx].setIDMessage("Error", 735, parameters[param_idx].displayName)
                else:
                    # Not required for this analysis type
                    parameters[param_idx].clearMessage()
        else:
            # Clear out requirements if the analysis type is unset
            for param_idx in self.required_trf_idxs:
                parameters[param_idx].clearMessage()

        # Make MultiNet Logistics table parameters required only if boolean is true
        if parameters[self.param_idx_logistics_bool].value:
            # Get required parameters for this analysis type
            for param_idx in self.required_logistics_idxs:
                if not parameters[param_idx].valueAsText:
                    # The 735 error code doesn't display an actual error but displays the little red star to
                    # indicate that the parameter is required.
                    parameters[param_idx].setIDMessage("Error", 735, parameters[param_idx].displayName)
                else:
                    # Not required for this analysis type
                    parameters[param_idx].clearMessage()
        else:
            # Clear out requirements if the analysis type is unset
            for param_idx in self.required_logistics_idxs:
                parameters[param_idx].clearMessage()

        # Make time zone parameters required depending on selected type
        time_zone_type = param_to_time_zone_enum(parameters[self.param_idx_tz_type].valueAsText)
        if time_zone_type == Process_MultiNet.TimeZoneType.NoTimeZone:
            parameters[self.param_idx_tz_name].clearMessage()
            parameters[self.param_idx_tz_table].clearMessage()
            parameters[self.param_idx_tz_ft_field].clearMessage()
            parameters[self.param_idx_tz_tf_field].clearMessage()
        elif time_zone_type == Process_MultiNet.TimeZoneType.Single:
            if not parameters[self.param_idx_tz_name].valueAsText:
                # The 735 error code doesn't display an actual error but displays the little red star to
                # indicate that the parameter is required.
                parameters[self.param_idx_tz_name].setIDMessage(
                    "Error", 735, parameters[self.param_idx_tz_name].displayName)
            parameters[self.param_idx_tz_table].clearMessage()
            parameters[self.param_idx_tz_ft_field].clearMessage()
            parameters[self.param_idx_tz_tf_field].clearMessage()
        elif time_zone_type == Process_MultiNet.TimeZoneType.Table:
            parameters[self.param_idx_tz_name].clearMessage()
            if not parameters[self.param_idx_tz_table].valueAsText:
                # The 735 error code doesn't display an actual error but displays the little red star to
                # indicate that the parameter is required.
                parameters[self.param_idx_tz_table].setIDMessage(
                    "Error", 735, parameters[self.param_idx_tz_table].displayName)
            if not parameters[self.param_idx_tz_ft_field].valueAsText:
                # The 735 error code doesn't display an actual error but displays the little red star to
                # indicate that the parameter is required.
                parameters[self.param_idx_tz_ft_field].setIDMessage(
                    "Error", 735, parameters[self.param_idx_tz_ft_field].displayName)
            if not parameters[self.param_idx_tz_tf_field].valueAsText:
                # The 735 error code doesn't display an actual error but displays the little red star to
                # indicate that the parameter is required.
                parameters[self.param_idx_tz_tf_field].setIDMessage(
                    "Error", 735, parameters[self.param_idx_tz_tf_field].displayName)

        return

    def execute(self, parameters, messages):
        """The source code of the tool."""
        in_multinet = Process_MultiNet.MultiNetInputData(
            parameters[0].value,
            parameters[1].value,
            parameters[2].value,
            parameters[3].value,
            parameters[4].value,
            parameters[5].value,
            parameters[12].value,
            parameters[13].value,
            parameters[14].value,
            parameters[10].value,
            parameters[16].value,
            parameters[17].value,
        )
        out_folder = parameters[6].valueAsText
        gdb_name = parameters[7].valueAsText
        unit_type = param_to_unit_type_enum(parameters[8].valueAsText)
        build_network = parameters[9].value
        include_historical_traffic = parameters[self.param_idx_trf_bool].value
        include_logistics = parameters[self.param_idx_logistics_bool].value
        time_zone_type = param_to_time_zone_enum(parameters[self.param_idx_tz_type].valueAsText)
        time_zone_name = parameters[self.param_idx_tz_name].valueAsText
        time_zone_table = parameters[self.param_idx_tz_table].valueAsText
        time_zone_ft_field = parameters[self.param_idx_tz_ft_field].valueAsText
        time_zone_tf_field = parameters[self.param_idx_tz_tf_field].valueAsText
        processor = Process_MultiNet.MultiNetProcessor(
            out_folder, gdb_name, in_multinet, unit_type, include_historical_traffic, include_logistics,
            time_zone_type, time_zone_name, time_zone_table, time_zone_ft_field, time_zone_tf_field, build_network
        )
        processor.process_multinet_data()

        # Set derived output
        parameters[23].value = processor.network
        return


def param_to_unit_type_enum(unit_type_str):
    """Conert the tool parameter string value for unit type to an enum."""
    if unit_type_str == "Metric":
        return Process_MultiNet.UnitType.Metric
    return Process_MultiNet.UnitType.Imperial


def param_to_time_zone_enum(time_zone_str):
    """Convert the tool parameter string value for time zone type to an enum."""
    if time_zone_str == "Single time zone":
        return Process_MultiNet.TimeZoneType.Single
    if time_zone_str == "Use time zone table":
        return Process_MultiNet.TimeZoneType.Table
    return Process_MultiNet.TimeZoneType.NoTimeZone  # "None" or something invalid
