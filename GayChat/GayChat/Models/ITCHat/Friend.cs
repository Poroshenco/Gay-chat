﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GayChat.Models.ITCHat
{
    public class Friend
    {
        public byte[] FriendIdInBytes { get; set; }

        public FriendStatus Status { get; set; }
    }

    public enum FriendStatus
    {
        Accepted,
        Invited,
        Subscriber
    }
}