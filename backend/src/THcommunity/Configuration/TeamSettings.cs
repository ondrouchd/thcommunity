namespace THcommunity.Configuration;

public class TeamSettings
{
    public string[] Roles { get; set; } = ["Admin", "Coach", "Player"];
    public string[] Positions { get; set; } = ["Player", "Goalie"];
    public PricingConfig Pricing { get; set; } = new();
    public CapacityConfig Capacity { get; set; } = new();
    public NotificationSettings Notifications { get; set; } = new();
}

public class PricingConfig
{
    public decimal GoaliePrice { get; set; } = 0;
    public int Tier1Count { get; set; } = 4;
    public decimal Tier1Price { get; set; } = 400;
    public int Tier2Count { get; set; } = 12;
    public decimal Tier2Price { get; set; } = 350;
    public decimal Tier3Price { get; set; } = 300;
}

public class CapacityConfig
{
    public int DefaultPlayers { get; set; } = 20;
    public int DefaultGoalies { get; set; } = 2;
}

public class NotificationSettings
{
    public bool EventCreated { get; set; } = true;
    public bool EventReminder { get; set; } = true;
    public int ReminderHoursBefore { get; set; } = 24;
    public bool SpotOpened { get; set; } = true;
    public string SpotOpenedMode { get; set; } = "allWaitlist";
}
