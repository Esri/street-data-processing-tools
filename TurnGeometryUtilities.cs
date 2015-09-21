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

using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;

using ESRI.ArcGIS.Geoprocessor;
using ESRI.ArcGIS.DataManagementTools;

namespace GPProcessVendorDataFunctions
{
    /// <summary>
    /// TurnGeometryUtilities class has functions for working with turn geometries.
    /// </summary>
    public class TurnGeometryUtilities
    {
        public TurnGeometryUtilities()
        {
        }

        public static void WriteTurnGeometry(string outputFileGdbPath, string StreetsFCName, string TurnFCName,
                                             int numAltIDFields, double trimRatio, IGPMessages messages, ITrackCancel trackcancel)
        {
            messages.AddMessage("Writing turn geometries...");

            // Open the feature classes in the file geodatabase

            Type factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.FileGDBWorkspaceFactory");
            var wsf = Activator.CreateInstance(factoryType) as IWorkspaceFactory;
            var fws = wsf.OpenFromFile(outputFileGdbPath, 0) as IFeatureWorkspace;
            IFeatureClass streetsFC = fws.OpenFeatureClass(StreetsFCName);
            IFeatureClass turnFC = fws.OpenFeatureClass(TurnFCName);

            // Look up the Edge1End, EdgeFCID and EdgeFID fields on the turn feature class

            int edge1EndField = turnFC.FindField("Edge1End");
            int[] edgeFCIDFields = new int[numAltIDFields];
            int[] edgeFIDFields = new int[numAltIDFields];
            for (int i = 0; i < numAltIDFields; i++)
            {
                edgeFCIDFields[i] = turnFC.FindField("Edge" + (i + 1) + "FCID");
                edgeFIDFields[i] = turnFC.FindField("Edge" + (i + 1) + "FID");
            }

            // Look up the FCID of the Streets feature class and open a random access cursor on it
            
            int streetsFCID = streetsFC.FeatureClassID;
            IRandomAccessCursor rac = (streetsFC as IRandomAccessTable).GetRandomRows("", true);

            // Create an update cursor on the turn feature class

            var qf = new QueryFilterClass() as IQueryFilter;
            IFeatureCursor featCursor = turnFC.Update(qf, false);
            IFeature turnFeat = null;
            int numFeatures = 0;
            double lastCurveLength = 0.0;
            while ((turnFeat = featCursor.NextFeature()) != null)
            {
                // Get the geometry of the first line in the turn, rotate and trim it
                var lineFeat = rac.GetRow((int)turnFeat.get_Value(edgeFIDFields[0])) as IFeature;
                var featCurve = lineFeat.ShapeCopy as ICurve;
                ICurve workingCurve = null;
                switch ((string)turnFeat.get_Value(edge1EndField))
                {
                    case "Y":
                        featCurve.GetSubcurve(1.0 - trimRatio, 1.0, true, out workingCurve);
                        break;
                    case "N":
                        featCurve.GetSubcurve(0.0, trimRatio, true, out workingCurve);
                        workingCurve.ReverseOrientation();
                        break;
                    default:
                        messages.AddWarning("ERROR: Invalid Edge1End value!  Turn OID: " + turnFeat.OID);
                        break;
                }
                if (workingCurve == null)
                {
                    continue;
                }

                // Create a new polyline and add the trimmed first line to it
                var segColl = new PolylineClass() as ISegmentCollection;
                segColl.AddSegmentCollection(workingCurve as ISegmentCollection);

                // Remember the last point of the curve
                IPoint lastCurveEnd = workingCurve.ToPoint;

                bool earlyExit = false;
                for (int i = 1; i < numAltIDFields; i++)
                {
                    if ((int)turnFeat.get_Value(edgeFCIDFields[i]) != streetsFCID)
                    {
                        // This was the last part of the turn -- break out and finalize the geometry
                        break;
                    }

                    // Otherwise get the geometry of this line in the turn, rotate it if necessary,
                    // and add it to the segment collection
                    lineFeat = rac.GetRow((int)turnFeat.get_Value(edgeFIDFields[i])) as IFeature;
                    var poly = lineFeat.ShapeCopy as IPolycurve;
                    bool splitHappened;
                    int newPart, newSeg;
                    poly.SplitAtDistance(0.5, true, false, out splitHappened, out newPart, out newSeg);
                    featCurve = poly as ICurve;
                    IPoint myPoint = featCurve.FromPoint;
                    if (EqualPoints(myPoint, lastCurveEnd))
                    {
                        segColl.AddSegmentCollection(featCurve as ISegmentCollection);
                    }
                    else
                    {
                        myPoint = featCurve.ToPoint;
                        if (EqualPoints(myPoint, lastCurveEnd))
                        {
                            featCurve.ReverseOrientation();
                            segColl.AddSegmentCollection(featCurve as ISegmentCollection);
                        }
                        else
                        {
                            messages.AddWarning("ERROR: Edge " + (i+1) + " is discontinuous with the previous curve!  Turn OID: " + turnFeat.OID);
                            earlyExit = true;
                            break;
                        }
                    }

                    // Remember the length of the last curve added, and the last point of the curve
                    lastCurveLength = featCurve.Length;
                    lastCurveEnd = featCurve.ToPoint;
                }
                // If the edges of the turn were read in successfully...
                if (!earlyExit)
                {
                    // Trim the segment such that the last curve is the length of the trim ratio
                    workingCurve = segColl as ICurve;
                    workingCurve.GetSubcurve(0.0, workingCurve.Length - ((1.0 - trimRatio) * lastCurveLength), false, out featCurve);
                    turnFeat.Shape = featCurve as IGeometry;
                    
                    // Write out the turn geometry and increment the count
                    featCursor.UpdateFeature(turnFeat);
                    numFeatures++;

                    if ((numFeatures % 100) == 0)
                    {
                        // check for user cancel

                        if (trackcancel != null && !trackcancel.Continue())
                            throw (new COMException("Function cancelled."));
                    }
                }
            }
        }

        public static bool EqualPoints(IPoint p1, IPoint p2)
        {
            return ((p1.X == p2.X) && (p1.Y == p2.Y));
        }

        public static void WriteBuildErrorsToTurnFC(string outputFileGdbPath, string fdsName, string turnFCName,
                                                    IGPMessages messages, ITrackCancel trackcancel)
        {
            messages.AddMessage("Writing build errors to the turn feature class...");

            // Create a new field on the turn feature class for the build errors

            Geoprocessor gp = new Geoprocessor();
            gp.AddOutputsToMap = false;
            AddField addFieldTool = new AddField();
            addFieldTool.in_table = outputFileGdbPath + "\\" + fdsName + "\\" + turnFCName;
            addFieldTool.field_name = "BuildError";
            addFieldTool.field_type = "SHORT";
            gp.Execute(addFieldTool, trackcancel);

            // Open the turn feature class in the file geodatabase and find the BuildError field on it

            Type factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.FileGDBWorkspaceFactory");
            var wsf = Activator.CreateInstance(factoryType) as IWorkspaceFactory;
            var fws = wsf.OpenFromFile(outputFileGdbPath, 0) as IFeatureWorkspace;
            IFeatureClass turnFC = fws.OpenFeatureClass(turnFCName);
            int buildErrorField = turnFC.FindField("BuildError");

            // Open the BuildErrors.txt file generated from building the network dataset

            string s, leftTrimmedString, oidString;
            int leftTrimAmt = 24 + turnFCName.Length;
            IFeature feat = null;
            System.IO.StreamReader f = new System.IO.StreamReader(Environment.GetEnvironmentVariable("TEMP") + "\\BuildErrors.txt");
            
            // Loop through the BuildErrors.txt file and write the value 1 for each entry found.
            
            while ((s = f.ReadLine()) != null)
            {
                // ignore blank lines
                if (s.Length == 0)
                    continue;

                // ignore build errors not dealing with the turn source
                if (s.Remove(leftTrimAmt) != ("SourceName: " + turnFCName + ", ObjectID: "))
                    continue;

                leftTrimmedString = s.Substring(leftTrimAmt);
                oidString = leftTrimmedString.Remove(leftTrimmedString.IndexOf(", "));
                feat = turnFC.GetFeature(Convert.ToInt32(oidString, System.Globalization.CultureInfo.InvariantCulture));
                feat.set_Value(buildErrorField, 1);
                feat.Store();
            }
            f.Close();
        }
    }
}
