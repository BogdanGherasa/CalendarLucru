using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TimeBlocker.Web.Models;

namespace TimeBlocker.Web.Pages.Account;

public class RegisterModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public RegisterModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Emailul este obligatoriu.")]
        [EmailAddress(ErrorMessage = "Adresa de email nu este validă.")]
        [StringLength(256, ErrorMessage = "Emailul poate avea maxim {1} caractere.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Parola este obligatorie.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Parola trebuie să aibă între {2} și {1} caractere.")]
        [DataType(DataType.Password)]
        [Display(Name = "Parolă")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirmarea parolei este obligatorie.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Parolele nu coincid.")]
        [Display(Name = "Confirmă parola")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public IActionResult OnGet()
    {
        if (_signInManager.IsSignedIn(User))
        {
            return LocalRedirect("/");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (_signInManager.IsSignedIn(User))
        {
            return LocalRedirect("/");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var email = Input.Email.Trim();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email
        };

        var result = await _userManager.CreateAsync(user, Input.Password);
        if (result.Succeeded)
        {
            await _signInManager.SignInAsync(user, isPersistent: false);
            return LocalRedirect("/");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, Translate(error));
        }

        return Page();
    }

    private static string Translate(IdentityError error) => error.Code switch
    {
        nameof(IdentityErrorDescriber.DuplicateEmail) => "Există deja un cont cu această adresă de email.",
        nameof(IdentityErrorDescriber.DuplicateUserName) => "Există deja un cont cu această adresă de email.",
        nameof(IdentityErrorDescriber.InvalidEmail) => "Adresa de email nu este validă.",
        nameof(IdentityErrorDescriber.InvalidUserName) => "Adresa de email conține caractere nepermise.",
        nameof(IdentityErrorDescriber.PasswordTooShort) => "Parola trebuie să aibă cel puțin 8 caractere.",
        nameof(IdentityErrorDescriber.PasswordRequiresDigit) => "Parola trebuie să conțină cel puțin o cifră.",
        nameof(IdentityErrorDescriber.PasswordRequiresLower) => "Parola trebuie să conțină cel puțin o literă mică.",
        nameof(IdentityErrorDescriber.PasswordRequiresUpper) => "Parola trebuie să conțină cel puțin o literă mare.",
        nameof(IdentityErrorDescriber.PasswordRequiresNonAlphanumeric) => "Parola trebuie să conțină cel puțin un caracter special.",
        nameof(IdentityErrorDescriber.PasswordRequiresUniqueChars) => "Parola trebuie să conțină mai multe caractere distincte.",
        _ => "Contul nu a putut fi creat. Verifică datele introduse."
    };
}
