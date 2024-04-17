using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SioForgeCAD.Commun
{
    static class HightLighter
    {
        private static readonly List<ObjectId> HightLightedObject = new List<ObjectId>();
        public static void RegisterHighlight(this Autodesk.AutoCAD.DatabaseServices.ObjectId ObjectId)
        {
            if (!HightLightedObject.Contains(ObjectId))
            {
                HightLightedObject.Add(ObjectId);
            }
            ObjectId.GetEntity().Highlight();
        }
        public static void RegisterHighlight(this Autodesk.AutoCAD.DatabaseServices.Entity Entity)
        {
            RegisterHighlight(Entity.ObjectId);
        }

        public static void RegisterUnhighlight(this Autodesk.AutoCAD.DatabaseServices.Entity Entity)
        {
            RegisterUnhighlight(Entity.ObjectId);
        }
        public static void RegisterUnhighlight(this Autodesk.AutoCAD.DatabaseServices.ObjectId ObjectId)
        {
            try
            {
                HightLightedObject.Remove(ObjectId);
                ObjectId.GetEntity().Unhighlight();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
        public static void UnhighlightAll()
        {
            foreach (ObjectId objectId in HightLightedObject.ToArray())
            {
                RegisterUnhighlight(objectId);
            }
        }
        public static void UnhighlightAll(IEnumerable<ObjectId> HightLightedObject)
        {
            foreach (ObjectId objectId in HightLightedObject)
            {
                RegisterUnhighlight(objectId);
            }
        }
    }
}
