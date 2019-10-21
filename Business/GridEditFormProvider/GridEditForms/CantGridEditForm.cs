using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Business.GridEditFormProvider.Components;

namespace Business.GridEditFormProvider.GridEditForms
{
    public class CantGridEditForm : GridEditFormBase, IGridEditFormProvider
    {
        public JObject Build()
        {

            Form mainForm = new Form();
            mainForm.ColCount = 1;

            mainForm.Items = new List<FormItem>();

            TabbedItem mainContent = new TabbedItem();

            mainContent.Tabs = new List<Tab>();
            mainContent.ItemType = "tabbed";

            #region GENERALI

            Tab generaliTab = new Tab();

            generaliTab.Title = "Generale";
            generaliTab.Items = new List<FormItem>();
            generaliTab.ColCount = 2;
            
            generaliTab.Items.Add(GenerateSimpleItem("Codice_Cantiere"));
            generaliTab.Items.Add(GenerateSimpleItem("Descrizione_Can"));
            generaliTab.Items.Add(GenerateSimpleItem("Tipologia_Can", Enums.EditorTypeEnum.DxSelectBox));
            generaliTab.Items.Add(GenerateSimpleItem("Cli_Id", Enums.EditorTypeEnum.DxSelectBox));
            generaliTab.Items.Add(GenerateSimpleItem("Fil_Id", Enums.EditorTypeEnum.DxSelectBox));
            generaliTab.Items.Add(GenerateSimpleItem("DisAbilitazione_Can", Enums.EditorTypeEnum.DxCheckBox));
            generaliTab.Items.Add(GenerateSimpleItem("Data_Nascita_Can", Enums.EditorTypeEnum.DxDateBox, new { readOnly = true}));
            generaliTab.Items.Add(GenerateSimpleItem("Data_Registrazione_Can", Enums.EditorTypeEnum.DxDateBox, new { readOnly = true }));
            generaliTab.Items.Add(GenerateSimpleItem("DataOraUltimaModifica_Can", Enums.EditorTypeEnum.DxDateBox, new { readOnly = true }));
            generaliTab.Items.Add(GenerateSimpleItem("Singola_Reg", Enums.EditorTypeEnum.DxCheckBox));

            mainContent.Tabs.Add(generaliTab);

            #endregion

            #region UBICAZIONE

            Tab ubicazioneTab = new Tab();

            ubicazioneTab.Title = "Ubicazione";
            ubicazioneTab.Items = new List<FormItem>();
            ubicazioneTab.ColCount = 2;

            ubicazioneTab.Items.Add(GenerateSimpleItem("RaggioGps_Can", Enums.EditorTypeEnum.DxNumberBox));
            ubicazioneTab.Items.Add(GenerateSimpleItem("Provincia_Can"));
            ubicazioneTab.Items.Add(GenerateSimpleItem("Luogo_Can"));
            ubicazioneTab.Items.Add(GenerateSimpleItem("Cap_Can"));
            ubicazioneTab.Items.Add(GenerateSimpleItem("Indirizzo_Can"));

            ubicazioneTab.Items.Add(GenerateSimpleItem("RaggioGps_Can", Enums.EditorTypeEnum.DxNumberBox));
            ubicazioneTab.Items.Add(GenerateSimpleItem("LatitudineGps_Can", Enums.EditorTypeEnum.DxNumberBox));
            ubicazioneTab.Items.Add(GenerateSimpleItem("LongitudineGps_Can", Enums.EditorTypeEnum.DxNumberBox));

            mainContent.Tabs.Add(ubicazioneTab);

            #endregion

            mainForm.Items.Add(mainContent);

            return JObject.FromObject(mainForm);
        }
    }
}
