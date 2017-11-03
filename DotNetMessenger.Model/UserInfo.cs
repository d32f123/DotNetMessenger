﻿using System;
using DotNetMessenger.Model.Enums;

namespace DotNetMessenger.Model
{
    public class UserInfo
    {
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string LastAndFirstName => LastName + " " + FirstName;

        public string Phone { get; set; }
        public string Email { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public Genders? Gender { get; set; }
        public byte[] Avatar { get; set; }

        public UserInfo()
        {
            DateOfBirth = null;
        }
    }
}