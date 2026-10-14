using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace TornMod.Common;

public class WeaponConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ServerSide;
    public static WeaponConfig Current => ModContent.GetInstance<WeaponConfig>();
    [DefaultValue(false)]
    public bool InstantRespawn = false;
    [DefaultValue(false)]
    public bool Invincibility = false;
    [DefaultValue(false)]
    public bool WarpToCursor = false;
}
