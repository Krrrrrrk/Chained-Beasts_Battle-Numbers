namespace ChainedBeasts.BattleNumbers;

public sealed class NumberPacket
{
    public int Protocol { get; set; } = 1;
    // Optional v1 extension: subscribe requests party receipts; accepted advertises host support.
    // Old peers omit/ignore it and retain owner-only routing.
    public bool AllPlayers { get; set; }
    public string Kind { get; set; }
    public string Session { get; set; }
    public string Battle { get; set; }
    public long Sequence { get; set; }
    public ulong Source { get; set; }
    public ulong Victim { get; set; }
    public ulong SourceOwner { get; set; }
    public ulong VictimOwner { get; set; }
    public bool SourcePlayer { get; set; }
    public bool VictimPlayer { get; set; }
    public bool SourceEnemy { get; set; }
    public bool VictimEnemy { get; set; }
    public int Loss { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public DamageReceipt ForClient(ulong local, double now, bool showOtherPlayers = false)
    {
        var source = new Actor(Source, SourcePlayer && SourceOwner == local, SourceEnemy, SourcePlayer);
        var victim = new Actor(Victim, VictimPlayer && VictimOwner == local, VictimEnemy, VictimPlayer);
        return new(Sequence, source, victim, ReceiptRules.Classify(source, victim, showOtherPlayers), Loss, new WorldPoint(X, Y, Z), now);
    }
}
