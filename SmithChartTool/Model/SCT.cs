﻿using MathNet.Numerics;
using SmithChartTool.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmithChartTool.Model
{
    public class SCT
    {
        public enum StatusType
        {
            Ready,
            Busy,
            Error
        }
        public static void ClearAction()
        {
            StatusChanged = null;
        }
        private static void ChangeStatus(StatusType t)
        {
            if (StatusChanged != null)
                StatusChanged.Invoke(t);
        }

        public SmithChart SC { get; private set; }
        public Schematic Schematic { get; private set; }
        public ObservableCollection<InputImpedance> InputImpedances { get; set; }
        public Log LogData { get; private set; }
        public Project ProjectData { get; private set; }

        public StatusType Status { get; private set; }
        public static event Action<StatusType> StatusChanged;

        public SCT()
        {
            SC = new SmithChart();
            Schematic = new Schematic();
            InputImpedances = new ObservableCollection<InputImpedance>();
            LogData = new Log();
            ProjectData = new Project();

            InsertSchematicElement(-1, SchematicElementType.CapacitorParallel, 22e-12);
            InsertSchematicElement(-1, SchematicElementType.ResistorParallel, 23);
            InsertSchematicElement(-1, SchematicElementType.InductorParallel, 10e-9);
            InsertSchematicElement(-1, SchematicElementType.CapacitorSerial, 22e-12);
            InsertSchematicElement(-1, SchematicElementType.ResistorSerial, 23);
            InsertSchematicElement(-1, SchematicElementType.InductorSerial, 10e-9);
        }

        public void UpdateInputImpedances()
        {
            InputImpedances.Clear();
            Complex32 transformer;

            for (int i = Schematic.Elements.Count - 1; i >= 0; i--)
            {
                if (Schematic.Elements[i].Type == SchematicElementType.Port)
                    InputImpedances.Add(new InputImpedance(i, Schematic.Elements[i].Impedance));
                else
                {
                    switch (Schematic.Elements[i].Type)
                    {
                        case SchematicElementType.ResistorSerial:
                            transformer = RF.CalculateSerialResistorResistance(Schematic.Elements[i].Value);
                            break;
                        case SchematicElementType.CapacitorSerial:
                            transformer = RF.CalculateSerialCapacitorReactance(Schematic.Elements[i].Value, SC.Frequency);
                            break;
                        case SchematicElementType.InductorSerial:
                            transformer = RF.CalculateSerialInductorReactance(Schematic.Elements[i].Value, SC.Frequency);
                            break;
                        case SchematicElementType.ResistorParallel:
                            transformer = RF.CalculateParallelResistorConductance(Schematic.Elements[i].Value);
                            break;
                        case SchematicElementType.CapacitorParallel:
                            transformer = RF.CalculateParallelCapacitorSusceptance(Schematic.Elements[i].Value, SC.Frequency);
                            break;
                        case SchematicElementType.InductorParallel:
                            transformer = RF.CalculateParallelInductorSusceptance(Schematic.Elements[i].Value, SC.Frequency);
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
                                InputImpedances.Add(new InputImpedance(i, (1/(1 / InputImpedances.Last().Impedance) + (1 / transformer)), SmithChartType.Admittance));
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
            SC.UpdateCurves(InputImpedances);
        }

        public void InsertSchematicElement(int index, SchematicElementType type)
        {
            Schematic.InsertElement(index, type);
            UpdateInputImpedances();
            LogData.AddLine("[schematic] " + GetSchematicElementTypeDescription(type) + " added to schematic.");
        }

        public void InsertSchematicElement(int index, SchematicElementType type, double value)
        {
            Schematic.InsertElement(index, type, value);
            UpdateInputImpedances();
            LogData.AddLine("[schematic] " + GetSchematicElementTypeDescription(type) + " added to schematic.");
        }

        public void InsertSchematicElement(int index, SchematicElementType type, Complex32 impedance, double value = 0)
        {
            Schematic.InsertElement(index, type, impedance, value);
            UpdateInputImpedances();
            LogData.AddLine("[schematic] " + GetSchematicElementTypeDescription(type) + " added to schematic.");
        }

        public void RemoveSchematicElement(int index)
        {
            LogData.AddLine("[schematic] " + GetSchematicElementTypeDescription(Schematic.Elements[index].Type) + " removed from schematic.");
            Schematic.RemoveElement(index);
            UpdateInputImpedances();
        }

        public void RemoveSchematicElement(object param)
        {
            //LogData.AddLine("[schematic] " + GetSchematicElementTypeDescription(Schematic.Elements[index].Type) + " removed from schematic.");
            //Schematic.RemoveElement(index);
            Debug.Assert(param is SchematicElement);
            if (Schematic.Elements.Contains(param as SchematicElement))
                Schematic.Elements.Remove(param as SchematicElement);
            UpdateInputImpedances();
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

        public void NewProject()
        {
            ChangeStatus(StatusType.Busy);
            Schematic.Elements.Clear();
            ProjectData.Init();
            ChangeStatus(StatusType.Ready);
        }

        public void SaveProjectAs(string fileName, string fileExt = "sctprj")
        {
            if (ProjectData.Path != String.Empty)
            {
                ChangeStatus(StatusType.Busy);
                LogData.AddLine("[fio] Saving project to file (\"" + fileName + "\")...");
                FileIO.SaveProjectToFile(fileName, ProjectData.Name, ProjectData.Description, SC.Frequency, SC.ReferenceImpedance.Impedance, SC.IsNormalized, Schematic.Elements);
                LogData.AddLine("[fio] Done.");
                ChangeStatus(StatusType.Ready);
            }
            else
            {
                LogData.AddLine("[fio] Save-operation not successfull.");
            }
        }

        public void OpenProject(string fileName, string fileExt = "sctprj")
        {
            ChangeStatus(StatusType.Busy);
            NewProject();

            string projectName;
            string projectDescription;
            double frequency;
            Complex32 refImpedance;
            bool isNormalized;

            LogData.AddLine("[fio] Reading project file (\"" + fileName + "\", ...).");
            Schematic.Elements = FileIO.ReadProjectFromFile(fileName, out projectName, out projectDescription, out frequency, out refImpedance, out isNormalized);
            LogData.AddLine("[fio] " + this.Schematic.Elements.Count + " Schematic Elements loaded.");
            LogData.AddLine("[fio] Done.");

            ProjectData.Name = projectName;
            ProjectData.Description = projectDescription;
            SC.Frequency = frequency;
            SC.ReferenceImpedance.Impedance = refImpedance;
            SC.IsNormalized = isNormalized;
            ChangeStatus(StatusType.Ready);
        }

        public void ExportSmithChart(string fileName)
        {
            //var stream = File.Create(fileName);

            ChangeStatus(StatusType.Busy);
            LogData.AddLine("[image] Exporting Smith Chart to image \'(" + fileName + ")\'...");
            OxyPlot.Wpf.PngExporter.Export(SC.Plot, fileName, 1000, 1000, OxyPlot.OxyColors.White, 300);
            //OxyPlot.SvgExporter.Export(SC.Plot, stream, 1000, 1000, true);
            LogData.AddLine("[image] Done.");
            ChangeStatus(StatusType.Ready);
        }
    }
}
