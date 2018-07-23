using System;
using System.Configuration;
using System.Threading.Tasks;

using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Builder.FormFlow;
using Helpdesk.Website.Models;
using System.Web.Mvc;
using System.Web;
using System.Threading;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Net;
using System.Text;
using System.IO;
using System.Web.Script.Serialization;
using Microsoft.Bot.Connector;

namespace Microsoft.Bot.Sample.LuisBot
{
    // For more information about this template visit http://aka.ms/azurebots-csharp-luis
    [LuisModel("826dfb7d-1d4a-4e6e-b916-ecb0425aea25", "908518d00733442da183098c31d1c690")]
    [Serializable]
    public class BasicLuisDialog : LuisDialog<object>
    {
        public BasicLuisDialog()
        {
           
        }


        [LuisIntent("None")]
        public async Task NoneIntent(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("I am sorry I don't get what you're saying");
            context.Wait(this.MessageReceived);
        }

        // Go to https://luis.ai and create a new intent, then train/publish your luis app.
        // Finally replace "Gretting" with the name of your newly created intent in the following handler
        [LuisIntent("Greeting")]
        public async Task GreetingIntent(IDialogContext context, LuisResult result)
        {
            Random rnd = new Random();
            int number = rnd.Next(1, 5);
            if (number == 1)
            {
                await context.PostAsync("Hello! What I can do for you?");
                context.Wait(this.MessageReceived);
            }
            else if (number == 2)
            {
                await GreetTheUser(context, result);
            }
            else if (number == 3)
            {
                await context.PostAsync("Hi there! How can I help you ?");
                context.Wait(this.MessageReceived);
            }
          


            context.Done<object>(new object());

        }

        [LuisIntent("LogIn")]
        public async Task LogInIntent(IDialogContext context, LuisResult result)
        {
            var myform = new FormDialog<LoginViewModel>(new LoginViewModel(), LoginViewModel.BuildLogInForm, FormOptions.PromptInStart, null);

            

            context.Call<LoginViewModel>(myform, ResumeAfterDialog);

        }

        [LuisIntent("Register")]
        public async Task RegisterIntent(IDialogContext context, LuisResult result)
        {
            var myform = new FormDialog<RegisterViewModel>(new RegisterViewModel(), RegisterViewModel.BuildUserForm, FormOptions.PromptInStart, null);

            context.Call<RegisterViewModel>(myform, ResumeAfterDialog);

        }

        [LuisIntent("RegisterTriggerCMD")]
        public async Task RegisterTriggerCMDIntent(IDialogContext context, LuisResult result)
        {
            var myform = new FormDialog<RegisterTriggerCMDModel>(new RegisterTriggerCMDModel(), RegisterTriggerCMDModel.BuildTriggerCMDRegistration, FormOptions.PromptInStart, null);

            context.Call<RegisterTriggerCMDModel>(myform, ResumeAfterDialog);

        }
        

        [LuisIntent("LogOut")]
        public async Task LogOutIntent(IDialogContext context, LuisResult result)
        {

            if (LoginViewModel.CheckIfLoggedIn(context.Activity.Conversation.Id) == false)
            {
                await context.PostAsync("You are not logged in");
            }
            else
            {
                LoginViewModel.LogOut(context.Activity.Conversation.Id);
                await context.PostAsync("You are logged out");
            }
            

        }

        [LuisIntent("LoggedInAs")]
        public async Task LoggedInAsIntent(IDialogContext context, LuisResult result)
        {
            var LoggedInAs = LoginViewModel.LoggedInAs(context.Activity.Conversation.Id);
            await context.PostAsync($"{LoggedInAs}");
        }

        private async Task ResumeAfterDialog(IDialogContext context, IAwaitable<object> result)
        {
            await context.PostAsync("Is there anything else I can help you with?");
        }

        [LuisIntent("Help")]
        public async Task HelpIntent(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("You can ask me to register for this amazing website that has no name. And if you were smart enough to forget your password I can help you reset it. Also you can say Hi, I can be quite nice. I also know one whole fact.. don't judge me I'm just graduation project... okay?!");
            context.Wait(this.MessageReceived);
        }

        [LuisIntent("Fact")]
        public async Task FactIntent(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("Bangin your head into a wall bruns up to 130 calories an hour!");
            context.Wait(this.MessageReceived);
        }

        [LuisIntent("Weather")]
        public async Task WeatherIntent(IDialogContext context, LuisResult result)
        {
            //await context.PostAsync("I don't know, look outside. If that doesn't help visit: http://buienradar.nl");
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    //Assuming that the api takes the user message as a query paramater
                    string RequestURI = "http://api.openweathermap.org/data/2.5/weather?q=" + result.Entities[0].Entity + "&APPID=d0dbc0e12f39972002314d6c2bebd042&units=metric";
                    HttpResponseMessage responsemMsg = await client.GetAsync(RequestURI);
                    if (responsemMsg.IsSuccessStatusCode)
                    {
                        var apiResponse = await responsemMsg.Content.ReadAsStringAsync();
                        dynamic weather = JObject.Parse(apiResponse);
                        var description = weather.weather[0].description;
                        var degrees = weather.main.temp;
                        var cityName = weather.name;


                        //Post the API response to bot again
                        await context.PostAsync($"In {cityName} there is {description} and it is {degrees} degrees Celcius");

                    }
                }
            }
            catch (Exception ex)
            {
                await context.PostAsync($"{ex} and the entity is: {result.Entities[0].Entity}");
            }
            context.Wait(this.MessageReceived);
        }

        private async Task ContinueRestart(IDialogContext context, IAwaitable<bool> result)
        {
            bool isSure = await result;
            string response = isSure ? "We could check for virusses" : "We could restart your computer";
            IMessageActivity messageActivity = context.MakeMessage();
            if (isSure == true)
            {
                if (LoginViewModel.CheckIfLoggedIn(context.Activity.Conversation.Id) == true)
                {
                    ApplicationUser user = new ApplicationUser();
                    user = LoginViewModel.LoggedInAsUser(context.Activity.Conversation.Id);
                    var program = "Reboot PC";
                    if (RegisterTriggerCMDModel.CheckIfTriggerCMDIsRegistered(user) == true)
                    {
                        MakeAPICall(user.UserComputerName, user.UserTriggerCMDToken, program, context);
                    }
                    else
                    {
                        await context.PostAsync("You did not sign up with TriggerCMD");
                    }
                }
                else
                {
                    await context.PostAsync("You are not logged in");
                }
            }
            else
            {
                await CheckAntiVirus(context);
            }
        }

        private async Task ContinueAntiVirus(IDialogContext context, IAwaitable<bool> result)
        {
            bool isSure = await result;
            string response = isSure ? "We could check for virusses" : "We could restart your computer";
            IMessageActivity messageActivity = context.MakeMessage();
            if (isSure == false)
            {
                if (LoginViewModel.CheckIfLoggedIn(context.Activity.Conversation.Id) == true)
                {
                    ApplicationUser user = new ApplicationUser();
                    user = LoginViewModel.LoggedInAsUser(context.Activity.Conversation.Id);
                    var program = "Antivirus";
                    if (RegisterTriggerCMDModel.CheckIfTriggerCMDIsRegistered(user) == true)
                    {
                        MakeAPICall(user.UserComputerName, user.UserTriggerCMDToken, program, context);
                    }
                    else
                    {
                        await context.PostAsync("You did not sign up with TriggerCMD");
                    }
                }
                else
                {
                    await context.PostAsync("You are not logged in");
                }
            }
            else
            {
                await context.PostAsync("I am sorry there is nothing we can do for you");
            }
            
        }

        private async Task ContinueAfterConfirm(IDialogContext context, IAwaitable<bool> result)
        {
            bool isSure = await result;
            string response = isSure ? "We could check for virusses" : "We could restart your computer";
            IMessageActivity messageActivity = context.MakeMessage();
            if(isSure == false)
            {
                await CheckRestartPC(context);
            }
            else
            {
                await CheckAntiVirus(context);
            }

        }

        public async Task CheckAntiVirus(IDialogContext context)
        {


            PromptDialog.Confirm(context, ContinueAntiVirus, "Did you check your computer for virusses?", "Sorry what was that?");
        }

        public async Task CheckRestartPC(IDialogContext context)
        {
            PromptDialog.Confirm(context, ContinueRestart, "Do you want to restart it now? (You will have to save all your work before answering)", "Sorry what was that?");
        }

        [LuisIntent("SlowPC")]
        public async Task RestartPC(IDialogContext context, LuisResult result)
        {
            PromptDialog.Confirm(context, ContinueAfterConfirm, "Did you already restart your computer?", "Sorry what was that?");
        }


        [LuisIntent("RebootPC")]
        public async Task RestartPCIntent(IDialogContext context, LuisResult result)
        {
            try
            {
                if (LoginViewModel.CheckIfLoggedIn(context.Activity.Conversation.Id) == true)
                {
                    ApplicationUser user = new ApplicationUser();
                    user = LoginViewModel.LoggedInAsUser(context.Activity.Conversation.Id);
                    if (RegisterTriggerCMDModel.CheckIfTriggerCMDIsRegistered(user) == true)
                    {

                        MakeAPICall(user.UserComputerName, user.UserTriggerCMDToken, "Reboot Pc", context);

                    }
                    else
                    {
                        await context.PostAsync("Please register the TriggerCMD details");
                    }

                }

            }
            catch (Exception ex)
            {
                await context.PostAsync($"{ex}");
            }
            context.Wait(this.MessageReceived);
        }


        [LuisIntent("TrunOffPC")]
        public async Task TrunOffPCIntent(IDialogContext context, LuisResult result)
        {
            try
            {
                if (LoginViewModel.CheckIfLoggedIn(context.Activity.Conversation.Id) == true)
                {
                    ApplicationUser user = new ApplicationUser();
                    user = LoginViewModel.LoggedInAsUser(context.Activity.Conversation.Id);
                    if (RegisterTriggerCMDModel.CheckIfTriggerCMDIsRegistered(user) == true)
                    {

                        MakeAPICall(user.UserComputerName, user.UserTriggerCMDToken, "Shutoff", context);

                    }
                    else
                    {
                        await context.PostAsync("Please register the TriggerCMD details");
                    }

                }

            }
            catch (Exception ex)
            {
                await context.PostAsync($"{ex}");
            }
            context.Wait(this.MessageReceived);
        }


        [LuisIntent("AbortShutDown")]
        public async Task AbortShutDownPCIntent(IDialogContext context, LuisResult result)
        {
            try
            {
                if (LoginViewModel.CheckIfLoggedIn(context.Activity.Conversation.Id) == true)
                {
                    ApplicationUser user = new ApplicationUser();
                    user = LoginViewModel.LoggedInAsUser(context.Activity.Conversation.Id);
                    if (RegisterTriggerCMDModel.CheckIfTriggerCMDIsRegistered(user) == true)
                    {

                        MakeAPICall(user.UserComputerName, user.UserTriggerCMDToken, "Abortshutoff", context);

                    }
                    else
                    {
                        await context.PostAsync("Please register the TriggerCMD details");
                    }

                }

            }
            catch (Exception ex)
            {
                await context.PostAsync($"{ex}");
            }
            context.Wait(this.MessageReceived);
        }

        [LuisIntent("LocKPC")]
        public async Task LockPCIntent(IDialogContext context, LuisResult result)
        {
            try
            {
                if (LoginViewModel.CheckIfLoggedIn(context.Activity.Conversation.Id) == true)
                {
                    ApplicationUser user = new ApplicationUser();
                    user = LoginViewModel.LoggedInAsUser(context.Activity.Conversation.Id);
                    if (RegisterTriggerCMDModel.CheckIfTriggerCMDIsRegistered(user) == true)
                    {

                        MakeAPICall(user.UserComputerName, user.UserTriggerCMDToken, "Lock my PC", context);

                    }
                    else
                    {
                        await context.PostAsync("Please register the TriggerCMD details");
                    }

                }

            }
            catch (Exception ex)
            {
                await context.PostAsync($"{ex}");
            }
            context.Wait(this.MessageReceived);
        }

        [LuisIntent("TurnOnPC")]
        public async Task TurnOnPCIntent(IDialogContext context, LuisResult result)
        {

            try
            {
                if (LoginViewModel.CheckIfLoggedIn(context.Activity.Conversation.Id) == true)
                {
                    ApplicationUser user = new ApplicationUser();
                    user = LoginViewModel.LoggedInAsUser(context.Activity.Conversation.Id);
                    if (RegisterTriggerCMDModel.CheckIfTriggerCMDIsRegistered(user) == true)
                    {
                       
                        MakeAPICall( "odroid", user.UserTriggerCMDToken, "Wakeup", context);
                    
                    }
                    else
                    {
                        await context.PostAsync("Please register the TriggerCMD details");
                    }

                }

            }
            catch (Exception ex)
            {
                await context.PostAsync($"{ex}");
            }
            context.Wait(this.MessageReceived);
        }


        [LuisIntent("ApiCall")]
        public async Task GenericAPICallIntent(IDialogContext context, LuisResult result)
        {

            try
            {
                if(LoginViewModel.CheckIfLoggedIn(context.Activity.Conversation.Id) == true)
                {
                    ApplicationUser user = new ApplicationUser();
                    user = LoginViewModel.LoggedInAsUser(context.Activity.Conversation.Id);
                    if (RegisterTriggerCMDModel.CheckIfTriggerCMDIsRegistered(user) == true)
                        {
                        if(!string.IsNullOrEmpty(result.Entities[0].Entity)) {
                            var program = new System.Globalization.CultureInfo("en-US", false).TextInfo.ToTitleCase(result.Entities[0].Entity.ToLower());
                            MakeAPICall(user.UserComputerName, user.UserTriggerCMDToken, program, context);
                        }
                        else
                        {
                            await context.PostAsync("This program can't be opened, try another program, or check your spelling");
                        }
                    }
                    else
                    {
                        await context.PostAsync("Please register the TriggerCMD details");
                    }

                }

            }
            catch (Exception ex)
            {
                await context.PostAsync($"{ex}");
            }
            context.Wait(this.MessageReceived);
        }

        [LuisIntent("LostPassword")]
        public async Task LostPasswordIntent(IDialogContext context, LuisResult result)
        {
            var myform = new FormDialog<ForgotPasswordViewModel>(new ForgotPasswordViewModel(), ForgotPasswordViewModel.BuildFormForgotPassword, FormOptions.PromptInStart, null);

            context.Call<ForgotPasswordViewModel>(myform, ResumeAfterDialog);
   
        }


        public async Task GreetTheUser(IDialogContext context, LuisResult result)
        {
            var now = DateTime.Now.Hour;
            var genericMessage = "you've reached the Helpdesk, what can I do for you ?";
            if (now < 12)
            {
                await context.PostAsync("Good morning, " + genericMessage);
                context.Wait(this.MessageReceived);
            }
            else if (now < 17)
            {
                await context.PostAsync("Good afternoon, " + genericMessage);
                context.Wait(this.MessageReceived);
            }
            else
            {
                await context.PostAsync("Good evening, " + genericMessage);
                context.Wait(this.MessageReceived);
            }
        }

        public async static void MakeAPICall(string pcName, string token, string program, IDialogContext context)
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create("https://maker.ifttt.com/trigger/GenericTrigger/with/key/fJOlW0DpSuY_h4auJ1l05");
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";
           

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                string json = new JavaScriptSerializer().Serialize(new
                {
                    value1 = program,
                    value2 = pcName,
                    value3 = token
                });

                streamWriter.Write(json);
            }

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var Httpresult = streamReader.ReadToEnd();
                await context.PostAsync($"{Httpresult}");
            }
        }
    }
}
