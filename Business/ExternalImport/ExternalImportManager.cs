using Business.ExternalImports;
using Business.Infrastructure;
using Business.Repository;

namespace Business.ExternalImport
{
    public static class ExternalImportManager
    {


        /// <summary>
        /// Richiede timbrature dai server remoti registrati
        /// </summary>
        public static void GetFromRemoteSource()
        {
            if (!RepoManager.ParamRepo.ParametersRow.Abilita_Import_Esterno)
                return;

            var regFactory = IoC.Resolve<ExternalImportFactory>(); //Ioc gestisce il ciclo di vita dell'oggetto(in questo caso la session)

            regFactory.Import();
        }
    }
}
