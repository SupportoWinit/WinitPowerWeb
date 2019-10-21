using System;

namespace Business.GridHelpers.Classes
{
    internal class DateColumn : Column
    {

        public DateColumn(Type propertyType, string propertyName) : base(propertyType, propertyName)
        {
            base.EditorOptions = new
            {
                zoomLevel = "month"
            };
        }
    }
}