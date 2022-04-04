# PanoramicData.Encryption

Although you could use the nuget package, why would you trust it?
Better to read and understand and copy the source into your project.

Seriously, there's only one file.  Copy it.  It's [here](https://raw.githubusercontent.com/panoramicdata/PanoramicData.Encryption/main/PanoramicData.Encryption/EncryptionService.cs).

To use:

```C#
   using PanoramicData.Encryption;

   // Preparation

   // Here is the plaintext we want to protect:
   var plaintext = "Hello, World!";
   
   // The encryption key should be a 32 byte array,
   // represented as a 64 character hex string.
   // NO NOT use this one.  It will work, but it is not secure.
   // DO NOT store your encryption keys in your source code.
   // Environment variables are one way to keep your data, keys and source code separate.
   // You could use var privateKey = Environment.GetEnvironmentVariable("PrivateKey") method to retrieve this.
   // So we have a working example, let's put it in code for now.
   var privateKey = "00112233445566778899AABBCCDDEEFF00112233445566778899AABBCCDDEEFF";

   // Create the encryption service using the private key.
   var encryptionService = new EncryptionService(privateKey);


   // Encryption

   // EITHER 1) create cipherText and salt at the same time.
   // This is the recommended way to do it.
   (var cipherText, var salt) = encryptionService.Encrypt(plaintext);

   // OR 2) create your own salt first:
   var salt = "00112233445566778899AABBCCDDEEFF";
   (var cipherText2, var _) = encryptionService.Encrypt(plaintext, salt);

   // Storage
   // You should store the salt and cipherText in your database.
   // You may prefer to Base64 encode them before storing them, for storage efficiency.

   // ...

   // Retrieval
   // The salt and cipherText are safe for anyone to have access to.
   // Without the private key, no-one cannot decrypt the data.
   // Retrieve them from storage...

   // Decryption

   // You can decrypt the cipherText using the EncryptionService and the salt:
   var restoredPlainText = encryptionService.Decrypt(cipherText, salt);

   Console.WriteLine(restoredPlainText);

   // Worst.  "Hello, World!"  Ever.
```