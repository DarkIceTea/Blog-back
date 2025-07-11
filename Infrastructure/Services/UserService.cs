﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Abstractions;
using Application.Results;
using Domain.Models;

using Infrastructure.Resources;
using Microsoft.AspNetCore.Identity;

namespace Application.Services
{
    public class UserService(RoleManager<IdentityRole<Guid>> roleManager, UserManager<CustomUser> userManager, IAccessTokenService accessTokenService, IRefreshTokenService refreshTokenService) : IUserService
    {
        public async Task<(Tokens, Guid)> RegisterUserAsync(string email, string userName, string password, string role, CancellationToken cancellationToken)
        {
            if (await userManager.FindByEmailAsync(email) is not null)
                throw new Exception(ExceptionMessages.UserExist);

            if (role != "Admin" && role != "User" && role != "Editor")
                throw new Exception(ExceptionMessages.BadUserRole);

            var user = new CustomUser() { Id = Guid.NewGuid(), UserName = userName, Email = email };
            var res = await userManager.CreateAsync(user, password);
            if (!res.Succeeded)
                throw new Exception("Error to create user: " + res.Errors.First().Description);
            await userManager.AddToRoleAsync(user, role);

            var refreshToken = await refreshTokenService.CreateRefreshTokenAsync(user, cancellationToken);
            user.RefreshToken = refreshToken;
            await refreshTokenService.SaveChangesAsync(cancellationToken);
            var tokens = new Tokens()
            {
                AccessToken = accessTokenService.CreateAccessToken(user, cancellationToken),
                RefreshToken = refreshToken.Token
            };

            return (tokens, user.Id);
        }

        public async Task<Tokens> LoginUserAsync(string email, string password, CancellationToken cancellationToken)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
                throw new Exception(ExceptionMessages.WrongUser);

            if (!await userManager.CheckPasswordAsync(user, password))
                throw new Exception(ExceptionMessages.WrongUser);

            if (user.RefreshTokenId is not null)
            {
                var a = await refreshTokenService.GetRefreshTokenByIdAsync((Guid)user.RefreshTokenId, cancellationToken);
                await refreshTokenService.DeleteRefreshTokenAsync(a, cancellationToken);
            }

            var refreshToken = await refreshTokenService.CreateRefreshTokenAsync(user, cancellationToken);

            await refreshTokenService.SaveChangesAsync(cancellationToken);

            var tokens = new Tokens()
            {
                AccessToken = accessTokenService.CreateAccessToken(user, cancellationToken),
                RefreshToken = refreshToken.Token
            };
            return tokens;
        }
        public async Task<CustomUser> GetUserByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await userManager.FindByIdAsync(id.ToString());
        }

        public async Task<IList<string>> GetUserRoles(CustomUser user, CancellationToken cancellationToken)
        {
            return await userManager.GetRolesAsync(user);
        }
    }
}
