namespace PasswordManager.Models;

public enum ItemType
{
    Login = 1,
    CreditCard = 2,
    SecureNote = 3,
    WiFi = 4,
    Password = 5,
    Passkey = 6,
    Identity = 7,
    SshKey = 8,
    BankAccount = 9,
    Database = 10,
    DriversLicense = 11,
    EmailAccount = 12,
    MedicalRecord = 13,
    Membership = 14,
    OutdoorLicense = 15,
    Passport = 16,
    RewardsProgram = 17,
    Server = 18,
    SocialSecurityNumber = 19,
    SoftwareLicense = 20,
    WirelessRouter = 21,
    CryptoWallet = 22,
    Document = 23,
    ApiCredentials = 24,
}

public enum SecurityType
{
    WEP,
    WPA,
    WPA2,
    WPA3,
    Open
}

public enum CardType
{
    Visa,
    MasterCard,
    AmericanExpress,
    Discover,
    DinersClub,
    JCB,
    Other
}

public enum FrequencyType
{
    TwoPointFourGHz,
    FiveGHz,
    SixGHz
}
