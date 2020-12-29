using AutoMapper;
using SocialNetworkServerless.AzureSocialNetworkServerless.Data;
using SocialNetworkServerless.AzureSocialNetworkServerless.SignalrHub;
using SocialNetworkServerless.AzureSocialNetworkServerless.Models;
using SocialNetworkServerless.AzureSocialNetworkServerless.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace SocialNetworkServerless.AzureSocialNetworkServerless.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TimelineController : ControllerBase
    {
        private readonly IHubContext<TimelineHub> _hubContext;
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public TimelineController(ApplicationDbContext context, IHubContext<TimelineHub> hubContext,
            IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
            _hubContext = hubContext;
        }
        [HttpGet]
        [Route("SendtoPersonalTimeline")]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(MessageViewModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> SendtoPersonalTimeline(string fromUser, string receiverUser, string roomName, string message)
        {
            if (string.IsNullOrEmpty(message.Trim()))
            {
                return BadRequest("Need a Message");
            }
            var recievedUserId = _context.Users.FirstOrDefault(u => u.UserName == receiverUser);
            if (recievedUserId == null)
            {
                return BadRequest($"non-existing User ({recievedUserId})");
            }
            var sender = _context.Users.Where(u => u.UserName == fromUser).FirstOrDefault();
            var room = _context.Rooms.Where(r => r.Name == roomName).FirstOrDefault();

            var newMessage = new Message()
            {
                Content = Regex.Replace(message, @"(?i)<(?!img|a|/a|/img).*?>", string.Empty),
                Timestamp = DateTime.Now
            };
             _context.Messages.Add(newMessage);
            var personalTimeline = new PersonalTimeline()
            {
                FromUser = sender,
                ToRoom = room,
                Message = newMessage,
                ToUser = recievedUserId
            };
             _context.BaseTimelines.Add(personalTimeline);
             _context.SaveChanges();
            await _hubContext.Clients.User(recievedUserId.Id).SendAsync("newMessage", newMessage);
            await _hubContext.Clients.User(sender.Id).SendAsync("newMessage", newMessage);
            
            return Ok();
        }
        [HttpGet]
        [Route("SendtoPublicTimeline")]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(MessageViewModel), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<MessageViewModel>> SendtoPublicTimeline(string fromUser, string roomName, string message)
        {

            if (string.IsNullOrEmpty(message.Trim()))
            {
                return BadRequest("Need a Message");
            }
            var user = _context.Users.Where(u => u.UserName == fromUser).FirstOrDefault();
            var room = _context.Rooms.Where(r => r.Name == roomName).FirstOrDefault();

            var newmessage = new Message()
            {
                Content = Regex.Replace(message, @"(?i)<(?!img|a|/a|/img).*?>", string.Empty),
                Timestamp = DateTime.Now
            };
            _context.Messages.Add(newmessage);
            var baseTimeline = new PersonalTimeline()
            {
                FromUser = user,
                ToRoom = room,
                Message = newmessage
            };
            _context.BaseTimelines.Add(baseTimeline);
            _context.SaveChanges();
            await _hubContext.Clients.Group(room.Name).
                SendAsync("newMessage", _mapper.Map<Tuple<Message, PersonalTimeline>, MessageViewModel>(Tuple.Create(newmessage, baseTimeline)));
            return Ok();
        }

        [HttpGet]
        [Route("GetTimelineHistory")]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(MessageViewModel), (int)HttpStatusCode.OK)]
        public IEnumerable<MessageViewModel> GetTimelineHistory(string fromUser,string roomName)
        {
            var user = _context.Users.Where(u => u.UserName == fromUser).FirstOrDefault();
            var messageHistory =
                (from b in _context.PersonalTimelines.Where(b => b.ToRoom.Name == roomName && b.ToUser.Equals(null)).Include(m => m.FromUser)
                 join m in _context.Messages
                 on b.Message.Id equals m.Id
                 select Tuple.Create(m, b))
                    .AsEnumerable().ToList()
                    .OrderByDescending(m => m.Item1.Timestamp)
                    .Take(30)
                    .Reverse();

            return _mapper.Map<IEnumerable<Tuple<Message, PersonalTimeline>>, IEnumerable<MessageViewModel>>(messageHistory);
        }
    }
}
