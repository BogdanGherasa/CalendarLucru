using Microsoft.AspNetCore.Identity;

namespace TimeBlocker.Web.Models;

public class ApplicationUser : IdentityUser
{
    public ICollection<TimeBlock> TimeBlocks { get; set; } = new List<TimeBlock>();
}
