using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;

using Autodesk.AutoCAD.ApplicationServices.Core;

using Autodesk.AutoCAD.DatabaseServices;

using Autodesk.AutoCAD.Geometry;


namespace SioForgeCAD.Commun
{
    public class Camera

    {

        // Members



        private Document _doc = null;

        private ViewTableRecord _vtr = null;

        private ViewTableRecord _initial = null;



        public Camera(Document doc)

        {

            _doc = doc;

            _initial = doc.Editor.GetCurrentView();

            _vtr = (ViewTableRecord)_initial.Clone();

        }



        // Reset to the initial view



        public void Reset()

        {

            _doc.Editor.SetCurrentView(_initial);

            _doc.Editor.Regen();

        }



        // Zoom in or out



        public void Zoom(double factor)

        {

            // Adjust the ViewTableRecord



            _vtr.Height *= factor;

            _vtr.Width *= factor;



            // Set it as the current view



            _doc.Editor.SetCurrentView(_vtr);



            // Zoom requires a regen for the gizmos to update



            _doc.Editor.Regen();

        }



        // Pan in the specified direction



        public void Pan(double leftRight, double upDown)

        {

            // Adjust the ViewTableRecord



            _vtr.CenterPoint += new Vector2d(leftRight, upDown);



            // Set it as the current view



            _doc.Editor.SetCurrentView(_vtr);

        }



        // Orbit by angle around axis



        public void Orbit(Vector3d axis, double angle)

        {

            // Adjust the ViewTableRecord



            _vtr.ViewDirection =

              _vtr.ViewDirection.TransformBy(

                Matrix3d.Rotation(angle, axis, Point3d.Origin)

              );



            // Set it as the current view



            _doc.Editor.SetCurrentView(_vtr);

        }

    }


}
