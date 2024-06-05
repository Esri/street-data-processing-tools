"""StreetDataProcessing.pyt

Tools to process raw TomTom MultiNet data into a network dataset.

   Copyright 2024 Esri
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
import Process_HERENavstreetsShp
from helpers import TimeZoneType


class Toolbox(object):
    def __init__(self):
        """Define the toolbox."""
        self.label = "Street Data Processing Tools"
        self.alias = "SDPT"

        # List of tool classes associated with this toolbox
        self.tools = [ProcessMultiNet, ProcessNAVSTREETS]


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

        time_zone_params = make_time_zone_params(param_in_network_geometry)

        params = [
            param_in_network_geometry,  # 0
            param_in_maneuvers_geometry,  # 1
            param_in_maneuver_path_index,  # 2
            param_in_sign_information,  # 3
            param_in_sign_path,  # 4
            param_in_restrictions,  # 5
            PARAM_OUT_FOLDER,  # 6
            PARAM_OUT_GDB_NAME,  # 7
            PARAM_UNIT_TYPE,  # 8
            PARAM_BUILD_NETWORK,  # 9
            param_in_ltr,  # 10
            param_include_traffic,  # 11
            param_in_network_profile_link,  # 12
            param_in_historical_speed_profiles,  # 13
            param_in_rds_tmc_info,  # 14
            param_include_multinet,  # 15
            param_in_logistics_lrs,  # 16
            param_in_logistics_lvc,  # 17
        ] + time_zone_params + [  # 18-22
            PARAM_OUT_NETWORK  # 23 Derived output
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
            if time_zone_type == TimeZoneType.NoTimeZone:
                parameters[self.param_idx_tz_name].enabled = False
                parameters[self.param_idx_tz_name].value = None
                parameters[self.param_idx_tz_table].enabled = False
                parameters[self.param_idx_tz_table].value = None
                parameters[self.param_idx_tz_ft_field].enabled = False
                parameters[self.param_idx_tz_ft_field].value = None
                parameters[self.param_idx_tz_tf_field].enabled = False
                parameters[self.param_idx_tz_tf_field].value = None
            elif time_zone_type == TimeZoneType.Single:
                parameters[self.param_idx_tz_name].enabled = True
                parameters[self.param_idx_tz_table].enabled = False
                parameters[self.param_idx_tz_table].value = None
                parameters[self.param_idx_tz_ft_field].enabled = False
                parameters[self.param_idx_tz_ft_field].value = None
                parameters[self.param_idx_tz_tf_field].enabled = False
                parameters[self.param_idx_tz_tf_field].value = None
            elif time_zone_type == TimeZoneType.Table:
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
        if time_zone_type == TimeZoneType.NoTimeZone:
            parameters[self.param_idx_tz_name].clearMessage()
            parameters[self.param_idx_tz_table].clearMessage()
            parameters[self.param_idx_tz_ft_field].clearMessage()
            parameters[self.param_idx_tz_tf_field].clearMessage()
        elif time_zone_type == TimeZoneType.Single:
            if not parameters[self.param_idx_tz_name].valueAsText:
                # The 735 error code doesn't display an actual error but displays the little red star to
                # indicate that the parameter is required.
                parameters[self.param_idx_tz_name].setIDMessage(
                    "Error", 735, parameters[self.param_idx_tz_name].displayName)
            parameters[self.param_idx_tz_table].clearMessage()
            parameters[self.param_idx_tz_ft_field].clearMessage()
            parameters[self.param_idx_tz_tf_field].clearMessage()
        elif time_zone_type == TimeZoneType.Table:
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

        # Time zone is required if the RD table is specified, because the only purpose of that table is to support
        # live traffic
        param_rd = parameters[14]
        if time_zone_type == TimeZoneType.NoTimeZone and param_rd.enabled and param_rd.valueAsText:
            msg = "Time zone is required when the Input RDS-TMC Information (RD) Table is specified."
            param_rd.setErrorMessage(msg)
            parameters[self.param_idx_tz_type].setErrorMessage(msg)

        return

    def execute(self, parameters, messages):
        """The source code of the tool."""
        include_historical_traffic = parameters[self.param_idx_trf_bool].value
        include_logistics = parameters[self.param_idx_logistics_bool].value
        in_multinet = Process_MultiNet.MultiNetInputData(
            parameters[0].value,
            parameters[1].value,
            parameters[2].value,
            parameters[3].value,
            parameters[4].value,
            parameters[5].value,
            include_historical_traffic,
            include_logistics,
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
        time_zone_type = param_to_time_zone_enum(parameters[self.param_idx_tz_type].valueAsText)
        time_zone_name = parameters[self.param_idx_tz_name].valueAsText
        time_zone_table = parameters[self.param_idx_tz_table].valueAsText
        time_zone_ft_field = parameters[self.param_idx_tz_ft_field].valueAsText
        time_zone_tf_field = parameters[self.param_idx_tz_tf_field].valueAsText
        processor = Process_MultiNet.MultiNetProcessor(
            out_folder, gdb_name, in_multinet, unit_type,
            time_zone_type, time_zone_name, time_zone_table, time_zone_ft_field, time_zone_tf_field,
            build_network
        )
        processor.process_multinet_data()

        # Set derived output
        parameters[23].value = processor.network
        return


class ProcessNAVSTREETS(object):

    def __init__(self):
        """Define the tool."""
        self.label = "Process NAVSTREETS"
        self.description = "Process HERE NAVSTREETS shapefile data for a network dataset"
        self.canRunInBackground = True

        self.param_idx_hist_traff_type = 10  # Parameter index of historical traffic type
        self.param_idx_live_traff = 11  # Parameter index of live traffic boolean
        self.param_idx_traff_table = 16  # Parameter index of traffic table
        # Set up list of indices of required/enabled parameters for different traffic types
        # For setting up historical traffic using Link Reference Files you need all of the following:
        #   - Input Link Reference File covering Functional Classes 1-4
        #   - Input Link Reference File covering Functional Class 5
        #   - Input Speed Pattern Dictionary (SPD) file
        # For setting up historical traffic using TMC Reference Files you need all of the following:
        #   - Input Traffic Table
        #   - Input TMC Reference File
        #   - Input Speed Pattern Dictionary (SPD) file
        # If live traffic will be configured, the Traffic Table is required.
        self.trf_idxs_by_type = {
            "Link Reference Files": [12, 13, 14],
            "TMC Reference Files": [12, 15, self.param_idx_traff_table]
        }
        self.all_trf_idx = range(12, self.param_idx_traff_table + 1)

        self.param_idx_tz_type = 17
        self.param_idx_tz_name = 18
        self.param_idx_tz_table = 19
        self.param_idx_tz_ft_field = 20
        self.param_idx_tz_tf_field = 21

    def getParameterInfo(self):
        """Define parameter definitions"""
        param_in_streets = arcpy.Parameter(
            displayName="Input Streets Feature Class",
            name="in_streets_fc",
            datatype="GPFeatureLayer",
            parameterType="Required",
            direction="Input"
        )

        param_in_alt_streets = arcpy.Parameter(
            displayName="Input AltStreets Feature Class",
            name="in_alt_streets_fc",
            datatype="GPFeatureLayer",
            parameterType="Required",
            direction="Input"
        )

        param_in_z_levels = arcpy.Parameter(
            displayName="Input Z-Levels Feature Class",
            name="in_z_levels_fc",
            datatype="GPFeatureLayer",
            parameterType="Required",
            direction="Input"
        )

        param_in_cdms = arcpy.Parameter(
            displayName="Input Condition/Driving Manoeuvres (CDMS) Table",
            name="in_cdms_table",
            datatype="GPTableView",
            parameterType="Required",
            direction="Input"
        )

        param_in_rdms = arcpy.Parameter(
            displayName="Input Restricted Driving Manoeuvres (RDMS) Table",
            name="in_rdms_table",
            datatype="GPTableView",
            parameterType="Required",
            direction="Input"
        )

        param_in_signs = arcpy.Parameter(
            displayName="Input Signs Table",
            name="in_signs_table",
            datatype="GPTableView",
            parameterType="Required",
            direction="Input"
        )

        param_hist_traffic_type = arcpy.Parameter(
            displayName="Historical Traffic Configuration Type",
            name="historical_traffic_type",
            datatype="GPString",
            parameterType="Required",
            direction="Input",
            category="Traffic"
        )
        param_hist_traffic_type.filter.list = ["None", "Link Reference Files", "TMC Reference Files"]
        param_hist_traffic_type.value = "None"

        param_include_live_traffic = arcpy.Parameter(
            displayName="Include live traffic",
            name="include_live_traffic",
            datatype="GPBoolean",
            parameterType="Optional",
            direction="Input",
            category="Traffic"
        )
        param_include_live_traffic.value = False

        param_traff_spd_file = arcpy.Parameter(
            displayName="Input Speed Pattern Dictionary (SPD) file",
            name="in_spd_file",
            datatype="DEFile",
            parameterType="Optional",
            direction="Input",
            category="Traffic"
        )
        param_traff_spd_file.filter.list = ["csv"]

        param_traff_link_ref_1_4 = arcpy.Parameter(
            displayName="Input Link Reference File covering Functional Classes 1-4",
            name="in_link_ref_1_4",
            datatype="DEFile",
            parameterType="Optional",
            direction="Input",
            category="Traffic"
        )
        param_traff_link_ref_1_4.filter.list = ["csv"]

        param_traff_link_ref_5 = arcpy.Parameter(
            displayName="Input Link Reference File covering Functional Class 5",
            name="in_link_ref_5",
            datatype="DEFile",
            parameterType="Optional",
            direction="Input",
            category="Traffic"
        )
        param_traff_link_ref_5.filter.list = ["csv"]

        param_traff_tmc_ref = arcpy.Parameter(
            displayName="Input TMC Reference File",
            name="in_tmc_ref_file",
            datatype="DEFile",
            parameterType="Optional",
            direction="Input",
            category="Traffic"
        )
        param_traff_tmc_ref.filter.list = ["csv"]

        param_traff_table = arcpy.Parameter(
            displayName="Input Traffic Table",
            name="in_traffic_table",
            datatype="GPTableView",
            parameterType="Optional",
            direction="Input",
            category="Traffic"
        )

        time_zone_params = make_time_zone_params(param_in_streets)

        param_condmod_us = arcpy.Parameter(
            displayName="Input Condition Modifier (CndMod) Table (US)",
            name="in_cndmod_us_table",
            datatype="GPTableView",
            parameterType="Optional",
            direction="Input",
            category="Transport Condition Modifiers"
        )

        param_condmod_nonus = arcpy.Parameter(
            displayName="Input Condition Modifier (CndMod) Table (non-US)",
            name="in_cndmod_non_us_table",
            datatype="GPTableView",
            parameterType="Optional",
            direction="Input",
            category="Transport Condition Modifiers"
        )

        params = [
            param_in_streets,  # 0
            param_in_alt_streets,  # 1
            param_in_z_levels,  # 2
            param_in_cdms,  # 3
            param_in_rdms,  # 4
            param_in_signs,  # 5
            PARAM_OUT_FOLDER,  # 6
            PARAM_OUT_GDB_NAME,  # 7
            PARAM_UNIT_TYPE,  # 8
            PARAM_BUILD_NETWORK,  # 9
            param_hist_traffic_type,  # 10
            param_include_live_traffic,  # 11
            param_traff_spd_file,  # 12
            param_traff_link_ref_1_4,  # 13
            param_traff_link_ref_5,  # 14
            param_traff_tmc_ref,  # 15
            param_traff_table  # 16
        ] + time_zone_params + [  # 17-21 params
            param_condmod_us,  # 22
            param_condmod_nonus,  # 23
            PARAM_OUT_NETWORK  # 24 Derived output
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
        traffic_type = parameters[self.param_idx_hist_traff_type].valueAsText
        if not parameters[self.param_idx_hist_traff_type].hasBeenValidated:
            if traffic_type == "None":
                parameters[self.param_idx_live_traff].enabled = False
                parameters[self.param_idx_live_traff].value = False
                for idx in self.all_trf_idx:
                    parameters[idx].enabled = False
                    parameters[idx].value = None
            else:
                parameters[self.param_idx_live_traff].enabled = True
                required_idxs = self.trf_idxs_by_type.get(traffic_type, [])
                for idx in required_idxs:
                    parameters[idx].enabled = True
                for idx in [i for i in self.all_trf_idx if i not in required_idxs]:
                    if idx == self.param_idx_traff_table and parameters[self.param_idx_live_traff].value == True:
                        # Don't mess with the traffic table if it's being used for live traffic
                        continue
                    parameters[idx].enabled = False
                    parameters[idx].value = None
        # Enable traffic table parameter if live traffic is selected. Disable it if live traffic is not selected unless
        # it is required for the type of historical traffic selected.
        if not parameters[self.param_idx_live_traff].hasBeenValidated:
            if parameters[self.param_idx_live_traff].value == True:
                parameters[self.param_idx_traff_table].enabled = True
            elif self.param_idx_traff_table not in self.trf_idxs_by_type.get(traffic_type, []):
                parameters[self.param_idx_traff_table].enabled = False
                parameters[self.param_idx_traff_table].value = None

        # Enable and disable time zone parameters depending on value of time zone type parameter
        if not parameters[self.param_idx_tz_type].hasBeenValidated:
            time_zone_type = param_to_time_zone_enum(parameters[self.param_idx_tz_type].valueAsText)
            if time_zone_type == TimeZoneType.NoTimeZone:
                parameters[self.param_idx_tz_name].enabled = False
                parameters[self.param_idx_tz_name].value = None
                parameters[self.param_idx_tz_table].enabled = False
                parameters[self.param_idx_tz_table].value = None
                parameters[self.param_idx_tz_ft_field].enabled = False
                parameters[self.param_idx_tz_ft_field].value = None
                parameters[self.param_idx_tz_tf_field].enabled = False
                parameters[self.param_idx_tz_tf_field].value = None
            elif time_zone_type == TimeZoneType.Single:
                parameters[self.param_idx_tz_name].enabled = True
                parameters[self.param_idx_tz_table].enabled = False
                parameters[self.param_idx_tz_table].value = None
                parameters[self.param_idx_tz_ft_field].enabled = False
                parameters[self.param_idx_tz_ft_field].value = None
                parameters[self.param_idx_tz_tf_field].enabled = False
                parameters[self.param_idx_tz_tf_field].value = None
            elif time_zone_type == TimeZoneType.Table:
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

        # Make traffic parameters required if enabled according to rules in updateParameters
        for idx in self.all_trf_idx:
            if parameters[idx].enabled and not parameters[idx].valueAsText:
                # The 735 error code doesn't display an actual error but displays the little red star to
                # indicate that the parameter is required.
                parameters[idx].setIDMessage("Error", 735, parameters[idx].displayName)
            else:
                # Parameter is either disabled or has a value
                parameters[idx].clearMessage()

        # Make time zone parameters required depending on selected type
        time_zone_type = param_to_time_zone_enum(parameters[self.param_idx_tz_type].valueAsText)
        if time_zone_type == TimeZoneType.NoTimeZone:
            parameters[self.param_idx_tz_name].clearMessage()
            parameters[self.param_idx_tz_table].clearMessage()
            parameters[self.param_idx_tz_ft_field].clearMessage()
            parameters[self.param_idx_tz_tf_field].clearMessage()
        elif time_zone_type == TimeZoneType.Single:
            if not parameters[self.param_idx_tz_name].valueAsText:
                # The 735 error code doesn't display an actual error but displays the little red star to
                # indicate that the parameter is required.
                parameters[self.param_idx_tz_name].setIDMessage(
                    "Error", 735, parameters[self.param_idx_tz_name].displayName)
            parameters[self.param_idx_tz_table].clearMessage()
            parameters[self.param_idx_tz_ft_field].clearMessage()
            parameters[self.param_idx_tz_tf_field].clearMessage()
        elif time_zone_type == TimeZoneType.Table:
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

        # Time zone is required if live traffic is requested
        param_live_traffic = parameters[self.param_idx_live_traff]
        if time_zone_type == TimeZoneType.NoTimeZone and \
            param_live_traffic.enabled and \
                param_live_traffic.value:
            msg = "Time zone is required when live traffic support is enabled."
            param_live_traffic.setErrorMessage(msg)
            parameters[self.param_idx_tz_type].setErrorMessage(msg)

        return

    def execute(self, parameters, messages):
        """The source code of the tool."""
        in_here = Process_HERENavstreetsShp.HereNavstreetsShpInputData(
            parameters[0].value,
            parameters[1].value,
            parameters[2].value,
            parameters[3].value,
            parameters[4].value,
            parameters[5].value,
            param_to_here_traffic_type_enum(parameters[self.param_idx_hist_traff_type].valueAsText),
            parameters[self.param_idx_live_traff].value,
            parameters[12].valueAsText,
            parameters[15].valueAsText,
            parameters[16].value,
            parameters[13].valueAsText,
            parameters[14].valueAsText,
            parameters[22].valueAsText,
            parameters[23].valueAsText,
        )
        out_folder = parameters[6].valueAsText
        gdb_name = parameters[7].valueAsText
        unit_type = param_to_unit_type_enum(parameters[8].valueAsText)
        build_network = parameters[9].value
        time_zone_type = param_to_time_zone_enum(parameters[self.param_idx_tz_type].valueAsText)
        time_zone_name = parameters[self.param_idx_tz_name].valueAsText
        time_zone_table = parameters[self.param_idx_tz_table].valueAsText
        time_zone_ft_field = parameters[self.param_idx_tz_ft_field].valueAsText
        time_zone_tf_field = parameters[self.param_idx_tz_tf_field].valueAsText
        processor = Process_HERENavstreetsShp.HereNavstreetsShpProcessor(
            out_folder, gdb_name, in_here, unit_type,
            time_zone_type, time_zone_name, time_zone_table, time_zone_ft_field, time_zone_tf_field,
            build_network
        )
        processor.process_here_data()

        # Set derived output
        parameters[24].value = processor.network
        return


def param_to_unit_type_enum(unit_type_str):
    """Convert the tool parameter string value for unit type to an enum."""
    if unit_type_str == "Metric":
        return Process_MultiNet.UnitType.Metric
    return Process_MultiNet.UnitType.Imperial


def param_to_time_zone_enum(time_zone_str):
    """Convert the tool parameter string value for time zone type to an enum."""
    if time_zone_str == "Single time zone":
        return TimeZoneType.Single
    if time_zone_str == "Use time zone table":
        return TimeZoneType.Table
    return TimeZoneType.NoTimeZone  # "None" or something invalid


def param_to_here_traffic_type_enum(traffic_type_str):
    """Convert the tool parameter string value for HERE historical traffic type to an enum."""
    if traffic_type_str == "Link Reference Files":
        return Process_HERENavstreetsShp.HistoricalTrafficConfigType.LinkReferenceFiles
    elif traffic_type_str == "TMC Reference Files":
        return Process_HERENavstreetsShp.HistoricalTrafficConfigType.TMCReferenceFiles
    return Process_HERENavstreetsShp.HistoricalTrafficConfigType.NoTraffic

# region Shared parameters

def make_time_zone_params(param_in_streets):
    """Construct parameter objects for the time zone parameters shared by all tools."""
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
    param_in_ft_time_zone_field_name.parameterDependencies = [param_in_streets.name]
    param_in_ft_time_zone_field_name.filter.list = ["Short", "Long"]

    param_in_tf_time_zone_field_name = arcpy.Parameter(
        displayName="Input TF Time Zone ID Field Name",
        name="in_tf_time_zone_field_name",
        datatype="Field",
        parameterType="Optional",
        direction="Input",
        category="Time Zone"
    )
    param_in_tf_time_zone_field_name.parameterDependencies = [param_in_streets.name]
    param_in_tf_time_zone_field_name.filter.list = ["Short", "Long"]

    return [
        param_time_zone_type,
        param_time_zone_name,
        param_in_time_zone_table,
        param_in_ft_time_zone_field_name,
        param_in_tf_time_zone_field_name
    ]


PARAM_OUT_FOLDER = arcpy.Parameter(
    displayName="Output Folder",
    name="output_folder",
    datatype="DEFolder",
    parameterType="Required",
    direction="Input"
)

PARAM_OUT_GDB_NAME = arcpy.Parameter(
    displayName="Output Geodatabase Name",
    name="output_geodatabase_name",
    datatype="GPString",
    parameterType="Required",
    direction="Input"
)

PARAM_UNIT_TYPE = arcpy.Parameter(
    displayName="Unit Type",
    name="unit_type",
    datatype="GPString",
    parameterType="Required",
    direction="Input"
)
PARAM_UNIT_TYPE.filter.list = ["Imperial", "Metric"]
PARAM_UNIT_TYPE.value = "Imperial"

PARAM_BUILD_NETWORK = arcpy.Parameter(
    displayName="Build the network dataset",
    name="build_network",
    datatype="GPBoolean",
    parameterType="Optional",
    direction="Input"
)
PARAM_BUILD_NETWORK.value = True

PARAM_OUT_NETWORK = arcpy.Parameter(
    displayName="Output Network Dataset",
    name="Output_Network",
    datatype="DENetworkDataset",
    parameterType="Derived",
    direction="Output"
)

# endregion Shared parameters