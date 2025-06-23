namespace WaterDelivery.Application.Interfaces
{
    public interface IPasswordService
    {
        string GenerateSalt();
        string HashPassword(string password, string salt);
        bool VerifyPassword(string password, string hash, string salt);
        bool IsValidPassword(string password);
    }
}