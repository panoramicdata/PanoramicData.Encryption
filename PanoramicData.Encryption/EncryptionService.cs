using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace PanoramicData.Encryption;

public class EncryptionService
{
	private readonly byte[] _key;

	private readonly static RandomNumberGenerator _random = RandomNumberGenerator.Create();
	private readonly static UTF8Encoding _encoder = new();
	private readonly static Aes _aes = Aes.Create();
	private readonly static SHA256 _hashAlgorithm = SHA256.Create();


	/// <summary>
	/// Created an EncryptionService
	/// </summary>
	/// <param name="encryptionKey">A 64 character hex string</param>
	/// <exception cref="ArgumentException"></exception>
	public EncryptionService(string encryptionKey)
	{
		ArgumentNullException.ThrowIfNull(encryptionKey, nameof(encryptionKey));
		if (encryptionKey.Length != 64) {
			throw new ArgumentException("EncryptionKey must be a 64 character hex string", nameof(encryptionKey));
		}
		_key = HexStringToByteArray(encryptionKey);
	}

	private static byte[] GenerateVector()
	{
		var vector = new byte[16];
		_random.GetBytes(vector);
		return vector;
	}

	public (string cipherText, string salt) Encrypt(string unencrypted)
	{
		var vector = GenerateVector();
		var encryptor = _aes.CreateEncryptor(_key, vector);
		return (ByteArrayToHexString(Transform(_encoder.GetBytes(unencrypted), encryptor)), ByteArrayToHexString(vector));
	}

	public (string cipherText, string salt) Encrypt(string unencrypted, string? salt)
	{
		if (salt is null) {
			return Encrypt(unencrypted);
		}

		if (salt.Length != 32) {
			throw new ArgumentException("Salt must be a 32 character hex string", nameof(salt));
		}

		var vector = HexStringToByteArray(salt);
		var encryptor = _aes.CreateEncryptor(_key, vector);
		return (ByteArrayToHexString(Transform(_encoder.GetBytes(unencrypted), encryptor)), ByteArrayToHexString(vector));
	}

	public string Decrypt(string encryptedString, string salt)
	{
		ArgumentNullException.ThrowIfNull(encryptedString, nameof(encryptedString));
		ArgumentNullException.ThrowIfNull(salt, nameof(salt));
		if (salt.Length != 32) {
			throw new ArgumentException("Salt must be a 32 character hex string", nameof(salt));
		}

		var vector = HexStringToByteArray(salt);

		var encrypted = HexStringToByteArray(encryptedString);

		var decryptor = _aes.CreateDecryptor(_key, vector);
		var decrypt = _encoder.GetString(Transform(encrypted, decryptor));
		return decrypt;
	}

	private static byte[] Transform(byte[] buffer, ICryptoTransform cryptoTransform)
	{
		var stream = new MemoryStream();
		using (var cs = new CryptoStream(stream, cryptoTransform, CryptoStreamMode.Write)) {
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

		return [.. Enumerable.Range(0, hexString.Length)
			.Where(x => x % 2 == 0)
			.Select(x => Convert.ToByte(hexString.Substring(x, 2), 16))];
	}

	public static string GetHash(string inputString)
	{
		ArgumentNullException.ThrowIfNull(inputString, nameof(inputString));

		var sb = new StringBuilder();
		foreach (var @byte in _hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString))) {
			sb.Append(@byte.ToString("X2", CultureInfo.InvariantCulture));
		}
		var result = sb.ToString();
		return result;
	}

	/// <summary>
	/// Returns a measure of entropy represented in a given string, per
	/// http://en.wikipedia.org/wiki/Entropy_(information_theory)
	/// Credit: https://codereview.stackexchange.com/questions/868/calculating-entropy-of-a-string
	/// </summary>
	public static double GetShannonEntropy(string text)
	{
		ArgumentNullException.ThrowIfNull(text, nameof(text));

		// Create a dictionary of each character and its frequency
		var characterFrequencyMap = new Dictionary<char, int>();
		foreach (var @char in text) {
			if (!characterFrequencyMap.TryAdd(@char, 1)) {
				characterFrequencyMap[@char] += 1;
			}
		}

		// Calculate the entropy
		var result = 0.0;
		foreach (var item in characterFrequencyMap) {
			var frequency = (double)item.Value / text.Length;
			result -= frequency * (Math.Log(frequency) / Math.Log(2));
		}

		// Return the result
		return result;
	}
}