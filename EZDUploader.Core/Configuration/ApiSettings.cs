﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EZDUploader.Core.Models;

namespace EZDUploader.Core.Configuration
{
    public class ApiSettings
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string ApplicationToken { get; set; } = string.Empty;
        public string Login { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public List<DocumentType> DocumentTypes { get; set; } = new();

public (string AuthParam, string AuthToken) GenerateAuthTokens()
        {
            if (string.IsNullOrEmpty(ApplicationToken))
                throw new InvalidOperationException("Token aplikacji nie jest ustawiony");

            var authParam = Guid.NewGuid().ToString();
            var tokenBase = $"{authParam}{ApplicationToken}{DateTime.Now:yyyyMMddhh}";

            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var tokenBytes = System.Text.Encoding.ASCII.GetBytes(tokenBase);
            var hashBytes = sha256.ComputeHash(tokenBytes);

            var authToken = string.Join("", hashBytes.Select(b => b.ToString("x2")));

            return (authParam, authToken);
        }
        public AuthenticationType AuthType { get; set; } = AuthenticationType.Token;

        public void CopyFrom(ApiSettings other)
        {
            if (other == null) return;

            BaseUrl = other.BaseUrl;
            ApplicationToken = other.ApplicationToken;
            Login = other.Login;
            Password = other.Password;
            AuthType = other.AuthType;
            DocumentTypes = other.DocumentTypes?.ToList() ?? new();
        }
    }
    public enum AuthenticationType
    {
        Token,
        LoginPassword
    }
}