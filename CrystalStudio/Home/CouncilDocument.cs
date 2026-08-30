namespace CrystalStudio.Home;

internal sealed class CouncilDocument
{
    public string? Listen { get; set; }

    public int? MaxRounds { get; set; }

    public double? ConvergenceThreshold { get; set; }

    public int? MemberTimeoutSeconds { get; set; }

    public string? Model { get; set; }

    public List<MemberDocument>? Members { get; set; }
}
