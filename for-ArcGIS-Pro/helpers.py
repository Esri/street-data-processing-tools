"""Street data processing tool helper functions

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
import time
import datetime
import functools
from enum import Enum
from lxml import etree
import psutil
import pandas as pd
import arcpy

PRINT_TIMINGS = False  # Set to True to log timings for various methods (primarily for debugging and development)

CURDIR = os.path.dirname(os.path.abspath(__file__))


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


class TimeZoneType(Enum):
    """Defines the time zone type to use."""

    NoTimeZone = 1
    Single = 2
    Table = 3


class UnitType(Enum):
    """Defines whether the units are imperial or metric."""

    Imperial = 1
    Metric = 2


class DataProductType(Enum):
    """Defines the data product being processed. Primarily used for switching behavior in shared classes."""

    TomTomMultinet = 1
    HereNavStreetsShp = 2


class StreetInputData:
    """Parent class that defines a collection of inputs to process."""

    def __init__(self, required_tables: list, required_fields: dict, sr_input: str):
        """Initialize an input dataset."""
        self.required_tables = required_tables
        self.required_fields = required_fields
        self.sr_input = sr_input
        self._sr = None  # Set with property

    @property
    def sr(self):
        """The spatial reference of the input data."""
        if self._sr is None:
            self._sr = arcpy.Describe(self.sr_input).spatialReference
        return self._sr

    def _validate_arcgis_table(self, table):
        """Validate an arcgis input table."""
        if not arcpy.Exists(table):
            arcpy.AddError(f"Input table {table} does not exist.")
            return False
        actual_fields = {(f.name, f.type) for f in arcpy.ListFields(table)}
        if not set(self.required_fields[table]).issubset(actual_fields):
            arcpy.AddError((
                f"Input table {table} does not have the correct schema. "
                f"Required fields: {self.required_fields[table]}"))
            return False
        if int(arcpy.management.GetCount(table).getOutput(0)) == 0:
            arcpy.AddWarning(f"Input table {table} has no rows.")
        return True

    def _validate_csv_table(self, table):
        """Validate a CSV input table."""
        if not os.path.exists(table):
            arcpy.AddError(f"Input table {table} does not exist.")
            return False
        bad_schema_msg = (
            f"Input table {table} does not have the correct schema. "
            f"Required fields: {self.required_fields[table]}"
        )
        # Read the first few rows of the CSV into a dataframe to validate it
        try:
            df = pd.read_csv(table, nrows=2)
        except pd.errors.EmptyDataError:
            # Table is totally empty, so there isn't even a schema
            arcpy.AddError(bad_schema_msg)
        if not set(self.required_fields[table]).issubset(set(df.columns)):
            arcpy.AddError(bad_schema_msg)
            return False
        if df.empty:
            arcpy.AddWarning(f"Input table {table} has no rows.")
        return True

    def validate_data(self):
        """Validate that the data exists and has the required fields."""
        # Check that all tables that need to be specified are specified.
        for table in self.required_tables:
            if not table:
                arcpy.AddError("Required input table not specified.")
                return False

        # Verify existence of tables and appropriate schema
        for table in [t for t in self.required_fields if t]:
            if isinstance(table, str) and table.endswith(".csv"):
                if not self._validate_csv_table(table):
                    return False
            else:
                if not self._validate_arcgis_table(table):
                    return False

        if self.sr.name == "Unknown":
            arcpy.AddError("The input data has an unknown spatial reference.")
            return False

        # Everything is valid
        return True


class StreetDataProcessor:
    """Parent class with variables and helper methods applicable to all street data processing classes."""

    def __init__(
        self, data_product: DataProductType, out_folder: str, gdb_name: str, in_data_object, unit_type: UnitType,
        include_historical_traffic: bool,
        time_zone_type: TimeZoneType, time_zone_name: str = "", in_time_zone_table=None,
        time_zone_ft_field: str = None, time_zone_tf_field: str = None, build_network: bool = True
    ):
        """Initialize a class to process MultiNet data into a network dataset."""
        self.data_product = data_product
        if self.data_product not in DataProductType:
            raise NotImplementedError(f"DataProductType not yet handled: {self.data_product}")
        self.in_data_object = in_data_object
        self.unit_type = unit_type
        self.include_historical_traffic = include_historical_traffic
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
        # Schema is a little different depending on data product
        if self.data_product is DataProductType.TomTomMultinet:
            self.profiles = os.path.join(self.out_folder, self.gdb_name, "DailyProfiles")
            self.streets_profiles = os.path.join(self.out_folder, self.gdb_name, "Streets_DailyProfiles")
        elif self.data_product is DataProductType.HereNavStreetsShp:
            self.profiles = os.path.join(self.out_folder, self.gdb_name, "Patterns")
            self.streets_profiles = os.path.join(self.out_folder, self.gdb_name, "Streets_Patterns")
        self.streets_tmc = os.path.join(self.out_folder, self.gdb_name, "Streets_TMC")
        self.time_zone_table = os.path.join(self.out_folder, self.gdb_name, "TimeZones")
        self.network = os.path.join(self.feature_dataset, "Routing_ND")

        # Global variables hard-coded or initialized later
        self.streets_oid_field = None  # OID field name of the output streets feature class
        if self.data_product is DataProductType.TomTomMultinet:
            self.streets_id_field_name = "ID"
            self.id_field_type = "DOUBLE"
        elif self.data_product is DataProductType.HereNavStreetsShp:
            self.streets_id_field_name = "LINK_ID"
            self.id_field_type = "LONG"
        self.fc_id = None  # Streets feature class dataset ID used in Edge#FCID fields
        self.max_turn_edges = None  # Maximum number of edges participating in a turn
        self.max_road_splits = 4  # Max allowed road split edges
        self.max_signpost_branches = 10  # Number of signpost branches
        self.edge_pos = 0.5  # Edge#Pos field values in turns are intentionally hard-coded
        self.streets_df = None  # Dataframe of output streets indexed by ID for quick lookups
        self.intermediate_outputs = []

    @timed_exec
    def _create_feature_dataset(self):
        """Create the output geodatabase and feature dataset."""
        self._add_message(f"Creating output geodatabase and feature dataset at {self.feature_dataset}...")
        arcpy.management.CreateFileGDB(self.out_folder, self.gdb_name)
        arcpy.management.CreateFeatureDataset(
            os.path.dirname(self.feature_dataset),
            os.path.basename(self.feature_dataset),
            self.in_data_object.sr
        )

    @timed_exec
    def _validate_inputs(self):
        """Validate the input data."""
        self._add_message("Validating inputs...")

        # Do some simple checks
        if not os.path.exists(self.out_folder):
            arcpy.AddError(f"Output folder {self.out_folder} does not exist.")
            return False
        if os.path.exists(os.path.join(self.out_folder, self.gdb_name)):
            arcpy.AddError(f"Output geodatabase {os.path.join(self.out_folder, self.gdb_name)} already exists.")
            return False

        # Make sure the license is available.
        try:
            arcpy.CheckOutExtension("network")
        except Exception:  # pylint:disable=broad-except
            arcpy.AddError("The Network Analyst extension license is unavailable.")
            return False

        # Check the input data
        if not self.in_data_object.validate_data():
            return False

        arcpy.AddMessage("Inputs validated successfully.")
        return True

    @timed_exec
    def _spatially_sort_streets(self):
        """Spatially sort the input streets feature class."""
        if self.data_product is DataProductType.TomTomMultinet:
            streets_to_sort = self.in_data_object.nw
        elif self.data_product is DataProductType.HereNavStreetsShp:
            streets_to_sort = self.in_data_object.streets
        temp_sorted_streets = arcpy.CreateUniqueName(
            "TempSDPTStreets", arcpy.env.scratchGDB)  # pylint:disable = no-member
        shape_field = arcpy.Describe(streets_to_sort).shapeFieldName
        try:
            arcpy.management.Sort(
                streets_to_sort,
                temp_sorted_streets,
                [[shape_field, "ASCENDING"]], "PEANO"
            )
            self.intermediate_outputs.append(temp_sorted_streets)
            return temp_sorted_streets
        except arcpy.ExecuteError:  # pylint:disable = no-member
            msgs = arcpy.GetMessages(2)
            if "000824" in msgs:  # ERROR 000824: The tool is not licensed.
                arcpy.AddMessage("Skipping spatial sorting because the Advanced license is not available.")
            else:
                arcpy.AddWarning(f"Skipping spatial sorting because the Sort tool failed. Messages:\n{msgs}")
            return streets_to_sort

    @timed_exec
    def _detect_and_delete_duplicate_streets(self, id_field_name):
        """Determine if there are duplicate street IDs, and if so, delete them."""
        assert self.streets_oid_field is not None
        # Duplicate street features occur along tile boundaries.
        # Use Pandas to identify duplicate ID values and associated OIDs to delete.
        with arcpy.da.SearchCursor(self.streets, ["OID@", id_field_name]) as cur:
            id_df = pd.DataFrame(cur, columns=["OID", id_field_name])
        duplicate_streets = id_df[id_df.duplicated(subset=id_field_name)]["OID"].to_list()
        del id_df
        # If there are any duplicates, delete them.
        if duplicate_streets:
            duplicate_streets = [str(oid) for oid in duplicate_streets]
            arcpy.AddMessage("Duplicate streets were detected and will be removed.")
            where = f"{self.streets_oid_field} IN ({', '.join(duplicate_streets)})"
            layer_name = "Temp_Streets"
            with arcpy.EnvManager(overwriteOutput=True):
                arcpy.management.MakeFeatureLayer(self.streets, layer_name, where)
            arcpy.management.DeleteRows(layer_name)

    @timed_exec
    def _handle_time_zone(self):
        """Handle the time zone table."""
        if self.time_zone_type == TimeZoneType.NoTimeZone:
            # Do nothing. No time zones will be used for the network.
            return

        if self.time_zone_type == TimeZoneType.Single:
            # Create a new time zone table with a single row whose value is the specified name
            self._add_message("Creating TimeZones table...")
            assert self.time_zone_name
            arcpy.management.CreateTable(os.path.dirname(self.time_zone_table), os.path.basename(self.time_zone_table))
            arcpy.management.AddField(self.time_zone_table, "MSTIMEZONE", "TEXT", field_length=len(self.time_zone_name))
            with arcpy.da.InsertCursor(self.time_zone_table, ["MSTIMEZONE"]) as cur:
                cur.insertRow((self.time_zone_name,))
            return

        if self.time_zone_type == TimeZoneType.Table:
            # Copy the user's time zone table to the network's gdb
            self._add_message("Copying TimeZones table...")
            assert self.in_time_zone_table
            assert self.time_zone_ft_field
            assert self.time_zone_tf_field
            arcpy.conversion.TableToTable(
                self.in_time_zone_table, os.path.dirname(self.time_zone_table), os.path.basename(self.time_zone_table))

    @timed_exec
    def _create_profiles_table(self):
        """Create the DailyProfiles/Patterns historical traffic table."""
        # Schema is a little different depending on data product
        if self.data_product is DataProductType.TomTomMultinet:
            id_field_name = "ProfileID"
            time_slice = 5
        elif self.data_product is DataProductType.HereNavStreetsShp:
            id_field_name = "PatternID"
            time_slice = 15

        table_name = os.path.basename(self.profiles)
        self._add_message(f"Creating the {table_name} table...")

        # Create the table with correct schema
        arcpy.management.CreateTable(os.path.dirname(self.profiles), table_name)
        field_defs = [[id_field_name, "SHORT"]]
        if self.data_product is DataProductType.HereNavStreetsShp:
            field_defs += [["BaseSpeed", "SHORT"], ["AverageSpeed", "FLOAT"]]
        added_minutes = 0
        midnight = datetime.datetime(2021, 1, 1, 0, 0, 0)  # Initialize midnight on an arbitrary date
        # Add a field for each time slice increment until midnight
        while added_minutes < 1440:
            current_time = midnight + datetime.timedelta(minutes=added_minutes)
            field_defs.append([f"SpeedFactor_{current_time.strftime('%H%M')}", "FLOAT"])
            added_minutes += time_slice
        arcpy.management.AddFields(self.profiles, field_defs)

    @timed_exec
    def _create_streets_tmc_table(self):
        """Create the Streets_TMC table with the correct schema and return the field names."""
        arcpy.management.CreateTable(os.path.dirname(self.streets_tmc), os.path.basename(self.streets_tmc))
        field_defs = [
            [self.streets_id_field_name, self.id_field_type],  # ID or LINK_ID
            ["TMC", "TEXT", "TMC", 9],
            ["EdgeFCID", "LONG"],
            ["EdgeFID", "LONG"],
            ["EdgeFrmPos", "DOUBLE"],
            ["EdgeToPos", "DOUBLE"]
        ]
        arcpy.management.AddFields(self.streets_tmc, field_defs)
        return [f[0] for f in field_defs]

    def _create_string_field_map(self, field_name, field_type, field_length=1):
        """Return a string, starting with a semicolon, that can be appending to a field mapping string.

        This method is used when copying data with FeatureClassToFeatureClass or TableToTable in order to add new
        fields to the output that are not in the original data. Adding them this way is drastically faster than calling
        AddFields after the fact on a table that already has data in it. The reason we're using strings instead of
        FieldMap objects is that FieldMap objects get unhappy when there is no input field from the original table.
        However, constructing a string-based field map representation works absolutely fine.
        """
        return f';{field_name} "{field_name}" true true false {field_length} {field_type} 0 0,First,#'

    @staticmethod
    def _polyline_to_points(polyline):
        """Return an ordered list of point objects from the polyline geometry."""
        array = arcpy.Array()
        parts = polyline.getPart()
        for part in parts:
            array.extend(part)
        points = []
        for _ in range(array.count):
            points.append(array.next())
        return points

    def _reverse_polyline(self, polyline):
        """Reverse a polyline."""
        vertices = self._polyline_to_points(polyline)
        vertices.reverse()
        return vertices
        # vertex_array = arcpy.Array(vertices)
        # return vertex_array
        # return arcpy.Polyline(vertex_array, self.in_data_object.sr)

    @timed_exec
    def _create_turn_fc(self, restriction_field_names, addl_turn_field_defs=None):
        """Create the turn feature class and add necessary fields."""
        assert self.max_turn_edges is not None
        self._add_message("Creating turn feature class...")
        arcpy.na.CreateTurnFeatureClass(self.feature_dataset, os.path.basename(self.turns), self.max_turn_edges)
        # Add additional fields
        # The ID field is added to easily relate this back to the original data but is not required by the network.
        if self.data_product is DataProductType.TomTomMultinet:
            field_defs = [["ID", self.id_field_type]]
        elif self.data_product is DataProductType.HereNavStreetsShp:
            # The COND_TYPE field is used in various network attributes
            field_defs = [["COND_ID", self.id_field_type], ["COND_TYPE", "LONG"]]
        else:
            raise NotImplementedError(f"DataProductType not supported yet. {self.data_product}")
        # Add restriction fields
        field_defs += [[field, "TEXT", "", 1] for field in restriction_field_names]
        if addl_turn_field_defs:
            field_defs += addl_turn_field_defs
        arcpy.management.AddFields(self.turns, field_defs)

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
        turn_vertices = self._polyline_to_points(first_edge)
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

            edge_vertices = self._polyline_to_points(edge)
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
        return arcpy.Polyline(turn_vertex_array, self.in_data_object.sr)

    @timed_exec
    def _create_road_forks_table(self):
        """Create the road forks table Streets_RoadSplits with the correct schema and return a list of field names."""
        arcpy.management.CreateTable(os.path.dirname(self.road_splits), os.path.basename(self.road_splits))
        # Schema for the road forks table:
        # https://pro.arcgis.com/en/pro-app/latest/tool-reference/data-management/add-fields.htm
        field_defs = []
        if self.data_product is DataProductType.TomTomMultinet:
            # The ID field is added to easily relate this back to the original data but is not required by the schema.
            field_defs.append(["ID", self.id_field_type])
        field_defs += [
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
        return [f[0] for f in field_defs]

    @timed_exec
    def _create_signposts_fc(self):
        """Create the Signposts feature class with correct schema."""
        self._add_message("Creating Signposts feature class...")
        arcpy.management.CreateFeatureclass(
            os.path.dirname(self.signposts), os.path.basename(self.signposts),
            "POLYLINE", has_m="DISABLED", has_z="DISABLED"
        )
        # Schema for the signposts feature class:
        # https://pro.arcgis.com/en/pro-app/latest/help/analysis/networks/signposts.htm
        field_defs = [
            ["ExitName", "TEXT", "ExitName", 24],
        ]
        for i in range(self.max_signpost_branches):
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
        self._add_message("Creating Signposts_Streets table...")
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

    def _add_attribute_indices(self):
        """Add attribute indices on the outputs."""
        self._add_message("Adding attribute indices...")
        # Road forks
        arcpy.management.AddIndex(self.road_splits, ["EdgeFCID"], "EdgeFCIDIdx")
        arcpy.management.AddIndex(self.road_splits, ["EdgeFID"], "EdgeFIDIdx")
        # Signpost_Streets
        arcpy.management.AddIndex(self.signposts_streets, ["SignpostID"], "SignpostIDIdx")
        arcpy.management.AddIndex(self.signposts_streets, ["Sequence"], "SequenceIdx")
        arcpy.management.AddIndex(self.signposts_streets, ["EdgeFCID"], "EdgeFCIDIdx")
        arcpy.management.AddIndex(self.signposts_streets, ["EdgeFID"], "EdgeFIDIdx")

    def _update_nd_template_with_time_zone(self, in_template, out_template):
        """Update the network dataset template dynamically to include the time zone attribute and its evaluators."""
        if self.time_zone_type == TimeZoneType.NoTimeZone:
            return in_template

        # Read the template xml
        tree = etree.parse(in_template)

        # Inject the time zone attribute definition
        # Get the max network attribute ID so we can increment it by 1 for the new time zone attribute
        nd_attr_max_id = max([
            int(attr.text) for attr in tree.xpath(
                "/DENetworkDataset/EvaluatedNetworkAttributes/EvaluatedNetworkAttribute/ID"
            )
        ])
        # Read the time zone attribute XML from a special template that has only this attribute in it
        time_zone_attribute_template = os.path.join(
            CURDIR, "NDTemplates", "TimeZone_Attribute.xml")
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
                CURDIR, "NDTemplates", "TimeZone_Evaluators_Constant.xml")
            tz_evaluator_tree = etree.parse(time_zone_evaluator_template)
        else:  # TimeZoneType.Table
            time_zone_evaluator_template = os.path.join(
                CURDIR, "NDTemplates", "TimeZone_Evaluators_Fields.xml")
            # Crack open the file as text to replace field name placeholders with the user's fields
            with open(time_zone_evaluator_template, "r", encoding="utf-8") as f:
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
        with open(out_template, 'wb') as f:
            tree.write(f, encoding="utf-8", xml_declaration=False, pretty_print=True)
        return out_template

    def _update_nd_template_with_live_traffic(self, in_template, out_template):
        """Update the network dataset template dynamically to include the TMC table and field name for live traffic."""
        # Inject TMC table and field name into the template xml
        # <DynamicTrafficTableName>Streets_TMC</DynamicTrafficTableName>
        # <DynamicTrafficTMCFieldName>TMC</DynamicTrafficTMCFieldName>
        tree = etree.parse(in_template)
        table_name = tree.xpath("/DENetworkDataset/TrafficData/DynamicTrafficTableName")
        table_name[0].text = "Streets_TMC"
        field_name = tree.xpath("/DENetworkDataset/TrafficData/DynamicTrafficTMCFieldName")
        field_name[0].text = "TMC"
        with open(out_template, 'wb') as f:
            tree.write(f, encoding="utf-8", xml_declaration=False, pretty_print=True)
        return out_template

    def _create_nd_from_template_and_build(self, template):
        """Create the network dataset from the provided template and optionally build it."""
        # Create the network from the template
        arcpy.nax.CreateNetworkDatasetFromTemplate(template, self.feature_dataset)

        # Build the network
        if self.build_network:
            self._add_message("Building network dataset...")
            arcpy.nax.BuildNetwork(self.network)
            warnings = arcpy.GetMessages(1).splitlines()
            for warning in warnings:
                arcpy.AddWarning(warning)
        else:
            arcpy.AddMessage((
                "Skipping building the network dataset. You must run the Build Network tool on the network dataset "
                "before it can be used for analysis."
            ))

    def _delete_intermediate_outputs(self):
        """Delete intermediate output data."""
        if self.intermediate_outputs:
            self._add_message("Deleting intermediate outputs...")
            try:
                arcpy.management.Delete(self.intermediate_outputs)
            except arcpy.ExecuteError:
                msgs = arcpy.GetMessages(2)
                arcpy.AddMessage(f"Failed to delete intermediate outputs. Messages: {msgs}")

    @staticmethod
    def _add_message(msg):
        """Add a GP message and update the progressor."""
        arcpy.AddMessage(msg)
        arcpy.SetProgressorLabel(msg)
