namespace HaroohieClub.NitroPacker.Nitro.Card;

/// <summary>
/// A struct representing regional ratings for Japan
/// </summary>
public struct JapanRegionRating
{
    /// <summary>
    /// Boolean value indicating whether a rating exists for this region
    /// </summary>
    public bool RatingExists { get; set; }
    /// <summary>
    /// Boolean value indicating whether the game has been banned in this region
    /// </summary>
    public bool GameProhibited { get; set; }
    /// <summary>
    /// The CERO rating for the game
    /// </summary>
    public CeroRating Rating { get; set; }
    
    /// <summary>
    /// Constructs a rating for the Japan region
    /// </summary>
    /// <param name="rating">The packed byte representation of the rating</param>
    public JapanRegionRating(byte rating)
    {
        RatingExists = (rating & (1 << 7)) != 0;
        GameProhibited = (rating & (1 << 6)) != 0;
        Rating = (CeroRating)(rating & 0b0001_1111);
    }

    /// <summary>
    /// Packs the rating and flags into a byte
    /// </summary>
    /// <returns>A byte representation of the rating data</returns>
    public byte Pack()
    {
        return (byte)((RatingExists ? (1 << 7) : 0) | (GameProhibited ? (1 << 6) : 0) | (byte)Rating);
    }

    /// <summary>
    /// Ratings from Computer Entertainment Rating Organization (CERO) for Japan
    /// </summary>
    public enum CeroRating : byte
    {
        /// <summary>
        /// A (All Ages) or no rating
        /// </summary>
        ANone = 0,
        /// <summary>
        /// B (Ages 12 and up)
        /// </summary>
        B = 12,
        /// <summary>
        /// C (Ages 15 and up)
        /// </summary>
        C = 15,
        /// <summary>
        /// D (Ages 17 and up)
        /// </summary>
        D = 17,
        /// <summary>
        /// Z (Ages 18 and up only)
        /// </summary>
        Z = 18,
    }
}

/// <summary>
/// A struct representing regional ratings for the USA and Canada
/// </summary>
public struct USCanadaRegionRating
{
    /// <summary>
    /// Boolean value indicating whether a rating exists for this region
    /// </summary>
    public bool RatingExists { get; set; }
    /// <summary>
    /// Boolean value indicating whether the game has been banned in this region
    /// </summary>
    public bool GameProhibited { get; set; }
    /// <summary>
    /// The ESRB rating for the game
    /// </summary>
    public EsrbRating Rating { get; set; }
    
    
    /// <summary>
    /// Constructs a rating for the US/Canada region
    /// </summary>
    /// <param name="rating">The packed byte representation of the rating</param>
    public USCanadaRegionRating(byte rating)
    {
        RatingExists = (rating & (1 << 7)) != 0;
        GameProhibited = (rating & (1 << 6)) != 0;
        Rating = (EsrbRating)(rating & 0b0001_1111);
    }

    /// <summary>
    /// Packs the rating and flags into a byte
    /// </summary>
    /// <returns>A byte representation of the rating data</returns>
    public byte Pack()
    {
        return (byte)((RatingExists ? (1 << 7) : 0) | (GameProhibited ? (1 << 6) : 0) | (byte)Rating);
    }
    
    /// <summary>
    /// Ratings from the Entertainment Software Rating Board (ESRB) for the USA/Canada
    /// </summary>
    public enum EsrbRating : byte
    {
        /// <summary>
        /// No Rating
        /// </summary>
        None = 0,
        /// <summary>
        /// eC (Early Childhood)
        /// </summary>
        EC = 3,
        /// <summary>
        /// E (Everyone)
        /// </summary>
        E = 6,
        /// <summary>
        /// E10+ (Everyone 10+)
        /// </summary>
        E10Plus = 10,
        /// <summary>
        /// T (Teen)
        /// </summary>
        T = 13,
        /// <summary>
        /// M (Mature)
        /// </summary>
        M = 17,
        /// <summary>
        /// AO (Adults Only) (unused in any retail games it seems)
        /// </summary>
        AO = 18,
    }
}

/// <summary>
/// A struct representing regional ratings for Germany
/// </summary>
public struct GermanyRegionRating
{
    /// <summary>
    /// Boolean value indicating whether a rating exists for this region
    /// </summary>
    public bool RatingExists { get; set; }
    /// <summary>
    /// Boolean value indicating whether the game has been banned in this region
    /// </summary>
    public bool GameProhibited { get; set; }
    /// <summary>
    /// The USK rating for the game
    /// </summary>
    public UskRating Rating { get; set; }
    
    /// <summary>
    /// Constructs a rating for the Germany region
    /// </summary>
    /// <param name="rating">The packed byte representation of the rating</param>
    public GermanyRegionRating(byte rating)
    {
        RatingExists = (rating & (1 << 7)) != 0;
        GameProhibited = (rating & (1 << 6)) != 0;
        Rating = (UskRating)(rating & 0b0001_1111);
    }

    /// <summary>
    /// Packs the rating and flags into a byte
    /// </summary>
    /// <returns>A byte representation of the rating data</returns>
    public byte Pack()
    {
        return (byte)((RatingExists ? (1 << 7) : 0) | (GameProhibited ? (1 << 6) : 0) | (byte)Rating);
    }
    
    /// <summary>
    /// Ratings from Unterhaltungssoftware Selbstkontrolle (USK) for Germany
    /// </summary>
    public enum UskRating : byte
    {
        /// <summary>
        /// 0 (Approved without age restriction) or no rating
        /// </summary>
        None = 0,
        /// <summary>
        /// 6 (Approved for children aged 6 and above)
        /// </summary>
        SixPlus = 6,
        /// <summary>
        /// 12 (Approved for children aged 12 and above)
        /// </summary>
        TwelvePlus = 12,
        /// <summary>
        /// 16 (Approved for children aged 16 and above)
        /// </summary>
        SixteenPlus = 16,
        /// <summary>
        /// 18 (Not approved for young persons)
        /// </summary>
        EighteenPlus = 18,
    }
}

/// <summary>
/// A struct representing regional ratings for the European region
/// </summary>
public struct EuropeanRegionRating
{
    /// <summary>
    /// Boolean value indicating whether a rating exists for this region
    /// </summary>
    public bool RatingExists { get; set; }
    /// <summary>
    /// Boolean value indicating whether the game has been banned in this region
    /// </summary>
    public bool GameProhibited { get; set; }
    /// <summary>
    /// The PEGI (Pan-European) rating for the game
    /// </summary>
    public PegiEuropeRating Rating { get; set; }
    
    /// <summary>
    /// Constructs a rating for the European region
    /// </summary>
    /// <param name="rating">The packed byte representation of the rating</param>
    public EuropeanRegionRating(byte rating)
    {
        RatingExists = (rating & (1 << 7)) != 0;
        GameProhibited = (rating & (1 << 6)) != 0;
        Rating = (PegiEuropeRating)(rating & 0b0001_1111);
    }

    /// <summary>
    /// Packs the rating and flags into a byte
    /// </summary>
    /// <returns>A byte representation of the rating data</returns>
    public byte Pack()
    {
        return (byte)((RatingExists ? (1 << 7) : 0) | (GameProhibited ? (1 << 6) : 0) | (byte)Rating);
    }
    
    /// <summary>
    /// Ratings from the Pan-European Game Information (PEGI) for all of Europe
    /// </summary>
    public enum PegiEuropeRating : byte
    {
        /// <summary>
        /// No rating
        /// </summary>
        None = 0,
        /// <summary>
        /// PEGI 3
        /// </summary>
        Three = 3,
        /// <summary>
        /// PEGI 7
        /// </summary>
        Seven = 7,
        /// <summary>
        /// PEGI 12
        /// </summary>
        Twelve = 12,
        /// <summary>
        /// PEGI 16
        /// </summary>
        Sixteen = 16,
        /// <summary>
        /// PEGI 18
        /// </summary>
        Eighteen = 18,
    }
}

/// <summary>
/// A struct representing regional ratings for the Portugal region
/// </summary>
public struct PortugalRegionRating
{
    /// <summary>
    /// Boolean value indicating whether a rating exists for this region
    /// </summary>
    public bool RatingExists { get; set; }
    /// <summary>
    /// Boolean value indicating whether the game has been banned in this region
    /// </summary>
    public bool GameProhibited { get; set; }
    /// <summary>
    /// The PEGI (Portugal) rating for the game
    /// </summary>
    public PegiPortugalRating Rating { get; set; }
    
    /// <summary>
    /// Constructs a rating for the Portugal region
    /// </summary>
    /// <param name="rating">The packed byte representation of the rating</param>
    public PortugalRegionRating(byte rating)
    {
        RatingExists = (rating & (1 << 7)) != 0;
        GameProhibited = (rating & (1 << 6)) != 0;
        Rating = (PegiPortugalRating)(rating & 0b0001_1111);
    }

    /// <summary>
    /// Packs the rating and flags into a byte
    /// </summary>
    /// <returns>A byte representation of the rating data</returns>
    public byte Pack()
    {
        return (byte)((RatingExists ? (1 << 7) : 0) | (GameProhibited ? (1 << 6) : 0) | (byte)Rating);
    }
    
    /// <summary>
    /// Ratings from the Pan-European Game Information (PEGI) specifically for Portugal
    /// </summary>
    public enum PegiPortugalRating : byte
    {
        /// <summary>
        /// No rating
        /// </summary>
        None,
        /// <summary>
        /// PEGI 4
        /// </summary>
        Four,
        /// <summary>
        /// PEGI 6
        /// </summary>
        Six,
        /// <summary>
        /// PEGI 12
        /// </summary>
        Twelve,
        /// <summary>
        /// PEGI 16
        /// </summary>
        Sixteen,
        /// <summary>
        /// PEGI 18
        /// </summary>
        Eighteen,
    }
}

/// <summary>
/// A struct representing the regional rating for the United Kingdom region
/// </summary>
public struct UnitedKingdomRegionRating
{
    /// <summary>
    /// Boolean value indicating whether a rating exists for this region
    /// </summary>
    public bool RatingExists { get; set; }
    /// <summary>
    /// Boolean value indicating whether the game has been banned in this region
    /// </summary>
    public bool GameProhibited { get; set; }
    /// <summary>
    /// The PEGI/BBFC rating for the game
    /// </summary>
    public PegiBbfcRating Rating { get; set; }
    
    /// <summary>
    /// Constructs a rating for the UK region
    /// </summary>
    /// <param name="rating">The packed byte representation of the rating</param>
    public UnitedKingdomRegionRating(byte rating)
    {
        RatingExists = (rating & (1 << 7)) != 0;
        GameProhibited = (rating & (1 << 6)) != 0;
        Rating = (PegiBbfcRating)(rating & 0b0001_1111);
    }

    /// <summary>
    /// Packs the rating and flags into a byte
    /// </summary>
    /// <returns>A byte representation of the rating data</returns>
    public byte Pack()
    {
        return (byte)((RatingExists ? (1 << 7) : 0) | (GameProhibited ? (1 << 6) : 0) | (byte)Rating);
    }
    
    /// <summary>
    /// Ratings from the Pan-European Game Information (PEGI) and the
    /// British Board of Film Classification (BBFC) for the United Kingdom
    /// </summary>
    public enum PegiBbfcRating : byte
    {
        /// <summary>
        ///  No rating
        /// </summary>
        None = 0,
        /// <summary>
        /// PEGI 3
        /// </summary>
        Three = 3,
        /// <summary>
        /// BBFC U (Universal)
        /// </summary>
        U = 4,
        /// <summary>
        /// PEGI 7
        /// </summary>
        Seven = 7,
        /// <summary>
        /// BBFC PG (Parental Guidance)
        /// </summary>
        PG = 8,
        /// <summary>
        /// PEGI/BBFC 12
        /// </summary>
        Twelve = 12,
        /// <summary>
        /// BBFC 15
        /// </summary>
        Fifteen = 15,
        /// <summary>
        /// PEGI 16
        /// </summary>
        Sixteen = 16,
        /// <summary>
        /// PEGI/BBFC 18
        /// </summary>
        Eighteen = 18,
    }
}

/// <summary>
/// A struct representing the regional rating for the Australia region
/// </summary>
public struct AustraliaRegionRating
{
    /// <summary>
    /// Boolean value indicating whether a rating exists for this region
    /// </summary>
    public bool RatingExists { get; set; }
    /// <summary>
    /// Boolean value indicating whether the game has been banned in this region
    /// </summary>
    public bool GameProhibited { get; set; }
    /// <summary>
    /// The ACB rating for the game
    /// </summary>
    public AcbRating Rating { get; set; }
    
    /// <summary>
    /// Constructs a rating for the Australia region
    /// </summary>
    /// <param name="rating">The packed byte representation of the rating</param>
    public AustraliaRegionRating(byte rating)
    {
        RatingExists = (rating & (1 << 7)) != 0;
        GameProhibited = (rating & (1 << 6)) != 0;
        Rating = (AcbRating)(rating & 0b0001_1111);
    }

    /// <summary>
    /// Packs the rating and flags into a byte
    /// </summary>
    /// <returns>A byte representation of the rating data</returns>
    public byte Pack()
    {
        return (byte)((RatingExists ? (1 << 7) : 0) | (GameProhibited ? (1 << 6) : 0) | (byte)Rating);
    }
    
    /// <summary>
    /// Ratings from the Australian Classification Board (ACB) for Australia
    /// </summary>
    public enum AcbRating : byte
    {
        /// <summary>
        /// None / G (General; Suitable for all ages)
        /// </summary>
        NoneG = 0,
        /// <summary>
        /// G 8+ (General; Suitable for children 8 years and over)
        /// </summary>
        G8Plus = 7,
        /// <summary>
        /// M 15+ (Mature; Suitable for children 15 years and over)
        /// </summary>
        M15Plus = 14,
        /// <summary>
        /// MA 15+ (Mature - Restricted; Restricted to persons 15 years and over)
        /// </summary>
        MA15Plus = 15,
    }
}

/// <summary>
/// Age rating for the South Korea region
/// </summary>
public struct SouthKoreaRegionRating
{
    /// <summary>
    /// Boolean value indicating whether a rating exists for this region
    /// </summary>
    public bool RatingExists { get; set; }
    /// <summary>
    /// Boolean value indicating whether the game has been banned in this region
    /// </summary>
    public bool GameProhibited { get; set; }
    /// <summary>
    /// The rating from the GRB for the game
    /// </summary>
    public GrbRating Rating { get; set; }
    
    /// <summary>
    /// Constructs a rating for the South Korean region
    /// </summary>
    /// <param name="rating">The packed byte representation of the rating</param>
    public SouthKoreaRegionRating(byte rating)
    {
        RatingExists = (rating & (1 << 7)) != 0;
        GameProhibited = (rating & (1 << 6)) != 0;
        Rating = (GrbRating)(rating & 0b0001_1111);
    }

    /// <summary>
    /// Packs the rating and flags into a byte
    /// </summary>
    /// <returns>A byte representation of the rating data</returns>
    public byte Pack()
    {
        return (byte)((RatingExists ? (1 << 7) : 0) | (GameProhibited ? (1 << 6) : 0) | (byte)Rating);
    }
    
    /// <summary>
    /// Ratings from the GRB (Game Rating Board) for South Korea
    /// </summary>
    public enum GrbRating : byte
    {
        /// <summary>
        /// No rating or All (may be suitable for all ages)
        /// </summary>
        NoneAll = 0,
        /// <summary>
        /// 12 (may be suitable for ages 12 and older)
        /// </summary>
        Twelve = 12,
        /// <summary>
        /// 15 (may be suitable for ages 15 and older)
        /// </summary>
        Fifteen = 15,
        /// <summary>
        /// 18 (restricted for ages 18 and older)
        /// </summary>
        Eighteen = 18
    }
}