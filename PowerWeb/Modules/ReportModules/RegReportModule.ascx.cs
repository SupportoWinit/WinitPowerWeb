using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Business;
using Business.Repository;
using Common;
using DevExpress.Web.ASPxGridView;
using DevExpress.Web.ASPxPanel;
using Domain;
using Business.Repository.Custom;

namespace PowerWeb.Modules.ReportModules
{
    public partial class RegReportModule : System.Web.UI.UserControl, IPrintCustomModule
    {
        private Col _colStub = null;

        private Cant _cantStub = null;

        private Tab_Decod _tabDecodStub = null;

        public string GridViewID { get { return "gvRegV"; } }

        /// <summary>
        /// Recupera il template della form utilizzata per il recupero dei dati di raggruppamento in stampa;
        /// se impostato a null si utilizza il valore specificato nel modulo.
        /// </summary>
        /// <value>
        /// il template della form utilizzata per il recupero dei dati di raggruppamento in stampa;
        /// se impostato a null si utilizza il valore specificato nel modulo.
        /// </value>
        public PowerFormTemplate PrintFormTemplate
        {
            get
            {
                return null;
            }
        }

        public List<Col> Cols
        {
            get
            {
                List<Col> currentCols = PowerWebContext.GetFromSession<List<Col>>("Cols_" + GridViewID);
                if (currentCols == null)
                {

                    currentCols = RepoManager.ColRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Col>>("Cols_" + GridViewID, currentCols);
                }
                return currentCols;
            }
        }

        public List<Cant> Cants
        {
            get
            {
                List<Cant> currentCants = PowerWebContext.GetFromSession<List<Cant>>("Cants_" + GridViewID);
                if (currentCants == null)
                {
                    currentCants = RepoManager.CantRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Cant>>("Cants_" + GridViewID, currentCants);
                }
                return currentCants;
            }
        }

        private void BindColGridLookup()
        {
            glCol.KeyFieldName = CommonService.GetPropertyName(() => _colStub.Col_Id);
            glCol.DataSource = Cols;
            if (!Page.IsCallback && !Page.IsPostBack)
                glCol.DataBind();
        }

        private void BindCantGridLookup()
        {
            glCant.KeyFieldName = CommonService.GetPropertyName(() => _cantStub.Cant_Id);
            glCant.DataSource = Cants;
            if (!Page.IsCallback && !Page.IsPostBack)
                glCant.DataBind();
        }

        private void BindFilColGridLookup()//TODONOW codice da cambiare usando la tabella Fil
        {
          //glFilCol.KeyFieldName = CommonService.GetPropertyName(() => _tabDecodStub.Tab_Decod_Id);
          //glFilCol.DataSource = RepoManager.TabDecodRepo.GetAllParametrized(TabDecodGroupTypeEnum.DECOD_TAB.ToString(), TabDecodNameEnum.FILIALI.ToString());
          //if (!Page.IsCallback && !Page.IsPostBack)
          //  glFilCol.DataBind();
        }

        private void BindFilCantGridLookup()//TODONOW codice da cambiare usando la tabella Fil
        {
          //glFilCant.KeyFieldName = CommonService.GetPropertyName(() => _tabDecodStub.Tab_Decod_Id);
          //glFilCant.DataSource = RepoManager.TabDecodRepo.GetAllParametrized(TabDecodGroupTypeEnum.DECOD_TAB.ToString(), TabDecodNameEnum.FILIALI.ToString());
          //if (!Page.IsCallback && !Page.IsPostBack)
          //  glFilCant.DataBind();
        }

        private void BindRespColGridLookup()//TODONOW codice da cambiare usando la tabella Resp
        {
          //glRespCol.KeyFieldName = CommonService.GetPropertyName(() => _tabDecodStub.Tab_Decod_Id);
          //glRespCol.DataSource = RepoManager.TabDecodRepo.GetAllParametrized(TabDecodGroupTypeEnum.DECOD_TAB.ToString(), TabDecodNameEnum.RESPONSABILI.ToString());
          //if (!Page.IsCallback && !Page.IsPostBack)
          //  glRespCol.DataBind();
        }

        private void BindRespCantGridLookup()//TODONOW codice da cambiare usando la tabella Resp
        {
          //glRespCant.KeyFieldName = CommonService.GetPropertyName(() => _tabDecodStub.Tab_Decod_Id);
          //glRespCant.DataSource = RepoManager.TabDecodRepo.GetAllParametrized(TabDecodGroupTypeEnum.DECOD_TAB.ToString(), TabDecodNameEnum.RESPONSABILI.ToString());
          //if (!Page.IsCallback && !Page.IsPostBack)
          //  glRespCant.DataBind();
        }

        private void BindMotGridLookup()
        {
          glMotivazioni.KeyFieldName = CommonService.GetPropertyName(() => _tabDecodStub.Tab_Decod_Id);
          glMotivazioni.DataSource = RepoManager.Tab_DecodRepo.GetAllParametrized(TabDecodGroupTypeEnum.DECOD_TAB.ToString(), TabDecodNameEnum.MOTIVAZIONI.ToString());
          if (!Page.IsCallback && !Page.IsPostBack)
            glMotivazioni.DataBind();
        }

        protected void Page_Init(object sender, EventArgs e)
        {
            PowerWebService.FillGridLabels(typeof(Col), glCol.GridView);
            BindColGridLookup();
            PowerWebService.FillGridLabels(typeof(Cant), glCant.GridView);
            BindCantGridLookup();
            PowerWebService.FillGridLabels(typeof(Tab_Decod), glFilCol.GridView);
            BindFilColGridLookup();
            PowerWebService.FillGridLabels(typeof(Tab_Decod), glFilCant.GridView);
            BindFilCantGridLookup();
            PowerWebService.FillGridLabels(typeof(Tab_Decod), glRespCol.GridView);
            BindRespColGridLookup();
            PowerWebService.FillGridLabels(typeof(Tab_Decod), glRespCant.GridView);
            BindRespCantGridLookup();
            PowerWebService.FillGridLabels(typeof(Tab_Decod), glMotivazioni.GridView);
            BindMotGridLookup();
        }

        public ASPxPanel CustomOptionsPanel
        {
            get { return customPrintOptionsPanel; }
        }

        /// <summary>
        /// Recupera la griglia utilizzata per il recupero dei dati di stampa;
        /// se impostato a null si utilizza la griglia impostata nella proprietà GridView.
        /// </summary>
        /// <value>
        /// La griglia utilizzata per il recupero dei dati di stampa;
        /// se impostato a null si utilizza la griglia impostata nella proprietà GridView.
        /// </value>
        /// <exception cref="NotImplementedException"></exception>
        public ASPxGridView PrintGridView
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public ExtXtraReport GetReport(Tab_Report report, List<TabPageExtended> selectedTabs, Dictionary<string, int> reportOptions, List<GroupingTreeListItem> groups, List<object> items, ASPxPanel customOptionsPanel = null)
        {
            throw new NotImplementedException();
        }
    }
}