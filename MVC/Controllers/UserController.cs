using Microsoft.AspNetCore.Mvc;
using MVC.Context;
using MVC.Models;
using MVC.Services;
using MVC.Services.Interfaces;


namespace MVC.Controllers
{
    public class UserController : Controller
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost]
        //[Route("register")]
        public async Task<IActionResult> RegisterUser(UserRegisterModel userRegisterModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenResponse = await _userService.RegistrationUser(userRegisterModel);
                    if (tokenResponse != null)
                    {
                        // Регистрация выполнена успешно, сохраняем токен в сессии или куки
                        HttpContext.Session.SetString("Token", tokenResponse.Token);
                        // Перенаправляем пользователя на страницу Index в контроллере Home
                        return RedirectToAction("Index", "Home");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message); // Добавляем ошибку в ModelState
                }
            }

            // Если регистрация не выполнена или возникла ошибка, возвращаем представление Register с сообщением об ошибке
            return View("Register");
        }

        [HttpPost]
        //[Route("login")]
        public async Task<IActionResult> Login([FromBody] LoginCredentials credentials) //TokenResponse
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tokenResponse = await _userService.LoginUser(credentials);

                    if (tokenResponse != null)
                    {
                        // Вход выполнен успешно, сохраняем токен в сессии или куки
                        HttpContext.Session.SetString("Token", tokenResponse.Token);
                        // Перенаправляем пользователя на страницу Index в контроллере Home
                        return RedirectToAction("Index", "Home");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message); // Добавляем ошибку в ModelState
                }
            }

            // Если вход не выполнен, возвращаем представление с ошибкой
            return View(credentials);
        }

        public IActionResult Login()
        {
            return View();
        }

        public IActionResult Register()
        {
            return View();
        }
    }
}
