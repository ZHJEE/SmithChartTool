﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmithChartToolLibrary
{
    public class SchematicElementInfo : Attribute
    {
        public string Name { get; }
        public string Icon { get; }
        public string Designator { get; }

        public SchematicElementInfo(string name, string icon, string designator)
        {
            Name = name;
            Icon = icon;
            Designator = designator;
        }
    }

    public class HideInList : Attribute
    {
        public HideInList()
        { }
    }
}
