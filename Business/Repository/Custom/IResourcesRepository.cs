using Domain;
using System;
using System.Collections.Generic;

namespace Business.Repository.Custom
{
    public interface IResourcesRepository : IRepository<Resources>
    {
        Dictionary<string, string> ResourcesDictionary { get; }

        void ResetResourcesDictionary();

        String GetResourcesDictionaryString(string value);
    }
}
