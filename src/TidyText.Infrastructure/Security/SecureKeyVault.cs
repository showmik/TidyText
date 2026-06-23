#pragma warning disable CA1416
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;

using TidyText.Domain.Security;
namespace TidyText.Infrastructure.Security
{
    public class SecureKeyVault : ISecureKeyVault
    {
        private readonly string _vaultPath;
        private Dictionary<string, string> _keys = new();

        public SecureKeyVault(string appDataFolderPath)
        {
            _vaultPath = Path.Combine(appDataFolderPath, "keys.dat");
            Load();
        }

        public void SetKey(string provider, string key)
        {
            _keys[provider] = key;
            Save();
        }

        public string GetKey(string provider)
        {
            return _keys.TryGetValue(provider, out var key) ? key : string.Empty;
        }

        public void RemoveKey(string provider)
        {
            if (_keys.Remove(provider))
            {
                Save();
            }
        }

        private void Load()
        {
            try
            {
                if (File.Exists(_vaultPath))
                {
                    byte[] encryptedData = File.ReadAllBytes(_vaultPath);
                    byte[] decryptedData = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);
                    string json = Encoding.UTF8.GetString(decryptedData);
                    _keys = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
                }
            }
            catch
            {
                _keys = new Dictionary<string, string>();
            }
        }

        private void Save()
        {
            try
            {
                string json = JsonSerializer.Serialize(_keys);
                byte[] dataToEncrypt = Encoding.UTF8.GetBytes(json);
                byte[] encryptedData = ProtectedData.Protect(dataToEncrypt, null, DataProtectionScope.CurrentUser);
                
                var dir = Path.GetDirectoryName(_vaultPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                
                File.WriteAllBytes(_vaultPath, encryptedData);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to save secure keys.", ex);
            }
        }
    }
}
