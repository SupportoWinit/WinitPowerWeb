using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.BusinessExtension
{
    public class CoordinatesData
    {
        #region Properties

        /// <summary>
        /// Recupera o imposta la latitudine corrente.
        /// </summary>
        /// <value>
        /// La latitudine corrente.
        /// </value>
        public double CurrentLatitude { get; set; }

        /// <summary>
        /// Recupera o imposta la longitudine corrente.
        /// </summary>
        /// <value>
        /// La longitudine corrente.
        /// </value>
        public double CurrentLongitude { get; set; }

        /// <summary>
        /// Recupera o imposta lo stato di where is it.
        /// </summary>
        /// <value>
        /// Lo stato di where is it.
        /// </value>
        public string WhereIsItState { get; set; }

        /// <summary>
        /// Recupera o imposta il titolo dell'infobox legato al pushpin visualizzato dal where is it.
        /// </summary>
        /// <value>
        /// Il titolo dell'infobox legato al pushpin visualizzato dal where is it.
        /// </value>
        public string InfoboxTitle { get; set; }

        /// <summary>
        /// Recupera o imposta la descrizione dell'infobox legato al pushpin visualizzato dal where is it.
        /// </summary>
        /// <value>
        /// La descrizione dell'infobox legato al pushpin visualizzato dal where is it.
        /// </value>
        public string InfoboxDescription { get; set; }

        /// <summary>
        /// Recupera o imposta la latitudine originale.
        /// </summary>
        /// <value>
        /// La latitudine originale.
        /// </value>
        public double OriginalLatitude { get; set; }

        /// <summary>
        /// Recupera o imposta la longitudine originale.
        /// </summary>
        /// <value>
        /// La latitudine longitudine originale.
        /// </value>
        public double OriginalLongitude { get; set; }

        /// <summary>
        /// Recupera o imposta il numero di elementi in sovrapposizione.
        /// </summary>
        /// <value>
        /// Il numero di elementi in sovrapposizone.
        /// </value>
        public int OverlappingNumber { get; set; }


        /// <summary>
        /// Recupera o imposta la label del pushpin.
        /// </summary>
        /// <value>
        /// Label del pushpin.
        /// </value>
        public string PushpinLabel { get; set; }

        #endregion
    }
}
