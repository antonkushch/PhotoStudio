using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;

namespace PhotoStudioDiploma.Controllers
{
    public class WebhooksController : Controller
    {
        [HttpGet]
        public ActionResult DropboxWebhook(string challenge)
        {
            return Content(challenge);
        }

        [HttpPost]
        public async Task<ActionResult> DropboxWebhook()
        {
            // Get the request signature
            var signatureHeader = Request.Headers.GetValues("X-Dropbox-Signature");
            if (signatureHeader == null || !signatureHeader.Any())
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            // Get the signature value
            string signature = signatureHeader.FirstOrDefault();

            // Extract the raw body of the request
            string body = null;
            using (StreamReader reader = new StreamReader(Request.InputStream))
            {
                body = await reader.ReadToEndAsync();
            }

            // Check that the signature is good
            string appSecret = WebConfigurationManager.AppSettings["DropboxAppSecret"];
            using (HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appSecret)))
            {
                if (!VerifySha256Hash(hmac, body, signature))
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // Do your thing here... e.g. store it in a queue to process later
            // ...

            // Return A-OK :)
            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }

        private string GetSha256Hash(HMACSHA256 sha256Hash, string input)
        {
            // Convert the input string to a byte array and compute the hash. 
            byte[] data = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            var stringBuilder = new StringBuilder();

            // Loop through each byte of the hashed data  
            // and format each one as a hexadecimal string. 
            foreach (byte t in data)
            {
                stringBuilder.Append(t.ToString("x2"));
            }

            // Return the hexadecimal string. 
            return stringBuilder.ToString();
        }

        private bool VerifySha256Hash(HMACSHA256 sha256Hash, string input, string hash)
        {
            // Hash the input. 
            string hashOfInput = GetSha256Hash(sha256Hash, input);

            if (String.Compare(hashOfInput, hash, StringComparison.OrdinalIgnoreCase) == 0)
                return true;

            return false;
        }
    }
}