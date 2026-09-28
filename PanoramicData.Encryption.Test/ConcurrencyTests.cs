using System.Collections.Concurrent;

namespace PanoramicData.Encryption.Test;

/// <summary>
/// Regression tests for OPS-157454: EncryptionService previously shared a single static SHA256
/// instance across threads, which intermittently threw "Concurrent operations from multiple
/// threads on this type are not supported" and could silently return wrong hashes.
/// Each test runs many operations concurrently and asserts every result equals the
/// single-threaded result.
/// </summary>
public class ConcurrencyTests
{
	private const int Iterations = 5000;
	private const string EncryptionKey = "00112233445566778899AABBCCDDEEFF00112233445566778899AABBCCDDEEFF";
	private const string Salt = "0f0e0d0c0b0a09080706050403020100";

	private static readonly ParallelOptions Options = new()
	{
		MaxDegreeOfParallelism = Math.Max(8, Environment.ProcessorCount * 2)
	};

	private static readonly string[] Inputs =
	[
		string.Empty,
		"abc",
		"The quick brown fox jumps over the lazy dog",
		"café € 100 🔒",
		new string('x', 10_000),
	];

	/// <summary>
	/// Hashing from many threads at once returns the single-threaded result every time.
	/// </summary>
	[Fact]
	public void GetHash_Concurrent_MatchesSingleThreadedResult()
	{
		var expected = Inputs.Select(EncryptionService.GetHash).ToArray();
		var actual = new string[Iterations];

		Parallel.For(0, Iterations, Options, i => actual[i] = EncryptionService.GetHash(Inputs[i % Inputs.Length]));

		for (var i = 0; i < Iterations; i++)
		{
			actual[i].Should().Be(expected[i % Inputs.Length], "iteration {0} must match the single-threaded hash", i);
		}
	}

	/// <summary>
	/// Encrypting and decrypting from many threads at once, on one shared service instance and
	/// across instances, returns the single-threaded result every time.
	/// </summary>
	[Fact]
	public void EncryptDecrypt_Concurrent_MatchesSingleThreadedResult()
	{
		var shared = new EncryptionService(EncryptionKey);
		var expected = Inputs.Select(input => shared.Encrypt(input, Salt).cipherText).ToArray();
		var cipherTexts = new string[Iterations];
		var plainTexts = new string[Iterations];

		Parallel.For(0, Iterations, Options, i =>
		{
			var service = i % 2 == 0 ? shared : new EncryptionService(EncryptionKey);
			var input = Inputs[i % Inputs.Length];
			cipherTexts[i] = service.Encrypt(input, Salt).cipherText;
			plainTexts[i] = service.Decrypt(cipherTexts[i], Salt);
		});

		for (var i = 0; i < Iterations; i++)
		{
			cipherTexts[i].Should().Be(expected[i % Inputs.Length], "iteration {0} must match the single-threaded cipher text", i);
			plainTexts[i].Should().Be(Inputs[i % Inputs.Length], "iteration {0} must round-trip", i);
		}
	}

	/// <summary>
	/// Randomly generated salts produced from many threads at once are well formed, distinct,
	/// and round-trip.
	/// </summary>
	[Fact]
	public void Encrypt_RandomSaltConcurrent_ProducesDistinctSaltsThatRoundTrip()
	{
		var service = new EncryptionService(EncryptionKey);
		var salts = new ConcurrentBag<string>();

		Parallel.For(0, Iterations, Options, i =>
		{
			var input = Inputs[i % Inputs.Length];
			var (cipherText, salt) = service.Encrypt(input);
			service.Decrypt(cipherText, salt).Should().Be(input);
			salts.Add(salt);
		});

		salts.Should().HaveCount(Iterations);
		salts.Should().OnlyHaveUniqueItems();
		salts.Should().AllSatisfy(salt => salt.Should().MatchRegex("^[0-9a-f]{32}$"));
	}
}
