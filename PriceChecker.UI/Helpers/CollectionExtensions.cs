namespace Genius.PriceChecker.UI.Helpers;

public static class CollectionExtensions
{
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
