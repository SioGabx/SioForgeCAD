﻿using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;

namespace SioForgeCAD.Commun.Extensions
{
    static class BlockReferenceExtensions
    {
        public static bool IsXref(this BlockReference blockRef)
        {
            return (blockRef.BlockTableRecord.GetDBObject() as BlockTableRecord).IsFromExternalReference;
        }


        public static string GetBlockReferenceName(this BlockReference blockRef)
        {
            if (blockRef.IsDynamicBlock)
            {
                // If it's a dynamic block, get the true name from the DynamicBlockTableRecord
                using (DBObject openResult = blockRef.DynamicBlockTableRecord.GetObject(OpenMode.ForRead))
                {
                    if (openResult is BlockTableRecord btr)
                    {
                        return btr.Name;
                    }
                }
            }
            return blockRef.Name;
        }


        public static DynamicBlockReferencePropertyCollection GetDynamicProperties(this BlockReference blockReference)
        {
            return blockReference.DynamicBlockReferencePropertyCollection;
        }

        public static void SetDynamicBlockReferenceProperty(this BlockReference blockReference, string propertyName, object value)
        {
            DynamicBlockReferencePropertyCollection propertyCollection = GetDynamicProperties(blockReference);

            if (propertyCollection != null)
            {
                foreach (DynamicBlockReferenceProperty prop in propertyCollection)
                {
                    if (prop.PropertyName.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!prop.ReadOnly)
                        {
                            prop.Value = value;
                            return;
                        }
                    }
                }
            }
        }

        public static Point3d ProjectPointToCurrentSpace(ObjectId xrefId, Point3d pointInXref)
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

        public static bool DoesBlockExist(Point3d position, string blockName, string attributeValue)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            using (Transaction transaction = db.TransactionManager.StartTransaction())
            {
                BlockTable blockTable = transaction.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord modelSpace = transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;

                foreach (ObjectId objId in modelSpace)
                {
                    if (objId.ObjectClass.DxfName == "INSERT")
                    {
                        BlockReference blockReference = transaction.GetObject(objId, OpenMode.ForRead) as BlockReference;

                        if (blockReference != null && blockReference.Name == blockName && blockReference.Position.IsEqualTo(position, Tolerance.Global))
                        {
                            // Check attribute values
                            foreach (ObjectId attId in blockReference.AttributeCollection)
                            {
                                DBObject obj = transaction.GetObject(attId, OpenMode.ForRead) as DBObject;
                                if (obj is AttributeReference attributeReference)
                                {
                                    if (attributeReference.TextString == attributeValue)
                                    {
                                        // The block with the same position and attribute values exists
                                        transaction.Commit();
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }

                // The block does not exist at the same position with the same attribute values
                transaction.Commit();
                return false;
            }
        }

    }
}
