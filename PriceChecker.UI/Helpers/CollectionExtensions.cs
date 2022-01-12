namespace Genius.PriceChecker.UI.Helpers;

public static class CollectionExtensions
{
    public static void ReplaceItems<T>(this ICollection<T> collection, IEnumerable<T> items)
    {
        collection.Clear();
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }

    // TODO: Not used yet.
    public static void ReplaceItemsGently<T>(this IList<T> collection, IEnumerable<T> items)
    {
        foreach (var itemToRemove in collection.Except(items).ToList())
        {
            collection.Remove(itemToRemove);
        }

        AppendItems(collection, items);
    }

    public static void AppendItems<T>(this IList<T> collection, IEnumerable<T> items)
    {
        var listItems = items.ToList();

        foreach (var item in listItems.Except(collection).ToList())
        {
            var index = listItems.IndexOf(item);
            collection.Insert(index, item);
        }
    }
}
