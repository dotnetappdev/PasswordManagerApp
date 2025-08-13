using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using PasswordManager.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace PasswordManager.WinUi.Services;

/// <summary>
/// Windows Credential Manager service for storing sensitive data like master key material
/// This provides additional security beyond DPAPI by using the Windows Credential Manager
/// </summary>
public class WindowsCredentialManagerService : ISecureStorageService
{
    private readonly ILogger<WindowsCredentialManagerService> _logger;
    private readonly string _applicationName = "PasswordManagerWinUI";

    public WindowsCredentialManagerService(ILogger<WindowsCredentialManagerService> logger)
    {
        _logger = logger;
    }

    public async Task<string?> GetAsync(string key)
    {
        return await Task.Run(() =>
        {
            try
            {
                var targetName = GetTargetName(key);
                var credential = ReadCredential(targetName);
                return credential;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve credential from Windows Credential Manager for key: {Key}", key);
                return null;
            }
        });
    }

    public async Task SetAsync(string key, string value)
    {
        await Task.Run(() =>
        {
            try
            {
                var targetName = GetTargetName(key);
                WriteCredential(targetName, value);
                _logger.LogDebug("Successfully stored credential in Windows Credential Manager for key: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store credential in Windows Credential Manager for key: {Key}", key);
                throw;
            }
        });
    }

    public bool Remove(string key)
    {
        try
        {
            var targetName = GetTargetName(key);
            return DeleteCredential(targetName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove credential from Windows Credential Manager for key: {Key}", key);
            return false;
        }
    }

    public void RemoveAll()
    {
        try
        {
            // Get all credentials for this application and remove them
            var credentials = EnumerateCredentials();
            foreach (var credential in credentials)
            {
                if (credential.StartsWith($"{_applicationName}:"))
                {
                    DeleteCredential(credential);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove all credentials from Windows Credential Manager");
        }
    }

    public async Task<bool> RemoveAsync(string key)
    {
        return await Task.Run(() => Remove(key));
    }

    public async Task RemoveAllAsync()
    {
        await Task.Run(() => RemoveAll());
    }

    private string GetTargetName(string key)
    {
        return $"{_applicationName}:{key}";
    }

    private void WriteCredential(string targetName, string secret)
    {
        var credential = new CREDENTIAL
        {
            TargetName = targetName,
            Type = CRED_TYPE_GENERIC,
            UserName = Environment.UserName,
            CredentialBlob = Encoding.UTF8.GetBytes(secret),
            CredentialBlobSize = (uint)Encoding.UTF8.GetByteCount(secret),
            Persist = CRED_PERSIST_LOCAL_MACHINE,
            AttributeCount = 0,
            Attributes = IntPtr.Zero,
            TargetAlias = null,
            Comment = "PasswordManager Master Key Material"
        };

        if (!CredWrite(ref credential, 0))
        {
            var errorCode = Marshal.GetLastWin32Error();
            throw new Exception($"Failed to write credential to Windows Credential Manager. Error code: {errorCode}");
        }
    }

    private string? ReadCredential(string targetName)
    {
        if (CredRead(targetName, CRED_TYPE_GENERIC, 0, out IntPtr credPtr))
        {
            try
            {
                var credential = Marshal.PtrToStructure<CREDENTIAL>(credPtr);
                var passwordBytes = new byte[credential.CredentialBlobSize];
                Marshal.Copy(credential.CredentialBlob, passwordBytes, 0, (int)credential.CredentialBlobSize);
                return Encoding.UTF8.GetString(passwordBytes);
            }
            finally
            {
                CredFree(credPtr);
            }
        }
        
        var errorCode = Marshal.GetLastWin32Error();
        if (errorCode != ERROR_NOT_FOUND)
        {
            throw new Exception($"Failed to read credential from Windows Credential Manager. Error code: {errorCode}");
        }
        
        return null;
    }

    private bool DeleteCredential(string targetName)
    {
        if (!CredDelete(targetName, CRED_TYPE_GENERIC, 0))
        {
            var errorCode = Marshal.GetLastWin32Error();
            if (errorCode != ERROR_NOT_FOUND)
            {
                throw new Exception($"Failed to delete credential from Windows Credential Manager. Error code: {errorCode}");
            }
            return false;
        }
        return true;
    }

    private string[] EnumerateCredentials()
    {
        var credentials = new List<string>();
        
        if (CredEnumerate(null, 0, out uint count, out IntPtr credPtrs))
        {
            try
            {
                var credPtrArray = new IntPtr[count];
                Marshal.Copy(credPtrs, credPtrArray, 0, (int)count);

                foreach (var credPtr in credPtrArray)
                {
                    var credential = Marshal.PtrToStructure<CREDENTIAL>(credPtr);
                    credentials.Add(credential.TargetName);
                }
            }
            finally
            {
                CredFree(credPtrs);
            }
        }
        
        return credentials.ToArray();
    }

    #region Windows API Declarations

    private const int CRED_TYPE_GENERIC = 1;
    private const int CRED_PERSIST_LOCAL_MACHINE = 2;
    private const int ERROR_NOT_FOUND = 1168;

    [StructLayout(LayoutKind.Sequential)]
    private struct CREDENTIAL
    {
        public uint Flags;
        public uint Type;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string TargetName;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? TargetAlias;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string UserName;
    }

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredWrite(ref CREDENTIAL credential, uint flags);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredRead(string targetName, uint type, uint flags, out IntPtr credential);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredDelete(string targetName, uint type, uint flags);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredEnumerate(string? filter, uint flags, out uint count, out IntPtr credentials);

    [DllImport("advapi32.dll")]
    private static extern void CredFree(IntPtr buffer);

    #endregion
}