using Autodesk.AutoCAD.DatabaseServices;

namespace SioForgeCAD.Commun.Overrules.PolyGripOverrule
{
    public class PolyGripMenu : MultiModesGripPE
    {
        public override GripMode CurrentMode(Entity entity, GripData gripData)
        {
            if (gripData is PolyCornerGrip PolyCorner)
            {
                var index = (int)PolyCorner.CurrentModeId - (int)GripMode.ModeIdentifier.CustomStart;
                return PolyCorner.GripModes[index];
            }
            else if (gripData is PolyMiddleGrip PolyMiddle)
            {
                var index = (int)PolyMiddle.CurrentModeId - (int)GripMode.ModeIdentifier.CustomStart;
                return PolyMiddle.GripModes[index];
            }
            return null;
        }

        public override uint CurrentModeId(Entity entity, GripData gripData)
        {
            if (gripData is PolyCornerGrip PolyCorner)
            {
                return (uint)PolyCorner.CurrentModeId;
            }
            else if (gripData is PolyMiddleGrip PolyMiddle)
            {
                return (uint)PolyMiddle.CurrentModeId;
            }
            return 0;
        }

        public override bool GetGripModes(
            Entity entity, GripData gripData, GripModeCollection modes, ref uint curMode)
        {
            if (gripData is PolyCornerGrip PolyCorner)
            {
                return PolyCorner.GetGripModes(ref modes, ref curMode);
            }
            else if (gripData is PolyMiddleGrip PolyMiddle)
            {
                return PolyMiddle.GetGripModes(ref modes, ref curMode);
            }
            return false;
        }

        public override GripType GetGripType(Entity entity, GripData gripData)
        {
            if (gripData is PolyCornerGrip || gripData is PolyMiddleGrip)
            {
                return GripType.Secondary;
            }
            return GripType.Primary;
        }

        public override bool SetCurrentMode(Entity entity, GripData gripData, uint curMode)
        {
            if (gripData is PolyCornerGrip PolyCorner)
            {
                PolyCorner.CurrentModeId = (GripMode.ModeIdentifier)curMode;
                return true;
            }
            else if (gripData is PolyMiddleGrip PolyMiddle)
            {
                PolyMiddle.CurrentModeId = (GripMode.ModeIdentifier)curMode;
                return true;
            }
            return false;
        }

        public override void Reset(Entity entity) { }
    }
}
