using Business.DataClasses.WebApiDataClasses;
using log4net;
using System;
using System.IO;

namespace Business.IocFactory.FileDecoder.FileDecoders
{
    class Mp3FileDecoder : FileDecoderToolBox, IFileDecoder
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Mp3FileDecoder));

        public void Decode(FileContainerJson json)
        {
            try
            {
                byte[] binaryData = Convert.FromBase64String(json.Data);
                
                string completePath = CreatePath(json.Device, json.FileName);

                File.WriteAllBytes(completePath, binaryData);
            }
            catch(Exception ex)
            {

                _log.ErrorFormat("Errore durante la decodifica da json di un file mp3 con exception : {0}", ex.Message);
                
            }
            
        }
    }
}
