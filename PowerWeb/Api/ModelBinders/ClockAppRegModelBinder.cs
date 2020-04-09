using Business.BusinessServices.RegTranslatorService.Classes.JsonReg;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Web.Http.Controllers;
using System.Web.Http.ModelBinding;

namespace PowerWeb.Api.ModelBinders
{
    public class ClockAppRegModelBinder : IModelBinder
    {
        public bool BindModel(HttpActionContext actionContext, ModelBindingContext bindingContext)
        {
            string content = actionContext.Request.Content.ReadAsStringAsync().Result;

            if (String.IsNullOrEmpty(content))
                return false;

            IEnumerable<ClockAppReg> obj = JArray.Parse(content).ToObject<List<ClockAppReg>>();
            bindingContext.Model = obj;
            return true;
        }
    }
}