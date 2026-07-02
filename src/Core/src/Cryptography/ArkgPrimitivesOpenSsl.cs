// Copyright 2025 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Buffers;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using Yubico.YubiKit.Core.Native;

namespace Yubico.YubiKit.Core.Cryptography;

/// <summary>
/// OpenSSL-backed implementation of <see cref="IArkgPrimitives"/> for ARKG-P256.
/// </summary>
/// <remarks>
/// Provides the security-critical primitives required by the ARKG-P256
/// algorithm: on-curve point validation, ECDH shared-secret computation,
/// and the full draft-bradleylundberg-cfrg-arkg-09 derivation. Point math
/// goes through Yubico.NativeShims (OpenSSL); scalar reduction uses
/// <see cref="System.Numerics.BigInteger"/>.
/// </remarks>
internal sealed class ArkgPrimitivesOpenSsl : IArkgPrimitives
{
    private const int P256CoordinateLength = 32;
    private const int Sec1UncompressedLength = 1 + (2 * P256CoordinateLength);
    private const byte Sec1UncompressedTag = 0x04;

    private const string DstExt = "ARKG-P256";

    // P-256 group order N (SEC2 v2 §2.4.2). Used for scalar reduction mod N.
    private static readonly BigInteger N = BigInteger.Parse(
        "00FFFFFFFF00000000FFFFFFFFFFFFFFFFBCE6FAADA7179E84F3B9CAC2FC632551",
        NumberStyles.HexNumber,
        CultureInfo.InvariantCulture);

    // 2^256 mod N — used by the wide reduction in HashToScalar.
    // Bytes (BE): 00 00 00 00 FF FF FF FF 00 00 00 00 00 00 00 00
    //             43 19 05 52 58 E8 61 7B 0C 46 35 3D 03 9C DA AF
    private static readonly BigInteger TwoPow256ModN = BigInteger.Parse(
        "0000000000FFFFFFFF00000000000000004319055258E8617B0C46353D039CDAAF",
        NumberStyles.HexNumber,
        CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public bool IsPointOnCurve(ReadOnlySpan<byte> point)
    {
        if (point.Length != Sec1UncompressedLength || point[0] != Sec1UncompressedTag)
        {
            return false;
        }

        Span<byte> xBytes = stackalloc byte[P256CoordinateLength];
        Span<byte> yBytes = stackalloc byte[P256CoordinateLength];
        point.Slice(1, P256CoordinateLength).CopyTo(xBytes);
        point.Slice(1 + P256CoordinateLength, P256CoordinateLength).CopyTo(yBytes);

        using SafeEcGroup group = NativeMethods.EcGroupNewByCurveName(415); // NID for P-256
        using SafeEcPoint sslPoint = NativeMethods.EcPointNew(group);
        using SafeBigNum xBn = NativeMethods.BnBinaryToBigNum(xBytes);
        using SafeBigNum yBn = NativeMethods.BnBinaryToBigNum(yBytes);

        // EC_POINT_set_affine_coordinates returns 0 for an off-curve point on
        // most modern OpenSSL builds; fall through to the explicit on-curve
        // check so the answer is unambiguous regardless of OpenSSL version.
        int setResult = NativeMethods.EcPointSetAffineCoordinates(group, sslPoint, xBn, yBn);
        if (setResult != 1)
        {
            return false;
        }

        int onCurve = NativeMethods.EcPointIsOnCurve(group, sslPoint);
        return onCurve == 1;
    }

    /// <inheritdoc />
    public byte[] ComputeEcdhSharedSecret(ReadOnlySpan<byte> privateScalar, ReadOnlySpan<byte> publicPoint)
    {
        if (publicPoint.Length != Sec1UncompressedLength || publicPoint[0] != Sec1UncompressedTag)
        {
            throw new ArgumentException(
                "Public point must be a 65-byte SEC1 uncompressed P-256 point.",
                nameof(publicPoint));
        }

        // Reject points that lie outside the curve before doing any scalar
        // multiplication. Defends against invalid-curve attacks on the KEM
        // public key carried in the previewSign generated-key blob.
        if (!IsPointOnCurve(publicPoint))
        {
            throw new SecurityException("Public point is not on the P-256 curve.");
        }

        // Perform ECDH using OpenSSL: compute privateScalar * publicPoint.
        // Per RFC, the shared secret is the X coordinate of the resulting point.
        using SafeEcGroup group = NativeMethods.EcGroupNewByCurveName(415);
        using SafeEcPoint pubPoint = SecToPoint(group, publicPoint);
        using SafeEcPoint sharedPoint = NativeMethods.EcPointNew(group);

        Span<byte> privateScalarBytes = stackalloc byte[P256CoordinateLength];
        privateScalar.CopyTo(privateScalarBytes);
        using SafeBigNum privateValueBn = NativeMethods.BnBinaryToBigNum(privateScalarBytes);

        // Perform the scalar multiplication: sharedPoint = privateScalar * publicPoint
        int rc = NativeMethods.EcPointMul(
            group,
            sharedPoint,
            IntPtr.Zero,
            pubPoint.DangerousGetHandle(),
            privateValueBn.DangerousGetHandle());

        if (rc != 1)
        {
            throw new CryptographicException("EC_POINT_mul failed in ComputeEcdhSharedSecret.");
        }

        // Retrieve the X coordinate only (ECDH shared secret)
        using SafeBigNum xBn = NativeMethods.BnNew();
        using SafeBigNum yBn = NativeMethods.BnNew();
        rc = NativeMethods.EcPointGetAffineCoordinates(group, sharedPoint, xBn, yBn);

        if (rc != 1)
        {
            throw new CryptographicException("EC_POINT_get_affine_coordinates failed.");
        }

        byte[] sharedSecret = new byte[P256CoordinateLength];
        _ = NativeMethods.BnBigNumToBinaryWithPadding(xBn, sharedSecret.AsSpan());
        return sharedSecret;
    }

    /// <inheritdoc />
    public (byte[] derivedPk, byte[] arkgKeyHandle) Derive(
        ReadOnlySpan<byte> pkBl,
        ReadOnlySpan<byte> pkKem,
        ReadOnlySpan<byte> ikm,
        ReadOnlySpan<byte> ctx)
    {
        if (ctx.Length > 64)
        {
            throw new ArgumentOutOfRangeException(nameof(ctx), "ctx must be <= 64 bytes.");
        }

        if (!IsPointOnCurve(pkBl))
        {
            throw new ArgumentException("pkBl is not on the P-256 curve.", nameof(pkBl));
        }

        if (!IsPointOnCurve(pkKem))
        {
            throw new ArgumentException("pkKem is not on the P-256 curve.", nameof(pkKem));
        }

        Span<byte> ctxPrime = stackalloc byte[1 + ctx.Length];
        ctxPrime[0] = (byte)ctx.Length;
        ctx.CopyTo(ctxPrime.Slice(1));

        byte[] ctxKem = Concat(Encoding.ASCII.GetBytes("ARKG-Derive-Key-KEM."), ctxPrime);
        byte[] ctxBl = Concat(Encoding.ASCII.GetBytes("ARKG-Derive-Key-BL."), ctxPrime);

        (byte[] ikmTau, byte[] cipher) = HmacKemEncaps(pkKem, ikm, ctxKem);
        BigInteger tau;
        try
        {
            tau = BlPrf(ikmTau, ctxBl);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(ikmTau); // ZeroMemory site #1
        }

        byte[] derivedPk = BlBlindPublicKey(pkBl, tau);
        return (derivedPk, cipher);
    }

    // ---------------------------------------------------------------------
    // ARKG-BL: blinding-key arithmetic
    // ---------------------------------------------------------------------

    private static BigInteger BlPrf(ReadOnlySpan<byte> ikmTau, ReadOnlySpan<byte> ctx)
    {
        byte[] dst = Concat(
            Encoding.ASCII.GetBytes("ARKG-BL-EC."),
            Encoding.ASCII.GetBytes(DstExt),
            ctx);
        return HashToScalar(ikmTau, dst);
    }

    private static byte[] BlBlindPublicKey(ReadOnlySpan<byte> pkBl, BigInteger tau)
    {
        byte[] tauBytes = ScalarToBytes(tau);
        try
        {
            using SafeEcGroup group = NativeMethods.EcGroupNewByCurveName(415);
            using SafeEcPoint pkBlPoint = SecToPoint(group, pkBl);
            using SafeEcPoint result = NativeMethods.EcPointNew(group);
            using SafeBigNum tauBn = NativeMethods.BnBinaryToBigNum(tauBytes);
            using SafeBigNum oneBn = NativeMethods.BnBinaryToBigNum(stackalloc byte[] { 0x01 });

            // r = tau*G + 1*pkBl  =>  r = pkBl + tau*G.
            int rc = NativeMethods.EcPointMul(
                group,
                result,
                tauBn.DangerousGetHandle(),
                pkBlPoint.DangerousGetHandle(),
                oneBn.DangerousGetHandle());
            if (rc != 1)
            {
                throw new CryptographicException("EC_POINT_mul failed in BlBlindPublicKey.");
            }

            return PointToSec(group, result);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(tauBytes); // ZeroMemory site #2
        }
    }

    // ---------------------------------------------------------------------
    // ARKG-KEM: ECDH-KEM with HMAC wrapper
    // ---------------------------------------------------------------------

    private (byte[] shared, byte[] ciphertext) HmacKemEncaps(ReadOnlySpan<byte> pkKem, ReadOnlySpan<byte> ikm, ReadOnlySpan<byte> ctx)
    {
        byte[] dstAug = Encoding.ASCII.GetBytes("ARKG-ECDH.ARKG-P256");

        // Generate ephemeral keypair from IKM (deterministic, matches Rust reference).
        (byte[] ephPk, BigInteger ephSk) = KemDeriveKeypair(ikm);

        byte[] ephSkBytes = ScalarToBytes(ephSk);
        byte[] kPrime;
        try
        {
            kPrime = ComputeEcdhSharedSecret(ephSkBytes, pkKem);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(ephSkBytes); // ZeroMemory site #3
        }

        byte[] macInfo = Concat(
            Encoding.ASCII.GetBytes("ARKG-KEM-HMAC-mac."),
            dstAug,
            ctx);
        byte[] mk = HkdfUtilities.DeriveKey(kPrime, salt: ReadOnlySpan<byte>.Empty, contextInfo: macInfo, length: 32).ToArray();

        byte[] tag;
        try
        {
            using HMACSHA256 hmac = new(mk);
            byte[] full = hmac.ComputeHash(ephPk);
            tag = new byte[16];
            Buffer.BlockCopy(full, 0, tag, 0, 16);
            CryptographicOperations.ZeroMemory(full); // ZeroMemory site #4
        }
        finally
        {
            CryptographicOperations.ZeroMemory(mk); // ZeroMemory site #5
        }

        byte[] sharedInfo = Concat(
            Encoding.ASCII.GetBytes("ARKG-KEM-HMAC-shared."),
            dstAug,
            ctx);
        byte[] shared = HkdfUtilities.DeriveKey(kPrime, salt: ReadOnlySpan<byte>.Empty, contextInfo: sharedInfo, length: kPrime.Length).ToArray();
        CryptographicOperations.ZeroMemory(kPrime); // ZeroMemory site #6

        // Ciphertext = MAC tag || ephemeral public key.
        byte[] ciphertext = new byte[tag.Length + ephPk.Length];
        Buffer.BlockCopy(tag, 0, ciphertext, 0, tag.Length);
        Buffer.BlockCopy(ephPk, 0, ciphertext, tag.Length, ephPk.Length);

        return (shared, ciphertext);
    }

    private static (byte[] pk, BigInteger sk) KemDeriveKeypair(ReadOnlySpan<byte> ikm)
    {
        byte[] dst = Concat(
            Encoding.ASCII.GetBytes("ARKG-KEM-ECDH-KG.ARKG-ECDH."),
            Encoding.ASCII.GetBytes(DstExt));
        BigInteger sk = HashToScalar(ikm, dst);
        byte[] pk = ScalarMulGenerator(sk);
        return (pk, sk);
    }

    private static byte[] ScalarMulGenerator(BigInteger sk)
    {
        byte[] skBytes = ScalarToBytes(sk);
        try
        {
            using SafeEcGroup group = NativeMethods.EcGroupNewByCurveName(415);
            using SafeEcPoint pkPoint = NativeMethods.EcPointNew(group);
            using SafeBigNum skBn = NativeMethods.BnBinaryToBigNum(skBytes);

            int rc = NativeMethods.EcPointMul(
                group,
                pkPoint,
                skBn.DangerousGetHandle(),
                IntPtr.Zero,
                IntPtr.Zero);
            if (rc != 1)
            {
                throw new CryptographicException("EC_POINT_mul failed in ScalarMulGenerator.");
            }

            return PointToSec(group, pkPoint);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(skBytes); // ZeroMemory site #7
        }
    }

    // ---------------------------------------------------------------------
    // RFC 9380 hash-to-curve helpers (scalar variant only)
    // ---------------------------------------------------------------------

    private static BigInteger HashToScalar(ReadOnlySpan<byte> msg, ReadOnlySpan<byte> dst)
    {
        // P256_L = 48 = ceil((ceil(log2(p)) + k) / 8) with k=128.
        const int L = 48;
        byte[] uniform = ExpandMessageXmd(msg, dst, L);

        // Wide reduction: split into high(16) || low(32), each interpreted big-endian,
        // then result = high * (2^256 mod N) + low (mod N).
        BigInteger high = BytesToBigIntBE(uniform, 0, 16);
        BigInteger low = BytesToBigIntBE(uniform, 16, 32);
        return Mod((high * TwoPow256ModN) + low, N);
    }

    private static byte[] ExpandMessageXmd(ReadOnlySpan<byte> msg, ReadOnlySpan<byte> dst, int lenInBytes)
    {
        const int BInBytes = 32;
        const int SInBytes = 64;

        int ell = (lenInBytes + BInBytes - 1) / BInBytes;
        if (ell > 255 || lenInBytes > 65535 || dst.Length > 255)
        {
            throw new ArgumentException("expand_message_xmd parameter out of range.");
        }

        Span<byte> dstPrime = stackalloc byte[dst.Length + 1];
        dst.CopyTo(dstPrime);
        dstPrime[dst.Length] = (byte)dst.Length;

        Span<byte> zPad = stackalloc byte[SInBytes];
        zPad.Clear();
        Span<byte> lIBStr = stackalloc byte[2];
        lIBStr[0] = (byte)((lenInBytes >> 8) & 0xFF);
        lIBStr[1] = (byte)(lenInBytes & 0xFF);

        byte[] zeroByte = [0x00];
        byte[] msgPrime = Concat(zPad, msg, lIBStr, zeroByte, dstPrime);

        byte[][] bVals = new byte[ell + 1][];
        bVals[0] = SHA256.HashData(msgPrime);

        byte[] xored = new byte[BInBytes];
        byte[] singleByte = new byte[1];

        using (var sha = SHA256.Create())
        {
            singleByte[0] = 0x01;
            byte[] input = Concat(bVals[0], singleByte, dstPrime);
            bVals[1] = sha.ComputeHash(input);
        }

        for (int i = 2; i <= ell; i++)
        {
            for (int j = 0; j < BInBytes; j++)
            {
                xored[j] = (byte)(bVals[0][j] ^ bVals[i - 1][j]);
            }

            singleByte[0] = (byte)i;
            using var sha = SHA256.Create();
            byte[] input = Concat(xored, singleByte, dstPrime);
            bVals[i] = sha.ComputeHash(input);
        }

        byte[] result = new byte[lenInBytes];
        int offset = 0;
        for (int i = 1; i <= ell && offset < lenInBytes; i++)
        {
            int copy = Math.Min(BInBytes, lenInBytes - offset);
            Buffer.BlockCopy(bVals[i], 0, result, offset, copy);
            offset += copy;
        }

        return result;
    }

    // ---------------------------------------------------------------------
    // Conversion helpers
    // ---------------------------------------------------------------------

    private static byte[] ScalarToBytes(BigInteger scalar)
    {
        // BigInteger.ToByteArray is little-endian and includes a sign byte
        // when the high bit would otherwise read as negative — strip it,
        // then left-pad to 32 bytes big-endian.
        byte[] le = scalar.ToByteArray();
        int len = le.Length;
        if (len > 1 && le[len - 1] == 0)
        {
            len--;
        }

        byte[] be = new byte[P256CoordinateLength];
        int copy = Math.Min(len, P256CoordinateLength);
        for (int i = 0; i < copy; i++)
        {
            be[P256CoordinateLength - 1 - i] = le[i];
        }

        return be;
    }

    private static BigInteger BytesToBigIntBE(ReadOnlySpan<byte> bytes, int offset, int length)
    {
        byte[] padded = new byte[length + 1];
        for (int i = 0; i < length; i++)
        {
            padded[length - 1 - i] = bytes[offset + i];
        }

        // padded[length] = 0 by default — explicit positive sign.
        return new BigInteger(padded);
    }

    private static BigInteger Mod(BigInteger value, BigInteger modulus)
    {
        BigInteger r = value % modulus;
        if (r.Sign < 0)
        {
            r += modulus;
        }

        return r;
    }

    private static SafeEcPoint SecToPoint(SafeEcGroup group, ReadOnlySpan<byte> sec1)
    {
        Span<byte> x = stackalloc byte[P256CoordinateLength];
        Span<byte> y = stackalloc byte[P256CoordinateLength];
        sec1.Slice(1, P256CoordinateLength).CopyTo(x);
        sec1.Slice(1 + P256CoordinateLength, P256CoordinateLength).CopyTo(y);

        using SafeBigNum xBn = NativeMethods.BnBinaryToBigNum(x);
        using SafeBigNum yBn = NativeMethods.BnBinaryToBigNum(y);
        SafeEcPoint point = NativeMethods.EcPointNew(group);
        int rc = NativeMethods.EcPointSetAffineCoordinates(group, point, xBn, yBn);
        if (rc != 1)
        {
            point.Dispose();
            throw new CryptographicException("EC_POINT_set_affine_coordinates failed.");
        }

        return point;
    }

    private static byte[] PointToSec(SafeEcGroup group, SafeEcPoint point)
    {
        using SafeBigNum xBn = NativeMethods.BnNew();
        using SafeBigNum yBn = NativeMethods.BnNew();
        int rc = NativeMethods.EcPointGetAffineCoordinates(group, point, xBn, yBn);
        if (rc != 1)
        {
            throw new CryptographicException("EC_POINT_get_affine_coordinates failed.");
        }

        Span<byte> xBytes = stackalloc byte[P256CoordinateLength];
        Span<byte> yBytes = stackalloc byte[P256CoordinateLength];
        _ = NativeMethods.BnBigNumToBinaryWithPadding(xBn, xBytes);
        _ = NativeMethods.BnBigNumToBinaryWithPadding(yBn, yBytes);

        byte[] sec1 = new byte[Sec1UncompressedLength];
        sec1[0] = Sec1UncompressedTag;
        xBytes.CopyTo(sec1.AsSpan(1, P256CoordinateLength));
        yBytes.CopyTo(sec1.AsSpan(1 + P256CoordinateLength, P256CoordinateLength));
        return sec1;
    }

    private static byte[] Concat(params byte[][] parts)
    {
        int total = 0;
        for (int i = 0; i < parts.Length; i++)
        {
            total += parts[i].Length;
        }

        byte[] result = new byte[total];
        int offset = 0;
        for (int i = 0; i < parts.Length; i++)
        {
            Buffer.BlockCopy(parts[i], 0, result, offset, parts[i].Length);
            offset += parts[i].Length;
        }

        return result;
    }

    private static byte[] Concat(ReadOnlySpan<byte> part1, ReadOnlySpan<byte> part2)
    {
        byte[] result = new byte[part1.Length + part2.Length];
        part1.CopyTo(result);
        part2.CopyTo(result.AsSpan(part1.Length));
        return result;
    }

    private static byte[] Concat(ReadOnlySpan<byte> part1, ReadOnlySpan<byte> part2, ReadOnlySpan<byte> part3)
    {
        byte[] result = new byte[part1.Length + part2.Length + part3.Length];
        part1.CopyTo(result);
        part2.CopyTo(result.AsSpan(part1.Length));
        part3.CopyTo(result.AsSpan(part1.Length + part2.Length));
        return result;
    }

    private static byte[] Concat(ReadOnlySpan<byte> part1, ReadOnlySpan<byte> part2, ReadOnlySpan<byte> part3, ReadOnlySpan<byte> part4)
    {
        byte[] result = new byte[part1.Length + part2.Length + part3.Length + part4.Length];
        part1.CopyTo(result);
        part2.CopyTo(result.AsSpan(part1.Length));
        part3.CopyTo(result.AsSpan(part1.Length + part2.Length));
        part4.CopyTo(result.AsSpan(part1.Length + part2.Length + part3.Length));
        return result;
    }

    private static byte[] Concat(ReadOnlySpan<byte> part1, ReadOnlySpan<byte> part2, ReadOnlySpan<byte> part3, ReadOnlySpan<byte> part4, ReadOnlySpan<byte> part5)
    {
        byte[] result = new byte[part1.Length + part2.Length + part3.Length + part4.Length + part5.Length];
        part1.CopyTo(result);
        part2.CopyTo(result.AsSpan(part1.Length));
        part3.CopyTo(result.AsSpan(part1.Length + part2.Length));
        part4.CopyTo(result.AsSpan(part1.Length + part2.Length + part3.Length));
        part5.CopyTo(result.AsSpan(part1.Length + part2.Length + part3.Length + part4.Length));
        return result;
    }

    // ---------------------------------------------------------------------
    // P/Invoke declarations and SafeHandle types
    // ---------------------------------------------------------------------

    private static class NativeMethods
    {
        [DllImport(Libraries.NativeShims, EntryPoint = "Native_EC_GROUP_new_by_curve_name", ExactSpelling = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        private static extern IntPtr EcGroupNewByCurveNameIntPtr(int curveId);

        public static SafeEcGroup EcGroupNewByCurveName(int curveId) =>
            new(EcGroupNewByCurveNameIntPtr(curveId), true);

        [DllImport(Libraries.NativeShims, EntryPoint = "Native_EC_GROUP_free", ExactSpelling = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern void EcGroupFree(IntPtr group);

        [DllImport(Libraries.NativeShims, EntryPoint = "Native_EC_POINT_new", ExactSpelling = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        private static extern IntPtr EcPointNewIntPtr(IntPtr ecGroup);

        public static SafeEcPoint EcPointNew(SafeEcGroup ecGroup) =>
            new(EcPointNewIntPtr(ecGroup.DangerousGetHandle()), true);

        [DllImport(Libraries.NativeShims, EntryPoint = "Native_EC_POINT_free", ExactSpelling = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern void EcPointFree(IntPtr ecPoint);

        [DllImport(Libraries.NativeShims, EntryPoint = "Native_EC_POINT_set_affine_coordinates", ExactSpelling = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        private static extern int EcPointSetAffineCoordinatesIntPtr(IntPtr group, IntPtr point, IntPtr x, IntPtr y, IntPtr ctx);

        public static int EcPointSetAffineCoordinates(
            SafeEcGroup group,
            SafeEcPoint point,
            SafeBigNum x,
            SafeBigNum y) =>
            EcPointSetAffineCoordinatesIntPtr(
                group.DangerousGetHandle(),
                point.DangerousGetHandle(),
                x.DangerousGetHandle(),
                y.DangerousGetHandle(),
                IntPtr.Zero);

        [DllImport(Libraries.NativeShims, EntryPoint = "Native_EC_POINT_get_affine_coordinates", ExactSpelling = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        private static extern int EcPointGetAffineCoordinatesIntPtr(IntPtr group, IntPtr point, IntPtr x, IntPtr y, IntPtr ctx);

        public static int EcPointGetAffineCoordinates(
            SafeEcGroup group,
            SafeEcPoint point,
            SafeBigNum x,
            SafeBigNum y) =>
            EcPointGetAffineCoordinatesIntPtr(
                group.DangerousGetHandle(),
                point.DangerousGetHandle(),
                x.DangerousGetHandle(),
                y.DangerousGetHandle(),
                IntPtr.Zero);

        [DllImport(Libraries.NativeShims, EntryPoint = "Native_EC_POINT_mul", ExactSpelling = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        private static extern int EcPointMulIntPtr(IntPtr group, IntPtr r, IntPtr n, IntPtr q, IntPtr m, IntPtr ctx);

        public static int EcPointMul(
            SafeEcGroup group,
            SafeEcPoint r,
            IntPtr n,
            IntPtr q,
            IntPtr m) =>
            EcPointMulIntPtr(
                group.DangerousGetHandle(),
                r.DangerousGetHandle(),
                n,
                q,
                m,
                IntPtr.Zero);

        [DllImport(Libraries.NativeShims, EntryPoint = "Native_EC_POINT_is_on_curve", ExactSpelling = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        private static extern int EcPointIsOnCurveIntPtr(IntPtr group, IntPtr point, IntPtr ctx);

        public static int EcPointIsOnCurve(SafeEcGroup group, SafeEcPoint point) =>
            EcPointIsOnCurveIntPtr(group.DangerousGetHandle(), point.DangerousGetHandle(), IntPtr.Zero);

        [DllImport(Libraries.NativeShims, EntryPoint = "Native_BN_bin2bn", ExactSpelling = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        private static extern IntPtr BnBinaryToBigNumIntPtr(byte[] buffer, int length, IntPtr ret);

        public static SafeBigNum BnBinaryToBigNum(ReadOnlySpan<byte> buffer)
        {
            byte[] bufferArray = buffer.ToArray(); // P/Invoke requires byte[]
            return new SafeBigNum(BnBinaryToBigNumIntPtr(bufferArray, bufferArray.Length, IntPtr.Zero), true);
        }

        [DllImport(Libraries.NativeShims, EntryPoint = "Native_BN_new", ExactSpelling = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        private static extern IntPtr BnNewIntPtr();

        public static SafeBigNum BnNew() => new(BnNewIntPtr(), true);

        [DllImport(Libraries.NativeShims, EntryPoint = "Native_BN_clear_free", ExactSpelling = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern void BnClearFree(IntPtr bignum);

        [DllImport(Libraries.NativeShims, EntryPoint = "Native_BN_bn2binpad", ExactSpelling = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        private static extern int BnBigNumToBinaryWithPaddingIntPtr(IntPtr bignum, byte[] buffer, int bufferSize);

        public static int BnBigNumToBinaryWithPadding(SafeBigNum bigNum, Span<byte> buffer)
        {
            byte[] bufferArray = buffer.ToArray(); // P/Invoke requires byte[]
            int result = BnBigNumToBinaryWithPaddingIntPtr(bigNum.DangerousGetHandle(), bufferArray, bufferArray.Length);
            bufferArray.CopyTo(buffer);
            return result;
        }
    }

    private sealed class SafeEcGroup : SafeHandle
    {
        public SafeEcGroup() : base(IntPtr.Zero, true) { }

        public SafeEcGroup(IntPtr handle, bool ownsHandle) : base(handle, ownsHandle) { }

        protected override bool ReleaseHandle()
        {
            if (!IsInvalid)
            {
                NativeMethods.EcGroupFree(handle);
            }

            return true;
        }

        public override bool IsInvalid => handle == IntPtr.Zero;
    }

    private sealed class SafeEcPoint : SafeHandle
    {
        public SafeEcPoint() : base(IntPtr.Zero, true) { }

        public SafeEcPoint(IntPtr handle, bool ownsHandle) : base(handle, ownsHandle) { }

        protected override bool ReleaseHandle()
        {
            if (!IsInvalid)
            {
                NativeMethods.EcPointFree(handle);
            }

            return true;
        }

        public override bool IsInvalid => handle == IntPtr.Zero;
    }

    private sealed class SafeBigNum : SafeHandle
    {
        public SafeBigNum() : base(IntPtr.Zero, true) { }

        public SafeBigNum(IntPtr handle, bool ownsHandle) : base(handle, ownsHandle) { }

        protected override bool ReleaseHandle()
        {
            if (!IsInvalid)
            {
                NativeMethods.BnClearFree(handle);
            }

            return true;
        }

        public override bool IsInvalid => handle == IntPtr.Zero;
    }
}