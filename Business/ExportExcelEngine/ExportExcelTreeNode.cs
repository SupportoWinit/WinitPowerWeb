using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Business.MDBSchema;

namespace Business.ExportExcelEngine
{
    public class ExportExcelTreeNode
    {



        private int GetNodeLevel(ExportExcelTreeNode current)
        {
            if (current.Parent == null)
                return 0;
            else
            {
                return 1 + GetNodeLevel(current.Parent);
            }
        }
        public int Level
        {
            get
            {

                return GetNodeLevel(this);

            }
        }

        private int _startRow = 5;
        public int StartRow
        {
            get
            {
                return _startRow;

            }
            set { _startRow = value; }
        }

        public Object Item { get; private set; }

        public ExportExcelTreeNode Parent { get; set; }

        public List<ExportExcelTreeNode> Children { get; private set; }

        public ExportExcelTreeNode()
            : this(null, null, new List<ExportExcelTreeNode>())
        {
        }

        public ExportExcelTreeNode(Object item)
            : this(item, null, new List<ExportExcelTreeNode>())
        {
        }

        public ExportExcelTreeNode(Object item, ExportExcelTreeNode parent)
            : this(item, parent, new List<ExportExcelTreeNode>())
        {
        }

        public ExportExcelTreeNode(Object item, ExportExcelTreeNode parent, List<ExportExcelTreeNode> children)
        {
            Item = item;
            Parent = parent;
            Children = children;
            StartRow = -1;
        }




        public void AddChild(ExportExcelTreeNode item)
        {
            item.Parent = this;
            Children.Add(item);
        }

        public void AddChildren(List<ExportExcelTreeNode> items)
        {
            foreach (ExportExcelTreeNode item in items)
            {
                item.Parent = this;
            }
            Children.AddRange(items);
        }

        /// <summary>
        /// Metodo che aggiunge i figli di tipo T al root
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items">The items.</param>
        public void AddChildren<T>(List<T> items)
        {
            for (int i = 0; i < items.Count; i++)
            {
                T item = items[i];

                var itemType = item.GetType();

                //se l'oggetto in questione è una Reg_V
               if(itemType == typeof(Domain.Reg_V))
                {
                    //conversione di un tipo generico T in una reg_v
                    object itemObj = (object)item;
                    Domain.Reg_V regvItem = (Domain.Reg_V)itemObj;

                    //se il codice cliente non è null vengono tolti gli spazi
                    if (regvItem.Codice_Cliente != null)
                        regvItem.Codice_Cliente = regvItem.Codice_Cliente.Trim();

                    //conversione da una reg_v ad un tipo generico T
                    itemObj = regvItem;
                    item =(T)itemObj;
                }
               
               //viene istanziato un nuovo nodo
                var node = new ExportExcelTreeNode(item, this);

                //si controlla che l'indice di riga non sia 0 oppure la prima riga sia diversa da -1
                if (i == 0 && this.StartRow != -1)
                    //si vanno a creare tutte le righe come nodi successive
                    node.StartRow = this.StartRow + 1;

                //viene aggiunt alla lista di List<ExportExcelTreeNode> i vari nodi figli
                Children.Add(node);
            }
        }

    }
}
