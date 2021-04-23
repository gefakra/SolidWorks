using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using System.Windows.Forms;
using System.IO;
using System.Windows.Input;
using Math;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace Save_File_As
{
    public class ConvertFile_s_ : INotifyPropertyChanged
    {
        #region private
        private OpenFileDialog _FileOpen;
        private string _Adress;
        #endregion
        #region public
        OpenFileDialog FileOpen { get => _FileOpen; set { _FileOpen = value; OnPropertyChanged(nameof(FileOpen)); } }
        SaveFileDialog Save { get => _Save; set { _Save = value; OnPropertyChanged(nameof(Save)); } }
        public string Adress { get => _Adress; set { _Adress = value; OnPropertyChanged(nameof(Adress)); } }
        public List<string> Elements { get; set; }
        public ObservableCollection<Document> Progress { get; set; }

        /// <summary>  Экземпляр SOLIDWORK  </summary>
        public SldWorks swApp;
        private SaveFileDialog _Save;
        #endregion
        public ICommand OpenCommand { get; private set; }
        public ICommand StartConvertCommand { get; private set; }
        public ConvertFile_s_()
        {
            StartConvertCommand = new DelegateCommand(OnLoad);
            OpenCommand = new DelegateCommand(SelectCatolog);
            swApp = new SldWorks();
            swApp.Visible = true;
            Progress = new ObservableCollection<Document>();
        }
        /// <summary>  Логика Конвертирования файлов  </summary>
        public void OnLoad(object p)
        {
            var a = string.Empty;
            foreach (var t in FileOpen.FileNames)
            {
                a += Path.GetFileName(Path.ChangeExtension(t, "PDF")) + ";";
            }
            Save = new SaveFileDialog()
            {
                FileName = a,
            };
            Save.ShowDialog(new Form() { TopMost = true });
            CreateListElements();
           
            OpenDrawingDocument(0, 0);
            swApp.ExitApp();
        }

        /// <summary> Выбрать файлы  </summary>
        private void SelectCatolog(object parametr)
        {
            FileOpen = new OpenFileDialog()
            {
                Multiselect = true,
                DefaultExt = ".SLDDRW",
                Filter = "Чертёж |*.SLDDRW| Сборка |*.SLDASM",
            };
            FileOpen.ShowDialog(new Form() { TopMost = true });
            Adress = Path.GetDirectoryName(FileOpen.FileName) + ":" + "   ";
            FileOpen.SafeFileNames.ToList().ForEach(item => { Adress += item.ToString(); Adress += ";"; });
        }

        /// <summary> документ сборка или чертёж?  </summary>
        private bool IsDocumentType(string FileName)
        {
            return Path.GetExtension(FileName).ToUpper() == ".SLDDRW" ? true : false;
        }
        /// <summary>  Формировать список чертежей  </summary>
        private void CreateListElements()
        {
            Elements = new List<string>();
            foreach (var Temp in FileOpen.FileNames)
            {
                if (IsDocumentType(Temp)) Elements.Add(Temp);
                else AddElementsOfAssembly(Temp, 0, 0).ForEach(item => Elements.Add(item));
            }
        }
        /// <summary>   добавить елементы сборки </summary>        
        private List<string> AddElementsOfAssembly(string FileName, int err, int exp)
        {
            List<string> _temp = new List<string>();
            ModelDoc2 _Model = swApp.OpenDoc6(FileName, (int)swDocumentTypes_e.swDocASSEMBLY, (int)swOpenDocOptions_e.swOpenDocOptions_Silent, "", ref err, ref exp);
            var _FileName = Path.ChangeExtension(FileName, "SLDDRW");
            if (File.Exists(_FileName)) _temp.Add(_FileName);
            foreach (var _component in ((AssemblyDoc)_Model).GetComponents(false))
            {
                var _Name = Path.ChangeExtension(((Component2)_component).GetPathName(), "SLDDRW");
                if (File.Exists(_Name)) _temp.Add(_Name);
            }
            return _temp;
        }
        /// <summary> Открыть  документ чертежа</summary>
        private void OpenDrawingDocument(int err, int exp)
        {
            Task.Run(() =>
            {
                foreach (var el in Elements)
                {
                    ModelDoc2 _Model = (ModelDoc2)swApp.OpenDoc6(el, (int)swDocumentTypes_e.swDocDRAWING, (int)swOpenDocOptions_e.swOpenDocOptions_Silent, "", ref err, ref exp);
                    ModelDocExtension _ModExt = (ModelDocExtension)_Model.Extension;
                    ExportPdfData _ExportData = (ExportPdfData)swApp.GetExportFileData((int)swExportDataFileType_e.swExportPdfData);
                    ConvertToPDF(el, _Model, _ExportData, _ModExt, 0, 0);                    
                }
            });
        }
        /// <summary> Конвертировать чертёж в документ и сохранить </summary>        
        private void ConvertToPDF(string _File, ModelDoc2 _Model, ExportPdfData _ExportPdfData, ModelDocExtension _ModExt, int err, int exp)
        {
            bool status = false;
            DrawingDoc FileType = _Model as DrawingDoc;
            string[] elementName = (string[])FileType.GetSheetNames();
            object[] objs = new object[elementName.Length - 1];
            DispatchWrapper[] _arrObjIn = new DispatchWrapper[elementName.Length - 1];
            for (int i = 0; i < elementName.Length - 1; i++)
            {
                status = FileType.ActivateSheet(elementName[i]);
                objs[i] = (Sheet)FileType.GetCurrentSheet();
                _arrObjIn[i] = new DispatchWrapper(objs[i]);
            }    
            status = _ExportPdfData.SetSheets((int)swExportDataSheetsToExport_e.swExportData_ExportSpecifiedSheets, (_arrObjIn));
            _ExportPdfData.ViewPdfAfterSaving = true;//visibl             
                                                     //status = _ModExt.SaveAs(Path.ChangeExtension(_File,"PDF"), (int)swSaveAsVersion_e.swSaveAsCurrentVersion, (int)swSaveAsOptions_e.swSaveAsOptions_Silent, _ExportPdfData, ref err, ref exp);
            var toSave = Path.Combine(Path.GetDirectoryName(Save.FileName.ToString()), Path.ChangeExtension(Path.GetFileName(_File), "PDF"));
            status = _ModExt.SaveAs(toSave, (int)swSaveAsVersion_e.swSaveAsCurrentVersion, (int)swSaveAsOptions_e.swSaveAsOptions_Silent, _ExportPdfData, ref err, ref exp);
            System.Windows.Application.Current.Dispatcher.Invoke(()=> Progress.Add(new Document() { status = "Конвертирован ", text = toSave })); //Path.ChangeExtension(_File,"PDF") });
            swApp.CloseDoc(_File);
        }
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>Cобытие изменения свойств </summary>
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------		

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
    public class Document
    {
        public string status { get; set; }
        public string text { get; set; }

    }
}
