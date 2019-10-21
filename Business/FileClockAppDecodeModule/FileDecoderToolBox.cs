using System.IO;

namespace Business.IocFactory.FileDecoder
{
    public abstract class FileDecoderToolBox
    {
        public string CreatePath(string folder,string fileName)
        {
            string path = System.Web.HttpContext.Current.Server.MapPath(Common.Properties.Settings.Default.Files_Input_UserFiles);

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);


            path = Path.Combine(path, folder);

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            return Path.Combine(path, fileName);
        }

    }
}
