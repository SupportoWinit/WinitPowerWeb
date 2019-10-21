using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Exceptions
{
    [Serializable]
    public class RowDeletingException : Exception
    {
        Dictionary<string, string> Errors { get; set; }

        public RowDeletingException()
            : base()
        {
            Errors = new Dictionary<string, string>();
        }

        public RowDeletingException(Dictionary<string, string> errors)
            : base(Common.CommonService.GetErrorMessageFromDictionary(errors))
        {
            Errors = errors;
        }
    }
}
