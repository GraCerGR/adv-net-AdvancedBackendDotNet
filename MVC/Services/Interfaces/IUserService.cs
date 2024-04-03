using MVC.Models;
using MVC.Models.DTO;

namespace MVC.Services.Interfaces
{
    public interface IUserService
    {
        Task<TokenResponse> RegistrationUser(UserRegisterModel userRegisterModel);

        Task<TokenResponse> LoginUser(LoginCredentials credentials);


 //       Task LogoutUser(string token);
        //Task EditUserProfile(Guid guid, UserEditDto userEditDto);
        //Task<TokenResponse> RegisterUser(UserRegisterDto userRegisterDto);
        //Task<TokenResponse> LoginUser(LoginDto credentials);

        Task<UserDto> GetProfile(string guid);

        Task<string> GetUserIdFromToken(string token);
    }
}
