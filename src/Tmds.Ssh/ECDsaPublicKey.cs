// This file is part of Tmds.Ssh which is released under MIT.
// See file LICENSE for full license details.

using System.Buffers;
using System.Numerics;
using System.Security.Cryptography;
using System.Formats.Asn1;

namespace Tmds.Ssh;

class ECDsaPublicKey : PublicKey
{
    private readonly Name _name;
    private readonly ECCurve _curve;
    private readonly ECPoint _q;
    private readonly HashAlgorithmName _hashAlgorithm;

    private ECDsaPublicKey(Name name, ECCurve curve, ECPoint q, HashAlgorithmName hashAlgorithm)
    {
        _q = q;
        _curve = curve;
        _name = name;
        _hashAlgorithm = hashAlgorithm;
    }

    public static ECDsaPublicKey CreateFromSshKey(byte[] key)
    {
        SequenceReader reader = new SequenceReader(key);
        var name = reader.ReadName();
        if (name == AlgorithmNames.EcdsaSha2Nistp256)
        {
            reader.ReadName(AlgorithmNames.Nistp256);
            ECPoint q = reader.ReadStringAsECPoint();
            reader.ReadEnd();
            return new ECDsaPublicKey(AlgorithmNames.EcdsaSha2Nistp256, ECCurve.NamedCurves.nistP256, q, HashAlgorithmName.SHA256);
        }
        else if (name == AlgorithmNames.EcdsaSha2Nistp384)
        {
            reader.ReadName(AlgorithmNames.Nistp384);
            ECPoint q = reader.ReadStringAsECPoint();
            reader.ReadEnd();
            return new ECDsaPublicKey(AlgorithmNames.EcdsaSha2Nistp384, ECCurve.NamedCurves.nistP384, q, HashAlgorithmName.SHA384);
        }
        else if (name == AlgorithmNames.EcdsaSha2Nistp521)
        {
            reader.ReadName(AlgorithmNames.Nistp521);
            ECPoint q = reader.ReadStringAsECPoint();
            reader.ReadEnd();
            return new ECDsaPublicKey(AlgorithmNames.EcdsaSha2Nistp521, ECCurve.NamedCurves.nistP521, q, HashAlgorithmName.SHA512);
        }
        ThrowHelper.ThrowProtocolUnexpectedValue();
        return null!;
    }

    internal override bool VerifySignature(IReadOnlyList<Name> allowedAlgorithms, Span<byte> data, ReadOnlySequence<byte> signature)
    {
        var reader = new SequenceReader(signature);
        reader.ReadName(_name, allowedAlgorithms);
        ReadOnlySequence<byte> ecdsa_signature_blob = reader.ReadStringAsBytes();
        reader.ReadEnd();
        reader = new SequenceReader(ecdsa_signature_blob);
        BigInteger r = reader.ReadMPInt();
        BigInteger s = reader.ReadMPInt();
        reader.ReadEnd();

        using ECDsa key = ECDsa.Create(new ECParameters
        {
            Curve = _curve,
            Q = _q
        });

        AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
        writer.PushSequence();
        writer.WriteInteger(r);
        writer.WriteInteger(s);
        writer.PopSequence();
        byte[] signatureData = writer.Encode();

        return key.VerifyData(data, signatureData, _hashAlgorithm, DSASignatureFormat.Rfc3279DerSequence);
    }
}
