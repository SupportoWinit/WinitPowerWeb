using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using log4net;
using Domain;
using Business.Repository;
using DevExpress.Web.ASPxUploadControl;
using System.IO;
using System.Text;
using Common;
using System.Drawing;
using System.ComponentModel;
using Business;
using DevExpress.Web.ASPxGridView;
using DevExpress.Web.ASPxTreeList;
using DevExpress.Web.ASPxEditors;
using System.Threading;
using Common.Properties;

namespace PowerWeb.Modules
{
    public partial class MenuModule : BaseGridModule
    {
        const String TREE_KEYFIELD_NAME = "Id";
        const String TREE_PARENT_KEYFIELD_NAME = "ParentId";

        private List<MenuTreeListItem> GetMenuTreeItems(int? mnuTypeId = null)
        //Carica la struttura del Tipo menù ricevuto (Tipo 1 se Non ricevuto nessun Tipo Menù)
        {
            if (mnuTypeId == null)
                //Se non ha ricevuto nessun Tipo Menù prende per defauult il Menù 1
                mnuTypeId = 1;

            List<MenuTreeListItem> treeItems = PowerWebContext.GetFromSession<List<MenuTreeListItem>>("MenuTreeData_" + tlMenu.ID);

            if (treeItems == null)
            {
                treeItems = new List<MenuTreeListItem>();

                //Legge le Voci del Menù del Tipo Menù ricevuto
                var menus = RepoManager.MenuRepo.Find(mnu => mnu.Menu_Tipo_Id == mnuTypeId, mnu => mnu.Tab_Funz).OrderBy(mn => mn.Parent_Id).ThenBy(mn => mn.Ordering).ToList();

                var filteredFunzs = new List<Tab_Funz>();

                if (cbIncludeFunz.Checked)
                //nel caso in cui sia ON il Tasto di Visualizzazione delle Funzioni NON assegnate a quel Tipo Menù
                //Le mostra PRIMA della ROOT (Numerandole con indici NEGATIVI <= -2)
                //Altrimenti ne salta la relativa Visualizzazione
                {
                    int counter = -2;

                    foreach (var item in RepoManager.Tab_FunzRepo.GetAll(true))
                    {
                        if (!menus.Exists(mnu => mnu.Tab_Funz_Id == item.Tab_Funz_Id))
                        {
                            treeItems.Add(new MenuTreeListItem
                            {
                                Id = counter,
                                ParentId = Int32.MinValue,
                                Function = item,
                            });

                            counter--;
                        }
                    }
                }

                treeItems.Add(new MenuTreeListItem
                //Aggiunge in Automatico il Record della ROOT (Id=-1)
                //Dopo le eventuali Funzioni NON assegnate a quel Tipo Menù (che hanno ID<=-2)
                {
                    Id = -1,
                    ParentId = Int32.MinValue,
                    Caption = "Root",
                });


                foreach (var item in menus)
                //Carica i Record della Tabella Menù per quel Tipo_Menu secondo nell'Ordine previsto            
                {
                    treeItems.Add(new MenuTreeListItem
                    {
                        Id = item.Menu_Id,
                        ParentId = item.Parent_Id.HasValue ? item.Parent_Id.Value : -1,
                        Caption = BusinessService.GetLocalizedString(item.Testo, ResourceTypeEnum.Menu),
                        Function = item.Tab_Funz,
                        Order = item.Ordering,
                    });
                }

                PowerWebContext.SetToSession<List<MenuTreeListItem>>("MenuTreeData_" + tlMenu.ID, treeItems);
            }

            return treeItems;
        }

        protected void tlMenu_HtmlRowPrepared(object sender, TreeListHtmlRowEventArgs e)
        //Imposta i Colori dello Sfondo delle Righe in base al Tipo di Riga per differenziare 
        //   Verde  --> La Riga di ROOT
        //   Rosa   --> Le Righe di Gruppo/Sottogruppo
        //   Bianco --> Le Righe delle Funzioni
        {
            int nodeIndex = Convert.ToInt32(e.NodeKey);

            int? mnuTypeId = null;

            if (cbxMenuType.SelectedItem != null)
                mnuTypeId = Convert.ToInt32(cbxMenuType.SelectedItem.Value);

            MenuTreeListItem current = GetMenuTreeItems(mnuTypeId).SingleOrDefault(mnu => mnu.Id == nodeIndex);

            if (current != null)
            {
                if (current.Function == null)
                {
                    e.Row.BackColor = Color.FromArgb(235, 211, 183);
                    e.Row.Font.Bold = true;
                }

                if (current.Id == -1)
                {
                    e.Row.BackColor = Color.FromArgb(211, 235, 183);
                    e.Row.Font.Bold = true;
                }
            }
        }

        protected void Page_Init(object sender, EventArgs e)
        //Inizializza la Pagina
        {
            cbIncludeFunz.Checked = false;
            BindMenuTypeCombo();
            BindTree(PowerWebContext.Current.User.Menu_Tipo_Id);

        }

        protected void Page_Load(object sender, EventArgs e)
        //Carica i Tipi Menu
        {
            if (PowerWebContext.Current.UserLevel.Funz_Aut < Settings.Default.Winit_Level)
                //Se l'Utente NON è un Utente Winit
                //Impedisce la Modifica del Flag Menù Standard da parte degli Utenti
                //Se dovesse duplicare quel Tipo Menù NON essendo Utente Winit gli imposterà poi nella ADD comunque il Flag  MenùStandard = False
                //perchè un Utente NON WINIT non può salvare Menù Standard
                cbMenùStandard.Enabled = false;
            BindMenuTypeCombo();
        }

        private void BindMenuTypeCombo(bool isToReloadSelected = false, int passedMenuId = -1)
        //Caricamento dei Tipi Menù esistenti nella tabella MENU_TIPO compatibili con l'Utente Collegato
        //SE ADMIN          --> TUTTI (con e senza Codice Utente)
        //SE USER NON ADMIN --> solo quelli dell'Utente stesso + quelli senza Codice Utente
        {
            //legge tutti i Tipi Menù in Tabella Menu_Tipo
            var menutypes = RepoManager.Menu_TipoRepo.GetAll(true).ToList();
            if (PowerWebContext.Current.UserLevel.Funz_Aut < Settings.Default.Admin_Level)
                //se NON è un ADMIN toglie quelli degli Altri Utenti
                menutypes = menutypes.Where(mn => mn.Utenti_Id == null || mn.Utenti_Id == PowerWebContext.Current.User.Utenti_Id).ToList();

            var selectedMenuTypeId = passedMenuId == -1 ? PowerWebContext.Current.User.Menu_Tipo_Id : passedMenuId;
            cbxMenuType.Items.Clear();

            int selectedIndex = 0;

            for (int i = 0; i < menutypes.Count; i++)
            {
                var item = menutypes[i];

                cbxMenuType.Items.Add(new ListEditItem(BusinessService.GetLocalizedString(item.Nome_Risorsa_Menu_Tipo), item.Menu_Tipo_Id));
                if (item.Menu_Tipo_Id == selectedMenuTypeId)
                {
                    selectedIndex = i;
                    cbMenùStandard.Value = item.Tipo_Standard;
                }
            }

            cbxMenuType.DataBindItems();

            if (!Page.IsPostBack || isToReloadSelected)
            {
                if (cbxMenuType.Items.Count > 0)
                {
                    cbxMenuType.SelectedIndex = selectedIndex;
                }
            }
        }

        private void BindTree(int? mnuTypeId = null)
        //Carica a Video la Struttura ad Albero del Tipo_menù richiesto
        {
            tlMenu.KeyFieldName = TREE_KEYFIELD_NAME;
            tlMenu.DataSource = GetMenuTreeItems(mnuTypeId);
            tlMenu.DataBind();
            tlMenu.ExpandToLevel(1);
        }

        private Dictionary<string, string> Menu_TipoValidating(Menu_Tipo entity, Boolean isNew = false)
        //Chiama la Check del Tipo Menù x validare il Tipo Menù che si sta Inserendo
        {
            return RepoManager.Menu_TipoRepo.Check(entity, isNew);

        }

        private bool checkUserAutorized(int menu_Id)
        //Restituisce True solo se l'Utente è abilitato a toccare quel Tipo menù
        {
            Menu_Tipo entity = RepoManager.Menu_TipoRepo.Single(mn => mn.Menu_Tipo_Id == menu_Id);
            //Gli Utenti WINIT (Liv >=12) possono modificare TUTTI I TIPI
            //Gli Utenti ADMIN (liv >=10) possono modificare TITTI i TIPO Salvo quelli con Tipo_Standard = True
            //Gli Utenti USER  (liv<10) possono modificare SOLO i Tipi con USER-ID uguale al loro
            return (PowerWebContext.Current.UserLevel.Funz_Aut >= Settings.Default.Winit_Level) ||
                   (PowerWebContext.Current.UserLevel.Funz_Aut >= Settings.Default.Admin_Level && entity.Tipo_Standard == false) ||
                   (PowerWebContext.Current.UserLevel.Funz_Aut < Settings.Default.Admin_Level && entity.Utenti_Id == PowerWebContext.Current.User.Utenti_Id);
        }

        protected void tlMenu_NodeInserting(object sender, DevExpress.Web.Data.ASPxDataInsertingEventArgs e)
        //Gestisce l'Inserimento di un Nodo
        {

            MenuTreeListItem stubMenuTreeListItem = null;

            int mnuTypeId = Convert.ToInt32(cbxMenuType.SelectedItem.Value);

            int parentId = Convert.ToInt32(e.NewValues[TREE_PARENT_KEYFIELD_NAME]);

            if (checkUserAutorized(mnuTypeId))
            {
                Domain.Menu mnu = new Domain.Menu
                {
                    Menu_Tipo_Id = mnuTypeId,
                    Parent_Id = parentId == -1 ? (int?)null : parentId,
                    Testo = Convert.ToString(e.NewValues[CommonService.GetPropertyName(() => stubMenuTreeListItem.Caption)]),
                };

                RepoManager.MenuRepo.Add(mnu, true);

                PowerWebContext.SetToSession<List<MenuTreeListItem>>("MenuTreeData_" + tlMenu.ID, null);
                BindTree(mnuTypeId);
            }

            e.Cancel = true;
            tlMenu.CancelEdit();
        }

        protected void tlMenu_NodeUpdating(object sender, DevExpress.Web.Data.ASPxDataUpdatingEventArgs e)
        //Gestisce l'Aggiornamento di un Nodo
        {
            MenuTreeListItem stubMenuTreeListItem = null;

            int mnuTypeId = Convert.ToInt32(cbxMenuType.SelectedItem.Value);

            int currentItemId = (int)e.Keys[TREE_KEYFIELD_NAME];
            if (checkUserAutorized(mnuTypeId))
            // Verifica se l'Utente può Modifica questo Tipo di Menù
            {
                Domain.Menu currentItem = RepoManager.MenuRepo.SingleOrDefault(pit => pit.Menu_Id == currentItemId);
                if (currentItem != null)
                {
                    //Posso Cambiare SOLO il Testo della Caption
                    currentItem.Testo = Convert.ToString(e.NewValues[CommonService.GetPropertyName(() => stubMenuTreeListItem.Caption)]);
                    currentItem.Parent_Id = e.NewValues[CommonService.GetPropertyName(() => stubMenuTreeListItem.ParentId)] as Int32?;
                }

                RepoManager.MenuRepo.SaveChanges();

                PowerWebContext.SetToSession<List<MenuTreeListItem>>("MenuTreeData_" + tlMenu.ID, null);
                BindTree(mnuTypeId);
            }

            e.Cancel = true;
            tlMenu.CancelEdit();
        }

        protected void tlMenu_NodeDeleting(object sender, DevExpress.Web.Data.ASPxDataDeletingEventArgs e)
        //Gestisce la Cancellazione di un Nodo
        {
            int mnuTypeId = Convert.ToInt32(cbxMenuType.SelectedItem.Value);

            int currentItemId = (int)e.Keys[TREE_KEYFIELD_NAME];
            if (checkUserAutorized(mnuTypeId))
            // Verifica se l'Utente può Modifica questo Tipo di Menù
            {
                Domain.Menu currentItem = RepoManager.MenuRepo.SingleOrDefault(pit => pit.Menu_Id == currentItemId);
                if (currentItem != null)
                {
                    List<Domain.Menu> toBeDeletedMenus = new List<Domain.Menu>();

                    var children = RepoManager.MenuRepo.Find(mnu => mnu.Parent_Id == currentItem.Parent_Id).ToList();
                    foreach (var child in children)
                        RecursiveDeleteMenus(currentItem, toBeDeletedMenus);

                    var root = RepoManager.MenuRepo.Find(mnu => mnu.Menu_Id == currentItem.Parent_Id).FirstOrDefault();
                    if (root != null)
                        toBeDeletedMenus.Add(root);

                    RepoManager.MenuRepo.Delete(toBeDeletedMenus, true);
                }

                PowerWebContext.SetToSession<List<MenuTreeListItem>>("MenuTreeData_" + tlMenu.ID, null);
                BindTree(mnuTypeId);
            }

            e.Cancel = true;
            tlMenu.CancelEdit();
        }

        private void RecursiveDeleteMenus(Domain.Menu currentItem, List<Domain.Menu> menus)
        //Gestisce la Cancellazione di Tutti i Record di quel Tipo Menù
        {
            if (currentItem != null)
            {
                menus.Add(currentItem);

                var children = RepoManager.MenuRepo.Find(mnu => mnu.Parent_Id == currentItem.Menu_Id);
                foreach (var child in children)
                    RecursiveDeleteMenus(child, menus);
            }
        }

        private void RecursivePopMenus(TreeListNode node, List<Domain.Menu> menus)
        //Gestisce la Lettura di Tutti i Record di quel Tipo Menù
        {
            MenuTreeListItem currentItem = node.DataItem as MenuTreeListItem;

            Domain.Menu currentMenu = null;

            if (currentItem != null)
            {
                currentMenu = RepoManager.MenuRepo.SingleOrDefault(mnu => mnu.Menu_Id == currentItem.Id);
            }

            if (currentMenu != null)
                menus.Add(currentMenu);

            if (node.HasChildren)
            {
                foreach (TreeListNode childNode in node.ChildNodes)
                {
                    RecursivePopMenus(childNode, menus);
                }
            }
        }

        protected void tlMenu_ProcessDragNode(object sender, TreeListNodeDragEventArgs e)
        //Gestisce lo Spsotamento di un Nodo
        {
            MenuTreeListItem child = e.Node.DataItem as MenuTreeListItem;

            int mnuTypeId = Convert.ToInt32(cbxMenuType.SelectedItem.Value);

            if (child != null && checkUserAutorized(mnuTypeId))
            // Verifica se l'Utente può Modifica questo Tipo di Menù
            {
                if (String.IsNullOrEmpty(e.NewParentNode.Key))
                {
                    List<Domain.Menu> selectedMenus = new List<Domain.Menu>();
                    RecursivePopMenus(e.Node, selectedMenus);

                    RepoManager.MenuRepo.Delete(selectedMenus, true);

                    PowerWebContext.SetToSession<List<MenuTreeListItem>>("MenuTreeData_" + tlMenu.ID, null);
                }
                else
                {

                    MenuTreeListItem newParent = e.NewParentNode.DataItem as MenuTreeListItem;
                    if (newParent != null && newParent.Function == null)
                    {
                        var currentMnu = RepoManager.MenuRepo.SingleOrDefault(mnu => mnu.Menu_Id == child.Id);

                        if (currentMnu == null)
                        {
                            currentMnu = new Domain.Menu
                            {
                                Menu_Tipo_Id = mnuTypeId,
                                Parent_Id = newParent.Id == -1 ? (int?)null : newParent.Id,
                                Testo = child.Function.Nome_Tab_Funz,
                                Tab_Funz_Id = child.Function.Tab_Funz_Id,
                            };

                            RepoManager.MenuRepo.Add(currentMnu, true);
                        }
                        else
                        {
                            currentMnu.Parent_Id = newParent.Id == -1 ? (int?)null : newParent.Id;
                            RepoManager.MenuRepo.SaveChanges();
                        }

                        PowerWebContext.SetToSession<List<MenuTreeListItem>>("MenuTreeData_" + tlMenu.ID, null);
                    }
                }
            }

            e.Cancel = true;
            e.Handled = true;

            BindTree(mnuTypeId);
        }

        protected void cpTree_Callback(object sender, DevExpress.Web.ASPxClasses.CallbackEventArgsBase e)
        //gestisce le Operazioni richeiste dall'Utente a Video sui Tipi Menù
        {
            var splitter = e.Parameter.Split(new char[] { '|' });

            if (splitter.Count() > 1)
            {

                if (splitter[0] == "index")
                {
                    var selectedIndex = Convert.ToInt32(splitter[1]);

                    int mnuTypeId = Convert.ToInt32(cbxMenuType.Items[selectedIndex].Value);

                    PowerWebContext.SetToSession<List<MenuTreeListItem>>("MenuTreeData_" + tlMenu.ID, null);

                    BindTree(mnuTypeId);

                    BindMenuTypeCombo(true, mnuTypeId);
                }
                else if (splitter[0] == "hide")
                {
                    int mnuTypeId = Convert.ToInt32(cbxMenuType.SelectedItem.Value);

                    PowerWebContext.SetToSession<List<MenuTreeListItem>>("MenuTreeData_" + tlMenu.ID, null);

                    BindTree(mnuTypeId);
                }
                else if (splitter[0] == "delete")
                //Gestisce la Cancellazione di un Tipo Menù e dei relativi Record di quel Tipo Menù
                {
                    var selectedIndex = Convert.ToInt32(splitter[1]);

                    int mnuTypeId = Convert.ToInt32(cbxMenuType.Items[selectedIndex].Value);

                    Menu_Tipo defaultMenu = RepoManager.Menu_TipoRepo.First();

                    var currentUser = RepoManager.UtentiRepo.Single(ut => ut.Utenti_Id == PowerWebContext.Current.User.Utenti_Id);
                    currentUser.Menu_Tipo_Id = defaultMenu.Menu_Tipo_Id;

                    var toDeleteMenus = RepoManager.MenuRepo.Find(mn => mn.Menu_Tipo_Id == mnuTypeId).ToList();
                    RepoManager.MenuRepo.Delete(toDeleteMenus, true);

                    Menu_Tipo oldMenu = RepoManager.Menu_TipoRepo.SingleOrDefault(mnu => mnu.Menu_Tipo_Id == mnuTypeId);
                    RepoManager.Menu_TipoRepo.Delete(oldMenu, true);

                    BindMenuTypeCombo(true);
                    BindTree(mnuTypeId);
                }
            }

            if (e.Parameter == "add")
            //Gestisce la Creazione di un Nuovo Tipo Menù e dei relativi Record di quel Menù
            {
                int? utentiId = PowerWebContext.Current.User.Utenti_Id;

                if (PowerWebContext.Current.UserLevel.Funz_Aut >= Common.Properties.Settings.Default.Admin_Level)
                    //Se l'Utente è WINIT e/o Admin il Nuovo Menù va creato con il Codice Utente = Null 
                    utentiId = null;

                Menu_Tipo newMenu = new Menu_Tipo
                {
                    //Se è Enabled significa che l'Utente è WINIT e quindi associala Nuovo Menù il valore del Flag MenùStandard impostato a Video
                    //SE NON è enabled signifca che NON è un Utente Winit e quindi il Nuovo Menù ha sempre e solo il Falg MenuStandard impostato a NULL
                    Tipo_Standard = cbMenùStandard.Checked && cbMenùStandard.Enabled,
                    Nome_Risorsa_Menu_Tipo = cbxMenuType.Text,
                    Utenti_Id = utentiId,

                };

                if (Menu_TipoValidating(newMenu, true).Count == 0)
                {
                    RepoManager.Menu_TipoRepo.Add(newMenu, true);

                    List<MenuTreeListItem> treeItems = PowerWebContext.GetFromSession<List<MenuTreeListItem>>("MenuTreeData_" + tlMenu.ID);
                    var firstTli = treeItems.Where(tli => tli.Id > 0).FirstOrDefault();
                    if (firstTli != null)
                    {

                        var oldMenuId = RepoManager.MenuRepo.Single(mn => mn.Menu_Id == firstTli.Id).Menu_Tipo_Id;

                        var currentMenuItems = RepoManager.MenuRepo.Find(mn => mn.Menu_Tipo_Id == oldMenuId).ToList();
                        var toAddMenuItems = new List<Domain.Menu>();

                        Dictionary<int, int?> idDic = new Dictionary<int, int?>();

                        var index = 0;

                        while (currentMenuItems.Count > 0)
                        {
                            if (index == (currentMenuItems.Count - 1))
                                index = 0;

                            var oldMenu = currentMenuItems.ElementAt(index);
                            if (oldMenu.Parent_Id == null || idDic.ContainsKey(oldMenu.Parent_Id.Value))
                            {
                                var newElement = new Domain.Menu
                                {
                                    Menu_Tipo_Id = newMenu.Menu_Tipo_Id,
                                    Parent_Id = oldMenu.Parent_Id == null ? null : idDic[oldMenu.Parent_Id.Value],
                                    Ordering = oldMenu.Ordering,
                                    Tab_Funz_Id = oldMenu.Tab_Funz_Id,
                                    Testo = oldMenu.Testo,
                                };

                                RepoManager.MenuRepo.Add(newElement, true);

                                idDic.Add(oldMenu.Menu_Id, newElement.Menu_Id);

                                currentMenuItems.RemoveAt(index);
                            }
                            else index++;
                        }

                        RepoManager.MenuRepo.Add(toAddMenuItems, true);
                    }

                }
                BindMenuTypeCombo(true, newMenu.Menu_Tipo_Id);
                BindTree(newMenu.Menu_Tipo_Id);
            }
        }

        protected void tlMenu_CustomCallback(object sender, TreeListCustomCallbackEventArgs e)
        {
            ASPxTreeList tl = sender as ASPxTreeList;

            int mnuTypeId = Convert.ToInt32(cbxMenuType.SelectedItem.Value);

            if (tl != null && checkUserAutorized(mnuTypeId))
            // Verifica se l'Utente può Modifica questo Tipo di Menù
            {
                if (e.Argument.StartsWith("reorder:"))
                {
                    string[] arg = e.Argument.Split(':');
                    var source = tl.FindNodeByKeyValue(arg[1]);
                    var dest = tl.FindNodeByKeyValue(arg[2]);
                    int sourceIndex = Convert.ToInt32(source.Key);
                    int destIndex = Convert.ToInt32(dest.Key);
                    var currentSourceMnu = RepoManager.MenuRepo.SingleOrDefault(mnu => mnu.Menu_Id == sourceIndex);
                    var currentDestMnu = RepoManager.MenuRepo.SingleOrDefault(mnu => mnu.Menu_Id == destIndex);

                    List<Domain.Menu> childrenList = null;
                    if (currentSourceMnu != null)
                    //Caso in cui viene spostata una Pagina da un Menù all'altro
                    {
                        if (currentSourceMnu.Parent == null)
                            childrenList = RepoManager.MenuRepo.Find(mn => mn.Parent_Id == null).OrderBy(mn => mn.Ordering).ToList();
                        else childrenList = currentSourceMnu.Parent.Children.OrderBy(mn => mn.Ordering).ToList();

                        childrenList.Remove(currentSourceMnu);

                        childrenList.Insert(currentDestMnu.Ordering, currentSourceMnu);

                        for (int i = 0; i < childrenList.Count; i++)
                            childrenList.ElementAt(i).Ordering = i;

                        RepoManager.MenuRepo.SaveChanges();

                        PowerWebContext.SetToSession<List<MenuTreeListItem>>("MenuTreeData_" + tlMenu.ID, null);

                        BindTree(mnuTypeId);
                    }
                }
                else
                {
                    var splitter = e.Argument.Split(new char[] { '|' });

                    var currentNodeKey = -1;
                    if (splitter.Count() > 1)
                    {
                        currentNodeKey = Convert.ToInt32(splitter[1]);

                        if (splitter[0] == "delete" && currentNodeKey != -1)
                        {
                            int currentItemId = currentNodeKey;

                            if (checkUserAutorized(mnuTypeId))
                            // Verifica se l'Utente può Modifica questo Tipo di Menù
                            {
                                Domain.Menu currentItem = RepoManager.MenuRepo.SingleOrDefault(pit => pit.Menu_Id == currentItemId);
                                if (currentItem != null)
                                {
                                    List<Domain.Menu> toBeDeletedMenus = new List<Domain.Menu>();

                                    toBeDeletedMenus.Add(currentItem);

                                    var children = RepoManager.MenuRepo.Find(mnu => mnu.Parent_Id == currentItem.Menu_Id).ToList();

                                    if (children != null && children.Count > 0)
                                    {
                                        foreach (var child in children)
                                            RecursiveDeleteMenus(child, toBeDeletedMenus);
                                    }

                                    RepoManager.MenuRepo.Delete(toBeDeletedMenus, true);
                                }

                                PowerWebContext.SetToSession<List<MenuTreeListItem>>("MenuTreeData_" + tlMenu.ID, null);
                                BindTree(mnuTypeId);
                            }
                        }
                    }
                }
            }
        }

        protected void tlMenu_CommandColumnButtonInitialize(object sender, TreeListCommandColumnButtonEventArgs e)
        //Gestisce l'eventuale DISABILITAZIONE dei Singoli Bottoni in base al Recrod ricveuto               
        {
            //Viene chiamata per ogni Record da Visualizzare e per ognuno dei 3 previsti Bottoni (Ins/Del/Mod)
            //Per il Record di Root vengono disabilitati tutti e 3 i Bottoni
            //Per tutti gli altri tipi di Record va sempre lasciato Abilitato il Tasto di Delete
            //Per le Funzioni va disabilitato il Tasto di ADD
            
            int nodeIndex = Convert.ToInt32(e.NodeKey);
            int? mnuTypeId = null;

            if (cbxMenuType.SelectedItem != null)
                mnuTypeId = Convert.ToInt32(cbxMenuType.SelectedItem.Value);

            if (mnuTypeId == null)
                return;

            MenuTreeListItem current = GetMenuTreeItems(mnuTypeId).SingleOrDefault(mnu => mnu.Id == nodeIndex);

            if(current != null && current.Id <= -1)
            //nel caso della Root e delle Funzioni non attivate per quel Tipo Menù
            //vanno disabilitati Tutti I Tasti
            {
                e.Visible =  DevExpress.Utils.DefaultBoolean.False;
                return;
            }

            if (e.ButtonType == TreeListCommandColumnButtonType.Custom)                
                //Nel caso della Delete (Custom) NON va MAI Disabilitato
                return;

            if (current != null && current.Function != null && current.Id !=-1)
                //Se è una Funzione e NON è una ROOT Disabilito TUTTI I restanti Tasti
                //che non siano DEL (trattato prima)
                    e.Visible = DevExpress.Utils.DefaultBoolean.False;                                               
        }
    }

    public class MenuTreeListItem
    {
        private String _caption = String.Empty;

        public int Id { get; set; }
        public int ParentId { get; set; }
        public String Caption
        {
            get
            {
                return Function == null ? _caption : BusinessService.GetLocalizedString(Function.Nome_Tab_Funz);
            }
            set
            {
                _caption = value;
            }
        }

        public Tab_Funz Function { get; set; }

        public int? Order { get; set; }
    }
}