using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Common;
using Business.Repository;
using Business;
using Domain;

namespace PowerWeb.Menu
{
    public class PowerWebSitemapProvider : StaticSiteMapProvider
    {
        private List<Domain.Menu> Menus { get; set; }
        
        /// <summary>
        /// Getter che restituisce il nodo radice della mappa del sito (nel nostro caso la home)
        /// </summary>
        public override SiteMapNode RootNode
        {
            get
            {
                var root = PowerWebContext.GetFromSession<SiteMapNode>("RootSiteMapNode") as SiteMapNode;

                if (root == null)
                {
                    root = InitSiteMap();
                    PowerWebContext.SetToSession<SiteMapNode>("RootSiteMapNode", root);
                }

                return root;
            }
        }
        
        /// <summary>
        /// Inizializza la mappa del sito e costruisce il menu di navigazione tramite la tabella menu
        /// </summary>
        /// <returns></returns>
        private SiteMapNode InitSiteMap()
        {
            lock (this)
            {
                SiteMapNode root = null;

                Clear();

                Menus = new List<Domain.Menu>();

                if (PowerWebContext.Current != null && PowerWebContext.Current.User != null)
                {
                    root = new SiteMapNode(this, "Key", "default.aspx", "Home");

                    AddNode(root);

                    var tmpMenus = RepoManager.MenuRepo.Find(mnu => mnu.Menu_Tipo_Id == PowerWebContext.Current.User.Menu_Tipo_Id).ToList();

                    tmpMenus.ForEach(mn =>
                    {
                        if (PowerWebContext.Current.TabFunzs.FirstOrDefault(ff => mn.Tab_Funz_Id == ff.Tab_Funz_Id) != null || mn.Tab_Funz_Id == null)
                            Menus.Add(mn);
                    });

                    List<Domain.Menu> rootMenus = Menus.Where(mn => mn.Parent_Id == null).OrderBy(mn => mn.Ordering).ToList();

                    foreach (Domain.Menu rootMenu in rootMenus)
                        PopulateMenu(rootMenu, root);
                }

                return root;
            }

        }

        public override SiteMapNode BuildSiteMap()
        {
            return RootNode;
        }
        
        /// <summary>
        /// Popolazione del menu in maniera ricorsiva (struttura ad albero)
        /// </summary>
        /// <param name="menu">The menu.</param>
        /// <param name="parent">The parent.</param>
        private void PopulateMenu(Domain.Menu menu, SiteMapNode parent)
        {
            if (menu == null)
                return;
            else
            {
                string menuURL = null;
                if (menu.Tab_Funz != null)
                    menuURL = menu.Tab_Funz.Link_Tab_Funz;

                SiteMapNode currentSMN = new SiteMapNode(this, menu.Menu_Id.ToString(), menuURL,  BusinessService.GetLocalizedString(menu.Testo));

                AddNode(currentSMN, parent);

                List<Domain.Menu> children = Menus.FindAll(mn => mn.Parent_Id == menu.Menu_Id).OrderBy(mnu => mnu.Ordering).ToList();

                foreach (Domain.Menu childMenu in children)
                    PopulateMenu(childMenu, currentSMN);
            }
        }

        protected override SiteMapNode GetRootNodeCore()
        {
            return RootNode;
        }
    }
}