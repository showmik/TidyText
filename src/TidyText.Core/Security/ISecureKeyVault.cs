namespace TidyText.Core.Security
{
    /// <summary>
    /// Abstracts secure key storage so consumers don't depend on
    /// the concrete DPAPI + file-system implementation.
    /// </summary>
    public interface ISecureKeyVault
    {
        string GetKey(string provider);
        void SetKey(string provider, string key);
        void RemoveKey(string provider);
    }
}
