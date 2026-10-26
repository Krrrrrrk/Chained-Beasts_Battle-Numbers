namespace ChainedBeasts.BattleNumbers;

public sealed class NumberPacket
{
    public int Protocol { get; set; } = 1;
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
    public DamageReceipt ForClient(ulong local, double now) => new(Sequence,
        new Actor(Source, SourcePlayer && SourceOwner == local, SourceEnemy), new Actor(Victim, VictimPlayer && VictimOwner == local, VictimEnemy),
        ReceiptRules.Classify(new Actor(Source, SourcePlayer && SourceOwner == local, SourceEnemy), new Actor(Victim, VictimPlayer && VictimOwner == local, VictimEnemy)),
        Loss, new WorldPoint(X, Y, Z), now);
}
