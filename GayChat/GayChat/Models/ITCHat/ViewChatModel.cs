using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GayChat.Models.ITCHat
{
    public class ViewChatModel
    {
        public string UserId { get; set; }

        public string UserFullname { get; set; }

        public string UserNickname { get; set; }

        public string LastMessage { get; set; }

        public bool FromMe { get; set; }

        public bool IsNew { get; set; }
    }
}
