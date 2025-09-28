using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SimpleFolderEncryptor
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("Simple File/Folder Encryptor");
            
            try
            {
                if (args.Length == 3)
                {
                    string mode = args[0].ToLower();
                    string path = args[1];
                    string password = args[2];
                    
                    ProcessPath(mode, path, password);
                }
                else
                {
                    Console.Write("Enter mode (encrypt/decrypt): ");
                    string mode = Console.ReadLine().ToLower();
                    
                    Console.Write("Enter file or folder path: ");
                    string path = Console.ReadLine();
                    
                    Console.Write("Enter password: ");
                    string password = Console.ReadLine();
                    
                    ProcessPath(mode, path, password);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
        
        static void ProcessPath(string mode, string path, string password)
        {
            var crypto = new SimpleCrypto();
            if (File.Exists(path))
            {
                // если на вход файл
                //выбираем режим
                if (mode == "encrypt")
                {
                    Console.WriteLine($"Encrypting file: {path}");
                    string encryptedFile = path + ".encrypted";
                    crypto.EncryptFile(path, encryptedFile, password);
                    File.Delete(path);
                    Console.WriteLine("File encrypted successfully!");
                }
                else if (mode == "decrypt")
                {
                    if (path.EndsWith(".encrypted"))
                    {
                        Console.WriteLine($"Decrypting file: {path}");
                        string decryptedFile = path.Substring(0, path.Length - 10);
                        crypto.DecryptFile(path, decryptedFile, password);
                        File.Delete(path);
                        Console.WriteLine("File decrypted successfully!");
                    }
                    else
                    {
                        Console.WriteLine("File doesn't have .encrypted extension. Skipping.");
                    }
                }
                else
                {
                    throw new ArgumentException("Mode must be 'encrypt' or 'decrypt'");
                }
            }
            else if (Directory.Exists(path))
            {
                if (mode == "encrypt")
                {
                    crypto.EncryptFolder(path, password);
                    Console.WriteLine("Folder encrypted successfully!");
                }
                else if (mode == "decrypt")
                {
                    crypto.DecryptFolder(path, password);
                    Console.WriteLine("Folder decrypted successfully!");
                }
                else
                {
                    throw new ArgumentException("Mode must be 'encrypt' or 'decrypt'");
                }
            }
            else
            {
                throw new FileNotFoundException($"Path not found: {path}");
            }
        }
    }
    
    public class SimpleCrypto
    {
        private readonly byte[] _salt = Encoding.UTF8.GetBytes("Salt123456"); // статичная соль для повышения стойкости и исключения перебора по хэшу
        
        public void EncryptFolder(string folderPath, string password)
        {
            var files = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);
            
            foreach (string file in files)
            {
                if (!file.EndsWith(".encrypted"))
                {
                    Console.WriteLine($"Encrypting: {file}");
                    string encryptedFile = file + ".encrypted";
                    EncryptFile(file, encryptedFile, password);
                    File.Delete(file);
                }
            }
        }
        
        public void DecryptFolder(string folderPath, string password)
        {
            var files = Directory.GetFiles(folderPath, "*.encrypted", SearchOption.AllDirectories);
            
            foreach (string file in files)
            {
                Console.WriteLine($"Decrypting: {file}");
                string decryptedFile = file.Substring(0, file.Length - 10); // 10 - длина .encrypted
                DecryptFile(file, decryptedFile, password);
                File.Delete(file);
            }
        }
        
        public void EncryptFile(string inputFile, string outputFile, string password)
        {
            using (var aes = Aes.Create())
            {
                var key = new Rfc2898DeriveBytes(password, _salt, 1000, HashAlgorithmName.SHA256); // Password-Based Key Derivation Function 2
                aes.Key = key.GetBytes(32);
                aes.IV = key.GetBytes(16); // IV - initialization vector
                
                using (var inputStream = File.OpenRead(inputFile))
                using (var outputStream = File.Create(outputFile))
                using (var cryptoStream = new CryptoStream(outputStream, 
                    aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    inputStream.CopyTo(cryptoStream);
                }
            }
        }
        
        public void DecryptFile(string inputFile, string outputFile, string password)
        {
            using (var aes = Aes.Create())
            {
                var key = new Rfc2898DeriveBytes(password, _salt, 1000, HashAlgorithmName.SHA256);
                aes.Key = key.GetBytes(32);
                aes.IV = key.GetBytes(16);
                
                using (var inputStream = File.OpenRead(inputFile))
                using (var outputStream = File.Create(outputFile))
                using (var cryptoStream = new CryptoStream(inputStream, 
                    aes.CreateDecryptor(), CryptoStreamMode.Read))
                {
                    cryptoStream.CopyTo(outputStream);
                }
            }
        }
    }
}