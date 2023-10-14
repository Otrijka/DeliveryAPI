﻿using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Azure.Core;
using DeliveryApi.Context;
using DeliveryApi.Enums;
using DeliveryApi.Helpers;
using DeliveryApi.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DeliveryApi.Services;

public class AccountService : IAccountService
{
    private readonly DeliveryContext _context;
    private readonly IConfiguration _configuration;
    private readonly JwtTokenHelper _tokenHepler;

    public AccountService(DeliveryContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;

        string key = _configuration["JWTSettings:SecretKey"];
        string issuer = _configuration["JWTSettings:Issuer"];
        string audience = _configuration["JWTSettings:Audience"];
        double durationInMinute = double.Parse(_configuration["JWTSettings:DurationInMinute"]);

        _tokenHepler = new JwtTokenHelper(key, issuer, audience, durationInMinute);
    }

    //Регистрация юзера
    public async Task<string> CreateUser(UserRegistration model)
    {
        var checkUser = await _context.Users.FirstOrDefaultAsync(u => model.Email == u.Email);
        if (checkUser != null)
        {
            return "email data is already in use";
        }

        UserDTO newUser = new UserDTO
        {
            Id = Guid.NewGuid(),
            Email = model.Email,
            FullName = model.FullName,
            BirthDate = model.BirthDate,
            Gender = model.Gender,
            Phone = model.Phone,
            AddressId = model.AddressId,
            HashedPassword = HashPasswordHelper.HashPassword(model.Password)
        };
        await _context.AddAsync(newUser);
        await _context.SaveChangesAsync();

        var token = _tokenHepler.GenerateToken(newUser.Email, newUser.Role);

        return token;
    }

    //Логин юзера
    public async Task<string> LoginUser(UserLogin model)
    {
        var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == model.Email);

        if (user == null)
        {
            throw new Exception(message: "Bad email");
            return null;
        }

        var verifyPassword = HashPasswordHelper.VerifyPassword(model.Password, user.HashedPassword);

        if (!verifyPassword)
        {
            throw new Exception(message: "Bad password");
            return null;
        }

        Role role = (user.Role == Role.Admin) ? Role.Admin : Role.User;

        var token = _tokenHepler.GenerateToken(model.Email, role);
        return token;
    }

    //Получение юзера
    public async Task<UserProfile> GetProfile(string token)
    {
        var userEmail = JwtParseHelper.GetClaimValue(token, ClaimTypes.Email);
        var user = await _context.Users.FirstOrDefaultAsync(user => user.Email == userEmail);

        return new UserProfile
        {
            FullName = user.FullName,
            Email = user.Email,
            AddressId = user.AddressId,
            BirthDate = user.BirthDate,
            Gender = user.Gender,
            Phone = user.Phone
        };
    }

    //Редактирование юзера
    public async Task EditProfile(string token, UserEditProfile model)
    {
        var userEmail = JwtParseHelper.GetClaimValue(token, ClaimTypes.Email);
        var user = await _context.Users.FirstOrDefaultAsync(user => user.Email == userEmail);

        if (user == null)
        {
            return;
        }

        user.FullName = model.FullName;
        user.AddressId = model.AddressId;
        user.BirthDate = model.BirthDate;
        user.Gender = model.Gender;
        user.Phone = model.Phone;
        await _context.SaveChangesAsync();
        return;
    }
}