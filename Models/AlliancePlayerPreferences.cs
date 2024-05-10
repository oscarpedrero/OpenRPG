namespace OpenRPG.Models;

public struct AlliancePlayerPreferences
{
    public bool IgnoringInvites = false;

    public AlliancePlayerPreferences()
    {
    }

    public override string ToString()
    {
        return $"Ignoring invites: {IgnoringInvites}";
    }
}