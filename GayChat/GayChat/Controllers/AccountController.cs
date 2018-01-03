using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using GayChat.Models;
using GayChat.Models.AccountViewModels;
using GayChat.Services;
using System.IO;
using GayChat.Models.ITCHat;
using System.Text;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace GayChat.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly ISmsSender _smsSender;
        private readonly ILogger _logger;
        private readonly string _externalCookieScheme;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IOptions<IdentityCookieOptions> identityCookieOptions,
            IEmailSender emailSender,
            ISmsSender smsSender,
            ILoggerFactory loggerFactory)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _externalCookieScheme = identityCookieOptions.Value.ExternalCookieAuthenticationScheme;
            _emailSender = emailSender;
            _smsSender = smsSender;
            _logger = loggerFactory.CreateLogger<AccountController>();
        }

        //
        // GET: /Account/Login
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string returnUrl = null)
        {
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.Authentication.SignOutAsync(_externalCookieScheme);

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, true, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    _logger.LogInformation(1, "User logged in.");
                    return RedirectToLocal(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToAction(nameof(SendCode), new { ReturnUrl = returnUrl, RememberMe = true });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning(2, "User account locked out.");
                    return View("Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpGet]
        public IActionResult AccountSettings()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public string IsAuthorized()
        {
            var result = HttpContext.User.Identity.IsAuthenticated ? "True" : "False";

            return result;
        }

        [HttpGet]
        public async Task<int> GetNumberOfFriends()
        {
            var current = await _userManager.GetUserAsync(HttpContext.User);

            return current.Friends.Where(e => e.Status == FriendStatus.Subscriber).ToList().Count;
        }

        [HttpGet]
        public IActionResult UserChat(string userID)
        {
            return View(model: userID);
        }

        [HttpGet]
        public async Task<string> GetNewMessages(string id)
        {
            var current = await _userManager.GetUserAsync(HttpContext.User);

            var chat = current.Chats.Find(e => e.UserId == id);

            var messages = "";
            var newmessages = chat.Messages.Where(e => e.IsNew == true);
            
            if (newmessages != null)
            {
                messages = JsonConvert.SerializeObject(newmessages);

                foreach (var message in newmessages)
                    message.IsNew = false;

                await _userManager.UpdateAsync(current);
            }
            
            return messages;
        }

        [HttpGet]
        public async Task SendMessage(string message, string userId)
        {
            var current = await _userManager.GetUserAsync(HttpContext.User);
            var user = await _userManager.FindByIdAsync(userId);
            
            current.Chats.Find(e => e.UserId == userId).Messages.Add(new Message() { MessageInner = message, FromMe = true, IsNew=false });

            if (user.Chats.Find(e => e.UserId == current.Id) == null)
                user.Chats.Add(new Chat() { UserFullname = current.FirstName + " " + current.Surname, UserId = current.Id, UserNickname = current.Nickname });

            user.Chats.Find(e => e.UserId == current.Id).Messages.Add(new Message() { MessageInner = message, FromMe = false, IsNew = true });

            await _userManager.UpdateAsync(current);
            await _userManager.UpdateAsync(user);
        }

        [HttpGet]
        public async Task<string> GetMessangesForChat(string userID)
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);

            var allmessanges = user.Chats.Find(e => e.UserId == userID);

            var messages = allmessanges.Messages;
            var newmessages = allmessanges.Messages.Where(e => e.IsNew == true);

            if (newmessages != null)
            {
                foreach (var message in newmessages)
                    message.IsNew = false;

                await _userManager.UpdateAsync(user);
            }

            return JsonConvert.SerializeObject(messages);
        }

        [HttpGet]
        public async Task<string> GetUserByID(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            return JsonConvert.SerializeObject(user);
        }

        [HttpPost]
        public async Task<ActionResult> StartChat(string userId)
        {
            var current = await _userManager.GetUserAsync(HttpContext.User);

            if (current.Chats.Find(e => e.UserId == userId) == null)
            {
                var user = await _userManager.FindByIdAsync(userId);

                current.Chats.Add(new Chat() { UserId = user.Id, UserFullname = user.FirstName + " " + user.Surname, UserNickname = user.Nickname });

                if (user.Chats.Find(e => e.UserId == current.Id) == null)
                {
                    user.Chats.Add(new Chat() { UserId = current.Id, UserFullname = current.FirstName + " " + current.Surname, UserNickname = current.Nickname });
                    await _userManager.UpdateAsync(user);
                }

                await _userManager.UpdateAsync(current);
            }

            return RedirectToAction("UserChat", new { userID = userId });
        }

        public async Task<string> GetChats()
        {
            var current = await _userManager.GetUserAsync(HttpContext.User);

            return JsonConvert.SerializeObject(current.Chats);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            string path = "";

            if (ModelState.IsValid)
            {
                if (model.Image != null)
                {
                    if (model.Image.Length > 0)
                    {
                        path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "UserImages", model.Username.Replace(" ", string.Empty) + ".jpg");

                        using (var fileStream = new FileStream(path, FileMode.Create))
                        {
                            model.Image.CopyTo(fileStream);
                        }
                    }
                }
                else
                {
                    path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "UserImages");

                    System.IO.File.Copy(Path.Combine(path, "DefaultUser.png"), Path.Combine(path, model.Username.Replace(" ", string.Empty) + ".jpg"));
                }

                var user = new ApplicationUser { UserName = model.Email, Email = model.Email, Nickname = "@" + model.Username, Surname = model.Surname, FirstName = model.FirstName };
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    // For more information on how to enable account confirmation and password reset please visit https://go.microsoft.com/fwlink/?LinkID=532713
                    // Send an email with this link
                    //var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    //var callbackUrl = Url.Action(nameof(ConfirmEmail), "Account", new { userId = user.Id, code = code }, protocol: HttpContext.Request.Scheme);
                    //await _emailSender.SendEmailAsync(model.Email, "Confirm your account",
                    //    $"Please confirm your account by clicking this link: <a href='{callbackUrl}'>link</a>");
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    _logger.LogInformation(3, "User created a new account with password.");
                    return RedirectToLocal(returnUrl);
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpPost]
        public async Task<ActionResult> ChangeAccountAvatar(IFormFile avatar)
        {
            if (avatar != null)
            {
                var current = await _userManager.FindByEmailAsync(HttpContext.User.Identity.Name);

                string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "UserImages", current.Nickname.Substring(1) + ".jpg");

                System.IO.File.Delete(path);

                using (var fileStream = new FileStream(path, FileMode.Create))
                {
                    avatar.CopyTo(fileStream);
                }

            }

            return RedirectToAction("AccountSettings");
        }

        [HttpPost]
        public async Task<IActionResult> AcceptInvite(string subscriberId)
        {
            var current = await _userManager.FindByEmailAsync(HttpContext.User.Identity.Name);

            if (current.Friends.Find(e => e.FriendId == subscriberId) != null)
            {
                var subscriber = await _userManager.FindByIdAsync(subscriberId);

                current.Friends.Find(e => e.FriendId == subscriberId).Status = FriendStatus.Accepted;
                subscriber.Friends.Find(e => e.FriendId == current.Id).Status = FriendStatus.Accepted;

                await _userManager.UpdateAsync(current);
                await _userManager.UpdateAsync(subscriber);
            }

            return RedirectToAction("Contacts", controllerName: "Account");
        }

        [HttpPost]
        public async Task Invite(string friendId)
        {
            var email = HttpContext.User.Identity.Name;

            var current = await _userManager.FindByEmailAsync(email).ConfigureAwait(false);

            var friend = current.Friends.Find(e => e.FriendId == friendId);

            if (friend == null)
            {
                var user = await _userManager.FindByIdAsync(friendId);

                current.Friends.Add(new Friend { FriendId = user.Id, Status = FriendStatus.Invited });
                user.Friends.Add(new Friend { FriendId = current.Id, Status = FriendStatus.Subscriber });

                await _userManager.UpdateAsync(current);
                await _userManager.UpdateAsync(user);
            }
            else if (friend.Status == FriendStatus.Subscriber)
            {
                var user = await _userManager.FindByIdAsync(friendId);

                friend.Status = FriendStatus.Accepted;
                user.Friends.Find(e => e.FriendId == current.Id).Status = FriendStatus.Accepted;

                await _userManager.UpdateAsync(current);
                await _userManager.UpdateAsync(user);
            }

        }

        [HttpGet]
        public IActionResult FindUsers()
        {
            return View();
        }

        [HttpGet]
        public async Task<string> UsersString()
        {
            var allusers = _userManager.Users.ToList();

            allusers.RemoveAll(e => e.Email == HttpContext.User.Identity.Name);

            var currentuser = await _userManager.GetUserAsync(HttpContext.User);
            List<FindUsersModel> users = new List<FindUsersModel>();

            foreach (var user in allusers)
            {
                var status = currentuser.GetStatusForFriendId(user.Id);

                var user_ = new FindUsersModel(status) { Id = user.Id, Nickname = user.Nickname, FullName = user.FirstName + " " + user.Surname };

                users.Add(user_);
            }

            var data = JsonConvert.SerializeObject(users);

            return data;
        }

        [HttpGet]
        public async Task RefreshUsers(string id)
        {
            var user = _userManager.Users.ToList()[int.Parse(id)];

            user.Chats = new List<Chat>();
            user.Friends = new List<Friend>();

            await _userManager.UpdateAsync(user);

            //foreach (var user in allusers)
            //{
            //    user.Chats = new List<Chat>();

            //    user.Friends = new List<Friend>();

            //    await _userManager.UpdateAsync(user);

            //    await Task.Delay(10000);
            //}
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetUsers()
        {
            if (User.Identity.Name == "danil.novikov.dev@gmail.com")
            {
                var users = _userManager.Users.ToList();

                return View(users);
            }

            return RedirectToAction("Login");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var removed = await _userManager.FindByIdAsync(id);

            foreach (var friend in removed.Friends)
            {
                var friend_ = await _userManager.FindByIdAsync(friend.FriendId);

                friend_.Friends.RemoveAll(e => e.FriendId == id);

                await _userManager.UpdateAsync(friend_);
            }

            await _userManager.DeleteAsync(removed);

            return RedirectToAction("GetUsers");
        }

        [HttpGet]
        public async Task<IActionResult> Contacts()
        {
            var user = await _userManager.FindByEmailAsync(HttpContext.User.Identity.Name);

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation(4, "User logged out.");
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string returnUrl = null)
        {
            // Request a redirect to the external login provider.
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { ReturnUrl = returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            if (remoteError != null)
            {
                ModelState.AddModelError(string.Empty, $"Error from external provider: {remoteError}");
                return View(nameof(Login));
            }
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return RedirectToAction(nameof(Login));
            }

            // Sign in the user with this external login provider if the user already has a login.
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false);
            if (result.Succeeded)
            {
                _logger.LogInformation(5, "User logged in with {Name} provider.", info.LoginProvider);
                return RedirectToLocal(returnUrl);
            }
            if (result.RequiresTwoFactor)
            {
                return RedirectToAction(nameof(SendCode), new { ReturnUrl = returnUrl });
            }
            if (result.IsLockedOut)
            {
                return View("Lockout");
            }
            else
            {
                // If the user does not have an account, then ask the user to create an account.
                ViewData["ReturnUrl"] = returnUrl;
                ViewData["LoginProvider"] = info.LoginProvider;
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { Email = email });
            }
        }

        //
        // POST: /Account/ExternalLoginConfirmation
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                // Get the information about the user from the external login provider
                var info = await _signInManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    return View("ExternalLoginFailure");
                }
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await _userManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await _userManager.AddLoginAsync(user, info);
                    if (result.Succeeded)
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        _logger.LogInformation(6, "User created an account using {Name} provider.", info.LoginProvider);
                        return RedirectToLocal(returnUrl);
                    }
                }
                AddErrors(result);
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        // GET: /Account/ConfirmEmail
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return View("Error");
            }
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return View("Error");
            }
            var result = await _userManager.ConfirmEmailAsync(user, code);
            return View(result.Succeeded ? "ConfirmEmail" : "Error");
        }

        //
        // GET: /Account/ForgotPassword
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        //
        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return View("ForgotPasswordConfirmation");
                }

                // For more information on how to enable account confirmation and password reset please visit https://go.microsoft.com/fwlink/?LinkID=532713
                // Send an email with this link
                //var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                //var callbackUrl = Url.Action(nameof(ResetPassword), "Account", new { userId = user.Id, code = code }, protocol: HttpContext.Request.Scheme);
                //await _emailSender.SendEmailAsync(model.Email, "Reset Password",
                //   $"Please reset your password by clicking here: <a href='{callbackUrl}'>link</a>");
                //return View("ForgotPasswordConfirmation");
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ForgotPasswordConfirmation
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        //
        // GET: /Account/ResetPassword
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string code = null)
        {
            return code == null ? View("Error") : View();
        }

        //
        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction(nameof(AccountController.ResetPasswordConfirmation), "Account");
            }
            var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction(nameof(AccountController.ResetPasswordConfirmation), "Account");
            }
            AddErrors(result);
            return View();
        }

        //
        // GET: /Account/ResetPasswordConfirmation
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        //
        // GET: /Account/SendCode
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult> SendCode(string returnUrl = null, bool rememberMe = false)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return View("Error");
            }
            var userFactors = await _userManager.GetValidTwoFactorProvidersAsync(user);
            var factorOptions = userFactors.Select(purpose => new SelectListItem { Text = purpose, Value = purpose }).ToList();
            return View(new SendCodeViewModel { Providers = factorOptions, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/SendCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendCode(SendCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return View("Error");
            }

            // Generate the token and send it
            var code = await _userManager.GenerateTwoFactorTokenAsync(user, model.SelectedProvider);
            if (string.IsNullOrWhiteSpace(code))
            {
                return View("Error");
            }

            var message = "Your security code is: " + code;
            if (model.SelectedProvider == "Email")
            {
                await _emailSender.SendEmailAsync(await _userManager.GetEmailAsync(user), "Security Code", message);
            }
            else if (model.SelectedProvider == "Phone")
            {
                await _smsSender.SendSmsAsync(await _userManager.GetPhoneNumberAsync(user), message);
            }

            return RedirectToAction(nameof(VerifyCode), new { Provider = model.SelectedProvider, ReturnUrl = model.ReturnUrl, RememberMe = true });
        }

        //
        // GET: /Account/VerifyCode
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyCode(string provider, bool rememberMe = true, string returnUrl = null)
        {
            // Require that the user has already logged in via username/password or external login
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return View("Error");
            }
            return View(new VerifyCodeViewModel { Provider = provider, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/VerifyCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyCode(VerifyCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // The following code protects for brute force attacks against the two factor codes.
            // If a user enters incorrect codes for a specified amount of time then the user account
            // will be locked out for a specified amount of time.
            var result = await _signInManager.TwoFactorSignInAsync(model.Provider, model.Code, model.RememberMe, model.RememberBrowser);
            if (result.Succeeded)
            {
                return RedirectToLocal(model.ReturnUrl);
            }
            if (result.IsLockedOut)
            {
                _logger.LogWarning(7, "User account locked out.");
                return View("Lockout");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid code.");
                return View(model);
            }
        }

        //
        // GET: /Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        #region Helpers

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }

        #endregion
    }
}
