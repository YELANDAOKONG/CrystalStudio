namespace CrystalStudio.Council;

/// <summary>
/// A proposal shown under an anonymous label.
/// </summary>
public sealed record LabeledProposal
{
    public LabeledProposal(string label, Proposal proposal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentNullException.ThrowIfNull(proposal);

        Label = label;
        Proposal = proposal;
    }

    public string Label { get; }

    public Proposal Proposal { get; }

    public override string ToString() => Label;
}
