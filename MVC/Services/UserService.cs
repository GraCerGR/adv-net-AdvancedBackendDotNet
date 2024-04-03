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
using MVC.Models.DTO;

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
            CheckBirthdate(userRegisterModel.Birthdate);

            byte[] salt;
            RandomNumberGenerator.Create().GetBytes(salt = new byte[16]);
            var pbkdf2 = new Rfc2898DeriveBytes(userRegisterModel.Password, salt, 100000);
            var hash = pbkdf2.GetBytes(20);
            var hashBytes = new byte[36];
            Array.Copy(salt, 0, hashBytes, 0, 16);
            Array.Copy(hash, 0, hashBytes, 16, 20);
            var savedPasswordHash = Convert.ToBase64String(hashBytes);

            if (userRegisterModel.Birthdate.HasValue)
            {
                userRegisterModel.Birthdate = userRegisterModel.Birthdate.Value.ToUniversalTime();
            }

            UserModel user = new UserModel 
            {
                Id = Guid.NewGuid(),
                Name = userRegisterModel.Name,
                Email = userRegisterModel.Email,
                Password = savedPasswordHash,
                Birthdate = userRegisterModel.Birthdate,
                Gender = userRegisterModel.Gender,
                PhoneNumber = userRegisterModel.PhoneNumber,
            };

            try
            {
                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                var innerException = ex.InnerException;
                while (innerException != null)
                {
                    Console.WriteLine("Inner Exception: " + innerException.Message);
                    innerException = innerException.InnerException;
                }
                // Дополнительная обработка ошибки, логирование и т.д.
            }

            //await _context.Users.AddAsync(user);
            //await _context.SaveChangesAsync();

            var credentials = new LoginCredentials
            {
                Email = userRegisterModel.Email,
                Password = userRegisterModel.Password
            };

            return await LoginUser(credentials);
        }


        public async Task<TokenResponse> LoginUser(LoginCredentials credentials)
        {

            var userEntity = await _context.Users.FirstOrDefaultAsync(x => x.Email == credentials.Email);

            if (userEntity == null)
            {
                var ex = new Exception();
                ex.Data.Add(StatusCodes.Status401Unauthorized.ToString(),
                    "User not exists"
                );
                throw ex;
            }

            if (!CheckHashPassword(userEntity.Password, credentials.Password))
            {
                var ex = new Exception();
                ex.Data.Add(StatusCodes.Status401Unauthorized.ToString(),
                    "Wrong password"
                );
                throw ex;
            }

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
            new Claim(ClaimTypes.Name, userEntity.Id.ToString())
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


        public async Task<UserDto> GetProfile(string userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id.ToString() == userId);

            if (user == null)
            {
                return null;
            }

            var userProfile = new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Birthdate = user.Birthdate,
                Gender = user.Gender,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Citizenship = user.Citizenship
            };

            return userProfile;
        }

        public async Task<string> GetUserIdFromToken(string bearerToken)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(bearerToken);
            return await Task.FromResult(jwtToken.Claims.FirstOrDefault(c => c.Type == "unique_name")?.Value);
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
            if (gender == null || gender == Gender.Male.ToString() || gender == Gender.Female.ToString()) return;

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

        private static bool CheckHashPassword(string savedPasswordHash, string password)
        {
            var hashBytes = Convert.FromBase64String(savedPasswordHash);
            var salt = new byte[16];
            Array.Copy(hashBytes, 0, salt, 0, 16);
            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000);
            var hash = pbkdf2.GetBytes(20);
            for (var i = 0; i < 20; i++)
                if (hashBytes[i + 16] != hash[i])
                    return false;
            return true;
        }
    }
}
