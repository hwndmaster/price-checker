using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;

namespace Genius.PriceChecker.UI.Forms.AutoGrid
{
    public class AutoGridColumnContext
    {
        public AutoGridColumnContext(DataGrid dataGrid, DataGridAutoGeneratingColumnEventArgs args,
            PropertyDescriptor property)
        {
            DataGrid = dataGrid;
            Args = args;
            Property = property;
        }

        public DataGrid DataGrid { get; }
        public DataGridAutoGeneratingColumnEventArgs Args { get; }
        public PropertyDescriptor Property { get; }

        public T GetAttribute<T>() where T: Attribute
            => Property.Attributes.OfType<T>().FirstOrDefault();

        public Binding GetBinding()
            => (Args.Column as DataGridBoundColumn)?.Binding as Binding;
    }
}
