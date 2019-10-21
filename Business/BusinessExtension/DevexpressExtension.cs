using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Web.ASPxCallbackPanel;

namespace Business.BusinessExtension
{
    /// <summary>
    /// Classe utilizzata per raccogliere tutte le estensioni da applicare agli elementi di devexpress
    /// </summary>
    public static class DevexpressExtension
    {

        #region Public Static Methods

        /// <summary>
        /// Inizializza (aggiunge se necessario ed imposta il valore) della proprietà specifica sull'elemento pannello specifico.
        /// </summary>
        /// <param name="jsPropContainer">Il pannello contenitore della jsproperty da inizializzare.</param>
        /// <param name="jsPropName">Il nome della jsproperty da inizializzare.</param>
        /// <param name="jsPropDefaultValue">Il valore della jsproperty da inizializzare.</param>
        public static void InitializeJsProperty(this ASPxCallbackPanel jsPropContainer, string jsPropName, object jsPropDefaultValue)
        {
            if (!jsPropContainer.JSProperties.ContainsKey(jsPropName))
                jsPropContainer.JSProperties.Add(jsPropName, jsPropDefaultValue);
        }

        #endregion

    }
}
