using System.IO;

namespace DSA_DigitalSignature.Services
{
    public class FileService
    {
        public static byte[] ReadAllBytes(string path)
        {
            return File.ReadAllBytes(path);
        }
    }
}
