﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using OxyPlot;
using OxyPlot.Series;
using SmithChartTool.View;
using SmithChartTool;
using SmithChartTool.Model;
using System.Collections.ObjectModel;

namespace SmithChartTool.ViewModel
{
    public class MainWindowViewModel : IDragDrop
    {
    
        /*
        // konforme Abbdildung in Matlab / Octave
        f = @(x) (x - 1) ./ (x + 1);

        close all;
        hold on;

        zero_line = 1E-20

        jrange = logspace(log10(0.15), log10(100), 10)

        # imag. const
        for jmul = [-jrange zero_line jrange]
            plot(f((0:0.1:100)+(j*jmul)))
        endfor

        # real. const
        for roffs = [0 0.2 0.5 1 2 5]
            plot(f(roffs + j*(-100:0.05:100)))
        endfor 
        */

        public PlotModel SmithChart { get; private set; }

        private MathNet.Numerics.Complex32 GetConformalValue(MathNet.Numerics.Complex32 z)
        {
            return ((z - 1) / (z + 1));
        }

        private List<double> GetRange(double start, double stop, int steps)
        {
            List<double> temp = new List<double>();
            for (int i=0; i<steps; i++)
            {
                temp.Add(start + (stop - start) * ((double)i / (steps - 1)));
            }
            return temp;

            //return Enumerable.Range(0, steps).Select(i => start + (stop-start) * ((double)i / (steps-1)));
        }

        public List<double> GetLogRange(double start, double stop, int steps)
        {
            double p = (stop - start) / (steps-1);
            List<double> temp = new List<double>();
            for (int i = 0; i < steps; i++)
            {
                temp.Add(Math.Pow(10.0, start+ p*i));
            }
            return temp;
        }

        public List<LineSeries> DrawSmithChart()
        {
            List<double> y = GetRange(0, 100, 1000);
            List<double> jrange = GetLogRange(Math.Log(0.15, 10), Math.Log(100, 10), 10);
            List<double> jrangeFull = new List<double>(jrange.Invert());
            
            jrangeFull.Add(1e-20);
            jrangeFull.AddRange(jrange);

            //List<List<MathNet.Numerics.Complex32>> imagConstValues = new List<List<MathNet.Numerics.Complex32>>();
            //List<List<DataPoint>> imagCurves = new List<List<DataPoint>>();
            
            List<LineSeries> series = new List<LineSeries>();

            int i = 0;
            foreach (var im in jrangeFull)
            {
                series.Add(new LineSeries { LineStyle = LineStyle.Dot });
                //imagConstValues.Add(new List<MathNet.Numerics.Complex32>());
                //imagCurves.Add(new List<DataPoint>());
                foreach (var re in y)
                {
                    MathNet.Numerics.Complex32 _z = GetConformalValue(new MathNet.Numerics.Complex32((float)re, (float)im));
                    //imagConstValues[i].Add(_z); // every i represents one circle with constant imaginary part
                    //imagCurves[i].Add(new DataPoint(_z.Real, _z.Imaginary));
                    series[i].Points.Add(new DataPoint(_z.Real, _z.Imaginary));
                }
                i++;
            }

            List<double> rrangeFull = new List<double> {0, 0.2, 0.5, 1, 2, 5, 10};
            List<double> x = GetLogRange(-10, Math.Log(100,10), 1000);//GetRange(-100, 100, 4000);
            x.AddRange(x.Invert());
            //List<List<MathNet.Numerics.Complex32>> realConstValues = new List<List<MathNet.Numerics.Complex32>>();
            //List<List<DataPoint>> realCurves = new List<List<DataPoint>>();

            i = 0;
            foreach (var re in rrangeFull)
            {
                series.Add(new LineSeries { LineStyle = LineStyle.Solid });
                //realConstValues.Add(new List<MathNet.Numerics.Complex32>());
                //realCurves.Add(new List<DataPoint>());
                foreach (var im in x)
                {
                    MathNet.Numerics.Complex32 _z = GetConformalValue(new MathNet.Numerics.Complex32((float)re, (float)im));
                    //_z = GetConformalValue(_z);
                    //realConstValues[i].Add(_z);  // every i represents one circle with constant real part
                    //realCurves[i].Add(new DataPoint(_z.Real, _z.Imaginary));
                    series[i].Points.Add(new DataPoint(_z.Real, _z.Imaginary));
                }
                i++;
            }

            return series;
        }

        public MainWindowViewModel()
        {
            this.SmithChart = new PlotModel();
            //this.SmithChart.LegendPosition = LegendPosition.RightBottom;
            //this.SmithChart.LegendPlacement = LegendPlacement.Outside;
            //this.SmithChart.LegendOrientation = LegendOrientation.Horizontal;
            this.SmithChart.IsLegendVisible = false;
            OxyPlot.Axes.LinearAxis XAxis = new OxyPlot.Axes.LinearAxis { Position = OxyPlot.Axes.AxisPosition.Bottom, Minimum = -1, Maximum = 1, IsZoomEnabled = false};
            OxyPlot.Axes.LinearAxis YAxis = new OxyPlot.Axes.LinearAxis { Position = OxyPlot.Axes.AxisPosition.Left, Minimum = -1, Maximum = 1, IsZoomEnabled = false };
            XAxis.Title = "Real";
            YAxis.Title = "Imaginary";
            this.SmithChart.Axes.Add(YAxis);
            this.SmithChart.Axes.Add(XAxis);
            this.SmithChart.DefaultColors = new List<OxyColor> {(OxyColors.Black)};

            List<LineSeries> series = DrawSmithChart();
            foreach (var item in series)
            {
                this.SmithChart.Series.Add(item);
            }
            this.SmithChart.InvalidatePlot(true);

            ArschBlubSource.Add(new SchematicElement() { Id = 1 });
            ArschBlubSource.Add(new SchematicElement() { Id = 2 });
            ArschBlubSource.Add(new SchematicElement() { Id = 3 });
        }

        public ObservableCollection<SchematicElement> ArschBlubSource { get; private set; } = new ObservableCollection<SchematicElement>();
        public ObservableCollection<SchematicElement> ArschBlubDest { get; private set; } = new ObservableCollection<SchematicElement>();


        public static RoutedUICommand CommandXYAsync = new RoutedUICommand("Run XY Async", "RXYA", typeof(MainWindowView), new InputGestureCollection() { new KeyGesture(Key.F5), new KeyGesture(Key.R, ModifierKeys.Control) });

        public async void RunCommandXYAsync()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            await RunXYAsync();
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            MessageBox.Show(elapsedMs.ToString());
        }

        private async Task RunXYAsync()
        {
            var result = await Task.Run(() => 0) ;  // insert lambda body

            /// or (in case of parallel run)
            //List<Task<XYDataModel>> tasks = new List<Task<XYDataModel>>();
            //foreach (string data in websites)
            //{
            //    tasks.Add(RunXYAsync(data));
            //}
            //var results = await Task.WhenAll(tasks);
        }

        public void Drop(DragEventArgs e)
        {
            if (e.Data.GetDataPresent("myFormat"))
            {
                SchematicElement element = e.Data.GetData("myFormat") as SchematicElement;
                ArschBlubDest.Add(element);
            }
        }
    }

}
