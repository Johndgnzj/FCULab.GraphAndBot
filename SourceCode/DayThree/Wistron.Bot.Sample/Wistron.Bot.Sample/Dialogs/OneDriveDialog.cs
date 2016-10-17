using AuthBot;
using AuthBot.Models;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Wistron.Bot.Sample.Helpers;

namespace Wistron.Bot.Sample.Dialogs
{
    [Serializable]
    public class OneDriveDialog : IDialog<string>
    {
        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }
        public virtual async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var message = await argument;
            if (string.IsNullOrEmpty(message.Text) && message.Attachments != null)
            {
                try
                {
                    String ContentUrl = HttpUtility.UrlDecode(message.Attachments[0].ContentUrl);
                    context.UserData.SetValue<string>("UploadFile", ContentUrl);
                    var connector = new ConnectorClient(new Uri(message.ServiceUrl));
                    var content = connector.HttpClient.GetStreamAsync(ContentUrl).R‌​esult;
                    //await context.PostAsync($"got file.");
                    string fileName = "tmp.dat";
                    string filePath = Path.Combine(System.Web.Hosting.HostingEnvironment.MapPath("~/UsersPhoto"), fileName);
                    FileStream fs = content as FileStream;
                    if (fs != null)
                    {
                        fileName = fs.Name;
                        await SaveAttachment(context, fileName);
                    }
                    else
                    {
                        PromptDialog.Text(context, AfterGiveFileName, "Give a full file name for this file(eg: MyImage.png...)", "not a file name");
                    }
                }
                catch { }
            }
            else
            {
                PromptDialog.Text(context, AfterInputKeyWordAsync, "Give me a keyword your're looking for.");
            }

        }
        /// <summary>
        /// 輸入搜尋的關鍵字後
        /// </summary>
        /// <param name="context"></param>
        /// <param name="argument"></param>
        /// <returns></returns>
        public virtual async Task AfterInputKeyWordAsync(IDialogContext context, IAwaitable<String> argument)
        {
            var keyword = await argument;
            var files = await SearchOneDrive(keyword, await context.GetAccessToken(AuthSettings.Scopes));
            if (files.Count == 0)
            {
                context.Done("No result was founded.");
            }
            else
            {
                PromptDialog.Choice(context, FileSelectResult, files, "Which file do you want?", null, 3, PromptStyle.PerLine);
            }
        }
        /// <summary>
        /// 取得選擇的檔案連結
        /// </summary>
        /// <param name="context"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        private async Task FileSelectResult(IDialogContext context, IAwaitable<Models.File> file)
        {
            string fileURL = (await file).WebURL;
            context.Done("Ok, here's link for the file:" + fileURL);

        }
        /// <summary>
        /// 搜尋OneDrive，回傳清單
        /// </summary>
        /// <param name="search"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task<List<Models.File>> SearchOneDrive(string search, string token)
        {
            List<Models.File> files = new List<Models.File>();
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var result = await client.GetAsync(string.Format(Settings.SearchOneDriveUrl, HttpUtility.UrlEncode(search)));
                var resultString = await result.Content.ReadAsStringAsync();

                var jResult = JObject.Parse(resultString);
                JArray jFiles = (JArray)jResult["value"];
                foreach (JObject item in jFiles)
                {
                    Models.File f = new Models.File();
                    f.CreatedBy = item["createdBy"]["user"].Value<string>("displayName");
                    f.CreatedDate = DateTimeOffset.Parse(item.Value<string>("createdDateTime"));
                    f.ID = item.Value<string>("id");
                    f.LastModifiedBy = item["lastModifiedBy"]["user"].Value<string>("displayName");
                    f.LastModifiedDate = DateTimeOffset.Parse(item.Value<string>("lastModifiedDateTime"));
                    f.Name = item.Value<string>("name");
                    f.WebURL = item.Value<string>("webUrl");
                    files.Add(f);
                    if (files.Count > 10)
                        return files;
                }
                return files;
            }
        }
        /// <summary>
        /// 上傳檔案到OneDrive中
        /// </summary>
        /// <param name="context"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public async Task SaveAttachment(IDialogContext context, string fileName)
        {
            string uploadFile = "";
            context.UserData.TryGetValue<string>("UploadFile", out uploadFile);
            if (uploadFile != "")
            {
                var connector = new ConnectorClient(new Uri(uploadFile));
                var content = connector.HttpClient.GetStreamAsync(uploadFile).R‌​esult;
                var memoryStream = new MemoryStream();
                content.CopyTo(memoryStream);
                string filePath = Path.Combine(System.Web.Hosting.HostingEnvironment.MapPath("~/UsersPhoto"), fileName);
                var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                memoryStream.WriteTo(fileStream);
                fileStream.Dispose();
                context.Done($"attachment save successed!({filePath})");
            }
            else
            {
                context.Done("attachment expired!");
            }
        }
        /// <summary>
        /// 輸入檔名後
        /// </summary>
        /// <param name="context"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private async Task AfterGiveFileName(IDialogContext context, IAwaitable<string> result)
        {
            var fileName = await result;
            if (Path.GetExtension(fileName) != "")
            {
                context.UserData.SetValue<string>("UploadFileName", fileName);
                List<string> action = new List<string>();
                action.Add("SaveToOneDrive");
                action.Add("cancel");
                IEnumerable<string> e = action.ToArray();
                PromptDialog.Choice(context, AfterUploadFileAsync, e, "And what do u want?");
                //await SaveAttachment(context, fileName);
            }
            else
            {
                context.Done("wrong file name,please upload again.");
                //PromptDialog.Text(context, AfterGiveFileName, "Give a full file name for this file(eg: MyImage.png...)", "not a file name");
            }
        }
        /// <summary>
        /// 上傳檔案後
        /// </summary>
        /// <param name="context"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private async Task AfterUploadFileAsync(IDialogContext context, IAwaitable<string> result)
        {
            try
            {
                var accessToken = await context.GetAccessToken(AuthSettings.Scopes);
                if (string.IsNullOrEmpty(accessToken))
                {
                    context.Done($"You should logon a MSA/Office365 account before you uploade onedrive(type logon).");
                }
                else
                {
                    var choice = await result;
                    if (choice.Equals("SaveToOneDrive"))
                    {
                        string uploadFile = "";
                        context.UserData.TryGetValue<string>("UploadFile", out uploadFile);
                        string uploadFileName = "";
                        context.UserData.TryGetValue<string>("UploadFileName", out uploadFileName);
                        if (uploadFile != "" && uploadFileName != "")
                        {
                            var connector = new ConnectorClient(new Uri(uploadFile));
                            var content = connector.HttpClient.GetStreamAsync(uploadFile).R‌​esult;
                            var memoryStream = new MemoryStream();
                            content.CopyTo(memoryStream);
                            byte[] data = memoryStream.ToArray();
                            var fileUrl = new Uri(string.Format(Settings.CreateOneDriveUrl, uploadFileName));
                            var request = (HttpWebRequest)WebRequest.Create(fileUrl);
                            request.Method = "PUT";
                            request.ContentLength = data.Length;
                            request.AllowWriteStreamBuffering = true;
                            //request.Accept = "application/json;odata=verbose";
                            request.ContentType = "text/plain";
                            request.Headers.Add("Authorization", "Bearer " + accessToken);
                            System.IO.Stream stream = request.GetRequestStream();
                            //filestream.CopyTo(stream);
                            stream.Write(data, 0, data.Length);
                            stream.Close();
                            //WebResponse response = request.GetResponse();
                            using (WebResponse wr = request.GetResponse())
                            {
                                using (StreamReader myStreamReader = new StreamReader(wr.GetResponseStream(), Encoding.GetEncoding("UTF-8")))
                                {
                                    string ret = myStreamReader.ReadToEnd();
                                    //return data;
                                }
                            }
                            context.Done($"Upload successed!");
                        }
                        else
                        {
                            context.Done("attachment expired!");
                        }
                    }
                    else
                    {
                        context.Done("Do nothing.");
                    }
                }
            }
            catch { }
        }

    }
}