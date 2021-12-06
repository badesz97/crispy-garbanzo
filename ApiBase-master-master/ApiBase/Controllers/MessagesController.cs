using ApiBase.Data;
using ApiBase.DTO;
using ApiBase.Models;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ApiBase.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MessagesController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ApiDbContext _database;
        private readonly IMapper mapper;

        public MessagesController(UserManager<AppUser> userManager, ApiDbContext database, IMapper mapper)
        {
            _userManager = userManager;
            _database = database;
            this.mapper = mapper;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<MessageDto>> CreateMessage(CreateMessageDto createMessageDto)
        {
            var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var sender = await _userManager.FindByNameAsync(username);
            var recipient = await _userManager.FindByNameAsync(createMessageDto.RecipientUserName);

            if (username == createMessageDto.RecipientUserName.ToLower())
                return BadRequest("Cannot send messages to themselves.");

            if (recipient == null) return NotFound();

            var message = new Message
            {
                Sender = sender,
                Recipient = recipient,
                SenderUsername = sender.UserName,
                RecipientUsername = recipient.UserName,
                Content = createMessageDto.Content
            };

            _database.Messages.Add(message);
            _database.SaveChanges();

            return Ok(mapper.Map<MessageDto>(message));
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessagesForUser(string predicate)
        {
            var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _userManager.FindByNameAsync(username);
            IEnumerable<Message> messages = new List<Message>();

            if (predicate == "incoming")
            {
                messages = _database.Messages
                .Where(m => m.RecipientUsername == username)
                .OrderByDescending(m => m.MessageSent).ToList();

                var messageDtos = new List<MessageDto>();
                foreach (var m in messages)
                {
                    m.DateRead = DateTime.Now;
                    var newMdto = mapper.Map<MessageDto>(m);
                    var sender = await _userManager.FindByNameAsync(m.SenderUsername);
                    newMdto.SenderPhotoUrl = sender.Photos.FirstOrDefault(x => x.IsMain).Url;

                    messageDtos.Add(newMdto);
                }
                _database.SaveChanges();

                return Ok(messageDtos);
            }
            else if (predicate == "outgoing")
            {
                messages = _database.Messages
                .Where(m => m.SenderUsername == username)
                .OrderByDescending(m => m.MessageSent).ToList();

                var messageDtos = new List<MessageDto>();
                foreach (var m in messages)
                {
                    var newMdto = mapper.Map<MessageDto>(m);
                    var rec = await _userManager.FindByNameAsync(m.RecipientUsername);
                    newMdto.RecipientPhotoUrl = rec.Photos.FirstOrDefault(x => x.IsMain).Url;

                    messageDtos.Add(newMdto);
                }

                return Ok(messageDtos);
            }
            else
                return BadRequest("Unknown predicate.");

            
        }

        [HttpGet("thread/{username}")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessageThreadForUsers(string username)
        {
            var currentUsername = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var currentUser = await _userManager.FindByNameAsync(currentUsername);
            var recipientUser = await _userManager.FindByNameAsync(username);
            IEnumerable<Message> messages = new List<Message>();

            if (currentUser != null && recipientUser != null)
            {
                messages = _database.Messages
                .Where(m => (m.RecipientUsername == username && m.SenderUsername == currentUsername) ||
                            (m.RecipientUsername == currentUsername && m.SenderUsername == username))
                .OrderByDescending(m => m.MessageSent).ToList();

                var messageDtos = new List<MessageDto>();
                foreach (var m in messages)
                {
                    messageDtos.Add(mapper.Map<MessageDto>(m));
                }
                return Ok(messageDtos);
            }
            else
            {
                return NotFound("Users not found");
            }
        }
    }
}
