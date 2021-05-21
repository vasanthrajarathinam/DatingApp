using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AccountController : BaseApiController
    {
        private readonly DataContext _context;
        private readonly ITokenService _tokenService;
        public AccountController(DataContext context, ITokenService tokenService)
        {
            _tokenService = tokenService;
            _context = context;

        }


        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
        {

            if (await UserExists(registerDto.Username))
            {
                return BadRequest("User Name is taken");
            }
            using var hmac = new HMACSHA512();

            var user = new AppUser()
            {
                UserName = registerDto.Username.ToLower(),
                PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.password)),
                PasswordSalt = hmac.Key
            };
            _context.Users.Add(user);

            await _context.SaveChangesAsync();
            return new UserDto{

                UserName = user.UserName,
                Token = _tokenService.CreateToken(user)
            };

        }

        private async Task<bool> UserExists(string username)
        {

            return await _context.Users.AnyAsync(x => x.UserName == username.ToLower());

        }


        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {

            var user = await _context.Users.SingleOrDefaultAsync(x => x.UserName == loginDto.Username.ToLower());

            if (user == null) return Unauthorized("User not found");


            using var hmca = new HMACSHA512(user.PasswordSalt);

            var comuptedHash = hmca.ComputeHash(Encoding.UTF8.GetBytes(loginDto.password));

            for (int i = 0; i < comuptedHash.Length; i++)
            {

                if (comuptedHash[i] != user.PasswordHash[i])
                    return Unauthorized("Invalid Password");

            }
             return new UserDto{

                UserName = user.UserName,
                Token = _tokenService.CreateToken(user)
            };
            
        }

    }
}