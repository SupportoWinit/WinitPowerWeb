using Business.BusinessClasses.ViewServiceDTOs;
using Business.Repository;
using System.Collections.Generic;

namespace Business.BusinessServices.ViewService
{
    public class ViewService
    {

        public IEnumerable<ViewDTO> LoadViews(string page)
        {
            
            return RepoManager.Tab_DataGridRepo.LoadViewsByPage(page);
        }

        public void SaveView(ViewDTO view)
        {

            RepoManager.Tab_DataGridRepo.InsertView(view);

        }
        
        public void DeleteView(int key)
        {
            RepoManager.Tab_DataGridRepo.DeleteView(key);
        }

    }
}
