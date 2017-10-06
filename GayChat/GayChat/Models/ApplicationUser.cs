using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using GayChat.Models.ITCHat;
using Newtonsoft.Json;

namespace GayChat.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string Nickname { get; set; }

        public string FirstName { get; set; }

        public string Surname { get; set; }

        public string Friends_JSON
        {
            get { return JsonConvert.SerializeObject(Friends); }
            set
            {
                if (!string.IsNullOrEmpty(value)) { Friends = JsonConvert.DeserializeObject<List<Friend>>(value); }
            }
        }

        public FriendStatus GetStatusForFriendId(string friendId)
        {
            foreach (var friend in Friends)
            {
                if (friend.FriendId == friendId)
                    return friend.Status;
            }

            return FriendStatus.None;
        }

        [NotMapped]
        public List<Friend> Friends { get; set; }

        public ApplicationUser()
        {
            Friends = new List<Friend>();
        }
    }
}
