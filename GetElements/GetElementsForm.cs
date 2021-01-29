using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Excel = Microsoft.Office.Interop.Excel;

namespace GetElements
{
    /// <summary>
    /// La classe della nostra finestra di dialogo non modale.
    /// </summary>
    /// <remarks>
    /// Oltre ad altri metodi, ha un metodo per ogni pulsante di comando. 
    /// In ognuno di questi metodi non viene fatto nient'altro che il sollevamento
    /// di un evento esterno con una richiesta specifica impostata nel gestore delle richieste.
    /// </remarks>
    /// 
    public partial class GetElementsForm : System.Windows.Forms.Form
    {
        // In questo esempio, la finestra di dialogo possiede il gestore e gli oggetti evento, 
        // ma non è un requisito. Potrebbero anche essere proprietà statiche dell'applicazione.

        private RequestHandler m_Handler;
        private ExternalEvent m_ExEvent;


        /// <summary>
        ///   Costruttore della finestra di dialogo
        /// </summary>
        /// 
        public GetElementsForm(ExternalEvent exEvent, RequestHandler handler)
        {
            InitializeComponent();
            m_Handler = handler;
            m_ExEvent = exEvent;
        }

        /// <summary>
        /// Modulo gestore eventi chiuso
        /// </summary>
        /// <param name="e"></param>
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // possediamo sia l'evento che il gestore
            // dovremmo eliminarlo prima di chiudere la finestra

            m_ExEvent.Dispose();
            m_ExEvent = null;
            m_Handler = null;

            // non dimenticare di chiamare la classe base
            base.OnFormClosed(e);
        }

        /// <summary>
        ///   Attivatore / disattivatore del controllo
        /// </summary>
        ///
        private void EnableCommands(bool status)
        {
            foreach (System.Windows.Forms.Control ctrl in this.Controls)
            {
                ctrl.Enabled = status;
            }
            if (!status)
            {
                this.exitButton.Enabled = true;
            }
        }

        /// <summary>
        ///   Un metodo di supporto privato per effettuare una richiesta 
        ///   e allo stesso tempo mettere la finestra di dialogo in sospensione.
        /// </summary>
        /// <remarks>
        ///   Ci si aspetta che il processo che esegue la richiesta 
        ///   (l'helper Idling in questo caso particolare) 
        ///   riattivi anche la finestra di dialogo dopo aver terminato l'esecuzione.
        /// </remarks>
        ///
        private void MakeRequest(RequestId request)
        {
            App.thisApp.DontShowFormTop();
            m_Handler.Request.Make(request);
            m_ExEvent.Raise();
            DozeOff();
        }


        /// <summary>
        ///   DozeOff -> disabilita tutti i controlli (tranne il pulsante Esci)
        /// </summary>
        /// 
        private void DozeOff()
        {
            EnableCommands(false);
        }

        /// <summary>
        ///   WakeUp -> abilita tutti i controlli
        /// </summary>
        /// 
        public void WakeUp()
        {
            EnableCommands(true);
        }

        /// <summary>
        ///   Metodo di interazione con la finestra di dialogo
        /// </summary>
        /// 
        private void ModelessForm_Load(object sender, EventArgs e)
        {
        }

        /// <summary>
        ///   Metodo di riempimento del DataGrid
        /// </summary>
        /// 
        private void button1_Click(object sender, EventArgs e)
        {
            MakeRequest(RequestId.Default);
        }

        /// <summary>
        ///   Metodo di restituisce i valori del DataGrid
        /// </summary>
        /// 
        public void FillDataGrid()
        {
            // Riempie il DataGridView
            List<string[]> lista = m_Handler.ElementList;
            var stringslist = lista
                .Select(arr => new {
                    Number = arr[0],
                    Id = arr[1],
                    Instance = arr[2],
                    Category = arr[3],
                    Type = arr[4],
                    Family = arr[5],
                    UnitTypeIdentifier = arr[6],
                    PanelTypeIdentifier = arr[7]
                }).ToArray();

            // Crea un DataTable (utile per fare poi l'ordinamento per colonne)
            DataTable dataTable = new DataTable();
            dataTable.Columns.Add(new DataColumn
            {
                ColumnName = "Number",
                DataType = typeof(String)
            });
            dataTable.Columns.Add(new DataColumn
            {
                ColumnName = "Id",
                DataType = typeof(String)
            });
            dataTable.Columns.Add(new DataColumn
            {
                ColumnName = "Instance",
                DataType = typeof(String)
            });
            dataTable.Columns.Add(new DataColumn
            {
                ColumnName = "Category",
                DataType = typeof(String)
            });
            dataTable.Columns.Add(new DataColumn
            {
                ColumnName = "Type",
                DataType = typeof(String)
            });
            dataTable.Columns.Add(new DataColumn
            {
                ColumnName = "Family",
                DataType = typeof(String)
            });
            dataTable.Columns.Add(new DataColumn
            {
                ColumnName = "UnitTypeIdentifier",
                DataType = typeof(String)
            });
            dataTable.Columns.Add(new DataColumn
            {
                ColumnName = "PanelTypeIdentifier",
                DataType = typeof(String)
            });

            List<DataRow> list = new List<DataRow>();
            foreach (var x in stringslist)
            {
                var row = dataTable.NewRow();
                row.SetField<string>("Number", x.Number);
                row.SetField<string>("Id", x.Id);
                row.SetField<string>("Instance", x.Instance);
                row.SetField<string>("Category", x.Category);
                row.SetField<string>("Type", x.Type);
                row.SetField<string>("Family", x.Family);
                row.SetField<string>("UnitTypeIdentifier", x.UnitTypeIdentifier);
                row.SetField<string>("PanelTypeIdentifier", x.PanelTypeIdentifier);
                list.Add(row);
            }
            dataTable = list.CopyToDataTable();

            // Riempie il DataGRidView
            dataGridView1.DataSource = dataTable;

            elementTextBox.Text = Convert.ToString(m_Handler.Count[0]);
            instanceTextBox.Text = Convert.ToString(m_Handler.Count[1]);
            categorieTextBox.Text = Convert.ToString(m_Handler.Count[2]);
            typeTextBox.Text = Convert.ToString(m_Handler.Count[3]);
            famiglieTextBox.Text = Convert.ToString(m_Handler.Count[4]);
            uiTextBox.Text = Convert.ToString(m_Handler.Count[5]);
            ptiTextBox.Text = Convert.ToString(m_Handler.Count[6]);
        }



        /// <summary>
        ///   Metodo di ordinamento delle colonne
        /// </summary>
        /// 
        private void dataGridView1_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            DataGridViewColumn newColumn = dataGridView1.Columns[e.ColumnIndex];
            DataGridViewColumn oldColumn = dataGridView1.SortedColumn;
            ListSortDirection direction;

            // If oldColumn is null, then the DataGridView is not sorted.
            if (oldColumn != null)
            {
                // Sort the same column again, reversing the SortOrder.
                if (oldColumn == newColumn &&
                    dataGridView1.SortOrder == SortOrder.Ascending)
                {
                    direction = ListSortDirection.Descending;
                }
                else
                {
                    // Sort a new column and remove the old SortGlyph.
                    direction = ListSortDirection.Ascending;
                    oldColumn.HeaderCell.SortGlyphDirection = SortOrder.None;
                }
            }
            else
            {
                direction = ListSortDirection.Ascending;
            }

            // Sort the selected column.
            dataGridView1.Sort(newColumn, direction);
            newColumn.HeaderCell.SortGlyphDirection =
                direction == ListSortDirection.Ascending ?
                SortOrder.Ascending : SortOrder.Descending;
        }

        /// <summary>
        ///   Metodo di cambio del metodo di ordinamento del DataGrid
        /// </summary>
        /// 
        private void dataGridView1_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            // Put each of the columns into programmatic sort mode.
            foreach (DataGridViewColumn column in dataGridView1.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.Programmatic;
            }
        }

        /// <summary>
        ///   Metodo di visualizzazione di un elemento
        /// </summary>
        /// 
        private void button2_Click(object sender, EventArgs e)
        {
            MakeRequest(RequestId.Id);
        }

        /// <summary>
        ///   Metodo che restituisce l'Id dell'elemento scelto
        /// </summary>
        /// 
        public ElementId GetElemId()
        {
            int id = Convert.ToInt32(textBox1.Text);
            ElementId eleID = new ElementId(id);
            return eleID;
        }

        /// <summary>
        ///   Metodo che riempie la ListBoxz del singolo elemento
        /// </summary>
        /// 
        public void SetListBox()
        {
            listBox1.DataSource = m_Handler.SingleElement;
        }

        /// <summary>
        ///   Exit - chiude la finestra di dialogo
        /// </summary>
        /// 
        private void exitButton_Click_1(object sender, EventArgs e)
        {
            Close();
        }



    }  // class
}
