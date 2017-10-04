using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using GayChat.Models.ITCHat;

namespace GayChat.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string Nickname { get; set; }

        public string FirstName { get; set; }

        public string Surname { get; set; }

        public List<Friend> Friends { get; set; }

        public ApplicationUser()
        {
            Friends = new List<Friend>();
        }
    }
}
