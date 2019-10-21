using System;
using Business.DataClasses.WebApiDataClasses;
using System.IO;
using log4net;

namespace Business.IocFactory.FileDecoder.FileDecoders
{
    class JpgFileDecoder : FileDecoderToolBox, IFileDecoder
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(JpgFileDecoder));

        public void Decode(FileContainerJson json)
        {
            try
            {
                byte[] binaryData = Convert.FromBase64String(json.Data);

                string completePath = CreatePath(json.Device, json.FileName);

                File.WriteAllBytes(completePath, binaryData);
            }
            catch (Exception ex)
            {
                _log.ErrorFormat("Errore durante la decodifica da json di un file jpg con exception : {0}", ex.Message);
            }
        }
    }
}
