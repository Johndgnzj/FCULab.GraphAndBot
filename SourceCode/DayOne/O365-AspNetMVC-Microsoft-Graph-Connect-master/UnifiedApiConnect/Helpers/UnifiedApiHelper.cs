// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See full license at the bottom of this file. 

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using UnifiedApiConnect.Models;

namespace UnifiedApiConnect.Helpers
{
    public class UnifiedApiHelper
    {
        static MediaTypeWithQualityHeaderValue Json = new MediaTypeWithQualityHeaderValue("application/json");

        // Get infomation about the current logged in user.
        public static async Task<UserInfo> GetUserInfoAsync(string accessToken)
        {
            UserInfo myInfo = new UserInfo();

            using (var client = new HttpClient())
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, Settings.GetMeUrl))
                {
                    request.Headers.Accept.Add(Json);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                    using (var response = await client.SendAsync(request))
                    {
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            var json = JObject.Parse(await response.Content.ReadAsStringAsync());
                            myInfo.Name = json?["displayName"]?.ToString();
                            myInfo.Address = json?["mail"]?.ToString().Trim().Replace(" ", string.Empty);

                        }
                    }
                }
            }

            return myInfo;
        }

        public static async Task<String> CreateEventAsync(string accessToken,string body)
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

        // Construct and send the message that the logged in user wants to send.
        public static async Task<SendMessageResponse> SendMessageAsync(string accessToken, SendMessageRequest sendMessageRequest)
        {
            var sendMessageResponse = new SendMessageResponse { Status = SendMessageStatusEnum.NotSent };

            using (var client = new HttpClient())
            {
                using (var request = new HttpRequestMessage(HttpMethod.Post, Settings.SendMessageUrl))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    request.Content = new StringContent(JsonConvert.SerializeObject(sendMessageRequest), Encoding.UTF8, "application/json");
                    using (HttpResponseMessage response = await client.SendAsync(request))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            sendMessageResponse.Status = SendMessageStatusEnum.Sent;
                            sendMessageResponse.StatusMessage = null;
                        }
                        else
                        {
                            sendMessageResponse.Status = SendMessageStatusEnum.Fail;
                            sendMessageResponse.StatusMessage = response.ReasonPhrase;
                        }
                    }
                }
            }

            return sendMessageResponse;
        }

        public static async Task<List<EventInfo>> GetEventInfoAsync(string accessToken)
        {

            List<EventInfo> myEventInfolList = new List<EventInfo>();
            using (var client = new HttpClient())
            {
                //Settings.GetOneDriveUrl
                using (var request = new HttpRequestMessage(HttpMethod.Get, Settings.GetEventsUrl))
                {
                    request.Headers.Accept.Add(Json);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                    using (var response = await client.SendAsync(request))
                    {
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            //var result = await response.Content.ReadAsStringAsync();

                            var json = JObject.Parse(await response.Content.ReadAsStringAsync());
                            foreach (JToken j in json["value"].Children())
                            {
                                EventInfo myEventInfo = new EventInfo();
                                myEventInfo.Subject = j?["subject"]?.ToString();
                                myEventInfo.createdDateTime = DateTime.Parse(j?["start"]?["dateTime"]?.ToString());
                                myEventInfo.id = j?["id"]?.ToString();
                                myEventInfolList.Add(myEventInfo);
                            }
                        }
                    }
                }
            }

            return myEventInfolList;
        }
        public static async Task<List<OneDriveInfo>> GetOneDriveAsync(string accessToken)
        {

            List<OneDriveInfo> myOneDriveInfolList = new List<OneDriveInfo>();
            using (var client = new HttpClient())
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, Settings.GetOneDriveUrl))
                {
                    request.Headers.Accept.Add(Json);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                    using (var response = await client.SendAsync(request))
                    {
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            var json = JObject.Parse(await response.Content.ReadAsStringAsync());
                            foreach (JToken j in json["value"].Children())
                            {
                                OneDriveInfo myOneDriveInfo = new OneDriveInfo();
                                myOneDriveInfo.Name = j?["name"]?.ToString();
                                myOneDriveInfo.createdDateTime = DateTime.Parse(j?["createdDateTime"]?.ToString());
                                myOneDriveInfo.microsoftgraphdownloadUrl = j?["@microsoft.graph.downloadUrl"]?.ToString();
                                myOneDriveInfo.folder = j?["folder"]?.ToString();
                                myOneDriveInfo.id = j?["id"]?.ToString();
                                myOneDriveInfolList.Add(myOneDriveInfo);
                            }
                        }
                    }
                }
            }

            return myOneDriveInfolList;
        }

    }
}

//********************************************************* 
// 
//O365-AspNetMVC-Unified-API-Connect, https://github.com/OfficeDev/O365-AspNetMVC-Unified-API-Connect
//
//Copyright (c) Microsoft Corporation
//All rights reserved. 
//
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// ""Software""), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:

// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 
//********************************************************* 
