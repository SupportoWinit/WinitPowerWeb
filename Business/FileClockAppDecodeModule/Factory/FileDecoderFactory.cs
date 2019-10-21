
using Business.IocFactory.FileDecoder.FileDecoders;
using log4net;
using System;
using System.Collections.Generic;

namespace Business.IocFactory.FileDecoder
{
    public static class FileDecoderFactory
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(FileDecoderFactory));

        private static IDictionary<string, Type> container;

        static FileDecoderFactory()
        {
            container = new Dictionary<string, Type>();

            container.Add("mp3", typeof(Mp3FileDecoder));
            container.Add("jpg", typeof(JpgFileDecoder));
        }

        public static IFileDecoder CreateInstance(string fileType)
        {

            if (!container.ContainsKey(fileType))
            {
                _log.ErrorFormat("Errore causato da richiesta decodifica per entità non registrata {0}", fileType);
                return null;
            }

            return Activator.CreateInstance(container[fileType]) as IFileDecoder;
        }
    }
}
