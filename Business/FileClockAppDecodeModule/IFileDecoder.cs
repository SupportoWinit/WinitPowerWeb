using Business.DataClasses.WebApiDataClasses;

namespace Business.IocFactory.FileDecoder
{
    public interface IFileDecoder
    {
        void Decode(FileContainerJson json);

    }
}
