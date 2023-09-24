using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace CoronaDeployments.Core.Models.Mvc
{
    public class UserCreateModel
    {
        public string Name { get; set; }
        public string Username { get; set; }
        public Guid CreatedByUserId { get; set; }
        private string Password { get; set; } = null;

        public string GetPassword()
        {
            if (Password == null)
            {
                var pwd = PasswordUtils.GenerateRandomPassword();
                Password = pwd;
                return pwd;
            }
            else
                return Password;
        }

        public string GetPasswordHashed()
        {
            return PasswordUtils.Hash(GetPassword());
        }
    }
}
