using System;
using System.Security.Cryptography;
using FluentAssertions;
using Xunit;

namespace PanoramicData.Encryption.Test;

public class EncryptionTests
{
	private const string PlainText = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Donec euismod, nisl eget consectetur consectetur, nisi nisi aliquet nunc, euismod aliquam nunc nisi euismod nisi. Donec euismod, nisl eget consectetur consectetur, nisi nisi aliquet nunc, euismod aliquam nunc nisi euismod nisi. Donec euismod, nisl eget consectetur consectetur, nisi nisi aliquet nunc, euismod aliquam nunc nisi euismod nisi. Donec euismod, nisl eget consectetur consectetur, nisi nisi aliquet nunc, euismod aliquam nunc nisi euismod nisi. Donec euismod, nisl eget consectetur consectetur, nisi nisi aliquet nunc, euismod aliquam nunc nisi euismod nisi. Donec euismod, nisl eget consectetur consectetur, nisi nisi aliquet nunc, euismod aliquam nunc nisi euismod nisi. Donec euismod, nisl eget consectetur consectetur, nisi nisi aliquet nunc, euismod aliquam nunc nisi euismod nisi. Donec euismod, nisl eget consectetur consectetur, nisi nisi aliquet nunc, euismod aliquam nunc nisi euismod nisi. Donec euismod, nisl eget consectetur consectetur, nisi nisi aliquet nunc, euismod aliquam nunc nisi euismod nisi. Donec euismod, nisl eget consectetur consectetur, nisi nisi aliquet nunc, euismod aliquam nunc nisi euismod nisi.";
	private const string EncryptionKey = "00112233445566778899AABBCCDDEEFF00112233445566778899AABBCCDDEEFF";
	private const string BadEncryptionKey = "00112233445566778899AABBCCDDEEFF00112233445566778899AABBCCDDEEF£";
	private const string WrongEncryptionKey = "00112233445566778899AABBCCDDEEFF00112233445566778899AABBCCDDEEFE";
	private const string ShortEncryptionKey = "00112233445566778899AABBCCDDEEFF00112233445566778899AABBCCDDEEF";
	private const string WrongSalt = "00112233445566778899AABBCCDDEEFF";
	private const string TooShortSalt = "001122334455667788";
	private readonly string _cipherText;
	private readonly string _salt;
	private readonly EncryptionService _goodSecurityService;
	private readonly EncryptionService _badSecurityService;

	public EncryptionTests()
	{
		_goodSecurityService = new EncryptionService(EncryptionKey);
		_badSecurityService = new EncryptionService(WrongEncryptionKey);
		(_cipherText, _salt) = _goodSecurityService.Encrypt(PlainText);
	}

	/// <summary>
	/// The encryption key should not be null
	/// </summary>
	[Fact]
	public void NullEncryptionKey_Fails()
		=> string.Empty.Invoking(o => new EncryptionService(null!))
			.Should()
			.Throw<ArgumentNullException>();

	/// <summary>
	/// A short encryption key should throw an ArgumentException
	/// </summary>
	[Fact]
	public void ShortEncryptionKey_Fails()
		=> string.Empty.Invoking(o => new EncryptionService(ShortEncryptionKey))
			.Should()
			.Throw<ArgumentException>();

	/// <summary>
	/// A bad encryption key should throw an ArgumentException
	/// </summary>
	[Fact]
	public void BadEncryptionKey_Fails()
		=> string.Empty.Invoking(o => new EncryptionService(BadEncryptionKey))
			.Should()
			.Throw<ArgumentException>();

	/// <summary>
	/// The cipher text and salt should be not be null, empty or the plain text
	/// </summary>
	[Fact]
	public void Basics_Succeed()
	{
		CheckCipherOrSalt(_cipherText);
		CheckCipherOrSalt(_salt);
	}

	private static void CheckCipherOrSalt(string salt)
	{
		salt.Should().NotBeNullOrEmpty();
		salt.Should().NotBe(PlainText);
	}

	/// <summary>
	/// Using the right salt should succeed
	/// </summary>
	[Fact]
	public void EncryptDecryptCycle_Succeeds()
		=> _goodSecurityService
			.Decrypt(_cipherText, _salt)
			.Should()
			.Be(PlainText);

	/// <summary>
	/// Using null salt should throw
	/// </summary>
	[Fact]
	public void NullSalt_Throws()
		=> _goodSecurityService
			.Invoking(ss => ss.Decrypt(_cipherText, null!))
			.Should().Throw<ArgumentNullException>();

	/// <summary>
	/// Using null cipherText should throw an ArgumentNullException
	/// </summary>
	[Fact]
	public void NullCipherText_Throws()
		=> _goodSecurityService
			.Invoking(ss => ss.Decrypt(null!, _salt))
			.Should().Throw<ArgumentNullException>();

	/// <summary>
	/// Using too short a salt to encrypt should throw an ArgumentException
	/// </summary>
	[Fact]
	public void EncryptWithPreviousSalt_Succeeds()
	{
		(var cipherText, var salt) = _goodSecurityService.Encrypt(PlainText, _salt);
		salt.Should().Be(_salt);
		cipherText.Should().Be(_cipherText);
		_goodSecurityService
			.Decrypt(cipherText, salt)
			.Should()
			.Be(PlainText);
	}

	/// <summary>
	/// Using too short a salt to encrypt should throw an ArgumentException
	/// </summary>
	[Fact]
	public void EncryptWithShortSalt_Throws()
		=> _goodSecurityService
			.Invoking(ss => ss.Encrypt(PlainText, TooShortSalt))
			.Should().Throw<ArgumentException>();

	/// <summary>
	/// Using too short a salt to decrypt should throw an ArgumentException
	/// </summary>
	[Fact]
	public void DecryptWithShortSalt_Throws()
		=> _goodSecurityService
			.Invoking(ss => ss.Decrypt(_cipherText, TooShortSalt))
			.Should().Throw<ArgumentException>();

	/// <summary>
	/// Using the wrong salt should partially fail (see https://stackoverflow.com/a/71740594/6508058)
	/// </summary>
	[Fact]
	public void WrongSalt_PartiallyFails()
	{
		// Using the wrong salt should fail
		var result1 = _goodSecurityService
				.Decrypt(_cipherText, WrongSalt);

		result1
		.Should()
		.NotBeNull();

		result1
		.Should()
		.NotBe(PlainText);

		result1
		.Should()
		.EndWith(PlainText[16..]);
	}

	/// <summary>
	/// Using the wrong encryption key should fail
	/// </summary>
	[Fact]
	public void WrongEncryptionKey_ReturnsNull()
	{
		// Using the wrong encryption key should fail
		var result2 = _badSecurityService
			.Invoking(ss => ss.Decrypt(_cipherText, _salt))
			.Should()
			.Throw<CryptographicException>();
	}
}
