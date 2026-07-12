namespace VaultGuard.Services.Interfaces
{
    public enum ApprovalState { Pending, Approved, Denied, Expired, NotFound }

    /// <summary>Result of creating an approval — the initiating device shows <see cref="Number"/>.</summary>
    public record ApprovalCreated(string Id, int Number, IReadOnlyList<int> Choices, DateTime ExpiresAt);

    /// <summary>What the mobile app sees: the action + number choices to match (correct one is hidden).</summary>
    public record PendingApproval(string Id, string Action, IReadOnlyList<int> Choices, DateTime ExpiresAt);

    /// <summary>
    /// GitHub/Microsoft-style number-matching push approval. A device initiates an action; the server
    /// shows a 2-digit number on that device and pushes a prompt to the user's phone, which displays
    /// several numbers — the user taps the matching one to approve. Codes are valid for 60 seconds.
    /// </summary>
    public interface IApprovalService
    {
        Task<ApprovalCreated> CreateAsync(string userId, string action, CancellationToken ct = default);
        Task<ApprovalState> GetStateAsync(string id, CancellationToken ct = default);
        Task<IReadOnlyList<PendingApproval>> GetPendingAsync(string userId, CancellationToken ct = default);

        /// <summary>Respond to an approval. Approves only if not expired and the number matches.</summary>
        Task<ApprovalState> RespondAsync(string id, string userId, int selectedNumber, bool approve, CancellationToken ct = default);
    }
}
