using Business.DataClasses.WebApiDataClasses;
using log4net;
using System.Collections.Generic;
using System.Linq;

namespace Business.IocFactory.FileDecoder
{
    public static class FileClockAppDecoder
    {
        private static readonly ILog _log;

        private static IDictionary<string, IFileDecoder> _decoderInstances;

        static FileClockAppDecoder()
        {
            _log = LogManager.GetLogger(typeof(FileClockAppDecoder));

            _decoderInstances = new Dictionary<string, IFileDecoder>();
        }

        public static void Decode(FileContainerJson json)
        {
            string extension = json.FileName.Split('.').Last();

            if (!_decoderInstances.ContainsKey(extension))
                _decoderInstances.Add(extension, FileDecoderFactory.CreateInstance(extension));

            _decoderInstances[extension].Decode(json);
        }
    }
}
