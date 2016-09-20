using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using UnifiedApiConnect.Helpers;
using UnifiedApiConnect.Models;

namespace UnifiedApiConnect.Controllers
{
    public class OneDriveController : Controller
    {
        // GET: OneDrive
        public ActionResult Index(UserInfo userInfo)
        {
            EnsureUser(ref userInfo);

            ViewBag.UserInfo = userInfo;
            return View();
        }


        public async Task<ActionResult> GetOneDriveList()
        {
            if (string.IsNullOrEmpty((string)Session[SessionKeys.Login.AccessToken])) return RedirectToAction(nameof(Index), "Home");
            List<OneDriveInfo> myDataModelList = new List<OneDriveInfo>();

            myDataModelList = await UnifiedApiHelper.GetOneDriveAsync((string)Session[SessionKeys.Login.AccessToken]);
            
            return View(myDataModelList);
        }
        public async Task<ActionResult> UploadFile()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Upload(HttpPostedFileBase file)
        {
            if (string.IsNullOrEmpty((string)Session[SessionKeys.Login.AccessToken])) return RedirectToAction(nameof(Index), "Home");

            if (file != null)
            {
                if (file.ContentLength > 0)
                {
                    var fileName = Path.GetFileName(file.FileName);
                    //var path = Path.Combine(Server.MapPath("~/FileUploads"), fileName);
                    //file.SaveAs(path);
                    byte[] data;
                    using (Stream inputStream = file.InputStream)
                    {
                        MemoryStream memoryStream = inputStream as MemoryStream;
                        if (memoryStream == null)
                        {
                            memoryStream = new MemoryStream();
                            inputStream.CopyTo(memoryStream);
                        }
                        data = memoryStream.ToArray();
                    }

                    var fileUrl = new Uri("https://graph.microsoft.com/v1.0/me/drive/root:/" + fileName + ":/content");
                    var request = (HttpWebRequest)WebRequest.Create(fileUrl);
                    request.Method = "PUT";
                    request.ContentLength = data.Length;
                    request.AllowWriteStreamBuffering = true;
                    //request.Accept = "application/json;odata=verbose";
                    request.ContentType = "text/plain";
                    request.Headers.Add("Authorization", "Bearer " + (string)Session[SessionKeys.Login.AccessToken]);


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
                }
            }
            return RedirectToAction("GetOneDriveList");
        }
        // Use the login user name or recipient email address if no user name.
        void EnsureUser(ref UserInfo userInfo)
        {
            var currentUser = (UserInfo)Session[SessionKeys.Login.UserInfo];

            if (userInfo == null || string.IsNullOrEmpty(userInfo.Address))
            {
                userInfo = currentUser;
            }
            else if (userInfo.Address.Equals(currentUser.Address, StringComparison.OrdinalIgnoreCase))
            {
                userInfo = currentUser;
            }
            else
            {
                userInfo.Name = userInfo.Address;
            }
        }

    }
}