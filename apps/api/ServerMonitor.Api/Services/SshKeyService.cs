using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using ServerMonitor.Api.BackgroundJobs;

namespace ServerMonitor.Api.Services;

public interface ISshKeyService
{
    (string privateKeyPath, string publicKey) GenerateKeyPair(int serverId, string serverName);
    string? GetPublicKey(int serverId);
    void DeleteKeyPair(int serverId);
    string GetPrivateKeyPath(int serverId);
}

public class SshKeyService : ISshKeyService
{
    private readonly string _keyBasePath;
    private readonly ILogger<SshKeyService> _logger;

    public SshKeyService(IOptions<MetricCollectionOptions> options, ILogger<SshKeyService> logger)
    {
        _keyBasePath = options.Value.SshKeyBasePath;
        _logger = logger;
        
        if (!Directory.Exists(_keyBasePath))
        {
            Directory.CreateDirectory(_keyBasePath);
        }
    }

    public (string privateKeyPath, string publicKey) GenerateKeyPair(int serverId, string serverName)
    {
        var privateKeyPath = GetPrivateKeyPath(serverId);
        var publicKeyPath = $"{privateKeyPath}.pub";

        using var rsa = RSA.Create(4096);
        
        var privateKeyPem = rsa.ExportRSAPrivateKeyPem();
        var publicKey = ExportOpenSshPublicKey(rsa, serverName);

        File.WriteAllText(privateKeyPath, privateKeyPem);
        File.WriteAllText(publicKeyPath, publicKey);

        _logger.LogInformation("Generated SSH key pair for server {ServerId} at {Path}", serverId, privateKeyPath);

        return (privateKeyPath, publicKey);
    }

    public string? GetPublicKey(int serverId)
    {
        var publicKeyPath = $"{GetPrivateKeyPath(serverId)}.pub";
        
        if (File.Exists(publicKeyPath))
        {
            return File.ReadAllText(publicKeyPath).Trim();
        }
        
        return null;
    }

    public void DeleteKeyPair(int serverId)
    {
        var privateKeyPath = GetPrivateKeyPath(serverId);
        var publicKeyPath = $"{privateKeyPath}.pub";

        if (File.Exists(privateKeyPath))
        {
            File.Delete(privateKeyPath);
            _logger.LogInformation("Deleted private key for server {ServerId}", serverId);
        }

        if (File.Exists(publicKeyPath))
        {
            File.Delete(publicKeyPath);
            _logger.LogInformation("Deleted public key for server {ServerId}", serverId);
        }
    }

    public string GetPrivateKeyPath(int serverId)
    {
        return Path.Combine(_keyBasePath, $"server_{serverId}_rsa");
    }

    private static string ExportOpenSshPublicKey(RSA rsa, string comment)
    {
        var parameters = rsa.ExportParameters(false);
        
        using var ms = new MemoryStream();
        
        WriteOpenSshBytes(ms, Encoding.ASCII.GetBytes("ssh-rsa"));
        WriteOpenSshBytes(ms, parameters.Exponent!);
        WriteOpenSshBytes(ms, AddLeadingZeroIfNeeded(parameters.Modulus!));
        
        var base64 = Convert.ToBase64String(ms.ToArray());
        var safeComment = comment.Replace(" ", "_").Replace("@", "_at_");
        
        return $"ssh-rsa {base64} monitor_{safeComment}";
    }

    private static void WriteOpenSshBytes(Stream stream, byte[] bytes)
    {
        var lengthBytes = BitConverter.GetBytes(bytes.Length);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(lengthBytes);
        stream.Write(lengthBytes, 0, 4);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static byte[] AddLeadingZeroIfNeeded(byte[] bytes)
    {
        if (bytes.Length > 0 && (bytes[0] & 0x80) != 0)
        {
            var result = new byte[bytes.Length + 1];
            result[0] = 0;
            Buffer.BlockCopy(bytes, 0, result, 1, bytes.Length);
            return result;
        }
        return bytes;
    }
}
