using Microsoft.AspNetCore.Mvc;
using MVC.Context;
using MVC.Models;
using MVC.Services.Interfaces;


namespace MVC.Controllers
{
    public class UserController : ControllerBase
    {
        private readonly IUserService _usersService;

        public UserController(IUserService usersService)
        {
            _usersService = usersService;
        }

        [HttpPost]
        [Route("register")]
        public async Task<TokenResponse> RegisterUser([FromBody] UserRegisterModel userRegisterModel)
        {
            return await _usersService.RegistrationUser(userRegisterModel);
        }

        [HttpPost]
        [Route("login")]
        public async Task<TokenResponse> Login([FromBody] LoginCredentials credentials)
        {
            return await _usersService.LoginUser(credentials);
        }
    }
}
