using System.ComponentModel;

namespace Ada.Core.Enums.Game.Furniture;

public enum FurnitureItemType
{
    [Description("s")]
    Floor,
    
    [Description("i")]
    Wall,
    
    [Description("b")]
    Badge,
    
    [Description("e")]
    Effect,
    
    [Description("r")]
    Bot,
    
    [Description("p")]
    Pet,
    
    [Description("h")]
    Club
}