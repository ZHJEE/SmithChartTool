﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using SmithChartTool.View;
using SmithChartTool.Model;
using SmithChartTool.Utility;
using MathNet.Numerics;
using System.IO;
using OxyPlot;
using OxyPlot.Series;
using System.Windows.Controls;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.ComponentModel;
using System.Windows.Media;

namespace SmithChartTool.ViewModel
{
    public class MainViewModel : IDragDrop, INotifyPropertyChanged
    {
        public enum StatusType
        {
            Ready,
            Busy,
            Error
        }
        private MainWindow Window { get; set; }
        public SmithChart SC { get; private set; }
        public Schematic Schematic { get; private set; }
        public ObservableCollection<InputImpedance> _inputImpedances;
        public ObservableCollection<InputImpedance> InputImpedances
        {
            get { return _inputImpedances; }
            set
            {
                _inputImpedances = value;
                OnPropertyChanged("InputImpedances");
            }
        }
        private string _projectName;
        public string ProjectName
        {
            get { return _projectName; }
            set
            {
                if (_projectName != value)
                    _projectName = value;
                OnPropertyChanged("ProjectName");
            }
        }
        private string _projectPath;
        public string ProjectPath
        {
            get { return _projectPath; }
            set
            {
                if (_projectPath != value)
                    _projectPath = value;
                OnPropertyChanged("ProjectPath");
            }
        }
        private string _projectDescription;
        public string ProjectDescription
        {
            get { return _projectDescription; }
            set
            {
                if(_projectDescription != value)
                _projectDescription = value;
                OnPropertyChanged("ProjectDescription");
            }
        }
        public int Progress { get; private set; }
        public StatusType Status { get; private set; }

        public Log LogData { get; set; }

        public static event Action<int> ProgressChanged;
        public static event Action<StatusType> StatusChanged;

        public static RoutedUICommand CommandTestFeature = new RoutedUICommand("Run Test Feature", "RTFE", typeof(MainWindow), new InputGestureCollection() { new KeyGesture(Key.T, ModifierKeys.Control) });
        public static RoutedUICommand CommandShowLogWindow = new RoutedUICommand("Show Log Window", "SLW", typeof(MainWindow), new InputGestureCollection() { new KeyGesture(Key.L, ModifierKeys.Control) });
        public static RoutedUICommand CommandShowAboutWindow = new RoutedUICommand("Show About Window", "SAW", typeof(MainWindow));
        public static RoutedUICommand CommandSaveProject = new RoutedUICommand("Save project file", "PS", typeof(MainWindow), new InputGestureCollection() { new KeyGesture(Key.S, ModifierKeys.Control) });
        public static RoutedUICommand CommandOpenProject = new RoutedUICommand("Open project file", "PO", typeof(MainWindow), new InputGestureCollection() { new KeyGesture(Key.O, ModifierKeys.Control) });
        public static RoutedUICommand CommandExportSmithChartImage = new RoutedUICommand("Export Smith Chart image", "ESCI", typeof(MainWindow), new InputGestureCollection() { new KeyGesture(Key.I, ModifierKeys.Control) });
        public static RoutedUICommand CommandExit = new RoutedUICommand("CloseApplication", "EXIT", typeof(MainWindow), new InputGestureCollection() { new KeyGesture(Key.F4, ModifierKeys.Alt) });
        public static RoutedUICommand CommandXYAsync = new RoutedUICommand("Run XY Async", "RXYA", typeof(MainWindow), new InputGestureCollection() { new KeyGesture(Key.F5), new KeyGesture(Key.F5, ModifierKeys.Control) });

        private const int ProgressUpdateIntervall = 400;
        private const int FinishedDelay = 400;
        private const char HeaderMarker = '#';
        private const char DataMarker = '!';

        public MainViewModel()
        {
            LogData = new Log();
            SC = new SmithChart();
            Schematic = new Schematic();
            InputImpedances = new ObservableCollection<InputImpedance>();
            ProjectDescription = "Empty project.";

            InsertSchematicElement(-1, SchematicElementType.CapacitorSerial, 22e-12);
            InsertSchematicElement(-1, SchematicElementType.ResistorSerial, 23);
            InsertSchematicElement(-1, SchematicElementType.InductorSerial, 10e-9);

            Window = new MainWindow(this);
            Window.CommandBindings.Add(new CommandBinding(CommandTestFeature, (s, e) => { RunTestFeature(); }));
            Window.CommandBindings.Add(new CommandBinding(CommandXYAsync, (s, e) => { RunXYAsync(); }, (s, e) => { Debug.Print("Blab"); })); //e.CanExecute = bli; }));
            Window.CommandBindings.Add(new CommandBinding(CommandExportSmithChartImage, (s, e) => { RunExportSmithChartImage(); }));
            Window.CommandBindings.Add(new CommandBinding(CommandShowLogWindow, (s, e) => { RunShowLogWindow(); }));
            Window.CommandBindings.Add(new CommandBinding(CommandShowAboutWindow, (s, e) => { RunShowAboutWindow(); }));
            Window.CommandBindings.Add(new CommandBinding(CommandSaveProject, (s, e) => { RunSaveProject(); }));
            Window.CommandBindings.Add(new CommandBinding(CommandOpenProject, (s, e) => { RunOpenProject(); }));
            Window.oxySmithChart.ActualController.UnbindMouseDown(OxyMouseButton.Left);
            Window.oxySmithChart.ActualController.BindMouseEnter(OxyPlot.PlotCommands.HoverPointsOnlyTrack);

            Window.Show();
        }

        public void DropSchematicElement(int index, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("SchematicElement"))
            {
                SchematicElementType type = (SchematicElementType)e.Data.GetData("SchematicElement");
                InsertSchematicElement(index, type);
            }
        }

        public void InvalidateInputImpedances()
        {
            InputImpedances.Clear();
            Complex32 transformer;

            for (int i = Schematic.Elements.Count - 1; i > 0; i--)
            {
                if (Schematic.Elements[i].Type == SchematicElementType.Port)
                    InputImpedances.Add(new InputImpedance(i, Schematic.Elements[i].Impedance));
                else
                {
                    switch (Schematic.Elements[i].Type)
                    {
                        case SchematicElementType.ResistorSerial:
                            transformer = SmithChart.CalculateSerialResistorResistance(Schematic.Elements[i].Value);
                            break;
                        case SchematicElementType.CapacitorSerial:
                            transformer = SmithChart.CalculateSerialCapacitorReactance(Schematic.Elements[i].Value, SC.Frequency);
                            break;
                        case SchematicElementType.InductorSerial:
                            transformer = SmithChart.CalculateSerialInductorReactance(Schematic.Elements[i].Value, SC.Frequency);
                            break;
                        case SchematicElementType.ResistorParallel:
                            transformer = SmithChart.CalculateParallelResistorConductance(Schematic.Elements[i].Value);
                            break;
                        case SchematicElementType.CapacitorParallel:
                            transformer = SmithChart.CalculateParallelCapacitorSusceptance(Schematic.Elements[i].Value, SC.Frequency);
                            break;
                        case SchematicElementType.InductorParallel:
                            transformer = SmithChart.CalculateParallelInductorSusceptance(Schematic.Elements[i].Value, SC.Frequency);
                            break;
                        case SchematicElementType.TLine:
                            transformer = Complex32.Zero;
                            break;
                        case SchematicElementType.OpenStub:
                            transformer = new Complex32(0, -(Schematic.Elements[i].Impedance.Real * (float)Math.Tan(Schematic.Elements[i].Value)));
                            break;
                        case SchematicElementType.ShortedStub:
                            transformer = new Complex32(0, (Schematic.Elements[i].Impedance.Real * (float)Math.Tan(Schematic.Elements[i].Value)));
                            break;
                        case SchematicElementType.ImpedanceSerial:
                            transformer = Schematic.Elements[i].Impedance;
                            break;
                        case SchematicElementType.ImpedanceParallel:
                            transformer = 1 / (Schematic.Elements[i].Impedance);
                            break;
                        default:
                            transformer = Complex32.Zero;
                            break;
                    }
                    if (transformer == Complex32.Zero)
                    {
                        InputImpedances.Add(new InputImpedance(i, InputImpedances.Last().Impedance));
                    }
                    else
                    {
                        switch (Schematic.Elements[i].Type)
                        {
                            case SchematicElementType.ResistorSerial:
                            case SchematicElementType.CapacitorSerial:
                            case SchematicElementType.InductorSerial:
                            case SchematicElementType.ImpedanceSerial:
                                InputImpedances.Add(new InputImpedance(i, InputImpedances.Last().Impedance + transformer));
                                break;
                            case SchematicElementType.ResistorParallel:
                            case SchematicElementType.CapacitorParallel:
                            case SchematicElementType.InductorParallel:
                            case SchematicElementType.ImpedanceParallel:
                            case SchematicElementType.OpenStub:
                            case SchematicElementType.ShortedStub:
                                InputImpedances.Add(new InputImpedance(i, 1 / ((1 / InputImpedances.Last().Impedance) + (1 / transformer))));
                                break;
                            case SchematicElementType.TLine:
                                InputImpedances.Add(new InputImpedance(i, 0));
                                float z1 = Schematic.Elements[i].Impedance.Real * (float)Math.Tan(Schematic.Elements[i].Value);
                                Complex32 z2 = Complex32.Multiply(InputImpedances.Last().Impedance, (Complex32)Trig.Tan(Schematic.Elements[i].Value));
                                //InputImpedances.Add( Complex32.Multiply(schematic.Elements[i].Impedance.Real ,((Complex32.Add(InputImpedances.Last().Impedance, z1)) / (Complex32.Add(schematic.Elements[i].Impedance, z2)))));
                                InputImpedances.Add(new InputImpedance(i, InputImpedances.Last().Impedance)); // not implemented yet
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            OnPropertyChanged("InputImpedances");
            SC.InvalidateMarkers(InputImpedances);
        }

        public void InsertSchematicElement(int index, SchematicElementType type)
        {
            Schematic.InsertElement(index, type);
            InvalidateInputImpedances();
            LogData.AddLine("[schematic] " + GetSchematicElementTypeDescription(type) + " added to schematic.");
        }

        public void InsertSchematicElement(int index, SchematicElementType type, double value)
        {
            Schematic.InsertElement(index, type, value);
            InvalidateInputImpedances();
            LogData.AddLine("[schematic] " + GetSchematicElementTypeDescription(type) + " added to schematic.");
        }

        public void InsertSchematicElement(int index, SchematicElementType type, Complex32 impedance, double value = 0)
        {
            Schematic.InsertElement(index, type, impedance, value);
            InvalidateInputImpedances();
            LogData.AddLine("[schematic] " + GetSchematicElementTypeDescription(type) + " added to schematic.");
        }

        public void RemoveSchematicElement(int index)
        {
            LogData.AddLine("[schematic] " + GetSchematicElementTypeDescription(Schematic.Elements[index].Type) + " removed from schematic.");
            Schematic.RemoveElement(index);
            InvalidateInputImpedances();
        }

        private string GetSchematicElementTypeDescription(SchematicElementType type)
        {
            string typeDescription = "";
            Type t = type.GetType();
            var b = t.GetMember(type.ToString());

            if (b.Count() > 0)
            {
                var c = b[0].GetCustomAttributes(typeof(SchematicElementInfo), false);
                if (c.Count() > 0)
                {
                    SchematicElementInfo sei = (SchematicElementInfo)c[0];
                    if (sei != null)
                    {
                        typeDescription = sei.Name;
                    }
                }
            }
            return typeDescription;
        }

        public void RunExportSmithChartImage()
        {
            SaveFileDialog sfd = new SaveFileDialog();

            sfd.Filter = "PNG|*.png|BMP|*.bmp|JPEG|*.jpeg,*.jpg";
            sfd.Title = "Export Smith Chart image";

            sfd.ShowDialog();

            if (sfd.FileName != string.Empty)
            {
                LogData.AddLine("[image] Exporting Smith Chart to image \'(" + sfd.FileName + ")\'...");

                string ImExt = Path.GetExtension(sfd.FileName);
                OxyPlot.Wpf.PngExporter.Export(SC.Plot, sfd.FileName, 1000, 1000, OxyPlot.OxyColors.White, 300);
                //SvgExporter.Export()

                LogData.AddLine("[image] Done.");
            }
        }

        public static void ActionClear()
        {
            ProgressChanged = null;
            StatusChanged = null;
        }

        private static void ChangeProgress(int progress)
        {
            if (ProgressChanged != null)
                ProgressChanged.Invoke(progress);
        }

        private static void ChangeStatus(StatusType t)
        {
            if (StatusChanged != null)
                StatusChanged.Invoke(t);
        }

        public void RunSaveProject()
        {
            SaveFileDialog fd = new SaveFileDialog();

            //fd.Filter = "PNG|*.png|BMP|*.bmp|JPEG|*.jpeg,*.jpg";
            fd.Title = "Save project file...";

            fd.ShowDialog();

            if (fd.FileName != string.Empty)
            {
                LogData.AddLine("[fio] Saving project to file (\"" + fd.FileName + "\")...");

                string FileExt = Path.GetExtension(fd.FileName);
                SaveProjectToFile(fd.FileName, this.ProjectName, this.ProjectDescription, this.SC.Frequency, this.SC.IsNormalized, this.Schematic.Elements);

                LogData.AddLine("[fio] Done.");
            }
        }

        public void RunOpenProject()
        {
            OpenFileDialog fd = new OpenFileDialog();

            //fd.Filter = "PNG|*.png|BMP|*.bmp|JPEG|*.jpeg,*.jpg";
            fd.Title = "Open project file...";

            fd.ShowDialog();

            if (fd.FileName != string.Empty)
            {
                LogData.AddLine("[fio] Reading project file (\"" + fd.FileName + "\", ...).");

                string FileExt = Path.GetExtension(fd.FileName);
                readFromFile(fd.FileName);
                LogData.AddLine("[fio] " + this.Schematic.Elements.Count + " Schematic Elements loaded.");

                LogData.AddLine("[fio] Done.");
            }
        }

        public void SaveProjectToFile(string path, string projectName, string description, double frequency, bool isNormalized, ObservableCollection<SchematicElement> elements)
        {
            using (StreamWriter sw = new StreamWriter(path, false, Encoding.UTF8))
            {
                sw.WriteLine(HeaderMarker + DateTime.Today.ToString(" MMMM dd, yyyy") + " " + DateTime.Now.ToLongTimeString());
                sw.WriteLine(HeaderMarker + path);
                sw.WriteLine(HeaderMarker +""+ HeaderMarker + " Description");

                if (description != null && description.Length > 0)
                {
                    string[] descriptionStringArray = description.Split('\n');

                    if (descriptionStringArray != null && descriptionStringArray.Length > 0)
                        foreach (string str in descriptionStringArray)
                            if (str != null && str.Length > 1)
                            {
                                while (str.IndexOf('\n') != -1 && str.Length > 0)
                                    str.Remove(str.IndexOf('\n'));

                                sw.Write(HeaderMarker + "" + HeaderMarker + str);
                            }
                }
                sw.WriteLine();
                sw.WriteLine(HeaderMarker + " Settings");
                sw.WriteLine(DataMarker + "projectName " + projectName);
                sw.WriteLine(DataMarker + "frequency " + frequency);
                sw.WriteLine(DataMarker + "isNormalized " + isNormalized);
                sw.WriteLine(DataMarker + "numElements " + elements.Count());


                if (elements != null && elements.Count > 0)
                    for (int i = 0; i < elements.Count; ++i)
                    {
                        SchematicElement el = elements.ElementAt(i);
                        sw.WriteLine(el.ToStringSimple());

                        if (i % ProgressUpdateIntervall == 0)
                            ChangeProgress((int)(100.0 * i) / elements.Count);
                    }
                ChangeProgress(100);
                Thread.Sleep(FinishedDelay);
                ChangeProgress(0);
                ChangeStatus(StatusType.Ready);
            }
        }

        public string ReadDescriptionFromFile(string path)
        {
            string ret = string.Empty;

            if (path == string.Empty)
                path = ProjectPath;

            if (!File.Exists(path))
                return ret;

            using (StreamReader sr = File.OpenText(path))
            {
                bool addLines = false;

                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();

                    if (line.Length < 2)
                        continue;

                    if (addLines && line.Substring(0, 2) == HeaderMarker.ToString() )
                        ret += line.Remove(0, 2) + "\n";

                    // found description
                    if (line.Substring(0, 2) == (HeaderMarker.ToString() + HeaderMarker.ToString()))
                    {
                        if (addLines == true)
                            break;
                        addLines = true;
                    }
                }
            }

            return ret;
        }
        public List<SchematicElement> ReadProjectFromFile(string path, out string projectName, out double frequency)
        {
            projectName = "";
            frequency = 0.0;
            return new List<SchematicElement>();
        }

        public List<SchematicElement> ReadProjectFromFile(string path, out string projectName, out double frequency, out bool isNormalized)
        {
            List<SchematicElement> list = new List<SchematicElement>();
            projectName = "";
            frequency = 0.0;
            isNormalized = false;
            int numElements = 0;

            using (StreamReader sr = File.OpenText(path))
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();

                    if ((line.FirstOrDefault() == HeaderMarker) || line == "")
                        continue;

                    //const string dataMarkerString = DataMarker.ToString();
                    string[] data = line.Split(' '); // split line at every whitespace to generate multiple string entries

                    if (line.First() == DataMarker)
                    {
                        string argument = data[1];
                        switch (data[0])
                        {
                            /// TODO: use variable data Marker (problem with const.)
                            case ("!projectName"): projectName = argument; break;

                            case "!frequency": frequency = double.Parse(argument); break;

                            case "!isNormalized": isNormalized = bool.Parse(argument); break;

                            case "!numElements": numElements = int.Parse(argument); break;
                        }
                    }
                    else
                    {
                        list.Add(ElementFromLine(ref data));

                        if (numElements != 0 && list.Count % ProgressUpdateIntervall == 0)
                            ChangeProgress((int)(100.0 * list.Count) / numElements);
                    }
                }
            }

            ChangeProgress(100);
            Thread.Sleep(FinishedDelay);

            //if (list.Count != numElements)
            //{
            //    MessageBoxResult mbr = MessageBox.Show("Error opening project file (content). Revert project?", "Error opening project file.", MessageBoxButton.YesNo, MessageBoxImage.Error);

            //    // Revert back to previous state
            //    if (mbr == MessageBoxResult.Yes)
            //        throw new NotImplementedException();
            //        //RevertBack();
            //}
            return list;
        }

        public void readFromFile(string path)
        {
            List<SchematicElement> list = new List<SchematicElement>();
            string projectName = "";
            double frequency = 0.0;
            bool isNormalized = false;

            list = ReadProjectFromFile(path, out projectName, out frequency, out isNormalized);
            ProjectDescription = ReadDescriptionFromFile(path);
            ProjectPath = path;

            ChangeStatus(StatusType.Ready);
        }

        public SchematicElement ElementFromLine(ref string[] data)
        {
            return new SchematicElement() { Type = SchematicElementType.Port };
        }

        public async void RunTestFeature()
        {
            await Task.Run(() => MessageBox.Show(SC.ReferenceImpedance.Impedance.ToString()));
        }

         public void RunShowLogWindow()
        {
            var logWindowViewModel = new LogViewModel(LogData);
        }

        public void RunShowAboutWindow()
        {
            var aboutWindowViewModel = new AboutViewModel();
        }

        public async void RunXYAsync()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            await XYAsync();
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            MessageBox.Show(elapsedMs.ToString());
        }

        private async Task XYAsync()
        {
            var result = await Task.Run(() => 0);  // insert lambda body

            /// or (in case of parallel run)
            //List<Task> tasks = new List<Task>();
            //foreach (string data in websites)
            //{
            //    tasks.Add(RunXYAsync(data));
            //}
            //var results = await Task.WhenAll(tasks);
        }

        #region INotifyPropertyChanged Members  
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }

}
