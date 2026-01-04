using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonitoringSystem.Data;
using MonitoringSystem.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MonitoringSystem.Controllers
{
    [Authorize]
    public class MessagesController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public MessagesController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // ================= GET ALL CONVERSATIONS =================
        [HttpGet]
        public async Task<IActionResult> GetConversations()
        {
            var currentUserId = _userManager.GetUserId(User);

            var conversations = await _db.Conversations
                .Include(c => c.User1)
                .Include(c => c.User2)
                .Include(c => c.Messages)
                .Where(c => c.User1Id == currentUserId || c.User2Id == currentUserId)
                .ToListAsync();

            var result = conversations.Select(c =>
            {
                var otherUser = c.User1Id == currentUserId ? c.User2 : c.User1;
                var lastMsg = c.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault();
                return new
                {
                    id = c.Id,
                    user = new { id = otherUser.Id, userName = otherUser.FirstName + " " + otherUser.LastName },
                    lastMessage = lastMsg?.Text
                };
            })
            .OrderByDescending(c => c.lastMessage)
            .ToList();

            return Json(result);
        }

        // ================= GET MESSAGES FOR A CONVERSATION =================
        [HttpGet]
        public async Task<IActionResult> GetMessages(int conversationId)
        {
            var messages = await _db.Messages
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new
                {
                    id = m.Id,
                    senderId = m.SenderId,
                    text = m.Text,
                    createdAt = m.CreatedAt
                })
                .ToListAsync();

            return Json(messages);
        }

        // ================= SEND MESSAGE =================
        [HttpPost]
        public async Task<IActionResult> SendMessage(int conversationId, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return BadRequest("Message cannot be empty.");

            var currentUserId = _userManager.GetUserId(User);

            var message = new Message
            {
                ConversationId = conversationId,
                SenderId = currentUserId,
                Text = text,
                CreatedAt = DateTime.UtcNow
            };

            _db.Messages.Add(message);
            await _db.SaveChangesAsync();

            return Ok(message);
        }

        // ================= CREATE NEW CONVERSATION =================
        [HttpPost]
        public async Task<JsonResult> CreateConversation([FromBody] CreateConversationDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.User2Id))
                return Json(new { success = false, message = "Invalid data" });

            var currentUserId = _userManager.GetUserId(User);

            // Check if conversation already exists between these users
            var existing = await _db.Conversations.FirstOrDefaultAsync(c =>
                (c.User1Id == currentUserId && c.User2Id == dto.User2Id) ||
                (c.User1Id == dto.User2Id && c.User2Id == currentUserId)
            );

            if (existing != null)
                return Json(new { success = true, conversationId = existing.Id });

            // Create new conversation
            var conversation = new Conversation
            {
                User1Id = currentUserId,
                User2Id = dto.User2Id
            };

            _db.Conversations.Add(conversation);
            await _db.SaveChangesAsync();

            return Json(new { success = true, conversationId = conversation.Id });
        }

        // ================= GET USERS FOR NEW CONVERSATION SEARCH =================
        [HttpGet]
        public async Task<JsonResult> GetUsers(string? search)
        {
            var currentUserId = _userManager.GetUserId(User);

            var users = await _userManager.Users
                .Where(u => u.IsApproved && u.Id != currentUserId &&
                            (string.IsNullOrEmpty(search) || (u.FirstName + " " + u.LastName).Contains(search)))
                .Select(u => new { id = u.Id, userName = u.FirstName + " " + u.LastName })
                .ToListAsync();

            return Json(users);
        }

        // ================= DTO =================
        public class CreateConversationDto
        {
            public string User2Id { get; set; } = string.Empty;
        }
    }
}
