namespace SioForgeCAD.Commun.Drawing
{
    public static class Colors
    {
        public enum Values
        {
            Layers = 0,
            Rouge = 1,
            Jaune = 2,
            Green = 3,
            Cyan = 4,
            Bleu = 5,
            Magenta = 6,
            Blanc = 7,
            GrisMoyen = 8,
            GrisClair = 9,
        }
        public static Autodesk.AutoCAD.Colors.Color ToAutoCADColor(Values color)
        {
            short EnumValue = short.Parse(((int)color).ToString());
            Autodesk.AutoCAD.Colors.Color colors = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByColor, EnumValue);
            return colors;
        }


    }

}
