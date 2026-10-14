namespace TornMod.Common.BoltAction;

public static class BoltMechanics
{
    public const int ReloadTicks = 120;
    public const int EjectTick = 12;
    public const int OpenTicks = 18;
    public const int CloseStart = 102;
    public const int GroundCasingTicks = 300;
    public static int Frame(int tick) => tick < 7 ? 1 : tick < OpenTicks ? 2 : tick < CloseStart ? 5 : tick < 111 ? 6 : 7;
}
