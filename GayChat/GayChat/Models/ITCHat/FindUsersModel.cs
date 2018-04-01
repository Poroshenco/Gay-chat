using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GayChat.Models.ITCHat
{
    public class FindUsersModel
    {
        public string Id { get; set; }

        public string Nickname { get; set; }

        public FriendStatus Status { get; set; }

        public string FullName { get; set; }

        public string ButtonColor { get; }

        public string ButtonText { get; }

        public FindUsersModel(FriendStatus status)
        {
            Status = status;

            ButtonColor = GetColorForButton();

            ButtonText = GetTextForButton();
        }

        public string GetColorForButton()
        {
            switch (Status)
            {
                case FriendStatus.Accepted:
                {
                    return "#6d4c41";
                }
                case FriendStatus.Invited:
                {
                    return "#757575";
                }
                case FriendStatus.Subscriber:
                {
                    return "#6d4c41";
                }
                default:
                {
                    return "#6d4c41";
                }
            }
        }

        public string GetTextForButton()
        {
            switch (Status)
            {
                case FriendStatus.Accepted:
                {
                    return "Friend";
                }
                case FriendStatus.Invited:
                {
                    return "Invited";
                }
                case FriendStatus.Subscriber:
                {
                    return "Subscriber";
                }
                default:
                {
                    return "Invite";
                }
            }
        }
    }
}
