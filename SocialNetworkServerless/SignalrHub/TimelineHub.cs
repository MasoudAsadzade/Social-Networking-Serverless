using AutoMapper;
using SocialNetworkServerless.AzureSocialNetworkServerless.Data;
using SocialNetworkServerless.AzureSocialNetworkServerless.Models;
using SocialNetworkServerless.AzureSocialNetworkServerless.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SocialNetworkServerless.AzureSocialNetworkServerless.SignalrHub
{
 
    [Authorize]
    public class TimelineHub : Hub
    {
        public readonly static List<UserViewModel> _Connections = new List<UserViewModel>();
        public readonly static List<RoomViewModel> _Rooms = new List<RoomViewModel>();
        private readonly static Dictionary<string, string> _ConnectionsMap = new Dictionary<string, string>();

        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public TimelineHub(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public async Task SendtoPersonalTimeline (string roomName,string receiverName, string message)
        {
            if (_ConnectionsMap.TryGetValue(receiverName, out string userId))
            {
                var recieveduserid = _context.Users.FirstOrDefault(u => u.UserName== receiverName);
                var sender = _context.Users.Where(u => u.UserName == IdentityName).FirstOrDefault();
                var room = _context.Rooms.Where(r => r.Name == roomName).FirstOrDefault();
                if (!string.IsNullOrEmpty(message.Trim()))
                {
                    // Build the message
                    var newmessage = new Message()
                    {
                        Content = Regex.Replace(message, @"(?i)<(?!img|a|/a|/img).*?>", string.Empty),
                        Timestamp = DateTime.Now
                    };
                    _context.Messages.Add(newmessage);
                    _context.SaveChanges();
                    var personalTimeline = new PersonalTimeline()
                    {
                        FromUser = sender,
                        ToRoom = room,
                        Message = newmessage,
                        ToUser= recieveduserid
                    };
                    _context.PersonalTimelines.Add(personalTimeline);
                    _context.SaveChanges();

                    var messageViewModel = _mapper.Map<Tuple<Message, PersonalTimeline>, MessageViewModel>(Tuple.Create(newmessage, personalTimeline));
                    await Clients.User(recieveduserid.Id).SendAsync("newMessage", messageViewModel);
                    await Clients.Caller.SendAsync("newMessage", messageViewModel);
                }
            }
        }

        public async Task SendtoPublicTimeline (string roomName, string message)
        {
            try
            {
                var user = _context.Users.Where(u => u.UserName == IdentityName).FirstOrDefault();
                var room = _context.Rooms.Where(r => r.Name == roomName).FirstOrDefault();

                if (!string.IsNullOrEmpty(message.Trim()))
                {

                    var newmessage = new Message()
                    {
                        Content = Regex.Replace(message, @"(?i)<(?!img|a|/a|/img).*?>", string.Empty),
                        Timestamp = DateTime.Now
                    };
                    _context.Messages.Add(newmessage);
                    _context.SaveChanges();
                    var baseTimeline = new PersonalTimeline()
                    {
                        FromUser = user,
                        ToRoom = room,
                        Message = newmessage
                    };
                    _context.BaseTimelines.Add(baseTimeline);
                    _context.SaveChanges();

                    var messageViewModel = _mapper.Map<Tuple<Message, PersonalTimeline>, MessageViewModel>(Tuple.Create(newmessage, baseTimeline));
                    await Clients.Group(roomName).SendAsync("newMessage", messageViewModel);
                }
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("onError", "Message not send! Message should be 1-500 characters.");
            }
        }

        public async Task Join(string roomName)
        {
            try
            {
                var user = _Connections.Where(u => u.Username == IdentityName).FirstOrDefault();
                if (user != null && user.CurrentRoom != roomName)
                {
                    // Remove user from others list
                    if (!string.IsNullOrEmpty(user.CurrentRoom))
                        await Clients.OthersInGroup(user.CurrentRoom).SendAsync("removeUser", user);

                    // Join to new chat room
                    await Leave(user.CurrentRoom);
                    await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
                    user.CurrentRoom = roomName;

                    // Tell others to update their list of users
                    await Clients.OthersInGroup(roomName).SendAsync("addUser", user);
                }
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("onError", "failed!" + ex.Message);
            }
        }

        public async Task Leave(string roomName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomName);
        }

        public IEnumerable<RoomViewModel> GetRooms()
        {
            // First run?
            if (_Rooms.Count == 0)
            {
                foreach (var room in _context.Rooms)
                {
                    var roomViewModel = _mapper.Map<Room, RoomViewModel>(room);
                    _Rooms.Add(roomViewModel);
                }
            }

            return _Rooms.ToList();
        }

        public IEnumerable<UserViewModel> GetUsers(string roomName)
        {
            return _Connections.Where(u => u.CurrentRoom == roomName).ToList();
        }

        public IEnumerable<MessageViewModel> GetPersonaltimelineHistory(string roomName)
        {
            var user = _context.Users.Where(u => u.UserName == IdentityName).FirstOrDefault();
            var messageHistory =
                (from b in _context.PersonalTimelines.Where(b => b.ToRoom.Name == roomName &&
                !b.ToUser.Equals(null) && (b.FromUser == user || b.ToUser == user)).Include(m => m.FromUser)
                 join m in _context.Messages
                 on b.Message.Id equals m.Id
                 select Tuple.Create(m, b)).AsQueryable().ToList()
                    .OrderByDescending(m => m.Item1.Timestamp)
                    .Take(10)
                    .Reverse();
            return _mapper.Map<IEnumerable<Tuple<Message, PersonalTimeline>>, IEnumerable<MessageViewModel>>(messageHistory);
        }
        public IEnumerable<MessageViewModel> GetTimelineHistory(string roomName)
        {

            var x = _context.PersonalTimelines.Where(b => b.ToRoom.Name == roomName && b.ToUser.Equals(null));
            var user = _context.Users.Where(u => u.UserName == IdentityName).FirstOrDefault();
            var messageHistory =
                (from b in _context.PersonalTimelines.Where(b => b.ToRoom.Name == roomName && b.ToUser.Equals(null)).Include(m => m.FromUser)
                 join m in _context.Messages
                 on b.Message.Id equals m.Id
                 select Tuple.Create(m, b)).AsQueryable().ToList()
                    .OrderByDescending(m => m.Item1.Timestamp)
                    .Take(10)
                    .Reverse();

            return _mapper.Map<IEnumerable<Tuple<Message, PersonalTimeline>>, IEnumerable<MessageViewModel>>(messageHistory);
        }

        public override Task OnConnectedAsync()
        {
            try
            {
                var user = _context.Users.Where(u => u.UserName == IdentityName).FirstOrDefault();
                var userViewModel = _mapper.Map<ApplicationUser, UserViewModel>(user);
                userViewModel.CurrentRoom = "";

                if (!_Connections.Any(u => u.Username == IdentityName))
                {
                    _Connections.Add(userViewModel);
                    _ConnectionsMap.Add(IdentityName, Context.ConnectionId);
                }

                Clients.Caller.SendAsync("getProfileInfo", user.FullName);
            }
            catch (Exception ex)
            {
                Clients.Caller.SendAsync("onError", "OnConnected:" + ex.Message);
            }
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            try
            {
                var user = _Connections.Where(u => u.Username == IdentityName).First();
                _Connections.Remove(user);

                // Tell other users to remove you from their list
                Clients.OthersInGroup(user.CurrentRoom).SendAsync("removeUser", user);

                // Remove mapping
                _ConnectionsMap.Remove(user.Username);
            }
            catch (Exception ex)
            {
                Clients.Caller.SendAsync("onError", "OnDisconnected: " + ex.Message);
            }

            return base.OnDisconnectedAsync(exception);
        }

        private string IdentityName
        {
            get { return Context.User.Identity.Name; }
        }

    }
}
