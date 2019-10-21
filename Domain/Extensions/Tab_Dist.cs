using Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Domain
{
    public partial class Tab_Dist
    {
        public DistTypeEnum DistType
        {
            get
            {
                var currentType = DistTypeEnum.None;
                if (Tab_Decod != null)
                {
                    var stringValue = Tab_Decod.Chiave_Tab;
                    if (stringValue == "C")
                        currentType = DistTypeEnum.Cant;
                    if (stringValue == "K")
                        currentType = DistTypeEnum.Cap;
                    if (stringValue == "Z")
                        currentType = DistTypeEnum.Zone;
                    if (stringValue == "P")
                        currentType = DistTypeEnum.Place; 
                    if (stringValue == "G")
                        currentType = DistTypeEnum.GIS;
                }
                return currentType;
            }
        }
    }
}
