using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun.Drawing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SioForgeCAD.Commun.Extensions
{
    static class BlockReferenceExtensions
    {
        public static bool IsXref(this BlockReference blockRef)
        {
            return (blockRef?.BlockTableRecord.GetDBObject() as BlockTableRecord)?.IsFromExternalReference ?? false;
        }

        public static BlockTableRecord GetBlocDefinition(this Database db, string BlocName)
        {
            BlockTable bt = db.BlockTableId.GetDBObject() as BlockTable;
            if (!bt.Has(BlocName))
            {
                throw new Exception($"Le bloc {BlocName} n'existe pas dans le dessin");
            }
            return bt[BlocName].GetObject(OpenMode.ForRead) as BlockTableRecord;
        }

        public static string GetBlockReferenceName(this BlockReference blockRef)
        {
            if (blockRef.IsDynamicBlock)
            {
                // If it's a dynamic block, get the true name from the DynamicBlockTableRecord
                using (BlockTableRecord btr = blockRef.DynamicBlockTableRecord.GetDBObject(OpenMode.ForRead) as BlockTableRecord)
                {
                    return btr.Name;
                }
            }
            return blockRef.Name;
        }

        public static string GetDescription(this BlockReference blkRef)
        {
            BlockTableRecord blockDef = blkRef.BlockTableRecord.GetDBObject(OpenMode.ForRead) as BlockTableRecord;
            return blockDef.Comments?.ToString();
        }

        public static List<DynamicBlockReferenceProperty> GetDynamicProperties(this BlockReference blockReference)
        {
            List<DynamicBlockReferenceProperty> Values = new List<DynamicBlockReferenceProperty>();
            DynamicBlockReferencePropertyCollection propertyCollection = blockReference.DynamicBlockReferencePropertyCollection;

            if (propertyCollection != null)
            {
                foreach (DynamicBlockReferenceProperty prop in propertyCollection)
                {
                    Values.Add(prop);
                }
            }
            return Values;
        }

        public static void SetDynamicBlockReferenceProperty(this BlockReference blockReference, string propertyName, object value)
        {
            foreach (DynamicBlockReferenceProperty prop in GetDynamicProperties(blockReference))
            {
                if (!prop.ReadOnly && prop.PropertyName.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    prop.Value = value;
                    return;
                }
            }
        }

        public static IEnumerable<KeyValuePair<string, AttributeReference>> GetAttributesByTag(this BlockReference source)
        {
            foreach (var att in source.AttributeCollection.GetObjects())
            {
                yield return new KeyValuePair<string, AttributeReference>(att.Tag, att);
            }
        }

        /// <summary>
        /// Gets all the attribute values by tag.
        /// </summary>
        /// <param name="source">Instance to which the method applies.</param>
        /// <returns>Collection of pairs Tag/Value.</returns>
        public static Dictionary<string, string> GetAttributesValues(this BlockReference source)
        {
            return source.GetAttributesByTag().ToDictionary(p => p.Key, p => p.Value.TextString);
        }

        /// <summary>
        /// Sets the value to the attribute.
        /// </summary>
        /// <param name="target">Instance to which the method applies.</param>
        /// <param name="tag">Attribute tag.</param>
        /// <param name="value">New value.</param>
        /// <returns>The value if attribute was found, null otherwise.</returns>
        public static string SetAttributeValue(this BlockReference target, string tag, string value)
        {
            foreach (AttributeReference attRef in target.AttributeCollection.GetObjects())
            {
                if (attRef.Tag == tag)
                {
                    attRef.TextString = value;
                    return value;
                }
            }
            return null;
        }

        /// <summary>
        /// Sets the values to the attributes.
        /// </summary>
        /// <param name="target">Instance to which the method applies.</param>
        /// <param name="attribs">Collection of pairs Tag/Value.</param>
        public static void SetAttributeValues(this BlockReference target, Dictionary<string, string> attribs)
        {
            Transaction tr = Generic.GetDatabase().TransactionManager.TopTransaction;
            foreach (AttributeReference attRef in target.AttributeCollection.GetObjects())
            {
                if (attribs.TryGetValue(attRef.Tag, out string value))
                {
                    tr.GetObject(attRef.ObjectId, OpenMode.ForWrite);
                    attRef.TextString = value;
                }
            }
        }

        public static Point3d ProjectXrefPointToCurrentSpace(this Point3d pointInXref, ObjectId xrefId)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            using (Transaction transaction = db.TransactionManager.StartTransaction())
            {
                // Ouvrez la référence externe (Xref) en mode de lecture
                BlockReference xrefBlockReference = transaction.GetObject(xrefId, OpenMode.ForRead) as BlockReference;

                if (xrefBlockReference != null)
                {
                    // Obtenez la matrice de transformation complète de la référence externe (Xref)
                    Matrix3d xrefTransform = xrefBlockReference.BlockTransform;

                    // Transformez le point dans la référence externe vers l'espace monde
                    Point3d worldPoint = pointInXref.TransformBy(xrefTransform);
                    transaction.Commit();

                    return worldPoint;
                }
            }

            return Point3d.Origin;
        }

        public static bool IsThereABlockReference(this Point3d position, string blockName, string attributeValue, out BlockReference blockReference)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord modelSpace = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                foreach (ObjectId objId in modelSpace)
                {
                    if (objId.ObjectClass.DxfName == "INSERT")
                    {
                        blockReference = tr.GetObject(objId, OpenMode.ForRead) as BlockReference;

                        if (blockReference != null && blockReference.Name == blockName && blockReference.Position.IsEqualTo(position, Tolerance.Global))
                        {
                            // Check attribute values
                            foreach (ObjectId attId in blockReference.AttributeCollection)
                            {
                                DBObject obj = tr.GetObject(attId, OpenMode.ForRead) as DBObject;
                                if (obj is AttributeReference attributeReference)
                                {
                                    if (attributeReference.TextString == attributeValue)
                                    {
                                        // The block with the same position and attribute values exists
                                        tr.Commit();
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }

                // The block does not exist at the same position with the same attribute values
                tr.Commit();
                blockReference = null;
                return false;
            }
        }


        public static void RegenAllBlkDefinition(this BlockReference BlockRef)
        {
            Database db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                ObjectIdCollection iter = BlockReferences.GetDynamicBlockReferences(BlockRef.GetBlockReferenceName());
                BlockTableRecord BlockDef = BlockRef.BlockTableRecord.GetDBObject(OpenMode.ForWrite) as BlockTableRecord;
                iter.Join(BlockDef.GetBlockReferenceIds(true, false));

                foreach (ObjectId entId in iter)
                {
                    if (entId.GetDBObject(OpenMode.ForWrite) is BlockReference otherBlockRef)
                    {
                        otherBlockRef.RecordGraphicsModified(true);
                    }
                }
                tr.Commit();
            }
        }

    }
}
