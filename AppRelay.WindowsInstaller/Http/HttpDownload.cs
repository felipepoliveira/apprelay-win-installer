using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AppRelay.WindowsInstaller.Http
{
    internal class HttpDownload
    {
        public delegate void DownloadChunkCallback(int downloadedChunkSize, long totalReadedBytes, long contentLength);

        /// <summary>
        /// Download the content from the given file HTTP URI and write it on the given stream. The stream will be considered an output stream.
        /// </summary>
        /// <param name="fileHttpUri">The URI that will be downloaded</param>
        /// <param name="outputStream">The output stream that the bytes will be written</param>
        /// <param name="bufferSize">Total buffer size used to iterate over the downloaded chunks</param>
        /// <param name="downloadChunkCallback">A callback that will be triggered each time an chunk of the file is downloaded</param>
        public static void DownloadToStream(Uri fileHttpUri, Stream outputStream, int bufferSize, DownloadChunkCallback downloadChunkCallback)
        {
            //Make data validation
            if (bufferSize < 0) throw new ArgumentOutOfRangeException($"Buffer size must be > 0");

            // create the request object
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var request = HttpWebRequest.Create(fileHttpUri);

            // get the response
            var response = request.GetResponse();

            // check if response returned data
            if (response.ContentLength > 0)
            {
                // create the response buffer
                byte[] responseBuffer = new byte[bufferSize];

                // get the response stream
                var responseStream = response.GetResponseStream();

                // Download the response and write it on the output stream
                int length = 0;
                long totalReadedBytes = 0;
                while (responseStream.CanRead && (length = responseStream.Read(responseBuffer, 0, bufferSize)) > 0) 
                {
                    totalReadedBytes += length;
                    outputStream.Write(responseBuffer, 0, length);
                    downloadChunkCallback(length, totalReadedBytes, response.ContentLength);
                }

                outputStream.Close();
            }

            response.Close();
        }


    }
}
