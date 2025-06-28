using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using InstantVoiceRoom.Framework.Services;
using InstantVoiceRoom.Models;

namespace InstantVoiceRoom.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly FileUserStore _store;
        public LoginModel(FileUserStore store) => _store = store;

        [BindProperty]
        public LoginViewModel Input { get; set; } = new();

        public string ReturnUrl { get; set; }

        public void OnGet(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            if (!ModelState.IsValid)
                return Page();

            if (!_store.ValidateCredentials(Input.UserName, Input.Password))
            {
                ModelState.AddModelError("", "Invalid login");
                return Page();
            }

            var claims = new[] { new Claim(ClaimTypes.Name, Input.UserName) };
            var id = new ClaimsIdentity(claims, 
                     CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                                         new ClaimsPrincipal(id));

            return LocalRedirect(returnUrl ?? "/");
        }
    }
}
