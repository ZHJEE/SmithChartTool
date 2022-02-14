﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Series;
using SmithChartTool.Utility;

namespace SmithChartTool.Model
{
    public enum SmithChartType
    {
        Impedance,
        Admittance
    }

    public enum MarkerType
    {
        Ref,
        Normal
    }

    public class SmithChart
    {
        //public List<List<Complex32>> ConstRealImpedanceCircles { get; set; }
        public ObservableCollection<List<Complex32>> ConstRealImpedanceCircles { get; set; }
        public List<List<Complex32>> ConstImaginaryImpedanceCircles { get; set; }
        public List<List<Complex32>> ConstRealAdmittanceCircles { get; set; }
        public List<List<Complex32>> ConstImaginaryAdmittanceCircles { get; set; }
        public List<Complex32> RefMarkers { get; set; }
        public List<Complex32> Markers { get; set; }
        public List<List<Complex32>> IntermediateCurves { get; set; }


        private double _frequency;
        public double Frequency
        {
            get
            {
                return _frequency;
            }
            set
            {
                if (value != _frequency)
                {
                    if (!(double.TryParse(value.ToString(), out _frequency)))
                        throw new ArgumentException("Given frequency is invalid", "Frequency");
                }
            }
        }
        private ImpedanceElement _referenceImpedance;
        public ImpedanceElement ReferenceImpedance
        {
            get
            {
                return _referenceImpedance;
            }
            set
            {
                if (value != _referenceImpedance)
                {
                    _referenceImpedance = value;
                }
            }
        }
        private bool _isNormalized;
        public bool IsNormalized
        {
            get
            {
                return _isNormalized;
            }
            set
            {
                if (value != _isNormalized)
                {
                    if (!(bool.TryParse(value.ToString(), out _isNormalized)))
                        throw new ArgumentException("Only true or false allowed", "IsNormalized");
                }
            }
        }
        private bool _isImpedanceSmithChart;
        public bool IsImpedanceSmithChart
        {
            get
            {
                return _isImpedanceSmithChart;
            }
            set
            {
                if (value != _isImpedanceSmithChart)
                {
                    _isImpedanceSmithChart = value;
                    OnSmithChartChanged();
                }
            }
        }
        private bool _isAdmittanceSmithChart;
        public bool IsAdmittanceSmithChart
        {
            get
            {
                return _isAdmittanceSmithChart;
            }
            set
            {
                if (value != _isAdmittanceSmithChart)
                {
                    _isAdmittanceSmithChart = value;
                    OnSmithChartChanged();
                }
            }
        }

        public int NumResistanceCircles { get; private set; }
        public int NumReactanceCircles { get; private set; }
        public int NumConductanceCircles { get; private set; }
        public int NumSusceptanceCircles { get; private set; }


        public Complex32 GetConformalGammaValue(Complex32 input, SmithChartType inputType, bool isNormalized)
        {
            Complex32 _input = ImpedanceNormalized(input);

            if (inputType == SmithChartType.Impedance)
                return ((input - 1) / (input + 1));
            else if (inputType == SmithChartType.Admittance)
                return -((input - 1) / (input + 1));
            else
                return Complex32.NaN;
        }

        public Complex32 ImpedanceNormalized(Complex32 impedance)
        {
            if (IsNormalized == true)
                return (impedance / ReferenceImpedance.Impedance);
            return impedance;
        }
        private void CalculateConstRealCircles(SmithChartType type)
        {
            ConstRealImpedanceCircles.Clear();
            ConstRealAdmittanceCircles.Clear();
            List<Complex32> plotList = new List<Complex32>();
            List<double> reRangeFull = new List<double> { 0, 0.2, 0.5, 1, 2, 5, 10, 50};
            List<double> values = Lists.GetLogRange(Math.Log(1e-6, 10), Math.Log(1e6, 10), 500); // imaginary value of every circle
            var temp = values.Invert();
            var temp2 = values;
            temp2.Reverse();
            temp.AddRange(temp2);
            values = temp;

            foreach (var re in reRangeFull) // for every real const circle
            {
                plotList.Clear();
                foreach (var im in values) // plot every single circle through conformal mapping
                {
                    Complex32 r = GetConformalGammaValue(new Complex32((float)re, (float)im), type, true);
                    plotList.Add(r);
                }
                if (type == SmithChartType.Impedance)
                    ConstRealImpedanceCircles.Add(new List<Complex32>(plotList));
                else if (type == SmithChartType.Admittance)
                    ConstRealAdmittanceCircles.Add(new List<Complex32>(plotList));
                else
                    throw new ArgumentException("Wrong SmithChart Type", "type");
            }
        }

        private void CalculateConstImaginaryCircles(SmithChartType type)
        {
            ConstImaginaryImpedanceCircles.Clear();
            ConstImaginaryAdmittanceCircles.Clear();
            List<Complex32> plotList = new List<Complex32>();
            List<double> values = Lists.GetLogRange(Math.Log(1e-6, 10), Math.Log(1e6, 10), 1000); // real value of every circle
            List<double> imRange = new List<double>() { 0.2, 0.5, 1, 2, 5, 10, 20, 50 };
            List<double> imRangeFull = new List<double>(imRange.Invert());
            imRangeFull.Add(1e-20);  // "zero" line
            imRangeFull.AddRange(imRange);

            foreach (var im in imRangeFull) // for every imaginary const circle
            {
                plotList.Clear();
                foreach (var re in values) // data point for every single circle through conformal mapping
                {
                    Complex32 r = GetConformalGammaValue(new Complex32((float)re, (float)im), type, true);
                    plotList.Add(r);
                }
                if (type == SmithChartType.Impedance)
                    ConstImaginaryImpedanceCircles.Add(new List<Complex32>(plotList));
                else if (type == SmithChartType.Admittance)
                    ConstImaginaryAdmittanceCircles.Add(new List<Complex32>(plotList));
                else
                    throw new ArgumentException("Wrong SmithChart Type", "type");
            }
        }

        public void Create(SmithChartType type)
        {
            CalculateConstRealCircles(type);
            CalculateConstImaginaryCircles(type);

            if (type == SmithChartType.Impedance && (IsImpedanceSmithChart == false))
            {
                IsImpedanceSmithChart = true;
            }
            else if (type == SmithChartType.Admittance && (IsAdmittanceSmithChart == false))
            {
                IsAdmittanceSmithChart = true;
            }
            else
                return;

            //OnSmithChartChanged();
        }


        public void Clear(SmithChartType type)
        {
            if (type == SmithChartType.Impedance && IsImpedanceSmithChart)
            {
                IsImpedanceSmithChart = false;
            }
            else if (type == SmithChartType.Admittance && IsAdmittanceSmithChart)
            {
                IsAdmittanceSmithChart = false;
            }
            else
                return;

            //OnSmithChartChanged();
        }

        private void CalculateMarker(Complex32 inputVal, SmithChartType type, MarkerType mtype)
        {
            Complex32 gamma = GetConformalGammaValue(inputVal, type, IsNormalized);
            if (mtype == MarkerType.Ref)
                RefMarkers.Add(gamma);
            else if (mtype == MarkerType.Normal)
                Markers.Add(gamma);
        }

        private void CalculateIntermediateCurve(Complex32 inputValNew, Complex32 inputValOld, SmithChartType type, int numberOfPoints = 1000)
        {
            List<Complex32> plotPoints = new List<Complex32>();
 
            Complex32 gamma;
            List <double> listReal = Lists.GetLinRange(inputValOld.Real, inputValNew.Real, numberOfPoints);
            List <double> listImaginary = Lists.GetLinRange(inputValOld.Imaginary, inputValNew.Imaginary, numberOfPoints);

            for ( int i = 0; i < numberOfPoints; i++)
            {
                gamma = GetConformalGammaValue(new Complex32((float)listReal[i], (float)listImaginary[i]), type, IsNormalized);
                plotPoints.Add(gamma);
            }
            IntermediateCurves.Add(plotPoints);
        }

        public void UpdateCurves(IList<InputImpedance> inputImpedances)
        {
            Markers.Clear();
            RefMarkers.Clear();
            IntermediateCurves.Clear();
            
            for (int i = 0; i<inputImpedances.Count-1; i++)
            {
                if (i > 0)
                {
                    CalculateIntermediateCurve(inputImpedances[i].Impedance, inputImpedances[i - 1].Impedance, inputImpedances[i].Type);
                }
                CalculateMarker(inputImpedances[i].Impedance, inputImpedances[i].Type, MarkerType.Normal);  
            }
            CalculateMarker(inputImpedances.Last().Impedance, inputImpedances.Last().Type, MarkerType.Ref);

            OnSmithChartCurvesChanged();
        }


        public SmithChart()
        {
            Frequency = 1.0e9;
            ReferenceImpedance = new ImpedanceElement(new Complex32(50, 0));
            IsNormalized = false;
            //ConstRealImpedanceCircles = new List<List<Complex32>>();
            ConstRealImpedanceCircles = new ObservableCollection<List<Complex32>>();
            ConstImaginaryImpedanceCircles = new List<List<Complex32>>();
            ConstRealAdmittanceCircles = new List<List<Complex32>>();
            ConstImaginaryAdmittanceCircles = new List<List<Complex32>>();
            Markers = new List<Complex32>();
            RefMarkers = new List<Complex32>();
            IntermediateCurves = new List<List<Complex32>>();
            IsImpedanceSmithChart = false;
            IsAdmittanceSmithChart = false;            
        }

        public event EventHandler SmithChartChanged;
        protected void OnSmithChartChanged()
        {
            SmithChartChanged?.Invoke(this, new EventArgs());
        }

        public event EventHandler SmithChartCurvesChanged;
        protected void OnSmithChartCurvesChanged()
        {
            SmithChartCurvesChanged?.Invoke(this, new EventArgs());
        }
    }
}
