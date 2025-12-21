using DSA_DigitalSignature.Models;
using System;
using System.Numerics;
using System.Security.Cryptography;

namespace DSA_DigitalSignature.Services
{
    public static class DsaKeyGenerator
    {
        public static DsaParams Generate2048BitKey()
        {
            using var dsa = DSA.Create(2048);
            var p = dsa.ExportParameters(true);

            return new DsaParams
            {
                P = new BigInteger(p.P, isUnsigned: true, isBigEndian: true),
                Q = new BigInteger(p.Q, isUnsigned: true, isBigEndian: true),
                G = new BigInteger(p.G, isUnsigned: true, isBigEndian: true),
                X = new BigInteger(p.X, isUnsigned: true, isBigEndian: true),
                Y = new BigInteger(p.Y, isUnsigned: true, isBigEndian: true)
            };
        }
    }
}
