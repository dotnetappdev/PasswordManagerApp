namespace PasswordManager.Models;

public enum ItemType
{
    Login = 1,
    CreditCard = 2,
    SecureNote = 3,
    WiFi = 4,
    Password = 5,
    Passkey = 6
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
