using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
// using SioForgeCAD.Forms; // Inutile ici sauf si vous utilisez des formulaires custom
using System;
using System.Collections.Generic;
using System.Windows.Forms; // Pour le Timer
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace SioForgeCAD.Functions
{
    public static class VEGBLOCSCATTER
    {
        public class CircleSimulation : IDisposable
        {
            public class CircleParticle
            {
                public Point3d Position;
                public double Radius;
                public BlockReference Entity;
            }

            private List<CircleParticle> particles = new List<CircleParticle>();
            private Random random = new Random();
            private Timer simulationTimer;
            private TransientManager currentTm;
            private bool isRunning = false;

            // --- Paramètres configurables ---
            private double attractionStrength = 0.01;
            private double maxStep = 2.0;
            private double scatterWidth = 20.0; // Zone de dispersion (fixe ou calculée)

            private double compressionFactor = 0.005;
            private double compressionResistanceExponent = 0.8; // Variable rétablie
            private int relaxIterations = 3;
            private int maxTotalFrames = 500;
            private int currentFrame = 0;

            public CircleSimulation()
            {
                currentTm = TransientManager.CurrentTransientManager;
            }
            Point3d centerPoint = Point3d.Origin;
            public void Execute()
            {
                Editor ed = Generic.GetEditor();
                Database db = Generic.GetDatabase();

                // 1. Sélection des blocs (Une seule fois au début)
                SelectionFilter filterList = new SelectionFilter(new TypedValue[] { new TypedValue((int)DxfCode.Start, "INSERT") });
                var blk = ed.GetSelectionRedraw("Selectionnez les blocs sources", false, false, filterList, null);
                if (blk.Status != PromptStatus.OK) return;

                // 2. Sélection du point central
                var InsertRes = ed.GetPoint("\nSelectionnez le centre de la dispersion");
                if (InsertRes.Status != PromptStatus.OK) return;
                centerPoint = InsertRes.Value;

                ObjectId[] selectedIds = blk.Value.GetObjectIds();

                // Boucle de régénération
                while (true)
                {
                    // Sécurité : arrêt propre avant de recommencer
                    StopAndClear();
                    particles.Clear();

                    // 3. Création des particules (Transaction locale à l'itération)
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        foreach (var id in selectedIds)
                        {
                            BlockReference blkRef = tr.GetObject(id, OpenMode.ForRead) as BlockReference;
                            if (blkRef == null) continue;

                            // -- Correction Mathématique --
                            // Dispersion AUTOUR du point central (centerPoint)
                            double offsetX = (random.NextDouble() - 0.5) * scatterWidth;
                            double offsetY = (random.NextDouble() - 0.5) * scatterWidth;

                            Point3d spawnPos = new Point3d(centerPoint.X + offsetX, centerPoint.Y + offsetY, 0);

                            // Récupération des données (Radius)
                            double particleRadius = 0;
                            var BlocData = VEGBLOC.GetDataStore(blkRef);
                            if (BlocData != null)
                            {
                                double.TryParse(BlocData.TryGetValueString(VEGBLOC.DataStore.Width), out particleRadius);
                            }

                            // Création du clone pour affichage temporaire (Transient)
                            BlockReference clone = blkRef.Clone() as BlockReference;
                            clone.Position = spawnPos; // Important : placer le visuel au départ

                            particles.Add(new CircleParticle
                            {
                                Radius = particleRadius / 2,
                                Position = spawnPos,
                                Entity = clone
                            });
                        }
                        tr.Commit();
                    }

                    // 4. Lancement de la simulation
                    Start();

                    // 5. Interaction pendant que ça bouge
                    PromptPointOptions promptOpts = new PromptPointOptions("\nCliquez pour VALIDER et quitter, ou tapez 'R' pour REGÉNÉRER")
                    {
                        AllowNone = true // Permet 'Entrée'
                    };
                    promptOpts.Keywords.Add("Regenerer");

                    PromptPointResult userAction = ed.GetPoint(promptOpts);

                    if (userAction.Status == PromptStatus.Keyword && userAction.StringResult == "Regenerer")
                    {
                        // La boucle 'while' continue, on efface et on recommence
                        continue;
                    }
                    else if (userAction.Status == PromptStatus.OK || userAction.Status == PromptStatus.None)
                    {
                        // Validation : On arrête la simu et on sort de la boucle
                        Stop(); // On arrête le timer, mais on ne Clear PAS les particules si on veut les transformer en vrais objets

                        // NOTE : Ici, vous devriez probablement transformer les 'transients' en vrais objets DB
                        // Pour l'instant, le code original ne faisait rien (les objets disparaissaient).
                        // Je laisse StopAndClear() à la fin du using/dispose pour nettoyer si pas sauvegardé.
                        using (Transaction tr2 = db.TransactionManager.StartTransaction())
                        {

                            foreach (var p in particles)
                            {
                                p.Entity.AddToDrawingCurrentTransaction();
                            }
                            tr2.Commit();
                        }
                        break;
                    }
                    else
                    {
                        // Annulation (Echap)
                        StopAndClear();
                        return;
                    }
                }
            }

            public void Start()
            {
                if (isRunning) return;

                // Initialisation des graphiques
                foreach (var p in particles)
                {
                    // On s'assure que la position visuelle correspond à la logique
                    p.Entity.Position = p.Position;
                    currentTm.AddTransient(p.Entity, TransientDrawingMode.DirectShortTerm, 128, new IntegerCollection());
                }

                currentFrame = 0;
                isRunning = true;

                simulationTimer = new Timer { Interval = 15 }; // ~60 FPS
                simulationTimer.Tick += (s, e) => UpdateSimulation();
                simulationTimer.Start();
            }

            public void Stop()
            {
                if (simulationTimer != null)
                {
                    simulationTimer.Stop();
                    simulationTimer.Dispose();
                    simulationTimer = null;
                }
                isRunning = false;
            }

            public void StopAndClear()
            {
                Stop();
                foreach (var p in particles)
                {
                    if (p.Entity != null)
                    {
                        if (currentTm != null)
                        {
                            // Try/Catch pour éviter erreur si Transient déjà supprimé
                            try { currentTm.EraseTransient(p.Entity, new IntegerCollection()); } catch { }
                        }
                        p.Entity.Dispose();
                    }
                }
                particles.Clear();
                Application.DocumentManager.MdiActiveDocument?.Editor.UpdateScreen();
            }

            private void UpdateSimulation()
            {
                if (Application.DocumentManager.MdiActiveDocument == null)
                {
                    StopAndClear();
                    return;
                }

                currentFrame++;
                if (currentFrame > maxTotalFrames)
                {
                    Stop();
                    return;
                }


                // 1. Attraction vers le centre
                foreach (var p in particles)
                {
                    Vector3d toCenter = centerPoint - p.Position;

                    // CORRECTION : Utilisation de DotProduct pour la distance au carré
                    double distSq = toCenter.DotProduct(toCenter);

                    if (distSq > 0.001)
                    {
                        Vector3d move;
                        if (distSq > maxStep * maxStep)
                            move = toCenter.GetNormal() * maxStep * attractionStrength;
                        else
                            move = toCenter * attractionStrength;

                        p.Position += move;
                    }
                }

                // 2. Résolution des collisions
                for (int iter = 0; iter < relaxIterations; iter++)
                {
                    for (int i = 0; i < particles.Count; i++)
                    {
                        var p1 = particles[i];

                        for (int j = i + 1; j < particles.Count; j++)
                        {
                            var p2 = particles[j];

                            Vector3d delta = p2.Position - p1.Position;

                            // CORRECTION : DotProduct remplace LengthSq
                            double distSq = delta.DotProduct(delta);

                            double sumR = p1.Radius + p2.Radius;
                            double allowedDist = sumR * (1.0 - compressionFactor);
                            double allowedDistSq = allowedDist * allowedDist;

                            // Comparaison des carrés pour éviter Math.Sqrt si pas nécessaire
                            if (distSq < allowedDistSq && distSq > 1e-6)
                            {
                                double dist = Math.Sqrt(distSq); // On ne fait la racine qu'ici
                                double overlap = allowedDist - dist;

                                double factor = (overlap / allowedDist);
                                double resistance = Math.Pow(factor, compressionResistanceExponent);

                                Vector3d move = delta * (overlap * resistance / (dist * 2.0));

                                p1.Position -= move;
                                p2.Position += move;
                            }
                        }
                    }
                }

                // 3. Mise à jour graphique
                foreach (var p in particles)
                {
                    if (p.Entity.Position != p.Position)
                    {
                        p.Entity.Position = p.Position;
                        currentTm.UpdateTransient(p.Entity, new IntegerCollection());
                    }
                }

                Application.DocumentManager.MdiActiveDocument.Editor.UpdateScreen();
            }


            public void Dispose()
            {
                StopAndClear();
            }
        }
    }
}