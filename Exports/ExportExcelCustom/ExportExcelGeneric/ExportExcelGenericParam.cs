using Domain;

namespace Exports.ExportExcelGeneric
{
    public class ExportExcelGenericParam : ExportExcelGeneric<Param>
    {
        public override string SheetName
        {
            get { return "Parametri"; }
        }
    }
}