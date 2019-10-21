using System;
using System.Collections.Generic;
using System.Linq;
using Data;
using Domain;
using System.Text;
using Common;
using Business.MDBSchema;

namespace Business.Repository.Custom
{
    public class TabDistanzeRepository : GenericRepository<Tab_Dist>, ITabDistanzeRepository
    {
        public TabDistanzeRepository(PowerWebEntities context)
            : base(context)
        {
        }

        private static List<Tab_Decod> Tab_Decods
        {
            get
            {
                List<Tab_Decod> oLista = PowerWebContext.GetFromSession<List<Tab_Decod>>("Tab_Decods_Tab_DistanzeRepo");
                if (oLista == null)
                {
                    oLista = RepoManager.TabDecodRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Tab_Decod>>("Tab_Decods_Tab_DistanzeRepo", oLista);
                }
                return oLista;
            }
        }

        private static void ResetSession()
        {
            PowerWebContext.SetToSession<List<Tab_Decod>>("Tab_Decods_Tab_DistanzeRepo", null);
        }

        public Tab_Dist Init()
        {
            Tab_Dist oNewRecord = new Tab_Dist()
            {
            };
            return oNewRecord;
        }

        public override Dictionary<string, string> Check(Tab_Dist entity, bool isNew = false, bool isResetSession = true)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            //      
            //1) verifico che il Valore della Chiave sia impostato perché è obbligatorio e che sia univoco
            //
            //
            //TODO AGGIUNGERE CONTROLLO SU CHIAVE UNIVOCA 
            //
            //2) verifico i campi obbligatori e che siano eventualmente presenti nella relativa Tabella
            //
            // TODO AGGIUNGERE CONTROLLI MANCANTI
            //
            //
            //
            //3) verifico, per una serie di campi, che il valore di un campo sia minore del valore di un altro campo
            //
            //
            //4) verifico, per una serie di campi, che il valore del campo sia corretto
            //
            //if (CommonService.Nz(entity.Tipo_Tab_Dist, "") == "")
            //    result.Add(CommonService.GetPropertyName(() => entity.Tipo_Tab_Dist),
            //      CommonService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO,
            //      PowerWebResources.FLD_TIPO_PARTENZA_TAB_Dist));
            //if (CommonService.Nz(entity.Partenza_Tab_Dist, "") == "")
            //    result.Add(CommonService.GetPropertyName(() => entity.Partenza_Tab_Dist),
            //      CommonService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO,
            //      PowerWebResources.FLD_PARTENZA_TAB_Dist));
            //if (CommonService.Nz(entity.Arrivo_Tab_Dist, "") == "")
            //    result.Add(CommonService.GetPropertyName(() => entity.Arrivo_Tab_Dist),
            //      CommonService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO,
            //      PowerWebResources.FLD_ARRIVO_TAB_Dist));
            //
            //5) verifico, per una serie di campi, che il valore del campo sia presente nella relativa Tabella
            //      
            //if (!String.IsNullOrEmpty(entity.Tipo_Tab_Dist))
            //    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_SYS.ToString()
            //    && x.Nome_Tab == TabDecodNameEnum.TIPO_DISTANZA.ToString() && x.Chiave_Tab == entity.Tipo_Tab_Dist) == null)
            //        result.Add(CommonService.GetPropertyName(() => entity.Tipo_Tab_Dist),
            //        CommonService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
            //        PowerWebResources.FLD_TIPO_PARTENZA_TAB_DISTANZE));


            //
            //6) Scrittura del Record di LOG
            //
            WriteCheckLog(entity, result, Log);
            return result;
        }

        public override void ImportFromDataSet(PowerMDBDataSet oDataSet,
        ref Tab_Chk_Imp oTab_Chk_Imp, Tab_Chk_Imp[] oArrayTab_Chk_Imp, bool bTrattaSoloErrori)
        {
            Dictionary<string, string> oResultDictionary = new Dictionary<string, string>();
            List<PowerMDBDataSet.Tab_DistRow> oListaAccess = RepoManager.Tab_Chk_ImpRepo.OttieniListaRecordDaTrattare(
              oDataSet.Tab_Dist.ToList(), ref oTab_Chk_Imp, oArrayTab_Chk_Imp, bTrattaSoloErrori,
              "Tab_Dist", oDataSet.Tab_Dist.RRNColumn.ColumnName);
            if (oListaAccess == null)
                return;//esco: devo proseguire l'import da un'altra tabella
            foreach (PowerMDBDataSet.Tab_DistRow oRow in oListaAccess)
            {
                Tab_Dist oNewRecord = this.Init();
                oNewRecord.Partenza_Tab_Dist = oRow.Partenza_Tab_Dist;
                oNewRecord.Arrivo_Tab_Dist = oRow.Arrivo_Tab_Dist;

                oNewRecord.Tipo_Tab_Dist = (int)Enum.Parse(typeof(DistTypeEnum), oRow.Tipo_Arrivo_Tab_Dist);
                oNewRecord.KM_Tab_Dist = oRow.IsKM_Tab_DistNull() ? (double)0 : oRow.KM_Tab_Dist;
                oNewRecord.Ore_Tab_Dist = oRow.IsOre_Tab_DistNull() ? (double)0 : oRow.Ore_Tab_Dist;
                oResultDictionary = Check(oNewRecord, true);
                if (oResultDictionary.Count == 0)
                    Add(oNewRecord, true);
                RepoManager.Tab_Chk_ImpRepo.AggiornaSituazioneImport((oResultDictionary.Count == 0), bTrattaSoloErrori, "Tab_Dist", oRow.RRN.ToString());
            }
            ResetSession();
        }
    }
}
