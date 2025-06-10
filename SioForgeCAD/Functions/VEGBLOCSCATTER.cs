using Autodesk.AutoCAD.ApplicationServices.Core;
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
            public Vector3d Velocity = new Vector3d(0, 0, 0);
            public Circle CircleEntity;
        }

        public class CircleSimulation
        {
            static int tickCount = 0;


            private List<CircleParticle> particles = new List<CircleParticle>();
            private List<Drawable> transients = new List<Drawable>();
            private Random random = new Random();
            private Timer timer;
            private double minRadius = 2.0;
            private double maxRadius = 10.0;
            private int circleCount = 300;
            private double attractionStrength = 0.05;
            private double repulsionStrength = 1.0;
            private double repulsionExponent = 2.5;

            public void Start()
            {
                foreach (var t in transients)
                {
                    t.Dispose();
                }
                    ClearTransients();

                CreateParticles();
                tickCount = 0;
                timer = new Timer { Interval = 30 };
                timer.Tick += (s, e) => ContinueSimulation();
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

                    var circle = new Autodesk.AutoCAD.DatabaseServices.Circle(
                        new Point3d(x, y, 0),
                        Vector3d.ZAxis,
                        r
                    );

                    circle.ColorIndex = 1 + random.Next(6);

                    particles.Add(new CircleParticle
                    {
                        Radius = r,
                        Position = new Point3d(x, y, 0),
                        CircleEntity = circle
                    });
                }
            }

            private void ContinueSimulation()
            {
                tickCount++;
                if (tickCount > 3)
                {
                    timer.Stop();
                    return;
                }
                ClearTransients();
                UpdateSimulation();
            }

            private void UpdateSimulation()
            {
                ClearTransients();
                double epsilon = 0.1;
                double maxMove = 1.0;
                int relaxIterations = 10;

                // Attraction vers le centre
                foreach (var p in particles)
                {
                    Vector3d toCenter = Point3d.Origin - p.Position;
                    if (toCenter.Length > maxMove)
                        toCenter = toCenter.GetNormal() * maxMove;

                    p.Position += toCenter * 0.05; // attraction douce
                }

                // Relaxation multiple : repousser les cercles trop proches
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
                            double minDist = p1.Radius + p2.Radius + epsilon;

                            if (dist < minDist && dist > 0.0001)
                            {
                                Vector3d dir = delta.GetNormal();
                                double overlap = minDist - dist;
                                Vector3d move = dir * (overlap / 2.0);

                                // Déplace les deux à parts égales
                                p1.Position -= move;
                                p2.Position += move;
                            }
                        }
                    }
                }

                // Appliquer les nouvelles positions aux transients
                foreach (var p in particles)
                {
                    p.CircleEntity.Center = p.Position;

                    TransientManager.CurrentTransientManager.AddTransient(
                        p.CircleEntity,
                        TransientDrawingMode.DirectShortTerm,
                        128,
                        new IntegerCollection()
                    );
                    transients.Add(p.CircleEntity);
                }

                Application.DocumentManager.MdiActiveDocument.Editor.UpdateScreen();
            }


            private Vector3d ComputeAttraction(Point3d pos)
            {
                Vector3d toCenter = Point3d.Origin - pos;
                return toCenter * attractionStrength;
            }

            private Vector3d ComputeRepulsion(CircleParticle p1, CircleParticle p2)
            {
                Vector3d delta = p1.Position - p2.Position;
                double distance = delta.Length;
                double overlap = (p1.Radius + p2.Radius) - distance;

                if (overlap > 0)
                {
                    // Plus les cercles sont proches, plus la force est grande (exponentielle)
                    double strength = repulsionStrength * Math.Pow(overlap / (p1.Radius + p2.Radius), repulsionExponent);
                    Vector3d dir = delta.GetNormal();
                    return dir * strength;
                }

                return new Vector3d(0, 0, 0);
            }

            private void ClearTransients()
            {
                foreach (var t in transients)
                {
                    TransientManager.CurrentTransientManager.EraseTransient(t, new IntegerCollection());
                }
                transients.Clear();
            }
        }

    }
}
