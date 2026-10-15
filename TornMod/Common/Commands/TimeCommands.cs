using Terraria;
using Terraria.ModLoader;

namespace TornMod.Common.Commands;

public abstract class TimeCommand : LocalTestCommand
{
    protected abstract bool Daytime { get; }
    public override string Usage => $"/{Command}";
    public override void Action(CommandCaller caller, string input, string[] args)
    {
        if (!Ready(caller)) return;
        if (args.Length != 0) { caller.Reply(Usage); return; }
        Main.dayTime = Daytime;
        Main.time = 0;
        caller.Reply(Daytime ? "Time set to dawn (4:30 AM)." : "Time set to nightfall (7:30 PM).");
    }
}

public class DayCommand : TimeCommand
{
    protected override bool Daytime => true;
    public override string Command => "day";
    public override string Description => "Sets the time to the start of the day (4:30 AM).";
}

public class NightCommand : TimeCommand
{
    protected override bool Daytime => false;
    public override string Command => "night";
    public override string Description => "Sets the time to the start of the night (7:30 PM).";
}
