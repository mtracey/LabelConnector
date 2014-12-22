using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web.Mvc;
using Intuit.Ipp.Core;
using Intuit.Ipp.Core.Configuration;
using Intuit.Ipp.Data;
using Intuit.Ipp.DataService;
using Intuit.Ipp.Exception;
using Intuit.Ipp.QueryFilter;
using Intuit.Ipp.Security;
using Intuit.Ipp.LinqExtender;
using DevDefined.OAuth.Consumer;
using DevDefined.OAuth.Framework;
using LabelConnector.utils;
using Newtonsoft.Json;
using System.IO;

namespace LabelConnector.Controllers
{
    /// <summary>
    /// Controller which connects to QuickBooks and Pulls customer Info.
    /// This flow will make use of Data Service SDK V2 to create OAuthRequest and connect to 
    /// Customer Data under the service context and display data inside the grid.
    /// </summary>
    public class QuickBooksCustomersController : Controller
    {
        /// <summary>
        /// RealmId, AccessToken, AccessTokenSecret, ConsumerKey, ConsumerSecret, DataSourceType
        /// </summary>
        private String realmId, accessToken, accessTokenSecret, consumerKey, consumerSecret, dataSourcetype,apptoken;
        private FileStream fs;
        private IEnumerable<Invoice> Invoice_specific;
        private SalesItemLineDetail Itemls;
        private StreamReader sr;
        private string strFile = " ";
        private string strFormatting = " ";


        public QuickBooksCustomersController()
        {
             //realmId = Session["realm"].ToString();
             //accessToken = Session["accessToken"].ToString();
             //accessTokenSecret = Session["accessTokenSecret"].ToString();
             //consumerKey = ConfigurationManager.AppSettings["consumerKey"].ToString();
             //consumerSecret = ConfigurationManager.AppSettings["consumerSecret"].ToString();
             //apptoken = ConfigurationManager.AppSettings["applicationToken"].ToString();
             //dataSourcetype = Session["dataSource"].ToString().ToLower();
        }

        /// <summary>
        /// Action Results for Index
        /// </summary>
        /// <returns>Action Result.</returns>
        public ActionResult Index(string DocNum = null)
        {
            realmId = Session["realm"].ToString();
            accessToken = Session["accessToken"].ToString();
            accessTokenSecret = Session["accessTokenSecret"].ToString();
            consumerKey = ConfigurationManager.AppSettings["consumerKey"].ToString();
            consumerSecret = ConfigurationManager.AppSettings["consumerSecret"].ToString();
            apptoken = ConfigurationManager.AppSettings["applicationToken"].ToString();
            dataSourcetype = Session["dataSource"].ToString().ToLower();
            try
            {
                IntuitServicesType intuitServicesType = new IntuitServicesType();
                switch (dataSourcetype)
                {
                    case "qbo":
                        intuitServicesType = IntuitServicesType.QBO;
                        break;
                    case "qbd":
                        intuitServicesType = IntuitServicesType.QBD;
                        break;
                    default:
                        throw new Exception("Data source type not found.");
                        break;

                }

                //OAuthRequestValidator requestValidator = new OAuthRequestValidator(accessToken, accessTokenSecret, consumerKey, consumerSecret);
                //ServiceContext serviceContext = new ServiceContext(apptoken,realmId, IntuitServicesType.QBO, requestValidator);
                //DataService service = new DataService(serviceContext);
                //// QueryService<Customer> customerQueryService = new QueryService<Customer>(serviceContext);
                //QueryService<Invoice> invoiceQueryService = new QueryService<Invoice>(serviceContext);
                //IEnumerable<Invoice> Invoice_specific = invoiceQueryService.Where(c => c.DocNumber == "1036");

                OAuthRequestValidator oauthValidator = new OAuthRequestValidator(accessToken, accessTokenSecret, consumerKey, consumerSecret);
                ServiceContext context = new ServiceContext(realmId, intuitServicesType, oauthValidator);
                DataService dataService = new DataService(context);              
                //List<Customer> customers = dataService.FindAll(new Customer(), 1, 31).ToList<Customer>(); 
                QueryService<Invoice> invoiceQueryService = new QueryService<Invoice>(context);
                Invoice_specific = invoiceQueryService.Where(c => c.DocNumber == DocNum);
                ViewBag.Invoice = Invoice_specific;
              
            }
            catch (InvalidTokenException exp)
            {
                //Remove the Oauth access token from the OauthAccessTokenStorage.xml
                OauthAccessTokenStorageHelper.RemoveInvalidOauthAccessToken(Session["FriendlyEmail"].ToString(), this);

                Session["show"] = true;
                return Redirect("/Home/index");
            }
            catch (System.Exception exp)
            {
                throw exp;
            }

            return View("_InvoicePartial");
        }

        /// <summary>
        /// Action Results for Print
        /// </summary>
        /// <returns>Json Object.</returns>
        public JsonResult print(string array)
        {
            realmId = Session["realm"].ToString();
            accessToken = Session["accessToken"].ToString();
            accessTokenSecret = Session["accessTokenSecret"].ToString();
            consumerKey = ConfigurationManager.AppSettings["consumerKey"].ToString();
            consumerSecret = ConfigurationManager.AppSettings["consumerSecret"].ToString();
            apptoken = ConfigurationManager.AppSettings["applicationToken"].ToString();
            dataSourcetype = Session["dataSource"].ToString().ToLower();

            IntuitServicesType intuitServicesType = new IntuitServicesType();
            switch (dataSourcetype)
            {
                case "qbo":
                    intuitServicesType = IntuitServicesType.QBO;
                    break;
                case "qbd":
                    intuitServicesType = IntuitServicesType.QBD;
                    break;
                default:
                    throw new Exception("Data source type not found.");
                    break;

            }
            string[] values = JsonConvert.DeserializeObject<string[]>(array);
            OAuthRequestValidator oauthValidator = new OAuthRequestValidator(accessToken, accessTokenSecret, consumerKey, consumerSecret);
            ServiceContext context = new ServiceContext(realmId, intuitServicesType, oauthValidator);
            DataService service = new DataService(context);
            // QueryService<Customer> customerQueryService = new QueryService<Customer>(serviceContext);
            QueryService<Invoice> source = new QueryService<Invoice>(context);
            string str = "<html>";
            string docNum = values[values.Length - 1];
            Invoice_specific = source.Where(c => c.DocNumber == docNum).ToList<Invoice>();
            for (int i = 0; i < (values.Length - 1); i++)
            {
                string path = Server.MapPath("~/ZPLFORMAT") + @"\QuickBooks_label.txt";
                fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                sr = new StreamReader(fs);
                if (strFile.Trim() == "")
                {
                    strFile = str + "<div style=page-break-after: always>" + sr.ReadToEnd() + "</div>";
                }
                else
                {
                    strFile = strFile + "<div style=page-break-before:always; page-break-after:always>" + sr.ReadToEnd() + "</div>";
                }
                this.strFile = this.strFile.Replace("\r\n", "<br />");
                foreach (Line line in this.Invoice_specific.FirstOrDefault<Invoice>().Line)
                {
                    if ((line.Id == values[i]) && (line.AnyIntuitObject.GetType().ToString() == "Intuit.Ipp.Data.SalesItemLineDetail"))
                    {
                        Itemls = (SalesItemLineDetail)line.AnyIntuitObject;
                        strFile = strFile.Replace("Item Description", line.Description);
                        strFile = strFile.Replace("Txn.Id", values[values.Length - 1]);
                        strFile = strFile.Replace("Qty", this.Itemls.Qty.ToString());
                        strFile = strFile.Replace("Line1", this.Invoice_specific.FirstOrDefault<Invoice>().BillAddr.Line1);
                    }
                }
                strFile = strFile.Replace("item.Id", values[i]);
            }
            str = "</html>";
            strFile = strFile + str;
            int length = strFile.Length;
            return Json(strFile, JsonRequestBehavior.AllowGet);
        }


    }


}