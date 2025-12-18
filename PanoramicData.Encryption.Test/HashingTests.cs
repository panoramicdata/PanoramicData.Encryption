namespace PanoramicData.Encryption.Test;

public class HashingTests
{
	private const string PlainText = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Donec euismod, nisl eget consectetur consectetur, nisi nisi aliquet nunc, euismod aliquam nunc nisi euismod nisi. Donec euismod, nisl eget consectetur consectetur, nisi nisi aliquet nunc, euismod aliquam nunc nisi euismod nisi. Donec euismod, nisl eget consectetur consectetur, nisi nisi aliquet nunc, euismod aliquam nunc nisi euismod nisi. Donec euismod, nisl eget consectetur consectetur, nisi nisi aliquet nunc, euismod aliquam nunc nisi euismod nisi. Donec euismod, nisl eget consectetur consectetur, nisi nisi aliquet nunc, euismod aliquam nunc nisi euismod nisi. Donec euismod, nisl eget consectetur consectetur, nisi nisi aliquet nunc, euismod aliquam nunc nisi euismod nisi. Donec euismod, nisl eget consectetur consectetur, nisi nisi aliquet nunc, euismod aliquam nunc nisi euismod nisi. Donec euismod, nisl eget consectetur consectetur, nisi nisi aliquet nunc, euismod aliquam nunc nisi euismod nisi. Donec euismod, nisl eget consectetur consectetur, nisi nisi aliquet nunc, euismod aliquam nunc nisi euismod nisi. Donec euismod, nisl eget consectetur consectetur, nisi nisi aliquet nunc, euismod aliquam nunc nisi euismod nisi.";

	/// <summary>
	/// Hashing should work
	/// </summary>
	[Fact]
	public void Hash_Text_Succeeds()
	{
		// Using the wrong salt should fail
		var hash = EncryptionService.GetHash(PlainText);
		hash.Should().NotBeNull();
		hash.Should().NotBe(PlainText);

		// Ensure that the hash entropy is high
		var shannonEntropy = EncryptionService.GetShannonEntropy(hash);
		shannonEntropy.Should().BeGreaterThan(3.5);
	}

	/// <summary>
	/// Hashing an empty string should work,
	/// the result should be as expected
	/// and the entopy should be high
	/// </summary>
	[Fact]
	public void Hash_StringEmpty_Succeeds()
	{
		// Using the wrong salt should fail
		var hash = EncryptionService.GetHash(string.Empty);
		hash.Should().NotBeNull();
		hash.Should().NotBe(string.Empty);
		hash.Should().Be("E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855");

		// Ensure that the hash entropy is high
		var shannonEntropy = EncryptionService.GetShannonEntropy(hash);
		shannonEntropy.Should().BeGreaterThan(3.5);
	}

	/// <summary>
	/// Using null should fail
	/// </summary>
	[Fact]
	public void Hash_Null_Throws()
	{
		// Using the wrong encryption key should fail
		_ = string.Empty
			.Invoking(ss => EncryptionService.GetHash(null!))
			.Should()
			.Throw<ArgumentNullException>();
	}
}
