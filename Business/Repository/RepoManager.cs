using Business.Infrastructure;
using Business.Repository.Custom;
using Data;
using Domain;

namespace Business.Repository
{
    public static class RepoManager
    {

        public static ICantRepository CantRepo
        {
            get { return IoC.Resolve<ICantRepository>(); }
        }
        public static ICant_VRepository Cant_VRepo
        {
            get { return IoC.Resolve<ICant_VRepository>(); }
        }
        public static IRepository<Cant_Fil_V> Cant_Fil_VRepo
        {
            get { return IoC.Resolve<IRepository<Cant_Fil_V>>(); }
        }
        public static ICant_NoteRepository Cant_NoteRepo
        {
            get { return IoC.Resolve<ICant_NoteRepository>(); }
        }
        public static ICant_VarRepository Cant_VarRepo
        {
            get { return IoC.Resolve<ICant_VarRepository>(); }
        }
        public static ICliRepository CliRepo
        {
            get { return IoC.Resolve<ICliRepository>(); }
        }
        public static IColRepository ColRepo
        {
            get { return IoC.Resolve<IColRepository>(); }
        }
        public static ICol_VRepository Col_VRepo
        {
            get { return IoC.Resolve<ICol_VRepository>(); }
        }
        public static IRepository<Col_Monte_Minuti> Col_Monte_MinutiRepo
        {
            get { return IoC.Resolve<IRepository<Col_Monte_Minuti>>(); }
        }
        public static ICol_NoteRepository Col_NoteRepo
        {
            get { return IoC.Resolve<ICol_NoteRepository>(); }
        }
        public static IRepository<Col_Resp_V> Col_Resp_VRepo
        {
            get { return IoC.Resolve<IRepository<Col_Resp_V>>(); }
        }
        public static ICol_VarRepository Col_VarRepo
        {
            get { return IoC.Resolve<ICol_VarRepository>(); }
        }

        public static IDamageRepository DamageRepo
        {
            get { return IoC.Resolve<IDamageRepository>(); }
        }

        public static IFilRepository FilRepo
        {
            get { return IoC.Resolve<IFilRepository>(); }
        }
        public static IFruRepository FruRepo
        {
            get { return IoC.Resolve<IFruRepository>(); }
        }
        public static IFru_CantRepository Fru_CantRepo
        {
            get { return IoC.Resolve<IFru_CantRepository>(); }
        }
        public static ILingueRepository LingueRepo
        {
            get { return IoC.Resolve<ILingueRepository>(); }
        }
        public static IRepository<Menu> MenuRepo
        {
            get { return IoC.Resolve<IRepository<Menu>>(); }
        }
        public static IMenu_TipoRepository Menu_TipoRepo
        {
            get { return IoC.Resolve<IMenu_TipoRepository>(); }
        }
        public static IRepository<MetaDescriptor> MetaDescriptorRepo
        {
            get { return IoC.Resolve<IRepository<MetaDescriptor>>(); }
        }
        public static ITimesheetRepository TimesheetRepo
        {
            get { return IoC.Resolve<ITimesheetRepository>(); }
        }
        public static IRepository<MetaFieldDescriptor> MetaFieldDescriptorRepo
        {
            get { return IoC.Resolve<IRepository<MetaFieldDescriptor>>(); }
        }
        public static IUtenti_HistoryRepository Utenti_HistoryRepo
        {
            get { return IoC.Resolve<IUtenti_HistoryRepository>(); }
        }
        public static IParamRepository ParamRepo
        {
            get { return IoC.Resolve<IParamRepository>(); }
        }
        public static IRepository<PendingElab> PendingElabRepo
        {
            get { return IoC.Resolve<IRepository<PendingElab>>(); }
        }
        public static IPruRepository PruRepo
        {
            get { return IoC.Resolve<IPruRepository>(); }
        }
        public static IPru_ColRepository Pru_ColRepo
        {
            get { return IoC.Resolve<IPru_ColRepository>(); }
        }
        public static IRegRepository RegRepo
        {
            get { return IoC.Resolve<IRegRepository>(); }
        }
        public static IReg_StoredRepository RegStoredRepo
        {
            get { return IoC.Resolve<IReg_StoredRepository>(); }
        }
        public static IReg_VRepository Reg_VRepo
        {
            get { return IoC.Resolve<IReg_VRepository>(); }
        }
        public static IResourcesRepository ResourcesRepo
        {
            get { return IoC.Resolve<IResourcesRepository>(); }
        }
        public static IRespRepository RespRepo
        {
            get { return IoC.Resolve<IRespRepository>(); }
        }
        public static ITab_AutRepository Tab_AutRepo
        {
            get { return IoC.Resolve<ITab_AutRepository>(); }
        }
        public static ITab_Chk_ImpRepository Tab_Chk_ImpRepo
        {
            get { return IoC.Resolve<ITab_Chk_ImpRepository>(); }
        }
        public static ITab_ComuniRepository Tab_ComuniRepo
        {
            get { return IoC.Resolve<ITab_ComuniRepository>(); }
        }

        public static ITab_DamageRepository Tab_DamageRepo
        {
            get { return IoC.Resolve<ITab_DamageRepository>(); }
        }

        public static ITab_DataGridRepository Tab_DataGridRepo
        {
            get { return IoC.Resolve<ITab_DataGridRepository>(); }
        }
        public static ITab_DecodRepository Tab_DecodRepo
        {
            get { return IoC.Resolve<ITab_DecodRepository>(); }
        }
        public static ITab_DistRepository Tab_DistRepo
        {
            get { return IoC.Resolve<ITab_DistRepository>(); }
        }
        public static ITab_EditFormTemplateRepository Tab_EditFormTemplateRepo
        {
            get { return IoC.Resolve<ITab_EditFormTemplateRepository>(); }
        }
        public static ITab_Excel_ModelRepository Tab_Excel_ModelRepo
        {
            get { return IoC.Resolve<Tab_Excel_ModelRepository>(); }
        }
        public static ITab_FestiviRepository Tab_FestiviRepo
        {
            get { return IoC.Resolve<ITab_FestiviRepository>(); }
        }
        public static ITab_FunzRepository Tab_FunzRepo
        {
            get { return IoC.Resolve<ITab_FunzRepository>(); }
        }
        public static ITab_GridLookupRepository Tab_GridLookupRepo
        {
            get { return IoC.Resolve<ITab_GridLookupRepository>(); }
        }
        public static ITab_MessaggiRepository Tab_MessaggiRepo
        {
            get { return IoC.Resolve<ITab_MessaggiRepository>(); }
        }
        public static ITab_OrariRepository Tab_OrariRepo
        {
            get { return IoC.Resolve<ITab_OrariRepository>(); }
        }
        public static ITab_OrariTipoRepository Tab_OrariTipoRepo
        {
            get { return IoC.Resolve<ITab_OrariTipoRepository>(); }
        }
        public static ITab_ProvRepository Tab_ProvRepo
        {
            get { return IoC.Resolve<ITab_ProvRepository>(); }
        }
        public static IRepository<Tab_Report> Tab_ReportRepo
        {
            get { return IoC.Resolve<IRepository<Tab_Report>>(); }
        }
        public static IRepository<Tab_Report_Group> Tab_Report_GroupRepo
        {
            get { return IoC.Resolve<IRepository<Tab_Report_Group>>(); }
        }
        public static IUtentiRepository UtentiRepo
        {
            get { return IoC.Resolve<IUtentiRepository>(); }
        }
        public static IUtenti_FilRepository Utenti_FilRepo
        {
            get { return IoC.Resolve<IUtenti_FilRepository>(); }
        }
        public static IUtenti_RespRepository Utenti_RespRepo
        {
            get { return IoC.Resolve<IUtenti_RespRepository>(); }
        }
        public static IVersioniRepository VersioniRepo
        {
            get { return IoC.Resolve<IVersioniRepository>(); }
        }
        public static IColCantOrarioRepository ColCantOrarioRepo
        {
            get { return IoC.Resolve<IColCantOrarioRepository>(); }
        }
        public static IAut_StrRepository Aut_StrRepo
        {
            get { return IoC.Resolve<IAut_StrRepository>(); }
        }

        public static ICentroDiCostoRepository CentroDiCostoRepo => IoC.Resolve<ICentroDiCostoRepository>();

        public static int SaveChanges()
        {
            return IoC.Resolve<PowerWebEntities>().SaveChanges();
        }
    }
}
