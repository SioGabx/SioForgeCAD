namespace SioForgeCAD.Commun.Extensions
{
    public static class ObjectExtensions
    {
        public static bool TryGetDoubleValue(this object obj, out double value)
        {
            if (obj is double)
            {
                value = (double)obj;
            }
            else if (obj is float)
            {
                value = (float)obj;
            }
            else if (obj is int)
            {
                value = (int)obj;
            }
            else if (obj is short)
            {
                value = (short)obj;
            }
            else
            {
                value = 0;
                return false;
            }
            return true;
        }

    }
}
