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
using MathNet.Numerics;
using System.IO;
using OxyPlot.Wpf;
using System.Windows.Controls;
using System.Text;
using System.Threading;

namespace SmithChartTool.ViewModel
{
    public class MainWindowViewModel : IDragDrop
    {
        public enum StatusType
        {
            Ready,
            Busy,
            Error
        }

        public SmithChart SC { get; private set; }
        public Schematic Schematic { get; private set; }

        public string ProjectName { get; private set; }
        public string ProjectPath { get; private set; }
        public string ProjectDescription { get; private set; }
        public int Progress { get; private set; }

        public static event Action<int> ProgressChanged;
        public static event Action<StatusType> StatusChanged;

        private const int ProgressUpdateIntervall = 400;
        private const int FinishedDelay = 400;
        private const char HeaderMarker = '#';
        private const char DataMarker = '!';

        public MainWindowViewModel()
        {
            SC = new SmithChart();
            Schematic = new Schematic();
            Schematic.AddElement(SchematicElementType.ResistorParallel);
            Schematic.ChangeElementValue(1, 33);
        }

        public void Drop(int index, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("SchematicElement"))
            {
                SchematicElementType type = (SchematicElementType)e.Data.GetData("SchematicElement");
                Schematic.InsertElement(index, type);
            }
        }


        public static RoutedUICommand CommandSaveSmithChartImage = new RoutedUICommand("Save Smith Chart image", "RSSCI", typeof(MainWindow));
        public void RunSaveSmithChartImage()
        {
            SaveFileDialog sfd = new SaveFileDialog();

            sfd.Filter = "PNG|*.png|BMP|*.bmp|JPEG|*.jpeg,*.jpg";
            sfd.Title = "Export Smith Chart image";

            sfd.ShowDialog();

            if (sfd.FileName != string.Empty)
            {
                Log.AddLine("[image] Exporting Smith Chart to image \'(" + sfd.FileName + ")\'...");

                string ImExt = Path.GetExtension(sfd.FileName);
                PngExporter.Export(SC.Plot, sfd.FileName, 800, 600, OxyPlot.OxyColor.Parse("FFFFFF00"), 96);
                
                Log.AddLine("[image] Done.");
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

        public static void SaveProjectToFile(string path, string projectName, string description, double frequency, bool isNormalized, List<SchematicElement> elements)
        {
            Log.AddLine("[fio] Saving project to file (\"" + path + "\")...");

            using (StreamWriter sw = new StreamWriter(path, false, Encoding.UTF8))
            {
                sw.WriteLine(HeaderMarker + DateTime.Today.ToString(" MMMM dd, yyyy") + " " + DateTime.Now.ToLongTimeString());
                sw.WriteLine(HeaderMarker + path);
                sw.WriteLine(HeaderMarker + HeaderMarker + " Description");

                if (description != null && description.Length > 0)
                {
                    string[] descriptionStringArray = description.Split('\n');

                    if (descriptionStringArray != null && descriptionStringArray.Length > 0)
                        foreach (string str in descriptionStringArray)
                            if (str != null && str.Length > 1)
                            {
                                while (str.IndexOf('\n') != -1 && str.Length > 0)
                                    str.Remove(str.IndexOf('\n'));

                                sw.Write(HeaderMarker + str);
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

            Log.AddLine("[fio] Done.");
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
            Log.AddLine("[pfio] Starte readFromFile(\"" + path + "\", ...).");

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

            Log.AddLine("[fio] " + list.Count + " Schematic Elements loaded.");

            if (list.Count != numElements)
            {
                MessageBoxResult mbr = MessageBox.Show("Error opening project file (content). Revert project?", "Error opening project file.", MessageBoxButton.YesNo, MessageBoxImage.Error);

                // Revert back to previous state
                if (mbr == MessageBoxResult.Yes)
                    throw new NotImplementedException();
                    //RevertBack();
            }
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

        public static RoutedUICommand CommandTestFeature = new RoutedUICommand("Run Test Feature", "RTFE", typeof(MainWindow));
        public async void RunTestFeature()
        {
            await Task.Run(() => MessageBox.Show(SC.Frequency.ToString()));
        }



        public static RoutedUICommand CommandXYAsync = new RoutedUICommand("Run XY Async", "RXYA", typeof(MainWindow), new InputGestureCollection() { new KeyGesture(Key.F5), new KeyGesture(Key.R, ModifierKeys.Control) });
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
    }

}
