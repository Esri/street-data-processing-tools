"""Class to process raw TomTom MultiNet data into a network dataset.

   Copyright 2023 Esri
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
import time
import functools
import datetime
import os
import uuid
import enum
import psutil
from lxml import etree
import arcpy

CURDIR = os.path.dirname(os.path.abspath(__file__))

LNG_CODES = {
    "ALB": "sq",  # Albanian
    "ALS": "",  # Alsacian
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
    "LTZ": "lb",  # Letzeburgesch
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
    "UKL": "uk",  # Ukranian (Latin)
    "UKR": "uk",  # Ukranian
    "UND": "",  # Undefined
    "VAL": "ca",  # Valencian
    "VIE": "vi",  # Vietnamese
    "WEL": "cy",  # Welsh
    "WEN": "",  # Sorbian (Other)
}

PRINT_TIMINGS = False  # Set to True to log timings for various methods (primarily for debugging and development)


def timed_exec(func):
    """Measure time in seconds to execute a function.

    This function is meant to be used as a decorator on a function.

    Args:
        func: The decorated function that is being timed
    """
    @functools.wraps(func)
    def wrapper(*args, **kwargs):
        """Wrap the function to be run."""
        # Using an inner function so the timing can happen directly around the function under test.
        def inner_func():
            t0 = time.time()
            return_val = func(*args, **kwargs)
            if PRINT_TIMINGS:
                arcpy.AddMessage(f"Time to run {func.__name__}: {time.time() - t0}")
                arcpy.AddMessage("System memory usage:")
                arcpy.AddMessage(str(psutil.virtual_memory()))
            return return_val

        return inner_func()

    return wrapper


class TimeZoneType(enum.Enum):
    """Defines the time zone type to use."""

    NoTimeZone = 1
    Single = 2
    Table = 3


class UnitType(enum.Enum):
    """Defines whether the units are imperial or metric."""

    Imperial = 1
    Metric = 2


class MultiNetInputData:
    """Defines a collection of MultiNet inputs to process."""

    def __init__(
        self, network_geometry_fc, maneuvers_geometry_fc, maneuver_path_idx_table, sign_info_table, sign_path_table,
        restrictions_table, network_profile_link_table=None, historical_speed_profiles_table=None,
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
        self.required_tables = [self.nw, self.mn, self.mp, self.si, self.sp, self.rs]
        self.required_traffic_tables = [self.hsnp, self.hspr]
        self.required_logistics_tables = [self.lrs, self.lvc]

    def validate_data(self, check_traffic, check_logistics):
        """Validate that the data exists and has the required fields."""
        # Check that all tables that need to be specified are specified.
        for table in self.required_tables:
            if not table:
                arcpy.AddError("Required MultiNet input table not specified.")
                return False
        if check_traffic:
            for table in self.required_traffic_tables:
                if not table:
                    arcpy.AddError("Required MultiNet traffic input table not specified.")
                    return False
        if check_logistics:
            for table in self.required_logistics_tables:
                if not table:
                    arcpy.AddError("Required MultiNet Logistics input table not specified.")
                    return False

        # Verify existence of tables and appropriate schema
        required_fields = {
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
        }
        for table in [t for t in required_fields if t]:
            if not arcpy.Exists(table):
                arcpy.AddError(f"Input table {table} does not exist.")
                return False
            actual_fields = {(f.name, f.type) for f in arcpy.ListFields(table)}
            if not set(required_fields[table]).issubset(actual_fields):
                arcpy.AddError(
                    f"Input table {table} does not have the correct schema. Required fields: {required_fields[table]}")
                return False
            if int(arcpy.management.GetCount(table).getOutput(0)) == 0:
                arcpy.AddWarning(f"Input table {table} has no rows.")

        # Everything is valid
        return True


class MultiNetProcessor:

    def __init__(
        self, out_folder: str, gdb_name: str, in_multinet: MultiNetInputData, unit_type: UnitType,
        include_historical_traffic: bool, include_logistics: bool,
        time_zone_type: TimeZoneType, time_zone_name: str = "", in_time_zone_table=None,
        time_zone_ft_field: str = None, time_zone_tf_field: str = None, build_network: bool = True
    ):
        """Initialize a class to process MultiNet data into a network dataset."""
        self.in_multinet = in_multinet
        self.unit_type = unit_type
        self.include_historical_traffic = include_historical_traffic
        self.include_logistics = include_logistics
        self.time_zone_type = time_zone_type
        self.time_zone_name = time_zone_name
        self.in_time_zone_table = in_time_zone_table
        self.time_zone_ft_field = time_zone_ft_field
        self.time_zone_tf_field = time_zone_tf_field
        self.build_network = build_network

        self.out_folder = out_folder
        self.gdb_name = gdb_name
        if not self.gdb_name.endswith(".gdb"):
            self.gdb_name += ".gdb"
        self.feature_dataset = os.path.join(self.out_folder, self.gdb_name, "Routing")
        self.streets = os.path.join(self.feature_dataset, "Streets")
        self.turns = os.path.join(self.feature_dataset, "RestrictedTurns")
        self.road_splits = os.path.join(self.out_folder, self.gdb_name, "Streets_RoadSplits")
        self.signposts = os.path.join(self.feature_dataset, "Signposts")
        self.signposts_streets = os.path.join(self.out_folder, self.gdb_name, "Signposts_Streets")
        self.streets_profiles = os.path.join(self.out_folder, self.gdb_name, "Streets_DailyProfiles")
        self.profiles = os.path.join(self.out_folder, self.gdb_name, "DailyProfiles")
        self.streets_tmc = os.path.join(self.out_folder, self.gdb_name, "Streets_TMC")
        self.time_zone_table = os.path.join(self.out_folder, self.gdb_name, "TimeZones")
        self.network = os.path.join(self.feature_dataset, "Routing_ND")

        self.out_sr = arcpy.Describe(self.in_multinet.nw).spatialReference

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
        self.streets_df = None  # Dataframe of output streets indexed by ID for quick lookups
        self.streets_oid_field = None  # OID field name of the output streets feature class
        self.lrs_df = None  # Dataframe of logistics LRS table
        self.unique_lrs_df = None  # Dataframe holding unique combinations of logistics restriction data
        self.max_turn_edges = None  # Maximum number of edges participating in a turn
        self.fc_id = None  # Streets feature class dataset ID used in Edge#FCID fields

    def process_multinet_data(self):
        """Process multinet data into a network dataset."""
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
        self._detect_and_delete_duplicate_streets()
        self._populate_streets_fields()
        # We're now done with the Logistics restrictions table, so clear the variable to free up memory
        del self.lrs_df
        self.lrs_df = None

        # Read in output streets for future look-ups
        self._read_and_index_streets()

        # Create and populate the turn feature class
        self._create_turn_fc()
        self._generate_turn_features()
        # We're now done with the restrictions table, so clear the variable to free up memory
        del self.r_df
        self.r_df = None

        # Create and populate the road forks table
        self._create_road_forks_table()
        self._populate_road_forks()
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
            self._create_and_populate_profiles_table()
            self._create_and_populate_streets_tmc_table()
        # We're done with the streets table, so clear the variable to free up memory
        del self.streets_df
        self.streets_df = None

        # Handle time zone table if needed
        if self.time_zone_type != TimeZoneType.NoTimeZone:
            self._handle_time_zone()

        # Create the network dataset from a template and build it
        self._create_and_build_nd()

    @timed_exec
    def _validate_inputs(self):
        """Validate the input data."""
        arcpy.AddMessage("Validating inputs...")

        # Do some simple checks
        if not os.path.exists(self.out_folder):
            arcpy.AddMessage(f"Output folder {self.out_folder} does not exist.")
            return False
        if os.path.exists(os.path.join(self.out_folder, self.gdb_name)):
            arcpy.AddMessage(f"Output geodatabase {os.path.join(self.out_folder, self.gdb_name)} already exists.")
            return False
        if self.out_sr.name == "Unknown":
            arcpy.AddError("The input data has an unknown spatial reference.")
            return False

        # Make sure the license is available.
        if arcpy.CheckExtension("network").lower() == "available":
            arcpy.CheckOutExtension("network")
        else:
            arcpy.AddError("The Network Analyst extension license is unavailable.")
            return False

        # Check the input data
        if not self.in_multinet.validate_data(self.include_historical_traffic, self.include_logistics):
            return False

        arcpy.AddMessage("Inputs validated successfully.")
        return True

    @timed_exec
    def _create_feature_dataset(self):
        """Create the output geodatabase and feature dataset."""
        arcpy.AddMessage(f"Creating output geodatabase and feature dataset at {self.feature_dataset}...")
        arcpy.management.CreateFileGDB(self.out_folder, self.gdb_name)
        arcpy.management.CreateFeatureDataset(
            os.path.dirname(self.feature_dataset),
            os.path.basename(self.feature_dataset),
            self.out_sr
        )

    @timed_exec
    def _copy_streets(self):
        """Copy the network geometry feature class to the target feature dataset and add fields."""
        arcpy.AddMessage("Copying input network geometry feature class to target feature dataset...")

        # Filter out address area boundary elements
        nw_layer = arcpy.management.MakeFeatureLayer(self.in_multinet.nw, "NW layer", "FEATTYP <> 4165").getOutput(0)

        # Construct field mappings to use when copying the original data.
        field_mappings = arcpy.FieldMappings()
        # Add all the fields from the input data
        field_mappings.addTable(self.in_multinet.nw)

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
        if self.in_multinet.ltr:
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
    def _detect_and_delete_duplicate_streets(self):
        """Determine if there are duplicate street IDs, and if so, delete them."""
        # Duplicate street features occur along tile boundaries.
        # Use Pandas to identify duplicate ID values and associated OIDs to delete.
        with arcpy.da.SearchCursor(self.streets, ["OID@", "ID"]) as cur:
            id_df = pd.DataFrame(cur, columns=["OID", "ID"])
        duplicate_streets = id_df[id_df.duplicated(subset="ID")]["OID"].to_list()
        # If there are any duplicates, delete them.
        if duplicate_streets:
            duplicate_streets = [str(oid) for oid in duplicate_streets]
            arcpy.AddMessage("Duplicate streets were detected and will be removed.")
            where = f"{self.streets_oid_field} IN ({', '.join(duplicate_streets)})"
            layer_name = "Temp_Streets"
            arcpy.management.MakeFeatureLayer(self.streets, layer_name, where)
            arcpy.management.DeleteRows(layer_name)

    @timed_exec
    def _create_turn_fc(self):
        """Create the turn feature class and add necessary fields."""
        assert self.max_turn_edges is not None
        arcpy.AddMessage("Creating turn feature class...")
        arcpy.na.CreateTurnFeatureClass(self.feature_dataset, os.path.basename(self.turns), self.max_turn_edges)
        # Add restriction fields
        # The ID field is added to easily relate this back to the original data but is not required by the network.
        field_defs = [["ID", "DOUBLE"]] + [[field, "TEXT", "", 1] for field in self.restriction_field_names]
        arcpy.management.AddFields(self.turns, field_defs)

    @timed_exec
    def _create_road_forks_table(self):
        """Create the road forks table Streets_RoadSplits with the correct schema."""
        arcpy.AddMessage("Creating road forks table...")
        arcpy.management.CreateTable(os.path.dirname(self.road_splits), os.path.basename(self.road_splits))
        # Schema for the road forks table:
        # https://pro.arcgis.com/en/pro-app/latest/tool-reference/data-management/add-fields.htm
        # The ID field is added to easily relate this back to the original data but is not required by the schema.
        field_defs = [
            ["ID", "DOUBLE"],
            ["EdgeFCID", "LONG"],
            ["EdgeFID", "LONG"],
            ["EdgeFrmPos", "DOUBLE"],
            ["EdgeToPos", "DOUBLE"],
            ["Branch0FCID", "LONG"],
            ["Branch0FID", "LONG"],
            ["Branch0FrmPos", "DOUBLE"],
            ["Branch0ToPos", "DOUBLE"],
            ["Branch1FCID", "LONG"],
            ["Branch1FID", "LONG"],
            ["Branch1FrmPos", "DOUBLE"],
            ["Branch1ToPos", "DOUBLE"],
            ["Branch2FCID", "LONG"],
            ["Branch2FID", "LONG"],
            ["Branch2FrmPos", "DOUBLE"],
            ["Branch2ToPos", "DOUBLE"]
        ]
        arcpy.management.AddFields(self.road_splits, field_defs)

    @timed_exec
    def _create_signposts_fc(self):
        """Create the Signposts feature class with correct schema."""
        arcpy.AddMessage("Creating Signposts feature class...")
        arcpy.management.CreateFeatureclass(
            os.path.dirname(self.signposts), os.path.basename(self.signposts),
            "POLYLINE", has_m="DISABLED", has_z="DISABLED"
        )
        # Schema for the signposts feature class:
        # https://pro.arcgis.com/en/pro-app/latest/help/analysis/networks/signposts.htm
        field_defs = [
            ["ExitName", "TEXT", "ExitName", 24],
        ]
        for i in range(10):
            field_defs += [
                [f"Branch{i}", "TEXT", f"Branch{i}", 180],
                [f"Branch{i}Dir", "TEXT", f"Branch{i}Dir", 5],
                [f"Branch{i}Lng", "TEXT", f"Branch{i}Lng", 2],
                [f"Toward{i}", "TEXT", f"Toward{i}", 180],
                [f"Toward{i}Lng", "TEXT", f"Toward{i}Lng", 2],
            ]
        arcpy.management.AddFields(self.signposts, field_defs)

    @timed_exec
    def _create_signposts_streets_table(self):
        """Create the Signposts_Streets table with correct schema."""
        arcpy.AddMessage("Creating Signposts_Streets table...")
        arcpy.management.CreateTable(os.path.dirname(self.signposts_streets), os.path.basename(self.signposts_streets))
        # Schema for the Signposts_Streets table:
        # https://pro.arcgis.com/en/pro-app/latest/help/analysis/networks/signposts.htm
        field_defs = [
            ["SignpostID", "LONG"],
            ["Sequence", "LONG"],
            ["EdgeFCID", "LONG"],
            ["EdgeFID", "LONG"],
            ["EdgeFrmPos", "DOUBLE"],
            ["EdgeToPos", "DOUBLE"]
        ]
        arcpy.management.AddFields(self.signposts_streets, field_defs)

    @timed_exec
    def _create_and_populate_streets_profiles_table(self):
        """Create the Streets_DailyProfiles table."""
        if not self.include_historical_traffic:
            return
        arcpy.AddMessage("Creating and populating Streets_DailyProfiles table...")
        assert self.streets_df is not None  # Confidence check

        # Create the table with desired schema
        arcpy.management.CreateTable(
            os.path.dirname(self.streets_profiles),
            os.path.basename(self.streets_profiles),
            self.in_multinet.hsnp  # Template table used to define schema
        )
        field_defs = [
            ["EdgeFCID", "LONG"],
            ["EdgeFID", "LONG"],
            ["EdgeFrmPos", "DOUBLE"],
            ["EdgeToPos", "DOUBLE"]
        ]
        arcpy.management.AddFields(self.streets_profiles, field_defs)

        # Insert rows
        desc = arcpy.Describe(self.in_multinet.hsnp)
        input_fields = [f.name for f in desc.fields if f.name != desc.OIDFieldName]
        output_fields = input_fields + [f[0] for f in field_defs]
        network_id_idx = input_fields.index("NETWORK_ID")
        val_dir_idx = input_fields.index("VAL_DIR")
        with arcpy.da.InsertCursor(self.streets_profiles, output_fields) as cur:
            for row in arcpy.da.SearchCursor(
                self.in_multinet.hsnp, input_fields, "SPFREEFLOW > 0 And VAL_DIR IN (2, 3)"
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
    def _create_and_populate_profiles_table(self):
        """Create the DailyProfiles table."""
        if not self.include_historical_traffic:
            return
        arcpy.AddMessage("Creating and populating DailyProfiles table...")

        # Create the table with correct schema
        arcpy.management.CreateTable(os.path.dirname(self.profiles), os.path.basename(self.profiles))
        field_defs = [["ProfileID", "SHORT"]]
        added_minutes = 0
        midnight = datetime.datetime(2021, 1, 1, 0, 0, 0)  # Initialize midnight on an arbitrary date
        # Add a field for each 5-minute increment until 11:55 at night
        while added_minutes < 1440:
            current_time = midnight + datetime.timedelta(minutes=added_minutes)
            field_defs.append([f"SpeedFactor_{current_time.strftime('%H%M')}", "FLOAT"])
            added_minutes += 5
        arcpy.management.AddFields(self.profiles, field_defs)

        # Read the Historical Speed Profiles table into a temporary dataframe so we can quickly sort it. Normally we
        # could sort it using the da.SearchCursor's sql clause, but the ORDER BY option doesn't work with shapefile
        # tables.
        fields = ["PROFILE_ID", "TIME_SLOT", "REL_SP"]
        with arcpy.da.SearchCursor(self.in_multinet.hspr, fields) as cur:
            hspr_df = pd.DataFrame(cur, columns=fields)
        hspr_df = hspr_df.sort_values("PROFILE_ID").groupby(["PROFILE_ID"])

        # Insert the rows
        output_fields = [f[0] for f in field_defs]
        with arcpy.da.InsertCursor(self.profiles, output_fields) as cur:
            # Loop through the records in the HSPR table and calculate the SpeedFactor fields accordingly
            for profile_id, group in hspr_df:
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
        if not self.include_historical_traffic or not self.in_multinet.rd:
            return
        arcpy.AddMessage("Creating and populating Streets_TMC table...")
        assert self.streets_df is not None  # Confidence check

        arcpy.management.CreateTable(os.path.dirname(self.streets_tmc), os.path.basename(self.streets_tmc))
        field_defs = [
            ["ID", "DOUBLE"],
            ["TMC", "TEXT", "TMC", 9],
            ["EdgeFCID", "LONG"],
            ["EdgeFID", "LONG"],
            ["EdgeFrmPos", "DOUBLE"],
            ["EdgeToPos", "DOUBLE"]
        ]
        arcpy.management.AddFields(self.streets_tmc, field_defs)

        with arcpy.da.InsertCursor(self.streets_tmc, [f[0] for f in field_defs]) as cur:
            for row in arcpy.da.SearchCursor(self.in_multinet.rd, ["ID", "RDSTMC"]):
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
        arcpy.AddMessage("Reading and grouping restrictions table...")
        where = f"VT IN ({', '.join([str(vt) for vt in self.vt_field_map])})"
        fields = ["ID", "FEATTYP", "VT", "DIR_POS", "RESTRTYP"]
        with arcpy.da.SearchCursor(self.in_multinet.rs, fields, where) as cur:
            self.r_df = pd.DataFrame(cur, columns=fields)
        # Cast the ID column from its original double to an explicit int64 so we can use it for indexing and lookups
        self.r_df = self.r_df.astype({"ID": np.int64})
        # Index the dataframe by ID for quick retrieval later, and sort the index to make those lookups even faster
        self.r_df.set_index("ID", inplace=True)
        self.r_df.sort_index(inplace=True)

    @timed_exec
    def _read_and_index_maneuver_paths(self):
        """Read in the maneuver paths table and index it for quick lookups."""
        arcpy.AddMessage("Reading and grouping maneuver paths table...")
        fields = ["ID", "TRPELID", "SEQNR"]
        with arcpy.da.SearchCursor(self.in_multinet.mp, fields) as cur:
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
        with arcpy.da.SearchCursor(self.in_multinet.hsnp, fields, "VAL_DIR IN (2, 3)") as cur:
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
        with arcpy.da.SearchCursor(self.in_multinet.lvc, fields) as cur:
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
        with arcpy.da.SearchCursor(self.in_multinet.lrs, fields, where) as cur:
            self.lrs_df = pd.DataFrame(cur, columns=fields)
        # Cast the ID field from its original double to an int64 for lookups and indexing
        self.lrs_df = self.lrs_df.astype({"ID": np.int64})
        self.lrs_df.set_index(["ID", "SEQNR"], inplace=True)

        # Join LVC to LRS and drop any rows that had a match and got transferred to LRS
        self.lrs_df = self.lrs_df.join(lvc_df, how="left")
        self.lrs_df = self.lrs_df[self.lrs_df["DROP"] is not True]
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
        if not self.in_multinet.ltr:
            return None
        fields = ["ID", "PREFERRED", "RESTRICTED"]
        with arcpy.da.SearchCursor(self.in_multinet.ltr, fields) as cur:
            # Explicitly read it in using int64 to convert the double-based ID field for easy indexing and lookups
            ltr_df = pd.DataFrame(cur, columns=fields, dtype=np.int64)
        # Index the dataframe by ID for quick retrieval later, and sort the index to make those lookups even faster
        ltr_df.set_index("ID", inplace=True)
        ltr_df.sort_index(inplace=True)
        return ltr_df

    @timed_exec
    def _read_and_index_streets(self):
        """Read in the streets table and index it for quick lookups."""
        arcpy.AddMessage("Reading and indexing Streets table...")
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
            arcpy.AddMessage("Populating restriction fields in streets...")
        else:
            arcpy.AddMessage("Populating restriction and historical traffic fields in streets...")
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
        if self.in_multinet.ltr:
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
                if self.in_multinet.ltr:
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

    def _get_street_geometry(self, street_id, oid):
        """Return the geometry of the designated street feature."""
        # Check to see if the geometry for this street is already stored
        geom = self.streets_df.at[street_id, "SHAPE"]
        if geom is None:
            # If it's not stored already, get it from the dataset and store it in case it gets queried again
            with arcpy.da.SearchCursor(self.streets, ["SHAPE@"], f"{self.streets_oid_field} = {oid}") as cur:
                geom = next(cur)[0]
            self.streets_df.at[street_id, "SHAPE"] = geom
        return geom

    @timed_exec
    def _generate_turn_features(self):
        """Generate the turn features and insert them into the turn feature class."""
        arcpy.AddMessage("Populating turn feature class...")
        assert self.streets_df is not None
        assert self.mp_df is not None
        assert self.r_df is not None

        # Edge#Pos field values are intentionally hard-coded to 0.5
        edge_pos = 0.5

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
            for row in arcpy.da.SearchCursor(self.in_multinet.mn, ["ID", "JNCTID"], where):
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
                    edge_fields += [self.fc_id, street["OID"], edge_pos]
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

    def _build_turn_geometry(self, edge_segments, edge1_end, turn_oid):
        """Create the polyline geometry of a turn from its component edge segments."""
        # Get the starting edge and determine its direction based on the edge1_end field value
        first_edge = edge_segments[0]
        reverse_first = edge1_end == "N"
        if edge1_end == "?":
            # Handle the data error case where the edge direction couldn't be determined by checking if one end matches
            # one of the ends of the second edge
            second_edge = edge_segments[1]
            if first_edge.firstPoint.equals(second_edge.firstPoint) or \
                    first_edge.firstPoint.equals(second_edge.lastPoint):
                reverse_first = True

        # Trim the first edge to the last 30% of the street feature
        if reverse_first:
            first_edge = first_edge.segmentAlongLine(0, 0.3, use_percentage=True)
        else:
            first_edge = first_edge.segmentAlongLine(0.7, 1, use_percentage=True)

        # Convert the now-trimmed first edge to points and reverse them if needed
        turn_vertices = self._polygon_to_points(first_edge)
        if reverse_first:
            turn_vertices.reverse()

        # For the remaining edges, explode them to points and determine directionality by checking which end coincides
        # with the geometry of the previous edge.
        geom_error = False
        for idx, edge in enumerate(edge_segments[1:]):
            if edge.firstPoint.equals(turn_vertices[-1]):
                # The edge is in the correct order already
                reverse_edge = False
            elif edge.lastPoint.equals(turn_vertices[-1]):
                # Reverse the order of the points for this edge
                reverse_edge = True
            else:
                # The endpoints of adjacent segments don't match up at all. This is a data error.
                # Don't fail the tool, just continue and build the geometry but indicate the problem with a warning.
                reverse_edge = False
                geom_error = True

            if idx == len(edge_segments) - 2:
                # Do special handling of the last edge to trim the geometry to only the first 30%
                if reverse_edge:
                    edge = edge.segmentAlongLine(0.7, 1, use_percentage=True)
                else:
                    edge = edge.segmentAlongLine(0, 0.3, use_percentage=True)

            edge_vertices = self._polygon_to_points(edge)
            if reverse_edge:
                edge_vertices.reverse()
            # Append the vertices of this edge to our growing list to model the turn
            turn_vertices += edge_vertices

        if geom_error:
            arcpy.AddWarning((
                f"Turn geometry may be incorrect for turn ObjectID {turn_oid} because the geometry of adjacent street "
                "segments used to build the turn geometry did not have coincident endpoints."
            ))

        # Generate a polyline from the ordered vertices
        turn_vertex_array = arcpy.Array(turn_vertices)
        return arcpy.Polyline(turn_vertex_array, self.out_sr)

    @timed_exec
    def _populate_road_forks(self):
        """Populate the road splits table."""
        arcpy.AddMessage("Populating road forks table...")
        assert self.streets_df is not None
        assert self.mp_df is not None

        # Open an insert cursor so we can add entries to the road splits table. We will build the rows below.
        road_splits_fields = [
            "ID",
            "EdgeFCID",
            "EdgeFID",
            "EdgeFrmPos",
            "EdgeToPos",
            "Branch0FCID",
            "Branch0FID",
            "Branch0FrmPos",
            "Branch0ToPos",
            "Branch1FCID",
            "Branch1FID",
            "Branch1FrmPos",
            "Branch1ToPos",
            "Branch2FCID",
            "Branch2FID",
            "Branch2FrmPos",
            "Branch2ToPos",
        ]
        max_road_splits = 4
        with arcpy.da.InsertCursor(self.road_splits, road_splits_fields) as cur_rs:

            # Loop through the manuever geometry table and generate a turn feature for each record
            for row in arcpy.da.SearchCursor(self.in_multinet.mn, ["ID", "JNCTID"], "FEATTYP = 9401"):
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
                    if len(seqnr) >= max_road_splits:
                        # This is a rare case where the data includes entries with MP.SEQNR=5 or more, or at least we
                        # have more than four parts to the fork. These entries should be ignored, as we don't support
                        # reporting 4-way (or more) forks. Truncate the road fork record and throw a warning.
                        arcpy.AddWarning((
                            f"The maneuver path with ID {id_dbl}, has more than {max_road_splits} parts. Because the "
                            f"network dataset does not support more than {max_road_splits} parts, the road fork record "
                            "will be truncated."
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
                for _ in range(max_road_splits - len(seqnr)):
                    new_row += [None, None, None, None]

                # Insert the road fork row
                cur_rs.insertRow(new_row)

        # Add an attribute index
        arcpy.management.AddIndex(self.road_splits, ["EdgeFCID"], "EdgeFCIDIdx")
        arcpy.management.AddIndex(self.road_splits, ["EdgeFID"], "EdgeFIDIdx")

    @timed_exec
    def _populate_signposts_and_signposts_streets(self):
        """Populate the Signposts feature class."""
        arcpy.AddMessage("Populating Signposts feature class and Signposts_Streets table...")
        assert self.streets_df is not None

        # Read the sp table into a dataframe
        fields = ["ID", "TRPELID", "SEQNR"]
        with arcpy.da.SearchCursor(self.in_multinet.sp, fields) as cur:
            # Explicitly read it in using int64 to convert the double-based ID field for easy indexing and lookups
            sp_df = pd.DataFrame(cur, columns=fields, dtype=np.int64)
        # Group the sp_df by ID for quick lookups
        grouped_sp_df = sp_df.groupby(["ID"], sort=True)

        # Read the si table into a dataframe
        fields = ["ID", "INFOTYP", "TXTCONT", "TXTCONTLC", "CONTYP", "SEQNR", "DESTSEQ", "RNPART"]
        with arcpy.da.SearchCursor(self.in_multinet.si, fields) as cur:
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
            for i in range(10):
                signpost_fields += [f"Branch{i}", f"Branch{i}Dir", f"Branch{i}Lng"]
            for i in range(10):
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
                        if len(branch_fields) > 30:
                            truncate = True
                            branch_fields = branch_fields[:30]
                        if len(toward_fields) > 20:
                            truncate = True
                            toward_fields = toward_fields[:20]
                        if truncate:
                            arcpy.AddWarning((
                                f"There were too many records in the sign info table for ID {float(id)}, which appears "
                                f"in the sign path table. The signpost (ObjectID {signpost_oid}) will be truncated."
                            ))

                        # Fill in remaining branch and toward fields with None if the record doesn't use all of them
                        for _ in range(30 - len(branch_fields)):
                            branch_fields.append(None)
                        for _ in range(20 - len(toward_fields)):
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

        # Add an attribute index
        arcpy.management.AddIndex(self.signposts_streets, ["SignpostID"], "SignpostIDIdx")
        arcpy.management.AddIndex(self.signposts_streets, ["Sequence"], "SequenceIdx")
        arcpy.management.AddIndex(self.signposts_streets, ["EdgeFCID"], "EdgeFCIDIdx")
        arcpy.management.AddIndex(self.signposts_streets, ["EdgeFID"], "EdgeFIDIdx")

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
            signpost_vertices = self._polygon_to_points(first_edge)
            signpost_vertices.reverse()
        else:
            first_edge = first_edge.segmentAlongLine(0.75, 1, use_percentage=True)
            first_row += [0, 1]
            signpost_vertices = self._polygon_to_points(first_edge)

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

            edge_vertices = self._polygon_to_points(edge)
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
        return arcpy.Polyline(signpost_vertex_array, self.out_sr)

    @timed_exec
    def _handle_time_zone(self):
        """Handle the time zone table."""
        if self.time_zone_type == TimeZoneType.NoTimeZone:
            # Do nothing. No time zones will be used for the network.
            return

        if self.time_zone_type == TimeZoneType.Single:
            # Create a new time zone table with a single row whose value is the specified name
            arcpy.AddMessage("Creating TimeZones table...")
            assert self.time_zone_name
            arcpy.management.CreateTable(os.path.dirname(self.time_zone_table), os.path.basename(self.time_zone_table))
            arcpy.management.AddField(self.time_zone_table, "MSTIMEZONE", "TEXT", field_length=len(self.time_zone_name))
            with arcpy.da.InsertCursor(self.time_zone_table, ["MSTIMEZONE"]) as cur:
                cur.insertRow((self.time_zone_name,))
            return

        if self.time_zone_type == TimeZoneType.Table:
            # Copy the user's time zone table to the network's gdb
            arcpy.AddMessage("Copying TimeZones table...")
            assert self.in_time_zone_table
            assert self.time_zone_ft_field
            assert self.time_zone_tf_field
            arcpy.conversion.TableToTable(
                self.in_time_zone_table, os.path.dirname(self.time_zone_table), os.path.basename(self.time_zone_table))

    @timed_exec
    def _create_and_build_nd(self):
        """Create the network dataset from the appropriate template and build the network."""
        arcpy.AddMessage("Creating network dataset...")

        # Choose the correct base template file based on settings
        template_name = f"MultiNet_{self.unit_type.name}"
        if self.include_historical_traffic:
            template_name += "_Traffic"
        if self.in_multinet.ltr:
            template_name += "_LTR"
        template = os.path.join(CURDIR, "NDTemplates", template_name + ".xml")
        assert os.path.exists(template)

        # Update the template with dynamic content for the current situation, if needed. We'll use a temporary template
        # file, which we will delete when we're done with it.
        temp_template = os.path.join(
            arcpy.env.scratchFolder, f"NDTemplate_{uuid.uuid4().hex}.xml")  # pylint: disable=no-member

        # Update the template dynamically to include the TMC table and field name for life traffic, if relevant
        if self.include_historical_traffic and self.in_multinet.rd:
            # Inject TMC table and field name into the template xml
            # <DynamicTrafficTableName>Streets_TMC</DynamicTrafficTableName>
            # <DynamicTrafficTMCFieldName>TMC</DynamicTrafficTMCFieldName>
            tree = etree.parse(template)
            table_name = tree.xpath("/DENetworkDataset/TrafficData/DynamicTrafficTableName")
            table_name[0].text = "Streets_TMC"
            field_name = tree.xpath("/DENetworkDataset/TrafficData/DynamicTrafficTMCFieldName")
            field_name[0].text = "TMC"
            with open(temp_template, 'wb') as f:
                tree.write(f, encoding="utf-8", xml_declaration=False, pretty_print=True)
            template = temp_template

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
            # The attributes and evaluators for later injection into the final template
            limit_restr_attr_template = os.path.join(
                CURDIR, "NDTemplates", "MultiNet_Logistics_LimitRestriction_Attribute.xml")
            limit_descr_attr_template = os.path.join(
                CURDIR, "NDTemplates", "MultiNet_Logistics_LimitDescriptor_Attribute.xml")
            load_restr_attr_template = os.path.join(
                CURDIR, "NDTemplates", "MultiNet_Logistics_LoadRestriction_Attribute.xml")
            limit_restr_eval_template = os.path.join(
                CURDIR, "NDTemplates", "MultiNet_Logistics_LimitRestriction_Evaluators.xml")
            limit_descr_eval_template = os.path.join(
                CURDIR, "NDTemplates", "MultiNet_Logistics_LimitDescriptor_Evaluators.xml")
            load_restr_eval_template = os.path.join(
                CURDIR, "NDTemplates", "MultiNet_Logistics_LoadRestriction_Evaluators.xml")
            attr_defs = []
            evaluator_defs = []
            for _, record in self.unique_lrs_df.iterrows():

                if record["RESTRTYP"].startswith("!"):  # This is a limit restriction
                    # Configure the restriction attribute
                    attr_id += 1
                    with open(limit_restr_attr_template, "r") as f:
                        raw_xml = f.read()
                        raw_xml = raw_xml.replace("$ID$", str(attr_id))
                        raw_xml = raw_xml.replace("$NAME$", record["RestrictionName"])
                        raw_xml = raw_xml.replace("$RESTRICTIONUSAGEVALUE$", str(record["RestrictionUsage"]))
                        raw_xml = raw_xml.replace("$PARAMETERNAME$", str(record[f"ParameterName{self.unit_type.name}"]))
                    attr_defs.append(etree.fromstring(raw_xml))
                    # Configure the associated descriptor attribute
                    attr_id += 1
                    with open(limit_descr_attr_template, "r") as f:
                        raw_xml = f.read()
                        raw_xml = raw_xml.replace("$ID$", str(attr_id))
                        raw_xml = raw_xml.replace("$NAME$", record["DescriptorName"])
                    attr_defs.append(etree.fromstring(raw_xml))
                    # Configure the restriction's evaluators
                    with open(limit_restr_eval_template, "r") as f:
                        raw_xml = f.read()
                        raw_xml = raw_xml.replace("$NAME$", record["RestrictionName"])
                        raw_xml = raw_xml.replace("$DESCRIPTORNAME$", str(record["DescriptorName"]))
                        raw_xml = raw_xml.replace("$PARAMETERNAME$", str(record[f"ParameterName{self.unit_type.name}"]))
                    evaluator_defs.append(etree.fromstring(raw_xml))
                    # Configure the descriptor's evaluators
                    with open(limit_descr_eval_template, "r") as f:
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
            # Read the template xml
            tree = etree.parse(template)

            # Inject the time zone attribute definition
            # Get the max network attribute ID so we can increment it by 1 for the new time zone attribute
            nd_attr_max_id = max([
                int(attr.text) for attr in tree.xpath(
                    "/DENetworkDataset/EvaluatedNetworkAttributes/EvaluatedNetworkAttribute/ID"
                )
            ])
            # Read the time zone attribute XML from a special template that has only this attribute in it
            time_zone_attribute_template = os.path.join(CURDIR, "NDTemplates", "MultiNet_TimeZone_Attribute.xml")
            tz_attribute_tree = etree.parse(time_zone_attribute_template)
            tz_attr_element = tz_attribute_tree.xpath("/DENetworkDataset/EvaluatedNetworkAttribute")[0]
            # Set the ID to the next available
            tz_attr_id = tz_attribute_tree.xpath("/DENetworkDataset/EvaluatedNetworkAttribute/ID")[0]
            tz_attr_id.text = str(nd_attr_max_id + 1)
            # Inject the time zone attribute into the template's list of attributes
            nd_attrs = tree.xpath("/DENetworkDataset/EvaluatedNetworkAttributes/EvaluatedNetworkAttribute")
            last_attr = nd_attrs[-1]
            parent = last_attr.getparent()
            ix = parent.index(last_attr)
            parent.insert(ix + 1, tz_attr_element)

            # Inject the time zone attribute name and table name into the top-level template code
            # <TimeZoneAttributeName>TimeZoneID</TimeZoneAttributeName>
            # <TimeZoneTableName>TimeZones</TimeZoneTableName>
            root = tree.getroot()
            etree.SubElement(root, "TimeZoneAttributeName").text = "TimeZoneID"
            etree.SubElement(root, "TimeZoneTableName").text = "TimeZones"

            # Inject evaluator information
            # Read the time zone evaluator XML from a special template that has only this info in it
            if self.time_zone_type == TimeZoneType.Single:
                time_zone_evaluator_template = os.path.join(
                    CURDIR, "NDTemplates", "MultiNet_TimeZone_Evaluators_Constant.xml")
                tz_evaluator_tree = etree.parse(time_zone_evaluator_template)
            else:  # TimeZoneType.Table
                time_zone_evaluator_template = os.path.join(
                    CURDIR, "NDTemplates", "MultiNet_TimeZone_Evaluators_Fields.xml")
                # Crack open the file as text to replace field name placeholders with the user's fields
                with open(time_zone_evaluator_template, "r") as f:
                    raw_xml = f.read()
                    raw_xml = raw_xml.replace("$FROMFIELD$", self.time_zone_ft_field)
                    raw_xml = raw_xml.replace("$TOFIELD$", self.time_zone_tf_field)
                # Create XML from it and inject it into the template
                tz_evaluator_tree = etree.fromstring(raw_xml)
            # Inject the time zone attribute evaluators into the template's list of attributes
            new_tz_assignments = tz_evaluator_tree.xpath("/DENetworkDataset/NetworkAssignment")
            nd_assignments = tree.xpath("/DENetworkDataset/NetworkAssignments/NetworkAssignment")
            last_assignment = nd_assignments[-1]
            parent = last_assignment.getparent()
            ix = parent.index(last_assignment)
            for i, assignment in enumerate(new_tz_assignments):
                parent.insert(ix + i, assignment)

            # Write out the updated template
            with open(temp_template, 'wb') as f:
                tree.write(f, encoding="utf-8", xml_declaration=False, pretty_print=True)
            template = temp_template

        # Create the network from the template
        arcpy.nax.CreateNetworkDatasetFromTemplate(template, self.feature_dataset)

        # Clean up temporary edited template if needed
        if os.path.exists(temp_template):
            os.remove(temp_template)

        # Build the network
        if self.build_network:
            arcpy.AddMessage("Building network dataset...")
            arcpy.nax.BuildNetwork(self.network)
            warnings = arcpy.GetMessages(1).splitlines()
            for warning in warnings:
                arcpy.AddWarning(warning)
        else:
            arcpy.AddMessage((
                "Skipping building the network dataset. You must run the Build Network tool on the network dataset "
                "before it can be used for analysis."
            ))

    @staticmethod
    def _polygon_to_points(polygon):
        """Return an ordered list of point objects from the polygon geometry."""
        array = arcpy.Array()
        parts = polygon.getPart()
        for part in parts:
            array.extend(part)
        points = []
        for _ in range(array.count):
            points.append(array.next())
        return points

    @staticmethod
    def _create_string_field_map(field_name, field_type, field_length=1):
        """Return a string, starting with a semicolon, that can be appending to a field mapping string.

        This method is used when copying data with FeatureClassToFeatureClass or TableToTable in order to add new
        fields to the output that are not in the original data. Adding them this way is drastically faster than calling
        AddFields after the fact on a table that already has data in it. The reason we're using strings instead of
        FieldMap objects is that FieldMap objects get unhappy when there is no input field from the original table.
        However, constucting a string-based field map representation works absolutely fine.
        """
        return f';{field_name} "{field_name}" true true false {field_length} {field_type} 0 0,First,#'

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
