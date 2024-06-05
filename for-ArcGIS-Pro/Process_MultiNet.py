"""Class to process raw TomTom MultiNet data into a network dataset.

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
import pandas as pd
import numpy as np
import os
import uuid
from lxml import etree
import arcpy
from helpers import CURDIR, timed_exec, TimeZoneType, UnitType, DataProductType, StreetInputData, StreetDataProcessor


LNG_CODES = {
    "ALB": "sq",  # Albanian
    "ALS": "",  # Alsatian
    "ARA": "ar",  # Arabic
    "BAQ": "eu",  # Basque
    "BAT": "",  # Baltic (Other)
    "BEL": "be",  # Belarusian
    "BET": "be",  # Belarusian (Latin)
    "BOS": "bs",  # Bosnian
    "BRE": "br",  # Breton
    "BUL": "bg",  # Bulgarian
    "BUN": "bg",  # Bulgarian (Latin)
    "BUR": "my",  # Burmese
    "CAT": "ca",  # Catalan
    "CEL": "",  # Celtic (Other)
    "CHI": "zh",  # Chinese, Han Simplified
    "CHL": "zh",  # Chinese, Mandarin Pinyin
    "CHT": "zh",  # Chinese, Han Traditional
    "CTN": "zh",  # Chinese, Cantonese Pinyin
    "CZE": "cs",  # Czech
    "DAN": "da",  # Danish
    "DUT": "nl",  # Dutch
    "ENG": "en",  # English
    "EST": "et",  # Estonian
    "FAO": "fo",  # Faroese
    "FIL": "",  # Filipino
    "FIN": "fi",  # Finnish
    "FRE": "fr",  # French
    "FRY": "fy",  # Frisian
    "FUR": "",  # Friulian
    "GEM": "",  # Franco-Provencal
    "GER": "de",  # German
    "GLA": "gd",  # Gaelic (Scots)
    "GLE": "ga",  # Irish
    "GLG": "gl",  # Galician
    "GRE": "el",  # Greek (Modern)
    "GRL": "el",  # Greek (Latin Transcription)
    "HEB": "he",  # Hebrew
    "HIN": "hi",  # Hindi
    "HUN": "hu",  # Hungarian
    "ICE": "is",  # Icelandic
    "IND": "id",  # Indonesian
    "ITA": "it",  # Italian
    "KHM": "km",  # Khmer
    "KOL": "ko",  # Korean (Latin)
    "KOR": "ko",  # Korean
    "LAD": "",  # Ladin
    "LAO": "lo",  # Lao
    "LAT": "la",  # Latin
    "LAV": "lv",  # Latvian
    "LIT": "lt",  # Lithuanian
    "LTZ": "lb",  # Luxembourgish
    "MAC": "mk",  # Macedonian
    "MAP": "",  # Austronesian (Other)
    "MAT": "mk",  # Macedonian (Latin Transcription)
    "MAY": "ms",  # Malaysian
    "MLT": "mt",  # Maltese
    "MOL": "mo",  # Moldavian
    "MYN": "",  # Mayan Languages
    "NOR": "no",  # Norwegian
    "OCI": "oc",  # Occitan
    "PAA": "",  # Papuan-Australian (Other)
    "POL": "pl",  # Polish
    "POR": "pt",  # Portuguese
    "PRO": "",  # Provencal
    "ROA": "",  # Romance (Other)
    "ROH": "rm",  # Raeto-Romance
    "ROM": "",  # Romani
    "RUL": "ru",  # Russian (Latin Transcription)
    "RUM": "ro",  # Romanian
    "RUS": "ru",  # Russian
    "SCC": "sh",  # Serbian (Latin)
    "SCO": "gd",  # Scots
    "SCR": "sh",  # Croatian
    "SCY": "sh",  # Serbian (Cyrillic)
    "SLA": "cu",  # Slavic
    "SLO": "sk",  # Slovak
    "SLV": "sv",  # Slovenian
    "SMC": "",  # Montenegrin (Cyrillic)
    "SMI": "se",  # Lapp (Sami)
    "SML": "",  # Montenegrin (Latin)
    "SPA": "es",  # Spanish
    "SRD": "sc",  # Sardinian
    "SWE": "sv",  # Swedish
    "THA": "th",  # Thai
    "THL": "th",  # Thai (Latin)
    "TUR": "tr",  # Turkish
    "UKL": "uk",  # Ukrainian (Latin)
    "UKR": "uk",  # Ukrainian
    "UND": "",  # Undefined
    "VAL": "ca",  # Valencian
    "VIE": "vi",  # Vietnamese
    "WEL": "cy",  # Welsh
    "WEN": "",  # Sorbian (Other)
}


class MultiNetInputData(StreetInputData):
    """Defines a collection of MultiNet inputs to process."""

    def __init__(
        self, network_geometry_fc, maneuvers_geometry_fc, maneuver_path_idx_table, sign_info_table, sign_path_table,
        restrictions_table, include_historical_traffic, include_logistics,
        network_profile_link_table=None, historical_speed_profiles_table=None,
        rds_tmc_info_table=None, logistics_truck_routes_table=None, logistics_lrs_table=None, logistics_lvc_table=None
    ):
        """Initialize an input MultiNet dataset with all the appropriate feature classes and tables"""
        self.nw = network_geometry_fc
        self.mn = maneuvers_geometry_fc
        self.mp = maneuver_path_idx_table
        self.si = sign_info_table
        self.sp = sign_path_table
        self.rs = restrictions_table
        self.hsnp = network_profile_link_table
        self.hspr = historical_speed_profiles_table
        self.rd = rds_tmc_info_table
        self.ltr = logistics_truck_routes_table
        self.lrs = logistics_lrs_table
        self.lvc = logistics_lvc_table
        self.include_historical_traffic = include_historical_traffic
        self.include_logistics = include_logistics
        required_tables = [self.nw, self.mn, self.mp, self.si, self.sp, self.rs]
        if self.include_historical_traffic:
            required_tables += [self.hsnp, self.hspr]
        if self.include_logistics:
            required_tables += [self.lrs, self.lvc]
        super().__init__(
            required_tables=required_tables,
            required_fields={
                self.nw: [
                    ("ID", "Double"),
                    ("FEATTYP", "SmallInteger"),
                    ("F_JNCTID", "Double"),
                    ("T_JNCTID", "Double"),
                    ("PJ", "SmallInteger"),
                    ("METERS", "Double"),
                    ("NET2CLASS", "SmallInteger"),
                    ("NAME", "String"),
                    ("FOW", "SmallInteger"),
                    ("FREEWAY", "SmallInteger"),
                    ("BACKRD", "SmallInteger"),
                    ("TOLLRD", "SmallInteger"),
                    ("RDCOND", "SmallInteger"),
                    ("PRIVATERD", "SmallInteger"),
                    ("CONSTATUS", "String"),
                    ("ONEWAY", "String"),
                    ("F_ELEV", "SmallInteger"),
                    ("T_ELEV", "SmallInteger"),
                    ("KPH", "SmallInteger"),
                    ("MINUTES", "Single"),  # Float
                    ("NTHRUTRAF", "SmallInteger"),
                    ("ROUGHRD", "SmallInteger"),
                ],
                self.mn: [
                    ("ID", "Double"),
                    ("JNCTID", "Double"),
                    ("FEATTYP", "SmallInteger")
                ],
                self.mp: [
                    ("ID", "Double"),
                    ("TRPELID", "Double"),
                    ("SEQNR", "Integer")
                ],
                self.si: [
                    ("ID", "Double"),
                    ("INFOTYP", "String"),
                    ("TXTCONT", "String"),
                    ("TXTCONTLC", "String"),
                    ("CONTYP", "SmallInteger"),
                    ("SEQNR", "Integer"),
                    ("DESTSEQ", "Integer"),
                    ("RNPART", "SmallInteger")
                ],
                self.sp: [
                    ("ID", "Double"),
                    ("TRPELID", "Double"),
                    ("SEQNR", "Integer")
                ],
                self.rs: [
                    ("ID", "Double"),
                    ("FEATTYP", "SmallInteger"),
                    ("VT", "SmallInteger"),
                    ("DIR_POS", "SmallInteger"),
                    ("RESTRTYP", "String")
                ],
                self.hsnp: [
                    ("NETWORK_ID", "Double"),
                    ("VAL_DIR", "SmallInteger"),
                    ("SPFREEFLOW", "SmallInteger"),
                    ("SPWEEKDAY", "SmallInteger"),
                    ("SPWEEKEND", "SmallInteger"),
                    ("SPWEEK", "SmallInteger"),
                    ("PROFILE_1", "SmallInteger"),
                    ("PROFILE_2", "SmallInteger"),
                    ("PROFILE_3", "SmallInteger"),
                    ("PROFILE_4", "SmallInteger"),
                    ("PROFILE_5", "SmallInteger"),
                    ("PROFILE_6", "SmallInteger"),
                    ("PROFILE_7", "SmallInteger")
                ],
                self.hspr: [
                    ("PROFILE_ID", "SmallInteger"),
                    ("TIME_SLOT", "Integer"),
                    ("REL_SP", "Single")
                ],
                self.rd: [
                    ("ID", "Double"),
                    ("RDSTMC", "String")
                ],
                self.ltr: [
                    ("ID", "Double"),
                    ("PREFERRED", "SmallInteger"),
                    ("RESTRICTED", "SmallInteger")
                ],
                self.lrs: [
                    ("ID", "Double"),
                    ("SEQNR", "SmallInteger"),
                    ("RESTRTYP", "String"),
                    ("VT", "SmallInteger"),
                    ("RESTRVAL", "SmallInteger"),
                    ("LIMIT", "Double"),
                    ("UNIT_MEAS", "SmallInteger")
                ],
                self.lvc: [
                    ("ID", "Double"),
                    ("SEQNR", "SmallInteger")
                ]
            },
            sr_input=self.nw
        )


class MultiNetProcessor(StreetDataProcessor):

    def __init__(
        self, out_folder: str, gdb_name: str, in_multinet: MultiNetInputData, unit_type: UnitType,
        time_zone_type: TimeZoneType, time_zone_name: str = "", in_time_zone_table=None,
        time_zone_ft_field: str = None, time_zone_tf_field: str = None, build_network: bool = True
    ):
        """Initialize a class to process MultiNet data into a network dataset."""
        self.include_logistics = in_multinet.include_logistics
        super().__init__(
            DataProductType.TomTomMultinet, out_folder, gdb_name, in_multinet, unit_type,
            in_multinet.include_historical_traffic,
            time_zone_type, time_zone_name, in_time_zone_table, time_zone_ft_field,
            time_zone_tf_field, build_network)

        # Maps VT field codes to restriction names
        self.vt_field_map = {
            0: "AllVehicles_Restricted",
            11: "PassengerCars_Restricted",
            12: "ResidentialVehicles_Restricted",
            16: "Taxis_Restricted",
            17: "PublicBuses_Restricted"
        }
        self.restriction_field_names = [self.vt_field_map[vt] for vt in sorted(self.vt_field_map)]
        # Historical traffic base field names
        self.historical_traffic_fields = ["Weekday", "Weekend", "AllWeek"]
        # Logistics Truck Routes field names
        self.ltr_fields = [
            "NationalSTAARoute", "NationalRouteAccess", "DesignatedTruckRoute", "TruckBypassRoad",
            "NoCommercialVehicles", "ImmediateAccessOnly", "TrucksRestricted"
        ]

        # Global dataframes and variables used by multiple processes and initialized later
        self.r_df = None  # Restrictions table indexed by ID for quick lookups
        self.mp_df = None  # Maneuver paths table indexed by ID for quick lookups
        self.lrs_df = None  # Dataframe of logistics LRS table
        self.unique_lrs_df = None  # Dataframe holding unique combinations of logistics restriction data

    def process_multinet_data(self):
        """Process multinet data into a network dataset."""
        # Set the progressor so the user is informed of progress
        arcpy.SetProgressor("default")

        # Validate the input data
        if not self._validate_inputs():
            return

        # Create the output location
        self._create_feature_dataset()

        # Read in some tables we're going to need to reference later in multiple places
        self._read_and_index_restrictions()
        self._read_and_index_maneuver_paths()
        self._read_and_index_logistics_tables()

        # Create the output Streets feature class and populate it
        self._copy_streets()
        self._detect_and_delete_duplicate_streets("ID")
        self._populate_streets_fields()
        # We're now done with the Logistics restrictions table, so clear the variable to free up memory
        del self.lrs_df
        self.lrs_df = None

        # Read in output streets for future look-ups
        self._read_and_index_streets()

        # Create and populate the turn feature class
        self._create_turn_fc(self.restriction_field_names)
        self._generate_turn_features()
        # We're now done with the restrictions table, so clear the variable to free up memory
        del self.r_df
        self.r_df = None

        # Create and populate the road forks table
        self._create_and_populate_road_forks()
        # We're now done with the maneuver path table, so clear the variable to free up memory
        del self.mp_df
        self.mp_df = None

        # Create and populate Signposts and Signposts_Streets
        self._create_signposts_fc()
        self._create_signposts_streets_table()
        self._populate_signposts_and_signposts_streets()

        # Create and populate historical traffic tables
        if self.include_historical_traffic:
            self._create_and_populate_streets_profiles_table()
            self._create_profiles_table()
            self._populate_profiles_table()
            self._create_and_populate_streets_tmc_table()
        # We're done with the streets table, so clear the variable to free up memory
        del self.streets_df
        self.streets_df = None

        # Handle time zone table if needed
        if self.time_zone_type != TimeZoneType.NoTimeZone:
            self._handle_time_zone()

        # Add attribute indices
        self._add_attribute_indices()

        # Clean up intermediate data
        self._delete_intermediate_outputs()

        # Create the network dataset from a template and build it
        self._create_and_build_nd()

    @timed_exec
    def _copy_streets(self):
        """Copy the network geometry feature class to the target feature dataset and add fields."""
        self._add_message("Copying input network geometry feature class to target feature dataset...")

        # Attempt to spatially sort input streets
        self.in_data_object.nw = self._spatially_sort_streets()

        # Filter out address area boundary elements
        with arcpy.EnvManager(overwriteOutput=True):
            nw_layer = arcpy.management.MakeFeatureLayer(
                self.in_data_object.nw, "NW layer", "FEATTYP <> 4165").getOutput(0)

        # Construct field mappings to use when copying the original data.
        field_mappings = arcpy.FieldMappings()
        # Add all the fields from the input data
        field_mappings.addTable(self.in_data_object.nw)

        # Add the TOLLRDDIR and restriction fields and historical traffic fields if relevant.
        field_mappings = field_mappings.exportToString()
        field_mappings += self._create_string_field_map("TOLLRDDIR", "Text", 2)
        for restr_field in self.restriction_field_names:
            field_mappings += self._create_string_field_map(f"FT_{restr_field}", "Text", 1)
            field_mappings += self._create_string_field_map(f"TF_{restr_field}", "Text", 1)
        if self.include_historical_traffic:
            for trf_fld in self.historical_traffic_fields:
                field_mappings += self._create_string_field_map(f"FT_{trf_fld}", "Short")
                field_mappings += self._create_string_field_map(f"TF_{trf_fld}", "Short")
                field_mappings += self._create_string_field_map(f"FT_{trf_fld}Minutes", "Float")
                field_mappings += self._create_string_field_map(f"TF_{trf_fld}Minutes", "Float")
        if self.in_data_object.ltr:
            for ltr_field in self.ltr_fields:
                field_mappings += self._create_string_field_map(ltr_field, "Text", 1)
        if self.include_logistics:
            # Derive a list of logistics restriction field names based on data from the LRS table
            for _, record in self.unique_lrs_df.iterrows():
                field_mappings += self._create_string_field_map(
                    record["FieldName"], record["FieldType"], record["FieldLength"])

        # Copy the input network geometry feature class to the target feature dataset
        arcpy.conversion.FeatureClassToFeatureClass(
            nw_layer, self.feature_dataset, os.path.basename(self.streets), field_mapping=field_mappings)

        # Update the fc_id that will be used to relate back to this Streets feature class in Edge#FCID fields
        desc = arcpy.Describe(self.streets)
        self.fc_id = desc.DSID
        self.streets_oid_field = desc.oidFieldName

    @timed_exec
    def _create_and_populate_streets_profiles_table(self):
        """Create the Streets_DailyProfiles table."""
        if not self.include_historical_traffic:
            return
        self._add_message("Creating and populating Streets_DailyProfiles table...")
        assert self.streets_df is not None  # Confidence check

        # Create the table with desired schema
        arcpy.management.CreateTable(
            os.path.dirname(self.streets_profiles),
            os.path.basename(self.streets_profiles),
            self.in_data_object.hsnp  # Template table used to define schema
        )
        field_defs = [
            ["EdgeFCID", "LONG"],
            ["EdgeFID", "LONG"],
            ["EdgeFrmPos", "DOUBLE"],
            ["EdgeToPos", "DOUBLE"]
        ]
        arcpy.management.AddFields(self.streets_profiles, field_defs)

        # Insert rows
        desc = arcpy.Describe(self.in_data_object.hsnp)
        input_fields = [f.name for f in desc.fields if f.name != desc.OIDFieldName]
        output_fields = input_fields + [f[0] for f in field_defs]
        network_id_idx = input_fields.index("NETWORK_ID")
        val_dir_idx = input_fields.index("VAL_DIR")
        with arcpy.da.InsertCursor(self.streets_profiles, output_fields) as cur:
            for row in arcpy.da.SearchCursor(
                self.in_data_object.hsnp, input_fields, "SPFREEFLOW > 0 And VAL_DIR IN (2, 3)"
            ):
                # Initialize the row to insert with all the values from the original table
                new_row = [val for val in row]

                # Calculate the additional, new fields: EdgeFCID, EdgeFID, EdgeFrmPos, EdgeToPos
                street_id = np.int64(row[network_id_idx])
                try:
                    # Find the street record associated with this street profile record
                    edge_fid = self.streets_df.loc[street_id]["OID"]
                except KeyError:
                    arcpy.AddWarning((
                        f"The Streets table is missing an entry with ID {row[network_id_idx]}, which is used in the "
                        "network profile link historical traffic table."))
                    # Just skip this row and don't add it
                    continue
                val_dir = row[val_dir_idx]
                if val_dir == 2:
                    edge_from_pos = 0
                    edge_to_pos = 1
                elif val_dir == 3:
                    edge_from_pos = 1
                    edge_to_pos = 0
                else:
                    # This should never happen because of our where clause, but check just in case
                    continue
                new_row += [self.fc_id, edge_fid, edge_from_pos, edge_to_pos]

                # Insert the row
                cur.insertRow(new_row)

    @timed_exec
    def _populate_profiles_table(self):
        """Populate the DailyProfiles table."""
        if not self.include_historical_traffic:
            return
        self._add_message("Populating DailyProfiles table...")

        # Read the Historical Speed Profiles table into a temporary dataframe so we can quickly sort it. Normally we
        # could sort it using the da.SearchCursor's sql clause, but the ORDER BY option doesn't work with shapefile
        # tables.
        fields = ["PROFILE_ID", "TIME_SLOT", "REL_SP"]
        with arcpy.da.SearchCursor(self.in_data_object.hspr, fields) as cur:
            hspr_df = pd.DataFrame(cur, columns=fields)
        hspr_df = hspr_df.sort_values("PROFILE_ID").groupby(["PROFILE_ID"])

        # Insert the rows
        desc = arcpy.Describe(self.profiles)
        output_fields = [f.name for f in desc.fields if f.name != desc.oidFieldName]
        with arcpy.da.InsertCursor(self.profiles, output_fields) as cur:
            # Loop through the records in the HSPR table and calculate the SpeedFactor fields accordingly
            for profile_id, group in hspr_df:
                if isinstance(profile_id, (tuple, list)):
                    # In newer versions of pandas, groupby keys come back as tuples, so just get the first item
                    # in the tuple
                    profile_id = profile_id[0]

                # Initialize a new row with the ProfileID and defaulting all the SpeedFactor fields to None.
                new_row = [profile_id] + [None] * (len(output_fields) - 1)

                # Iterate through the records in this group and populate the SpeedFactor fields
                for _, record in group.iterrows():
                    # Figure out which SpeedFactor field this record is for based on the TIME_SLOT field value
                    # The TIME_SLOT field indicates the time of day as measured in seconds since midnight.  Since the
                    # granularity is 5 minutes, the TIME_SLOT values are all multiples of 300 (e.g., TIME_SLOT=0
                    # represents 12:00am, TIME_SLOT=300 represents 12:05am, TIME_SLOT=600 represents 12:10am, etc.).
                    # Add 1 to the index because ProfileID is the first field in the row
                    time_slot_index = int((record['TIME_SLOT'] / 300)) + 1
                    new_row[time_slot_index] = record["REL_SP"] / 100

                # Check if the row is missing any values, and if so, default them to 1 and add a warning.
                if None in new_row:
                    arcpy.AddWarning((
                        "The Historical Speed Profiles table has incomplete TIME_SLOT records for PROFILE_ID "
                        f"{profile_id}. The missing values have been filled in with a value of 1."
                    ))
                    new_row = [val if val is not None else 1 for val in new_row]

                # Finally, insert the row
                cur.insertRow(new_row)

    @timed_exec
    def _create_and_populate_streets_tmc_table(self):
        if not self.include_historical_traffic or not self.in_data_object.rd:
            return
        self._add_message("Creating and populating Streets_TMC table...")
        assert self.streets_df is not None  # Confidence check
        field_names = self._create_streets_tmc_table()

        with arcpy.da.InsertCursor(self.streets_tmc, field_names) as cur:
            for row in arcpy.da.SearchCursor(self.in_data_object.rd, ["ID", "RDSTMC"]):
                id = row[0]
                # The TMC field value comes from the last 9 characters of the RDSTMC field of the RD table.
                rdstmc = row[1]
                tmc = rdstmc[-9:]
                try:
                    # Find the street record associated with this street profile record
                    edge_fid = self.streets_df.loc[np.int64(row[0])]["OID"]
                except KeyError:
                    arcpy.AddWarning((
                        f"The Streets table is missing an entry with ID {id}, which is used in the RDS-TMC Information "
                        "(RD) historical traffic table."))
                    # Just skip this row and don't add it
                    continue
                if rdstmc[0] == "+":
                    edge_from_pos = 0
                    edge_to_pos = 1
                elif rdstmc[0] == "-":
                    edge_from_pos = 1
                    edge_to_pos = 0
                else:
                    arcpy.AddWarning((
                        "The RDS-TMC Information (RD) historical traffic table has an invalid RDSTMC field value for "
                        f"ID {id}."
                    ))
                    continue

                cur.insertRow([id, tmc, self.fc_id, edge_fid, edge_from_pos, edge_to_pos])

    @timed_exec
    def _read_and_index_restrictions(self):
        """Read in the restrictions table and index it for quick lookups."""
        self._add_message("Reading and grouping restrictions table...")
        where = f"VT IN ({', '.join([str(vt) for vt in self.vt_field_map])})"
        fields = ["ID", "FEATTYP", "VT", "DIR_POS", "RESTRTYP"]
        with arcpy.da.SearchCursor(self.in_data_object.rs, fields, where) as cur:
            self.r_df = pd.DataFrame(cur, columns=fields)
        # Cast the ID column from its original double to an explicit int64 so we can use it for indexing and lookups
        self.r_df = self.r_df.astype({"ID": np.int64})
        # Index the dataframe by ID for quick retrieval later, and sort the index to make those lookups even faster
        self.r_df.set_index("ID", inplace=True)
        self.r_df.sort_index(inplace=True)

    @timed_exec
    def _read_and_index_maneuver_paths(self):
        """Read in the maneuver paths table and index it for quick lookups."""
        self._add_message("Reading and grouping maneuver paths table...")
        fields = ["ID", "TRPELID", "SEQNR"]
        with arcpy.da.SearchCursor(self.in_data_object.mp, fields) as cur:
            # Explicitly read it in using int64 to convert the double-based ID field for easy indexing and lookups
            self.mp_df = pd.DataFrame(cur, columns=fields, dtype=np.int64)
        # Index the dataframe by ID for quick retrieval later, and sort the index to make those lookups even faster
        self.mp_df.set_index("ID", inplace=True)
        self.mp_df.sort_index(inplace=True)
        # Determine the max number of edges participating in a turn. This will be used when creating the turn feature
        # class to initialize the proper number of fields.
        self.max_turn_edges = int(self.mp_df["SEQNR"].max())

    @timed_exec
    def _read_and_index_historical_traffic(self):
        """Read and index historical traffic tables."""
        if not self.include_historical_traffic:
            # Confidence check
            return None
        fields = ["NETWORK_ID", "VAL_DIR", "SPWEEKDAY", "SPWEEKEND", "SPWEEK"]
        with arcpy.da.SearchCursor(self.in_data_object.hsnp, fields, "VAL_DIR IN (2, 3)") as cur:
            # Explicitly read it in using int64 to convert the double-based ID field for easy indexing and lookups
            hsnp_df = pd.DataFrame(cur, columns=fields, dtype=np.int64)
        # Index the dataframe by NETWORK_ID for quick retrieval later,
        # and sort the index to make those lookups even faster
        hsnp_df.set_index("NETWORK_ID", inplace=True)
        hsnp_df.sort_index(inplace=True)
        return hsnp_df

    @timed_exec
    def _read_and_index_logistics_tables(self):
        """Read and index MultiNet Logistics tables."""
        if not self.include_logistics:
            # Confidence check
            return

        # Read in lookup tables
        restrtype_df = pd.read_csv(
            os.path.join(CURDIR, "LogisticsAttributeLookupTables", "RESTRTYP.csv"), index_col="RESTRTYP")
        vt_df = pd.read_csv(os.path.join(CURDIR, "LogisticsAttributeLookupTables", "VT.csv"), index_col="VT")
        restrval_df = pd.read_csv(
            os.path.join(CURDIR, "LogisticsAttributeLookupTables", "RESTRVAL.csv"), index_col="RESTRVAL")

        # Because restrictions that require additional caveats from the LVC table are quite complex and are difficult to
        # model accurately, these are not included in our output network dataset. Read the LVC table to weed out any
        # records in the LRS table that have a matching ID and SEQNR combination.
        fields = ["ID", "SEQNR"]
        with arcpy.da.SearchCursor(self.in_data_object.lvc, fields) as cur:
            # Explicitly read it in using int64 to convert the double-based ID field for easy indexing and lookups
            lvc_df = pd.DataFrame(cur, columns=fields, dtype=np.int64)
        # Add a field to use as a mask after joining
        lvc_df["DROP"] = True
        # Index the dataframe by ID and SEQNR for joining
        lvc_df.set_index(["ID", "SEQNR"], inplace=True)

        # Read the LRS table
        fields = ["ID", "SEQNR", "RESTRTYP", "VT", "RESTRVAL", "LIMIT", "UNIT_MEAS"]
        codes = [f"'{r}'" for r in restrtype_df.index.tolist()]
        where = f"RESTRTYP IN ({', '.join(codes)})"
        with arcpy.da.SearchCursor(self.in_data_object.lrs, fields, where) as cur:
            self.lrs_df = pd.DataFrame(cur, columns=fields)
        # Cast the ID field from its original double to an int64 for lookups and indexing
        self.lrs_df = self.lrs_df.astype({"ID": np.int64})
        self.lrs_df.set_index(["ID", "SEQNR"], inplace=True)

        # Join LVC to LRS and drop any rows that had a match and got transferred to LRS
        self.lrs_df = self.lrs_df.join(lvc_df, how="left")
        self.lrs_df = self.lrs_df[self.lrs_df["DROP"] != True]
        self.lrs_df.reset_index(inplace=True)
        self.lrs_df.drop(columns=["DROP", "SEQNR"], inplace=True)
        del lvc_df

        # Generate a list of LogisticsAttribute objects which we will use to create the necessary fields and network
        # dataset template updates
        # First, identify the unique RESTRTYP, VT, RESTRVAL combinations from the input data
        self.unique_lrs_df = self.lrs_df.drop_duplicates(["RESTRTYP", "VT", "RESTRVAL"], ignore_index=True)

        # Join data from the lookup tables
        self.unique_lrs_df = self.unique_lrs_df.join(restrtype_df, "RESTRTYP", how="left")
        self.unique_lrs_df = self.unique_lrs_df.join(vt_df, "VT", how="left")
        self.unique_lrs_df = self.unique_lrs_df.join(restrval_df, "RESTRVAL", how="left")
        # Drop any records where essential fields were not properly mapped
        orig_num_rows = self.unique_lrs_df.shape[0]
        self.unique_lrs_df.dropna(inplace=True, subset=["FieldNamePrefix", "FieldNameComponent", "FieldNameSuffix"])
        num_rows = self.unique_lrs_df.shape[0]
        if num_rows < orig_num_rows:
            arcpy.AddWarning((
                "Some records from the MultiNet Logistics LRS table had unsupported RESTRTYP, VT, or RESTRVAL field "
                "values. These records have been ignored."
            ))
        arcpy.AddMessage(f"{num_rows} MultiNet Logistics restrictions will be included in the network.")

        # Calculate the field and attribute names from the joined components
        self.unique_lrs_df["FieldName"] = self.unique_lrs_df["FieldNamePrefix"] + \
                                          self.unique_lrs_df["FieldNameComponent"] + \
                                          self.unique_lrs_df["FieldNameSuffix"]
        self.unique_lrs_df["RestrictionName"] = self.unique_lrs_df["RestrictionNamePrefix"] + \
                                                self.unique_lrs_df["AttributeNameComponent"] + \
                                                self.unique_lrs_df["AttributeNameSuffix"]
        self.unique_lrs_df["DescriptorName"] = self.unique_lrs_df[f"DescriptorNamePrefix{self.unit_type.name}"] + \
                                               self.unique_lrs_df["AttributeNameComponent"] + \
                                               self.unique_lrs_df["AttributeNameSuffix"]

        # Join the field name back to the lrs_df for quick lookups when we populate the Streets table later
        unique_lrs_df_indexed = self.unique_lrs_df.set_index(["RESTRTYP", "VT", "RESTRVAL"])
        self.lrs_df = self.lrs_df.join(
            unique_lrs_df_indexed["FieldName"], how="left", on=["RESTRTYP", "VT", "RESTRVAL"])
        del unique_lrs_df_indexed
        self.lrs_df.drop(columns=["VT", "RESTRVAL"], inplace=True)

        # Index lrs_df for quick lookups
        self.lrs_df.set_index("ID", inplace=True)

    @timed_exec
    def _read_and_index_ltr(self):
        """Read and index the logistics truck routes table."""
        if not self.in_data_object.ltr:
            return None
        fields = ["ID", "PREFERRED", "RESTRICTED"]
        with arcpy.da.SearchCursor(self.in_data_object.ltr, fields) as cur:
            # Explicitly read it in using int64 to convert the double-based ID field for easy indexing and lookups
            ltr_df = pd.DataFrame(cur, columns=fields, dtype=np.int64)
        # Index the dataframe by ID for quick retrieval later, and sort the index to make those lookups even faster
        ltr_df.set_index("ID", inplace=True)
        ltr_df.sort_index(inplace=True)
        return ltr_df

    @timed_exec
    def _read_and_index_streets(self):
        """Read in the streets table and index it for quick lookups."""
        self._add_message("Reading and indexing Streets table...")
        # Store street info in a dataframe for quick lookups
        with arcpy.da.SearchCursor(self.streets, ["ID", "OID@", "F_JNCTID", "T_JNCTID"]) as cur:
            self.streets_df = pd.DataFrame(cur, columns=["ID", "OID", "F_JNCTID", "T_JNCTID"])
        # Cast the ID field from its original double to an int64 for lookups and indexing
        self.streets_df = self.streets_df.astype({"ID": np.int64})
        self.streets_df.set_index("ID", inplace=True)
        # Add an empty field to store street geometries.  They'll be filled only as needed to conserve memory
        self.streets_df["SHAPE"] = None

    @timed_exec
    def _populate_streets_fields(self):
        """Populate the restrictions and traffic fields in the streets table."""
        if not self.include_historical_traffic:
            self._add_message("Populating restriction fields in streets...")
        else:
            self._add_message("Populating restriction and historical traffic fields in streets...")
        assert self.r_df is not None  # Confidence check

        # Read some additional tables if needed
        hsnp_df = self._read_and_index_historical_traffic()
        ltr_df = self._read_and_index_ltr()

        # Subset the restrictions table to include only the restriction type we care about
        r_df_streets_subset = self.r_df[self.r_df["RESTRTYP"] == "DF"]

        # Build a list of fields to retrieve in the Streets NW table, including the ID, all the restriction value
        # fields, and, optionally, historical traffic fields
        fields = ["ID", "TOLLRD", "TOLLRDDIR"]
        for restr_field in self.restriction_field_names:
            fields += [f"FT_{restr_field}", f"TF_{restr_field}"]
        if self.include_historical_traffic:
            fields += ["METERS", "KPH"]
            for trf_fld in self.historical_traffic_fields:
                fields += [f"FT_{trf_fld}", f"TF_{trf_fld}", f"FT_{trf_fld}Minutes", f"TF_{trf_fld}Minutes"]
        if self.include_logistics:
            # Add logistics restriction fields if needed. This method actually adds the fields to the table.
            fields += self.unique_lrs_df["FieldName"].tolist()
        if self.in_data_object.ltr:
            fields += self.ltr_fields
        field_idxs = {field: fields.index(field) for field in fields}

        def calc_tollrddir(row):
            """Calculate the TOLLRDDIR field based on the value of TOLLRD."""
            tollrd = row[field_idxs["TOLLRD"]]
            if tollrd in [11, 21]:
                row[field_idxs["TOLLRDDIR"]] = "B"
            if tollrd in [12, 22]:
                row[field_idxs["TOLLRDDIR"]] = "FT"
            if tollrd in [13, 23]:
                row[field_idxs["TOLLRDDIR"]] = "TF"
            return row

        def calc_minutes(trf_fld, meters, kph):
            """Calculate minutes for the given traffic field based on meters or kph."""
            if trf_fld is not None:
                speed = trf_fld
            else:
                speed = kph
            return meters * 0.06 / speed

        def calc_restr_field(row, dir_pos, vt):
            restr_field = self.vt_field_map[vt]
            if dir_pos in [1, 2]:
                row[field_idxs[f"FT_{restr_field}"]] = "Y"
            if dir_pos in [1, 3]:
                row[field_idxs[f"TF_{restr_field}"]] = "Y"
            return row

        def calc_basic_traffic_fields(row, hsnp_record):
            """Update the basic traffic fields in the row."""
            val_dir = hsnp_record['VAL_DIR']
            if val_dir == 2:
                row[field_idxs["FT_Weekday"]] = hsnp_record["SPWEEKDAY"]
                row[field_idxs["FT_Weekend"]] = hsnp_record["SPWEEKEND"]
                row[field_idxs["FT_AllWeek"]] = hsnp_record["SPWEEK"]
            elif val_dir == 3:
                row[field_idxs["TF_Weekday"]] = hsnp_record["SPWEEKDAY"]
                row[field_idxs["TF_Weekend"]] = hsnp_record["SPWEEKEND"]
                row[field_idxs["TF_AllWeek"]] = hsnp_record["SPWEEK"]
            return row

        def calc_traffic_minutes_fields(row):
            """Calculate the FT_*Minutes and TF_*Minutes traffic fields."""
            meters = row[field_idxs["METERS"]]
            kph = row[field_idxs["KPH"]]
            for trf_fld in self.historical_traffic_fields:
                row[field_idxs[f"FT_{trf_fld}Minutes"]] = calc_minutes(
                    row[field_idxs[f"FT_{trf_fld}"]], meters, kph)
                row[field_idxs[f"TF_{trf_fld}Minutes"]] = calc_minutes(
                    row[field_idxs[f"TF_{trf_fld}"]], meters, kph)
            return row

        def calc_ltr_fields(row, ltr_record):
            preferred = ltr_record['PREFERRED']
            restricted = ltr_record['RESTRICTED']
            if preferred == 1:
                row[field_idxs["NationalSTAARoute"]] = "Y"
            elif preferred == 2:
                row[field_idxs["NationalRouteAccess"]] = "Y"
            elif preferred == 3:
                row[field_idxs["DesignatedTruckRoute"]] = "Y"
            elif preferred == 4:
                row[field_idxs["TruckBypassRoad"]] = "Y"
            if restricted == 1:
                row[field_idxs["NoCommercialVehicles"]] = "Y"
            elif restricted == 2:
                row[field_idxs["ImmediateAccessOnly"]] = "Y"
            elif restricted == 3:
                row[field_idxs["TrucksRestricted"]] = "Y"
            return row

        def calc_logistics_restr_field(lrs_record):
            """Determine the logistics restriction field being calculated and is value."""
            restrtyp = lrs_record["RESTRTYP"]
            value = None
            if restrtyp in ["!A", "!B", "!C", "!D", "!E", "!F"]:
                if lrs_record["UNIT_MEAS"] == 7:
                    value = lrs_record["LIMIT"]
                elif lrs_record["UNIT_MEAS"] == 3:
                    value = lrs_record["LIMIT"] / 0.90718474
            elif restrtyp in ["!G", "!H", "!I", "!J", "!K", "!L", "!M", "!N", "!O", "!P"]:
                if lrs_record["UNIT_MEAS"] == 9:
                    value = lrs_record["LIMIT"]
                elif lrs_record["UNIT_MEAS"] == 8:
                    value = lrs_record["LIMIT"] / 12
                elif lrs_record["UNIT_MEAS"] == 5:
                    value = lrs_record["LIMIT"] / 0.3048
                elif lrs_record["UNIT_MEAS"] == 4:
                    value = lrs_record["LIMIT"] / 30.48
            elif restrtyp.startswith("@"):
                value = "Y"

            return value

        # Iterate through the streets table and populate the TOLLRDDIR field, the restriction fields based on the values
        # in the restrictions table and, optionally, the historical traffic fields based on the values in the historical
        # traffic profiles table.
        with arcpy.da.UpdateCursor(self.streets, fields) as cur:
            for row in cur:
                # Initialize the new row to be the same as the original
                updated_row = list(row)

                # Calculate the value of the TOLLRDDIR field based on the value of TOLLRD
                updated_row = calc_tollrddir(updated_row)

                # The ID fields are stored as doubles in the table because they're too large for 32-bit int fields in
                # the gdb. convert to an int64 for easy retrieval from the associated pandas dataframe.
                id = np.int64(row[0])

                # Populate the basic restrictions fields
                try:
                    # Retrieve the restriction records for this ID
                    subset_df = r_df_streets_subset.loc[id]
                    # Loop through all records associated with this ID and update the appropriate restriction fields
                    if isinstance(subset_df, pd.Series):
                        # There was only one record with this ID, so pandas returns a series
                        updated_row = calc_restr_field(updated_row, subset_df['DIR_POS'], subset_df["VT"])
                    else:
                        # There were multiple records with this ID, so pandas returns a dataframe.
                        for _, record in subset_df.iterrows():
                            updated_row = calc_restr_field(updated_row, record['DIR_POS'], record["VT"])
                except KeyError:
                    # There were no restriction records for this ID. Just skip it and move on.
                    pass

                # Populate the historical traffic fields
                if self.include_historical_traffic:
                    # Calculate the FT_ and TF_ fields
                    try:
                        # Retrieve the historical traffic records for this ID
                        subset_df = hsnp_df.loc[id]
                        # Loop through all records associated with this ID and update the appropriate traffic fields
                        if isinstance(subset_df, pd.Series):
                            # There was only one record with this ID, so pandas returns a series
                            updated_row = calc_basic_traffic_fields(updated_row, subset_df)
                        else:
                            # There were multiple records with this ID, so pandas returns a dataframe.
                            for _, record in subset_df.iterrows():
                                updated_row = calc_basic_traffic_fields(updated_row, record)
                    except KeyError:
                        # There were no historical traffic records for this ID. Just skip it and move on
                        pass

                    # Calculate the FT_*Minutes and TF_*Minutes traffic fields
                    updated_row = calc_traffic_minutes_fields(updated_row)

                # Populate the logistics truck route restriction fields
                if self.in_data_object.ltr:
                    try:
                        # Retrieve the restriction records for this ID
                        subset_df = ltr_df.loc[id]
                        # Loop through the LTR records and update the appropriate rows
                        if isinstance(subset_df, pd.Series):
                            # There was only one record with this ID, so pandas returns a series
                            updated_row = calc_ltr_fields(updated_row, subset_df)
                        else:
                            # There were multiple records with this ID, so pandas returns a dataframe.
                            for _, record in subset_df.iterrows():
                                updated_row = calc_ltr_fields(updated_row, record)
                    except KeyError:
                        # There were no ltr restriction records for this ID. Just skip it and move on.
                        pass

                # Populate the MultiNet Logistics restriction fields
                if self.include_logistics:
                    try:
                        # Retrieve the logistics lrs records for this ID
                        subset_df = self.lrs_df.loc[id]
                        # Loop through all records associated with this ID and update the appropriate logistics fields
                        if isinstance(subset_df, pd.Series):
                            # There was only one record with this ID, so pandas returns a series
                            field_name = subset_df["FieldName"]
                            value = calc_logistics_restr_field(subset_df)
                            if field_name and value:
                                updated_row[field_idxs[field_name]] = value
                        else:
                            # There were multiple records with this ID, so pandas returns a dataframe.
                            for _, record in subset_df.iterrows():
                                field_name = record["FieldName"]
                                value = calc_logistics_restr_field(record)
                                if field_name and value:
                                    updated_row[field_idxs[field_name]] = value
                    except KeyError:
                        # There were no logistics records for this ID. Just skip it and move on
                        pass

                # Update the row in the Streets table
                cur.updateRow(updated_row)

    @timed_exec
    def _generate_turn_features(self):
        """Generate the turn features and insert them into the turn feature class."""
        self._add_message("Populating turn feature class...")
        assert self.streets_df is not None
        assert self.mp_df is not None
        assert self.r_df is not None

        # Subset the restrictions table to include only the restriction type we care about
        r_df_turns_subset = self.r_df[self.r_df["FEATTYP"].isin([2101, 2103])]

        # Create a list of turn fields based on the max turn edges and standard turn feature class schema
        turn_fields = ["SHAPE@", "ID", "Edge1End"]
        for idx in range(1, self.max_turn_edges + 1):
            turn_fields += [f"Edge{idx}FCID", f"Edge{idx}FID", f"Edge{idx}Pos"]
        # Add restriction fields
        turn_fields += self.restriction_field_names
        restr_idxs = {name: self.restriction_field_names.index(name) for name in self.restriction_field_names}

        def calc_restr_field(restriction_values, restrtyp, vt):
            """Calculate restriction field values."""
            restr_field = self.vt_field_map[vt]
            restriction_values[restr_idxs[restr_field]] = "Y"
            if restrtyp == "8I":
                restriction_values[restr_idxs["AllVehicles_Restricted"]] = "Y"
            return restriction_values

        # Open an insert cursor so we can add entries to the turn feature class. We will build the rows below.
        with arcpy.da.InsertCursor(self.turns, turn_fields) as cur_t:

            # Loop through the manuever geometry table and generate a turn feature for each record
            where = f"FEATTYP IN ({', '.join([str(feattyp) for feattyp in [2101, 2103]])})"
            turn_oid = 1
            for row in arcpy.da.SearchCursor(self.in_data_object.mn, ["ID", "JNCTID"], where):
                id_dbl = row[0]
                # Cast the ID field to int64 for lookups and indexing
                id = np.int64(id_dbl)
                jnctid = row[1]

                # Generate the values for the turn edge fields and create the turn geometry
                edge_fields = []  # Store the Edge#FCID, Edge#FID, Edge#Pos fields to insert
                edge1_end = "?"  # Default to ?, which indicates a data error. Will be overwritten below.
                edge_geom = []  # Store the polyline geometry of the edges participating in the turn
                num_edges = 0  # Count the number of edges participating
                try:
                    # Retrieve the turn records for this ID from the maneuver path table sorted by sequence
                    # subset_df = self.grouped_mp_df.get_group(id).sort_values("SEQNR")
                    subset_df = self.mp_df.loc[id]
                except KeyError:
                    # There were no records in the maneuver path table for this entry in the maneuver geometry feature
                    # class. This is a data error. Just move on to the next one.
                    arcpy.AddWarning((
                        f"There were no records in the maneuver path table for ID {id_dbl}, which appears in the "
                        "maneuver geometry feature class."
                    ))
                    continue
                if isinstance(subset_df, pd.Series):
                    # There was only one record with this ID, so pandas returns a series. This is invalid, as all turns
                    # must have more than one edge.
                    arcpy.AddWarning((
                        f"The turn with {id_dbl} in the maneuver paths table has only one associated edge."
                    ))
                    continue
                # Loop through all manuever path records associated with this ID and generate the edge fields
                # Also query the streets table to get the edge geometry to build the geometry for the turn
                valid = True
                subset_df = subset_df.sort_values("SEQNR")
                for _, record in subset_df.iterrows():
                    if num_edges >= self.max_turn_edges:
                        # This should technically never happen because the turn feature class is explicitly created to
                        # allow the maximum number of edges found in the input data. However, check just to be safe.
                        arcpy.AddWarning((
                            f"The turn with {id_dbl} in the maneuver paths table has more associated edges than the "
                            f"maximum allowed edges for a turn ({self.max_turn_edges}) and will be truncated."
                        ))
                        break
                    street_id = record["TRPELID"]
                    try:
                        # Find the street record associated with this edge in the turn manuever path
                        street = self.streets_df.loc[street_id]
                    except KeyError:
                        arcpy.AddWarning((
                            f"The Streets table is missing an entry with ID {street_id}, which is used in the manuever "
                            "path table."))
                        valid = False
                        break
                    # Construct the edge fields for this segment
                    edge_fields += [self.fc_id, street["OID"], self.edge_pos]
                    num_edges += 1
                    # Store the geometry for this segment
                    edge_geom.append(self._get_street_geometry(street_id, street["OID"]))
                    # For the first edge in the sequence, determine the value of the Edge1End field
                    if num_edges == 1:
                        if jnctid == street["F_JNCTID"]:
                            edge1_end = "N"
                        elif jnctid == street["T_JNCTID"]:
                            edge1_end = "Y"
                if not valid:
                    # Something went wrong in constructing the turn. Skip it and move on.
                    continue
                # Add empty records for the remaining turn edge fields if this turn doesn't use the max available
                for _ in range(self.max_turn_edges - num_edges):
                    edge_fields += [None, None, None]

                # Build turn geometry
                turn_geom = self._build_turn_geometry(edge_geom, edge1_end, turn_oid)

                # Generate the values for the restriction fields
                # Initialize them all to None. We'll update them if relevant.
                restriction_values = [None for _ in self.restriction_field_names]
                try:
                    # Populate the basic restrictions fields
                    subset_df = r_df_turns_subset.loc[id]
                    # Loop through all records associated with this ID and update the appropriate restriction fields
                    if isinstance(subset_df, pd.Series):
                        # There was only one record with this ID, so pandas returns a series
                        restriction_values = calc_restr_field(
                            restriction_values, subset_df['RESTRTYP'], subset_df["VT"])
                    else:
                        # There were multiple records with this ID, so pandas returns a dataframe.
                        for _, record in subset_df.iterrows():
                            restriction_values = calc_restr_field(
                                restriction_values, record['RESTRTYP'], record["VT"])
                except KeyError:
                    # There were no records in the restrictions table for this ID. Just leave them as default.
                    pass

                # Construct the final row and insert it
                turn_row = [turn_geom, id_dbl, edge1_end] + edge_fields + restriction_values
                cur_t.insertRow(turn_row)
                turn_oid += 1

    @timed_exec
    def _create_and_populate_road_forks(self):
        """Populate the road splits table."""
        self._add_message("Creating and populating road forks table...")
        assert self.streets_df is not None
        assert self.mp_df is not None

        # Create the table
        road_splits_fields = self._create_road_forks_table()

        # Open an insert cursor so we can add entries to the road splits table. We will build the rows below.
        with arcpy.da.InsertCursor(self.road_splits, road_splits_fields) as cur_rs:

            # Loop through the manuever geometry table and generate a turn feature for each record
            for row in arcpy.da.SearchCursor(self.in_data_object.mn, ["ID", "JNCTID"], "FEATTYP = 9401"):
                id_dbl = row[0]
                # Cast the ID field to int64 for lookups and indexing
                id = np.int64(id_dbl)
                jnctid = row[1]

                # Generate the values for the road fork fields
                new_row = [id_dbl]
                try:
                    # Retrieve the turn records for this ID from the maneuver path table sorted by sequence
                    subset_df = self.mp_df.loc[id]
                except KeyError:
                    # There were no records in the maneuver path table for this entry in the maneuver geometry feature
                    # class. This is a data error. Just move on to the next one.
                    arcpy.AddWarning((
                        f"There were no records in the maneuver path table for ID {id_dbl}, which appears in the "
                        "maneuver geometry feature class."
                    ))
                    continue
                if isinstance(subset_df, pd.Series):
                    # There was only one record with this ID, so pandas returns a series. This is invalid, as all
                    # signposts must have at least three maneuvers.
                    arcpy.AddWarning(f"The road fork maneuver path for ID {id_dbl} has too few maneuvers.")
                    continue
                # Loop through all manuever path records associated with this ID and generate the edge fields
                # Also query the streets table to get the edge geometry to build the geometry for the turn
                seqnr = []
                subset_df = subset_df.sort_values("SEQNR")
                valid = True
                for _, record in subset_df.iterrows():
                    if len(seqnr) >= self.max_road_splits:
                        # This is a rare case where the data includes entries with MP.SEQNR=5 or more, or at least we
                        # have more than four parts to the fork. These entries should be ignored, as we don't support
                        # reporting 4-way (or more) forks. Truncate the road fork record and throw a warning.
                        arcpy.AddWarning((
                            f"The maneuver path with ID {id_dbl}, has more than {self.max_road_splits} parts. Because "
                            f"the network dataset does not support more than {self.max_road_splits} parts, the road "
                            "fork record will be truncated."
                        ))
                        break
                    street_id = record["TRPELID"]
                    seqnr.append(record["SEQNR"])
                    try:
                        # Find the street record associated with this edge in the road fork manuever path
                        street = self.streets_df.loc[street_id]
                    except KeyError:
                        arcpy.AddWarning((
                            f"The Streets table is missing an entry with ID {street_id}, which is used in the manuever "
                            "path table."))
                        valid = False
                        break
                    if jnctid == street["F_JNCTID"]:
                        if len(seqnr) == 1:
                            from_pos = 1
                            to_pos = 0
                        else:
                            from_pos = 0
                            to_pos = 1
                    elif jnctid == street["T_JNCTID"]:
                        if len(seqnr) == 1:
                            from_pos = 0
                            to_pos = 1
                        else:
                            from_pos = 1
                            to_pos = 0
                    else:
                        arcpy.AddWarning((
                            f"The maneuver geometry table's JNCTID field value {jnctid} for ID {id_dbl} does not "
                            f"match F_JNCTID ({street['F_JNCTID']}) or T_JNCTID ({street['T_JNCTID']}) in the Streets "
                            "table."
                        ))
                        valid = False
                        break
                    new_row += [self.fc_id, street["OID"], from_pos, to_pos]

                if not valid:
                    # Something went wrong in constructing the road fork. Skip it and move on.
                    continue

                # Check that the fork had enough edges to be valid
                if len(seqnr) < 3:
                    arcpy.AddWarning(f"The road fork maneuver path for ID {id_dbl} has too few maneuvers.")
                    continue

                # Check that the SEQNR values were sequential integers starting at 1.
                if seqnr != list(range(1, len(seqnr) + 1)):
                    arcpy.AddWarning((
                        f"The SEQNR values in the maneuver path table for ID {id_dbl} are not sequential integers "
                        "starting at 1."
                    ))
                    continue

                # Add empty records for the remaining road fork fields if this fork doesn't use the max available
                for _ in range(self.max_road_splits - len(seqnr)):
                    new_row += [None, None, None, None]

                # Insert the road fork row
                cur_rs.insertRow(new_row)

    @timed_exec
    def _populate_signposts_and_signposts_streets(self):
        """Populate the Signposts feature class."""
        self._add_message("Populating Signposts feature class and Signposts_Streets table...")
        assert self.streets_df is not None

        # Read the sp table into a dataframe
        fields = ["ID", "TRPELID", "SEQNR"]
        with arcpy.da.SearchCursor(self.in_data_object.sp, fields) as cur:
            # Explicitly read it in using int64 to convert the double-based ID field for easy indexing and lookups
            sp_df = pd.DataFrame(cur, columns=fields, dtype=np.int64)
        # Group the sp_df by ID for quick lookups
        grouped_sp_df = sp_df.groupby(["ID"], sort=True)

        # Read the si table into a dataframe
        fields = ["ID", "INFOTYP", "TXTCONT", "TXTCONTLC", "CONTYP", "SEQNR", "DESTSEQ", "RNPART"]
        with arcpy.da.SearchCursor(self.in_data_object.si, fields) as cur:
            si_df = pd.DataFrame(cur, columns=fields)
        # Cast the ID column from its original double to an explicit int64 so we can use it for indexing and lookups
        si_df = si_df.astype({"ID": np.int64})
        # Index the dataframe by ID for quick retrieval later, and sort the index to make those lookups even faster
        si_df.set_index("ID", inplace=True)
        si_df.sort_index(inplace=True)

        def calc_si_fields(si_record, exit_name, toward_fields, branch_fields):
            """Update signpost-related fields."""
            info_typ = si_record["INFOTYP"]
            txt_cont = si_record["TXTCONT"]
            if info_typ == "4E":
                exit_name = txt_cont
            elif info_typ in ["9D", "4I"]:
                lang = LNG_CODES.get(si_record["TXTCONTLC"], "")
                toward_fields += [txt_cont, lang]
            elif info_typ in ["6T", "RN"]:
                lang = LNG_CODES.get(si_record["TXTCONTLC"], "")
                if si_record["CONTYP"] == 2:
                    toward_fields += [txt_cont, lang]
                else:
                    branch_fields += [txt_cont, None, lang]
            return exit_name, toward_fields, branch_fields

        try:
            # Must open an edit session because we're writing to more than one gdb item at once.
            edit = arcpy.da.Editor(os.path.join(self.out_folder, self.gdb_name))
            # Set with_undo=False to enhance performance
            # Set multiuser_mode=False to make it work on SDE (unversioned only)
            edit.startEditing(with_undo=False, multiuser_mode=False)
            edit.startOperation()

            # Create a list of signposts fields
            signpost_fields = ["SHAPE@", "ExitName"]
            for i in range(self.max_signpost_branches):
                signpost_fields += [f"Branch{i}", f"Branch{i}Dir", f"Branch{i}Lng"]
            for i in range(self.max_signpost_branches):
                signpost_fields += [f"Toward{i}", f"Toward{i}Lng"]
            # Open an insert cursor so we can add entries to the signposts feature class. We will build the rows below.
            with arcpy.da.InsertCursor(self.signposts, signpost_fields) as cur_sp:

                # Open an insert cursor so we can add entries to the signposts_streets table. We will generate entries
                # while building the signpost geometry from the streets based on records in the input signposts table.
                si_fields = ["SignpostID", "Sequence", "EdgeFCID", "EdgeFID", "EdgeFrmPos", "EdgeToPos"]
                with arcpy.da.InsertCursor(self.signposts_streets, si_fields) as cur_si:

                    # For each ID in the input sp table, grab the records and build the geometry
                    signpost_oid = 1
                    for id, group in grouped_sp_df:
                        if isinstance(id, tuple):
                            # In newer versions of pandas, groupby keys come back as tuples, so just get the first item
                            # in the tuple
                            id = id[0]
                        exit_name = None
                        branch_fields = []
                        toward_fields = []

                        # Get the records for this ID from the input si table to populate the text fields
                        try:
                            # Retrieve the records for this ID from the sign info table sorted by relevant fields
                            subset_df = si_df.loc[id]
                        except KeyError:
                            # There were no records in the sign info table for this entry in the sign path table. This
                            # is a data error. Just move on to the next one.
                            arcpy.AddWarning((
                                f"There were no records in the sign info table for ID {float(id)}, which appears in "
                                "the sign path table."
                            ))
                            continue
                        if isinstance(subset_df, pd.Series):
                            # There was only one record with this ID, so pandas returns a series
                            exit_name, toward_fields, branch_fields = calc_si_fields(
                                subset_df, exit_name, toward_fields, branch_fields)
                        else:
                            subset_df = subset_df.sort_values(["SEQNR", "DESTSEQ", "RNPART"])
                            # There were multiple records with this ID, so pandas returns a dataframe.
                            for _, record in subset_df.iterrows():
                                exit_name, toward_fields, branch_fields = calc_si_fields(
                                    record, exit_name, toward_fields, branch_fields)

                        # Truncate the record if we have too many branch and toward fields
                        truncate = False
                        if len(branch_fields) > 3 * self.max_signpost_branches:
                            truncate = True
                            branch_fields = branch_fields[:3 * self.max_signpost_branches]
                        if len(toward_fields) > 2 * self.max_signpost_branches:
                            truncate = True
                            toward_fields = toward_fields[:2 * self.max_signpost_branches]
                        if truncate:
                            arcpy.AddWarning((
                                f"There were too many records in the sign info table for ID {float(id)}, which appears "
                                f"in the sign path table. The signpost (ObjectID {signpost_oid}) will be truncated."
                            ))

                        # Fill in remaining branch and toward fields with None if the record doesn't use all of them
                        for _ in range(3 * self.max_signpost_branches - len(branch_fields)):
                            branch_fields.append(None)
                        for _ in range(2 * self.max_signpost_branches - len(toward_fields)):
                            toward_fields.append(None)

                        # Query the streets table to get the edge geometry and relevant fields to build the geometry
                        # for the signpost and the fields in the signposts_streets table
                        edge_info = []
                        group.sort_values("SEQNR", inplace=True)
                        valid = True
                        for _, record in group.iterrows():
                            street_id = record["TRPELID"]
                            try:
                                # Find the street record associated with this edge in the turn manuever path
                                street = self.streets_df.loc[street_id]
                            except KeyError:
                                arcpy.AddWarning((
                                    f"The Streets table is missing an entry with ID {street_id}, which is used in the "
                                    "signpost table."))
                                valid = False
                                break
                            # Store the geometry for this segment
                            edge_info.append(
                                (self._get_street_geometry(street_id, street["OID"]), record["SEQNR"], street["OID"]))
                        if not valid:
                            # Something went wrong in constructing the signpost. Skip it and move on.
                            continue
                        if len(edge_info) < 2:
                            arcpy.AddWarning(f"The input signpost paths had fewer than two entries for ID {float(id)}.")
                            continue

                        # Build signpost geometry and populate the associated entries in the Signposts_Streets table
                        signpost_geom = self._build_signpost_geometry(edge_info, signpost_oid, cur_si)

                        # Construct the final signpost row and insert it
                        signpost_row = [signpost_geom, exit_name] + branch_fields + toward_fields
                        cur_sp.insertRow(signpost_row)
                        signpost_oid += 1

            # Stop the editing operation and save edits
            edit.stopOperation()
            edit.stopEditing(True)

        # Deal with terrible problems
        except Exception as ex:  # pylint:disable=broad-except
            # Stop the editing operation and abandon changes
            if 'edit' in locals():
                if edit.isEditing:
                    edit.stopOperation()
                    edit.stopEditing(False)
            # Then pass through the raised exception
            raise ex

    def _build_signpost_geometry(self, edge_info, signpost_oid, cur_si):
        """Create the polyline geometry of a signpost from its component edge segments and add associated records to the
        signposts_streets table.

        edge_info is a list of tuples of (Street geometry, SEQNR, street OID)
        """
        first_row = [signpost_oid, edge_info[0][1], self.fc_id, edge_info[0][2]]
        first_edge = edge_info[0][0]
        second_edge = edge_info[1][0]
        reverse_first = False
        # Determine the correct direction of the first edge by matching it up with the second
        if first_edge.firstPoint.equals(second_edge.firstPoint) or \
                first_edge.firstPoint.equals(second_edge.lastPoint):
            reverse_first = True

        # Trim the first edge to the last 25% of the street feature
        # Then initialize the signpost geometry with the first edge's truncated list of vertices
        # Also append the appropriate values for the EdgeFrmPos and EdgeToPos fields
        if reverse_first:
            first_edge = first_edge.segmentAlongLine(0, 0.25, use_percentage=True)
            first_row += [1, 0]
            signpost_vertices = self._polyline_to_points(first_edge)
            signpost_vertices.reverse()
        else:
            first_edge = first_edge.segmentAlongLine(0.75, 1, use_percentage=True)
            first_row += [0, 1]
            signpost_vertices = self._polyline_to_points(first_edge)

        # Add the first row's record to the Signposts_Streets table
        cur_si.insertRow(first_row)

        # For the remaining edges, explode them to points and determine directionality by checking which end coincides
        # with the geometry of the previous edge.
        geom_error = False
        for idx, edge_item in enumerate(edge_info[1:]):
            new_row = [signpost_oid, edge_item[1], self.fc_id, edge_item[2]]
            edge = edge_item[0]
            if edge.firstPoint.equals(signpost_vertices[-1]):
                # The edge is in the correct order already
                reverse_edge = False
            elif edge.lastPoint.equals(signpost_vertices[-1]):
                # Reverse the order of the points for this edge
                reverse_edge = True
            else:
                # The endpoints of adjacent segments don't match up at all. This is a data error.
                # Don't fail the tool, just continue and build the geometry but indicate the problem with a warning.
                reverse_edge = False
                geom_error = True

            if idx == len(edge_info) - 2:
                # Do special handling of the last edge to trim the geometry to only the first 25%
                if reverse_edge:
                    edge = edge.segmentAlongLine(0.75, 1, use_percentage=True)
                else:
                    edge = edge.segmentAlongLine(0, 0.25, use_percentage=True)

            edge_vertices = self._polyline_to_points(edge)
            if reverse_edge:
                edge_vertices.reverse()
                new_row += [1, 0]
            else:
                new_row += [0, 1]

            # Append the vertices of this edge to our growing list to model the turn
            signpost_vertices += edge_vertices

            # Insert the record into the signposts_streets table
            cur_si.insertRow(new_row)

        if geom_error:
            arcpy.AddWarning((
                f"Signpost geometry may be incorrect for Signpost ObjectID {signpost_oid} because the geometry of "
                "adjacent street segments used to build the signpost geometry did not have coincident endpoints."
            ))

        # Generate a polyline from the ordered vertices
        signpost_vertex_array = arcpy.Array(signpost_vertices)
        return arcpy.Polyline(signpost_vertex_array, self.in_data_object.sr)

    @timed_exec
    def _create_and_build_nd(self):
        """Create the network dataset from the appropriate template and build the network."""
        self._add_message("Creating network dataset...")

        # Choose the correct base template file based on settings
        template_name = f"MultiNet_{self.unit_type.name}"
        if self.include_historical_traffic:
            template_name += "_Traffic"
        if self.in_data_object.ltr:
            template_name += "_LTR"
        template = os.path.join(CURDIR, "NDTemplates", template_name + ".xml")
        assert os.path.exists(template)

        # Update the template with dynamic content for the current situation, if needed. We'll use a temporary template
        # file, which we will delete when we're done with it.
        temp_template = os.path.join(
            arcpy.env.scratchFolder, f"NDTemplate_{uuid.uuid4().hex}.xml")  # pylint: disable=no-member

        # Update the template dynamically to include the TMC table and field name for live traffic, if relevant
        if self.include_historical_traffic and self.in_data_object.rd:
            template = self._update_nd_template_with_live_traffic(template, temp_template)

        # Update the template dynamically to include the Logistics restriction attributes and evaluators
        if self.include_logistics:
            # Read the template xml
            tree = etree.parse(template)
            # Get the max network attribute ID so we can increment it by 1 for each new Logistics attribute
            attr_id = max([
                int(attr.text) for attr in tree.xpath(
                    "/DENetworkDataset/EvaluatedNetworkAttributes/EvaluatedNetworkAttribute/ID"
                )
            ])

            # For each Logistics restriction, read the attribute and evaluator template snippets. Crack open the
            # template snippet files as text to replace placeholders with the specific data for this attribute. Store
            # the attributes and evaluators for later injection into the final template.
            limit_restr_attr_template = os.path.join(
                CURDIR, "NDTemplates", "MultiNet_Logistics_LimitRestriction_Attribute.xml")
            limit_descr_attr_template = os.path.join(
                CURDIR, "NDTemplates", "MultiNet_Logistics_LimitDescriptor_Attribute.xml")
            load_restr_attr_template = os.path.join(
                CURDIR, "NDTemplates", "MultiNet_Logistics_LoadRestriction_Attribute.xml")
            limit_restr_eval_template = os.path.join(
                CURDIR, "NDTemplates", "MultiNet_Logistics_LimitRestriction_Evaluators.xml")
            limit_descr_eval_template = os.path.join(
                CURDIR, "NDTemplates", f"MultiNet_Logistics_LimitDescriptor_Evaluators_{self.unit_type.name}.xml")
            load_restr_eval_template = os.path.join(
                CURDIR, "NDTemplates", "MultiNet_Logistics_LoadRestriction_Evaluators.xml")
            attr_defs = []
            evaluator_defs = []
            for _, record in self.unique_lrs_df.iterrows():

                if record["RESTRTYP"].startswith("!"):  # This is a limit restriction
                    # Configure the restriction attribute
                    attr_id += 1
                    with open(limit_restr_attr_template, "r", encoding="utf-8") as f:
                        raw_xml = f.read()
                        raw_xml = raw_xml.replace("$ID$", str(attr_id))
                        raw_xml = raw_xml.replace("$NAME$", record["RestrictionName"])
                        raw_xml = raw_xml.replace("$RESTRICTIONUSAGEVALUE$", str(record["RestrictionUsage"]))
                        raw_xml = raw_xml.replace("$PARAMETERNAME$", str(record[f"ParameterName{self.unit_type.name}"]))
                    attr_defs.append(etree.fromstring(raw_xml))
                    # Configure the associated descriptor attribute
                    attr_id += 1
                    with open(limit_descr_attr_template, "r", encoding="utf-8") as f:
                        raw_xml = f.read()
                        raw_xml = raw_xml.replace("$ID$", str(attr_id))
                        raw_xml = raw_xml.replace("$NAME$", record["DescriptorName"])
                    attr_defs.append(etree.fromstring(raw_xml))
                    # Configure the restriction's evaluators
                    with open(limit_restr_eval_template, "r", encoding="utf-8") as f:
                        raw_xml = f.read()
                        raw_xml = raw_xml.replace("$NAME$", record["RestrictionName"])
                        raw_xml = raw_xml.replace("$DESCRIPTORNAME$", str(record["DescriptorName"]))
                        raw_xml = raw_xml.replace("$PARAMETERNAME$", str(record[f"ParameterName{self.unit_type.name}"]))
                    evaluator_defs.append(etree.fromstring(raw_xml))
                    # Configure the descriptor's evaluators
                    with open(limit_descr_eval_template, "r", encoding="utf-8") as f:
                        raw_xml = f.read()
                        raw_xml = raw_xml.replace("$NAME$", record["DescriptorName"])
                        raw_xml = raw_xml.replace("$FIELDNAME$", str(record["FieldName"]))
                        metric_multiplier = ""
                        if self.unit_type == UnitType.Metric:
                            # Converts tons to metric tons, feet to meters, etc.
                            metric_multiplier = f" * {record['DescriptorMultiplierMetric']}"
                            raw_xml = raw_xml.replace("$METRICMULTIPLIER$", metric_multiplier)
                    evaluator_defs.append(etree.fromstring(raw_xml))

                elif record["RESTRTYP"].startswith("@"):  # This is a load restriction
                    # Configure the restriction attribute
                    attr_id += 1
                    with open(load_restr_attr_template, "r") as f:
                        raw_xml = f.read()
                        raw_xml = raw_xml.replace("$ID$", str(attr_id))
                        raw_xml = raw_xml.replace("$NAME$", record["RestrictionName"])
                        raw_xml = raw_xml.replace("$RESTRICTIONUSAGEVALUE$", str(record["RestrictionUsage"]))
                    attr_defs.append(etree.fromstring(raw_xml))
                    # Configure the restriction's evaluators
                    with open(load_restr_eval_template, "r") as f:
                        raw_xml = f.read()
                        raw_xml = raw_xml.replace("$NAME$", record["RestrictionName"])
                        raw_xml = raw_xml.replace("$FIELDNAME$", str(record["FieldName"]))
                    evaluator_defs.append(etree.fromstring(raw_xml))

                else:  # Some unknown restriction type. This should never happen because of prior data processing.
                    arcpy.AddWarning(f"Unknown Logistics restriction type: {record['RESTRTYP']}")
                    continue

            # Inject the Logistics restriction and descriptor attribute definitions
            nd_attrs = tree.xpath("/DENetworkDataset/EvaluatedNetworkAttributes/EvaluatedNetworkAttribute")
            last_attr = nd_attrs[-1]
            parent = last_attr.getparent()
            ix = parent.index(last_attr) + 1
            for i, attr in enumerate(attr_defs):
                attr_def = attr.xpath("/DENetworkDataset/EvaluatedNetworkAttribute")[0]
                parent.insert(ix + 1, attr_def)

            # Inject the Logistics restriction and descriptor evaluators
            nd_assignments = tree.xpath("/DENetworkDataset/NetworkAssignments/NetworkAssignment")
            last_assignment = nd_assignments[-1]
            parent = last_assignment.getparent()
            ix = parent.index(last_assignment)
            for attr_evals in evaluator_defs:
                new_attr_assignments = attr_evals.xpath("/DENetworkDataset/NetworkAssignment")
                for assignment in new_attr_assignments:
                    ix += 1
                    parent.insert(ix, assignment)

            # Write out the updated template
            with open(temp_template, 'wb') as f:
                tree.write(f, encoding="utf-8", xml_declaration=False, pretty_print=True)
            template = temp_template

        # Update the template dynamically to include the time zone attribute and its evaluators
        if self.time_zone_type != TimeZoneType.NoTimeZone:
            template = self._update_nd_template_with_time_zone(template, temp_template)

        # Create the network dataset from the template and build it if requested
        self._create_nd_from_template_and_build(template)

        # Clean up temporary edited template if needed
        if os.path.exists(temp_template):
            os.remove(temp_template)

    @staticmethod
    def _calc_tollrddir(tollrd):
        """Calculate the TOLLRDDIR field value based on value of TOLLRD field in Streets."""
        if tollrd in [11, 21]:
            return "B"
        if tollrd in [12, 22]:
            return "FT"
        if tollrd in [13, 23]:
            return "TF"
        return ""


if __name__ == '__main__':
    print("Please run this code via the script tool interface.")
