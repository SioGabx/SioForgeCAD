using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SioForgeCAD.Functions
{
    public static class UPDATEXREFS
    {
        public static void Update()
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();
            string hostDir = Path.GetDirectoryName(db.Filename);

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

                foreach (ObjectId btrId in bt)
                {
                    BlockTableRecord btr =
                        (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);

                    if (!btr.IsFromExternalReference || string.IsNullOrEmpty(btr.PathName))
                        continue;

                    // Résolution du chemin réel
                    string resolvedPath = ResolveXrefPath(db, btr.PathName);

                    string directory = Path.GetDirectoryName(resolvedPath);
                    string fileName = Path.GetFileNameWithoutExtension(resolvedPath);

                    if (!Directory.Exists(directory))
                    {
                        continue;
                    }


                    Match match = Regex.Match(fileName, @"^(.*_)(\d+)$");
                    if (!match.Success)
                        continue;

                    string baseName = match.Groups[1].Value;
                    int currentVersion = int.Parse(match.Groups[2].Value);

                    var newer = Directory.GetFiles(directory, baseName + "*.dwg")
                        .Select(f =>
                        {
                            Match m = Regex.Match(
                                Path.GetFileNameWithoutExtension(f),
                                @"^(.*_)(\d+)$");

                            return new
                            {
                                FullPath = f,
                                Version = m.Success ? int.Parse(m.Groups[2].Value) : -1
                            };
                        })
                        .Where(x => x.Version > currentVersion)
                        .OrderByDescending(x => x.Version)
                        .FirstOrDefault();

                    if (newer == null)
                        continue;

                    // Recalcul du chemin relatif (si la Xref était relative)
                    string newPath = btr.PathName.StartsWith("..")
                        ? MakeRelativePath(hostDir, newer.FullPath)
                        : newer.FullPath;

                    btr.UpgradeOpen();
                    btr.PathName = newPath;

                    ed.WriteMessage(
                        $"\nXref mise à jour :\n{btr.PathName}\n→ {newPath}");
                }

                tr.Commit();
            }
        }

        private static string ResolveXrefPath(Database db, string xrefPath)
        {
            if (Path.IsPathRooted(xrefPath))
                return xrefPath;

            string hostDir = Path.GetDirectoryName(db.Filename);
            return Path.GetFullPath(Path.Combine(hostDir, xrefPath));
        }

        private static string MakeRelativePath(string fromPath, string toPath)
        {
            Uri fromUri = new Uri(fromPath.EndsWith("\\") ? fromPath : fromPath + "\\");
            Uri toUri = new Uri(toPath);

            Uri relativeUri = fromUri.MakeRelativeUri(toUri);
            return Uri.UnescapeDataString(relativeUri.ToString().Replace('/', '\\'));
        }
    }
}
