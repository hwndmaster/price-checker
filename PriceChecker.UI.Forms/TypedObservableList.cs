using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Genius.PriceChecker.UI.Forms
{
    public interface ITypedObservableList
    {
        Type ItemType { get; }
    }

    public class TypedObservableList<TContract, TType> : ObservableCollection<TContract>, ITypedObservableList, ITypedList
    {
        public PropertyDescriptorCollection GetItemProperties(PropertyDescriptor[] listAccessors)
        {
            return TypeDescriptor.GetProperties(typeof(TType));
        }

        public string GetListName(PropertyDescriptor[] listAccessors)
        {
            return null;
        }

        public Type ItemType => typeof(TType);
    }
}
