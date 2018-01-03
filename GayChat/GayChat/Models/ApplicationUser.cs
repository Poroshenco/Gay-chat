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

        public string Chats_JSON
        {
            get
            {
                if (Chats == null)
                    return null;
                return JsonConvert.SerializeObject(Chats);
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                    return;
                Chats = JsonConvert.DeserializeObject<List<Chat>>(value);
            }
        }

        public string Friends_JSON
        {
            get
            {
                if (Friends == null)
                    return null;
                return JsonConvert.SerializeObject(Friends);
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                    return;
                Friends = JsonConvert.DeserializeObject<List<Friend>>(value);
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

        [NotMapped]
        public List<Chat> Chats { get; set; }

        public int NotViewedChats()
        {
            int count = 0;

            foreach (var chat in Chats)
                if (chat.Messages.Where(e => e.IsNew == true).ToList().Count > 0)
                    count++;

            return count;
        }

        public ApplicationUser()
        {
            Friends = new List<Friend>();

            Chats = new List<Chat>();
        }
    }
}
