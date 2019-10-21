using Domain;
using Microsoft.Practices.Unity;
using System;
using System.Web;

namespace Business.Infrastructure
{


    /// <summary>
    /// La seguente classe si occupa di gestire il ciclo di vita di un oggetto seguento la session di PowerWeb
    /// (ossia quella che viene creata al login e distrutta al logout) cosi un oggetto viene creato e distrutto solo in quei due casi
    /// </summary>
    /// <seealso cref="Microsoft.Practices.Unity.LifetimeManager" />
    public class UnityPerSessionContextLifeTimeManager : LifetimeManager
    {
        Guid _key;

        public UnityPerSessionContextLifeTimeManager() : this(Guid.NewGuid()) { }


        UnityPerSessionContextLifeTimeManager(Guid key)
        {
            if (key == Guid.Empty)
                throw new ArgumentException("Key cannot be empty");

            _key = key;
        }

        public override object GetValue()
        {
            object result = null;

            if(PowerWebContext.Current != null)
            {
                if (PowerWebContext.GetFromSession<object>(_key.ToString()) != null)
                    result = PowerWebContext.GetFromSession<object>(_key.ToString());
            }

            return result;
            
        }

        public override void SetValue(object newValue)
        {
            if (PowerWebContext.Current != null)
            {
                PowerWebContext.SetToSession(_key.ToString(), newValue);
            }
        }

        public override void RemoveValue()
        {
            if (PowerWebContext.Current == null)
            {
                HttpContext.Current.Session[_key.ToString()] = null;
            }
        }
    }
}
