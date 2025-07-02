using PasswordManager.Imports.Interfaces;

namespace PasswordManager.Imports.Interfaces;

public interface IImportService
{
    void RegisterProvider(IPasswordImportProvider provider);
    IEnumerable<IPasswordImportProvider> GetAvailableProviders();
    Task<ImportResult> ImportPasswordsAsync(string providerName, Stream fileStream, string fileName);
}
