using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GayChat.Models.ITCHat
{
    public class Chat
    {
        public string UserId { get; set; }

        public string UserFullname { get; set; }

        public string UserNickname { get; set; }

        public List<Message> NotViewedMessages { get; set; }

        public List<Message> Messages { get; set; }

        public Chat()
        {
            NotViewedMessages = new List<Message>();

            Messages = new List<Message>();
        }

        public void Viewed()
        {
            Messages.AddRange(NotViewedMessages);
        }
    }
}
