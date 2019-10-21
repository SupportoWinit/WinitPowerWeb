using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Business.LicenceServiceReference;

namespace Business.BusinessExtension
{
    /// <summary>
    /// Classe che contiene una lista di <see cref="ModulesActivation"/> ed è utilizzata per la sua serializzazione su xml
    /// </summary>
    public class ModulesActivationList
    {

        #region Fields
        
        /// <summary>
        /// L'elenco dei moduli da attivare/disattivare sull'applicazione
        /// </summary>
        private List<ModulesActivation> _modulesActivation;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ModulesActivationList"/> class.
        /// </summary>
        public ModulesActivationList() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModulesActivationList"/> class.
        /// </summary>
        /// <param name="modulesActivation">The modules activation.</param>
        public ModulesActivationList (IEnumerable<ModulesActivation> modulesActivation)
        {
            _modulesActivation = modulesActivation.ToList();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Recupera o imposta l'elenco dei moduli da attivare/disattivare sull'applicazione.
        /// </summary>
        /// <value>
        /// L'elenco dei moduli da attivare/disattivare sull'applicazione.
        /// </value>
        public List<ModulesActivation> ModulesActivation
        {
            get { return _modulesActivation; }
            set { _modulesActivation = value; }
        }
        

        #endregion

    }
}
