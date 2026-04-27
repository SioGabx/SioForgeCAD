using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Runtime;
using SioForgeCAD.Commun;


namespace SioForgeCAD.Functions
{
    public static class VIEWGEOMETRYVERTEX
    {
        public class VertexCircleOverrule : DrawableOverrule
        {
            private readonly double _radiusInPixels = .05;

            public override bool WorldDraw(Drawable drawable, WorldDraw wd)
            {
                // 1. On laisse AutoCAD dessiner l'entité de base (la vraie polyligne/ligne)
                base.WorldDraw(drawable, wd);

                // 2. CRUCIAL : On retourne 'false'. 
                // Cela dit à AutoCAD : "Attention, la suite de ma géométrie dépend de la vue".
                // Cela va forcer AutoCAD à appeler la méthode ViewportDraw à chaque coup de molette (zoom).
                return false;
            }

            public override void ViewportDraw(Drawable drawable, ViewportDraw vd)
            {
                // On s'assure de dessiner la géométrie de base de la vue
                base.ViewportDraw(drawable, vd);

                if (drawable is Autodesk.AutoCAD.DatabaseServices.Polyline pline)
                {
                    for (int i = 0; i < pline.NumberOfVertices; i++)
                    {
                        Point3d pt = pline.GetPoint3dAt(i);
                        DrawDynamicCircle(vd, pt, pline.Normal);
                    }
                }
                else if (drawable is Line line)
                {
                    DrawDynamicCircle(vd, line.StartPoint, Vector3d.ZAxis);
                    DrawDynamicCircle(vd, line.EndPoint, Vector3d.ZAxis);
                }
                else if (drawable is Circle circle)
                {
                    DrawDynamicCircle(vd, circle.Center, circle.Normal);
                }
            }

            // --- Méthode magique pour la taille constante ---
            private void DrawDynamicCircle(ViewportDraw vd, Point3d centerPt, Vector3d normal)
            {
                // 1. Calculer la matrice de transformation locale (Bloc) vers globale (Monde)
                // Formule mathématique : WCS = Inverse(WorldToEye) * ModelToEye
                Matrix3d mcsToWcs = vd.Viewport.WorldToEyeTransform.Inverse() * vd.Viewport.ModelToEyeTransform;

                // 2. Extraire l'échelle globale appliquée au bloc (en mesurant l'allongement de l'axe X)
                double blockScale = Vector3d.XAxis.TransformBy(mcsToWcs).Length;

                // Sécurité pour éviter les divisions par zéro
                if (blockScale <= 0) return;

                // 3. Trouver le centre du cercle dans l'espace Monde (WCS) absolu
                Point3d centerWcs = centerPt.TransformBy(mcsToWcs);

                // 4. Obtenir les pixels par unité Monde (WCS) à cette position absolue
                Point2d pixelsPerUnitWcs = vd.Viewport.GetNumPixelsInUnitSquare(centerWcs);

                if (pixelsPerUnitWcs.X <= 0) return;

                // 5. Calculer le rayon absolu en unités Monde (WCS)
                double radiusWcs = _radiusInPixels;// / pixelsPerUnitWcs.X; //pixelsPerUnitWcs = ajustement par rapport à la vue

                // 6. CRUCIAL : Convertir le rayon en unités Locales (MCS).
                // On divise par l'échelle du bloc. Ainsi, quand AutoCAD multipliera le dessin
                // par l'échelle du bloc pour l'afficher, les deux s'annuleront !
                double radiusMcs = radiusWcs / blockScale;

                // 7. Dessiner le cercle (qui s'adaptera maintenant au zoom ET à l'échelle du bloc)
                vd.Geometry.Circle(centerPt, radiusMcs, normal);
            }
        }


        private static VertexCircleOverrule _myOverrule;

        public static void StartOverrule()
        {
            if (_myOverrule == null)
            {
                _myOverrule = new VertexCircleOverrule();

                // Cibler les classes spécifiques
                Overrule.AddOverrule(RXObject.GetClass(typeof(Autodesk.AutoCAD.DatabaseServices.Polyline)), _myOverrule, true);
                Overrule.AddOverrule(RXObject.GetClass(typeof(Line)), _myOverrule, true);
                Overrule.AddOverrule(RXObject.GetClass(typeof(Circle)), _myOverrule, true);
            }

            // Activer globalement les Overrules dans AutoCAD
            Overrule.Overruling = true;

            // Rafraîchir l'écran pour voir les changements immédiatement
            Generic.Regen();

            Generic.WriteMessage("Overrule activé : Les vertex sont affichés.");
        }

        public static void StopOverrule()
        {
            if (_myOverrule != null)
            {
                Overrule.RemoveOverrule(RXObject.GetClass(typeof(Autodesk.AutoCAD.DatabaseServices.Polyline)), _myOverrule);
                Overrule.RemoveOverrule(RXObject.GetClass(typeof(Line)), _myOverrule);
                Overrule.RemoveOverrule(RXObject.GetClass(typeof(Circle)), _myOverrule);

                _myOverrule.Dispose();
                _myOverrule = null;
            }

            Generic.Regen();
            Generic.WriteMessage("Overrule désactivé : Les vertex sont masqués.");
        }

        public static void ToggleOverrule()
        {
            if (_myOverrule is null)
            {
                StartOverrule();
            }
            else
            {
                StopOverrule();
            }
        }


    }
}

