using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options; // Importar Options
using Microsoft.IdentityModel.Tokens;
using StreamForge.Application.Interfaces;
using StreamForge.Domain.Entities;
using StreamForge.Infrastructure.Options; // Importar Options Class

namespace StreamForge.Infrastructure.Services;

public class TokenService : ITokenService
{
    private readonly JwtSettings _settings;

    public TokenService(IOptions<JwtSettings> options)
    {
        _settings = options.Value;
    }

        public string GenerateToken(User user)

        {

            var jwtKey = _settings.Key ?? throw new ArgumentNullException(nameof(_settings.Key), "Jwt:Key is missing");

            var issuer = _settings.Issuer ?? throw new ArgumentNullException(nameof(_settings.Issuer), "Jwt:Issuer is missing");

            var audience = _settings.Audience ?? throw new ArgumentNullException(nameof(_settings.Audience), "Jwt:Audience is missing");

    

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    

            var claims = new[]

            {

                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),

                new Claim(JwtRegisteredClaimNames.Email, user.Email),

                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())

            };

    

            var token = new JwtSecurityToken(

                issuer: issuer,

                audience: audience,

                claims: claims,

                expires: DateTime.UtcNow.AddHours(2),

                signingCredentials: creds

            );

    

            return new JwtSecurityTokenHandler().WriteToken(token);

        }

    }

    