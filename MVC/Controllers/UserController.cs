using System.IdentityModel.Tokens.Jwt;
using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MVC.Context;
using MVC.Models;
using MVC.Services;
using MVC.Services.Interfaces;
using Microsoft.EntityFrameworkCore;


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
        public async Task<IActionResult> LoginUser(LoginCredentials credentials) //TokenResponse
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
            return View("Login");
        }


        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                // Получаем значение заголовка "Authorization"
                //string authorizationHeader = Request.Headers["Authorization"];
                // Извлекаем токен Bearer из значения заголовка
                //string bearerToken = authorizationHeader.Substring("Bearer ".Length);
                string bearerToken = HttpContext.Session.GetString("Token");

                var userId = await _userService.GetUserIdFromToken(bearerToken);

                var userProfile = await _userService.GetProfile(userId);

                if (userProfile == null)
                {
                    return NotFound();
                }

                return Ok(userProfile);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }


        [HttpGet("{userApplicantId}")]
        [Authorize]
        public async Task<IActionResult> GetProfileById(string userApplicantId)
        {
            try
            {
                // Получаем значение заголовка "Authorization"
                //string authorizationHeader = Request.Headers["Authorization"];
                // Извлекаем токен Bearer из значения заголовка
                //string bearerToken = authorizationHeader.Substring("Bearer ".Length);
                string bearerToken = HttpContext.Session.GetString("Token");

                var userId = await _userService.GetUserIdFromToken(bearerToken);

                var userProfile = await _userService.GetProfile(userId);

                var userApplicantProfile = await _userService.GetProfile(userApplicantId);

                if (userProfile == null || userApplicantProfile == null)
                {
                    return NotFound();
                }

                //Проверка: UserId менеджер абитуриента userApplicantId

                return Ok(userApplicantProfile);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
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
