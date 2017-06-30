using System;
using System.Security.Cryptography;

namespace EPC.ReferenceIntegration.Helpers
{
    public class WebHookHelper
    {
        /// <summary>
        /// Gets the Webhook Notification Token based on the message and secret.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="secret"></param>
        /// <returns></returns>
        public static string GetWebhookNotificationToken(string message, string secret)
        {
            secret = secret ?? "";
            var encoding = new System.Text.ASCIIEncoding();
            byte[] keyByte = encoding.GetBytes(secret);
            byte[] messageBytes = encoding.GetBytes(message);
            using (var hmacsha256 = new HMACSHA256(keyByte))
            {
                byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
                return Convert.ToBase64String(hashmessage);
            }
        }

        /// <summary>
        /// Compares the ElliSignature from Notification header with the WebHook token generate from the secret key
        /// </summary>
        /// <param name="elliSignature"></param>
        /// <param name="webHookToken"></param>
        /// <returns></returns>
        public static bool IsValidWebhookToken(string elliSignature, string webHookToken)
        {
            var isValid = false;

            if (string.Compare(elliSignature, webHookToken, StringComparison.OrdinalIgnoreCase) == 0)
                isValid = true;

            return isValid;
        }

    }
}
