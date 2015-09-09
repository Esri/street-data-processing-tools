using System;
using System.Runtime.InteropServices;
using System.Collections;

using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geoprocessing;
using ESRI.ArcGIS.Geometry;

namespace GPProcessVendorDataFunctions
{
    /// <summary>
    /// SignpostUtilities class has functions for creating and populating the signpost
    /// schema.
    /// </summary>
    public class SignpostUtilities
    {
        public static readonly int MaxBranchCount = 10;

        public struct FeatureData
        {
            public FeatureData(int ID, IGeometry geom)
            {
                OID = ID;
                feature = geom;
            }
            public int OID;
            public IGeometry feature;
        }

        public SignpostUtilities()
        {
        }

        public static IFeatureClass CreateSignsFeatureClass(IFeatureClass linesFeatureClass, string name)
        {
            // Locations are all relative to the location of the reference lines.
            // For geodatabase, signs feature class is at the same location and the streets table
            // is at the level of the containing feature dataset.
            // For shapefile, both are at the same location as the reference lines.

            // start with the initial set of required fields for a feature class

            IFeatureClassDescription fcDescription = new FeatureClassDescriptionClass();
            IObjectClassDescription ocDescription = fcDescription as IObjectClassDescription;
            IFieldsEdit outFields = ocDescription.RequiredFields as IFieldsEdit;

            // make the shape field to be of type polyline with the same spatial reference as the reference lines

            IField shapeField = outFields.get_Field(outFields.FindField(fcDescription.ShapeFieldName));
            IGeometryDefEdit geomDefEdit = shapeField.GeometryDef as IGeometryDefEdit;
            geomDefEdit.GeometryType_2 = esriGeometryType.esriGeometryPolyline;
            geomDefEdit.SpatialReference_2 = (linesFeatureClass as IGeoDataset).SpatialReference;

            // add the other fields to the feature class

            IFieldEdit field = new FieldClass();
            field.Name_2 = "ExitName";
            field.Type_2 = esriFieldType.esriFieldTypeString;
            field.Length_2 = 24;
            outFields.AddField(field);

            string currentNumber;

            for (int i = 0; i < MaxBranchCount; i++)
            {
                currentNumber = Convert.ToString(i, System.Globalization.CultureInfo.InvariantCulture);

                field = new FieldClass();
                field.Name_2 = "Branch" + currentNumber;
                field.Type_2 = esriFieldType.esriFieldTypeString;
                field.Length_2 = 180;
                outFields.AddField(field);

                field = new FieldClass();
                field.Name_2 = "Branch" + currentNumber + "Dir";
                field.Type_2 = esriFieldType.esriFieldTypeString;
                field.Length_2 = 5;
                outFields.AddField(field);

                field = new FieldClass();
                field.Name_2 = "Branch" + currentNumber + "Lng";
                field.Type_2 = esriFieldType.esriFieldTypeString;
                field.Length_2 = 2;
                outFields.AddField(field);

                field = new FieldClass();
                field.Name_2 = "Toward" + currentNumber;
                field.Type_2 = esriFieldType.esriFieldTypeString;
                field.Length_2 = 180;
                outFields.AddField(field);

                field = new FieldClass();
                field.Name_2 = "Toward" + currentNumber + "Lng";
                field.Type_2 = esriFieldType.esriFieldTypeString;
                field.Length_2 = 2;
                outFields.AddField(field);
            }

            // make the feature class

            IFeatureDataset pFeatureDataset = linesFeatureClass.FeatureDataset;
            IWorkspace pWorkspace = (linesFeatureClass as IDataset).Workspace;

            if (pFeatureDataset != null)
                return pFeatureDataset.CreateFeatureClass(name, outFields, ocDescription.InstanceCLSID, 
                    ocDescription.ClassExtensionCLSID, esriFeatureType.esriFTSimple, fcDescription.ShapeFieldName, "");
            else if (pWorkspace is IFeatureWorkspace)
                return (pWorkspace as IFeatureWorkspace).CreateFeatureClass(name, outFields, ocDescription.InstanceCLSID,
                    ocDescription.ClassExtensionCLSID, esriFeatureType.esriFTSimple, fcDescription.ShapeFieldName, "");
            else
                return null;   // not expected
        }

        public static ITable CreateSignsDetailTable(IFeatureClass linesFeatureClass, string name)
        {
            // Locations are all relative to the location of the reference lines.
            // For Geodatabase, signs feature class is at the same location and the streets table
            // is at the level of the containing feature dataset.
            // For shapefile, both are at the same location as the reference lines.

            // start with the initial set of required fields for a table

            IObjectClassDescription ocDescription = new ObjectClassDescriptionClass();
            IFieldsEdit outFields = ocDescription.RequiredFields as IFieldsEdit;

            // add the SignpostID field to the table

            IFieldEdit field = new FieldClass();
            field.Name_2 = "SignpostID";
            field.Type_2 = esriFieldType.esriFieldTypeInteger;
            outFields.AddField(field);

            // add the other fields to the table

            field = new FieldClass();
            field.Name_2 = "Sequence";
            field.Type_2 = esriFieldType.esriFieldTypeInteger;
            outFields.AddField(field);

            field = new FieldClass();
            field.Name_2 = "EdgeFCID";
            field.Type_2 = esriFieldType.esriFieldTypeInteger;
            outFields.AddField(field);

            field = new FieldClass();
            field.Name_2 = "EdgeFID";
            field.Type_2 = esriFieldType.esriFieldTypeInteger;
            outFields.AddField(field);

            field = new FieldClass();
            field.Name_2 = "EdgeFrmPos";
            field.Type_2 = esriFieldType.esriFieldTypeDouble;
            outFields.AddField(field);

            field = new FieldClass();
            field.Name_2 = "EdgeToPos";
            field.Type_2 = esriFieldType.esriFieldTypeDouble;
            outFields.AddField(field);

            // make the table

            IFeatureDataset featureDataset = linesFeatureClass.FeatureDataset;
            IWorkspace workspace = (linesFeatureClass as IDataset).Workspace;

            if (featureDataset != null)
            {
                // up a level
                IFeatureWorkspace createWS = featureDataset.Workspace as IFeatureWorkspace;
                return createWS.CreateTable(name, outFields, ocDescription.InstanceCLSID, null, "");
            }
            else if (workspace is IFeatureWorkspace)
                return (workspace as IFeatureWorkspace).CreateTable(name, outFields, ocDescription.InstanceCLSID, null, "");
            else
                return null;   // not expected        
        }

        public static Hashtable FillFeatureCache(ITable inputSignsTable, int inFromIDFI, int inToIDFI,
                                                 IFeatureClass inputLineFeatures, string linesIDFieldName,
                                                 ITrackCancel trackcancel)
        {
            // make and fill a SortedList from the IDs referenced in the table

            // for MultiNet data, there is only one ID field, so its index will be 
            // passed in as inFromIDFI, while -1 will be passed in to inToIDFI.

            SortedList IDs = new System.Collections.SortedList();

            ICursor inCursor = inputSignsTable.Search(null, true);
            IRow row;

            long fromID, toID;
            bool exists;
            int cancelCheckInterval = 100;

            if (inToIDFI == -1)
            {
                while ((row = inCursor.NextRow()) != null)
                {
                    fromID = Convert.ToInt64(row.get_Value(inFromIDFI));

                    exists = IDs.Contains(fromID);
                    if (!exists)
                        IDs.Add(fromID, fromID);
                }
            }
            else
            {
                while ((row = inCursor.NextRow()) != null)
                {
                    fromID = Convert.ToInt64(row.get_Value(inFromIDFI));
                    toID = Convert.ToInt64(row.get_Value(inToIDFI));

                    exists = IDs.Contains(fromID);
                    if (!exists)
                        IDs.Add(fromID, fromID);

                    exists = IDs.Contains(toID);
                    if (!exists)
                        IDs.Add(toID, toID);
                }
            }

            // make the query filter for fetching features

            IQueryFilter queryFilter = new QueryFilterClass();
            queryFilter.SubFields = "*";

            // Now fetch batches of features

            long currID;
            int numFeaturesPerQuery = 200;
            int numToFetch = IDs.Count;
            int totalRemaining, totalDone = 0;

            int linesIDFieldIndex = inputLineFeatures.FindField(linesIDFieldName);

            Hashtable outputFeatures = new System.Collections.Hashtable((int)numToFetch);

            if (numFeaturesPerQuery > numToFetch)
                numFeaturesPerQuery = numToFetch;

            while (totalDone < numToFetch)
            {
                // Populate the QueryDef Where clause IN() statement for the current batch of features.
                // This is going to be very slow unless linesIDFieldName is indexed and this is why
                // we added a warning to the GP message window if this is the case.  If you cannot
                // index linesIDFieldName, then this code would run faster scanning the whole feature
                // class looking for the records we need (no Where clause).

                string whereClause = linesIDFieldName + " IN(";

                for (int i = 0; i < numFeaturesPerQuery; i++)
                {
                    currID = Convert.ToInt64(IDs.GetByIndex(totalDone + i), System.Globalization.CultureInfo.InvariantCulture);
                    whereClause += Convert.ToString(currID, System.Globalization.CultureInfo.InvariantCulture);
                    if (i != (numFeaturesPerQuery - 1))
                        whereClause += ",";
                    else
                        whereClause += ")";
                }

                queryFilter.WhereClause = whereClause;

                // select the features

                IFeatureCursor inputFeatureCursor = inputLineFeatures.Search(queryFilter, false);

                // get the features

                IFeature feature;

                while ((feature = inputFeatureCursor.NextFeature()) != null)
                {
                    // keep a copy of the OID and shape of feature - skip records that cause errors
                    // (perhaps pass the GPMessages in and log warnings in there if you need to log exceptions)

                    try
                    {
                        FeatureData data = new FeatureData(feature.OID, feature.ShapeCopy);
                        outputFeatures.Add(Convert.ToInt64(feature.get_Value(linesIDFieldIndex)), data);
                    }
                    catch
                    {
                    }

                    if ((totalDone % cancelCheckInterval) == 0)
                    {
                        // check for user cancel

                        if (trackcancel != null && !trackcancel.Continue())
                            throw (new COMException("Function cancelled."));
                    }
                }

                // finished? set up for next batch

                totalDone += numFeaturesPerQuery;

                totalRemaining = numToFetch - totalDone;
                if (totalRemaining > 0)
                {
                    if (numFeaturesPerQuery > totalRemaining)
                        numFeaturesPerQuery = totalRemaining;
                }
            }

            return outputFeatures;
        }

        public static string GetLanguageValue(string langCode, Hashtable langLookup)
        {
            if (langLookup.ContainsKey(langCode))
                return (string)(langLookup[langCode]);
            else
                return "";
        }

        public static void CleanUpSignpostFeatureValues(IFeatureBuffer featureBuffer, int lastValidBranchNum, int lastValidTowardNum,
                                                        int[] outBranchXFI, int[] outBranchXDirFI, int[] outBranchXLngFI,
                                                        int[] outTowardXFI, int[] outTowardXLngFI)
        {
            // set unused sequence number values to null (our row buffer may still
            // have junk at the end)

            for (int i = lastValidBranchNum + 1; i < SignpostUtilities.MaxBranchCount; i++)
            {
                featureBuffer.set_Value(outBranchXFI[i], null);
                featureBuffer.set_Value(outBranchXDirFI[i], null);
                featureBuffer.set_Value(outBranchXLngFI[i], null);
            }

            for (int i = lastValidTowardNum + 1; i < SignpostUtilities.MaxBranchCount; i++)
            {
                featureBuffer.set_Value(outTowardXFI[i], null);
                featureBuffer.set_Value(outTowardXLngFI[i], null);
            }
        }
    }
}
