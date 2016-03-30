using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.Web.Http;



namespace Dmx.Windows.MPC.InkToOcr
{
    public static class ProjOxWrapper
    {
        const string OxOCRURI = "https://api.projectoxford.ai/vision/v1/ocr?detectOrientation=true";

        public async static Task<string> PostToOnlineOcrRecoAsync(StorageFile file, string subscriptionKey )
        {
            HttpClient cl = new HttpClient();
            cl.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey );
            cl.DefaultRequestHeaders.Add("ContentType", "application/octet-stream");            

            var fileContent = await file.OpenReadAsync();
            HttpMultipartFormDataContent fdc = new HttpMultipartFormDataContent();
            HttpStreamContent con = new HttpStreamContent(fileContent);
            fdc.Add(con);

            var res = await cl.PostAsync(new Uri(OxOCRURI), fdc);
            var obj = DeserializeJson<ProjOxOcrResponse>(res.Content.ToString());

            var resString = "";
            if (obj != null)
            {
                if (obj.regions != null)
                {
                    foreach (var item in obj.regions)
                    {
                        if (item != null)
                        {
                            foreach (var line in item.lines)
                            {
                                if (line != null)
                                {
                                    foreach (var word in line.words)
                                    {
                                        if (word != null)
                                        {

                                            resString += word.text + " ";
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return resString;

        }

        public static T DeserializeJson<T>(string json)
        {
            var _Bytes = Encoding.Unicode.GetBytes(json);
            using (MemoryStream _Stream = new MemoryStream(_Bytes))
            {
                var _Serializer = new DataContractJsonSerializer(typeof(T));
                return (T)_Serializer.ReadObject(_Stream);
            }
        }
    }
}
