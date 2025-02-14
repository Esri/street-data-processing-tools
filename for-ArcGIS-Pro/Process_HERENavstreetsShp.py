"""Class to process raw HERE NAVSTREETS shapefile data into a network dataset.

   Copyright 2025 Esri
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
import datetime
import uuid
import pandas as pd
from enum import Enum
import arcpy
from helpers import CURDIR, timed_exec, TimeZoneType, UnitType, DataProductType, StreetInputData, StreetDataProcessor


LNG_CODES = {
    "ALB": "sq",  # Albanian
    "AMT": "hy",  # Armenian Transcribed
    "ARA": "ar",  # Arabic
    "ARE": "en",  # Arabic English
    "ARM": "hy",  # Armenian
    "ARX": "hy",  # Armenian Transliterated
    "ASM": "as",  # Assamese
    "ASX": "as",  # Assamese Transliterated
    "AZE": "az",  # Azerbaijan
    "AZX": "az",  # Azerbaijan Transliterated
    "BAQ": "eu",  # Basque
    "BEL": "be",  # Belarusian
    "BEN": "bn",  # Bengali
    "BET": "be",  # Belarusian Transcribed
    "BEX": "be",  # Belarusian Transliterated
    "BGX": "bn",  # Bengali Transliterated
    "BOS": "bs",  # Bosnian
    "BOX": "bs",  # Bosnian Transliterated
    "BUL": "bg",  # Bulgarian
    "BUT": "bg",  # Bulgarian Transcribed
    "BUX": "bg",  # Bulgarian Transliterated
    "CAT": "ca",  # Catalan
    "CHI": "zh",  # Chinese (Modern)
    "CHT": "zh",  # Chinese (Traditional)
    "CZE": "cs",  # Czech
    "CZX": "cs",  # Czech Transliterated
    "DAN": "da",  # Danish
    "DUT": "nl",  # Dutch
    "ENG": "en",  # English
    "EST": "et",  # Estonian
    "ESX": "et",  # Estonian Transliterated
    "FAO": "fo",  # Faroese
    "FIN": "fi",  # Finnish
    "FRE": "fr",  # French
    "GEO": "ka",  # Georgian
    "GER": "de",  # German
    "GET": "ka",  # Georgian Transcribed
    "GEX": "ka",  # Georgian Transliterated
    "GJX": "gu",  # Gujarati Transliterated
    "GLE": "ga",  # Irish Gaelic
    "GLG": "gl",  # Galician
    "GRE": "el",  # Greek
    "GRN": "gn",  # Guarani
    "GRT": "el",  # Greek Transcribed
    "GRX": "el",  # Greek Transliterated
    "GUJ": "gu",  # Gujarati
    "HEB": "he",  # Hebrew
    "HIN": "hi",  # Hindi
    "HUN": "hu",  # Hungarian
    "HUX": "hu",  # Hungarian Transliterated
    "ICE": "is",  # Icelandic
    "IND": "id",  # Bahasa Indonesia
    "ITA": "it",  # Italian
    "JPN": "ja",  # Japanese
    "KAN": "kn",  # Kannada
    "KAT": "kk",  # Kazakh Transcribed
    "KAX": "kk",  # Kazakh Transliterated
    "KAZ": "kk",  # Kazakh
    "KIR": "ky",  # Kyrgyz
    "KIT": "ky",  # Kyrgyz Transcribed
    "KIX": "ky",  # Kyrgyz Transliterated
    "KNX": "kn",  # Kannada Transliterated
    "KOR": "ko",  # Korean
    "KOX": "ko",  # Korean Transliterated
    "LAV": "lv",  # Latvian
    "LAX": "lv",  # Latvian Transliterated
    "LIT": "lt",  # Lithuanian
    "LIX": "lt",  # Lithuanian Transliterated
    "MAC": "mk",  # Macedonian
    "MAL": "ml",  # Malayalam
    "MAR": "mr",  # Marathi
    "MAT": "mk",  # Macedonian Transcribed
    "MAY": "ms",  # Malaysian
    "MGX": "mn",  # Mongolian Transliterated
    "MLT": "mt",  # Maltese
    "MLX": "mt",  # Maltese Transliterated
    "MNE": "",  # Montenegrin
    "MNX": "",  # Montenegrin Transliterated
    "MOL": "mo",  # Moldovan
    "MON": "mn",  # Mongolian
    "MOX": "mo",  # Moldovan Transliterated
    "MRX": "mr",  # Marathi Transliterated
    "MYX": "ml",  # Malayalam Transliterated
    "NOR": "no",  # Norwegian
    "ORI": "or",  # Oriya
    "ORX": "or",  # Oriya Transliterated
    "OTH": "",  # Other
    "PAN": "pa",  # Punjabi
    "PNX": "pa",  # Punjabi Transliterated
    "POL": "pl",  # Polish
    "POR": "pt",  # Portuguese
    "POX": "pl",  # Polish Transliterated
    "PYN": "zh",  # Pinyin
    "RMX": "ro",  # Romanian Transliterated
    "RST": "ru",  # Russian Transcribed
    "RUM": "ro",  # Romanian
    "RUS": "ru",  # Russian
    "RUX": "ru",  # Russian Transliterated
    "SCR": "sh",  # Croatian
    "SCT": "sr",  # Serbian Transcribed
    "SCX": "sr",  # Serbian Transliterated
    "SIX": "sv",  # Slovenian Transliterated
    "SLO": "sk",  # Slovak
    "SLV": "sv",  # Slovenian
    "SLX": "sk",  # Slovak Transliterated
    "SPA": "es",  # Spanish
    "SRB": "sr",  # Serbian
    "SRX": "sh",  # Croatian Transliterated
    "SWE": "sv",  # Swedish
    "TAM": "ta",  # Tamil
    "TEL": "te",  # Telugu
    "THA": "th",  # Thai
    "THE": "en",  # Thai English
    "TKT": "tr",  # Turkish Transcribed
    "TLX": "te",  # Telugu Transliterated
    "TMX": "ta",  # Tamil Transliterated
    "TUR": "tr",  # Turkish
    "TUX": "tr",  # Turkish Transliterated
    "TWE": "en",  # Taiwan English
    "UKR": "uk",  # Ukrainian
    "UKT": "uk",  # Ukrainian Transcribed
    "UKX": "uk",  # Ukrainian Transliterated
    "UND": "",  # Undefined
    "URD": "ur",  # Urdu
    "UZB": "uz",  # Uzbek
    "VIE": "vi",  # Vietnamese
    "WEL": "cy",  # Welsh
    "WEN": "en",  # World English
}
AR_FLD_SUFS = ["AUTO", "BUS", "TAXIS", "CARPOOL", "PEDSTRN", "TRUCKS", "THRUTR", "DELIVER", "EMERVEH", "MOTOR"]
AR_FLDS = [f"AR_{suf}" for suf in AR_FLD_SUFS]
DAY_FIELDS = ["U", "M", "T", "W", "R", "F", "S"]


class HistoricalTrafficConfigType(Enum):
    """Defines which type of inputs to use for configuring historical traffic."""

    NoTraffic = 1
    LinkReferenceFiles = 2
    TMCReferenceFiles = 3


class HereNavstreetsShpInputData(StreetInputData):
    """Defines a collection of HERE NAVSTREETS inputs to process."""

    def __init__(
        self, streets, alt_streets, z_levels, cdms, rdms, signs,
        historical_traffic_type: HistoricalTrafficConfigType, include_live_traffic,
        historical_speed_profiles_table=None,
        tmc_ref_table=None, traffic_table=None, link_ref_table_1_4=None, link_ref_table_5=None,
        cndmod_us_table=None, cndmod_non_us_table=None
    ):
        """Initialize an input HERE NAVSTREETS dataset with all the appropriate feature classes and tables"""
        self.streets = streets
        self.alt_streets = alt_streets
        self.z_levels = z_levels
        self.cdms = cdms
        self.rdms = rdms
        self.signs = signs
        self.historical_speed_profiles_table = historical_speed_profiles_table
        self.tmc_ref_table = tmc_ref_table
        self.traffic_table = traffic_table
        self.link_ref_table_1_4 = link_ref_table_1_4
        self.link_ref_table_5 = link_ref_table_5
        self.cndmod_us_table = cndmod_us_table
        self.cndmod_non_us_table = cndmod_non_us_table
        self.historical_traffic_type = historical_traffic_type
        self.include_historical_traffic = self.historical_traffic_type is not HistoricalTrafficConfigType.NoTraffic
        self.include_live_traffic = include_live_traffic
        self.use_transport_fields = self.cndmod_us_table is not None or self.cndmod_non_us_table is not None
        required_tables = [self.streets, self.alt_streets, self.z_levels, self.cdms, self.rdms, self.signs]
        # For setting up historical traffic using Link Reference Files you need all of the following:
        #   - Input Link Reference File covering Functional Classes 1-4
        #   - Input Link Reference File covering Functional Class 5
        #   - Input Speed Pattern Dictionary (SPD) file
        # For setting up historical traffic using TMC Reference Files you need all of the following:
        #   - Input Traffic Table
        #   - Input TMC Reference File
        #   - Input Speed Pattern Dictionary (SPD) file
        # If live traffic will be configured, the Traffic Table is required.
        if self.historical_traffic_type is HistoricalTrafficConfigType.LinkReferenceFiles:
            required_tables += [self.historical_speed_profiles_table, self.link_ref_table_1_4, self.link_ref_table_5]
            if self.include_live_traffic:
                required_tables.append(self.traffic_table)
        elif self.historical_traffic_type is HistoricalTrafficConfigType.TMCReferenceFiles:
            required_tables += [self.historical_speed_profiles_table, self.tmc_ref_table, self.traffic_table]

        # Make list of expected SPD table H fields
        spd_h_fields = []
        added_minutes = 0
        midnight = datetime.datetime(2021, 1, 1, 0, 0, 0)  # Initialize midnight on an arbitrary date
        # Add a field for each time slice increment until midnight
        while added_minutes < 1440:
            current_time = midnight + datetime.timedelta(minutes=added_minutes)
            spd_h_fields.append(f"H{current_time.strftime('%H_%M')}")
            added_minutes += 15

        # Initialize parent class
        super().__init__(
            required_tables=required_tables,
            required_fields={
                self.streets: [
                    ("LINK_ID", "Integer"),
                    ("ST_TYP_BEF", "String"),
                    ("ST_NM_BASE", "String"),
                    ("ST_TYP_AFT", "String"),
                    ("CONTRACC", "String"),
                    ("SPEED_CAT", "String"),
                    ("ST_LANGCD", "String"),
                    ("DIR_TRAVEL", "String"),
                    ("REF_IN_ID", "Integer"),
                    ("NREF_IN_ID", "Integer"),
                ] + [(field, "String") for field in [
                    "AR_AUTO", "AR_BUS", "AR_TAXIS", "AR_TRUCKS", "AR_DELIV", "AR_EMERVEH", "AR_MOTOR"]
                ],
                self.alt_streets: [
                    ("LINK_ID", "Integer"),
                    ("ST_TYP_BEF", "String"),
                    ("ST_NM_BASE", "String"),
                    ("ST_TYP_AFT", "String"),
                    ("ST_NAME", "String"),
                    ("ST_LANGCD", "String"),
                    ("ST_NM_PREF", "String"),
                    ("ST_NM_SUFF", "String"),
                    ("DIRONSIGN", "String"),
                    ("EXPLICATBL", "String")
                ],
                self.z_levels: [
                    ("LINK_ID", "Integer"),
                    ("INTRSECT", "String"),
                    ("POINT_NUM", "SmallInteger"),
                    ("Z_LEVEL", "SmallInteger")
                ],
                self.cdms: [
                    ("LINK_ID", "Integer"),
                    ("COND_ID", "Integer"),
                    ("COND_TYPE", "Integer"),
                    ("END_OF_LK", "String")
                ] + [(field, "String") for field in AR_FLDS],
                self.rdms: [
                    ("LINK_ID", "Integer"),
                    ("COND_ID", "Integer"),
                    ("SEQ_NUMBER", "SmallInteger"),
                ],
                self.signs: [
                    ("SIGN_ID", "Integer"),
                    ("SEQ_NUM", "SmallInteger"),
                    ("EXIT_NUM", "String"),
                    ("SRC_LINKID", "Integer"),
                    ("DST_LINKID", "Integer"),
                    ("LANG_CODE", "String"),
                    ("BR_RTEID", "String"),
                    ("BR_RTEDIR", "String"),
                    ("SIGN_TEXT", "String"),
                    ("SIGN_TXTTP", "String"),
                    ("TOW_RTEID", "String")
                ],
                self.historical_speed_profiles_table: ["PATTERN_ID"] + spd_h_fields,
                self.link_ref_table_1_4: ["LINK_PVID", "TRAVEL_DIRECTION"] + DAY_FIELDS,
                self.link_ref_table_5: ["LINK_PVID", "TRAVEL_DIRECTION"] + DAY_FIELDS,
                self.tmc_ref_table: ["TMC"] + DAY_FIELDS,
                self.traffic_table: [
                    ("LINK_ID", "Integer"),
                    ("TRAFFIC_CD", "String")
                ],
                self.cndmod_us_table: [
                    ("MOD_TYPE", "Integer"),
                    ("MOD_VAL", "String"),
                    ("COND_ID", "Integer")
                ],
                self.cndmod_non_us_table: [
                    ("MOD_TYPE", "Integer"),
                    ("MOD_VAL", "String"),
                    ("COND_ID", "Integer")
                ]
            },
            sr_input=self.streets
        )


class HereNavstreetsShpProcessor(StreetDataProcessor):

    def __init__(
        self, out_folder: str, gdb_name: str, in_here: HereNavstreetsShpInputData, unit_type: UnitType,
        time_zone_type: TimeZoneType, time_zone_name: str = "", in_time_zone_table=None,
        time_zone_ft_field: str = None, time_zone_tf_field: str = None, build_network: bool = True
    ):
        """Initialize a class to process HERE data into a network dataset."""
        self.historical_traffic_type = in_here.historical_traffic_type
        self.include_live_traffic = in_here.include_live_traffic
        self.use_transport_fields = in_here.use_transport_fields
        super().__init__(
            DataProductType.HereNavStreetsShp, out_folder, gdb_name, in_here, unit_type,
            in_here.include_historical_traffic,
            time_zone_type, time_zone_name, in_time_zone_table, time_zone_ft_field,
            time_zone_tf_field, build_network)

        # Initialized shared dataframes that will be populated later
        self.grouped_rdms_df = None  # Stores turn manuevers
        self.spd_df = None  # Stores traffic profiles from the SPD table
        self.traff_df = None  # Processed historical traffic data from link reference files or TMC files
        self.cndmod_df = None  # Stores records from the combined US and non-US CndMod table for restrictions
        self.preferred_dir_df = None  # Stores records describing preferred restrictions
        self.prohib_dir_df = None  # Stores records describing prohibited restrictions

        # Useful shared variables

        self.prefer_restr_fields = [f"{prefix}{suffix}" for suffix in [
                "STAAPreferred", "TruckDesignatedPreferred", "NRHMPreferred", "ExplosivesPreferred", "PIHPreferred",
                "MedicalWastePreferred", "RadioactivePreferred", "HazmatPreferred", "LocallyPreferred",
                "PreferredTruckRoute"
            ] for prefix in ["FT_", "TF_"]]
        self.prohib_restr_fields = [f"{prefix}{suffix}" for suffix in [
                "ExplosivesProhibited", "GasProhibited", "FlammableProhibited", "CombustibleProhibited",
                "OrganicProhibited", "PoisonProhibited", "RadioactiveProhibited", "CorrosiveProhibited",
                "OtherHazmatProhibited", "AnyHazmatProhibited", "PIHProhibited", "HarmfulToWaterProhibited",
                "ExplosiveAndFlammableProhibited"
            ] for prefix in ["FT_", "TF_"]]
        self.limit_restr_fields = [f"{prefix}{suffix}" for suffix in [
                "HeightLimit_Meters", "WeightLimit_Kilograms", "WeightLimitPerAxle_Kilograms", "LengthLimit_Meters",
                "WidthLimit_Meters", "KingpinToRearAxleLengthLimit_Meters"
            ] for prefix in ["FT_", "TF_"]]
        self.other_restr_fields = [f"{prefix}{suffix}" for suffix in [
                "MaxTrailersAllowedOnTruck", "SemiOrTractorWOneOrMoreTrailersProhibited", "MaxAxlesAllowed",
                "SingleAxleProhibited", "TandemAxleProhibited"
            ] for prefix in ["FT_", "TF_"]]
        self.prefer_suffixes = {  # {MOD_VAL: restriction name suffix}
            "1": "STAAPreferred",
            "2": "TruckDesignatedPreferred",
            "3": "NRHMPreferred",
            "4": "ExplosivesPreferred",
            "5": "PIHPreferred",
            "6": "MedicalWastePreferred",
            "7": "RadioactivePreferred",
            "8": "HazmatPreferred",
            "9": "LocallyPreferred"
        }
        self.prohib_suffixes = {  # {MOD_VAL: restriction name suffix}
            "1": "ExplosivesProhibited",
            "2": "GasProhibited",
            "3": "FlammableProhibited",
            "4": "CombustibleProhibited",
            "5": "OrganicProhibited",
            "6": "PoisonProhibited",
            "7": "RadioactiveProhibited",
            "8": "CorrosiveProhibited",
            "9": "OtherHazmatProhibited",
            "20": "AnyHazmatProhibited",
            "21": "PIHProhibited",
            "22": "HarmfulToWaterProhibited",
            "23": "ExplosiveAndFlammableProhibited"
        }
        self.limit_suffixes = {  # {MOD_TYPE: restriction name suffix}
            41: "HeightLimit_Meters",
            42: "WeightLimit_Kilograms",
            43: "WeightLimitPerAxle_Kilograms",
            44: "LengthLimit_Meters",
            45: "WidthLimit_Meters",
            81: "KingpinToRearAxleLengthLimit_Meters"
        }
        self.turn_restr_fields = list(AR_FLDS)
        self.addl_turn_field_defs = []
        if self.use_transport_fields:
            self.turn_restr_fields += list(self.prohib_suffixes.values())
            self.addl_turn_field_defs += [[f, "DOUBLE"] for f in self.limit_suffixes.values()] + [
                ["MaxTrailersAllowedOnTruck", "SHORT"],
                ["SemiOrTractorWOneOrMoreTrailersProhibited", "TEXT", "", 1],
                ["MaxAxlesAllowed", "SHORT"],
                ["SingleAxleProhibited", "TEXT", "", 1],
                ["TandemAxleProhibited", "TEXT", "", 1],
                ["AllTransportProhibited", "TEXT", "", 1]
            ]

    def process_here_data(self):
        """Process HERE NAVSTREETS shapefile data into a network dataset."""
        # Set the progressor so the user is informed of progress
        arcpy.SetProgressor("default")

        # Validate the input data
        if not self._validate_inputs():
            return

        # Create the output location
        self._create_feature_dataset()

        # Create the output Streets feature class
        self._copy_streets()
        self._detect_and_delete_duplicate_streets("LINK_ID")

        # Partially create and populate historical traffic tables
        # Some traffic-related tables must be read early because the information is used in populating Streets feature
        # class fields. More work will be done later on populating traffic tables because a fully-populated
        # Streets feature class is needed for that. (A bit of a chicken-and-egg problem here.)
        if self.include_historical_traffic:
            # Create the Profiles table and populate the spd_df dataframe that is used later
            self._create_profiles_table()
            self._populate_profiles_table()
            # Create and populate the traff_df dataframe used later
            self._read_and_process_historical_traffic_tables()

        # Populate Streets feature class fields with info from other tables
        if self.use_transport_fields:
            self.cndmod_df, self.preferred_dir_df, self.prohib_dir_df = self._read_and_process_cndmod_tables()
        self._populate_streets_fields()

        # Read in output streets for future look-ups
        self._read_and_index_streets()

        # Create and populate the Streets_Patterns historical traffic table, which requires.  Other historical traffic
        # was handled earlier, but this table requires the Streets feature class to be fully populated first.
        if self.include_historical_traffic:
            self._create_and_populate_streets_patterns_table()
        # Clean up memory
        del self.traff_df
        self.traff_df = None
        del self.spd_df
        self.spd_df = None

        # Create and populate the turn feature class
        self._read_and_index_turn_tables()
        self._create_turn_fc(self.turn_restr_fields, self.addl_turn_field_defs)
        self._generate_turn_features()
        # We're now done with the restrictions tables, so clear the variable to free up memory
        del self.cndmod_df
        del self.preferred_dir_df
        del self.prohib_dir_df
        self.cndmod_df = None
        self.preferred_dir_df = None
        self.prohib_dir_df = None

        # Create and populate the road forks table
        self._create_and_populate_road_forks()

        # Create and populate live traffic table
        if self.include_live_traffic:
            self._create_and_populate_streets_tmc_table()

        # Create and populate Signposts and Signposts_Streets
        self._create_signposts_fc()
        self._create_signposts_streets_table()
        self._populate_signposts_and_signposts_streets()

        # Clean up memory
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
        """Copy the streets shapefile to the target feature dataset and add fields."""
        self._add_message("Copying input streets shapefile to target feature dataset...")

        # Attempt to spatially sort input streets
        self.in_data_object.streets = self._spatially_sort_streets()

        # Construct field mappings to use when copying the original data.
        field_mappings = arcpy.FieldMappings()
        # Add all the fields from the input data
        field_mappings.addTable(self.in_data_object.streets)

        # Add new fields
        field_mappings = field_mappings.exportToString()
        # Add street name alternates
        field_mappings += self._create_string_field_map("ST_NAME_Alt", "Text", 240)
        field_mappings += self._create_string_field_map("ST_LANGCD_Alt", "Text", 3)
        field_mappings += self._create_string_field_map("ST_NM_PREF_Alt", "Text", 6)
        field_mappings += self._create_string_field_map("ST_TYP_BEF_Alt", "Text", 90)
        field_mappings += self._create_string_field_map("ST_NM_BASE_Alt", "Text", 105)
        field_mappings += self._create_string_field_map("ST_NM_SUFF_Alt", "Text", 6)
        field_mappings += self._create_string_field_map("ST_TYP_AFT_Alt", "Text", 90)
        field_mappings += self._create_string_field_map("DIRONSIGN_Alt", "Text", 1)
        # Add ZLEV fields
        field_mappings += self._create_string_field_map("F_ZLEV", "Short", 1)
        field_mappings += self._create_string_field_map("T_ZLEV", "Short", 1)
        # Add other fields
        field_mappings += self._create_string_field_map("Meters", "Double", 8)
        field_mappings += self._create_string_field_map("KPH", "Float", 1)
        field_mappings += self._create_string_field_map("Language", "Text", 2)
        field_mappings += self._create_string_field_map("Language_Alt", "Text", 2)
        field_mappings += self._create_string_field_map("ClosedForConstruction", "Text", 1)
        # Add usage fee fields
        for suffix in AR_FLD_SUFS:
            field_mappings += self._create_string_field_map(f"UFR_{suffix}", "Text", 1)
        # Impedance fields
        if self.include_historical_traffic:
            field_mappings += self._create_string_field_map("FT_AverageSpeed", "Float", 1)
            field_mappings += self._create_string_field_map("TF_AverageSpeed", "Float", 1)
            field_mappings += self._create_string_field_map("FT_Minutes", "Float", 1)
            field_mappings += self._create_string_field_map("TF_Minutes", "Float", 1)
        else:
            field_mappings += self._create_string_field_map("Minutes", "Float", 1)
        # Add transport fields
        if self.use_transport_fields:
            field_mappings += self._create_string_field_map("TruckFCOverride", "Short", 1)
            for field in self.prefer_restr_fields:
                field_mappings += self._create_string_field_map(field, "Text", 1)
            for field in self.prohib_restr_fields:
                field_mappings += self._create_string_field_map(field, "Text", 1)
            for field in self.limit_restr_fields:
                field_mappings += self._create_string_field_map(field, "Double", 8)
            field_mappings += self._create_string_field_map("FT_MaxTrailersAllowedOnTruck", "Short", 1)
            field_mappings += self._create_string_field_map("TF_MaxTrailersAllowedOnTruck", "Short", 1)
            field_mappings += self._create_string_field_map("FT_SemiOrTractorWOneOrMoreTrailersProhibited", "Text", 1)
            field_mappings += self._create_string_field_map("TF_SemiOrTractorWOneOrMoreTrailersProhibited", "Text", 1)
            field_mappings += self._create_string_field_map("FT_MaxAxlesAllowed", "Short", 1)
            field_mappings += self._create_string_field_map("TF_MaxAxlesAllowed", "Short", 1)
            field_mappings += self._create_string_field_map("FT_SingleAxleProhibited", "Text", 1)
            field_mappings += self._create_string_field_map("TF_SingleAxleProhibited", "Text", 1)
            field_mappings += self._create_string_field_map("FT_TandemAxleProhibited", "Text", 1)
            field_mappings += self._create_string_field_map("TF_TandemAxleProhibited", "Text", 1)
            field_mappings += self._create_string_field_map("FT_TruckKPH", "Double", 8)
            field_mappings += self._create_string_field_map("TF_TruckKPH", "Double", 8)

        # Copy the input network geometry feature class to the target feature dataset
        arcpy.conversion.FeatureClassToFeatureClass(
            self.in_data_object.streets, self.feature_dataset, os.path.basename(self.streets), field_mapping=field_mappings)

        # Update the fc_id that will be used to relate back to this Streets feature class in Edge#FCID fields
        desc = arcpy.Describe(self.streets)
        self.fc_id = desc.DSID
        self.streets_oid_field = desc.oidFieldName

    @timed_exec
    def _read_and_index_streets(self):
        """Read in the streets table and index it for quick lookups."""
        self._add_message("Reading and indexing Streets table...")
        # Store street info in a dataframe for quick lookups
        with arcpy.da.SearchCursor(self.streets, ["LINK_ID", "OID@", "Meters", "REF_IN_ID", "NREF_IN_ID"]) as cur:
            self.streets_df = pd.DataFrame(cur, columns=["LINK_ID", "OID", "Meters", "REF_IN_ID", "NREF_IN_ID"])
        self.streets_df.set_index("LINK_ID", inplace=True)
        # Add an empty field to store street geometries.  They'll be filled only as needed to conserve memory
        self.streets_df["SHAPE"] = None

    @timed_exec
    def _read_and_index_alt_streets(self):
        """Read in the alt streets table and index it for quick lookups."""
        where = "EXPLICATBL = 'Y'"
        fields = ["LINK_ID", "ST_TYP_BEF", "ST_NM_BASE", "ST_TYP_AFT", "ST_NAME", "ST_LANGCD", "ST_NM_PREF",
                  "ST_NM_SUFF", "DIRONSIGN"]
        with arcpy.da.SearchCursor(self.in_data_object.alt_streets, fields, where) as cur:
            alt_streets_df = pd.DataFrame(cur, columns=fields)
        alt_streets_df.drop_duplicates(inplace=True)
        # Index the dataframe by LINK_ID for quick retrieval later
        alt_streets_df.set_index("LINK_ID", inplace=True)
        return alt_streets_df

    @timed_exec
    def _read_and_index_z_levels(self):
        """Read in the z-levels table and index it for quick lookups."""
        where = "INTRSECT = 'Y'"
        fields = ["LINK_ID", "POINT_NUM", "Z_LEVEL"]
        with arcpy.da.SearchCursor(self.in_data_object.z_levels, fields, where) as cur:
            z_levels_df = pd.DataFrame(cur, columns=fields)
        z_levels_df = z_levels_df.sort_values(["LINK_ID", "POINT_NUM"])
        z_levels_df.drop(columns=["POINT_NUM"], inplace=True)
        # Find the first and last Z_LEVEL entry for each LINK_ID and log these as F_ZLEV and T_ZLEV
        f_zlev_df = z_levels_df.groupby("LINK_ID").first()
        f_zlev_df.rename(columns={"Z_LEVEL": "F_ZLEV"}, inplace=True)
        z_levels_df = pd.concat([f_zlev_df, z_levels_df.groupby("LINK_ID").last()], axis='columns')
        z_levels_df.rename(columns={"Z_LEVEL": "T_ZLEV"}, inplace=True)
        del f_zlev_df
        return z_levels_df

    @timed_exec
    def _read_cdms_construction_links(self):
        """Read in the csms table to create a list of roads closed for construction."""
        where = "COND_TYPE = 3"
        cmds_links = set()
        for row in arcpy.da.SearchCursor(self.in_data_object.cdms, ["LINK_ID"], where):
            cmds_links.add(row[0])
        return cmds_links

    @timed_exec
    def _read_cdms_usage_fee_links(self):
        """Read in the cdms table to create a dataframe of usage fee restrictions and index it for quick lookups."""
        where = "COND_TYPE = 12"
        fields = ["LINK_ID"] + AR_FLDS
        with arcpy.da.SearchCursor(self.in_data_object.cdms, fields, where) as cur:
            ufr_df = pd.DataFrame(cur, columns=fields)
        # Index the dataframe by LINK_ID for quick retrieval later
        ufr_df.set_index("LINK_ID", inplace=True)
        return ufr_df

    @staticmethod
    def _read_cndmod_table(cndmod_table):
        """Read the designated cndmod table and convert it to a dataframe."""
        where = "MOD_TYPE IN (38, 39, 41, 42, 43, 44, 45, 46, 48, 49, 60, 75, 81)"
        fields = ["MOD_TYPE", "MOD_VAL", "COND_ID"]
        with arcpy.da.SearchCursor(cndmod_table, fields, where) as cur:
            cndmod_df = pd.DataFrame(cur, columns=fields)
        # The MOD_VAL column is text in the original input, but the only values we'll be working with are integers, and,
        # for some rows, we will need to modify them to match the desired units of measurement, which means making them
        # floats.
        cndmod_df["MOD_VAL_U"] = cndmod_df["MOD_VAL"].astype(float)
        return cndmod_df

    @timed_exec
    def _read_and_process_cndmod_tables(self):
        """Read the cndmod tables, calculate values in desired units, and extract direction tables."""
        self._add_message("Reading and processing transport condition modifier (CndMod) tables...")
        # Read the US and non-US CndMod tables (if relevant) and do some unit conversions
        cndmod_us_df = None
        cndmod_non_us_df = None
        if self.in_data_object.cndmod_us_table:
            cndmod_us_df = self._read_cndmod_table(self.in_data_object.cndmod_us_table)
            # Convert values to desired units (Meters, Kilograms, KPH)
            cndmod_us_df.loc[cndmod_us_df["MOD_TYPE"].isin([41, 44, 45, 81]), "MOD_VAL_U"] = \
                cndmod_us_df.loc[cndmod_us_df["MOD_TYPE"].isin([41, 44, 45, 81]), "MOD_VAL_U"] * 0.0254
            cndmod_us_df.loc[cndmod_us_df["MOD_TYPE"].isin([42, 43]), "MOD_VAL_U"] = \
                cndmod_us_df.loc[cndmod_us_df["MOD_TYPE"].isin([42, 43]), "MOD_VAL_U"] * 0.45359237
            cndmod_us_df.loc[cndmod_us_df["MOD_TYPE"] == 48, "MOD_VAL_U"] = \
                cndmod_us_df.loc[cndmod_us_df["MOD_TYPE"] == 48, "MOD_VAL_U"] * 1.609344
        if self.in_data_object.cndmod_non_us_table:
            cndmod_non_us_df = self._read_cndmod_table(self.in_data_object.cndmod_non_us_table)
            # Convert values to desired units
            cndmod_non_us_df.loc[cndmod_non_us_df["MOD_TYPE"].isin([41, 44, 45, 81]), "MOD_VAL_U"] = \
                cndmod_non_us_df.loc[cndmod_non_us_df["MOD_TYPE"].isin([41, 44, 45, 81]), "MOD_VAL_U"] * 0.01

        # Figure out which tables we're using
        cndmod_dfs = [df for df in [cndmod_us_df, cndmod_non_us_df] if df is not None]
        if not cndmod_dfs:
            # Not using transport conditional modifiers for this network
            return None
        if len(cndmod_dfs) == 1:
            # Using only US or non-US table only
            cndmod_df = cndmod_dfs[0]
        else:
            # Using both tables
            cndmod_df = pd.concat(cndmod_dfs)

        # Extract rows that determine the directionality of prohibited and preferred restrictions
        preferred_dir_df = cndmod_df[cndmod_df["MOD_TYPE"] == 60].copy()
        preferred_dir_df.loc[preferred_dir_df["MOD_VAL"] == "1", "Direction"] = "FT"
        preferred_dir_df.loc[preferred_dir_df["MOD_VAL"] == "2", "Direction"] = "TF"
        preferred_dir_df.loc[preferred_dir_df["MOD_VAL"] == "3", "Direction"] = "B"
        preferred_dir_df.drop(columns=["MOD_TYPE", "MOD_VAL", "MOD_VAL_U"], inplace=True)
        preferred_dir_df.set_index("COND_ID", inplace=True)
        prohib_dir_df = cndmod_df[cndmod_df["MOD_TYPE"] == 38].copy()
        prohib_dir_df.loc[prohib_dir_df["MOD_VAL"] == "1", "Direction"] = "B"
        prohib_dir_df.loc[prohib_dir_df["MOD_VAL"] == "2", "Direction"] = "FT"
        prohib_dir_df.loc[prohib_dir_df["MOD_VAL"] == "3", "Direction"] = "TF"
        prohib_dir_df.drop(columns=["MOD_TYPE", "MOD_VAL", "MOD_VAL_U"], inplace=True)
        prohib_dir_df.set_index("COND_ID", inplace=True)
        # Drop those rows from the main dataframe since they are now being held as separate lookup tables
        cndmod_df = cndmod_df[~cndmod_df["MOD_TYPE"].isin([60, 38])]

        # Join LINK_ID from cdms table
        where = "COND_TYPE IN (23, 25, 27)"
        fields = ["LINK_ID", "COND_ID"]
        with arcpy.da.SearchCursor(self.in_data_object.cdms, fields, where) as cur:
            cdms_df = pd.DataFrame(cur, columns=fields)
        cdms_df.set_index("COND_ID", inplace=True)
        cndmod_df = cndmod_df.join(cdms_df, "COND_ID", how="inner")
        # Index by LINK_ID for quick lookups
        cndmod_df.set_index("LINK_ID", inplace=True)

        # Return three dataframes that will all be used for lookups when populating restriction fields in Streets
        return cndmod_df, preferred_dir_df, prohib_dir_df

    @timed_exec
    def _populate_streets_fields(self):
        """Populate the fields in the streets table."""
        self._add_message("Populating Streets fields...")
        if self.include_historical_traffic:
            assert self.traff_df is not None
            self.traff_df.set_index("LINK_ID", inplace=True)

        # Calculate the Meters field using geodesic distance
        arcpy.management.CalculateGeometryAttributes(self.streets, "Meters LENGTH_GEODESIC", "METERS")

        # Read some additional tables
        alt_streets_df = self._read_and_index_alt_streets()
        z_levels_df = self._read_and_index_z_levels()
        construction_links = self._read_cdms_construction_links()
        ufr_df = self._read_cdms_usage_fee_links()

        def calc_kph(contr_acc, speed_cat):
            """Calculate the KPH field value based on the CONTRACC and SPEED_CAT fields."""
            speed_cat_to_kph = {
                "1": 112,
                "2": 92,
                "3": 76,
                "4": 64,
                "5": 48,
                "6": 32,
                "7": 16,
                "8": 4
            }
            kph = speed_cat_to_kph.get(speed_cat, 1)
            if contr_acc == "Y":
                kph = kph * 1.2
            return kph

        def calc_traffic_based_speed_and_minutes(row, kph_val):
            """Populate AverageSpeed and Minutes fields based on traffic table info."""
            # CalculatefAverageSpeed field from matching traffic records
            try:
                # Retrieve the relevant traffic records for this LINK_ID
                traff_records = self.traff_df.loc[link_id]
                if isinstance(traff_records, pd.Series):
                    if traff_records["EdgeFrmPos"] == 0:
                        row[fname_idx["FT_AverageSpeed"]] = traff_records["AverageSpeed"]
                    elif traff_records["EdgeFrmPos"] == 1:
                        row[fname_idx["TF_AverageSpeed"]] = traff_records["AverageSpeed"]
                else:
                    for _, traff_record in traff_records.iterrows():
                        if traff_record["EdgeFrmPos"] == 0:
                            row[fname_idx["FT_AverageSpeed"]] = traff_record["AverageSpeed"]
                        elif traff_record["EdgeFrmPos"] == 1:
                            row[fname_idx["TF_AverageSpeed"]] = traff_record["AverageSpeed"]
            except KeyError:
                # There were no traffic records for this LINK_ID. Just skip it and move on.
                pass

            # Populate Minutes fields based on Meters and AverageSpeed
            meters = row[fname_idx["Meters"]]
            ft_speed = row[fname_idx["FT_AverageSpeed"]]
            if ft_speed is None:
                ft_speed = kph_val
            row[fname_idx["FT_Minutes"]] = meters * 0.06 / ft_speed
            tf_speed = row[fname_idx["TF_AverageSpeed"]]
            if tf_speed is None:
                tf_speed = kph_val
            row[fname_idx["TF_Minutes"]] = meters * 0.06 / tf_speed

            return row

        def calc_directional_condmod_fields(row, field_suffix, cond_id, value_to_set, is_preferred):
            """For a given field and restriction condition, get the direction and update appropriate fields."""
            # Figure out which direction this record is for and update appropriate fields in row
            try:
                # Retrieve the restriction records for this ID from the correct reference dataframe
                if is_preferred:
                    subset_df = self.preferred_dir_df.loc[cond_id]
                else:
                    subset_df = self.prohib_dir_df.loc[cond_id]
                if isinstance(subset_df, pd.DataFrame):
                    subset_df = subset_df.iloc[0]
            except KeyError:
                # There was no indication of the directionality of this record, so it's not usable.
                # Return row unchanged.
                return row
            # Calculate directional fields
            direction = subset_df["Direction"]
            if direction in ["FT", "B"]:
                row[fname_idx[f"FT_{field_suffix}"]] = value_to_set
            if direction in ["TF", "B"]:
                row[fname_idx[f"TF_{field_suffix}"]] = value_to_set
            return row

        def calc_cndmod_fields(row, cndmod_record):
            """Calculate restriction field values from data in the cndmod tables."""
            mod_type = cndmod_record["MOD_TYPE"]
            mod_val = cndmod_record["MOD_VAL"]
            cond_id = cndmod_record["COND_ID"]

            # Calculate value for a preferred restriction or TruckFCOverride field
            if mod_type == 49:
                if mod_val == "15":
                    row[fname_idx["TruckFCOverride"]] = 1
                elif mod_val == "16":
                    row[fname_idx["TruckFCOverride"]] = 2
                else:
                    # Figure out which prefer restriction this record is for
                    prefer_suffix = self.prefer_suffixes.get(mod_val)
                    if prefer_suffix:
                        row = calc_directional_condmod_fields(row, prefer_suffix, cond_id, "Y", is_preferred=True)

            # Calculate a value for a prohibit restriction
            elif mod_type == 39:
                # Figure out which prohibit restriction this is for
                prohib_suffix = self.prohib_suffixes.get(mod_val)
                if not prohib_suffix:
                    # Invalid or irrelevant record.  Return row unchanged.
                    return row
                row = calc_directional_condmod_fields(row, prohib_suffix, cond_id, "Y", is_preferred=False)

            # Calculate a value for a limit restriction field
            elif mod_type in self.limit_suffixes.keys():
                mod_val = cndmod_record["MOD_VAL_U"]
                limit_suffix = self.limit_suffixes[mod_type]
                row = calc_directional_condmod_fields(row, limit_suffix, cond_id, mod_val, is_preferred=False)

            # Handle a couple of specific restriction cases
            elif mod_type == 46:
                if mod_val in ('1', '2', '3'):
                    row = calc_directional_condmod_fields(
                        row, "MaxTrailersAllowedOnTruck", cond_id, int(mod_val), is_preferred=False)
                elif mod_val == "4":
                    row = calc_directional_condmod_fields(
                        row, "SemiOrTractorWOneOrMoreTrailersProhibited", cond_id, "Y", is_preferred=False)
            elif mod_type == 75:
                if mod_val in ('1', '2', '3', '4', '5'):
                    row = calc_directional_condmod_fields(
                        row, "MaxAxlesAllowed", cond_id, int(mod_val), is_preferred=False)
                elif mod_val == '6':
                    row = calc_directional_condmod_fields(row, "SingleAxleProhibited", cond_id, "Y", is_preferred=False)
                elif mod_val == '7':
                    row = calc_directional_condmod_fields(row, "TandemAxleProhibited", cond_id, "Y", is_preferred=False)
            elif mod_type == 48:
                mod_val = cndmod_record["MOD_VAL_U"]
                row = calc_directional_condmod_fields(row, "TruckKPH", cond_id, mod_val, is_preferred=True)

            return row

        def calc_additional_transport_fields(row, field_suffix, other_field_suffixes):
            """Calculate additional restriction fields based on the values of other restrictions already set."""
            for prefix in ["FT_", "TF_"]:
                is_restricted = False
                for other_suffix in other_field_suffixes:
                    if row[fname_idx[f"{prefix}{other_suffix}"]] == "Y":
                        is_restricted = True
                if is_restricted:
                    row[fname_idx[f"{prefix}{field_suffix}"]] = "Y"
            return row

        # Iterate through the streets table and populate update fields
        alt_street_fields = [
            "ST_NAME_Alt", "ST_LANGCD_Alt", "ST_NM_PREF_Alt", "ST_TYP_BEF_Alt", "ST_NM_BASE_Alt", "ST_NM_SUFF_Alt",
            "ST_TYP_AFT_Alt", "DIRONSIGN_Alt"]
        ufr_fields = [f"UFR_{ufr_suff}" for ufr_suff in AR_FLD_SUFS]
        fields = [
            "LINK_ID",
            "F_ZLEV", "T_ZLEV",
            "KPH", "CONTRACC", "SPEED_CAT",
            "Language", "Language_Alt", "ST_LANGCD",
            "ST_TYP_BEF", "ST_NM_BASE", "ST_TYP_AFT",
            "ClosedForConstruction",
            "Meters"
        ] + alt_street_fields + ufr_fields
        if not self.include_historical_traffic:
            fields += ["Minutes"]
        else:
            fields += ["FT_AverageSpeed", "TF_AverageSpeed", "FT_Minutes", "TF_Minutes"]
        if self.cndmod_df is not None:
            fields += self.prefer_restr_fields
            fields += self.prohib_restr_fields
            fields += self.limit_restr_fields
            fields += [
                "FT_MaxTrailersAllowedOnTruck", "TF_MaxTrailersAllowedOnTruck",
                "FT_SemiOrTractorWOneOrMoreTrailersProhibited", "TF_SemiOrTractorWOneOrMoreTrailersProhibited",
                "FT_MaxAxlesAllowed", "TF_MaxAxlesAllowed",
                "FT_SingleAxleProhibited", "TF_SingleAxleProhibited",
                "FT_TandemAxleProhibited", "TF_TandemAxleProhibited",
                "FT_TruckKPH", "TF_TruckKPH",
                "TruckFCOverride"
            ]
        fname_idx = {f: i for i, f in enumerate(fields)}  # {Field name: index in cursor row}
        # Set up progressor
        num_rows = int(arcpy.management.GetCount(self.streets).getOutput(0))
        current_row_num = 0
        arcpy.SetProgressor("step", "Populating Streets fields...", 0, num_rows, 1)
        with arcpy.da.UpdateCursor(self.streets, fields) as cur:
            for row in cur:
                current_row_num += 1
                arcpy.SetProgressorPosition(current_row_num)

                # Initialize the new row to be the same as the original
                updated_row = list(row)

                link_id = row[0]

                # Populate the alt streets fields
                try:
                    # Retrieve the alt streets records for this LINK_ID
                    # The *_Alt fields in Streets are to contain an alternate name for each street (when there is one).
                    # The primary name is already on the raw Streets feature class, while the alternate names (which
                    # there can be many) are provided in the raw AltStreets feature class.  However, not all names are
                    # useful for including in the driving directions text (most names are only useful for geocoding).
                    # The ones that are suitable for driving directions are those that have an EXPLICATBL value of Y.
                    # Also, many alternate names are very similar to the primary name (which isn't useful due to
                    # redundancy), so we only care about names that are different enough.  Despite all the filtering,
                    # there still can be multiple alternate names – at that point, we don't really care which one we
                    # use, and tool does not try to populate multiple alternate names.  The older ArcMap version of the
                    # tool always used the last one, so that's what we do here.
                    alt_streets_records = alt_streets_df.loc[link_id]
                    if isinstance(alt_streets_records, pd.Series):
                        if (
                            alt_streets_records["ST_TYP_BEF"] != updated_row[fname_idx["ST_TYP_BEF"]] or
                            alt_streets_records["ST_NM_BASE"] != updated_row[fname_idx["ST_NM_BASE"]] or
                            alt_streets_records["ST_TYP_AFT"] != updated_row[fname_idx["ST_TYP_AFT"]]
                        ):
                            for field in alt_street_fields:
                                updated_row[fname_idx[field]] = alt_streets_records[field.rstrip("_Alt")]
                    else:
                        # If more than one matching record was returned, find the last record with differences and use
                        # that, which matches the ArcMap tool's behavior. (iterating in backwards order)
                        for _, alt_streets_record in alt_streets_records.iloc[::-1].iterrows():
                            if (
                                alt_streets_record["ST_TYP_BEF"] != updated_row[fname_idx["ST_TYP_BEF"]] or
                                alt_streets_record["ST_NM_BASE"] != updated_row[fname_idx["ST_NM_BASE"]] or
                                alt_streets_record["ST_TYP_AFT"] != updated_row[fname_idx["ST_TYP_AFT"]]
                            ):
                                for field in alt_street_fields:
                                    updated_row[fname_idx[field]] = alt_streets_record[field.rstrip("_Alt")]
                                break
                except KeyError:
                    # There were no alt streets records for this LINK_ID. Just skip it and move on.
                    pass

                # Populate the zlev fields
                try:
                    # Retrieve the zlev record for this LINK_ID
                    zlev_record = z_levels_df.loc[link_id]
                    updated_row[fname_idx["F_ZLEV"]] = zlev_record["F_ZLEV"]
                    updated_row[fname_idx["T_ZLEV"]] = zlev_record["T_ZLEV"]
                except KeyError:
                    # There were no zlev records for this LINK_ID. Just skip it and move on.
                    pass

                # Populate the KPH field
                kph_val = calc_kph(
                    updated_row[fname_idx["CONTRACC"]], updated_row[fname_idx["SPEED_CAT"]])
                updated_row[fname_idx["KPH"]] = kph_val

                if not self.include_historical_traffic:
                    # Populate the Minutes field based on speed categories if there's no historical traffic
                    updated_row[fname_idx["Minutes"]] = updated_row[fname_idx["Meters"]] * 0.06 / kph_val
                else:
                    # Populate Minutes and AverageSpeed fields from traffic tables
                    updated_row = calc_traffic_based_speed_and_minutes(updated_row, kph_val)

                # Populate the Language field
                updated_row[fname_idx["Language"]] = LNG_CODES.get(updated_row[fname_idx["ST_LANGCD"]], "")
                in_alt_lng = updated_row[fname_idx["ST_LANGCD_Alt"]]
                if in_alt_lng is not None:
                    updated_row[fname_idx["Language_Alt"]] = LNG_CODES.get(in_alt_lng, "")

                # Populate roads closed for construction
                if link_id in construction_links:
                    updated_row[fname_idx["ClosedForConstruction"]] = "Y"

                # Populate the usage fee restriction fields
                try:
                    # Retrieve the relevant cdms records for this LINK_ID
                    ufr_record = ufr_df.loc[link_id]
                    for ufr_suff in AR_FLD_SUFS:
                        updated_row[fname_idx[f"UFR_{ufr_suff}"]] = ufr_record[f"AR_{ufr_suff}"]
                except KeyError:
                    # There were no UFR records for this LINK_ID. Just skip it and move on.
                    pass

                # Populate the restriction fields associated with the transport condition modifier table
                if self.cndmod_df is not None:
                    try:
                        # Retrieve the restriction records for this ID
                        subset_df = self.cndmod_df.loc[link_id]
                        # Loop through the transport condition modifier records and update the appropriate rows
                        if isinstance(subset_df, pd.Series):
                            # There was only one record with this ID, so pandas returns a series
                            updated_row = calc_cndmod_fields(updated_row, subset_df)
                        else:
                            # There were multiple records with this ID, so pandas returns a dataframe.
                            for _, record in subset_df.iterrows():
                                updated_row = calc_cndmod_fields(updated_row, record)

                        # Update additional restrictions based on the values of the other cndmod restrictions
                        updated_row = calc_additional_transport_fields(
                            updated_row,
                            "PreferredTruckRoute",
                            ["STAAPreferred", "TruckDesignatedPreferred", "LocallyPreferred"]
                        )

                    except KeyError:
                        # There were no cndmod restriction records for this ID. Just skip it and move on.
                        pass

                # Update the row in the Streets table
                cur.updateRow(updated_row)

        # Clean up and free up memory
        arcpy.ResetProgressor()
        del alt_streets_df
        del z_levels_df
        del construction_links

    @timed_exec
    def _populate_profiles_table(self):
        """Populate the traffic profiles (Patterns) table."""
        if not self.include_historical_traffic:
            return
        self._add_message(f"Populating the {os.path.basename(self.profiles)} table...")

        # Get the list of all the fields to populate in the Patterns table
        profiles_oid = arcpy.Describe(self.profiles).oidFieldName
        profiles_fields = [f.name for f in arcpy.ListFields(self.profiles) if f.name != profiles_oid]

        # The SPD csv file in HERE's Traffic Patterns data provides the absolute speed values in km/h (we ask the user
        # to use the km/h 15-minute slice file.  To get the relative speed, you need to know the freeflow speed to base
        # it upon.  Our tool should assume that the freeflow speed is the speed at midnight (the value in the H00_00
        # column).  So to get each time slice's relative speed, you need to divide the time slice's absolute speed by
        # the midnight speed – for example, for the 4:30pm time slice, divide the speed found in the H16_30 column by
        # the speed found in the H00_00 (midnight) column.  Note that the midnight relative speed will always yield a
        # value of 1.0, since it's the speed divided by itself.  Note also that some speeds are not the absolute fastest
        # at midnight (sometimes the absolute fastest is in the 1am or 2am hour, but it's only faster by about a km/h or
        # two) – this will yield values greater than 1.0, which is okay, as the solver will just assume you're traveling
        # at full freeflow.
        h_fields = [f.split("SpeedFactor_")[1] for f in profiles_fields if f.startswith("SpeedFactor_")]
        h_fields = [f"H{f[0:2]}_{f[2:4]}" for f in h_fields]
        spd_cols = ["PATTERN_ID"] + h_fields
        self.spd_df = pd.read_csv(self.in_data_object.historical_speed_profiles_table, usecols=spd_cols)
        self.spd_df["BaseSpeed"] = self.spd_df["H00_00"]
        self.spd_df["AverageSpeed"] = sum(1/self.spd_df[h] for h in h_fields) / sum(1/(self.spd_df[h]**2) for h in h_fields)
        for col in self.spd_df.columns:
            if col == "H00_00" or not col.startswith("H"):
                continue
            self.spd_df[col] = self.spd_df[col] / self.spd_df["H00_00"]
        self.spd_df["H00_00"] = 1.0

        # Insert the rows
        fields = ["PatternID"] + [f for f in profiles_fields if f.startswith("SpeedFactor_")] + \
            ["BaseSpeed", "AverageSpeed"]
        with arcpy.da.InsertCursor(self.profiles, fields) as cur:
            # Loop through the records in the SPD table and populate the Patterns table with the values
            for _, row in self.spd_df.iterrows():
                cur.insertRow(row.to_list())

        # Add a field to represent the OID that will be used for the final table, which will be referenced later
        self.spd_df["OID"] = range(1, len(self.spd_df) + 1)
        # Set index to prepare for future use
        self.spd_df.set_index("PATTERN_ID", inplace=True)
        # Check for constant speed patterns where all H**_** fields are the same across the day
        self.spd_df["IsConst"] = self.spd_df[h_fields].eq(self.spd_df[h_fields].iloc[:, 0], axis=0).all(axis=1)
        # Drop H**_** fields, which are no longer needed
        self.spd_df.drop(columns=h_fields, inplace=True)

    @timed_exec
    def _read_link_ref_files(self):
        """Read in the link reference tables and use them to populate the Streets_Patterns table."""
        assert self.spd_df is not None

        # Make a list of street link IDs to use for filtering out irrelevant traffic records
        streets_ids = []
        for row in arcpy.da.SearchCursor(self.streets, ["LINK_ID"]):
            streets_ids.append(row[0])

        # The traffic tables can sometimes be huge, so read them in and process them in chunks to reduce
        # memory consumption
        chunk_size = 100000

        # Read the link reference table covering functional class 5 and remove records that are constant across all
        # times of day and days of week
        lr5_df_chunks = []
        num_chunks = 0
        for lr5_df in pd.read_csv(self.in_data_object.link_ref_table_5, chunksize=chunk_size):
            num_chunks += 1
            lr5_df = lr5_df[lr5_df["LINK_PVID"].isin(streets_ids)]
            lr5_df = lr5_df.join(self.spd_df["IsConst"], "U")
            lr5_df["IsConst2"] = lr5_df[DAY_FIELDS].eq(lr5_df[DAY_FIELDS].iloc[:, 0], axis=0).all(axis=1)
            lr5_df = lr5_df.loc[~(lr5_df["IsConst"] & lr5_df["IsConst2"])]
            lr5_df.drop(columns=["IsConst", "IsConst2"], inplace=True)
            # Calculate from and to pos based on travel direction
            lr5_df["EdgeFrmPos"] = lr5_df["TRAVEL_DIRECTION"] == "T"
            lr5_df["EdgeToPos"] = lr5_df["TRAVEL_DIRECTION"] == "F"
            lr5_df["EdgeFrmPos"] = lr5_df["EdgeFrmPos"].astype(int)
            lr5_df["EdgeToPos"] = lr5_df["EdgeToPos"].astype(int)
            lr5_df.drop(columns=["TRAVEL_DIRECTION"], inplace=True)
            lr5_df_chunks.append(lr5_df)
        lr5_df = pd.concat(lr5_df_chunks)
        del lr5_df_chunks

        # Read the link reference table covering functional class 1-4 and combine it with the processed functional
        # class 5 table
        lr_df_chunks = []
        num_chunks = 0
        for lr_df in pd.read_csv(self.in_data_object.link_ref_table_1_4, chunksize=chunk_size):
            num_chunks += 1
            lr_df = lr_df[lr_df["LINK_PVID"].isin(streets_ids)]
            # Calculate from and to pos based on travel direction
            lr_df["EdgeFrmPos"] = lr_df["TRAVEL_DIRECTION"] == "T"
            lr_df["EdgeToPos"] = lr_df["TRAVEL_DIRECTION"] == "F"
            lr_df["EdgeFrmPos"] = lr_df["EdgeFrmPos"].astype(int)
            lr_df["EdgeToPos"] = lr_df["EdgeToPos"].astype(int)
            lr_df.drop(columns=["TRAVEL_DIRECTION"], inplace=True)
            lr_df_chunks.append(lr_df)
        lr_df = pd.concat(lr_df_chunks + [lr5_df])
        del lr_df_chunks
        del lr5_df

        # Standardize schema
        lr_df.rename(columns={"LINK_PVID": "LINK_ID"}, inplace=True)

        return lr_df

    @timed_exec
    def _read_tmc_traffic_table(self):
        """Read the TMC Traffic table and calculate the Edge*Pos fields."""
        with arcpy.da.SearchCursor(self.in_data_object.traffic_table, ["LINK_ID", "TRAFFIC_CD"]) as cur:
            traff_df = pd.DataFrame(cur, columns=["LINK_ID", "TMC"])
        # Calculate from and to pos based on TRAFFIC_CD prefix
        traff_df["EdgeFrmPos"] = traff_df["TMC"].str.startswith("-")
        traff_df["EdgeToPos"] = ~traff_df["EdgeFrmPos"]
        traff_df["EdgeFrmPos"] = traff_df["EdgeFrmPos"].astype(int)
        traff_df["EdgeToPos"] = traff_df["EdgeToPos"].astype(int)
        return traff_df

    @timed_exec
    def _read_tmc_files(self):
        """Read in the TMC traffic tables and use them to populate the Streets_Patterns table."""
        # Read TMC reference table
        tmc_df = pd.read_csv(self.in_data_object.tmc_ref_table, usecols=["TMC"] + DAY_FIELDS)
        tmc_df.set_index("TMC", inplace=True)
        # Read TMC Traffic table
        traff_df = self._read_tmc_traffic_table()
        # Standardize the TMC code to the same format as the other table
        traff_df["TMC"] = traff_df["TMC"].str.lstrip("+").str.lstrip("-").str.replace("+", "P", regex=False).str.replace("-", "N", regex=False)
        # Join the dataframes and keep only rows that are in both
        traff_df = traff_df.join(tmc_df, "TMC", how="inner")
        return traff_df

    @timed_exec
    def _read_and_process_historical_traffic_tables(self):
        """Read the relevant historical traffic tables based on the provided inputs and do some calculations."""
        if not self.include_historical_traffic:
            return
        assert self.spd_df is not None

        # Read and process the appropriate input tables
        if self.historical_traffic_type is HistoricalTrafficConfigType.LinkReferenceFiles:
            self._add_message("Populating Streets_Patterns table from link reference tables...")
            self.traff_df = self._read_link_ref_files()
        elif self.historical_traffic_type is HistoricalTrafficConfigType.TMCReferenceFiles:
            self._add_message("Populating Streets_Patterns table from TMC traffic tables...")
            self.traff_df = self._read_tmc_files()
        else:
            raise NotImplementedError(f"Unknown historical traffic config type: {self.historical_traffic_type}")

        # Calculate AverageSpeed and BaseSpeed for each weekday based on info in the Patterns table
        for day in DAY_FIELDS:
            self.traff_df = self.traff_df.join(self.spd_df[["AverageSpeed", "BaseSpeed", "OID"]], day)
            self.traff_df.rename(
                columns={
                    "AverageSpeed": f"AverageSpeed_{day}",
                    "BaseSpeed": f"BaseSpeed_{day}"
                },
                inplace=True
            )
            # Update day field to reference the Patterns OIDs instead of PatternID.
            self.traff_df[day] = self.traff_df["OID"]
            self.traff_df.drop(columns=["OID"], inplace=True)

        # Calculate overall AverageSpeed and BaseSpeed
        # To get the freeflow speed, use the weighted harmonic mean of these seven speeds as follows:
        # vAvg = ( (1/v1)+(1/v2)+(1/v3)+(1/v4)+(1/v5)+(1/v6)+(1/v7) ) /
        #        ( (1/v1^2)+(1/v2^2)+(1/v3^2)+(1/v4^2)+(1/v5^2)+(1/v6^2)+(1/v7^2) )
        self.traff_df["AverageSpeed"] = sum(1/self.traff_df[f"AverageSpeed_{day}"] for day in DAY_FIELDS) / \
            sum(1/(self.traff_df[f"AverageSpeed_{day}"]**2) for day in DAY_FIELDS)
        self.traff_df["BaseSpeed"] = sum(1/self.traff_df[f"BaseSpeed_{day}"] for day in DAY_FIELDS) / \
            sum(1/(self.traff_df[f"BaseSpeed_{day}"]**2) for day in DAY_FIELDS)

        # Drop temporary fields
        self.traff_df.drop(
            columns=[f"AverageSpeed_{day}" for day in DAY_FIELDS] + \
                [f"BaseSpeed_{day}" for day in DAY_FIELDS],
            inplace=True
        )

    @timed_exec
    def _create_and_populate_streets_patterns_table(self):
        """Create and populate the Streets_Patterns table."""
        if not self.include_historical_traffic:
            return
        self._add_message("Creating and populating Streets_Patterns table...")
        assert self.spd_df is not None
        assert self.traff_df is not None
        assert self.streets_df is not None

        # Create the table with desired schema
        arcpy.management.CreateTable(
            os.path.dirname(self.streets_profiles),
            os.path.basename(self.streets_profiles)
        )
        field_defs = [["LINK_ID", "LONG"]]
        if self.historical_traffic_type is HistoricalTrafficConfigType.TMCReferenceFiles:
            field_defs.append(["TMC", "TEXT", "", 9])
        field_defs += [[day, "SHORT"] for day in DAY_FIELDS]
        field_defs += [
            ["EdgeFCID", "LONG"],
            ["EdgeFID", "LONG"],
            ["EdgeFrmPos", "DOUBLE"],
            ["EdgeToPos", "DOUBLE"],
            ["AverageSpeed", "FLOAT"],
            ["BaseSpeed", "FLOAT"]
        ]
        arcpy.management.AddFields(self.streets_profiles, field_defs)

        # Join info from the Streets dataframe (both are indexed by LINK_ID at this point)
        self.traff_df = self.traff_df.join(self.streets_df[["Meters", "OID"]])
        self.traff_df.rename(columns={"OID": "EdgeFID"}, inplace=True)
        self.traff_df.reset_index(inplace=True)

        # Write the records to the Streets_Patterns table
        out_fields = [
            "EdgeFCID",
            "AverageSpeed", "BaseSpeed",
            "EdgeFrmPos", "EdgeToPos", "EdgeFID",
            "LINK_ID"
        ] + DAY_FIELDS
        if "TMC" in self.traff_df.columns:
            out_fields.append("TMC")
        with arcpy.da.InsertCursor(self.streets_profiles, out_fields) as cur:
            for _, traff_record in self.traff_df.iterrows():
                new_row = [self.fc_id] + [traff_record[f] for f in out_fields[1:]]
                cur.insertRow(new_row)

    @timed_exec
    def _create_and_populate_streets_tmc_table(self):
        if not self.include_live_traffic:
            return
        self._add_message("Creating and populating Streets_TMC table...")
        assert self.streets_df is not None  # Confidence check
        field_names = self._create_streets_tmc_table()

        # Read TMC Traffic table
        traff_df = self._read_tmc_traffic_table()
        # Standardize the TMC code to the same format required by live traffic
        # Note: Not the same as what gets put in Streets_Patterns
        traff_df["TMC"] = traff_df["TMC"].str.lstrip("+").str.lstrip("-")
        # Calculate EdgeFID by joining info from the Streets dataframe
        traff_df = traff_df.join(self.streets_df[["OID"]], "LINK_ID")
        traff_df.rename(columns={"OID": "EdgeFID"}, inplace=True)

        field_names = ["EdgeFCID"] + [f for f in field_names if f != "EdgeFCID"]
        with arcpy.da.InsertCursor(self.streets_tmc, field_names) as cur:
            for _, traff_record in traff_df.iterrows():
                new_row = [self.fc_id] + [traff_record[f] for f in field_names[1:]]
                cur.insertRow(new_row)

    @timed_exec
    def _read_and_index_turn_tables(self):
        """Read and index turn tables."""
        self._add_message("Reading and indexing restricted turn tables...")
        fields = ["LINK_ID", "MAN_LINKID", "COND_ID", "SEQ_NUMBER"]
        with arcpy.da.SearchCursor(self.in_data_object.rdms, fields) as cur:
            rdms_df = pd.DataFrame(cur, columns=fields)
        rdms_df.set_index("COND_ID", inplace=True)

        # Read cdms table again to retrieve turn restrictions
        if self.use_transport_fields:
            where = "COND_TYPE IN (4, 7, 26)"
        else:
            where = "COND_TYPE IN (4, 7)"
        fields = ["COND_ID", "COND_TYPE", "END_OF_LK"] + ["AR_" + suf for suf in AR_FLD_SUFS]
        with arcpy.da.SearchCursor(self.in_data_object.cdms, fields, where) as cur:
            cdms_df = pd.DataFrame(cur, columns=fields)
        cdms_df.set_index("COND_ID", inplace=True)

        # Join the cdms table to the rdms table to transfer END_OF_LK and to drop rows that don't match the COND_TYPE
        rdms_df = rdms_df.join(cdms_df, how="inner")
        rdms_df.reset_index(inplace=True)

        # Determine the max number of edges participating in a turn. This will be used when creating the turn feature
        # class to initialize the proper number of fields.
        self.max_turn_edges = int(rdms_df["SEQ_NUMBER"].max()) + 1

        # Group by LINK_ID to ensure that turn maneuver records are grouped together
        self.grouped_rdms_df = rdms_df.groupby(["COND_ID", "LINK_ID"])

    @timed_exec
    def _generate_turn_features(self):
        """Generate the turn features and insert them into the turn feature class."""
        self._add_message("Populating turn feature class...")
        assert self.streets_df is not None
        assert self.grouped_rdms_df is not None
        assert self.max_turn_edges is not None

        def calc_cndmod_fields(row, cndmod_record):
            """Calculate restriction field values from data in the cndmod tables."""
            mod_type = cndmod_record["MOD_TYPE"]
            mod_val = cndmod_record["MOD_VAL"]

            # Calculate a value for a prohibit restriction
            if mod_type == 39:
                # Figure out which prohibit restriction this is for
                prohib_field = self.prohib_suffixes.get(mod_val)
                if not prohib_field:
                    # Invalid or irrelevant record.  Return row unchanged.
                    return row
                row[cndmod_turn_fname_idx[prohib_field]] = "Y"

            # Calculate a value for a limit restriction field
            elif mod_type in self.limit_suffixes.keys():
                mod_val = cndmod_record["MOD_VAL_U"]
                limit_field = self.limit_suffixes[mod_type]
                row[cndmod_turn_fname_idx[limit_field]] = mod_val

            # Handle a couple of specific restriction cases
            elif mod_type == 46:
                if mod_val in ('1', '2', '3'):
                    row[cndmod_turn_fname_idx["MaxTrailersAllowedOnTruck"]] = int(mod_val)
                elif mod_val == "4":
                    row[cndmod_turn_fname_idx["SemiOrTractorWOneOrMoreTrailersProhibited"]] = "Y"
            elif mod_type == 75:
                if mod_val in ('1', '2', '3', '4', '5'):
                    row[cndmod_turn_fname_idx["MaxAxlesAllowed"]] = int(mod_val)
                elif mod_val == '6':
                    row[cndmod_turn_fname_idx["SingleAxleProhibited"]] = "Y"
                elif mod_val == '7':
                    row[cndmod_turn_fname_idx["TandemAxleProhibited"]] = "Y"

            return row

        # Create a list of turn fields based on the max turn edges and standard turn feature class schema
        turn_fields = ["SHAPE@", "COND_ID", "COND_TYPE", "Edge1End"]
        for idx in range(1, self.max_turn_edges + 1):
            turn_fields += [f"Edge{idx}FCID", f"Edge{idx}FID", f"Edge{idx}Pos"]
        # Add restriction fields
        turn_fields += AR_FLDS
        cndmod_turn_fname_idx = None
        if self.cndmod_df is not None:
            # Add more restriction fields
            added_turn_restr_fields = list(self.prohib_suffixes.values()) + [f[0] for f in self.addl_turn_field_defs]
            cndmod_turn_fname_idx = {f: i for i, f in enumerate(added_turn_restr_fields)}  # {Field name: index}
            turn_fields += added_turn_restr_fields
            # Update index of cndmod_df to COND_ID for quick lookups for turn records
            self.cndmod_df.reset_index(inplace=True)
            self.cndmod_df.set_index("COND_ID", inplace=True)

        # Open an insert cursor so we can add entries to the turn feature class. We will build the rows below.
        with arcpy.da.InsertCursor(self.turns, turn_fields) as cur_t:

            # For each LINK_ID in the input rdms table, grab the records and build the geometry
            # Set up progressor
            arcpy.SetProgressor("step", "Populating turn feature class...", 0, len(self.grouped_rdms_df), 1)
            turn_oid = 1
            for cond_link_id, group in self.grouped_rdms_df:
                arcpy.SetProgressorPosition(turn_oid)
                cond_id, link_id = cond_link_id

                # Generate the values for the turn edge fields and create the turn geometry
                edge_fields = []  # Store the Edge#FCID, Edge#FID, Edge#Pos fields to insert
                edge_geom = []  # Store the polyline geometry of the edges participating in the turn

                # Loop through all manuever records associated with this LINK_ID and generate the edge fields
                # Also query the streets table to get the edge geometry to build the geometry for the turn
                # First, identify a list of link IDs associated with the turn maneuver.  The first edge is always the
                # LINK_ID field for the group, and subsequent edges are in the MAN_LINKID field in the rdms rows.
                group.sort_values("SEQ_NUMBER", inplace=True)
                if len(group) > len(group["SEQ_NUMBER"].unique()):
                    arcpy.AddWarning((
                        f"Duplicate SEQ_NUMBER values detected for the turn feature described by LINK_ID {link_id} "
                        f"and COND_ID {cond_id} (output turn ObjectID {turn_oid}). This likely indicates and input "
                        " data error and may result in an invalid output turn feature."
                    ))
                first_row = group.iloc[0]
                turn_link_ids = [link_id] + group["MAN_LINKID"].to_list()
                num_edges = len(turn_link_ids)  # Count the number of edges participating
                if num_edges > self.max_turn_edges:
                    # This should technically never happen because the turn feature class is explicitly created to
                    # allow the maximum number of edges found in the input data. However, check just to be safe.
                    arcpy.AddWarning((
                        f"The turn with {link_id} in the rdms table has more associated edges than the "
                        f"maximum allowed edges for a turn ({self.max_turn_edges}) and will be truncated."
                    ))
                    continue

                # Figure out the Edge1End value
                end_of_lk_val = first_row["END_OF_LK"]
                if end_of_lk_val == "N":
                    edge1_end = "Y"
                elif end_of_lk_val == "R":
                    edge1_end = "N"
                else:
                    edge1_end = "?"

                valid = True
                for turn_link_id in turn_link_ids:
                    # Retrieve the street record
                    try:
                        # Find the street record associated with this edge in the turn manuever path
                        street = self.streets_df.loc[turn_link_id]
                    except KeyError:
                        arcpy.AddWarning((
                            f"The Streets table is missing an entry with LINK_ID {turn_link_id}, which is used in the "
                            "rdms table."))
                        valid = False
                        break
                    # Construct the edge fields for this segment
                    edge_fields += [self.fc_id, street["OID"], self.edge_pos]
                    # Store the geometry for this segment
                    edge_geom.append(self._get_street_geometry(turn_link_id, street["OID"]))

                if not valid:
                    # Something went wrong in constructing the turn. Skip it and move on.
                    continue
                # Add empty records for the remaining turn edge fields if this turn doesn't use the max available
                for _ in range(self.max_turn_edges - num_edges):
                    edge_fields += [None, None, None]

                # Build turn geometry
                turn_geom = self._build_turn_geometry(edge_geom, edge1_end, turn_oid)

                # Generate the values for the standard restriction fields
                restriction_values = []
                for restr_fld in AR_FLDS:
                    restriction_values.append(first_row[restr_fld])

                # Populate the restriction fields associated with the transport condition modifier table
                cndmod_restr_values = []
                if self.cndmod_df is not None:
                    # Initialize transport restriction field values to None.
                    cndmod_restr_values = [None] * len(cndmod_turn_fname_idx)
                    try:
                        # Retrieve the restriction records for this ID
                        subset_df = self.cndmod_df.loc[cond_id]
                        # Loop through the transport condition modifier records and update the appropriate rows
                        if isinstance(subset_df, pd.Series):
                            # There was only one record with this ID, so pandas returns a series
                            cndmod_restr_values = calc_cndmod_fields(cndmod_restr_values, subset_df)
                        else:
                            # There were multiple records with this ID, so pandas returns a dataframe.
                            for _, record in subset_df.iterrows():
                                cndmod_restr_values = calc_cndmod_fields(cndmod_restr_values, record)
                    except KeyError:
                        # There were no cndmod restriction records for this ID. Just skip it and move on.
                        pass

                    # Populate AllTransportProhibited restriction field
                    if first_row["COND_TYPE"] == 26:
                        is_restricted = True
                        for restr_value in cndmod_restr_values:
                            if restr_value is not None:
                                is_restricted = False
                                break
                        if is_restricted:
                            cndmod_restr_values[cndmod_turn_fname_idx["AllTransportProhibited"]] = "Y"

                # Construct the final row and insert it
                turn_row = [turn_geom, cond_id, first_row["COND_TYPE"], edge1_end] + \
                    edge_fields + restriction_values + cndmod_restr_values
                cur_t.insertRow(turn_row)
                turn_oid += 1

        arcpy.ResetProgressor()

    @timed_exec
    def _create_and_populate_road_forks(self):
        """Create and populate the road splits table."""
        self._add_message("Creating and populating road forks table...")
        assert self.streets_df is not None

        # Create the table
        self._create_road_forks_table()

        # Read rdms table again to handle road forks
        fields = ["LINK_ID", "MAN_LINKID", "COND_ID"]
        with arcpy.da.SearchCursor(self.in_data_object.rdms, fields) as cur:
            rdms_df = pd.DataFrame(cur, columns=fields)
        rdms_df.set_index("COND_ID", inplace=True)

        # Read cdms table again to select rows representing road forks
        where = "COND_TYPE = 9"
        fields = ["COND_ID"]  # ["COND_ID", "END_OF_LK"]
        with arcpy.da.SearchCursor(self.in_data_object.cdms, fields, where) as cur:
            cdms_df = pd.DataFrame(cur, columns=fields)
        cdms_df.set_index("COND_ID", inplace=True)

        # Join the cdms table to the rdms table to transfer END_OF_LK and to drop rows that don't match the COND_TYPE
        rdms_df = rdms_df.join(cdms_df, how="inner")
        rdms_df.reset_index(inplace=True)

        # Group by LINK_ID to ensure that road fork records are grouped together
        rf_grouped_rdms_df = rdms_df.groupby(["LINK_ID"])

        # Set up output fields in an order that is easy to work with
        field_prefixes = ["Edge", "Branch0", "Branch1", "Branch2"]
        fields = []
        for pref in field_prefixes:
            fields += [f"{pref}FCID", f"{pref}FID"]
        for pref in field_prefixes:
            fields += [f"{pref}FrmPos", f"{pref}ToPos"]
        # Open an insert cursor so we can add entries to the turn feature class. We will build the rows below.
        with arcpy.da.InsertCursor(self.road_splits, fields) as cur:

            # For each LINK_ID in the input rdms table, grab the records and build the sequence of edge IDs
            for link_id, group in rf_grouped_rdms_df:
                if isinstance(link_id, tuple):
                    # In newer versions of pandas, groupby keys come back as tuples, so just get the first item
                    # in the tuple
                    link_id = link_id[0]

                # Generate the values for the road for edge IDs
                edge_fields = []  # Store the Edge#FCID, Edge#FID, Branch#FCID... fields to insert

                # Loop through all manuever records associated with this LINK_ID and generate the edge fields
                # First, identify a list of link IDs associated with the road fork.  The first edge is always the
                # LINK_ID field for the group, and subsequent edges are in the MAN_LINKID field in the rdms rows.
                rf_link_ids = [link_id] + group["MAN_LINKID"].to_list()
                num_edges = len(rf_link_ids)  # Count the number of edges participating
                if num_edges > self.max_road_splits:
                    # There are more than four parts to the fork. These entries should be ignored, as we don't support
                    # reporting 4-way (or more) forks. Truncate the road fork record and throw a warning.  This should
                    # be rare or should never happen.
                    arcpy.AddWarning((
                        f"The road fork starting with LINK_ID {link_id}, has more than {self.max_road_splits} parts. "
                        f"Because the network dataset does not support more than {self.max_road_splits} parts, the "
                        "road fork record will be truncated."
                    ))
                    rf_link_ids = rf_link_ids[:self.max_road_splits]
                    num_edges = len(rf_link_ids)
                # Check that the fork had enough edges to be valid
                if num_edges < 3:
                    arcpy.AddWarning(f"The road fork starting with LINK_ID {link_id} has too few maneuvers.")
                    continue

                valid = True
                ref_ids = []
                for rf_link_id in rf_link_ids:
                    # Retrieve the street record
                    try:
                        # Find the street record associated with this edge in the road fork
                        street = self.streets_df.loc[rf_link_id]
                    except KeyError:
                        arcpy.AddWarning((
                            f"The Streets table is missing an entry with LINK_ID {rf_link_id}, which is used in the "
                            "rdms table."))
                        valid = False
                        break
                    # Store REF_IN_ID and NREF_IN_ID for each segment so we can determine directionality after we
                    # retrieve them all
                    ref_ids.append((street["REF_IN_ID"], street["NREF_IN_ID"]))

                    # Construct the edge fields for this segment
                    edge_fields += [self.fc_id, street["OID"]]

                if not valid:
                    # Something went wrong in constructing the road fork. Skip it and move on.
                    continue

                # Add empty records for the remaining road fork edge fields if this the max available aren't used
                for _ in range(self.max_road_splits - num_edges):
                    edge_fields += [None, None]

                # The REF_IN_ID is the ID for the "from" endpoint of the street, while the NREF_IN_ID is the ID for
                # the "to" end of the street. Determine the directionality of the road fork segments by matching up
                # the IDs of the endpoints of adjacent segments
                edge_pos_fields = []
                # Specially handle the first edge
                if ref_ids[0][0] == ref_ids[1][0] or ref_ids[0][0] == ref_ids[1][1]:
                    # First edge is against the direction of digitization
                    edge_pos_fields += [1, 0]
                    prev_end_id = ref_ids[0][0]
                else:
                    # First edge is in the direction of digitization
                    edge_pos_fields += [0, 1]
                    prev_end_id = ref_ids[0][1]
                for ref_id in ref_ids[1:]:
                    if ref_id[0] == prev_end_id:
                        # Edge is in the direction of digitization
                        edge_pos_fields += [0, 1]
                        prev_end_id = ref_id[1]
                    else:
                        # Edge is against the direction of digitization
                        edge_pos_fields += [1, 0]
                        prev_end_id = ref_id[0]

                # Add empty records for the remaining road fork edge fields if this the max available aren't used
                for _ in range(self.max_road_splits - num_edges):
                    edge_pos_fields += [None, None]

                # Insert the road fork row
                new_row = edge_fields + edge_pos_fields
                cur.insertRow(new_row)

    @timed_exec
    def _populate_signposts_and_signposts_streets(self):
        "Populate the Signposts feature class and Signposts_Streets table."
        self._add_message("Populating the Signposts feature class and Signposts_Streets table...")
        assert self.streets_df is not None
        # Read the signs table
        fields = ["SEQ_NUM", "EXIT_NUM", "SRC_LINKID", "DST_LINKID", "LANG_CODE", "BR_RTEID", "BR_RTEDIR", "SIGN_TEXT",
                  "SIGN_TXTTP", "TOW_RTEID", "SIGN_ID"]
        with arcpy.da.SearchCursor(self.in_data_object.signs, fields) as cur:
            signs_df = pd.DataFrame(cur, columns=fields)
        grouped_signs_df = signs_df.groupby(["SRC_LINKID", "DST_LINKID"], sort=True)

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

                    # For each item in the signs table, grab the records and build the sigposts and signposts_streets
                    # records
                    signpost_oid = 1
                    for link_ids, group in grouped_signs_df:

                        # First, build the signpost_streets records and the signpost geometry

                        # Retrieve associated streets records
                        link_id_src, link_id_dst = link_ids
                        try:
                            first_street = self.streets_df.loc[link_id_src]
                        except KeyError:
                            arcpy.AddWarning((
                                f"The Streets table is missing an entry with ID {link_id_src}, which is used in the "
                                "signpost table."))
                            # Something went wrong in constructing the signpost. Skip it and move on.
                            continue
                        try:
                            second_street = self.streets_df.loc[link_id_dst]
                        except KeyError:
                            arcpy.AddWarning((
                                f"The Streets table is missing an entry with ID {link_id_dst}, which is used in the "
                                "signpost table."))
                            # Something went wrong in constructing the signpost. Skip it and move on.
                            continue

                        # The REF_IN_ID is the ID for the "from" endpoint of the street, while the NREF_IN_ID is the ID
                        # for the "to" end of the street. Determine the directionality of the signposts segments by
                        # matching up the IDs of the endpoints of adjacent segments.
                        # Note: HERE only provides the first and last edges of the sequence.  If the edge sequence
                        # consists of more than two edges, then the signpost geometries can end up as a multipart line
                        # feature consisting of two disjoint road segments.  In this case, the ID fields below won't
                        # match up, so just insert the geometry segments as is without reversing them.
                        reverse_first = False
                        reverse_second = False
                        is_disjoint = True
                        if first_street["REF_IN_ID"] == second_street["REF_IN_ID"]:
                            # First edge is against the direction of digitization
                            # Second edge is in the direction of digitization
                            reverse_first = True
                            is_disjoint = False
                        elif first_street["REF_IN_ID"] == second_street["NREF_IN_ID"]:
                            # First edge is against the direction of digitization
                            # Second edge is against the direction of digitization
                            reverse_first = True
                            reverse_second = True
                            is_disjoint = False
                        elif first_street["NREF_IN_ID"] == second_street["NREF_IN_ID"]:
                            # First edge is in the direction of digitization
                            # Second edge is against the direction of digitization
                            reverse_second = True
                            is_disjoint = False
                        elif first_street["NREF_IN_ID"] == second_street["REF_IN_ID"]:
                            # First edge is in the direction of digitization
                            # Second edge is in the direction of digitization
                            is_disjoint = False

                        # Trim the first edge to the last 25% of the street feature and reverse if needed
                        # Also set the appropriate values for the EdgeFrmPos and EdgeToPos fields
                        first_edge = self._get_street_geometry(link_id_src, first_street["OID"])
                        if reverse_first:
                            signpost_vertices_1 = self._reverse_polyline(
                                first_edge.segmentAlongLine(0, 0.25, use_percentage=True))
                            pos_fields_1 = [1, 0]
                        else:
                            signpost_vertices_1 = self._polyline_to_points(
                                first_edge.segmentAlongLine(0.75, 1, use_percentage=True))
                            pos_fields_1 = [0, 1]
                        # Trim the second edge to the first 25% of the street feature and reverse if needed
                        # Also set the appropriate values for the EdgeFrmPos and EdgeToPos fields
                        second_edge = self._get_street_geometry(link_id_dst, second_street["OID"])
                        if reverse_second:
                            signpost_vertices_2 = self._reverse_polyline(
                                second_edge.segmentAlongLine(0.75, 1, use_percentage=True))
                            pos_fields_2 = [1, 0]
                        else:
                            signpost_vertices_2 = self._polyline_to_points(
                                second_edge.segmentAlongLine(0, 0.25, use_percentage=True))
                            pos_fields_2 = [0, 1]

                        # Construct the signpost geometry by combining the updated geometry for the first and second
                        # edge segments. Note: If the edge segments are disjoint, it is possible to create a multipart
                        # feature that contains only the segment geometry like this:
                        # vertex_array = arcpy.Array([signpost_vertices_1, signpost_vertices_2])
                        # However, we decided to connect the disjoint segments with a straight line, so just use the
                        # vertices all in one part.  The code above still identifies disjoint segments in case we want
                        # to change this in the future.
                        vertex_array = arcpy.Array(signpost_vertices_1 + signpost_vertices_2)
                        signpost_geom = arcpy.Polyline(vertex_array, self.in_data_object.sr)

                        # Add the records to the Signposts_Streets table
                        # ["SignpostID", "Sequence", "EdgeFCID", "EdgeFID", "EdgeFrmPos", "EdgeToPos"]
                        # The first edge is always Sequence = 1, and the last edge is Sequence = 0 if the exact edge
                        # sequence is not known, as is the case here. I'm not sure why it was designed this way, but
                        # this 1-0 sequene is intentional.
                        cur_si.insertRow([signpost_oid, 1, self.fc_id, first_street["OID"]] + pos_fields_1)
                        cur_si.insertRow([signpost_oid, 0, self.fc_id, second_street["OID"]] + pos_fields_2)

                        # Loop through the Signs records in this group to construct the branch and toward fields
                        branch_fields = []
                        toward_fields = []
                        group.sort_values("SEQ_NUM", inplace=True)
                        for _, record in group.iterrows():
                            # Exit name should be the same for all rows but retrieve it for each anyway
                            exit_name = record["EXIT_NUM"]
                            # Shared values for branch and toward records
                            lang = LNG_CODES[record["LANG_CODE"]]
                            branch_dir = record["BR_RTEDIR"]
                            # Construct the first set of Branch* fields from BR_RTEID and BR_RTEDIR
                            branch_text = record["BR_RTEID"].strip()
                            if branch_text:
                                branch_fields += [branch_text, branch_dir, lang]
                            # Construct additional Branch* or Toward* fields from the SIGN_TEXT field
                            # if SIGN_TXTTP == "B", populate a branch
                            # SIGN_TXTTP == "T", populate a toward
                            sign_text = record["SIGN_TEXT"].strip()
                            if sign_text:
                                sign_type = record["SIGN_TXTTP"]
                                if sign_type == "B":
                                    branch_fields += [sign_text, branch_dir, lang]
                                elif sign_type == "T":
                                    toward_fields += [sign_text, lang]
                            # Construct the final set of Toward* fields from the TOW_RTEID field
                            toward_text = record["TOW_RTEID"].strip()
                            if toward_text:
                                toward_fields += [toward_text, lang]

                        # Truncate the record if we have too many branch and toward fields
                        truncate = False
                        if len(branch_fields) > 3 * self.max_signpost_branches:
                            truncate = True
                            branch_fields = branch_fields[:3 * self.max_signpost_branches]
                        if len(toward_fields) > 2 * self.max_signpost_branches:
                            truncate = True
                            toward_fields = toward_fields[:2 * self.max_signpost_branches]
                        if truncate:
                            sign_id = group.iloc[0]['SIGN_ID']
                            arcpy.AddWarning((
                                "There were too many records in the input Signs table for SIGN_ID "
                                f"{sign_id}. The signpost (OID {signpost_oid}) will be truncated."
                            ))

                        # Fill in remaining branch and toward fields with None if the record doesn't use all of them
                        for _ in range(3 * self.max_signpost_branches - len(branch_fields)):
                            branch_fields.append(None)
                        for _ in range(2 * self.max_signpost_branches - len(toward_fields)):
                            toward_fields.append(None)

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

    @timed_exec
    def _create_and_build_nd(self):
        """Create the network dataset from the appropriate template and build the network."""
        self._add_message("Creating network dataset...")

        # Choose the correct base template file based on settings
        template_name = f"HEREshp_{self.unit_type.name}"
        if self.include_historical_traffic:
            template_name += "_Traffic"
        if self.use_transport_fields:
            template_name += "_Transport"
        template = os.path.join(CURDIR, "NDTemplates", template_name + ".xml")
        assert os.path.exists(template)

        # Update the template with dynamic content for the current situation, if needed. We'll use a temporary template
        # file, which we will delete when we're done with it.
        temp_template = os.path.join(
            arcpy.env.scratchFolder, f"NDTemplate_{uuid.uuid4().hex}.xml")  # pylint: disable=no-member

        # Update the template dynamically to include the TMC table and field name for live traffic, if relevant
        if self.include_live_traffic:
            template = self._update_nd_template_with_live_traffic(template, temp_template)

        # Update the template dynamically to include the time zone attribute and its evaluators
        if self.time_zone_type != TimeZoneType.NoTimeZone:
            template = self._update_nd_template_with_time_zone(template, temp_template)

        # Create the network dataset from the template and build it if requested
        self._create_nd_from_template_and_build(template)

        # Clean up temporary edited template if needed
        if os.path.exists(temp_template):
            os.remove(temp_template)


if __name__ == '__main__':
    print("Please run this code via the script tool interface.")
