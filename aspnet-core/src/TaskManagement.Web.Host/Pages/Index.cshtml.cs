using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;

namespace TaskManagement.Web.Pages;

public class IndexModel : TaskManagementPageModel
{
    public void OnGet()
    {

    }

    public async Task OnPostLoginAsync()
    {
        await HttpContext.ChallengeAsync("oidc");
    }
}
