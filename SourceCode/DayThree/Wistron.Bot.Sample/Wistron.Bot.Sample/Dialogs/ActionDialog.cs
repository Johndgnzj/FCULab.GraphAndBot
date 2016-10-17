using System;
using System.Threading;
using System.Threading.Tasks;
using AuthBot;
using AuthBot.Dialogs;
using AuthBot.Models;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using Newtonsoft.Json.Linq;
using Wistron.Bot.Sample.Helpers;
using System.IO;
using System.Net;
using System.Text;

namespace Wistron.Bot.Sample.Dialogs
{

    [Serializable]
    public class ActionDialog : IDialog<string>
    {
        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }
        public virtual async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> item)
        {
            var message = await item;
            if ((message.Text+"").ToLower() == "/menu")
            {
                var accessToken = await context.GetAccessToken(AuthSettings.Scopes);
                await ShowMenu(context, (string.IsNullOrEmpty(accessToken) ? false : true));
            }
            else if (message.Text == ("/" + MenuHelper.MainMenu.LOGON.ToString()))
            {
                //endpoint v2
                if (string.IsNullOrEmpty(await context.GetAccessToken(AuthSettings.Scopes)))
                {
                    await context.Forward(new AzureAuthDialog(AuthSettings.Scopes), this.ResumeAfterForward, message, CancellationToken.None);
                }
                else
                {
                    context.Wait(MessageReceivedAsync);
                }
            }
            else if (message.Text == ("/" + MenuHelper.MainMenu.TOKEN.ToString()))
            {
                await GetTokenAsync(context);
            }
            else if (message.Text == ("/" + MenuHelper.MainMenu.LOGOUT.ToString()))
            {
                await context.Logout();
                context.Wait(this.MessageReceivedAsync);
            }
            else if (message.Text == ("/" + MenuHelper.MainMenu.ME.ToString()))
            {
                await GetUserDataAsync(context, message);
            }
            else if (message.Text == ("/" + MenuHelper.MainMenu.GETCONTACT.ToString()))
            {
                await context.Forward(new ContactDialog(""), ResumeAfterForward, message, CancellationToken.None);
            }
            else if (message.Text == ("/" + MenuHelper.MainMenu.SENDEMAIL.ToString()) ||
                message.Text == ("/" + MenuHelper.MainMenu.GETEMAIL.ToString()))
            {
                await context.Forward(new MailDialog(), ResumeAfterForward, message, CancellationToken.None);
            }
            else if ((message.Text + "").Contains("/" + MenuHelper.MainMenu.GETCALENDAR.ToString()))
            {
                string subject = message.Text.Replace("/" + MenuHelper.MainMenu.GETCALENDAR.ToString() + "/", "");
                await context.Forward(new CalendarDialog(subject), ResumeAfterForward, message, CancellationToken.None);
            }
            else if (message.Text == ("/" + MenuHelper.MainMenu.CREATEEVENT.ToString()))
            {
                await context.Forward(new CalendarDialog(""), ResumeAfterForward, message, CancellationToken.None);
            }
            else if (message.Text == ("/" + MenuHelper.MainMenu.GETONEDRIVE.ToString()) || message.Attachments != null)
            {
                await context.Forward(new OneDriveDialog(), ResumeAfterForward, message, CancellationToken.None);
            }
            else if ((message.Text+"").Contains("/" + MenuHelper.MainMenu.GETONECONTACT.ToString()) )
            {
                string content = message.Text.Replace("/" + MenuHelper.MainMenu.GETONECONTACT.ToString() + "/", "");
                if (content != "")
                    await context.Forward(new ContactDialog(content), ResumeAfterForward, message, CancellationToken.None);
                else
                    PromptDialog.Text(context, AfterEnterNameAsync, "Give me a name who you're finding.", "that's not a name.");
            }else
            {
                context.Wait(MessageReceivedAsync);
            }
        }
        /// <summary>
        /// 顯示功能目錄
        /// </summary>
        /// <param name="context"></param>
        /// <param name="IsLogin"></param>
        /// <returns></returns>
        private async Task ShowMenu(IDialogContext context,bool IsLogin)
        {
            var reply = context.MakeMessage();
            reply.Attachments = new List<Attachment>();
            Dictionary<String, String> MainMenu = MenuHelper.GetMainMenu(IsLogin);
            var actions = new List<CardAction>();
            reply.AttachmentLayout = AttachmentLayoutTypes.Carousel;
            foreach (String i in MainMenu.Keys)
            {
                actions.Add(new CardAction
                {
                    Title =MainMenu[i],
                    Value = "/" + i,
                    Type = ActionTypes.PostBack
                });
            }
            reply.Attachments.Add(
                     new HeroCard
                     {
                         Title = "Main Menu",
                         Buttons = actions
                     }.ToAttachment()
                );
            await context.PostAsync(reply);
            context.Wait(MessageReceivedAsync);
        }
        private async Task ResumeAfterForward(IDialogContext context, IAwaitable<String> result)
        {
            var message = await result;

            await context.PostAsync(message);
            context.Wait(MessageReceivedAsync);
        }

        public async Task GetTokenAsync(IDialogContext context)
        {
            //endpoint v2
            var accessToken = await context.GetAccessToken(AuthSettings.Scopes);

            if (string.IsNullOrEmpty(accessToken))
            {
                return;
            }

            await context.PostAsync($"Your access token is: {accessToken}");

            context.Wait(MessageReceivedAsync);
        }

        /// <summary>
        /// 取得個人資料組成卡片。
        /// </summary>
        /// <param name="context"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task GetUserDataAsync(IDialogContext context, IMessageActivity message)
        {
            var reply = context.MakeMessage();
            reply.Text = string.Format("You're {0}  who Come from {1}", message.From.Name, message.ChannelId);
            //endpoint v2
            var accessToken = await context.GetAccessToken(AuthSettings.Scopes);
            if (string.IsNullOrEmpty(accessToken))
            {
                reply.Text += ",type 'logon' to authorize permission for more feature.";
            }
            else
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    var result = await client.GetAsync(Settings.GetMeUrl);
                    var resultString = await result.Content.ReadAsStringAsync();

                    var jResult = JObject.Parse(resultString);
                    var actions = new List<CardAction>();
                    reply.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                    actions.Add(new CardAction { Title = $"Go to My Mail Box", Value = Settings.OutlookUrl, Type = ActionTypes.OpenUrl });
                    reply.Attachments.Add(
                             new HeroCard
                             {
                                 Title = jResult["displayName"].ToString(),
                                 Subtitle = jResult["mail"].ToString(),
                                 Text = "Your MSA/Office365 Account Data is :",
                                 Buttons = actions
                             }.ToAttachment()
                        );
                }
            }
            await context.PostAsync(reply);
            context.Wait(MessageReceivedAsync);
        }
        /// <summary>
        /// 輸入文字後的動作
        /// </summary>
        /// <param name="context"></param>
        /// <param name="argument"></param>
        /// <returns></returns>
        public async Task AfterEnterNameAsync(IDialogContext context, IAwaitable<String> argument)
        {
            var message = context.MakeMessage();
            var name = await argument;
            if (!string.IsNullOrEmpty(name))
            {
                await context.Forward(new ContactDialog(name), ResumeAfterForward, message, CancellationToken.None);
            }
            else
            {
                await context.PostAsync("Didn't get it.");
            }
        }

    }
}