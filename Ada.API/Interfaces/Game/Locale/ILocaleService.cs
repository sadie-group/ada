namespace Ada.API.Interfaces.Game.Locale;

public interface ILocaleService
{
    string this[string key] { get; set; }
}