
namespace BuildManager
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;

    internal static class WebQueryHelper
    {
        /// <summary>
        /// Get data from the given url.
        /// </summary>
        /// <param name="url">The url to query.</param>
        /// <param name="token">The access token to use.</param>
        /// <returns>The serialized response.</returns>
        internal static string GetData(
            string url,
            string token)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    "Basic",
                    Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(string.Format("{0}:{1}", string.Empty, token))));

                using (HttpResponseMessage response = client.GetAsync(url).Result)
                {
                    string responseBody = response.Content.ReadAsStringAsync().Result;
                    response.EnsureSuccessStatusCode();

                    return responseBody;
                }
            }
        }

        /// <summary>
        /// Post data to the given url.
        /// </summary>
        /// <param name="url">The url to query.</param>
        /// <param name="payload">The payload for the post.</param>
        /// <param name="token">The access token to use.</param>
        /// <returns>The serialized response.</returns>
        internal static string PostData(
            string url,
            string payload,
            string token)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    "Basic",
                    Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(string.Format("{0}:{1}", string.Empty, token))));

                using (StringContent content = new StringContent(payload, Encoding.UTF8, "application/json"))
                {
                    using (HttpResponseMessage response = client.PostAsync(url, content).Result)
                    {
                        string responseBody = response.Content.ReadAsStringAsync().Result;
                        response.EnsureSuccessStatusCode();

                        return responseBody;
                    }
                }
            }
        }

        /// <summary>
        /// Put data to the given url.
        /// </summary>
        /// <param name="url">The url to query.</param>
        /// <param name="payload">The payload for the post.</param>
        /// <param name="token">The access token to use.</param>
        /// <returns>The serialized response.</returns>
        internal static string PutData(
            string url,
            string payload,
            string token)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    "Basic",
                    Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(string.Format("{0}:{1}", string.Empty, token))));

                using (StringContent content = new StringContent(payload, Encoding.UTF8, "application/json"))
                {
                    using (HttpResponseMessage response = client.PutAsync(url, content).Result)
                    {
                        string responseBody = response.Content.ReadAsStringAsync().Result;
                        response.EnsureSuccessStatusCode();

                        return responseBody;
                    }
                }
            }
        }

        /// <summary>
        /// Delete data at the given url.
        /// </summary>
        /// <param name="url">The url to query.</param>
        /// <param name="token">The access token to use.</param>
        /// <returns>The serialized response.</returns>
        internal static string DeleteData(
            string url,
            string token)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    "Basic",
                    Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(string.Format("{0}:{1}", string.Empty, token))));

                using (HttpResponseMessage response = client.DeleteAsync(url).Result)
                {
                    string responseBody = response.Content.ReadAsStringAsync().Result;
                    response.EnsureSuccessStatusCode();

                    return responseBody;
                }
            }
        }
    }
}
