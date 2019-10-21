using Domain;
using System;
using System.Collections.Generic;

namespace Business.IocFactory.ClockAppSynchronizationFactory
{
    public static class DictionaryEntityControllers
    {

        private static Dictionary<Type, string> typesDictionary;

        static DictionaryEntityControllers()
        {
            typesDictionary = new Dictionary<Type, string>();
            typesDictionary.Add(typeof(Cant), "//api/ManagePowerWebCant/");
            typesDictionary.Add(typeof(Col), "//api/ManagePowerWebCol/");
            typesDictionary.Add(typeof(Fru), "//api/ManagePowerWebFru/");
            typesDictionary.Add(typeof(Pru), "//api/ManagePowerWebPru/");
            typesDictionary.Add(typeof(Tab_Damage), "//api/ManagePowerWebTab_Damage/");
        }

        public static string GetControllerPath<T>()
        {

            if (!typesDictionary.ContainsKey(typeof(T)))
                return null;

            return typesDictionary[typeof(T)];

        }

    }
}
