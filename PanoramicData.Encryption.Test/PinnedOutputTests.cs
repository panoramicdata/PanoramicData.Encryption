namespace PanoramicData.Encryption.Test;

/// <summary>
/// Pins the exact output of the library for known inputs, so that any change to the input
/// encoding, the algorithm, the padding or the output format (hex, casing) fails a test.
/// The expected values are published SHA-256 test vectors where one exists, and otherwise were
/// computed independently with the .NET BCL (SHA256.HashData / Aes.EncryptCbc over UTF-8).
/// </summary>
public class PinnedOutputTests
{
	private const string EncryptionKey = "00112233445566778899AABBCCDDEEFF00112233445566778899AABBCCDDEEFF";
	private const string Salt = "0f0e0d0c0b0a09080706050403020100";

	// "café € 100 🔒": two-, three- and four-byte UTF-8 sequences, including a surrogate pair.
	private const string UnicodeText = "café € 100 🔒";

	/// <summary>
	/// GetHash returns the uppercase hex SHA-256 of the UTF-8 bytes of the input.
	/// </summary>
	[Theory]
	[InlineData("", "E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855")]
	[InlineData("abc", "BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD")]
	[InlineData("The quick brown fox jumps over the lazy dog", "D7A8FBB307D7809469CA9ABCB0082E4F8D5651E46D3CDB762D02D0BF37C9E592")]
	[InlineData(UnicodeText, "91FD866D1271995C1AD7BC61B1970441991C32696B88F2ED75CB874B996D88F7")]
	public void GetHash_KnownInput_ReturnsPinnedValue(string input, string expected)
		=> EncryptionService.GetHash(input).Should().Be(expected);

	/// <summary>
	/// Encrypting with a fixed key and salt returns pinned lowercase hex cipher text (AES-256-CBC,
	/// PKCS7 padding, UTF-8 plaintext), and the salt is returned unchanged in lowercase hex.
	/// </summary>
	[Theory]
	[InlineData("", "2258814885f97e0e539cc0613346d8f4")]
	[InlineData("Hello, World!", "3ce17eea6f106637d64736e7ed1280f1")]
	[InlineData(UnicodeText, "08514b73e16093df81a1feea30eaae85efe306b05dc057f56172f0fbf88b3766")]
	public void Encrypt_KnownInput_ReturnsPinnedValue(string plainText, string expectedCipherText)
	{
		var service = new EncryptionService(EncryptionKey);

		var (cipherText, salt) = service.Encrypt(plainText, Salt);

		cipherText.Should().Be(expectedCipherText);
		salt.Should().Be(Salt);
		service.Decrypt(cipherText, salt).Should().Be(plainText);
	}

	/// <summary>
	/// Decrypting pinned cipher text returns the original plaintext, so data encrypted by earlier
	/// versions of the library remains readable.
	/// </summary>
	[Theory]
	[InlineData("2258814885f97e0e539cc0613346d8f4", "")]
	[InlineData("3ce17eea6f106637d64736e7ed1280f1", "Hello, World!")]
	[InlineData("08514B73E16093DF81A1FEEA30EAAE85EFE306B05DC057F56172F0FBF88B3766", UnicodeText)]
	public void Decrypt_PinnedCipherText_ReturnsPlainText(string cipherText, string expectedPlainText)
		=> new EncryptionService(EncryptionKey)
			.Decrypt(cipherText, Salt)
			.Should()
			.Be(expectedPlainText);

	/// <summary>
	/// A randomly generated salt is a 32 character lowercase hex string (16 bytes).
	/// </summary>
	[Fact]
	public void Encrypt_RandomSalt_IsLowercaseHexOf16Bytes()
	{
		var (_, salt) = new EncryptionService(EncryptionKey).Encrypt("Hello, World!");

		salt.Should().HaveLength(32);
		salt.Should().MatchRegex("^[0-9a-f]{32}$");
	}
}
