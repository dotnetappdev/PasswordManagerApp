using VaultGuard.Imports.Interfaces;

namespace VaultGuard.Imports.Interfaces;

public interface IImportService
{
    void RegisterProvider(IPasswordImportProvider provider);
    IEnumerable<IPasswordImportProvider> GetAvailableProviders();
    Task<IEnumerable<IPasswordImportProvider>> GetAvailableProvidersAsync();
    Task<ImportResult> ImportPasswordsAsync(string providerName, Stream fileStream, string fileName, string? userId = null, IProgress<int>? progress = null, string? sessionId = null);
}
