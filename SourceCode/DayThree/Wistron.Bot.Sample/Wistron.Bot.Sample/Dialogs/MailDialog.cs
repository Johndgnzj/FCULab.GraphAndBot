using AuthBot;
using AuthBot.Models;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Wistron.Bot.Sample.Helpers;

namespace Wistron.Bot.Sample.Dialogs
{
    [Serializable]
    public class MailDialog : IDialog<string>
    {
        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }
        public virtual async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            PromptDialog.Text(context, AfterToAsync, "Send to who?", "not email format");
            //context.Wait(MessageReceivedAsync);
        }
        /// <summary>
        /// 輸入收件人後
        /// </summary>
        /// <param name="context"></param>
        /// <param name="argument"></param>
        /// <returns></returns>
        public virtual async Task AfterToAsync(IDialogContext context, IAwaitable<String> argument)
        {
            var address = await argument;
            address = Regex.Replace(address, "<[^>]+>", string.Empty); // 把<…>取代為空白
            context.UserData.SetValue<string>("/MAIL/ADDRESS", address);
            PromptDialog.Text(context, AfterSubjectAsync, "Email Subject", "didn't get it.");
        }
        /// <summary>
        /// 輸入主旨後
        /// </summary>
        /// <param name="context"></param>
        /// <param name="argument"></param>
        /// <returns></returns>
        public virtual async Task AfterSubjectAsync(IDialogContext context, IAwaitable<String> argument)
        {
            var subject = await argument;
            context.UserData.SetValue<string>("/MAIL/SUBJECT", subject);
            PromptDialog.Text(context, AfterBodyAsync, "Email body", "didn't get it.");
        }
        /// <summary>
        /// 輸入內文後
        /// </summary>
        /// <param name="context"></param>
        /// <param name="argument"></param>
        /// <returns></returns>
        public virtual async Task AfterBodyAsync(IDialogContext context, IAwaitable<String> argument)
        {
            var body = await argument;
            string address = "", subject = "";
            context.UserData.SetValue<string>("/MAIL/BODY", body);
            context.UserData.TryGetValue<string>("/MAIL/ADDRESS", out address);
            context.UserData.TryGetValue<string>("/MAIL/SUBJECT", out subject);
            var reply = context.MakeMessage();
            reply.Text = "This is your Email content";
            var actions = new List<CardAction>();
            reply.AttachmentLayout = AttachmentLayoutTypes.Carousel;
            reply.Attachments.Add(
                     new HeroCard
                     {
                         Title = "Send Email to: " + address,
                         Subtitle = "Email Subject: " + subject,
                         Text = body
                     }.ToAttachment()
                );
            await context.PostAsync(reply);
            PromptDialog.Confirm(context, afterConfirmAsync, "Are you sure sending this email?", "Didn't get it.");
        }
        /// <summary>
        /// 確認寄信後
        /// </summary>
        /// <param name="context"></param>
        /// <param name="argument"></param>
        /// <returns></returns>
        public virtual async Task afterConfirmAsync(IDialogContext context, IAwaitable<bool> argument)
        {
            var choice = await argument;
            if (choice)
            {
                var accessToken = await context.GetAccessToken(AuthSettings.Scopes);
                string address = "", subject = "", body = "";
                context.UserData.TryGetValue<string>("/MAIL/ADDRESS", out address);
                context.UserData.TryGetValue<string>("/MAIL/SUBJECT", out subject);
                context.UserData.TryGetValue<string>("/MAIL/BODY", out body);
                var ret = await SendEmailAsync(accessToken,
                GetMailBody(subject, address, body));
                if (string.IsNullOrEmpty(ret))
                {
                    context.Done("Your Email has was send.");
                }
                else
                {
                    context.Done($"Send Email failed!({ret})");
                }
            }
            else
            {
                context.Done("Your Email was canceled");
            }
        }
        /// <summary>
        /// 寄信
        /// </summary>
        /// <param name="accessToken"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        public static async Task<String> SendEmailAsync(string accessToken, string body)
        {
            string strRet = string.Empty;
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json; charset=utf-8");
                client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bearer " + accessToken);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                HttpContent content = new StringContent(body, Encoding.UTF8, "application/json");
                HttpResponseMessage msg = await client.PostAsync(Settings.PostEmailUrl, content);
                if (msg.IsSuccessStatusCode)
                {
                    var jsonResponse = await msg.Content.ReadAsStringAsync();
                    strRet = jsonResponse;
                }
                else
                {
                    strRet = msg.StatusCode.ToString();
                    //throw new Exception(msg.StatusCode.ToString());
                }
            }
            return strRet;
        }
        /// <summary>
        /// 取得SendMail的JSON文字
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="To"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        public static string GetMailBody(String subject, String To, String body)
        {
            try
            {
                var a = new { emailAddress = new { name = To, address = To } };
                List<Object> attendees = new List<object> { a };
                JObject o = JObject.FromObject(new
                {
                    Message = new
                    {
                        subject = (string.IsNullOrEmpty(subject) ? "透過程式建立的EMail" : subject),
                        toRecipients = attendees,
                        body = new
                        {
                            contentType = "TEXT",
                            content = body
                        }
                    }
                });

                return o.ToString();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}