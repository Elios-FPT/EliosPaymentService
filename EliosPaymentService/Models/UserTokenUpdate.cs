namespace EliosPaymentService.Models;

public class UserTokenUpdate
{
    public Guid UserId { get; set; }
    public int Tokens { get; set; }
}

