using AuthBot;
using AuthBot.Models;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Wistron.Bot.Sample.Helpers;

namespace Wistron.Bot.Sample.Dialogs
{
    [Serializable]
    public class CalendarDialog : IDialog<string>
    {
        private string _SubjectName;
        public CalendarDialog(String SubjectName)
        {
            this._SubjectName = SubjectName;
        }

        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }
        public virtual async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var message = await argument;
            if (message.Text == ("/" + MenuHelper.MainMenu.GETCALENDAR.ToString()))
            {
                await GetCalendarList(context,this._SubjectName);
            }
            else if (message.Text == ("/" + MenuHelper.MainMenu.CREATEEVENT.ToString()))
            {
                //await context.Forward(new MailDialog(), ResumeAfterForward, message, CancellationToken.None);
                PromptDialog.Text(context, ResumeAfterSubjectAsync, "Give a Subject", "didn't get it.");
                //context.Done("Create Events successed!!");
            }
            else
            {
                context.Done(string.Empty);
            }
        }

        /// <summary>
        /// 取得行事曆清單
        /// </summary>
        /// <param name="context"></param>
        /// <param name="SubjectName"></param>
        /// <returns></returns>
        private async Task GetCalendarList(IDialogContext context,String SubjectName)
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
                    string uri = Settings.GetEventsUrl;
                    if (!string.IsNullOrEmpty(SubjectName))
                    {
                        if (SubjectName.Contains("$skip"))
                        {
                            uri += SubjectName;
                        }
                    }
                    var result = await client.GetAsync(uri);
                    var resultString = await result.Content.ReadAsStringAsync();

                    var jResult = JObject.Parse(resultString);
                    if (jResult["value"] != null)
                    {
                        reply.Attachments = new List<Attachment>();
                        //reply.Text = jResult["value"].ToString();
                        var actions = new List<CardAction>();
                        reply.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                        foreach (JToken j in jResult["value"].Children())
                        {
                            actions.Add(new CardAction { Title = (string.IsNullOrEmpty(j["subject"].ToString()) ? "(No subject)" : j["subject"].ToString()), Value = j["webLink"].ToString(), Type = ActionTypes.OpenUrl });
                        }
                        if (jResult["@odata.nextLink"] != null)
                        {
                            actions.Add(new CardAction { Title = "Show More", Value = "/" + MenuHelper.MainMenu.GETCALENDAR.ToString() + "/" + jResult["@odata.nextLink"].ToString().Substring(jResult["@odata.nextLink"].ToString().IndexOf("?$skip=")), Type = ActionTypes.PostBack });
                        }
                        reply.Attachments.Add(
                                 new HeroCard
                                 {
                                     Title = "Your Events",
                                     Subtitle = "Click to check detail",
                                     Buttons = actions
                                 }.ToAttachment()
                            );
                    }
                    else
                    {
                        reply.Text = "You don't have any events.";
                    }
                }
            }
            await context.PostAsync(reply);
            context.Done(string.Empty);
        }
        /// <summary>
        /// 輸入要新增的行程主旨後
        /// </summary>
        /// <param name="context"></param>
        /// <param name="argument"></param>
        /// <returns></returns>
        public async Task ResumeAfterSubjectAsync(IDialogContext context, IAwaitable<string> argument)
        {
            var subject = await argument;
            context.UserData.SetValue<string>("tmp_event_subject", subject);
            PromptDialog.Text(context, ResumeAfterStartDateAsync, "Give a event date(eg:2016/10/19 12:00)", "didn't get it.");

        }
        /// <summary>
        /// 輸入行程時間後
        /// </summary>
        /// <param name="context"></param>
        /// <param name="argument"></param>
        /// <returns></returns>
        public async Task ResumeAfterStartDateAsync(IDialogContext context, IAwaitable<string> argument)
        {
            var inputDate = await argument;
            DateTime startdate;
            if (DateTime.TryParse(inputDate, out startdate))
            {
                context.UserData.SetValue<DateTime>("tmp_event_startdate", startdate);
                PromptDialog.Text(context, ResumeAfterAttendeAsync, "Who you wanna invite to this meeting.", "didn't get it.");
            }
            else
            {
                PromptDialog.Text(context, ResumeAfterStartDateAsync, "Wrong datetime Format(eg:2016/10/19)", "didn't get it.");
            }

        }
        /// <summary>
        /// 輸入參與者後
        /// </summary>
        /// <param name="context"></param>
        /// <param name="argument"></param>
        /// <returns></returns>
        public async Task ResumeAfterAttendeAsync(IDialogContext context, IAwaitable<string> argument)
        {
            var attende = await argument;
            var accessToken = await context.GetAccessToken(AuthSettings.Scopes);
            string subject;
            DateTime startdate;
            context.UserData.TryGetValue<string>("tmp_event_subject", out subject);
            context.UserData.TryGetValue<DateTime>("tmp_event_startdate", out startdate);
            var ret = await CreateEventAsync(accessToken,
                getCalendarEventContents(subject, await getMeInfo(accessToken), startdate, startdate.AddHours(1), attende, attende));
            if (!string.IsNullOrEmpty(ret)) { context.Done(ret); } else { context.Done("Create failed!"); }
        }
        /// <summary>
        /// 取得登入者的資料
        /// </summary>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        public async Task<string> getMeInfo(String accessToken)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var result = await client.GetAsync(Settings.GetMeUrl);
                var resultString = await result.Content.ReadAsStringAsync();

                var jResult = JObject.Parse(resultString);
                return jResult["displayName"].ToString();
            }
        }
        /// <summary>
        /// 新增行程
        /// </summary>
        /// <param name="accessToken"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        public static async Task<String> CreateEventAsync(string accessToken, string body)
        {
            string strRet = string.Empty;
            string jsonStr = "{\"subject\":\"hello\"}";
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json; charset=utf-8");
                client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bearer " + accessToken);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                HttpContent content = new StringContent(body, Encoding.UTF8, "application/json");
                HttpResponseMessage msg = await client.PostAsync(Settings.PostEventsUrl, content);
                if (msg.IsSuccessStatusCode)
                {
                    var jsonResponse = await msg.Content.ReadAsStringAsync();
                    strRet = jsonResponse;
                }
                else
                {
                    //throw new Exception(msg.StatusCode.ToString());
                }
            }
            return strRet;
        }
        /// <summary>
        /// 取得新增行程的Json文字
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="name"></param>
        /// <param name="startDatetime"></param>
        /// <param name="endDatetime"></param>
        /// <param name="attendeName"></param>
        /// <param name="attendeEmail"></param>
        /// <returns></returns>
        public string getCalendarEventContents(string subject, string name, DateTime startDatetime, DateTime endDatetime, string attendeName, string attendeEmail)
        {
            var a = new { emailAddress = new { name = attendeName, address = attendeEmail } };
            List<Object> attendees = new List<object> { a };
            JObject o = JObject.FromObject(new
            {
                subject = (string.IsNullOrEmpty(subject) ? "透過程式建立的事件" : subject),
                isAllDay = false,
                start = new
                {
                    dateTime = startDatetime.ToString("yyyy-MM-dd HH:mm:ss"),
                    timeZone = "UTC"
                },
                end = new
                {
                    dateTime = endDatetime.ToString("yyyy-MM-dd HH:mm:ss"),
                    timeZone = "UTC"
                },
                attendees = attendees,
                location = new { displayName = "ConCall Meeting" },
                reminderMinutesBeforeStart = 0,
                isReminderOn = true,
                body = new
                {
                    contentType = "HTML",
                    content = string.Format("發起人：{0}<br/> 與會者：{1}", name, attendeName)
                }
            });

            return o.ToString();
        }

    }
}