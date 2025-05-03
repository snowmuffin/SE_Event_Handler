public class FactionReputation
{
    public long PlayerId { get; set; }
    public string PlayerName { get; set; }
    public int Reputation { get; set; } // 읽기/쓰기 가능

    // Additional properties for extended functionality
    public string FactionName { get; set; } // Faction name for better context
    public DateTime LastUpdated { get; set; } // Timestamp for tracking changes
}