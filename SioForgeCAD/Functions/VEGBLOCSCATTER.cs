using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace SioForgeCAD.Functions
{
    public static class VEGBLOCSCATTER
    {

        public class CircleParticle
        {
            public Point3d Position;
            public double Radius;
            public Circle CircleEntity;
        }

        public class CircleSimulation
        {
            private List<CircleParticle> particles = new List<CircleParticle>();
            private List<Drawable> transients = new List<Drawable>();
            private Random random = new Random();

            // Paramètres configurables
            private double minRadius = 2.0;
            private double maxRadius = 10.0;
            private int circleCount = 300;
            //private int intinerationCount = 30;
            private double attractionStrength = 0.05;
            private double maxStep = 2.0;

            private double compressionFactor = 0.1;
            private double compressionResistanceExponent = 0.5;
            private int relaxIterations = 3;

            public void Start()
            {
                ClearTransients(true);
                CreateParticles();

                Timer timer = new Timer { Interval = 10 };
                timer.Tick += (s, e) => UpdateSimulation();
                timer.Start();
            }

            private void CreateParticles()
            {
                particles.Clear();
                for (int i = 0; i < circleCount; i++)
                {
                    double r = minRadius + random.NextDouble() * (maxRadius - minRadius);
                    double x = (random.NextDouble() - 0.5) * 200;
                    double y = (random.NextDouble() - 0.5) * 200;

                    var circle = new Circle(new Point3d(x, y, 0), Vector3d.ZAxis, r);
                    circle.ColorIndex = 1 + random.Next(6);

                    particles.Add(new CircleParticle
                    {
                        Radius = r,
                        Position = new Point3d(x, y, 0),
                        CircleEntity = circle
                    });
                }
            }

            private void UpdateSimulation()
            {

                ClearTransients();

                // Attraction vers le centre
                foreach (var p in particles)
                {
                    Vector3d toCenter = Point3d.Origin - p.Position;
                    if (toCenter.Length > maxStep)
                        toCenter = toCenter.GetNormal() * maxStep;

                    p.Position += toCenter * attractionStrength;
                }

                // Relaxation multiple pour propagation des contraintes
                for (int iter = 0; iter < relaxIterations; iter++)
                {
                    for (int i = 0; i < particles.Count; i++)
                    {
                        for (int j = i + 1; j < particles.Count; j++)
                        {
                            var p1 = particles[i];
                            var p2 = particles[j];

                            Vector3d delta = p2.Position - p1.Position;
                            double dist = delta.Length;
                            double sumR = p1.Radius + p2.Radius;

                            // Distance minimale autorisée avec compression
                            double allowedDist = sumR * (1 - compressionFactor);
                            if (dist < allowedDist && dist > 1e-4)
                            {
                                // Calcul de la résistance en fonction de la compression
                                double overlap = allowedDist - dist;
                                double resistance = Math.Pow(overlap / allowedDist, compressionResistanceExponent);

                                // Déplacement proportionnel /2 pour chaque particule
                                Vector3d move = delta.GetNormal() * ((overlap * resistance) / 2.0);

                                p1.Position -= move;
                                p2.Position += move;
                            }
                        }
                    }
                }

                // Affichage des transients
                foreach (var p in particles)
                {
                    p.CircleEntity.Center = p.Position;
                    TransientManager.CurrentTransientManager.AddTransient(
                        p.CircleEntity,
                        TransientDrawingMode.DirectShortTerm,
                        128,
                        new IntegerCollection());
                    transients.Add(p.CircleEntity);
                }

                Application.DocumentManager.MdiActiveDocument.Editor.UpdateScreen();
            }

            private void ClearTransients(bool Dispose = false)
            {
                foreach (var t in transients)
                {
                    TransientManager.CurrentTransientManager.EraseTransient(t, new IntegerCollection());
                    if (Dispose) t.Dispose();
                }

                transients.Clear();
            }

        }
    }
}