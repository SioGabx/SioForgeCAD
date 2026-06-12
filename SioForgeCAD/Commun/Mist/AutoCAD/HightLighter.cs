using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SioForgeCAD.Commun
{
    static class HightLighter
    {
        private static readonly List<ObjectId> HightLightedObject = new List<ObjectId>();

        /// <summary>
        /// Require Transaction
        /// </summary>
        public static void RegisterHighlight(this ObjectId ObjectId)
        {
            if (!HightLightedObject.Contains(ObjectId))
            {
                HightLightedObject.Add(ObjectId);
            }
            ObjectId.GetEntity().Highlight();
        }

        /// <summary>
        /// Require Transaction
        /// </summary>
        public static void RegisterHighlight(this Entity Entity)
        {
            RegisterHighlight(Entity.ObjectId);
        }

        /// <summary>
        /// Require Transaction
        /// </summary>
        public static void RegisterUnhighlight(this Entity Entity)
        {
            RegisterUnhighlight(Entity.ObjectId);
        }

        /// <summary>
        /// Require Transaction
        /// </summary>
        public static void RegisterUnhighlight(this ObjectId ObjectId)
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

        /// <summary>
        /// Require Transaction
        /// </summary>
        public static void UnhighlightAll()
        {
            foreach (ObjectId objectId in HightLightedObject.ToArray())
            {
                RegisterUnhighlight(objectId);
            }
        }

        /// <summary>
        /// Require Transaction
        /// </summary>
        public static void UnhighlightAll(IEnumerable<ObjectId> HightLightedObject)
        {
            foreach (ObjectId objectId in HightLightedObject)
            {
                RegisterUnhighlight(objectId);
            }
        }
    }
}
