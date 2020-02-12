﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SmithChartTool.Model;

namespace SmithChartTool.View
{
    //[ContentProperty("Bildchen")]
    public class MySchematicElementSource : ContentControl
    {
        private Image img = null;
        static public DependencyProperty TypeProperty = DependencyProperty.Register("Type", typeof(string), typeof(MySchematicElementSource), new PropertyMetadata(SchematicElementType.ResistorSerial.ToString(), OnTypeChanged));
        static public DependencyProperty HeaderProperty = DependencyProperty.Register("Header", typeof(string), typeof(MySchematicElementSource), new PropertyMetadata(string.Empty));
        //static public DependencyProperty BildchenProperty = DependencyProperty.Register("Bildchen", typeof(object), typeof(MySchematicElementSource), new PropertyMetadata(null));


        public string Type
        {
            get { return (string)GetValue(TypeProperty); }
            set { SetValue(TypeProperty, value); }
        }

        public string Header
        {
            get { return (string)GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }

        //public object Bildchen
        //{
        //    get { return GetValue(BildchenProperty); }
        //    set { SetValue(BildchenProperty, value); }
        //}

        static MySchematicElementSource()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MySchematicElementSource), new FrameworkPropertyMetadata(typeof(MySchematicElementSource)));
        }

        private void UpdateImage()
        {
            //if(img != null)
            //{
                var a = typeof(SchematicElementType).FromName(Type);

                
                Type t = a.GetType();
                var b = t.GetMember(a.ToString());
                
                if(b.Count() > 0)
                {
                    var c = b[0].GetCustomAttributes(typeof(SchematicElementInfo), false);
                    if(c.Count() > 0)
                    {
                        SchematicElementInfo sei = (SchematicElementInfo)c[0];
                        Header = sei.Name;
                        //img.Source = new BitmapImage(new Uri("pack://application:,,,/Images/SchematicElements/"+ sei.Icon +".png"));
                        try
                        {
                            var sri = Application.GetResourceStream(new Uri("pack://application:,,,/Images/SchematicElements/" + sei.Icon + ".xaml"));
                            var aa = XamlReader.Load(sri.Stream);
                            //Bildchen = aa;
                            Content = aa;
                        }
                        catch (Exception)
                        {

                        }
                    }
                }

                return;

                Header = Type;
                switch (a)
                {
                    case SchematicElementType.Port:
                        img.Source = new BitmapImage(new Uri("pack://application:,,,/Images/SchematicElements/Port1.png"));
                        break;
                   
                    case SchematicElementType.ResistorSerial:
                        img.Source = new BitmapImage(new Uri("pack://application:,,,/Images/SchematicElements/ResistorSerial.png"));
                        break;
                    case SchematicElementType.ResistorParallel:
                        img.Source = new BitmapImage(new Uri("pack://application:,,,/Images/SchematicElements/ResistorParallel.png"));
                        break;
                    case SchematicElementType.CapacitorSerial:
                        img.Source = new BitmapImage(new Uri("pack://application:,,,/Images/SchematicElements/CapacitorSerial.png"));
                        break;
                    case SchematicElementType.CapacitorParallel:
                        img.Source = new BitmapImage(new Uri("pack://application:,,,/Images/SchematicElements/CapacitorParallel.png"));
                        break;
                    case SchematicElementType.InductorSerial:
                        img.Source = new BitmapImage(new Uri("pack://application:,,,/Images/SchematicElements/InductorSerial.png"));
                        break;
                    case SchematicElementType.InductorParallel:
                        img.Source = new BitmapImage(new Uri("pack://application:,,,/Images/SchematicElements/InductorParallel.png"));
                        break;
                    case SchematicElementType.TLine:
                        img.Source = new BitmapImage(new Uri("pack://application:,,,/Images/SchematicElements/TLine.png"));
                        break;
                    case SchematicElementType.OpenStub:
                        img.Source = new BitmapImage(new Uri("pack://application:,,,/Images/SchematicElements/OpenStub.png"));
                        break;
                    case SchematicElementType.ShortedStub:
                        img.Source = new BitmapImage(new Uri("pack://application:,,,/Images/SchematicElements/ShortedStub.png"));
                        break;
                    default:
                        img.Source = new BitmapImage(new Uri("pack://application:,,,/Images/SchematicElements/Default.png"));
                        break;
                }
            //}
        }

        public static void OnTypeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            (sender as MySchematicElementSource).UpdateImage();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            DependencyObject b = GetTemplateChild("PART_MySchematicElementSourceImage"); // UI element out of template
            if(b != null && (b.GetType() == typeof(Image)))
            {
                img = b as Image;
                UpdateImage();
            }

            UpdateImage();
        }
    }
}