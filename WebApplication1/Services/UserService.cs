﻿using WebApplication1.Context;
using WebApplication1.Models;
using WebApplication1.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models.DTO;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace WebApplication1.Services
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

            var savedPasswordHash = await HashingPassword(userRegisterModel.Password);

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

/*            if (userEntity == null)
            {
                var ex = new Exception();
                ex.Data.Add(StatusCodes.Status401Unauthorized.ToString(),
                    "User not exists"
                );
                throw ex;
            }*/

            if (!CheckHashPassword(userEntity.Password, credentials.Password))
            {
                var ex = new Exception();
                ex.Data.Add(StatusCodes.Status401Unauthorized.ToString(),"Wrong password");
                throw ex;
            }

            // Роли

        string role = "Applicant";

        var manager = await _context.Managers.FirstOrDefaultAsync(x => x.UserId == userEntity.Id);

            if (manager != null)
            {
                if (manager.MainManager == false)
                {
                    role = "Manager";
                }
                else
                {
                    role = "MainManager";
                }
            }

/*            if (await _context.Admins.FirstOrDefaultAsync(x => x.UserId == userEntity.Id) != null)
            {
                role = "Admin";
            }*/


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
            new Claim(ClaimTypes.Name, userEntity.Id.ToString()),
            new Claim(ClaimTypes.Role, role)
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

/*            if (user == null)
            {
                var ex = new Exception();
                ex.Data.Add(StatusCodes.Status401Unauthorized.ToString(),
                    "User not exists"
                );
                throw ex;
            }*/

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

        public async Task<UserDto> EditProfile(EditUserModel editUserModel, string userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id.ToString() == userId);

/*            if (user == null)
            {
                var ex = new Exception();
                ex.Data.Add(StatusCodes.Status401Unauthorized.ToString(),
                    "User not exists"
                );
                throw ex;
            }*/

            CheckGender(editUserModel.Gender);
            CheckBirthdate(editUserModel.Birthdate);

            if (editUserModel.Birthdate.HasValue)
            {
                editUserModel.Birthdate = editUserModel.Birthdate.Value.ToUniversalTime();
            }

            user.Name = editUserModel?.Name;
            user.Email = editUserModel?.Email;
            user.Birthdate = editUserModel?.Birthdate;
            user.Gender = editUserModel?.Gender;
            user.Citizenship = editUserModel?.Citizenship;
            user.PhoneNumber = editUserModel?.PhoneNumber;

            await _context.SaveChangesAsync();

            return await GetProfile(userId);
        }

        public async Task<string> ChangePassword(string userId, EditPasswordModel editPasswordModel)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id.ToString() == userId);

/*            if (user == null)
            {
                var ex = new Exception();
                ex.Data.Add(StatusCodes.Status401Unauthorized.ToString(),
                    "User not exists"
                );
                throw ex;
            }*/

            if (!CheckHashPassword(user.Password, editPasswordModel.OldPassword))
            {
                var ex = new Exception();
                ex.Data.Add(StatusCodes.Status401Unauthorized.ToString(),"Wrong password");
                throw ex;
            }

            //Удаление всех прошлых проверочных кодов
            var codesToDelete = await _context.Codes.Where(x => x.UserId == userId).ToListAsync();
            if (codesToDelete != null)
            {
                _context.Codes.RemoveRange(codesToDelete);
                await _context.SaveChangesAsync();
            }
            //

            var savedPasswordHash = await HashingPassword(editPasswordModel.NewPassword);
            Random random = new Random();

            var addDbNumber = new EditPasswordCode
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                NewPassword = savedPasswordHash,
                Code = random.Next(100000, 999999).ToString(),
                CreatedDate = DateTime.UtcNow
            };

            var messageData = new MessageDto
            {
                Id = Guid.NewGuid(),
                Email = user.Email,
                Message = $"Your verification code: {addDbNumber.Code}"
            };

            await SendNotificationRabbitMQ(messageData);

            try
            {
                await _context.Codes.AddAsync(addDbNumber);
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
            }

            return $"The verification code has been sent to your email {user.Email}";
        }


        public async Task SendNotificationRabbitMQ(MessageDto messageData)
        {
            var factory = new ConnectionFactory()
            {
                HostName = "localhost",
                Port = 5672,
                UserName = "guest",
                Password = "guest"
            };


            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();
            //Console.WriteLine("Письмо отправлено");

            channel.QueueDeclare(queue: "EmailQueue",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

            var message = JsonConvert.SerializeObject(messageData);
            var body = Encoding.UTF8.GetBytes(message);

            channel.BasicPublish(exchange: "",
            routingKey: "EmailQueue",
            basicProperties: null,
            body: body);
        }


        public async Task SendCode(string code, string userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id.ToString() == userId);

            var codeDB = await _context.Codes.FirstOrDefaultAsync(x => x.Code == code && x.UserId == userId);

            if (codeDB == null)
            {
                var ex = new Exception();
                ex.Data.Add(StatusCodes.Status403Forbidden.ToString(), "Invalid code");
                throw ex;
            }
            else if (codeDB.CreatedDate.AddMinutes(10) < DateTime.UtcNow)
            {
                var ex = new Exception();
                ex.Data.Add(StatusCodes.Status403Forbidden.ToString(), "This code is outdated");
                throw ex;
            }
            else
            {
                user.Password = codeDB.NewPassword;
                await _context.SaveChangesAsync();
                _context.Codes.Remove(codeDB);
                _context.SaveChanges();
            }
        }


        public async Task<string> GetUserIdFromToken(string bearerToken)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(bearerToken);

            string userId = await Task.FromResult(jwtToken.Claims.FirstOrDefault(c => c.Type == "unique_name")?.Value);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id.ToString() == userId);
            if (user == null)
            {
                var ex = new Exception();
                ex.Data.Add(StatusCodes.Status401Unauthorized.ToString(),"User not exists");
                throw ex;
            }

            return userId;
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

        private async Task<string> HashingPassword(string password)
        {
            byte[] salt;
            RandomNumberGenerator.Create().GetBytes(salt = new byte[16]);
            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000);
            var hash = pbkdf2.GetBytes(20);
            var hashBytes = new byte[36];
            Array.Copy(salt, 0, hashBytes, 0, 16);
            Array.Copy(hash, 0, hashBytes, 16, 20);
            return Convert.ToBase64String(hashBytes);
        }
    }
}
