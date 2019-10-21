using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Domain;

namespace Business.Repository.Custom
{
    public interface IResourcesRepository : IRepository<Resources>
    {
        Dictionary<string, string> ResourcesDictionary { get; }

        void ResetResourcesDictionary();

        String GetResourcesDictionaryString(string value);
    }
}
