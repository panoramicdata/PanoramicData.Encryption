using System.Security.Cryptography;
using System.Text;

namespace PanoramicData.Encryption;

public class EncryptionService
{
	private readonly byte[] _key;

	private readonly static RandomNumberGenerator _random = RandomNumberGenerator.Create();
	private readonly static UTF8Encoding _encoder = new();
	private readonly static Aes _aes = Aes.Create();


	public EncryptionService(string encryptionKey)
	{
		ArgumentNullException.ThrowIfNull(encryptionKey, nameof(encryptionKey));
		if (encryptionKey.Length != 64)
			throw new ArgumentException("EncryptionKey must be a 64 character hex string", nameof(encryptionKey));
		_key = HexStringToByteArray(encryptionKey);
	}

	public (string cipherText, string salt) Encrypt(string unencrypted, string? salt = null)
	{
		if (salt is not null && salt.Length != 32)
			throw new ArgumentException("Salt must be a 32 character hex string", nameof(salt));

		static byte[] GenerateVector()
		{
			var vector = new byte[16];
			_random.GetBytes(vector);
			return vector;
		}

		var vector = salt is null
				? GenerateVector()
				: HexStringToByteArray(salt);
		var encryptor = _aes.CreateEncryptor(_key, vector);
		return (ByteArrayToHexString(Transform(_encoder.GetBytes(unencrypted), encryptor)), ByteArrayToHexString(vector));
	}

	public string Decrypt(string encryptedString, string salt)
	{
		ArgumentNullException.ThrowIfNull(encryptedString, nameof(encryptedString));
		ArgumentNullException.ThrowIfNull(salt, nameof(salt));
		if (salt.Length != 32)
			throw new ArgumentException("Salt must be a 16 character hex string", nameof(salt));

		var vector = HexStringToByteArray(salt);

		var encrypted = HexStringToByteArray(encryptedString);

		var decryptor = _aes.CreateDecryptor(_key, vector);
		var decrypt = _encoder.GetString(Transform(encrypted, decryptor));
		return decrypt;
	}

	private static byte[] Transform(byte[] buffer, ICryptoTransform transform)
	{
		var stream = new MemoryStream();
		using (var cs = new CryptoStream(stream, transform, CryptoStreamMode.Write)) {
			cs.Write(buffer, 0, buffer.Length);
		}

		return stream.ToArray();
	}

	private static string ByteArrayToHexString(byte[] byteArray)
	{
		var hex = new StringBuilder(byteArray.Length * 2);
		foreach (var @byte in byteArray) {
			hex.AppendFormat("{0:x2}", @byte);
		}

		return hex.ToString();
	}

	private static byte[] HexStringToByteArray(string hexString)
	{
		if (!hexString.All("0123456789abcdefABCDEF".Contains)) {
			throw new ArgumentException("Expected a hexadecimal string.");
		}

		return Enumerable.Range(0, hexString.Length)
			.Where(x => x % 2 == 0)
			.Select(x => Convert.ToByte(hexString.Substring(x, 2), 16))
			.ToArray();
	}
}