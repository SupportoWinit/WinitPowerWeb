using Domain;

namespace Exports.ExportExcelGeneric
{
    public class ExportExcelGenericCantV : ExportExcelGeneric<Cant_V>
    {
        public override string SheetName
        {
            get { return "Attività"; }
        }
    }
}