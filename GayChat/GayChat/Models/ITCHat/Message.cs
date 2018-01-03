using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GayChat.Models.ITCHat
{
    public class Message
    {
        public DateTime FullSendTime { get; set; }

        public string ShortSendTime { get; set; }

        public string MessageInner { get; set; }

        public bool FromMe { get; set; }

        public bool IsNew { get; set; }

        public Message()
        {
            FullSendTime = DateTime.Now;

            ShortSendTime = DateTime.Now.ToString("HH:mm");
        }
    }
}
