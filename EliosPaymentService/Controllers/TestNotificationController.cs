using EliosPaymentService.Models;
using EliosPaymentService.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace SUtility.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestUserController : ControllerBase
    {
        private readonly IKafkaProducerRepository<User> _producerRepository;

        private const string ResponseTopic = "user-forum-user";
        private const string DestinationService = "user";

        public TestUserController(IKafkaProducerRepository<User> producerRepository)
        {
            _producerRepository = producerRepository;
        }

        /// <summary>
        /// Get all Users from Utility Service
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetAll()
        {
            try
            {
                Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] [USER] Getting all Users from UTILITY");

                var Users = await _producerRepository.ProduceGetAllAsync(
                    DestinationService,
                    ResponseTopic
                );

                Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] [USER] Received {Users.Count()} Users");
                return Ok(Users);
            }
            catch (TimeoutException ex)
            {
                return StatusCode(408, new { message = $"Request timeout: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Get User by ID from Utility Service
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetById(Guid id)
        {
            try
            {
                Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] [USER] Getting User {id} from UTILITY");

                var User = await _producerRepository.ProduceGetByIdAsync(
                    id,
                    DestinationService,           // utility
                    ResponseTopic,                // utility-user-User
                    cancellationToken: HttpContext.RequestAborted
                );

                if (User == null)
                {
                    return NotFound(new { message = $"User with ID {id} not found in Utility Service" });
                }

                Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] [USER] Found User {id}");
                return Ok(User);
            }
            catch (TimeoutException ex)
            {
                return StatusCode(408, new { message = $"Request timeout: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }



        /// <summary>
        /// Delete User from Utility Service
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            try
            {
                var existingUser = await _producerRepository.ProduceGetByIdAsync(
                    id,
                    DestinationService,
                    ResponseTopic,
                    cancellationToken: HttpContext.RequestAborted
                );

                if (existingUser == null)
                {
                    return NotFound(new { message = $"User with ID {id} not found in Utility Service" });
                }

                Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] [USER] Deleting User {id} from UTILITY");

                var res = await _producerRepository.ProduceDeleteAsync(
                    id,
                    DestinationService,
                    ResponseTopic,
                    cancellationToken: HttpContext.RequestAborted
                );

                Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] [USER] Deleted User {id}");
                return NoContent();
            }
            catch (TimeoutException ex)
            {
                return StatusCode(408, new { message = $"Request timeout: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }



        // REQUEST MODELS (KHÔNG ĐỔI)
        public class CreateUserRequest
        {
            public Guid UserId { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public string? Url { get; set; }
            public string? Metadata { get; set; }
        }

        public class UpdateUserRequest
        {
            public string? Title { get; set; }
            public string? Message { get; set; }
            public bool IsRead { get; set; }
            public string? Url { get; set; }
            public string? Metadata { get; set; }
        }
    }
}