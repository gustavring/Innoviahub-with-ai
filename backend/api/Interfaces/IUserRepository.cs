using api.Models;

namespace api.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByIdAsync(string id);

        Task<bool> CheckPasswordAsync(User user, string password);

        Task<bool> CreateUserAsync(User user, string password);
        Task<List<User>> GetAllAsync();
        Task<IList<string>> GetRolesAsync(User user);
        Task<bool> DeleteUserAsync(User user);
    }
}
