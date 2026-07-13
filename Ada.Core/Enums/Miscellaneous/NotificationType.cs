using System.ComponentModel;

namespace Ada.Core.Enums.Miscellaneous;

public enum NotificationType
{
    [Description("furni_placement_error")]
    FurniturePlacementError,
    
    [Description("floorplan_editor.error")]
    FloorPlanEditor
}