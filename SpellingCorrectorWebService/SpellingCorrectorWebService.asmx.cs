using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Web.Configuration;
using System.Data.SqlClient;
using System.Web.Script.Serialization;

namespace SpellingCorrectorWebService
{
    /// <summary>
    /// Summary description for SpellingCorrectorWebService
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    [System.Web.Script.Services.ScriptService]
    public class SpellingCorrectorWebService : System.Web.Services.WebService
    {
        [WebMethod]
        // Implementing spell corrector funcionality here.
        public void GetCorrection(string input, string language)
        {
            // We will create the wordList once only for providing source words for comparison.
            // adv. save time, improve performance.
            // disadv. when we need update the wordList, we need remove the if clause.

            SpellingCorrectorFunction.GetWordList();

            if(SpellingCorrectorFunction.wordList.Count == 0)
                // Create dictioanry in terms of the corpus and language type provided.
                SpellingCorrectorFunction.CreateDictionary(@"C:\Users\Daniel\Documents\Visual Studio 2015\Projects\SpellingCorrectorWebService\SpellingCorrectorWebService\source\source.txt", language);

            // Get response.
            var resp = SpellingCorrectorFunction.ReadFromStdIn(input, language);

            JavaScriptSerializer js = new JavaScriptSerializer();
            Context.Response.Write(js.Serialize(resp));
        }
    }
}
