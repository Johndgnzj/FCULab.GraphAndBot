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
namespace Wistron.Bot.Sample.Dialogs
{
    [Serializable]
    public class ContactDialog : IDialog<string>
    {
        private string _ContactName;
        public ContactDialog(String ContactName)
        {
            this._ContactName = ContactName;
        }
        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }
        public virtual async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            await GetContactDataAsync(context, this._ContactName);
        }
        /// <summary>
        /// 取得聯絡人清單
        /// </summary>
        /// <param name="context"></param>
        /// <param name="ContactName"></param>
        /// <returns></returns>
        public async Task GetContactDataAsync(IDialogContext context,String ContactName)
        {
            var reply = context.MakeMessage();

            //endpoint v2
            var accessToken = await context.GetAccessToken(AuthSettings.Scopes);
            if (string.IsNullOrEmpty(accessToken))
            {
                reply.Text = "Access token mssing,find contact has been cancel.";
            }
            else
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    string uri = Settings.GetContactsUrl;
                    if (!string.IsNullOrEmpty(ContactName))
                    {
                        if (ContactName.Contains("$skip"))
                        {
                            uri += ContactName;
                        }
                        else
                        {
                            string strEscaped = Uri.EscapeDataString(ContactName);
                            uri += string.Format("?$filter=startswith(displayname,'{0}') or startswith(givenName,'{0}') or emailAddresses/any(x: x/address eq '{0}')", strEscaped);
                        }
                    }
                    var result = await client.GetAsync(uri);
                    var resultString = await result.Content.ReadAsStringAsync();

                    var jResult = JObject.Parse(resultString);
                    if (jResult["value"] != null)
                    {
                        // 多個結果的卡片
                        var actions = new List<CardAction>();
                        reply.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                        foreach(JToken j in jResult["value"].Children())
                        {
                            actions.Add(new CardAction { Title = j["displayName"].ToString(), Value = MenuHelper.MainMenu.GETONECONTACT.ToString() + "/" + j["displayName"].ToString(), Type = ActionTypes.PostBack });
                        }
                        actions.Add(new CardAction { Title = "Search By name", Value = MenuHelper.MainMenu.GETONECONTACT.ToString() + "/", Type = ActionTypes.PostBack });
                        if (jResult["@odata.nextLink"] != null)
                        {
                            actions.Add(new CardAction { Title = "Show More", Value = MenuHelper.MainMenu.GETONECONTACT.ToString() + "/" + jResult["@odata.nextLink"].ToString().Substring(jResult["@odata.nextLink"].ToString().IndexOf("?$skip=")), Type = ActionTypes.PostBack });
                        }
                        reply.Attachments.Add(
                                 new HeroCard
                                 {
                                     Title = "Your Contacts",
                                     Subtitle = "Click to check detail",
                                     Buttons = actions
                                 }.ToAttachment()
                            );
                        // 單一結果時的卡片
                        ///TODO
                    }
                    else
                    {
                        reply.Text = "I can't find any contacts.";
                    }
                }
            }
            await context.PostAsync(reply);
            context.Done(string.Empty);
        }

    }
}