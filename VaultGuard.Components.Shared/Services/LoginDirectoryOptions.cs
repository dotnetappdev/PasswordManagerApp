namespace VaultGuard.Components.Shared.Services;

/// <summary>
/// How much the (anonymous) login page may reveal. The desktop/mobile apps are personal devices, so they keep
/// the "Who's unlocking?" account picker. A web server is public: listing accounts there hands every visitor
/// every user's name and email, and seeding the default accounts there creates admin accounts whose master
/// key is published in the repository. Hosts that don't register this get the device defaults.
/// </summary>
public sealed class LoginDirectoryOptions
{
    /// <summary>Show the account picker and the stored password hint. Off on the web host.</summary>
    public bool ListAccounts { get; init; } = true;

    /// <summary>Offer "Create default accounts". Off on the web host unless explicitly enabled (dev/demo).</summary>
    public bool AllowDefaultAccountSeeding { get; init; } = true;
}
