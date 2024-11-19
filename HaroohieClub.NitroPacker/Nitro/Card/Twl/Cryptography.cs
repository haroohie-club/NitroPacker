using HaroohieClub.NitroPacker.IO.Serialization;

namespace HaroohieClub.NitroPacker.Nitro.Card.Twl;

/// <summary>
/// [DSi] Representation of the hashes/encryption section of the DSi header
/// </summary>
public class Cryptography
{
    /// <summary>
    /// SHA1-HMAC hash ARM9 (with encrypted secure area)
    /// </summary>
    [ArraySize(0x14)]
    public byte[] Arm9WithSecureAreaSha1HmacHash { get; set; }
    /// <summary>
    /// SHA1-HMAC hash ARM7
    /// </summary>
    [ArraySize(0x14)]
    public byte[] Arm7Sha1HmacHash { get; set; }
    /// <summary>
    /// SHA1-HMAC hash Digest master
    /// </summary>
    [ArraySize(0x14)]
    public byte[] DigestMasterSha1HmacHash { get; set; }
    /// <summary>
    /// SHA1-HMAC hash Icon/Title (also in newer NDS titles)
    /// </summary>
    [ArraySize(0x14)]
    public byte[] IconTitleSha1HmacHash { get; set; }
    /// <summary>
    /// SHA1-HMAC hash ARM9i (decrypted)
    /// </summary>
    [ArraySize(0x14)]
    public byte[] Arm9iDecryptedSha1HmacHash { get; set; }
    /// <summary>
    /// SHA1-HMAC hash ARM7i (decrypted)
    /// </summary>
    [ArraySize(0x14)]
    public byte[] Arm7iDecryptedSha1HmacHash { get; set; }
    /// <summary>
    /// Reserved (zero-filled) (but used for non-whitelisted NDS titles)
    /// </summary>
    [ArraySize(0x14)]
    public byte[] ReservedA { get; set; }
    /// <summary>
    /// Reserved (zero-filled) (but used for non-whitelisted NDS titles)
    /// </summary>
    [ArraySize(0x14)]
    public byte[] ReservedB { get; set; }
    /// <summary>
    /// SHA1-HMAC hash ARM9 (without 16Kbyte secure area)
    /// </summary>
    [ArraySize(0x14)]
    public byte[] Arm9WithoutSecureAreaSha1HmacHash { get; set; }
    /// <summary>
    /// Reserved (zero-filled)
    /// </summary>
    [ArraySize(0xA4C)]
    public byte[] ReservedC { get; set; }
    /// <summary>
    /// Reserved and unchecked region, always zero. Used for passing arguments in debug environment.
    /// </summary>
    [ArraySize(0x180)]
    public byte[] ReservedD { get; set; }
    /// <summary>
    /// RSA-SHA1 signature across header entries [0x000..0xDFF]
    /// </summary>
    [ArraySize(0x80)]
    public byte[] RsaSha1Signature { get; set; }
}