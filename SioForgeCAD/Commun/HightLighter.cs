using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Commun
{
    static class HightLighter
    {
        private static List<Autodesk.AutoCAD.DatabaseServices.ObjectId> HightLightedObject = new List<Autodesk.AutoCAD.DatabaseServices.ObjectId>();
        public static void RegisterHighlight(this Autodesk.AutoCAD.DatabaseServices.ObjectId ObjectId)
        {
            HightLightedObject.Add(ObjectId);
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
            HightLightedObject.Remove(ObjectId);
            ObjectId.GetEntity().Unhighlight();
        }
        public static void UnhighlightAll()
        {
            foreach(ObjectId objectId in HightLightedObject.ToArray())
            {
                RegisterUnhighlight(objectId);
            }
        }
    }
}
