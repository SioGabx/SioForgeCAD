using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace SioForgeCAD.Commun
{
    public class GetStringTransientNoColorChange : TransientBase
    {
        public GetStringTransientNoColorChange(DBObjectCollection Entities) : base(Entities, null)
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

        public PromptResult GetString(string Message, string DefaultValue = "")
        {
            var ed = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.Editor;

            CreateTransGraphics();

            PromptStringOptions options = new PromptStringOptions("\n" + Message)
            {
                AllowSpaces = false, // Empêche les espaces pour forcer la validation sur "Espace"
                DefaultValue = DefaultValue,
                UseDefaultValue = !string.IsNullOrEmpty(DefaultValue),
            };

            PromptResult result = ed.GetString(options);

            ClearTransGraphics();

            return result;
        }
    }
}