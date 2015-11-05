using Newtonsoft.Json;
using SampleWebAPIConsumer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;



namespace SampleWebAPIConsumer.Controllers
{
    public class HomeController : Controller
    {
        protected string accessToken;
        public string AccessToken
        {
            get
            {
                if (String.IsNullOrWhiteSpace(accessToken))
                {
                    var header = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
                        System.Configuration.ConfigurationManager.AppSettings["Username"] 
                        + ":"
                        + System.Configuration.ConfigurationManager.AppSettings["Password"]));
                    var token = CallServer<String, object>("authorize", retry: false, 
                        headers: new Dictionary<string,string> { 
                            {"Authorization", "Basic " + header }
                            ,{"Act-Database-Name", System.Configuration.ConfigurationManager.AppSettings["Database"] }
                        }).Result;
                    accessToken = token;
                }
                return accessToken;
            }
        }

        protected Guid groupID = Guid.Empty;
        public Guid GroupID
        {
            get
            {
                return groupID;
            }
            set
            {
                groupID = value;
            }
        }

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Signup()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Signup(SignupModel model)
        {
            var contact = await CallServer<Contact, Contact>("api/contacts", new Contact { FirstName = model.Name, BusinessPhone = model.PhoneNumber, EmailAddress = model.EmailAddress }, "POST");
            var id = await FindGroupID(System.Configuration.ConfigurationManager.AppSettings["GroupName"]);

            var contactInGroup = await CallServer<HttpWebResponse, object>("api/groups/" + id + "/contacts/" + contact.ID, null, "PUT");

            if (contactInGroup.StatusCode != HttpStatusCode.OK)
                throw new Exception("Failed to add contact to group");


            return View("SignupSuccess", contact);
        }

        [NonAction]
        public async Task<Guid> FindGroupID(string name)
        {
            if (groupID == Guid.Empty) 
            { 
                var groups = await CallServer<List<Group>, object>("api/groups?$filter=name eq '" + name + "'");
                if (groups == null || groups.Count == 0)
                {
                    // create new group
                    var group = await CallServer<Group, Group>("api/groups", new Group { Name = name }, "POST");
                    if (group != null)
                    GroupID = group.ID;
                }
                else
                    GroupID = groups[0].ID;
            }
            return GroupID;
        }

        [NonAction]
        public async Task<T> CallServer<T,U>(string route, U data = null, string method = "GET", bool retry = true, Dictionary<string,string> headers = null) where T : class where U : class
        {
            // first try calling
            var req = HttpWebRequest.Create(System.Configuration.ConfigurationManager.AppSettings["ApiLocation"] + route);

            req.Method = method;

            if (headers == null || !headers.ContainsKey("Authorization"))
                req.Headers["Authorization"] = "Bearer " + AccessToken;

            if (headers != null)
                foreach (var header in headers)
                    req.Headers[header.Key] = header.Value;


            if ((method == "POST" || method == "PUT"))
            {
                if (data != null)
                {
                    string json = JsonConvert.SerializeObject(data);

                    req.ContentLength = System.Text.Encoding.UTF8.GetByteCount(json);

                    req.ContentType = "application/json; charset=utf-8";

                    // headers need to be set before flushing stream, as when manually setting ContentLength, the request is sent as soon as the stream is flushed.

                    using (var stream = await req.GetRequestStreamAsync())
                    {
                        using (var writer = new System.IO.StreamWriter(stream))
                        {


                            writer.Write(json);
                            writer.Flush();
                        }
                    }
                }
                else
                    req.ContentLength = 0;
                
                
            }

            

            bool reAuth = false;

            try
            {
                var res = req.GetResponse() as HttpWebResponse;

                if ((int)res.StatusCode > 199 && (int)res.StatusCode < 300)
                {
                    if (typeof(T) == typeof(HttpWebResponse))
                        return res as T;

                    using (var sr = new System.IO.StreamReader(res.GetResponseStream()))
                    {

                        var unparsed = await sr.ReadToEndAsync();

                        if (!String.IsNullOrWhiteSpace(unparsed))
                        {
                            if (typeof(T) == typeof(String)) return unparsed as T;
                            
                            return JsonConvert.DeserializeObject<T>(unparsed);
                        }

                    }


                }

            }
            catch (WebException ex)
            {
                var wresp = ex.Response as HttpWebResponse;
                if (wresp != null)
                {
                    if (wresp.StatusCode == HttpStatusCode.Unauthorized && retry)
                    {
                        reAuth = true;
                    }
                }
            }
            if (reAuth)
            {
                await Authenticate();
                return await CallServer<T, U>(route, data, method, false);
            }

            return null;
        }

        [NonAction]
        public async Task Authenticate()
        {
            var result = await CallServer<String, object>("authorize", retry: false);
            accessToken = result;
        }

    }
}