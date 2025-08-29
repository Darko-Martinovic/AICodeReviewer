using Microsoft.AspNetCore.Mvc;
using AICodeReviewer.Models.Collaboration;
using AICodeReviewer.Services.Interfaces;

namespace AICodeReviewer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CollaborationController : ControllerBase
    {
        private readonly ICollaborationService _collaborationService;
        private readonly ILogger<CollaborationController> _logger;

        public CollaborationController(ICollaborationService collaborationService, ILogger<CollaborationController> logger)
        {
            _collaborationService = collaborationService;
            _logger = logger;
        }

        [HttpPost("sessions")]
        public async Task<ActionResult<ReviewSession>> CreateSession([FromBody] CreateSessionRequest request)
        {
            try
            {
                // Check if there's already an active session for this commit
                var existingSession = await _collaborationService.GetSessionByCommitAsync(request.CommitSha, request.RepositoryFullName);
                if (existingSession != null)
                {
                    return Ok(existingSession);
                }

                var session = await _collaborationService.CreateSessionAsync(
                    request.CommitSha,
                    request.RepositoryFullName,
                    request.CreatorUserId,
                    request.CreatorUserName
                );

                return Ok(session);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating collaboration session");
                return StatusCode(500, "Failed to create session");
            }
        }

        [HttpGet("sessions/{sessionId}")]
        public async Task<ActionResult<ReviewSession>> GetSession(string sessionId)
        {
            try
            {
                var session = await _collaborationService.GetSessionAsync(sessionId);
                if (session == null)
                {
                    return NotFound();
                }

                return Ok(session);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting session {SessionId}", sessionId);
                return StatusCode(500, "Failed to get session");
            }
        }

        [HttpGet("sessions")]
        public async Task<ActionResult<List<ReviewSession>>> GetActiveSessions([FromQuery] string? repository = null)
        {
            try
            {
                var sessions = string.IsNullOrEmpty(repository)
                    ? await _collaborationService.GetActiveSessionsAsync()
                    : await _collaborationService.GetActiveSessionsForRepositoryAsync(repository);

                return Ok(sessions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active sessions");
                return StatusCode(500, "Failed to get sessions");
            }
        }

        [HttpGet("sessions/by-commit/{commitSha}")]
        public async Task<ActionResult<ReviewSession>> GetSessionByCommit(string commitSha, [FromQuery] string repository)
        {
            try
            {
                var session = await _collaborationService.GetSessionByCommitAsync(commitSha, repository);
                if (session == null)
                {
                    return NotFound();
                }

                return Ok(session);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting session for commit {CommitSha}", commitSha);
                return StatusCode(500, "Failed to get session");
            }
        }

        [HttpPost("sessions/{sessionId}/archive")]
        public async Task<ActionResult> ArchiveSession(string sessionId)
        {
            try
            {
                var success = await _collaborationService.ArchiveSessionAsync(sessionId);
                if (!success)
                {
                    return NotFound();
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error archiving session {SessionId}", sessionId);
                return StatusCode(500, "Failed to archive session");
            }
        }

        [HttpGet("sessions/{sessionId}/comments")]
        public async Task<ActionResult<List<LiveComment>>> GetSessionComments(string sessionId)
        {
            try
            {
                var session = await _collaborationService.GetSessionAsync(sessionId);
                if (session == null)
                {
                    return NotFound();
                }

                return Ok(session.Comments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comments for session {SessionId}", sessionId);
                return StatusCode(500, "Failed to get comments");
            }
        }

        [HttpGet("sessions/{sessionId}/participants")]
        public async Task<ActionResult<List<SessionParticipant>>> GetSessionParticipants(string sessionId)
        {
            try
            {
                var session = await _collaborationService.GetSessionAsync(sessionId);
                if (session == null)
                {
                    return NotFound();
                }

                return Ok(session.Participants.Where(p => p.IsActive));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting participants for session {SessionId}", sessionId);
                return StatusCode(500, "Failed to get participants");
            }
        }

        [HttpPost("cleanup")]
        public async Task<ActionResult> CleanupInactiveSessions()
        {
            try
            {
                await _collaborationService.CleanupInactiveSessionsAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up inactive sessions");
                return StatusCode(500, "Failed to cleanup sessions");
            }
        }
    }

    public class CreateSessionRequest
    {
        public string CommitSha { get; set; } = string.Empty;
        public string RepositoryFullName { get; set; } = string.Empty;
        public string CreatorUserId { get; set; } = string.Empty;
        public string CreatorUserName { get; set; } = string.Empty;
    }
}
