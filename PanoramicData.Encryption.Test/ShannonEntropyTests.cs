using System;
using FluentAssertions;
using Xunit;

namespace PanoramicData.Encryption.Test;

public class ShannonEntropyTests
{
	private const string LoremIpsum = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Donec euismod, nisl eget consectetur consectetur, nisi nisi aliquet nunc, euismod aliquam nunc nisi euismod nisi. Donec euismod, nisl eget consectetur consectetur, nisi nisi aliquet nunc, euismod aliquam nunc nisi euismod nisi. Donec euismod, nisl eget consectetur consectetur, nisi nisi aliquet nunc, euismod aliquam nunc nisi euismod nisi. Donec euismod, nisl eget consectetur consectetur, nisi nisi aliquet nunc, euismod aliquam nunc nisi euismod nisi. Donec euismod, nisl eget consectetur consectetur, nisi nisi aliquet nunc, euismod aliquam nunc nisi euismod nisi. Donec euismod, nisl eget consectetur consectetur, nisi nisi aliquet nunc, euismod aliquam nunc nisi euismod nisi. Donec euismod, nisl eget consectetur consectetur, nisi nisi aliquet nunc, euismod aliquam nunc nisi euismod nisi. Donec euismod, nisl eget consectetur consectetur, nisi nisi aliquet nunc, euismod aliquam nunc nisi euismod nisi. Donec euismod, nisl eget consectetur consectetur, nisi nisi aliquet nunc, euismod aliquam nunc nisi euismod nisi. Donec euismod, nisl eget consectetur consectetur, nisi nisi aliquet nunc, euismod aliquam nunc nisi euismod nisi.";

	/// <summary>
	/// Hashing should work
	/// </summary>
	[Fact]
	public void EmptyString_ShannonEntroy_ShouldBeZero()
	{
		var shannonEntropy = EncryptionService.GetShannonEntropy(string.Empty);
		shannonEntropy.Should().Be(0);
	}

	/// <summary>
	/// Lorum ipsum should have a medium entropy
	/// </summary>
	[Fact]
	public void LoremIpsum_ShannonEntroy_ShouldBeLow()
	{
		var shannonEntropy = EncryptionService.GetShannonEntropy(LoremIpsum);
		shannonEntropy.Should().BeGreaterThan(1.0);
		shannonEntropy.Should().BeLessThan(4.0);
	}

	/// <summary>
	/// Lorum ipsum hash should have a high entropy
	/// </summary>
	[Fact]
	public void LoremIpsumHash_ShannonEntroy_ShouldBeHigh()
	{
		var shannonEntropy = EncryptionService.GetShannonEntropy(EncryptionService.GetHash(LoremIpsum));
		shannonEntropy.Should().BeGreaterThan(3.5);
	}

	/// <summary>
	/// Using null should fail
	/// </summary>
	[Fact]
	public void GetShannonEntropy_Null_Throws()
	{
		// Using the wrong encryption key should fail
		_ = string.Empty
			.Invoking(ss => EncryptionService.GetShannonEntropy(null!))
			.Should()
			.Throw<ArgumentNullException>();
	}
}
