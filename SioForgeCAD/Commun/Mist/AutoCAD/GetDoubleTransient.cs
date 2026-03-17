using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using System;
using System.Collections.Generic;

namespace SioForgeCAD.Commun
{
    public class GetDoubleTransient : TransientBase
    {
        public GetDoubleTransient(DBObjectCollection Entities) : base(Entities, null)
        {
        }

        public PromptDoubleResult GetDouble(string Message, params string[] KeyWords)
        {
            var ed = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.Editor;

            CreateTransGraphics();

            PromptDoubleOptions options = new PromptDoubleOptions("\n" + Message)
            {
                AllowNone = true, // Permet d'appuyer sur Entrée pour valider sans taper de chiffre
                UseDefaultValue = false // Désactivé pour que "Entrée" renvoie bien le status 'None' et pas 'OK'
            };

            foreach (string KeyWord in KeyWords)
            {
                if (!string.IsNullOrWhiteSpace(KeyWord))
                {
                    options.Keywords.Add(KeyWord);
                }
            }

            if (options.Keywords.Count > 0)
            {
                options.AppendKeywordsToMessage = true;
            }

            PromptDoubleResult result = ed.GetDouble(options);

            ClearTransGraphics();

            return result;
        }
    }

    // Variante pour conserver les couleurs d'origine des objets copiés dans l'aperçu
    public class GetDoubleTransientNoColorChange : GetDoubleTransient
    {
        public GetDoubleTransientNoColorChange(DBObjectCollection Entities) : base(Entities)
        {
        }

        public override Color GetTransGraphicsColor(Entity Drawable, bool IsStaticDrawable)
        {
            return Drawable.Color;
        }

        public override Transparency GetTransGraphicsTransparency(Entity Drawable, bool IsStaticDrawable)
        {
            return Drawable.Transparency;
        }
    }
}

public class GetDoubleTransient : TransientBase
{
    public GetDoubleTransient(DBObjectCollection Entities) : base(Entities, null)
    {
    }

    public PromptDoubleResult GetDouble(string Message, params string[] KeyWords)
    {
        var ed = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.Editor;

        CreateTransGraphics();

        PromptDoubleOptions options = new PromptDoubleOptions("\n" + Message)
        {
            AllowNone = true, // Permet d'appuyer sur Entrée pour valider sans taper de chiffre
            UseDefaultValue = false // Désactivé pour que "Entrée" renvoie bien le status 'None' et pas 'OK'
        };

        foreach (string KeyWord in KeyWords)
        {
            if (!string.IsNullOrWhiteSpace(KeyWord))
            {
                options.Keywords.Add(KeyWord);
            }
        }

        if (options.Keywords.Count > 0)
        {
            options.AppendKeywordsToMessage = true;
        }

        PromptDoubleResult result = ed.GetDouble(options);

        ClearTransGraphics();

        return result;
    }
}

// Variante pour conserver les couleurs d'origine des objets copiés dans l'aperçu
public class GetDoubleTransientNoColorChange : GetDoubleTransient
{
    public GetDoubleTransientNoColorChange(DBObjectCollection Entities) : base(Entities)
    {
    }

    public override Color GetTransGraphicsColor(Entity Drawable, bool IsStaticDrawable)
    {
        return Drawable.Color;
    }

    public override Transparency GetTransGraphicsTransparency(Entity Drawable, bool IsStaticDrawable)
    {
        return Drawable.Transparency;
    }
}
