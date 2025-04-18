using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace SioForgeCAD.Commun.Extensions
{
    public static class DBTextExtensions
    {
        //From https://www.keanw.com/2011/02/gathering-points-defining-2d-autocad-geometry-using-net.html
        public static void ExtractBounds(this DBText txt, Point3dCollection pts)
        {
            // We have a special approach for DBText and
            // AttributeReference objects, as we want to get
            // all four corners of the bounding box, even
            // when the text or the containing block reference
            // is rotated

            if (txt.Bounds.HasValue && txt.Visible)
            {
                // Create a straight version of the text object
                // and copy across all the relevant properties
                // (stopped copying AlignmentPoint, as it would
                // sometimes cause an eNotApplicable error)
                // We'll create the text at the WCS origin
                // with no rotation, so it's easier to use its extents
                DBText txt2 = new DBText
                {
                    Normal = Vector3d.ZAxis,
                    Position = Point3d.Origin,
                    TextString = txt.TextString,
                    TextStyleId = txt.TextStyleId,
                    LineWeight = txt.LineWeight,
                    Thickness = txt.Thickness,
                    HorizontalMode = txt.HorizontalMode,
                    VerticalMode = txt.VerticalMode,
                    WidthFactor = txt.WidthFactor,
                    Height = txt.Height,
                    IsMirroredInX = txt.IsMirroredInX,
                    IsMirroredInY = txt.IsMirroredInY,
                    Oblique = txt.Oblique,
                };

                // Get its bounds if it has them defined
                // (which it should, as the original did)
                if (txt2.Bounds.HasValue)
                {
                    Point3d maxPt = txt2.Bounds.Value.MaxPoint;
                    // Place all four corners of the bounding box
                    // in an array
                    Point2d[] bounds = new Point2d[] { Point2d.Origin, new Point2d(0.0, maxPt.Y), new Point2d(maxPt.X, maxPt.Y), new Point2d(maxPt.X, 0.0) };

                    // We're going to get each point's WCS coordinates
                    // using the plane the text is on
                    Plane pl = new Plane(txt.Position, txt.Normal);

                    // Rotate each point and add its WCS location to the collection

                    foreach (Point2d pt in bounds)
                    {
                        pts.Add(pl.EvaluatePoint(pt.RotateBy(txt.Rotation, Point2d.Origin)));
                    }
                }
            }
        }
    }
}
