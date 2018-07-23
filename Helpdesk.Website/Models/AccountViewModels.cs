using Helpdesk.Website.Controllers;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Connector;
using Microsoft.Owin.Security.DataProtection;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Helpdesk.Website.Models
{
    public class ExternalLoginConfirmationViewModel
    {
        [Required]
        [Display(Name = "Email")]
        public string Email { get; set; }
    }


    public class ExternalLoginListViewModel
    {
        public string ReturnUrl { get; set; }
    }

    public class SendCodeViewModel
    {
        public string SelectedProvider { get; set; }
        public ICollection<System.Web.Mvc.SelectListItem> Providers { get; set; }
        public string ReturnUrl { get; set; }
        public bool RememberMe { get; set; }
    }

    public class VerifyCodeViewModel
    {
        [Required]
        public string Provider { get; set; }

        [Required]
        [Display(Name = "Code")]
        public string Code { get; set; }
        public string ReturnUrl { get; set; }

        [Display(Name = "Remember this browser?")]
        public bool RememberBrowser { get; set; }

        public bool RememberMe { get; set; }
    }

    public class ForgotViewModel
    {
        [Required]
        [Display(Name = "Email")]
        public string Email { get; set; }
    }

    [Serializable]

    public class LoginViewModel
    {
        [Required]
        [Display(Name = "Email")]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }



        public static IForm<LoginViewModel> BuildLogInForm()
        {


            return new FormBuilder<LoginViewModel>()
                .Message("To login fill out these details")
            .OnCompletion(async (context, botUser) =>
            {
                // Set BotUserData
                context.PrivateConversationData.SetValue<string>(
                    "Email", botUser.Email);
                context.PrivateConversationData.SetValue<string>(
                "Password", botUser.Password);

                var manager = RegisterViewModel.GetUserManager();
                var loggedInUser = CheckIfLoggedIn(context.Activity.Conversation.Id);
                if (loggedInUser == false){
                    try
                    {
                        var signInManager = HttpContext.Current.GetOwinContext().GetUserManager<ApplicationSignInManager>();
                        var result = await signInManager.PasswordSignInAsync(botUser.Email, botUser.Password, botUser.RememberMe, shouldLockout: false);

                        if (result.ToString().ToLower().Equals(SignInStatus.Success.ToString().ToLower()))
                        {
                            await context.PostAsync($"Hi,{botUser.Email}! You are logged in.");
                            try
                            {
                                LogIn(botUser, context.Activity.Conversation.Id);
                                
                            }
                            catch (Exception e)
                            {
                                await context.PostAsync($"{e}");
                            }

                        }
                        else
                        {
                            await context.PostAsync("Wrong combination of username and password.");
                        }
                    }
                    catch (Exception e)
                    {
                        await context.PostAsync($"{e}");
                    }
                }
                else
                {
                    await context.PostAsync("A user is already logged in on this device");
                }
                

            }).Build();

        }

        public static ApplicationDbContext GetApplicationDb()
        {
            ApplicationDbContext applicationDb = new ApplicationDbContext();
            return applicationDb;
        }

        public static async void LogOut(string convCode)
        {
            var db = GetApplicationDb();
            var loggedInCount = db.LoggedInUsers.Where(x => x.ConversationCode.Equals(convCode)).Count();
            var manager = RegisterViewModel.GetUserManager();
            if(loggedInCount > 0)
            {
                var loggedIn = db.LoggedInUsers.Where(x => x.ConversationCode.Equals(convCode)).FirstOrDefault();
                var user = await manager.FindByNameAsync(loggedIn.LoggedInUserEmail);
                db.LoggedInUsers.Remove(loggedIn);
                db.SaveChanges();
            }
            
        }


        public static async void LogIn(LoginViewModel AppUser, string convCode)
        {
            var manager = RegisterViewModel.GetUserManager();
            var user = await manager.FindByNameAsync(AppUser.Email);
            LoggedInUser loggedIn = new LoggedInUser() { LoggedInUserEmail = AppUser.Email, ConversationCode = convCode };
            var db = GetApplicationDb();
            db.LoggedInUsers.Add(loggedIn);
            db.SaveChanges();
        }

        public static string LoggedInAs(string convCode)
        {
            var db = GetApplicationDb();
            

            if (db.LoggedInUsers.Where(x => x.ConversationCode.Equals(convCode)).Count() == 0)
            {
                var empty = "You are not logged in";
                return empty;
            }
            else
            {
                var loggedIn = db.LoggedInUsers.Where(x => x.ConversationCode.Equals(convCode)).FirstOrDefault();
                var notEmpty = "You are logged in as: " + loggedIn.LoggedInUserEmail;
                return notEmpty;
            }
        }

        public static ApplicationUser LoggedInAsUser(string convCode)
        {
            var db = GetApplicationDb();


            if (db.LoggedInUsers.Where(x => x.ConversationCode.Equals(convCode)).Count() == 0)
            {
                return null;
            }
            else
            {
                var loggedIn = db.LoggedInUsers.Where(x => x.ConversationCode.Equals(convCode)).FirstOrDefault();
                var manager = RegisterViewModel.GetUserManager();
                var user = manager.FindByEmail(loggedIn.LoggedInUserEmail);
                if(user == null)
                {
                    var fakeUser = new ApplicationUser() { Email = "User not found"};
                    return fakeUser;
                }
                else
                {
                    return user;
                }
                
            }
        }

        public static bool CheckIfLoggedIn(string convCode)
        {
            var db = GetApplicationDb();


            if (db.LoggedInUsers.Where(x => x.ConversationCode.Equals(convCode)).Count() == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

    }

    [Serializable]
    public class RegisterViewModel
    {

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        [Prompt("What's your email?")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        [Prompt("Please provide your password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [System.ComponentModel.DataAnnotations.Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        [Prompt("Confirm password")]
        public string ConfirmPassword { get; set; }


        public static IForm<RegisterViewModel> BuildUserForm()
        {

            //BotUser botUser = new BotUser();
            return new FormBuilder<RegisterViewModel>()
                .Message("To complete account registration please fill out these items")
            .OnCompletion(async (context, botUser) =>
            {
                // Set BotUserData
                context.PrivateConversationData.SetValue<string>(
                    "Email", botUser.Email);
                context.PrivateConversationData.SetValue<string>(
                "Password", botUser.Password);
                context.PrivateConversationData.SetValue<string>(
                    "ConfirmPassword", botUser.ConfirmPassword);
                if (IsValidEmail(botUser.Email))
                {


                    try
                    {
                        var userManager = GetUserManager();
                        var user = new ApplicationUser { UserName = botUser.Email, Email = botUser.Email };
                        var result = await userManager.CreateAsync(user, botUser.Password);


                        if (result.Succeeded)
                        {
                            CardAction btnAiHelpWebsite = new CardAction()
                            {
                                Type = "openUrl",
                                Title = "Log in here",
                                Value = "http://helpdeskharrywebsite.azurewebsites.net/Account/Login"
                            };
                            string strNumberGuesserOpeningCard = "https://t3.ftcdn.net/jpg/00/35/05/98/240_F_35059814_35v530XQqkaFnc1fRBV2nfO2WvkzBi25.jpg";
                            List<CardImage> cardImages1 = new List<CardImage>();
                            cardImages1.Add(new CardImage(url: strNumberGuesserOpeningCard));

                            // Finally create the Hero Card
                            // adding the image and the CardAction
                            HeroCard plCard1 = new HeroCard()
                            {
                                Title = "Please follow this link to log in",
                                Subtitle = "Log in",
                                Tap = btnAiHelpWebsite
                            };

                            Activity replyToConversation = (Activity)context.MakeMessage();
                            replyToConversation.Recipient = replyToConversation.Recipient;
                            replyToConversation.Type = "message";

                            Attachment plAttachment = plCard1.ToAttachment();
                            // Attach the Attachment to the reply
                            replyToConversation.Attachments.Add(plAttachment);
                            // set the AttachmentLayout as 'list'
                            replyToConversation.AttachmentLayout = "list";

                            // Send the reply
                            await context.PostAsync(replyToConversation);
                            await context.PostAsync("http://helpdeskharrywebsite.azurewebsites.net/Account/Login");
                        }
                        else
                        {
                            foreach (var error in result.Errors)
                            {
                                await context.PostAsync($"Registration failed. Error: {error}");
                            }

                        }
                    }
                    catch (Exception e)
                    {
                        await context.PostAsync($"Your registration went wrong, please try again. Error: {e}");
                    }
                }

                else if (!botUser.Password.Equals(botUser.ConfirmPassword))
                {
                    await context.PostAsync("Passwords don't match, please try again");
                }
                else
                {
                    await context.PostAsync($"{botUser.Email} is not a valid email address. Please ");
                }

                context.Done<object>(new object());

            })
            .Build();


        }

        public static ApplicationUserManager GetUserManager()
        {

            var userManager = new ApplicationUserManager(new UserStore<ApplicationUser>(new ApplicationDbContext())); 
            return userManager;

        }

        public static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }

    public class ResetPasswordViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [System.ComponentModel.DataAnnotations.Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        [Display(Name = "Token password")]
        public string TokenPassword { get; set; }

    }


    [Serializable]
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        [Prompt("Please provide the email you signed up with")]
        public string Email { get; set; }


        public static ApplicationUserManager GetUserManager()
        {
            var userManager = HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>();
            return userManager;
        }


        public static IForm<ForgotPasswordViewModel> BuildFormForgotPassword()
        {


            var url = new UrlHelper(HttpContext.Current.Request.RequestContext);



            return new FormBuilder<ForgotPasswordViewModel>()
                .Message("To reset your password, please fill out this form")
            .OnCompletion(async (context, botUser) =>
            {
                // Set BotUserData
                context.PrivateConversationData.SetValue<string>(
                    "Email", botUser.Email);

                var manager = GetUserManager();


                if (RegisterViewModel.IsValidEmail(botUser.Email))
                {
                    var user = await manager.FindByNameAsync(botUser.Email);
                    if (user == null)
                    {
                        // Don't reveal that the user does not exist or is not confirmed
                        await context.PostAsync("Please Try again");

                    }

                    // For more information on how to enable account confirmation and password reset please visit https://go.microsoft.com/fwlink/?LinkID=320771
                    else
                    {
                        // Send an email with this link
                        try
                        {

                            user.TokenPassword = GenerateToken(6);
                            await manager.UpdateAsync(user);
                            var input = "http://helpdeskharrywebsite.azurewebsites.net/Account/ResetPassword?userId=" + user.Id;

                            await manager.SendEmailAsync(user.Id, "Reset Password", "Please reset your password by clicking <a href=\"" + input + "\"> here </a> </br> This is your registration token: " + user.TokenPassword);

                        }
                        catch (Exception e)
                        {
                            await context.PostAsync($"{e}");
                        }
                        await context.PostAsync($"The password recovery email has been sent to: {botUser.Email}");
                    }
                }
                else
                {
                    await context.PostAsync($"{botUser.Email} is not a valid email");
                }


            }).Build();



        }

        public static string GenerateToken(int length)
        {
            RNGCryptoServiceProvider cryptRNG = new RNGCryptoServiceProvider();
            byte[] tokenBuffer = new byte[length];
            cryptRNG.GetBytes(tokenBuffer);
            return Convert.ToBase64String(tokenBuffer);

        }
    }


    public interface IContextWrapper
    {
        HttpContextBase Current { get; }
        HttpRequestBase Request { get; }
        HttpResponseBase Response { get; }
        HttpServerUtilityBase Server { get; }
    }

    public class ContextWrapper : IContextWrapper
    {
        public HttpContextBase Current
        {
            get
            {
                var httpContext = HttpContext.Current;
                return httpContext == null ? null : new HttpContextWrapper(httpContext);
            }
        }

        public HttpRequestBase Request
        {
            get { return Current.Request; }
        }

        public HttpResponseBase Response
        {
            get { return Current.Response; }
        }

        public HttpServerUtilityBase Server
        {
            get { return Current.Server; }
        }

    }

    [Serializable]
    public class RegisterTriggerCMDModel
    {
        [Required]
        [Display(Name = "Computer name")]
        public string UserComputerName { get; set; }

        [Required]
        [Display(Name = "TriggerCMD Token")]
        public string UserTriggerCMDToken { get; set; }

        [Required]
        [Display(Name = "Pincode")]
        public string UserCode { get; set; }



        public static IForm<RegisterTriggerCMDModel> BuildTriggerCMDRegistration()
        {


            return new FormBuilder<RegisterTriggerCMDModel>()
                .Message("Fill out this information to register TriggerCMD")
            .OnCompletion(async (context, botUser) =>
            {
                // Set BotUserData
                context.PrivateConversationData.SetValue<string>(
                    "UserComputerName", botUser.UserComputerName);
                context.PrivateConversationData.SetValue<string>(
                "UserTriggerCMDToken", botUser.UserTriggerCMDToken);
                context.PrivateConversationData.SetValue<string>(
                    "UserCode", botUser.UserCode);

                var user = LoginViewModel.LoggedInAsUser(context.Activity.Conversation.Id);

                if (CheckIfTriggerCMDIsRegistered(user) == false)
                {
                    RegisterTriggerCMD(botUser, user);
                }
                else
                {
                    await context.PostAsync("You already have registered a TriggerCMD account");
                }

            }).Build();

        }

        public static async void RegisterTriggerCMD(RegisterTriggerCMDModel AppUser, ApplicationUser user)
        {
            var manager = RegisterViewModel.GetUserManager();
            var final_user = await manager.FindByEmailAsync(user.Email);
            final_user.UserComputerName = AppUser.UserComputerName;
            final_user.UserTriggerCMDToken = AppUser.UserTriggerCMDToken;
            await manager.UpdateAsync(final_user);
            
        }

        public static bool CheckIfTriggerCMDIsRegistered(ApplicationUser AppUser)
        {

            if (string.IsNullOrEmpty(AppUser.UserComputerName) || string.IsNullOrEmpty(AppUser.UserTriggerCMDToken))
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }

      
}
