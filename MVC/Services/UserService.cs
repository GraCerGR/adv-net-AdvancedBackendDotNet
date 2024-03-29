using MVC.Context;
using MVC.Models;
using MVC.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace MVC.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationContext _context;

        public UserService(ApplicationContext context)
        {
            _context = context;
        }


        public async Task<TokenResponse> RegistrationUser(UserRegisterModel userRegisterModel)
        {

            CheckEmail(userRegisterModel);
            CheckGender(userRegisterModel.Gender);
            CheckBirthdate(userRegisterModel.BirthDate);

            byte[] salt;
            RandomNumberGenerator.Create().GetBytes(salt = new byte[16]);
            var pbkdf2 = new Rfc2898DeriveBytes(userRegisterModel.Password, salt, 100000);
            var hash = pbkdf2.GetBytes(20);
            var hashBytes = new byte[36];
            Array.Copy(salt, 0, hashBytes, 0, 16);
            Array.Copy(hash, 0, hashBytes, 16, 20);
            var savedPasswordHash = Convert.ToBase64String(hashBytes);

            UserModel user = new UserModel 
            {
                Id = Guid.NewGuid(),
                Name = userRegisterModel.Name,
                Email = userRegisterModel.Email,
                Password = savedPasswordHash,
                Birthdate = userRegisterModel.BirthDate,
                Gender = userRegisterModel.Gender,
                PhoneNumber = userRegisterModel.PhoneNumber,
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var credentials = new LoginCredentials
            {
                Email = userRegisterModel.Email,
                Password = userRegisterModel.Password
            };

            return await LoginUser(credentials);
        }


        public async Task<TokenResponse> LoginUser(LoginCredentials credentials)
        {

            // Генерация токена
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes("1234567890123456789012345678901234567890");
            var tokenDescriptor = new SecurityTokenDescriptor()
            {
                NotBefore = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = "HITS",
                Audience = "HITS",
                Subject = new ClaimsIdentity(new Claim[]
                {
            new Claim(ClaimTypes.Name, credentials.Email.ToString())
                })
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            var result = new TokenResponse()
            {
                Token = tokenString
            };

            return result;
        }


        private async Task CheckEmail(UserRegisterModel userRegisterModel)
        {
            var email = await _context.Users.Where(x => userRegisterModel.Email == x.Email).FirstOrDefaultAsync();

            if (email != null)
            {
                var ex = new Exception();
                ex.Data.Add(StatusCodes.Status409Conflict.ToString(), $"Account with email '{userRegisterModel.Email}' already exists");
                throw ex;
            }
        }

        private static void CheckGender(string gender)
        {
            if (gender == Gender.Male.ToString() || gender == Gender.Female.ToString()) return;

            var ex = new Exception();
            ex.Data.Add(StatusCodes.Status409Conflict.ToString(), $"Possible Gender values: {Gender.Male.ToString()}, {Gender.Female.ToString()}");
            throw ex;
        }

        private static void CheckBirthdate(DateTime? birthDate)
        {
            if (birthDate == null || birthDate <= DateTime.Now) return;

            var ex = new Exception();
            ex.Data.Add(StatusCodes.Status409Conflict.ToString(),"Birth date can't be later than today");
            throw ex;
        }
    }
}
