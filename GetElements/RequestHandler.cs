//
// (C) Copyright 2003-2017 by Autodesk, Inc.
//
// Permission to use, copy, modify, and distribute this software in
// object code form for any purpose and without fee is hereby granted,
// provided that the above copyright notice appears in all copies and
// that both that copyright notice and the limited warranty and
// restricted rights notice below appear in all supporting
// documentation.
//
// AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS.
// AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE. AUTODESK, INC.
// DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
// UNINTERRUPTED OR ERROR FREE.
//
// Use, duplication, or disclosure by the U.S. Government is subject to
// restrictions set forth in FAR 52.227-19 (Commercial Computer
// Software - Restricted Rights) and DFAR 252.227-7013(c)(1)(ii)
// (Rights in Technical Data and Computer Software), as applicable.
//
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace GetElements
{
    /// <summary>
    ///   Una classe con metodi per eseguire le richieste effettuate dall'utente della finestra di dialogo.
    /// </summary>
    /// 
    public class RequestHandler : IExternalEventHandler  // Un'istanza di una classe che implementa questa interfaccia verrà registrata prima con Revit e ogni volta che viene generato l'evento esterno corrispondente, verrà richiamato il metodo Execute di questa interfaccia.
    {
        #region Private data members
        // Il valore dell'ultima richiesta effettuata dal modulo non modale
        private Request m_request = new Request();

        // Instanza della classe 
        internal static RequestHandler thisApp = null;

        // Dichiara questa classe
        private GetElementsForm getElementsForm;

        // ArrayList da tornare di default
        List<string[]> _elementList;

        // Il valore dell'Unit Identifier
        private string _unitIdentifier = "";

        // Il valore delPanel TypeIdentifier
        private string _panelTypeIdentifier = "";

        // Il valore del singolo elemento cercato
        private ArrayList _elementSingle;

        // Count per il calcolo delle quantita
        private List<int> _count;
        private int _countElement = 1;
        private int _countInstance = 1;
        private int _countCategory = 1;
        private int _countType = 1;
        private int _countFamily = 1;
        private int _countUI = 1;
        private int _countPTI = 1;

        #endregion

        #region Class public property
        /// <summary>
        /// Proprietà pubblica per accedere al valore della richiesta corrente
        /// </summary>
        public Request Request
        {
            get { return m_request; }
        }

        /// <summary>
        /// Proprietà pubblica per accedere al valore della richiesta corrente
        /// </summary>
        public List<string[]> ElementList
        {
            get { return _elementList; }
        }

        /// <summary>
        /// Proprietà pubblica per accedere al valore della richiesta corrente
        /// </summary>
        public ArrayList SingleElement
        {
            get { return _elementSingle; }
        }

        /// <summary>
        /// Proprietà pubblica per accedere al valore della richiesta corrente
        /// </summary>
        public List<int> Count
        {
            get { return _count; }
        }
        #endregion

        #region Class public method
        /// <summary>
        /// Costruttore di default di RequestHandler
        /// </summary>
        public RequestHandler()
        {
            thisApp = this;
            // Costruisce i membri dei dati per le proprietà
            _elementList = new List<string[]>();
            _elementSingle = new ArrayList();
            _count = new List<int>();
        }
        #endregion

        /// <summary>
        ///   Un metodo per identificare questo gestore di eventi esterno
        /// </summary>
        public String GetName()
        {
            return "R2014 External Event Sample";
        }

        /// <summary>
        ///   Il metodo principale del gestore di eventi.
        /// </summary>
        /// <remarks>
        ///   Viene chiamato da Revit dopo che è stato generato l'evento esterno corrispondente 
        ///   (dal modulo non modale) e Revit ha raggiunto il momento in cui potrebbe 
        ///   chiamare il gestore dell'evento (cioè questo oggetto)
        /// </remarks>
        /// 
        public void Execute(UIApplication uiapp)
        {
            try
            {
                switch (Request.Take())
                {
                    case RequestId.None:
                        {
                            return;  // no request at this time -> we can leave immediately
                        }
                    case RequestId.Default:
                        {
                            _elementList = GetElementsfromDb(uiapp);
                            getElementsForm = App.thisApp.RetriveForm();
                            getElementsForm.FillDataGrid();
                            break;
                        }
                    case RequestId.Id:
                        {
                            getElementsForm = App.thisApp.RetriveForm();
                            ElementId id = getElementsForm.GetElemId();
                            _elementSingle = GetSingleElement(uiapp, id);
                            getElementsForm.SetListBox();
                            break;
                        }
                    default:
                        {
                            // Una sorta di avviso qui dovrebbe informarci di una richiesta imprevista
                            break;
                        }
                }
            }
            finally
            {
                App.thisApp.WakeFormUp();
                App.thisApp.ShowFormTop();
            }

            return;
        }

        /// <summary>
        ///   Metodo richiamato nello switch
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name="uiapp">L'oggetto Applicazione di Revit</param>m>
        /// 
        public List<string[]> GetElementsfromDb(UIApplication uiapp)
        {
            List<string[]> stringsList = new List<string[]>();
            List<Element> elements = new List<Element>();

            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            //// Metodo per catturare tutti gli elementi del Document
            //FilteredElementCollector collector = new FilteredElementCollector(doc);
            //collector.WherePasses(
            //  new LogicalOrFilter(
            //    new ElementIsElementTypeFilter(false),
            //    new ElementIsElementTypeFilter(true)));

            // Metodo per catturare i Curtain Panels del Document
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            ElementCategoryFilter categoryFilter = new ElementCategoryFilter(BuiltInCategory.OST_CurtainWallPanels);
            collector.WherePasses(categoryFilter);

            foreach (Element element in collector)
            {
                if(null != element.Category && element.Category.HasMaterialQuantities)
                {
                    elements.Add(element);
                }
            }

            // Dichiarazione delle liste per il conto delle proprieta' degli elementi
            List<string> instanceCount = new List<string>();
            List<string> categoryCount = new List<string>();
            List<string> typeCount = new List<string>();
            List<string> familyCount = new List<string>();
            List<string> ptiCount = new List<string>();
            List<string> uiCount = new List<string>();

            foreach (Element el in elements)
            {
                ElementId eTypeId = el.GetTypeId();
                ElementType eType = uiapp.ActiveUIDocument.Document.GetElement(eTypeId) as ElementType;

                string ui = PickUnitIdentifier(uiapp, el);
                string pti = PickPanelTypeIdentifier(uiapp, el);                

                if (!pti.Contains("xx"))
                {
                    if (eType != null)
                    {
                        stringsList.Add(new string[] {
                            Convert.ToString(_countElement),
                            Convert.ToString(el.Id),
                            el.Name,
                            el.Category.Name,
                            eType.Name,
                            eType.FamilyName,
                            ui,
                            pti
                        });
                        instanceCount.Add(el.Name);
                        categoryCount.Add(el.Category.Name);
                        typeCount.Add(eType.Name);
                        familyCount.Add(eType.FamilyName);
                        ptiCount.Add(pti);
                        uiCount.Add(ui);                    
                    }
                    else
                    {
                        stringsList.Add(new string[] {
                            Convert.ToString(_countElement),
                            Convert.ToString(el.Id),
                            el.Name,
                            el.Category.Name,
                            "xxx",
                            "xxx",
                            ui,
                            pti
                        });
                        instanceCount.Add(el.Name);
                        categoryCount.Add(el.Category.Name);
                        ptiCount.Add(pti);
                        uiCount.Add(ui);
                    }
                }
                _countElement++;

            }
            
            _count.Add(_countElement);
            _countInstance = instanceCount.Distinct().Count();
            _count.Add(_countInstance);
            _countCategory = categoryCount.Distinct().Count();
            _count.Add(_countCategory);
            _countType = typeCount.Distinct().Count();
            _count.Add(_countType);
            _countFamily = familyCount.Distinct().Count();
            _count.Add(_countFamily);
            _countUI = uiCount.Distinct().Count();
            _count.Add(_countUI);
            _countPTI = ptiCount.Distinct().Count();
            _count.Add(_countPTI);

            return stringsList;
        }

        public ArrayList GetSingleElement(UIApplication uiapp, ElementId eleId)
        {
            ArrayList elementStr = new ArrayList();

            Element el = uiapp.ActiveUIDocument.Document.GetElement(eleId);
            ElementId eTypeId = el.GetTypeId();
            ElementType eType = uiapp.ActiveUIDocument.Document.GetElement(eTypeId) as ElementType;

            string ui = PickUnitIdentifier(uiapp, el);
            string pti = PickPanelTypeIdentifier(uiapp, el);

            elementStr.Add("Id: " + el.Id);
            elementStr.Add("Instance: " + el.Name);
            elementStr.Add("Category: " + el.Category.Name);
            if (eType != null)
            {
                elementStr.Add("Type: " + eType.Name);
                elementStr.Add("Family: " + eType.FamilyName);
            } else
            {
                elementStr.Add("Type: xxx");
                elementStr.Add("Family: xxx");
            }
            elementStr.Add("UI: " + ui);
            elementStr.Add("PTI: " + pti);
            elementStr.Add("GroupId: " + el.GroupId);
            elementStr.Add("VersionGuid: " + el.VersionGuid);
            elementStr.Add("AssemblyInstanceId: " + el.AssemblyInstanceId);
            elementStr.Add("LevelId: " + el.LevelId);
            elementStr.Add("OwnerViewId: " + el.OwnerViewId);
            elementStr.Add("WorksetId: " + el.WorksetId);


            return elementStr;
        }

        // <summary>
        ///   La subroutine di selezione di un elemento che torna il valore stringa dell'Unit Identifier
        /// </summary>
        /// <remarks>
        /// Il valore dell'UnitIdentifier e' composto dai Parametri dell'elemento UI-ItemCategory, 
        /// UI-ProjectAbbreviation, UI-Quadrant, UI-FloorNumber e UI-UnitNumber
        /// </remarks>
        /// <param name="uiapp">L'oggetto Applicazione di Revit</param>m>
        /// 
        private string PickUnitIdentifier(UIApplication uiapp, Element ele)
        {
            // Chiamo la vista attiva e seleziono gli elementi che mi servono
            UIDocument uidoc = uiapp.ActiveUIDocument;
            ElementType eleType = uidoc.Document.GetElement(ele.GetTypeId()) as ElementType;

            // Restituisce il valore del parametro UI-ItemCategory  
            string strUIItemCategory = "";
            if (eleType != null && eleType.LookupParameter("UI-ItemCategory") != null)
            {
                Parameter par = eleType.LookupParameter("UI-ItemCategory");
                strUIItemCategory = par.AsString();
            }
            else { strUIItemCategory = "xxx"; }

            // Restituisce il valore del parametro UI-ProjectAbbreviation  
            string strUIProjectAbbreviation = "";
            if (eleType != null && eleType.LookupParameter("UI-ProjectAbbreviation") != null)
            {
                Parameter par = eleType.LookupParameter("UI-ProjectAbbreviation");
                strUIProjectAbbreviation = par.AsString();
            }
            else { strUIProjectAbbreviation = "xxx"; }

            // Restituisce il valore del parametro UI-Quadrant
            string strUIQuadrant = "";
            if (ele.LookupParameter("UI-Quadrant") != null)
            {
                Parameter par = ele.LookupParameter("UI-Quadrant");
                strUIQuadrant = par.AsString();
            }
            else { strUIQuadrant = "xx"; }

            // Restituisce il valore del parametro UI-FloorNumber
            string strUIFloorNumber = "";
            if (ele.LookupParameter("UI-FloorNumber") != null)
            {
                Parameter par = ele.LookupParameter("UI-FloorNumber");
                strUIFloorNumber = par.AsString();
            }
            else { strUIFloorNumber = "xx"; }

            // Restituisce il valore del parametro UI-UnitNumber
            string strUIUnitNumber = "";
            if (ele.LookupParameter("UI-UnitNumber") != null)
            {
                Parameter par = ele.LookupParameter("UI-UnitNumber");
                strUIUnitNumber = par.AsString();
            }
            else { strUIUnitNumber = "xxx"; }

            // Imposta la stringa finale
            _unitIdentifier =
                strUIItemCategory + "-" +
                strUIProjectAbbreviation + "-" +
                strUIQuadrant + "-" +
                strUIFloorNumber + "-" +
                strUIUnitNumber;

            return _unitIdentifier;
        }

        /// <summary>
        ///   La subroutine di selezione di un elemento che torna il valore stringa del Panel Type Identifier
        /// </summary>
        /// <remarks>
        /// Il valore del Panel Type Identifier e' composto dai Parametri dell'elemento PNT-ItemCategory, 
        /// PNT-ProjectAbbreviation, PNT-WallType e PNT-PanelType        
        /// </remarks>
        /// <param name="uiapp">L'oggetto Applicazione di Revit</param>m>
        /// 
        private string PickPanelTypeIdentifier(UIApplication uiapp, Element ele)
        {
            // Dall'elemento ottengo l'elementType
            UIDocument uidoc = uiapp.ActiveUIDocument;
            ElementType eleType = uidoc.Document.GetElement(ele.GetTypeId()) as ElementType;

            // Restituisce il valore del parametro PNT-ItemCategory
            string strPNTItemCategory = "";
            if (eleType != null && eleType.LookupParameter("PNT-ItemCategory") != null)
            {
                Parameter par = eleType.LookupParameter("PNT-ItemCategory");
                strPNTItemCategory = par.AsString();
            }
            else { strPNTItemCategory = "xxx"; }

            // Restituisce il valore del parametro PNT-ProjectAbbreviation
            string strPNTProjectAbbreviation = "";
            if (eleType != null && eleType.LookupParameter("PNT-ProjectAbbreviation") != null)
            {
                Parameter par = eleType.LookupParameter("PNT-ProjectAbbreviation");
                strPNTProjectAbbreviation = par.AsString();
            }
            else { strPNTProjectAbbreviation = "xxx"; }

            // Restituisce il valore del parametro PNT-WallType
            string strPNTWallType = "";
            if (eleType != null && eleType.LookupParameter("PNT-WallType") != null)
            {
                Parameter par = eleType.LookupParameter("PNT-WallType");
                strPNTWallType = par.AsString();
            }
            else { strPNTWallType = "xxxx"; }

            // Restituisce il valore del parametro PNT-PanelType
            string strPNTPanelType = "";
            if (eleType != null && ele.LookupParameter("PNT-PanelType") != null)
            {
                Parameter par = ele.LookupParameter("PNT-PanelType");
                strPNTPanelType = par.AsString();
            }
            else { strPNTPanelType = "xx"; }

            _panelTypeIdentifier =
                strPNTItemCategory + "-" +
                strPNTProjectAbbreviation + "-" +
                strPNTWallType + "-" +
                strPNTPanelType;

            return _panelTypeIdentifier;
        }


    }  // class

}  // namespace

