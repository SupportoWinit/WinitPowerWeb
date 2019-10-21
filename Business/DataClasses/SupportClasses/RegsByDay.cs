using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DataClasses.SupportClasses
{
    /// <summary>
    /// Classe utilizzata per contentere il conteggio girnaliero delle registrazioni
    /// nel db
    /// </summary>
    public class RegsByDay
    {
        public DateTime Date { get; set; }

        public int Count { get; set; }
    }
}
