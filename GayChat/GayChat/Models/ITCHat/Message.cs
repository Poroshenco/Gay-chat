using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GayChat.Models.ITCHat
{
    public class Message
    {
        public DateTime SendTime { get; set; }

        public string MessageInner { get; set; }

        public Message()
        {
            SendTime = DateTime.Now;
        }
    }
}
