using Business.BusinessClasses.Interfaces;
using Business.BusinessClasses.Jsonable.Interfaces;

namespace Business.BusinessServices.CartellinoService.Interfaces.Timesheet
{
    public interface ITimesheet : IxlsxPrintable, IJsonable
    {
        void BeforeCompute();
        void Compute();
        void AfterCompute();
    }
}
