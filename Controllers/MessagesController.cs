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
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;

        public MessagesController(UserManager<ApplicationUser> userManager, ApplicationDbContext db)
        {
            _userManager = userManager;
            _db = db;
        }

        // ===================== Get all users except current =====================
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var currentUserId = _userManager.GetUserId(User);

            var users = await _userManager.Users
                .Where(u => u.Id != currentUserId)
                .Select(u => new { u.Id, u.UserName })
                .ToListAsync();

            return Json(users);
        }

        // ===================== Get all conversations for current user =====================
        [HttpGet]
        public async Task<IActionResult> GetConversations()
        {
            var userId = _userManager.GetUserId(User);

            var conversations = await _db.Conversations
                .Include(c => c.User1)
                .Include(c => c.User2)
                .Include(c => c.Messages)
                .Where(c => c.User1Id == userId || c.User2Id == userId)
                .OrderByDescending(c => c.Messages.Max(m => (DateTime?)m.CreatedAt) ?? c.CreatedAt)
                .ToListAsync();

            return Json(conversations.Select(c => new
            {
                c.Id,
                User = c.User1Id == userId ? new { c.User2.Id, c.User2.UserName } : new { c.User1.Id, c.User1.UserName },
                LastMessage = c.Messages.OrderByDescending(m => m.CreatedAt).Select(m => m.Text).FirstOrDefault()
            }));
        }

        // ===================== Get messages for a conversation =====================
        [HttpGet]
        public async Task<IActionResult> GetMessages(int conversationId)
        {
            var userId = _userManager.GetUserId(User);

            var conversation = await _db.Conversations
                .Include(c => c.Messages)
                .ThenInclude(m => m.Sender)
                .FirstOrDefaultAsync(c => c.Id == conversationId && (c.User1Id == userId || c.User2Id == userId));

            if (conversation == null)
                return NotFound();

            return Json(conversation.Messages
                .OrderBy(m => m.CreatedAt)
                .Select(m => new
                {
                    m.Id,
                    m.SenderId,
                    SenderName = m.Sender.UserName,
                    m.Text,
                    m.CreatedAt
                }));
        }

        // ===================== Send a message =====================
        [HttpPost]
        public async Task<IActionResult> SendMessage(int conversationId, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return BadRequest("Message text cannot be empty.");

            var userId = _userManager.GetUserId(User);

            var conversation = await _db.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId && (c.User1Id == userId || c.User2Id == userId));

            if (conversation == null)
                return NotFound();

            var message = new Message
            {
                ConversationId = conversationId,
                SenderId = userId,
                Text = text,
                CreatedAt = DateTime.Now
            };

            _db.Messages.Add(message);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                message.Id,
                message.SenderId,
                SenderName = (await _userManager.FindByIdAsync(userId))?.UserName,
                message.Text,
                message.CreatedAt
            });
        }

        // ===================== Start or get conversation with a specific user =====================
        [HttpPost]
        public async Task<IActionResult> StartConversation(string userId)
        {
            var currentUserId = _userManager.GetUserId(User);

            if (currentUserId == userId)
                return BadRequest("Cannot start conversation with yourself.");

            // Check if conversation already exists
            var conversation = await _db.Conversations
                .FirstOrDefaultAsync(c =>
                    (c.User1Id == currentUserId && c.User2Id == userId) ||
                    (c.User1Id == userId && c.User2Id == currentUserId));

            // Create new if not exists
            if (conversation == null)
            {
                conversation = new Conversation
                {
                    User1Id = currentUserId,
                    User2Id = userId,
                    CreatedAt = DateTime.Now
                };

                _db.Conversations.Add(conversation);
                await _db.SaveChangesAsync();
            }

            return Ok(new { conversation.Id });
        }
    }
}
